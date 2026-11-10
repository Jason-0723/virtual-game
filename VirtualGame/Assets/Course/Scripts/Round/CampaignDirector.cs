using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 闖關版的導演：三天 × 5 關，內容在 Resources/levels.json
// 每一關：有人來找你（臉部特寫＋選擇）→ 你在座位的電腦上玩小遊戲（鏡頭在肩膀後面）→ 結果卡
// 每天下班：起身走到主管旁邊談話（兩人中景／臉部特寫，主管會點頭、搖頭、生氣）
// 選項每次都打亂順序；偷懶、說謊會累積「技術債」，讓後面的關卡變難
public class CampaignDirector : MonoBehaviour
{
    [Serializable] public class Choice { public string text, reply, anim, flag; public int trust, debt; }
    [Serializable] public class Level { public string id, name, game, speaker, line, learn; public float seconds; public int diff; public Choice[] choices; }
    [Serializable] public class Talk { public string line, anim; public Choice[] choices; }
    [Serializable] public class Day { public string name, intro; public Level[] levels; public Talk[] endTalk; }
    [Serializable] public class Data { public Day[] days; }

    [Header("角色")]
    public Animator player, boss;
    public Animator lewis, suzie, martha;
    public Transform seat, bossDeskSpot, talkSpotPlayer, talkSpotBoss;
    [Header("鏡頭")]
    public CinemachineCamera gameCam, deskCam, deskFace, bossDeskFace, twoShot, playerFace, bossFace, lewisFace, suzieFace, marthaFace;
    [Header("螢幕")]
    public RectTransform monitorArea;
    public Text monitorStatus;

    public enum Phase { Card, Map, Talk, Game, Result, Ending }
    public Phase Now { get; private set; }
    public bool WaitingContinue { get; private set; }
    public bool WaitingChoice { get; private set; }
    public MonitorGame CurrentGame { get; private set; }
    public int Trust => trust;
    public int TotalStars { get { int s = 0; foreach (int v in stars.Values) s += v; return s; } }
    public List<string> LastChoices { get; } = new List<string>();   // 給測試：上一次選項畫面的順序
    public string Ending { get; private set; }
    public Transform Focus => dofTarget;   // 給測試：現在特寫的是誰的頭
    public float SubtitleTop => bar.activeSelf ? (BarY + BarH) / 1080f : 0;   // 給測試：字幕列頂端在畫面多高（0～1）
    public int MaxStars { get { int n = 0; foreach (Day d in data.days) n += d.levels.Length * 3; return n; } }

    Data data;
    readonly Dictionary<string, int> stars = new Dictionary<string, int>();
    readonly HashSet<string> flags = new HashSet<string>();
    readonly Dictionary<string, MonitorGame> games = new Dictionary<string, MonitorGame>();
    int trust = 50, debt;
    int picked = -1;
    readonly System.Random rng = new System.Random();
    GameObject ui, bar, fade;
    Text whoText, lineText, hud;
    RectTransform optionBox;
    int options;
    DepthOfField dof;
    Transform dofTarget;
    CinemachineCamera activeCam;

    void Awake()
    {
        data = JsonUtility.FromJson<Data>(Resources.Load<TextAsset>("levels").text);
        GameObject monitor = monitorArea.GetComponentInParent<Canvas>().gameObject;
        monitor.GetComponent<Canvas>().worldCamera = Camera.main;   // 滑鼠點世界空間的 UI 要知道是哪台相機
        void Add<T>(string key, Action<T> set = null) where T : MonitorGame
        {
            T g = monitor.AddComponent<T>();
            g.area = monitorArea; g.status = monitorStatus;
            set?.Invoke(g);
            games[key] = g;
        }
        Add<BugGame>("bug");
        Add<DiffGame>("diff");
        Add<LoadGame>("load");
        Add<DeployGame>("deploy");
        Add<SortGame>("triage", g => g.phishing = false);
        Add<SortGame>("phish", g => g.phishing = true);
        Add<BackupGame>("backup");
        Add<PasswordGame>("password");
        Add<PlanGame>("plan");
        Add<BisectGame>("bisect");
        Add<FirewallGame>("firewall");
        Add<ChecklistGame>("checklist");
        Add<JargonGame>("jargon");
        Add<RestartGame>("restart");
        Add<RouteGame>("route");
    }

    void Start()
    {
        Volume v = FindFirstObjectByType<Volume>();
        if (v) v.profile.TryGet(out dof);
        BuildUi();
        SitAtDesk();
        StartCoroutine(Run());
    }

    // ───── 三天的流程 ─────
    IEnumerator Run()
    {
        for (int d = 0; d < data.days.Length; d++)
        {
            Day day = data.days[d];
            if (d > 0)
            {
                yield return Fade(1);
                SitAtDesk();
                yield return new WaitForSeconds(0.2f);
                yield return Fade(0);
            }
            yield return Card(day.name, day.intro, "開始上班");
            for (int i = 0; i < day.levels.Length; i++)
            {
                Level lv = day.levels[i];
                yield return Map(lv);
                yield return LevelTalk(lv);
                while (true)
                {
                    int got = -1;
                    string summary = "";
                    yield return Play(lv, (s, msg) => { got = s; summary = msg; });
                    stars[lv.id] = Mathf.Max(stars.TryGetValue(lv.id, out int old) ? old : 0, got);
                    bool retry = false;
                    yield return Result(lv, got, summary, r => retry = r);
                    if (!retry) break;
                }
            }
            yield return DayEnd(d);
        }
        ShowEnding();
    }

    void SitAtDesk()
    {
        player.transform.SetPositionAndRotation(seat.position, seat.rotation);
        player.CrossFadeInFixedTime("Typing", 0f);
        boss.transform.SetPositionAndRotation(bossDeskSpot.position, bossDeskSpot.rotation);
        boss.CrossFadeInFixedTime("Idle", 0f);
        Cut(deskCam, null);
    }

    // ───── 關卡地圖 ─────
    IEnumerator Map(Level next)
    {
        Now = Phase.Map;
        Cut(deskCam, null);
        GameObject map = Ui.Rect("Map", ui.transform, new Color(0.07f, 0.07f, 0.09f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 760)).gameObject;
        Ui.Label(map.transform, $"關卡地圖　　★ {TotalStars} / {MaxStars}　　主管信任 {trust}　　技術債 {debt}", 30, Gold, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1200, 50), DayManager.UiFont, TextAnchor.UpperCenter);
        for (int d = 0; d < data.days.Length; d++)
        {
            Ui.Label(map.transform, data.days[d].name, 26, Color.white, new Vector2(0, 1), new Vector2(40, -120 - d * 190), new Vector2(300, 40), DayManager.UiFont, TextAnchor.UpperLeft);
            for (int i = 0; i < data.days[d].levels.Length; i++)
            {
                Level lv = data.days[d].levels[i];
                bool done = stars.ContainsKey(lv.id), current = lv == next;
                Color bg = current ? Gold : done ? new Color(1, 1, 1, 0.18f) : new Color(1, 1, 1, 0.06f);
                RectTransform node = Ui.Rect("Node", map.transform, bg, new Vector2(0, 1), new Vector2(40 + i * 248, -165 - d * 190), new Vector2(236, 130));
                string st = done ? new string('★', stars[lv.id]) + new string('☆', 3 - stars[lv.id]) : current ? "▶ 下一關" : "（未解鎖）";
                string hard = "難度 " + new string('▮', lv.diff + 1) + new string('▯', 5 - lv.diff);   // 越後面越難
                Ui.Label(node, $"{lv.id}　{lv.name}\n{hard}\n{st}", 22, current ? Ink : Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(226, 124), DayManager.UiFont, TextAnchor.MiddleCenter);
                if (current) node.gameObject.AddComponent<Button>().onClick.AddListener(() => WaitingContinue = false);
            }
        }
        Ui.Button(map.transform, $"開始 {next.id}　{next.name}", Gold, Ink, new Vector2(0, -330), new Vector2(520, 80), DayManager.UiFont, () => WaitingContinue = false);
        WaitingContinue = true;
        while (WaitingContinue) yield return null;
        Destroy(map);
    }

    // ───── 關卡前：有人來找你 ─────
    IEnumerator LevelTalk(Level lv)
    {
        Now = Phase.Talk;
        yield return Says(lv.speaker, lv.line, "Talk");
        Choice c = null;
        yield return Choose(lv.choices, x => c = x);
        yield return PlayerSays(c.text, atDesk: true);
        yield return Says(lv.speaker, c.reply, string.IsNullOrEmpty(c.anim) ? "Talk" : c.anim);
        Apply(c);
        bar.SetActive(false);
    }

    void Apply(Choice c)
    {
        trust = Mathf.Clamp(trust + c.trust, 0, 100);
        debt = Mathf.Max(0, debt + c.debt);
        if (!string.IsNullOrEmpty(c.flag)) flags.Add(c.flag);
    }

    // ───── 螢幕小遊戲 ─────
    IEnumerator Play(Level lv, Action<int, string> done)
    {
        Now = Phase.Game;
        Cut(gameCam, null);
        player.CrossFadeInFixedTime("Typing", 0.2f);
        hud.gameObject.SetActive(true);
        hud.text = $"{lv.id}　{lv.name}";
        for (int n = 3; n > 0; n--) { monitorStatus.text = $"{lv.name}　{n}"; yield return new WaitForSeconds(0.6f); }
        MonitorGame g = games[lv.game];
        CurrentGame = g;
        int difficulty = lv.diff + Mathf.Min(2, debt / 2);   // 技術債每 2 點，難度 +1（最多 +2）
        bool finished = false;
        g.Begin(difficulty, lv.seconds, (s, msg) => { finished = true; done(s, msg); });
        while (!finished) yield return null;
        CurrentGame = null;
        hud.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.8f);
    }

    IEnumerator Result(Level lv, int got, string summary, Action<bool> retry)
    {
        Now = Phase.Result;
        string title = got > 0 ? $"過關！　{new string('★', got)}{new string('☆', 3 - got)}" : "沒過關⋯⋯";
        string body = $"{summary}\n\n【小知識】{lv.learn}";
        if (got > 0) { yield return Card(title, body, "回關卡地圖"); retry(false); }
        else
        {
            bool again = false;
            yield return Card(title, body, "再試一次", null, "跳過（0 星）", () => again = true, () => again = false);
            retry(again);
        }
    }

    // ───── 下班：和主管談話 ─────
    IEnumerator DayEnd(int d)
    {
        Day day = data.days[d];
        int dayStars = 0;
        foreach (Level lv in day.levels) dayStars += stars.TryGetValue(lv.id, out int s) ? s : 0;
        yield return Fade(1);
        player.transform.SetPositionAndRotation(talkSpotPlayer.position, talkSpotPlayer.rotation);
        player.CrossFadeInFixedTime("Idle", 0f);
        boss.transform.SetPositionAndRotation(talkSpotBoss.position, talkSpotBoss.rotation);
        Cut(twoShot, null);
        yield return new WaitForSeconds(0.3f);
        yield return Fade(0);

        Now = Phase.Talk;
        if (dayStars >= day.levels.Length * 2.5f) yield return Says("boss", $"今天拿了 {dayStars} 顆星，做得很好。", "Nod");
        else if (dayStars >= day.levels.Length * 1.5f) yield return Says("boss", $"今天 {dayStars} 顆星。還可以，但還有進步的空間。", "Talk");
        else yield return Says("boss", $"今天只有 {dayStars} 顆星。我們得好好談談。", "Angry");

        foreach (Talk t in day.endTalk)
        {
            yield return Says("boss", t.line, string.IsNullOrEmpty(t.anim) ? "Talk" : t.anim);
            Choice c = null;
            yield return Choose(t.choices, x => c = x);
            yield return PlayerSays(c.text, atDesk: false);
            yield return Says("boss", c.reply, string.IsNullOrEmpty(c.anim) ? "Talk" : c.anim);
            Apply(c);
        }
        bar.SetActive(false);
        Cut(twoShot, null);
        if (d < data.days.Length - 1)
            yield return Card($"{day.name}　結束", $"今天的星星 {dayStars} / {day.levels.Length * 3}\n主管信任 {trust}　技術債 {debt}\n\n技術債越高，明天的關卡就越難。", "回家睡一覺");
    }

    void ShowEnding()
    {
        Now = Phase.Ending;
        int s = TotalStars;
        string title, body;
        if (flags.Contains("scapegoat")) { title = "結局：通過試用期，但⋯⋯"; body = "你通過了試用期，報告上那個漏洞，掛的是你的名字。\n主管對你很好。你常常想起簽名那天，手有點抖。"; }
        else if (trust >= 70 && s >= MaxStars * 0.72f) { title = "結局：轉正加薪"; body = "三天撐過來了，表現和信任都很漂亮。人資寄來轉正通知，還附了一點加薪。"; }
        else if (flags.Contains("whistle") || flags.Contains("together")) { title = "結局：準時下班的人"; body = "你沒有替任何人背鍋，也沒有藏任何錯誤。\n週五 18:00，你準時走出大門，雨剛好停了。"; }
        else if (trust >= 45) { title = "結局：撐過第一週"; body = "不算完美，但你撐過來了。下週一，還有新的關卡在等你。"; }
        else { title = "結局：試用期沒過"; body = "主管說：「你很努力，但我們需要更能溝通的人。」\n工程師不只寫程式，也要誠實、會求助、會說不。"; }
        Ending = title;
        body = $"★ {s} / {MaxStars}　　主管信任 {trust}　　技術債 {debt}\n\n{body}\n\n你不是一個人，辛苦了。累了，請找人說說話：衛福部安心專線 1925";
        StartCoroutine(Card(title, body, "再玩一次", () => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
    }

    // ───── 對話小工具 ─────
    Animator Who(string speaker) => speaker == "lewis" ? lewis : speaker == "suzie" ? suzie : speaker == "martha" ? martha : boss;
    string Name(string speaker) => speaker == "lewis" ? "學長 阿傑" : speaker == "suzie" ? "設計師 小美" : speaker == "martha" ? "PM 瑪莎" : "主管 Joe";

    IEnumerator Says(string speaker, string text, string anim)
    {
        Animator a = Who(speaker);
        bool standingTalk = boss.transform.position == talkSpotBoss.position;
        CinemachineCamera cam = speaker == "lewis" ? lewisFace : speaker == "suzie" ? suzieFace : speaker == "martha" ? marthaFace : standingTalk ? bossFace : bossDeskFace;
        Cut(cam, a.GetBoneTransform(HumanBodyBones.Head));
        if (a == boss) boss.CrossFadeInFixedTime(anim, 0.25f);   // 同事坐著打字，不換動作
        yield return Line(Name(speaker), text);
        if (a == boss) boss.CrossFadeInFixedTime("Idle", 0.3f);
    }

    IEnumerator PlayerSays(string text, bool atDesk)
    {
        Cut(atDesk ? deskFace : playerFace, player.GetBoneTransform(HumanBodyBones.Head));
        if (!atDesk) player.CrossFadeInFixedTime("Talk", 0.25f);
        yield return Line("你", text);
        if (!atDesk) player.CrossFadeInFixedTime("Idle", 0.3f);
    }

    IEnumerator Line(string who, string text)
    {
        ShowBar(who, text);
        AddOption("▶ 繼續", () => WaitingContinue = false);
        WaitingContinue = true;
        while (WaitingContinue) yield return null;
    }

    // 選項打亂順序再顯示：不會永遠「第一個就是對的」
    IEnumerator Choose(Choice[] choices, Action<Choice> result)
    {
        var order = new List<Choice>(choices);
        Shuffle(order, rng);
        LastChoices.Clear();
        foreach (Choice c in order) LastChoices.Add(c.text);
        ShowBar("你", "你怎麼回答？");
        picked = -1;
        for (int i = 0; i < order.Count; i++) { int k = i; AddOption(order[i].text, () => picked = k); }
        WaitingChoice = true;
        while (picked < 0) yield return null;
        WaitingChoice = false;
        result(order[picked]);
    }

    public static void Shuffle<T>(IList<T> list, System.Random r)
    {
        for (int i = list.Count - 1; i > 0; i--) { int j = r.Next(i + 1); (list[i], list[j]) = (list[j], list[i]); }
    }

    // 給自動試玩用
    public void Continue() => WaitingContinue = false;
    public void Pick(int i) => picked = i;
    public int ChoiceCount => LastChoices.Count;

    // 切鏡頭：只開這一台；臉部特寫時打開景深並對焦到臉
    void Cut(CinemachineCamera cam, Transform focus)
    {
        foreach (var c in new[] { gameCam, deskCam, deskFace, bossDeskFace, twoShot, playerFace, bossFace, lewisFace, suzieFace, marthaFace })
            if (c && c != cam) c.gameObject.SetActive(false);
        cam.gameObject.SetActive(true);
        activeCam = cam;
        dofTarget = focus;
        if (dof) dof.active = focus != null;
    }

    Billboard[] tags;

    void LateUpdate()
    {
        // 浮空名牌只在對話時出現：看地圖、玩小遊戲時會擋住畫面
        tags ??= FindObjectsByType<Billboard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool show = Now == Phase.Talk;
        foreach (Billboard t in tags) if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
        if (dof && dof.active && dofTarget) dof.focusDistance.value = Vector3.Distance(activeCam.transform.position, dofTarget.position);
    }

    // ───── UI（執行時才畫）─────
    static readonly Color Gold = new Color32(0xf2, 0xb3, 0x3d, 255), Ink = new Color32(0x15, 0x17, 0x1c, 255);

    const float BarY = 12, BarH = 262;

    void BuildUi()
    {
        ui = new GameObject("Campaign UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        ui.GetComponent<Canvas>().sortingOrder = 10;
        var scaler = ui.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Font f = DayManager.UiFont;

        hud = Ui.Label(ui.transform, "", 30, Gold, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(1000, 50), f, TextAnchor.UpperCenter);
        hud.gameObject.SetActive(false);

        // 字幕列：貼著畫面最下面、壓扁一點，臉留在畫面上半部（臉部鏡頭也配合把臉放高）
        bar = Ui.Rect("Subtitle", ui.transform, new Color(0.07f, 0.07f, 0.09f, 0.85f), new Vector2(0.5f, 0), new Vector2(0, BarY), new Vector2(1600, BarH)).gameObject;
        whoText = Ui.Label(bar.transform, "", 28, Gold, new Vector2(0, 1), new Vector2(40, -12), new Vector2(600, 40), f, TextAnchor.UpperLeft);
        lineText = Ui.Label(bar.transform, "", 30, Color.white, new Vector2(0, 1), new Vector2(40, -52), new Vector2(1520, 76), f, TextAnchor.UpperLeft);
        optionBox = Ui.Rect("Options", bar.transform, new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(40, 12), new Vector2(1520, 124));
        bar.SetActive(false);

        fade = Ui.Rect("Fade", ui.transform, new Color(0, 0, 0, 0), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000)).gameObject;
        fade.GetComponent<Image>().raycastTarget = false;
    }

    void ShowBar(string who, string text)
    {
        bar.SetActive(true);
        whoText.text = who;
        lineText.text = text;
        foreach (Transform c in optionBox) Destroy(c.gameObject);
        options = 0;   // Destroy 要到這一格結束才生效，所以自己數
    }

    void AddOption(string text, Action onClick)
    {
        int i = options++;
        RectTransform r = Ui.Rect("Option", optionBox, new Color(1, 1, 1, 0.1f), new Vector2(0, 1), new Vector2(0, -i * 42), new Vector2(1520, 38));
        Ui.Label(r, (text.StartsWith("▶") ? "" : $"{i + 1}. ") + text, 26, Color.white, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(1480, 38), DayManager.UiFont, TextAnchor.MiddleLeft);
        Button b = r.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors; cb.highlightedColor = new Color(1f, 0.8f, 0.4f); b.colors = cb;
        b.onClick.AddListener(() => onClick());
    }

    // 大卡片：標題＋一段文字＋一或兩顆按鈕（第二顆按鈕給「跳過」用）
    IEnumerator Card(string title, string body, string button, Action after = null, string button2 = null, Action click1 = null, Action click2 = null)
    {
        Phase was = Now;
        if (Now != Phase.Result && Now != Phase.Ending) Now = Phase.Card;
        bool go = false;
        GameObject card = Ui.Rect("Card", ui.transform, new Color(0.07f, 0.07f, 0.09f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 660)).gameObject;
        Ui.Label(card.transform, title, 44, Gold, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1000, 60), DayManager.UiFont, TextAnchor.UpperCenter);
        Ui.Label(card.transform, body, 28, Color.white, new Vector2(0.5f, 1), new Vector2(0, -125), new Vector2(980, 420), DayManager.UiFont, TextAnchor.UpperLeft);
        float x = button2 == null ? 0 : -200;
        Ui.Button(card.transform, button, Gold, Ink, new Vector2(x, -260), new Vector2(360, 80), DayManager.UiFont, () => { click1?.Invoke(); go = true; });
        if (button2 != null) Ui.Button(card.transform, button2, new Color(1, 1, 1, 0.85f), Ink, new Vector2(200, -260), new Vector2(360, 80), DayManager.UiFont, () => { click2?.Invoke(); go = true; });
        WaitingContinue = true;
        while (!go && WaitingContinue) yield return null;
        WaitingContinue = false;
        Destroy(card);
        after?.Invoke();
    }

    IEnumerator Fade(float to)
    {
        Image img = fade.GetComponent<Image>();
        float from = img.color.a;
        for (float t = 0; t < 1; t += Time.deltaTime / 0.5f)
        {
            img.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t));
            yield return null;
        }
        img.color = new Color(0, 0, 0, to);
    }
}
