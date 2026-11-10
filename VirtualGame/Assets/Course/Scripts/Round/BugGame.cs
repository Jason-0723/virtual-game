using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 抓蟲：App 畫面上有蟲在爬，用滑鼠點掉。難度越高，要抓的越多、跑越快
public class BugGame : MonitorGame
{
    public override string Title => "抓蟲大作戰";

    readonly List<RectTransform> bugs = new List<RectTransform>();
    readonly Dictionary<RectTransform, Vector2> targets = new Dictionary<RectTransform, Vector2>();
    static Sprite sprite;
    int need, caught;
    float speed;

    protected override void Setup()
    {
        bugs.Clear(); targets.Clear();
        need = 8 + difficulty * 2;
        speed = 150 + difficulty * 45;
        caught = 0;
        if (!sprite) sprite = MakeBugSprite();
        for (int i = 0; i < 5; i++) Box(Paper, new Vector2(0, 150 - i * 76), new Vector2(860, 60)).GetComponent<Image>().raycastTarget = false;
        for (int i = 0; i < 4; i++) Spawn();
    }

    protected override string Goal() => $"點掉螢幕上的蟲：{caught} / {need}";

    protected override void Tick()
    {
        foreach (RectTransform b in bugs)
        {
            Vector2 t = targets[b];
            b.anchoredPosition = Vector2.MoveTowards(b.anchoredPosition, t, speed * Time.deltaTime);
            Vector2 d = t - b.anchoredPosition;
            if (d.sqrMagnitude > 1) b.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90);
            else targets[b] = RandomPoint();
        }
    }

    protected override void TimeUp()
    {
        float r = (float)caught / need;
        Finish(r >= 1 ? 3 : r >= 0.7f ? 2 : r >= 0.4f ? 1 : 0, $"抓到 {caught} / {need} 隻蟲");
    }

    public override void AutoPlay() { if (bugs.Count > 0) Squash(bugs[0]); }

    void Spawn()
    {
        var g = new GameObject("Bug", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)g.transform;
        rt.SetParent(area, false);
        rt.sizeDelta = new Vector2(64, 64);
        rt.anchoredPosition = RandomPoint();
        g.GetComponent<Image>().sprite = sprite;
        g.GetComponent<Button>().onClick.AddListener(() => Squash(rt));
        bugs.Add(rt);
        targets[rt] = RandomPoint();
    }

    void Squash(RectTransform b)
    {
        if (!Running) return;
        bugs.Remove(b); targets.Remove(b);
        Destroy(b.gameObject);
        caught++;
        if (caught >= need) { Finish(Left > 8 ? 3 : 2, $"全部抓完了！（{caught} 隻）"); return; }
        Spawn();
    }

    Vector2 RandomPoint() => new Vector2(Random.Range(-410f, 410f), Random.Range(-170f, 170f));

    // 用程式畫一隻蟲：深紅身體＋六隻腳＋觸角
    static Sprite MakeBugSprite()
    {
        const int n = 64;
        var t = new Texture2D(n, n);
        var clear = new Color(0, 0, 0, 0);
        var body = new Color32(0xb8, 0x2b, 0x1e, 255);
        var dark = new Color32(0x30, 0x14, 0x10, 255);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - 32) / 13f, dy = (y - 30) / 20f;
                bool inBody = dx * dx + dy * dy <= 1;
                bool head = (x - 32) * (x - 32) + (y - 52) * (y - 52) <= 64;
                bool spot = inBody && ((x - 27) * (x - 27) + (y - 26) * (y - 26) < 10 || (x - 37) * (x - 37) + (y - 34) * (y - 34) < 10);
                bool leg = !inBody && (Mathf.Abs(y - 20) < 2 || Mathf.Abs(y - 30) < 2 || Mathf.Abs(y - 40) < 2) && Mathf.Abs(x - 32) < 24;
                bool antenna = y > 56 && (Mathf.Abs((x - 32) - (y - 56)) < 2 || Mathf.Abs((32 - x) - (y - 56)) < 2);
                t.SetPixel(x, y, spot || head || leg || antenna ? (Color)dark : inBody ? (Color)body : clear);
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
}
