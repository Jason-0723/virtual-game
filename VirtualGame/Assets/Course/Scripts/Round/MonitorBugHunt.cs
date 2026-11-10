using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 座位電腦螢幕上的小遊戲：App 畫面上有蟲在爬，用滑鼠點掉牠們
// 畫在一塊貼在螢幕上的 World Space Canvas，所以鏡頭從角色肩膀後面看過去，看得到角色的手和螢幕
public class MonitorBugHunt : MonoBehaviour
{
    public RectTransform area;     // 蟲可以爬的範圍（螢幕上的 App 畫面）
    public Text status;

    readonly List<RectTransform> bugs = new List<RectTransform>();
    readonly Dictionary<RectTransform, Vector2> targets = new Dictionary<RectTransform, Vector2>();
    Sprite bugSprite;
    int need, caught;
    float until, speed;
    bool running;
    Action<int, int> onDone;

    public bool Running => running;
    public int Caught => caught;

    void Start()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;   // 滑鼠點世界空間的 UI 要知道是哪台相機
        status.text = "";
    }

    // need：要抓幾隻；seconds：時限；speed：蟲跑多快（技術債越高越快）
    public void Begin(int need, float seconds, float speed, Action<int, int> done)
    {
        this.need = need;
        this.speed = speed;
        caught = 0;
        until = Time.time + seconds;
        onDone = done;
        running = true;
        if (bugSprite == null) bugSprite = MakeBugSprite();
        for (int i = 0; i < 4; i++) Spawn();
    }

    void Update()
    {
        if (!running) return;
        float left = until - Time.time;
        status.text = $"抓到 {caught} / {need}　　剩 {Mathf.CeilToInt(Mathf.Max(0, left))} 秒";
        foreach (RectTransform b in bugs)
        {
            Vector2 t = targets[b];
            b.anchoredPosition = Vector2.MoveTowards(b.anchoredPosition, t, speed * Time.deltaTime);
            Vector2 d = t - b.anchoredPosition;
            if (d.sqrMagnitude > 1) b.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90);
            else targets[b] = RandomPoint();
        }
        if (left <= 0) Finish();
    }

    // 給自動試玩：直接點掉一隻
    public void SquashOne()
    {
        if (running && bugs.Count > 0) Squash(bugs[0]);
    }

    void Spawn()
    {
        var g = new GameObject("Bug", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)g.transform;
        rt.SetParent(area, false);
        rt.sizeDelta = new Vector2(64, 64);
        rt.anchoredPosition = RandomPoint();
        g.GetComponent<Image>().sprite = bugSprite;
        g.GetComponent<Button>().onClick.AddListener(() => Squash(rt));
        bugs.Add(rt);
        targets[rt] = RandomPoint();
    }

    void Squash(RectTransform b)
    {
        bugs.Remove(b);
        targets.Remove(b);
        Destroy(b.gameObject);
        caught++;
        if (caught >= need) Finish();
        else Spawn();
    }

    void Finish()
    {
        if (!running) return;
        running = false;
        foreach (RectTransform b in bugs) Destroy(b.gameObject);
        bugs.Clear();
        targets.Clear();
        status.text = caught >= need ? $"全部抓完了！（{caught} 隻）" : $"時間到：抓到 {caught} / {need}";
        onDone?.Invoke(caught, need);
    }

    Vector2 RandomPoint()
    {
        Vector2 s = area.rect.size * 0.5f - new Vector2(40, 40);
        return new Vector2(UnityEngine.Random.Range(-s.x, s.x), UnityEngine.Random.Range(-s.y, s.y));
    }

    // 用程式畫一隻蟲：深紅色身體＋六隻腳＋兩根觸角（不用另外找圖）
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
