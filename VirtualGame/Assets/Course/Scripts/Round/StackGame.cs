using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 堆疊（Stack）：餐盒照 1、2、3⋯⋯ 的順序從廚房出來，只能疊在桌上，也只能拿最上面那個
// 用「疊上去」和「拿最上面」兩個按鈕，照外送員要的順序交出去
public class StackGame : MonitorGame
{
    public override string Title => "疊餐盒（堆疊）";

    int n, nextIn, outIndex, mistakes;
    readonly List<int> stack = new List<int>(), target = new List<int>();
    Text kitchen, pile, wanted, hint;

    protected override void Setup()
    {
        n = 4 + Mathf.Min(3, difficulty);
        nextIn = 1; outIndex = mistakes = 0;
        stack.Clear(); target.Clear();
        // 隨機做一次合法的疊／拿，得到一定做得到的順序
        var s = new List<int>();
        int k = 1;
        while (target.Count < n)
            if (k <= n && (s.Count == 0 || Random.value < 0.55f)) s.Add(k++);
            else { target.Add(s[s.Count - 1]); s.RemoveAt(s.Count - 1); }

        Label("只能拿最上面的餐盒！照外送員要的順序交出去", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        Label("廚房出餐", 20, Ink, new Vector2(-300, 125), new Vector2(240, 30));
        kitchen = Label("", 28, Ink, new Vector2(-300, 70), new Vector2(260, 80));
        Label("桌上的疊（上面在上）", 20, Ink, new Vector2(0, 125), new Vector2(260, 30));
        Box(new Color32(0xdd, 0xd5, 0xc4, 255), new Vector2(0, -10), new Vector2(150, 230));
        pile = Label("", 28, Ink, new Vector2(0, -10), new Vector2(140, 220), null, TextAnchor.LowerCenter);
        Label("外送員要的順序", 20, Ink, new Vector2(300, 125), new Vector2(240, 30));
        wanted = Label("", 28, Ink, new Vector2(300, 70), new Vector2(280, 80));
        Btn("疊上去 ⬆", Paper, Ink, new Vector2(-300, -60), new Vector2(200, 64), Push);
        Btn("拿最上面 ➜", Gold, Ink, new Vector2(300, -60), new Vector2(200, 64), Pop);
        hint = Label("", 20, Red, new Vector2(0, -165), new Vector2(880, 28));
        Show();
    }

    void Show()
    {
        string k = "";
        for (int i = nextIn; i <= n; i++) k += $"[{i}] ";
        kitchen.text = k == "" ? "（出完了）" : k;
        string p = "";
        for (int i = stack.Count - 1; i >= 0; i--) p += $"[{stack[i]}]\n";
        pile.text = p;
        string w = "";
        for (int i = 0; i < target.Count; i++) w += i < outIndex ? "✔ " : $"{target[i]} ";
        wanted.text = w;
    }

    void Push()
    {
        if (!Running) return;
        if (nextIn > n) { hint.text = "廚房已經沒有餐盒了"; return; }
        stack.Add(nextIn++);
        hint.text = "";
        Show();
    }

    void Pop()
    {
        if (!Running) return;
        if (stack.Count == 0) { hint.text = "桌上沒有餐盒"; return; }
        int top = stack[stack.Count - 1];
        if (top != target[outIndex]) { mistakes++; hint.text = $"最上面是 [{top}]，外送員要的是 {target[outIndex]}"; return; }
        stack.RemoveAt(stack.Count - 1);
        outIndex++;
        hint.text = "";
        Show();
        if (outIndex >= n) Finish(mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1, $"全部交出去了！拿錯 {mistakes} 次");
    }

    protected override string Goal() => $"交出 {outIndex} / {n} 盒　拿錯 {mistakes} 次";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(outIndex * 2 >= n ? 1 : 0, $"時間到：交出 {outIndex} / {n} 盒");
    public override void AutoPlay() { if (stack.Count > 0 && stack[stack.Count - 1] == target[outIndex]) Pop(); else Push(); }
}
