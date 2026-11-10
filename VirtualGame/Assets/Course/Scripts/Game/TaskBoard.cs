using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 待辦倒數（像胡鬧廚房的多工）：左上角同時掛 2～3 件事，每件都有倒數
// 兩種待辦：走到某個地點（影印機、會議室、機房）、或回座位打字打夠字數
// 時間內完成 → 進度、信任加；來不及 → 壓力加、信任扣；urgent（事件觸發的緊急任務）加扣更多
public class TaskBoard : MonoBehaviour
{
    public RectTransform rowTemplate;   // 隱藏的一列：Text「Label」＋ Image「Bar」（Filled）

    class Live
    {
        public DayManager.TaskDef def;
        public float left;
        public int typed;
        public RectTransform row;
        public Text label;
        public Image bar;
    }

    readonly List<Live> live = new List<Live>();
    DayManager.DayData today;
    float nextSpawn;

    public int Count => live.Count;
    public IEnumerable<string> LiveIds { get { foreach (var l in live) yield return l.def.id; } }

    void Start()
    {
        rowTemplate.gameObject.SetActive(false);
        DayManager.Instance.typing.onTyped += OnTyped;
    }

    public void BeginDay(DayManager.DayData d)
    {
        Clear();
        today = d;
        nextSpawn = Time.time + Random.Range(d.taskInterval.x, d.taskInterval.y);
    }

    public void Clear()
    {
        foreach (Live l in live) Destroy(l.row.gameObject);
        live.Clear();
    }

    public void Add(string id)
    {
        DayManager.TaskDef def = System.Array.Find(DayManager.Instance.Data.tasks, t => t.id == id);
        if (def == null) { Debug.LogWarning("[TaskBoard] events.json 沒有這個待辦：" + id); return; }
        RectTransform row = Instantiate(rowTemplate, rowTemplate.parent);
        row.gameObject.SetActive(true);
        var l = new Live { def = def, left = def.seconds, row = row, label = row.Find("Label").GetComponent<Text>(), bar = row.Find("Bar").GetComponent<Image>() };
        if (def.urgent) l.bar.color = new Color32(0xc2, 0x3b, 0x22, 255);
        live.Add(l);
        if (def.urgent) DayManager.Instance.Shake(0.4f);
        Layout();
    }

    void Update()
    {
        DayManager day = DayManager.Instance;
        if (day == null || today == null || day.Ended || day.WorldPaused)
        {
            nextSpawn += Time.deltaTime;   // 暫停時不算時間
            return;
        }

        // 隨機待辦：還有空位、時間到了，就從 random 的待辦裡抽一件（同一件不重複掛）
        if (Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + Random.Range(today.taskInterval.x, today.taskInterval.y);
            if (live.Count < today.maxTasks)
            {
                var pool = new List<DayManager.TaskDef>();
                foreach (var t in day.Data.tasks)
                    if (t.random && !live.Exists(l => l.def.id == t.id)) pool.Add(t);
                if (pool.Count > 0) Add(pool[Random.Range(0, pool.Count)].id);
            }
        }

        for (int i = live.Count - 1; i >= 0; i--)
        {
            Live l = live[i];
            l.left -= Time.deltaTime;
            l.bar.fillAmount = Mathf.Clamp01(l.left / l.def.seconds);
            string how = l.def.chars > 0 ? $"回座位打字 {Mathf.Min(l.typed, l.def.chars)}/{l.def.chars}" : "→ " + PlaceName(l.def.place);
            l.label.text = $"{l.def.title}　{how}　{Mathf.CeilToInt(Mathf.Max(0, l.left))} 秒";
            if (l.left <= 0) Finish(l, false);
        }
    }

    // 走到地點：完成第一件要去那裡的待辦
    public void EnterPlace(string placeId)
    {
        Live l = live.Find(x => x.def.place == placeId);
        if (l != null) Finish(l, true);
    }

    // 打字：算給第一件打字型的待辦
    void OnTyped(int chars)
    {
        Live l = live.Find(x => x.def.chars > 0);
        if (l == null) return;
        l.typed += chars;
        if (l.typed >= l.def.chars) Finish(l, true);
    }

    void Finish(Live l, bool done)
    {
        bool u = l.def.urgent;
        if (done) DayManager.Instance.AddStats(u ? 8 : 4, u ? -5 : 0, u ? 5 : 2, 0);
        else
        {
            DayManager.Instance.AddStats(0, u ? 12 : 8, u ? -12 : -6, 0);
            DayManager.Instance.Shake(0.3f);
        }
        live.Remove(l);
        Destroy(l.row.gameObject);
        Layout();
    }

    void Layout()
    {
        for (int i = 0; i < live.Count; i++)
            live[i].row.anchoredPosition = rowTemplate.anchoredPosition - new Vector2(0, i * (rowTemplate.sizeDelta.y + 8));
    }

    static string PlaceName(string id)
    {
        PlaceZone z = System.Array.Find(FindObjectsByType<PlaceZone>(FindObjectsSortMode.None), p => p.placeId == id);
        return z ? z.displayName : id;
    }
}
