using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 找出壞掉的版本：App 有 16 個版本，第 1 版正常、最後一版壞了，中間某一版開始壞掉
// 點一個版本「測試看看」，會顯示正常或壞掉；用最少的次數找出「第一個壞掉的版本」
// 每次從中間猜最快（工程師叫它二分搜尋，git bisect）
public class BisectGame : MonitorGame
{
    public override string Title => "找出壞掉的版本";

    int n, firstBad, tests;
    readonly Dictionary<int, Image> boxes = new Dictionary<int, Image>();
    readonly Dictionary<int, bool> known = new Dictionary<int, bool>();

    protected override void Setup()
    {
        n = difficulty >= 2 ? 24 : 16;
        firstBad = Random.Range(2, n + 1);   // 第 1 版一定正常、第 n 版一定壞
        tests = 0;
        boxes.Clear(); known.Clear();
        known[1] = true; known[n] = false;
        Label($"第 1 版正常、第 {n} 版壞了。點版本來測試，找出「第一個壞掉」的版本", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        int perRow = 8;
        for (int v = 1; v <= n; v++)
        {
            int k = v;
            int row = (v - 1) / perRow, col = (v - 1) % perRow;
            RectTransform r = Box(Paper, new Vector2((col - 3.5f) * 105, 100 - row * 90), new Vector2(95, 75));
            Label($"v{v}", 26, Ink, Vector2.zero, new Vector2(90, 70), r).raycastTarget = false;
            r.gameObject.AddComponent<Button>().onClick.AddListener(() => Test(k));
            boxes[v] = r.GetComponent<Image>();
        }
        Paint();
    }

    void Test(int v)
    {
        if (!Running || known.ContainsKey(v)) return;
        tests++;
        known[v] = v < firstBad;
        Paint();
        // 找到了：這一版壞、前一版正常
        if (known.TryGetValue(firstBad, out bool b) && !b && known.TryGetValue(firstBad - 1, out bool g) && g)
        {
            int ideal = Mathf.CeilToInt(Mathf.Log(n, 2));
            Finish(tests <= ideal ? 3 : tests <= ideal + 2 ? 2 : 1, $"找到了！從 v{firstBad} 開始壞掉，測了 {tests} 次（最快大約 {ideal} 次）");
        }
    }

    void Paint()
    {
        foreach (var kv in boxes)
            kv.Value.color = known.TryGetValue(kv.Key, out bool ok) ? (ok ? (Color)new Color32(0xb8, 0xec, 0xc9, 255) : (Color)new Color32(0xff, 0xc4, 0xbb, 255)) : (Color)Paper;
    }

    protected override string Goal() => $"已測試 {tests} 次（綠＝正常、紅＝壞掉）";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(0, $"時間到。其實是從 v{firstBad} 開始壞的");

    // 自動試玩：二分搜尋
    public override void AutoPlay()
    {
        int lo = 1, hi = n;   // lo 正常、hi 壞
        foreach (var kv in known) { if (kv.Value && kv.Key > lo) lo = kv.Key; if (!kv.Value && kv.Key < hi) hi = kv.Key; }
        if (hi - lo > 1) Test((lo + hi) / 2);
    }
}
