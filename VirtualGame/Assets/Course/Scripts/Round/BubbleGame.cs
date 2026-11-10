using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 排序演算法（泡沫排序）：每次只能把相鄰兩個交換，把店家評分從低排到高
// 每一次交換如果都是「左邊比右邊大才換」，次數就是最少的
public class BubbleGame : MonitorGame
{
    public override string Title => "排序：一次換兩個";

    readonly List<int> values = new List<int>();
    readonly List<(RectTransform bar, Text label)> bars = new List<(RectTransform, Text)>();
    int swaps, best;

    protected override void Setup()
    {
        int n = 6 + Mathf.Min(2, difficulty);
        values.Clear(); bars.Clear();
        swaps = 0;
        var pool = new List<int>();
        for (int i = 1; i <= 9; i++) pool.Add(i);
        CampaignDirector.Shuffle(pool, new System.Random());
        values.AddRange(pool.GetRange(0, n));
        best = 0;
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++) if (values[i] > values[j]) best++;
        if (best == 0) { (values[0], values[1]) = (values[1], values[0]); best = 1; }

        Label("點兩根柱子中間的 ⇄ 交換它們，從矮排到高", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        float gap = 860f / n;
        for (int i = 0; i < n; i++)
        {
            RectTransform bar = Box(Blue, new Vector2(-430 + gap * (i + 0.5f), -120), new Vector2(gap * 0.6f, 0));
            bar.pivot = new Vector2(0.5f, 0);
            Text t = Label("", 26, Ink, new Vector2(-430 + gap * (i + 0.5f), -145), new Vector2(60, 34));
            bars.Add((bar, t));
            if (i < n - 1)
            {
                int k = i;
                Btn("⇄", Paper, Ink, new Vector2(-430 + gap * (i + 1), 140), new Vector2(gap * 0.5f, 44), () => Swap(k));
            }
        }
        Paint();
    }

    void Paint()
    {
        bool sorted = true;
        for (int i = 0; i < values.Count; i++)
        {
            bars[i].bar.sizeDelta = new Vector2(bars[i].bar.sizeDelta.x, values[i] * 26);
            bars[i].label.text = values[i].ToString();
            if (i > 0 && values[i - 1] > values[i]) sorted = false;
        }
        if (sorted && Running) Finish(swaps <= best ? 3 : swaps <= best + 4 ? 2 : 1, $"排好了！換了 {swaps} 次（最少 {best} 次）");
    }

    void Swap(int i)
    {
        if (!Running) return;
        (values[i], values[i + 1]) = (values[i + 1], values[i]);
        swaps++;
        Paint();
    }

    protected override string Goal() => $"交換了 {swaps} 次（最少要 {best} 次）";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(0, "時間到，還沒排好");
    public override void AutoPlay() { for (int i = 0; i + 1 < values.Count; i++) if (values[i] > values[i + 1]) { Swap(i); return; } }
}
