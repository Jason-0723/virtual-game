using System;
using UnityEngine;
using UnityEngine.UI;

// 座位電腦螢幕上的小遊戲共用的骨架：每一種小遊戲都繼承它
// 畫面畫在螢幕上的 World Space Canvas 裡（area），螢幕下方一行 status 顯示目標和剩餘時間
// difficulty：0 最簡單；天數越後面、技術債越高就越大
public abstract class MonitorGame : MonoBehaviour
{
    public RectTransform area;
    public Text status;

    protected int difficulty;
    protected float until;
    Action<int, string> onDone;
    bool running;

    public bool Running => running;
    public abstract string Title { get; }

    public void Begin(int difficulty, float seconds, Action<int, string> done)
    {
        Clear();
        this.difficulty = difficulty;
        until = Time.time + seconds;
        onDone = done;
        running = true;
        Setup();
    }

    protected abstract void Setup();
    protected abstract string Goal();        // 螢幕下方顯示的目標，例如「抓到 3 / 10」
    protected abstract void Tick();          // 每一格要做的事（移動、生成⋯）
    protected abstract void TimeUp();        // 時間到了怎麼算星星
    public abstract void AutoPlay();         // 給自動試玩：做一個正確的動作

    protected float Left => until - Time.time;

    void Update()
    {
        if (!running) return;
        status.text = $"{Goal()}　　剩 {Mathf.CeilToInt(Mathf.Max(0, Left))} 秒";
        Tick();
        if (running && Left <= 0) TimeUp();
    }

    // stars：0～3；summary：結果卡上的一句話
    protected void Finish(int stars, string summary)
    {
        if (!running) return;
        running = false;
        Clear();
        status.text = summary;
        onDone?.Invoke(stars, summary);
    }

    void Clear()
    {
        foreach (Transform c in area) Destroy(c.gameObject);
    }

    // ───── 畫面小工具（座標是螢幕上的像素，中心為 0,0；螢幕可用範圍約 900 × 400）─────
    protected Font F => DayManager.UiFont;

    protected RectTransform Box(Color c, Vector2 pos, Vector2 size, Transform parent = null) =>
        Ui.Rect("Box", parent ? parent : area, c, new Vector2(0.5f, 0.5f), pos, size);

    protected Text Label(string text, int size, Color c, Vector2 pos, Vector2 box, Transform parent = null, TextAnchor align = TextAnchor.MiddleCenter) =>
        Ui.Label(parent ? parent : area, text, size, c, new Vector2(0.5f, 0.5f), pos, box, F, align);

    protected Button Btn(string text, Color bg, Color fg, Vector2 pos, Vector2 size, Action click, Transform parent = null)
    {
        Button b = Ui.Button(parent ? parent : area, text, bg, fg, pos, size, F, () => click());
        b.GetComponentInChildren<Text>().fontSize = 24;
        return b;
    }

    protected static readonly Color Ink = new Color32(0x23, 0x21, 0x1d, 255), Paper = new Color32(0xf0, 0xed, 0xe6, 255),
        Orange = new Color32(0xef, 0x6c, 0x33, 255), Green = new Color32(0x2f, 0xae, 0x66, 255), Red = new Color32(0xe2, 0x45, 0x3c, 255),
        Blue = new Color32(0x2f, 0x7f, 0xd6, 255), Gold = new Color32(0xf2, 0xb3, 0x3d, 255);
}
