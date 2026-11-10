using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 排今天的工作：工作比時間多，只能挑幾件做。總時數不能超過今天的時間，挑出「最有價值」的組合
// 價值用 ★ 表示；按「就這樣決定」交出去，跟最好的排法比
public class PlanGame : MonitorGame
{
    public override string Title => "排今天的工作";

    static readonly (string name, int hours, int value)[] All =
    {
        ("修結帳算錯", 3, 5), ("回客服信", 1, 2), ("做優惠券", 4, 4), ("寫測試", 2, 3),
        ("改按鈕顏色", 1, 1), ("開週會", 2, 1), ("修登入", 3, 4), ("整理文件", 2, 2), ("升級套件", 3, 2),
    };

    List<(string name, int hours, int value)> tasks;
    readonly HashSet<int> chosen = new HashSet<int>();
    readonly List<Image> cards = new List<Image>();
    int budget, best;
    Text sum;

    protected override void Setup()
    {
        tasks = new List<(string, int, int)>(All);
        CampaignDirector.Shuffle(tasks, new System.Random());
        int count = Mathf.Min(tasks.Count, 6 + difficulty);
        tasks.RemoveRange(count, tasks.Count - count);
        budget = 8;
        chosen.Clear(); cards.Clear();
        best = Best();
        Label($"今天只有 {budget} 小時。挑幾件工作，讓 ★ 加起來越多越好", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        for (int i = 0; i < tasks.Count; i++)
        {
            int k = i;
            float x = (i % 3 - 1) * 290, y = 105 - (i / 3) * 95;
            RectTransform r = Box(Paper, new Vector2(x, y), new Vector2(270, 82));
            Label($"{tasks[i].name}\n{tasks[i].hours} 小時　{new string('★', tasks[i].value)}", 22, Ink, Vector2.zero, new Vector2(260, 78), r).raycastTarget = false;
            r.gameObject.AddComponent<Button>().onClick.AddListener(() => Toggle(k));
            cards.Add(r.GetComponent<Image>());
        }
        sum = Label("", 22, Ink, new Vector2(-150, -170), new Vector2(560, 34));
        Btn("就這樣決定", Orange, Color.white, new Vector2(300, -168), new Vector2(240, 56), Submit);
        Refresh();
    }

    int Hours() { int h = 0; foreach (int i in chosen) h += tasks[i].hours; return h; }
    int Value() { int v = 0; foreach (int i in chosen) v += tasks[i].value; return v; }

    void Toggle(int i)
    {
        if (!Running) return;
        if (chosen.Contains(i)) chosen.Remove(i);
        else if (Hours() + tasks[i].hours <= budget) chosen.Add(i);
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < cards.Count; i++) cards[i].color = chosen.Contains(i) ? new Color32(0xff, 0xd9, 0x8a, 255) : (Color)Paper;
        sum.text = $"已用 {Hours()} / {budget} 小時　★ {Value()}";
    }

    void Submit()
    {
        if (!Running) return;
        int v = Value();
        Finish(v >= best ? 3 : v >= best - 2 ? 2 : v >= best / 2 ? 1 : 0, $"你排了 ★ {v}（最好的排法是 ★ {best}）");
    }

    // 最好的排法：工作只有 6～9 件，全部組合試一遍就好
    int Best()
    {
        int n = tasks.Count, top = 0;
        for (int mask = 0; mask < 1 << n; mask++)
        {
            int h = 0, v = 0;
            for (int i = 0; i < n; i++) if ((mask & (1 << i)) != 0) { h += tasks[i].hours; v += tasks[i].value; }
            if (h <= budget && v > top) top = v;
        }
        return top;
    }

    protected override string Goal() => "挑工作，按「就這樣決定」交出";
    protected override void Tick() { }
    protected override void TimeUp() => Submit();

    // 自動試玩：直接選出最好的組合再交
    public override void AutoPlay()
    {
        int n = tasks.Count, bestMask = 0, top = -1;
        for (int mask = 0; mask < 1 << n; mask++)
        {
            int h = 0, v = 0;
            for (int i = 0; i < n; i++) if ((mask & (1 << i)) != 0) { h += tasks[i].hours; v += tasks[i].value; }
            if (h <= budget && v > top) { top = v; bestMask = mask; }
        }
        chosen.Clear();
        for (int i = 0; i < n; i++) if ((bestMask & (1 << i)) != 0) chosen.Add(i);
        Submit();
    }
}
