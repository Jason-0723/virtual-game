using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 接通網路：從左邊的「客人」一格一格點到右邊的「伺服器」，避開斷線的格子（紅色）
// 只能點路線最後一格旁邊的格子；點路線最後一格可以退回。路越短星星越多
public class RouteGame : MonitorGame
{
    public override string Title => "接通網路";

    const int W = 9, H = 5;
    readonly Vector2Int start = new Vector2Int(0, 2), goal = new Vector2Int(W - 1, 2);
    readonly HashSet<Vector2Int> broken = new HashSet<Vector2Int>();
    readonly List<Vector2Int> path = new List<Vector2Int>();
    readonly Dictionary<Vector2Int, Image> cells = new Dictionary<Vector2Int, Image>();
    int shortest;

    protected override void Setup()
    {
        cells.Clear(); path.Clear();
        // 斷線格子越多，路越彎；隨機到走不通就重來
        do
        {
            broken.Clear();
            int count = Mathf.Min(20, 8 + difficulty * 3);
            while (broken.Count < count)
            {
                var c = new Vector2Int(Random.Range(1, W - 1), Random.Range(0, H));
                broken.Add(c);
            }
            shortest = Bfs(start, new HashSet<Vector2Int>())?.Count ?? -1;
        } while (shortest < 0);
        path.Add(start);
        Label("從「客人」一格一格點到「伺服器」，避開紅色斷線", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var c = new Vector2Int(x, y);
                RectTransform r = Box(Paper, new Vector2((x - 4) * 92, 115 - y * 68), new Vector2(84, 60));
                string text = c == start ? "客人" : c == goal ? "伺服器" : broken.Contains(c) ? "斷線" : "";
                Label(text, 20, broken.Contains(c) ? Color.white : Ink, Vector2.zero, new Vector2(84, 60), r).raycastTarget = false;
                r.gameObject.AddComponent<Button>().onClick.AddListener(() => Click(c));
                cells[c] = r.GetComponent<Image>();
            }
        Paint();
    }

    void Click(Vector2Int c)
    {
        if (!Running || broken.Contains(c)) return;
        Vector2Int end = path[path.Count - 1];
        if (c == end && path.Count > 1) path.RemoveAt(path.Count - 1);
        else if (!path.Contains(c) && Mathf.Abs(c.x - end.x) + Mathf.Abs(c.y - end.y) == 1) path.Add(c);
        else return;
        Paint();
        if (c == goal)
        {
            int extra = path.Count - 1 - shortest;
            Finish(extra <= 0 ? 3 : extra <= 4 ? 2 : 1, extra <= 0 ? $"接通了！走了 {path.Count - 1} 格，是最短的路" : $"接通了！走了 {path.Count - 1} 格（最短 {shortest} 格）");
        }
    }

    void Paint()
    {
        foreach (var kv in cells)
            kv.Value.color = broken.Contains(kv.Key) ? Red : kv.Key == goal ? Gold : path.Contains(kv.Key) ? (Color)new Color32(0xb8, 0xec, 0xc9, 255) : Paper;
    }

    // 從 from 走到終點的最短路（不含起點），走不到回傳 null
    List<Vector2Int> Bfs(Vector2Int from, HashSet<Vector2Int> blocked)
    {
        var prev = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
        var q = new Queue<Vector2Int>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            Vector2Int p = q.Dequeue();
            if (p == goal)
            {
                var route = new List<Vector2Int>();
                for (; p != from; p = prev[p]) route.Insert(0, p);
                return route;
            }
            foreach (Vector2Int d in new[] { Vector2Int.right, Vector2Int.up, Vector2Int.down, Vector2Int.left })
            {
                Vector2Int n = p + d;
                if (n.x < 0 || n.y < 0 || n.x >= W || n.y >= H || broken.Contains(n) || blocked.Contains(n) || prev.ContainsKey(n)) continue;
                prev[n] = p;
                q.Enqueue(n);
            }
        }
        return null;
    }

    protected override string Goal() => $"已經接了 {path.Count - 1} 格（最短 {shortest} 格）";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(0, "時間到，網路還沒接通");

    public override void AutoPlay()
    {
        var route = Bfs(path[path.Count - 1], new HashSet<Vector2Int>(path));
        if (route != null && route.Count > 0) Click(route[0]);
        else if (path.Count > 1) Click(path[path.Count - 1]);   // 走進死路：退一格
    }
}
