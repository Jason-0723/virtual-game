using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 《工程師的一天》主控：遊戲時鐘、四個數值、依時間觸發 events.json 的事件、結局
public class DayManager : MonoBehaviour
{
    [Serializable] public class Option { public string text, result, setFlag; public int progress, stress, trust, life; }
    [Serializable] public class GameEvent { public int at; public string title, place, text, onlyIfFlag; public Option[] options; }
    [Serializable] class EventList { public string intro; public GameEvent[] events; }

    public static DayManager Instance;

    public EventDialog dialog;
    public Text clockText;                  // 出勤卡上的大時鐘
    public Image[] statBars;                // 進度、壓力、主管信任、生活（Filled 圖片）
    public Text objectiveText;
    public GameObject objectiveNote;        // 黃色便條紙，沒任務時藏起來
    public float minutesPerSecond = 0.9f;   // 09:00–18:00 約 10 分鐘玩完
    public int startMinute = 9 * 60, endMinute = 18 * 60;

    int progress = 0, stress = 20, trust = 50, life = 50;
    float clock;
    int next;
    GameEvent pending;                      // 等玩家走到指定地點的事件
    PlaceZone pendingZone;
    bool ended;
    readonly HashSet<string> flags = new HashSet<string>();
    EventList data;

    // 給自動試玩測試用
    public bool Ended => ended;
    public string PendingPlace => pending?.place;

    void Awake()
    {
        Instance = this;
        clock = startMinute;
        data = JsonUtility.FromJson<EventList>(Resources.Load<TextAsset>("events").text);

        // 標題用明體（襯線），其他沿用預設黑體；字型取自系統，不用另外匯入
        Font serif = Font.CreateDynamicFontFromOSFont(new[] { "PMingLiU", "MingLiU", "Noto Serif TC" }, 40);
        foreach (Text t in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name.StartsWith("Title")) t.font = serif;
    }

    void Start()
    {
        objectiveNote.SetActive(false);
        RefreshHud();
        dialog.Show("工程師的一天", data.intro, new[] { "打卡上班" }, _ => { });
    }

    void Update()
    {
        if (ended || dialog.IsOpen) return;  // 看對話框時時間暫停
        clock += minutesPerSecond * Time.deltaTime;

        if (stress >= 100) { End(); return; }
        if (pending == null && next < data.events.Length && data.events[next].at <= clock) Trigger(data.events[next++]);
        if (clock >= endMinute) { End(); return; }   // 18:00 到了就結算，沒處理的事件算沒做
        RefreshHud();
    }

    void Trigger(GameEvent e)
    {
        if (!string.IsNullOrEmpty(e.onlyIfFlag) && !flags.Contains(e.onlyIfFlag)) return;
        if (string.IsNullOrEmpty(e.place)) { Open(e); return; }
        pending = e;
        pendingZone = Array.Find(FindObjectsByType<PlaceZone>(FindObjectsSortMode.None), z => z.placeId == e.place);
        if (pendingZone && pendingZone.marker) pendingZone.marker.SetActive(true);
        objectiveText.text = e.title + "\n→ 請到：" + (pendingZone ? pendingZone.displayName : e.place);
        objectiveNote.SetActive(true);
    }

    public void EnterPlace(string placeId)
    {
        if (pending == null || pending.place != placeId || dialog.IsOpen) return;
        GameEvent e = pending;
        pending = null;
        if (pendingZone && pendingZone.marker) pendingZone.marker.SetActive(false);
        objectiveNote.SetActive(false);
        Open(e);
    }

    void Open(GameEvent e)
    {
        string[] labels = Array.ConvertAll(e.options, o => o.text);
        dialog.Show(Clock(clock) + "　" + e.title, e.text, labels, i => Apply(e.options[i]));
    }

    void Apply(Option o)
    {
        progress = Mathf.Clamp(progress + o.progress, 0, 100);
        stress = Mathf.Clamp(stress + o.stress, 0, 100);
        trust = Mathf.Clamp(trust + o.trust, 0, 100);
        life = Mathf.Clamp(life + o.life, 0, 100);
        if (!string.IsNullOrEmpty(o.setFlag)) flags.Add(o.setFlag);
        RefreshHud();
        if (!string.IsNullOrEmpty(o.result)) dialog.Show("後來⋯⋯", o.result + "\n\n" + Delta(o), new[] { "繼續" }, _ => { });
    }

    // 讓玩家看懂這個選擇改變了什麼
    static string Delta(Option o)
    {
        var parts = new List<string>();
        void Add(string n, int v) { if (v != 0) parts.Add(n + (v > 0 ? " +" : " ") + v); }
        Add("進度", o.progress); Add("壓力", o.stress); Add("主管信任", o.trust); Add("生活", o.life);
        return string.Join("　", parts);
    }

    void End()
    {
        ended = true;
        objectiveNote.SetActive(false);
        if (pendingZone && pendingZone.marker) pendingZone.marker.SetActive(false);
        string title, body;
        if (stress >= 100)
        {
            title = "結局：爆肝";
            body = "壓力破表。你盯著螢幕，什麼都看不進去。\n\n軟體業的過勞不是傳說，健康檢查上的紅字也不是。";
        }
        else if (flags.Contains("overtime"))
        {
            title = "結局：加班到 22:00";
            body = "問題修好了，但捷運上已經沒什麼人。明天 09:30 還有站立會議。\n\n很多工程師的下班時間寫在合約上，不在現實裡。";
        }
        else if (progress >= 60 && trust >= 50)
        {
            title = "結局：準時下班";
            body = "進度跟上了，主管也信任你。18:00，你闔上筆電。\n\n這樣的日子存在，但需要誠實、溝通，和一點運氣。";
        }
        else
        {
            title = "結局：準時下班，但⋯⋯";
            body = "你準時離開，但進度落後，手機裡的訊息一直跳。明天要先面對主管。\n\n下班不等於工作結束，這是很多人的日常。";
        }
        body += $"\n\n進度 {progress}　壓力 {stress}　主管信任 {trust}　生活 {life}"
              + "\n\n你不是一個人，辛苦了。\n累了，請找人說說話：衛福部安心專線 1925";
        dialog.Show(title, body, new[] { "再過一天" }, _ => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }

    void RefreshHud()
    {
        clockText.text = Clock(clock);
        int[] v = { progress, stress, trust, life };
        for (int i = 0; i < statBars.Length; i++) statBars[i].fillAmount = v[i] / 100f;
    }

    static string Clock(float minute) => $"{(int)minute / 60:00}:{(int)minute % 60:00}";
}
