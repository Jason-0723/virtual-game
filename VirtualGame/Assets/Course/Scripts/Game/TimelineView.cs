using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 因果時間軸：下班後像 git log --graph 一樣列出今天每個選擇，
// 「這件事是那個選擇造成的」用紅線連起來；按「看結局」才接 4 種結局
// 每一行都是執行時才生的（今天選了幾件事不一定），所以 UI 用程式畫
public class TimelineView : MonoBehaviour
{
    static readonly Color Paper = new Color32(0xef, 0xeb, 0xe2, 255), Ink = new Color32(0x1f, 0x20, 0x1d, 255),
        Dim = new Color32(0x55, 0x56, 0x4f, 255), Red = new Color32(0xc2, 0x3b, 0x22, 255),
        Amber = new Color32(0xa8, 0x6f, 0x12, 255), Rail = new Color32(0xb9, 0xb1, 0x9f, 255);

    GameObject panel;
    Action onContinue;
    Font font, mono;

    public bool IsOpen => panel != null && panel.activeSelf;
    public int LinkCount { get; private set; }

    public void Show(IReadOnlyList<DayManager.Record> records, Action then)
    {
        onContinue = then;
        font = DayManager.UiFont;
        mono = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New" }, 22);
        if (panel) Destroy(panel);

        // 整個畫面先蓋一層暗色，中間放一張紙
        panel = Rect("Timeline", transform, new Color(0.08f, 0.07f, 0.06f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000)).gameObject;
        RectTransform paper = Rect("Paper", panel.transform, Paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560, 1000));
        Text title = Label(paper, "下班了。這一週的提交紀錄", 40, Red, new Vector2(60, -36), new Vector2(1200, 56));
        title.font = Font.CreateDynamicFontFromOSFont(new[] { "PMingLiU", "MingLiU", "Noto Serif TC" }, 40);
        Label(paper, "git log --graph --since=週一　　紅線：後面那件事，是前面那個選擇造成的", 22, Dim, new Vector2(60, -96), new Vector2(1400, 34));

        int n = Mathf.Max(1, records.Count);
        const float top = -150, dotX = 300;
        float rowH = Mathf.Min(58, 720f / n);
        int fs = Mathf.Clamp(Mathf.RoundToInt(rowH * 0.55f), 15, 24);   // 三天的紀錄比較多，行高變小時字也跟著縮
        float Y(int i) => top - i * rowH - rowH / 2;

        if (records.Count > 1) Line(paper, Rail, dotX, Y(0), dotX, Y(records.Count - 1), 3);

        // 紅線：每條因果關係走自己的一條「車道」，才不會疊在一起
        var row = new Dictionary<int, int>();
        for (int i = 0; i < records.Count; i++) row[records[i].no] = i;
        LinkCount = 0;
        for (int j = 0; j < records.Count; j++)
            foreach (int c in records[j].causes)
            {
                if (!row.TryGetValue(c, out int i)) continue;
                float lane = dotX - 44 - 18 * (LinkCount % 12);
                Line(paper, Red, lane, Y(i), dotX, Y(i), 3);
                Line(paper, Red, lane, Y(i), lane, Y(j), 3);
                Line(paper, Red, lane, Y(j), dotX, Y(j), 3);
                LinkCount++;
            }

        for (int i = 0; i < records.Count; i++)
        {
            DayManager.Record r = records[i];
            bool effect = r.causes.Count > 0;
            RectTransform dot = Rect("Dot", paper, effect ? Red : Ink, new Vector2(0, 1), new Vector2(dotX, Y(i)), new Vector2(16, 16));
            dot.pivot = new Vector2(0.5f, 0.5f);
            dot.localRotation = Quaternion.Euler(0, 0, 45);   // 菱形當作 commit 點
            Text hash = Label(paper, Hash(r), fs - 2, Amber, new Vector2(dotX + 34, Y(i) + rowH / 2), new Vector2(110, rowH));
            hash.font = mono;
            hash.alignment = TextAnchor.MiddleLeft;
            Text when = Label(paper, r.time, fs - 2, Dim, new Vector2(dotX + 150, Y(i) + rowH / 2), new Vector2(150, rowH));
            when.alignment = TextAnchor.MiddleLeft;
            string because = effect ? "　← 因為 " + string.Join("、", r.causes.ConvertAll(c => Hash(records[row[c]]))) : "";
            Text msg = Label(paper, r.text + because, fs, effect ? Red : Ink, new Vector2(dotX + 310, Y(i) + rowH / 2), new Vector2(950, rowH));
            msg.alignment = TextAnchor.MiddleLeft;
        }
        if (records.Count == 0) Label(paper, "今天沒有做任何決定。", 26, Dim, new Vector2(dotX, top), new Vector2(800, 40));

        // 「看結局」按鈕
        RectTransform btn = Rect("Continue", paper, Ink, new Vector2(1, 0), new Vector2(-60, 40), new Vector2(260, 70));
        btn.pivot = new Vector2(1, 0);
        Text bl = Label(btn, "看結局 →", 28, Paper, Vector2.zero, new Vector2(260, 70));
        bl.rectTransform.anchorMin = bl.rectTransform.anchorMax = bl.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        bl.alignment = TextAnchor.MiddleCenter;
        btn.gameObject.AddComponent<Button>().onClick.AddListener(Continue);

        panel.transform.SetAsLastSibling();
    }

    public void Continue()
    {
        panel.SetActive(false);
        onContinue?.Invoke();
    }

    // 像 git 的 commit 編號：7 位 16 進位，由編號和內容算出來，每次玩同樣的選擇會一樣
    static string Hash(DayManager.Record r)
    {
        uint h = 2166136261;
        foreach (char ch in r.no + r.text) h = (h ^ ch) * 16777619;
        return (h & 0xfffffff).ToString("x7");
    }

    // ───── UI 小工具（錨點都在紙的左上角）─────
    static RectTransform Rect(string name, Transform parent, Color c, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        g.GetComponent<Image>().color = c;
        var rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    Text Label(Transform parent, string text, int size, Color c, Vector2 pos, Vector2 box)
    {
        var g = new GameObject("Text", typeof(RectTransform), typeof(Text));
        g.transform.SetParent(parent, false);
        var rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        Text t = g.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = c;
        t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // 兩點之間畫一條直線或橫線（只會用到水平和垂直）
    static void Line(Transform parent, Color c, float x1, float y1, float x2, float y2, float w)
    {
        RectTransform r = Rect("Line", parent, c, new Vector2(0, 1), new Vector2(Mathf.Min(x1, x2) - w / 2, Mathf.Max(y1, y2) + w / 2),
            new Vector2(Mathf.Abs(x2 - x1) + w, Mathf.Abs(y2 - y1) + w));
        r.GetComponent<Image>().raycastTarget = false;
    }
}
