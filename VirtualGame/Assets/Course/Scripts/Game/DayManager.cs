using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 《工程師的一天》主控：三天（週一／週三／週五）的遊戲時鐘、四個數值、依時間觸發 events.json 的事件、
// 跨天旗標（前一天的選擇會在後一天爆炸）、Game Over（重來這一天／看廣告復活）、最後的結局
public class DayManager : MonoBehaviour
{
    [Serializable]
    public class Option
    {
        public string id, log, text, result, setFlag;
        public string startTask;   // 選了就加一個倒數待辦（TaskBoard）
        public string gameOver;    // 有寫理由＝資工天條死罪，選了直接 Game Over
        public int progress, stress, trust, life, debt;
    }
    [Serializable]
    public class GameEvent
    {
        public int at, minAt, maxAt;                   // main 用 at；pool 用 minAt～maxAt（0 = 不限）
        public string title, place, text, onlyIfFlag;
        public int[] days;                             // pool：哪幾天會抽到（1～3，空的＝每天）
        public float weight, debtWeight, stressWeight, distrustWeight;   // pool 抽籤權重
        public bool bug;                               // 緊急事件：鏡頭會震一下
        public string[] causedBy;                      // 後果事件：是哪些選項 id 造成的（結局時間軸畫紅線）
        public Option[] options;
    }
    [Serializable]
    public class DayData
    {
        public string name, shortName, intro;
        public float bossSpeed, gloom;
        public int maxTasks;
        public Vector2 taskInterval;
        public int[] slots;
        public GameEvent[] main;
    }
    [Serializable] public class SlackItem { public string app, text; public int stress, life, trust, progress; }
    [Serializable] public class TaskDef { public string id, title, place; public int chars; public float seconds; public bool random, urgent; }
    [Serializable] public class EventList { public DayData[] days; public GameEvent[] pool; public TaskDef[] tasks; public SlackItem[] slack; }

    // 半動態排程：main 固定時間一定發生，slots 的時間點從 pool 依數值加權抽一件
    struct Slot { public int at; public GameEvent fixedEvent; }
    readonly List<Slot> schedule = new List<Slot>();
    readonly HashSet<GameEvent> used = new HashSet<GameEvent>();

    // 每個選擇都記下來：編號、時間、一句說明、是被哪幾個編號造成的
    public class Record { public int no; public string time, text, optionId; public List<int> causes; }
    readonly List<Record> records = new List<Record>();
    public IReadOnlyList<Record> Records => records;

    public static DayManager Instance;

    public EventDialog dialog;
    public Text clockText;                  // 出勤卡上的大時鐘
    public Text dayText;                    // 出勤卡上的「週一」
    public Image[] statBars;                // 進度、壓力、主管信任、生活（Filled 圖片）
    public Text objectiveText;
    public GameObject objectiveNote;        // 黃色便條紙，沒任務時藏起來
    public TypingTerminal typing;
    public TimelineView timeline;
    public TaskBoard tasks;
    public GameOverView gameOverView;
    public ScamRoute scam;
    public CinemachineImpulseSource impulse;
    public float minutesPerSecond = 1.5f;   // 09:00–18:00 一天 6 分鐘，三天約 18 分鐘
    public int startMinute = 9 * 60, endMinute = 18 * 60;

    int progress = 0, stress = 20, trust = 50, life = 50;
    int techDebt = 0;                       // 技術債：玩家看不到，貪快會累積，Bug 變多；結局才公開
    float clock;
    int next;
    int day;                                // 0＝週一、1＝週三、2＝週五
    GameEvent pending;                      // 等玩家走到指定地點的事件
    PlaceZone pendingZone;
    bool ended;                             // 下班結算、Game Over、詐騙支線時為 true：時鐘停止
    readonly HashSet<string> flags = new HashSet<string>();
    EventList data;

    // 每天開始時存一份，「重來這一天」就回到這裡
    struct Snapshot { public int progress, stress, trust, life, debt, records; public HashSet<string> flags; public HashSet<GameEvent> used; }
    Snapshot dayStart;

    public EventList Data => data;
    public DayData Today => data.days[day];
    public int DayIndex => day;
    public bool Ended => ended;
    // 世界停住：對話中、Game Over 畫面、下班結算；詐騙支線時世界照常動（幹部會巡邏）
    public bool WorldPaused => dialog.IsOpen || (gameOverView && gameOverView.IsOpen) || (ended && !(scam && scam.Active));
    public string PendingPlace => pending?.place;
    public int TechDebt => techDebt;
    public int Progress => progress;
    public int Stress => stress;
    public int Trust => trust;
    public int Life => life;
    public bool HasFlag(string f) => flags.Contains(f);
    public float Clock01 => Mathf.InverseLerp(startMinute, endMinute, clock);   // 0＝09:00、1＝18:00，給日光用
    public string Now => Today.shortName + " " + Clock(clock);

    void Awake()
    {
        Instance = this;
        data = JsonUtility.FromJson<EventList>(Resources.Load<TextAsset>("events").text);

        // 標題用明體（襯線），其他用正黑體；字型取自系統，不用另外匯入
        Font serif = Font.CreateDynamicFontFromOSFont(new[] { "PMingLiU", "MingLiU", "Noto Serif TC" }, 40);
        foreach (Text t in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            t.font = t.name.StartsWith("Title") ? serif : UiFont;
    }

    // 內文用微軟正黑體（系統內建，比預設字型清楚）；沒有就退回思源黑體
    static Font uiFont;
    public static Font UiFont => uiFont ? uiFont : uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft JhengHei UI", "Microsoft JhengHei", "Noto Sans TC" }, 28);

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        objectiveNote.SetActive(false);
        StartDay(0);
    }

    // ───────────── 一天的開始與結束 ─────────────
    void StartDay(int d)
    {
        day = d;
        clock = startMinute;
        next = 0;
        ended = false;
        ClearPending();
        schedule.Clear();
        foreach (GameEvent e in Today.main) schedule.Add(new Slot { at = e.at, fixedEvent = e });
        foreach (int t in Today.slots) schedule.Add(new Slot { at = t });
        schedule.Sort((a, b) => a.at.CompareTo(b.at));
        dayStart = new Snapshot
        {
            progress = progress, stress = stress, trust = trust, life = life, debt = techDebt, records = records.Count,
            flags = new HashSet<string>(flags), used = new HashSet<GameEvent>(used),
        };
        tasks.BeginDay(Today);
        WaypointPatrol boss = FindFirstObjectByType<WaypointPatrol>();
        if (boss) boss.GetComponent<NavMeshAgent>().speed = 1.1f * Today.bossSpeed;   // 一天比一天巡得快
        if (dayText) dayText.text = Today.shortName;
        RefreshHud();
        dialog.Show(Today.name, Today.intro, new[] { "打卡上班" }, _ => { });
    }

    void EndOfDay()
    {
        ended = true;
        ClearPending();
        tasks.Clear();
        if (day < data.days.Length - 1)
        {
            // 下班睡一覺：壓力降一點、生活回一點
            stress = Mathf.Clamp(stress - 15, 0, 100);
            life = Mathf.Clamp(life + 5, 0, 100);
            RefreshHud();
            dialog.Show(Today.shortName + "・下班",
                $"18:00，{Today.shortName}結束了。\n\n進度 {progress}　壓力 {stress}　主管信任 {trust}　生活 {life}\n\n今天做的選擇，接下來幾天可能會找上門。",
                new[] { "睡一覺，到" + data.days[day + 1].shortName }, _ => StartDay(day + 1));
        }
        else timeline.Show(records, ShowEnding);   // 週五下班：先看三天的提交紀錄，再看結局
    }

    void Update()
    {
        // 對話時鏡頭在拍臉、打字時坐在位子上、Game Over 畫面時，角色不能走開
        SetWalking(!WorldPaused && !(typing && typing.IsOpen));
        if (ended || dialog.IsOpen) return;  // 看對話框時時間暫停
        clock += minutesPerSecond * Time.deltaTime;

        if (stress >= 100) { GameOver("過勞倒下", "壓力破表。你盯著螢幕，眼前一黑。\n\n醒來時看著醫院的天花板，手上還握著滑鼠。\n\n軟體業的過勞不是傳說。"); return; }
        if (trust <= 0) { GameOver("被開除", "主管信任歸零。人資請你到會議室，桌上放著一張離職同意書。"); return; }
        if (pending == null && next < schedule.Count && schedule[next].at <= clock)
        {
            Slot s = schedule[next++];
            GameEvent e = s.fixedEvent ?? Draw();
            if (e != null) Trigger(e);
        }
        if (clock >= endMinute) { EndOfDay(); return; }   // 18:00 到了就下班，沒處理的事件算沒做
        RefreshHud();
    }

    // 從 pool 抽一件：今天可以出現、時間範圍內、條件符合、還沒出現過；權重越高越容易被抽到
    GameEvent Draw()
    {
        var candidates = new List<GameEvent>();
        var weights = new List<float>();
        foreach (GameEvent e in data.pool)
        {
            if (used.Contains(e) || clock < e.minAt || (e.maxAt > 0 && clock > e.maxAt)) continue;
            if (e.days != null && e.days.Length > 0 && Array.IndexOf(e.days, day + 1) < 0) continue;
            if (!string.IsNullOrEmpty(e.onlyIfFlag) && !flags.Contains(e.onlyIfFlag)) continue;
            candidates.Add(e);
            weights.Add(Weight(e, techDebt, stress, trust));
        }
        int i = WeightedPick(weights, UnityEngine.Random.value);
        if (i < 0) return null;
        used.Add(candidates[i]);
        return candidates[i];
    }

    // 技術債越高，bug 事件越容易抽到；壓力高容易抽到喘口氣；主管信任低容易被約談
    public static float Weight(GameEvent e, int debt, int stress, int trust) =>
        Mathf.Max(0, e.weight + e.debtWeight * debt / 10f + e.stressWeight * stress / 10f + e.distrustWeight * (100 - trust) / 10f);

    // roll 是 0～1 的亂數；全部權重都是 0 就回 -1
    public static int WeightedPick(List<float> weights, float roll)
    {
        float total = 0;
        foreach (float w in weights) total += w;
        if (total <= 0) return -1;
        float r = roll * total;
        for (int i = 0; i < weights.Count; i++)
        {
            if (r < weights[i]) return i;
            r -= weights[i];
        }
        return weights.Count - 1;   // roll = 1 的邊界
    }

    void Trigger(GameEvent e)
    {
        if (!string.IsNullOrEmpty(e.onlyIfFlag) && !flags.Contains(e.onlyIfFlag)) return;
        if (e.bug) Shake(0.5f);
        if (string.IsNullOrEmpty(e.place)) { Open(e); return; }
        pending = e;
        pendingZone = Array.Find(FindObjectsByType<PlaceZone>(FindObjectsSortMode.None), z => z.placeId == e.place);
        if (pendingZone && pendingZone.marker) pendingZone.marker.SetActive(true);
        objectiveText.text = e.title + "\n→ 請到：" + (pendingZone ? pendingZone.displayName : e.place);
        objectiveNote.SetActive(true);
    }

    public void EnterPlace(string placeId)
    {
        tasks.EnterPlace(placeId);
        if (pending == null || pending.place != placeId || dialog.IsOpen) return;
        GameEvent e = pending;
        ClearPending();
        Open(e);
    }

    void ClearPending()
    {
        pending = null;
        if (pendingZone && pendingZone.marker) pendingZone.marker.SetActive(false);
        pendingZone = null;
        objectiveNote.SetActive(false);
    }

    void Open(GameEvent e)
    {
        // 這件事是之前哪些選擇造成的（結局時間軸會畫紅線）
        var causes = new List<int>();
        if (e.causedBy != null)
            foreach (Record r in records)
                if (Array.IndexOf(e.causedBy, r.optionId) >= 0) causes.Add(r.no);
        string[] labels = Array.ConvertAll(e.options, o => o.text);
        dialog.Show(Now + "　" + e.title, e.text, labels, i => Apply(e, e.options[i], causes));
    }

    void Apply(GameEvent e, Option o, List<int> causes)
    {
        records.Add(new Record
        {
            no = records.Count + 1, time = Now, optionId = o.id, causes = causes,
            // 後果事件要寫出發生了什麼，時間軸上才看得懂紅線
            text = causes.Count > 0 ? e.title + " → " + (o.log ?? o.text) : string.IsNullOrEmpty(o.log) ? e.title + "：" + o.text : o.log,
        });
        if (!string.IsNullOrEmpty(o.gameOver)) { GameOver("當場開除", o.gameOver); return; }
        progress = Mathf.Clamp(progress + o.progress, 0, 100);
        stress = Mathf.Clamp(stress + o.stress, 0, 100);
        trust = Mathf.Clamp(trust + o.trust, 0, 100);
        life = Mathf.Clamp(life + o.life, 0, 100);
        techDebt = Mathf.Clamp(techDebt + o.debt, 0, 100);   // 不顯示在 HUD，也不放進 Delta
        if (!string.IsNullOrEmpty(o.setFlag)) flags.Add(o.setFlag);
        if (!string.IsNullOrEmpty(o.startTask)) tasks.Add(o.startTask);
        RefreshHud();
        if (!string.IsNullOrEmpty(o.result)) dialog.Show("後來⋯⋯", o.result + "\n\n" + Delta(o.progress, o.stress, o.trust, o.life), new[] { "繼續" }, _ => { });
    }

    // 讓玩家看懂這個選擇改變了什麼
    public static string Delta(int p, int s, int t, int l)
    {
        var parts = new List<string>();
        void Add(string n, int v) { if (v != 0) parts.Add(n + (v > 0 ? " +" : " ") + v); }
        Add("進度", p); Add("壓力", s); Add("主管信任", t); Add("生活", l);
        return string.Join("　", parts);
    }

    // ───────────── Game Over 與復活 ─────────────
    public void GameOver(string title, string reason)
    {
        ended = true;
        ClearPending();
        tasks.Clear();
        typing.Close();
        Shake(1f);
        gameOverView.Show(title, reason, RetryDay, () => scam.Begin());
    }

    // 回到今天早上：數值、旗標、紀錄都還原
    public void RetryDay()
    {
        progress = dayStart.progress; stress = dayStart.stress; trust = dayStart.trust; life = dayStart.life; techDebt = dayStart.debt;
        flags.Clear(); flags.UnionWith(dayStart.flags);
        used.Clear(); used.UnionWith(dayStart.used);
        records.RemoveRange(dayStart.records, records.Count - dayStart.records);
        StartDay(day);
    }

    // ───────────── 結局（撐完三天）─────────────
    void ShowEnding()
    {
        string title, body;
        if (progress >= 80 && trust >= 70)
        {
            title = "結局：轉正加薪";
            body = "三天撐過來了，進度和信任都很漂亮。人資寄來轉正通知，還附了一點加薪。\n\n下週一 09:00，鬧鐘照常響起。";
        }
        else if (life >= 75 && stress <= 40 && flags.Contains("whistle") && !flags.Contains("hidden_bug"))
        {
            title = "結局：準時下班的人";
            body = "你沒有背鍋，也沒有藏任何錯誤。主管被調職了。\n\n週五 18:00，你準時走出大門，雨剛好停了。";
        }
        else if (flags.Contains("scapegoat"))
        {
            title = "結局：通過試用期，但⋯⋯";
            body = "你通過了試用期，報告上那個漏洞，掛的是你的名字。\n\n主管對你很好。你常常想起簽名那天，手有點抖。";
        }
        else
        {
            title = "結局：撐過第一週";
            body = "你撐過了第一週。不算好，也不算壞。\n\n手機裡還有一堆沒回的訊息，下週一還有站立會議。";
        }
        body += $"\n\n進度 {progress}　壓力 {stress}　主管信任 {trust}　生活 {life}"
              + $"\n技術債 {techDebt}（一直沒顯示的數字：貪快留下的程式，之後都要有人還）"
              + "\n\n你不是一個人，辛苦了。累了，請找人說說話：衛福部安心專線 1925";
        dialog.Show(title, body, new[] { "再來一次" }, _ => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }

    // ───────────── 給其他元件用 ─────────────
    // 摸魚、待辦、Slack、主管查哨：直接改數值，不跳對話框
    public void AddStats(int p, int s, int t, int l)
    {
        progress = Mathf.Clamp(progress + p, 0, 100);
        stress = Mathf.Clamp(stress + s, 0, 100);
        trust = Mathf.Clamp(trust + t, 0, 100);
        life = Mathf.Clamp(life + l, 0, 100);
        RefreshHud();
    }
    public void AddProgress(int n) => AddStats(n, 0, 0, 0);
    public void AddStress(int n) => AddStats(0, n, 0, 0);

    // 臨時插進來的事（例如摸魚被主管抓到）：跳一個只有「繼續」的對話框，也記進時間軸
    public void Interrupt(string title, string text, string log, int stressDelta, int trustDelta)
    {
        AddStats(0, stressDelta, trustDelta, 0);
        records.Add(new Record { no = records.Count + 1, time = Now, optionId = "interrupt", causes = new List<int>(), text = log });
        dialog.Show(Now + "　" + title, text + "\n\n" + Delta(0, stressDelta, trustDelta, 0), new[] { "繼續" }, _ => { });
    }

    // 鏡頭震一下（Cinemachine Impulse）：緊急事件、Game Over、警察攻堅
    public void Shake(float force)
    {
        if (impulse) impulse.GenerateImpulseWithForce(force);
    }

    // 詐騙支線接管畫面時：時鐘停、HUD 交給 ScamRoute
    public void EnterScamMode()
    {
        ended = true;
        ClearPending();
        tasks.Clear();
    }

    void SetWalking(bool on)
    {
        if (walker == null) walker = FindFirstObjectByType<SimpleWalker>();
        if (walker == null || walker.enabled == on) return;
        walker.enabled = on;
        if (!on) walker.GetComponent<Animator>().SetBool("isWalking", false);   // 停用時動畫不會自己停
    }
    SimpleWalker walker;

    void RefreshHud()
    {
        if (scam && scam.Active) return;   // 詐騙支線自己管 HUD
        clockText.text = Clock(clock);
        int[] v = { progress, stress, trust, life };
        for (int i = 0; i < statBars.Length; i++) statBars[i].fillAmount = v[i] / 100f;
    }

    static string Clock(float minute) => $"{(int)minute / 60:00}:{(int)minute % 60:00}";
}
