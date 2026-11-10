using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 找不同：左邊是設計稿，右邊是做出來的畫面；點出右邊和設計稿不一樣的地方
// 每次隨機挑幾個元件改掉（顏色、文字、價格），難度越高要找的越多、時間越短
public class DiffGame : MonitorGame
{
    public override string Title => "設計稿找不同";

    class El { public string name, text; public Vector2 pos, size; public Color bg, fg; public string altText; public Color altBg; }

    readonly List<El> els = new List<El>();
    readonly HashSet<string> diffs = new HashSet<string>(), found = new HashSet<string>();
    readonly List<Button> builtButtons = new List<Button>();
    int misses;

    protected override void Setup()
    {
        els.Clear(); diffs.Clear(); found.Clear(); builtButtons.Clear(); misses = 0;
        els.Add(new El { name = "logo", text = "飯飯外送", pos = new Vector2(-80, 140), size = new Vector2(220, 44), bg = Orange, fg = Color.white, altBg = Blue });
        els.Add(new El { name = "search", text = "搜尋餐廳", pos = new Vector2(0, 85), size = new Vector2(380, 40), bg = Paper, fg = Ink, altText = "搜尋" });
        els.Add(new El { name = "food1", text = "雞排飯", pos = new Vector2(-95, 20), size = new Vector2(180, 64), bg = new Color32(0xff, 0xe3, 0xcc, 255), fg = Ink, altText = "雞腿飯" });
        els.Add(new El { name = "food2", text = "牛肉麵", pos = new Vector2(95, 20), size = new Vector2(180, 64), bg = new Color32(0xff, 0xe3, 0xcc, 255), fg = Ink, altBg = new Color32(0xd4, 0xf0, 0xd8, 255) });
        els.Add(new El { name = "price", text = "NT$ 120", pos = new Vector2(-95, -55), size = new Vector2(180, 40), bg = Color.white, fg = Ink, altText = "NT$ 0" });
        els.Add(new El { name = "cart", text = "購物車", pos = new Vector2(95, -55), size = new Vector2(180, 40), bg = Green, fg = Color.white, altBg = Red });
        els.Add(new El { name = "pay", text = "結帳", pos = new Vector2(0, -115), size = new Vector2(380, 44), bg = Ink, fg = Color.white, altText = "結帳（暫停服務）" });

        int count = Mathf.Min(3 + difficulty / 2, 5);
        var pool = new List<El>(els);
        for (int i = 0; i < count; i++) { int k = Random.Range(0, pool.Count); diffs.Add(pool[k].name); pool.RemoveAt(k); }

        Label("設計稿（正確的）", 20, Ink, new Vector2(-225, 185), new Vector2(420, 30));
        Label("做出來的（點出不一樣的地方）", 20, Red, new Vector2(225, 185), new Vector2(420, 30));
        RectTransform left = Box(Color.white, new Vector2(-225, -10), new Vector2(430, 350));
        RectTransform right = Box(Color.white, new Vector2(225, -10), new Vector2(430, 350));
        foreach (El e in els)
        {
            Draw(e, left, false);
            Button b = Draw(e, right, diffs.Contains(e.name));
            builtButtons.Add(b);
        }
    }

    Button Draw(El e, RectTransform parent, bool changed)
    {
        string text = changed && e.altText != null ? e.altText : e.text;
        Color bg = changed && e.altText == null ? e.altBg : e.bg;
        Vector2 scale = new Vector2(0.95f, 0.95f);
        RectTransform r = Box(bg, Vector2.Scale(e.pos, scale), Vector2.Scale(e.size, scale), parent);
        r.name = e.name;
        Label(text, 20, e.fg, Vector2.zero, r.sizeDelta, r).raycastTarget = false;
        Button b = r.gameObject.AddComponent<Button>();
        if (parent.anchoredPosition.x > 0) b.onClick.AddListener(() => Click(e.name, r));
        else r.GetComponent<Image>().raycastTarget = false;
        return b;
    }

    void Click(string name, RectTransform r)
    {
        if (!Running || found.Contains(name)) return;
        if (diffs.Contains(name))
        {
            found.Add(name);
            Outline o = r.gameObject.AddComponent<Outline>();
            o.effectColor = Green; o.effectDistance = new Vector2(5, -5);
            if (found.Count == diffs.Count) Finish(misses == 0 ? 3 : misses <= 2 ? 2 : 1, $"找到全部 {diffs.Count} 個不同，點錯 {misses} 次");
        }
        else misses++;
    }

    protected override string Goal() => $"找到 {found.Count} / {diffs.Count} 個不同（點錯 {misses} 次）";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(found.Count >= diffs.Count - 1 && found.Count > 0 ? 1 : 0, $"時間到：找到 {found.Count} / {diffs.Count} 個");

    public override void AutoPlay()
    {
        foreach (Button b in builtButtons)
            if (diffs.Contains(b.name) && !found.Contains(b.name)) { b.onClick.Invoke(); return; }
    }
}
