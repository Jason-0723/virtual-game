using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 什麼是程式：先排好一串指令（↑↓←→），按「執行」後外送機器人照著走，要剛好停在客人那格
// 電腦只會照指令做，撞牆、走過頭都不會自己修正
public class RobotGame : MonitorGame
{
    public override string Title => "寫程式給外送機器人";

    const int W = 7, H = 4;
    static readonly Vector2Int[] Dirs = { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right };   // 畫面上的 ↑ 是 y-1
    static readonly string[] Arrows = { "↑", "↓", "←", "→" };

    readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
    readonly List<int> program = new List<int>();
    readonly Dictionary<Vector2Int, Image> cells = new Dictionary<Vector2Int, Image>();
    Vector2Int robot, goal;
    int tries, shortest, step = -1;
    float nextStep;
    Text code, hint;
    RectTransform robotIcon;

    protected override void Setup()
    {
        program.Clear(); cells.Clear();
        tries = 0; step = -1;
        do
        {
            walls.Clear();
            for (int i = 0; i < 4 + difficulty * 2; i++) walls.Add(new Vector2Int(Random.Range(1, W), Random.Range(0, H)));
            goal = new Vector2Int(Random.Range(4, W), Random.Range(0, H));
            walls.Remove(goal);
            shortest = Path(Vector2Int.zero)?.Count ?? -1;
        } while (shortest < 4);
        Label("排好指令，再按「執行」：讓機器人走到客人那格", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var c = new Vector2Int(x, y);
                RectTransform r = Box(Paper, CellPos(c), new Vector2(66, 66));
                if (c == goal) Label("客人", 20, Ink, Vector2.zero, new Vector2(66, 66), r);
                if (walls.Contains(c)) Label("牆", 20, Color.white, Vector2.zero, new Vector2(66, 66), r);
                cells[c] = r.GetComponent<Image>();
            }
        robotIcon = Box(Orange, CellPos(Vector2Int.zero), new Vector2(44, 44));
        Label("車", 22, Color.white, Vector2.zero, new Vector2(44, 44), robotIcon);
        for (int i = 0; i < 4; i++)
        {
            int k = i;
            Btn(Arrows[i], Paper, Ink, new Vector2(250 + (i % 2) * 90, 95 - (i / 2) * 75), new Vector2(80, 64), () => Add(k)).GetComponentInChildren<Text>().fontSize = 34;
        }
        Btn("清除", Paper, Ink, new Vector2(250, -55), new Vector2(80, 56), () => { if (step < 0) { program.Clear(); Show(); } });
        Btn("執行 ▶", Green, Color.white, new Vector2(340, -55), new Vector2(90, 56), Run);
        code = Label("", 22, Ink, new Vector2(-130, -165), new Vector2(620, 34), null, TextAnchor.MiddleLeft);
        hint = Label("", 20, Red, new Vector2(295, -120), new Vector2(240, 30));
        robot = Vector2Int.zero;
        Paint();
        Show();
    }

    Vector2 CellPos(Vector2Int c) => new Vector2(-370 + c.x * 72, 110 - c.y * 72);
    void Show() => code.text = "程式：" + (program.Count == 0 ? "（還沒有指令）" : string.Join(" ", program.ConvertAll(i => Arrows[i])));

    void Paint()
    {
        foreach (var kv in cells) kv.Value.color = walls.Contains(kv.Key) ? (Color)Ink : kv.Key == goal ? Gold : Paper;
        robotIcon.anchoredPosition = CellPos(robot);
    }

    void Add(int dir)
    {
        if (!Running || step >= 0 || program.Count >= 16) return;
        program.Add(dir);
        hint.text = "";
        Show();
    }

    void Run()
    {
        if (!Running || step >= 0 || program.Count == 0) return;
        tries++;
        robot = Vector2Int.zero;
        step = 0;
        nextStep = Time.time + 0.25f;
        Paint();
    }

    protected override void Tick()
    {
        if (step < 0 || Time.time < nextStep) return;
        nextStep = Time.time + 0.28f;
        if (step >= program.Count)
        {
            step = -1;
            if (robot == goal)
            {
                bool best = program.Count == shortest;
                Finish(tries == 1 ? (best ? 3 : 2) : tries <= 3 ? 2 : 1, $"送到了！用了 {program.Count} 個指令（最少 {shortest} 個），試了 {tries} 次");
            }
            else { hint.text = "沒停在客人那格"; robot = Vector2Int.zero; Paint(); }
            return;
        }
        Vector2Int n = robot + Dirs[program[step]];
        if (n.x < 0 || n.y < 0 || n.x >= W || n.y >= H || walls.Contains(n))
        {
            hint.text = $"第 {step + 1} 個指令撞到了！";
            step = -1; robot = Vector2Int.zero; Paint();
            return;
        }
        robot = n;
        Paint();
        step++;
    }

    // 最短路線（方向編號），走不到回傳 null
    List<int> Path(Vector2Int from)
    {
        var prev = new Dictionary<Vector2Int, (Vector2Int, int)> { [from] = (from, -1) };
        var q = new Queue<Vector2Int>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            Vector2Int p = q.Dequeue();
            if (p == goal)
            {
                var route = new List<int>();
                for (; p != from; p = prev[p].Item1) route.Insert(0, prev[p].Item2);
                return route;
            }
            for (int d = 0; d < 4; d++)
            {
                Vector2Int n = p + Dirs[d];
                if (n.x < 0 || n.y < 0 || n.x >= W || n.y >= H || walls.Contains(n) || prev.ContainsKey(n)) continue;
                prev[n] = (p, d);
                q.Enqueue(n);
            }
        }
        return null;
    }

    protected override string Goal() => $"指令 {program.Count} 個　試了 {tries} 次";
    protected override void TimeUp() => Finish(0, "時間到，機器人還沒送到");

    public override void AutoPlay()
    {
        if (step >= 0) return;
        if (program.Count == 0) program.AddRange(Path(Vector2Int.zero));
        Run();
    }
}
