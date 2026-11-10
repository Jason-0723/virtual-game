using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 樹狀結構：電腦的資料夾就是一棵樹，從最上面一層一層點進去找檔案
// 想一下檔案應該放在哪一類，就不用每個資料夾都打開
public class TreeGame : MonitorGame
{
    public override string Title => "在資料夾樹找檔案";

    // 根 → 3 個大類 → 各 3 個小類 → 各 2 個檔案
    static readonly (string name, (string name, string[] files)[] subs)[] Tree =
    {
        ("圖片", new[] { ("菜單", new[] { "週一菜單.png", "週五菜單.png" }), ("Logo", new[] { "App圖示.png", "首頁橫幅.png" }), ("外送員", new[] { "大頭照.png", "制服.png" }) }),
        ("文件", new[] { ("合約", new[] { "店家合約.pdf", "外送員合約.pdf" }), ("報表", new[] { "十月營收.xlsx", "十一月營收.xlsx" }), ("會議", new[] { "週會記錄.docx", "上線檢討.docx" }) }),
        ("程式", new[] { ("網站", new[] { "首頁.html", "樣式.css" }), ("App", new[] { "登入畫面.swift", "購物車.kt" }), ("伺服器", new[] { "訂單.py", "付款.py" }) }),
    };

    int rounds, found, clicks, a = -1, b = -1;
    (int a, int b, int f) want;
    readonly List<Button> items = new List<Button>();
    Text path, ask;

    protected override void Setup()
    {
        rounds = 3 + Mathf.Min(2, difficulty);
        found = clicks = 0;
        Label("點資料夾一層一層找，「⬅ 上一層」可以退回", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        ask = Label("", 30, Orange, new Vector2(0, 130), new Vector2(880, 44));
        path = Label("", 22, Blue, new Vector2(0, 88), new Vector2(880, 30));
        NewFile();
    }

    void NewFile()
    {
        want = (Random.Range(0, 3), Random.Range(0, 3), Random.Range(0, 2));
        ask.text = "找出：" + Tree[want.a].subs[want.b].files[want.f];
        a = b = -1;
        Show();
    }

    void Show()
    {
        foreach (Button x in items) Destroy(x.gameObject);
        items.Clear();
        var names = new List<string>();
        if (a < 0) foreach (var t in Tree) names.Add("▸ " + t.name);
        else if (b < 0) foreach (var s in Tree[a].subs) names.Add("▸ " + s.name);
        else foreach (string f in Tree[a].subs[b].files) names.Add("・" + f);
        path.text = "飯飯外送" + (a >= 0 ? " / " + Tree[a].name : "") + (b >= 0 ? " / " + Tree[a].subs[b].name : "");
        for (int i = 0; i < names.Count; i++)
        {
            int k = i;
            items.Add(Btn(names[i], Paper, Ink, new Vector2(-120, 30 - i * 70), new Vector2(420, 60), () => Open(k)));
        }
        if (a >= 0) items.Add(Btn("⬅ 上一層", new Color32(0xdd, 0xd5, 0xc4, 255), Ink, new Vector2(270, 30), new Vector2(180, 60), Up));
    }

    void Open(int i)
    {
        if (!Running) return;
        clicks++;
        if (a < 0) a = i;
        else if (b < 0) b = i;
        else if (a == want.a && b == want.b && i == want.f)
        {
            if (++found >= rounds) { Finish(clicks <= rounds * 3 ? 3 : clicks <= rounds * 5 ? 2 : 1, $"找到 {rounds} 個檔案，點了 {clicks} 下（最少 {rounds * 3} 下）"); return; }
            NewFile();
            return;
        }
        Show();
    }

    void Up()
    {
        if (!Running) return;
        clicks++;
        if (b >= 0) b = -1; else a = -1;
        Show();
    }

    protected override string Goal() => $"找到 {found} / {rounds} 個　點了 {clicks} 下";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(found * 2 >= rounds ? 1 : 0, $"時間到：找到 {found} / {rounds} 個");

    public override void AutoPlay()
    {
        if (a >= 0 && a != want.a || b >= 0 && b != want.b) Up();
        else Open(a < 0 ? want.a : b < 0 ? want.b : want.f);
    }
}
