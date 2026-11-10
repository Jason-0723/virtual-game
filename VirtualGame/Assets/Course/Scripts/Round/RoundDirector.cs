using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 試玩關卡的導演：一輪 = 坐在座位用電腦工作（螢幕上抓蟲）→ 下班前到主管旁邊談話（三回合選擇）→ 結果
// 角色一直在畫面上：工作時鏡頭在肩膀後面，看得到手在打字；談話時鏡頭在兩人之間切換臉部特寫
public class RoundDirector : MonoBehaviour
{
    public Animator player, boss;
    public Transform talkSpotPlayer, talkSpotBoss;
    public CinemachineCamera deskCam, twoShot, playerFace, bossFace;
    public MonitorBugHunt hunt;
    public int bugsToCatch = 12;
    public float huntSeconds = 25f;

    public enum Phase { Intro, Work, Talk, Result }
    public Phase Now { get; private set; }
    public bool WaitingContinue { get; private set; }
    public bool WaitingChoice { get; private set; }
    public int Trust => trust;

    int trust = 50, caught, need;
    bool helper, overtime;
    int picked = -1;
    GameObject ui, bar, fade;
    Text whoText, lineText;
    RectTransform optionBox;
    DepthOfField dof;
    Transform dofTarget;
    CinemachineCamera activeCam;

    void Start()
    {
        Volume v = FindFirstObjectByType<Volume>();
        if (v) v.profile.TryGet(out dof);
        BuildUi();
        Cut(deskCam, null);
        player.CrossFadeInFixedTime("Typing", 0f);
        boss.CrossFadeInFixedTime("Idle", 0f);
        StartCoroutine(Run());
    }

    // ───── 整輪流程 ─────
    IEnumerator Run()
    {
        Now = Phase.Intro;
        yield return Card("週一 17:00・上線前一小時",
            "主管丟下一句話就走了：\n「下班前，把 App 裡的蟲抓乾淨，然後來找我。」\n\n用滑鼠點電腦螢幕上爬來爬去的蟲。",
            "開始工作");

        Now = Phase.Work;
        bool done = false;
        hunt.Begin(bugsToCatch, huntSeconds, 180f, (c, n) => { caught = c; need = n; done = true; });
        while (!done) yield return null;
        yield return new WaitForSeconds(1.2f);

        // 轉場：畫面變黑 → 角色起身走到主管旁邊 → 亮起來
        yield return Fade(1);
        player.transform.SetPositionAndRotation(talkSpotPlayer.position, talkSpotPlayer.rotation);
        player.CrossFadeInFixedTime("Idle", 0f);
        Cut(twoShot, null);
        yield return new WaitForSeconds(0.3f);
        yield return Fade(0);

        Now = Phase.Talk;
        yield return Talk();

        Now = Phase.Result;
        ShowResult();
    }

    IEnumerator Talk()
    {
        float perf = need > 0 ? (float)caught / need : 0;
        // 開場：主管看你今天的表現
        if (perf >= 0.9f) yield return BossSays("今天的蟲抓得很乾淨，做得好。", "Nod");
        else if (perf >= 0.6f) yield return BossSays($"還有 {need - caught} 隻蟲沒抓到。明天客人會遇到的。", "Shake");
        else yield return BossSays($"{need} 隻蟲只抓到 {caught} 隻？這樣怎麼上線！", "Angry");

        // 第一回合
        yield return Choose("你怎麼回答？",
            "我照著客人的回報，一個一個查。",
            "時間太趕了，不是我的問題。",
            "有些地方我不太懂，可以請前輩帶我嗎？");
        switch (picked)
        {
            case 0: yield return PlayerSays("我照著客人的回報，一個一個查。"); yield return BossSays("很好，就是要這樣，一步一步來。", "Nod"); trust += 5; break;
            case 1: yield return PlayerSays("時間太趕了，不是我的問題。"); yield return BossSays("每個人的時間都很趕。找藉口，事情不會變好。", "Angry"); trust -= 10; break;
            default: yield return PlayerSays("有些地方我不太懂，可以請前輩帶我嗎？"); yield return BossSays("會問問題很好。明天讓阿傑帶你。", "Nod"); trust += 8; helper = true; break;
        }

        // 第二回合
        yield return BossSays("客戶剛剛說，明天要多一個『優惠券』功能。做得完嗎？", "Talk");
        yield return Choose("你怎麼回答？",
            "可以，但要多一天。我先把今天的錯修完。",
            "沒問題！我今晚加班做完。",
            "做不完。叫業務自己去跟客戶說。");
        switch (picked)
        {
            case 0: yield return PlayerSays("可以，但要多一天。我先把今天的錯修完。"); yield return BossSays("好，說清楚要多久很重要。我去跟客戶談時程。", "Nod"); trust += 5; break;
            case 1: yield return PlayerSays("沒問題！我今晚加班做完。"); yield return BossSays("⋯⋯態度很好。但別把自己累垮。", "Talk"); trust += 3; overtime = true; break;
            default: yield return PlayerSays("做不完。叫業務自己去跟客戶說。"); yield return BossSays("這不是一個團隊該說的話。", "Shake"); trust -= 8; break;
        }

        // 第三回合：誠實
        yield return BossSays("最後問你。今天早上站會，你說登入功能『快好了』，是真的嗎？", "Talk");
        yield return Choose("你怎麼回答？",
            "其實還沒⋯⋯我怕大家失望。對不起。",
            "是真的，一切都在掌握中！",
            "（沉默，低頭看鞋子）");
        switch (picked)
        {
            case 0: yield return PlayerSays("其實還沒⋯⋯我怕大家失望。對不起。"); yield return BossSays("謝謝你說實話。下次早點講，我可以幫你找人。", "Nod"); trust += 6; break;
            case 1:
                yield return PlayerSays("是真的，一切都在掌握中！");
                if (perf < 0.9f) { yield return BossSays("那這些沒抓到的蟲是怎麼回事？", "Angry"); trust -= 12; }
                else { yield return BossSays("好，那我就放心了。", "Talk"); trust += 2; }
                break;
            default: yield return PlayerSays("⋯⋯"); yield return BossSays("⋯⋯我希望你下次能直接告訴我。", "Shake"); trust -= 4; break;
        }
    }

    // ───── 對話小工具 ─────
    IEnumerator BossSays(string text, string anim)
    {
        Cut(bossFace, boss.GetBoneTransform(HumanBodyBones.Head));
        boss.CrossFadeInFixedTime(anim, 0.25f);
        player.CrossFadeInFixedTime("Idle", 0.25f);
        yield return Line("主管", text);
        boss.CrossFadeInFixedTime("Idle", 0.3f);
    }

    IEnumerator PlayerSays(string text)
    {
        Cut(playerFace, player.GetBoneTransform(HumanBodyBones.Head));
        player.CrossFadeInFixedTime("Talk", 0.25f);
        yield return Line("你", text);
        player.CrossFadeInFixedTime("Idle", 0.3f);
    }

    IEnumerator Line(string who, string text)
    {
        ShowBar(who, text);
        AddOption("▶ 繼續", () => WaitingContinue = false);
        WaitingContinue = true;
        while (WaitingContinue) yield return null;
    }

    IEnumerator Choose(string prompt, params string[] options)
    {
        Cut(twoShot, null);
        ShowBar("你", prompt);
        picked = -1;
        for (int i = 0; i < options.Length; i++)
        {
            int k = i;
            AddOption(options[i], () => picked = k);
        }
        WaitingChoice = true;
        while (picked < 0) yield return null;
        WaitingChoice = false;
    }

    // 給自動試玩用
    public void Continue() => WaitingContinue = false;
    public void Pick(int i) => picked = i;

    // 切鏡頭：只開這一台（同優先度時最後啟用的接手）；臉部特寫時打開景深、對焦到臉
    void Cut(CinemachineCamera cam, Transform focus)
    {
        foreach (var c in new[] { deskCam, twoShot, playerFace, bossFace }) if (c != cam) c.gameObject.SetActive(false);
        cam.gameObject.SetActive(true);
        activeCam = cam;
        dofTarget = focus;
        if (dof) dof.active = focus != null;
    }

    void LateUpdate()
    {
        if (dof && dof.active && dofTarget) dof.focusDistance.value = Vector3.Distance(activeCam.transform.position, dofTarget.position);
    }

    // ───── 結果 ─────
    void ShowResult()
    {
        bar.SetActive(false);
        Cut(twoShot, null);
        int stars = trust >= 65 ? 3 : trust >= 45 ? 2 : 1;
        string mood = stars == 3 ? "主管很信任你" : stars == 2 ? "主管還在觀察你" : "主管對你很失望";
        string tomorrow = "";
        if (helper) tomorrow += "\n・明天前輩阿傑會來帶你（關卡變簡單）";
        if (overtime) tomorrow += "\n・你今晚要加班（明天一開始壓力就很高）";
        if (need > 0 && caught < need) tomorrow += $"\n・沒抓到的 {need - caught} 隻蟲，明天會變成顧客客訴";
        if (tomorrow == "") tomorrow = "\n・明天照常上班";
        StartCoroutine(Card($"週一結束　{new string('★', stars)}{new string('☆', 3 - stars)}",
            $"抓到 {caught} / {need} 隻蟲　　主管信任 {trust}\n\n{mood}。\n\n明天會發生的事：{tomorrow}\n\n工程師不只寫程式，也要跟人溝通：誠實回報進度、需要時開口求助，往往比硬撐更重要。",
            "再玩一次", () => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
    }

    // ───── UI（執行時才畫）─────
    void BuildUi()
    {
        ui = new GameObject("Round UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        ui.GetComponent<Canvas>().sortingOrder = 10;
        var scaler = ui.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Font f = DayManager.UiFont;

        // 字幕列：畫面下方，臉留在上面
        bar = Ui.Rect("Subtitle", ui.transform, new Color(0.07f, 0.07f, 0.09f, 0.88f), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1500, 340)).gameObject;
        whoText = Ui.Label(bar.transform, "", 30, new Color32(0xf2, 0xb3, 0x3d, 255), new Vector2(0, 1), new Vector2(40, -20), new Vector2(600, 44), f, TextAnchor.UpperLeft);
        lineText = Ui.Label(bar.transform, "", 32, Color.white, new Vector2(0, 1), new Vector2(40, -70), new Vector2(1420, 90), f, TextAnchor.UpperLeft);
        optionBox = Ui.Rect("Options", bar.transform, new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(40, 20), new Vector2(1420, 130));
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
        options = 0;   // Destroy 要到這一格結束才生效，所以自己數，不用 childCount
    }
    int options;

    void AddOption(string text, System.Action onClick)
    {
        int i = options++;
        RectTransform r = Ui.Rect("Option", optionBox, new Color(1, 1, 1, 0.1f), new Vector2(0, 1), new Vector2(0, -i * 44), new Vector2(1420, 40));
        Text t = Ui.Label(r, (text.StartsWith("▶") ? "" : $"{i + 1}. ") + text, 26, Color.white, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(1380, 40), DayManager.UiFont, TextAnchor.MiddleLeft);
        Button b = r.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors; cb.highlightedColor = new Color(1f, 0.8f, 0.4f); b.colors = cb;
        b.onClick.AddListener(() => onClick());
    }

    // 大卡片（開場、結果）：一段文字＋一顆按鈕
    IEnumerator Card(string title, string body, string button, System.Action after = null)
    {
        bool go = false;
        GameObject card = Ui.Rect("Card", ui.transform, new Color(0.07f, 0.07f, 0.09f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 640)).gameObject;
        Ui.Label(card.transform, title, 44, new Color32(0xf2, 0xb3, 0x3d, 255), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1000, 60), DayManager.UiFont, TextAnchor.UpperCenter);
        Ui.Label(card.transform, body, 30, Color.white, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(980, 400), DayManager.UiFont, TextAnchor.UpperLeft);
        Ui.Button(card.transform, button, new Color32(0xf2, 0xb3, 0x3d, 255), new Color32(0x15, 0x17, 0x1c, 255), new Vector2(0, -250), new Vector2(360, 80), DayManager.UiFont, () => go = true);
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
