using UnityEngine;
using UnityEngine.UI;

// 二進位：電腦只認得「開」和「關」。五顆燈泡分別代表 16、8、4、2、1，打開的加起來要等於目標數字
public class BinaryGame : MonitorGame
{
    public override string Title => "二進位燈泡";

    static readonly int[] Values = { 16, 8, 4, 2, 1 };
    readonly bool[] on = new bool[5];
    readonly Image[] bulbs = new Image[5];
    int target, rounds, done, clicks;
    Text targetText, sumText, bits;

    protected override void Setup()
    {
        rounds = 4 + difficulty;
        done = clicks = 0;
        Label("點燈泡開關：亮著的數字加起來，要等於目標", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        targetText = Label("", 34, Orange, new Vector2(0, 120), new Vector2(880, 50));
        for (int i = 0; i < 5; i++)
        {
            int k = i;
            RectTransform r = Box(Paper, new Vector2((i - 2) * 160, 0), new Vector2(130, 130));
            bulbs[i] = r.GetComponent<Image>();
            Label(Values[i].ToString(), 40, Ink, new Vector2(0, 10), new Vector2(120, 60), r).raycastTarget = false;
            r.gameObject.AddComponent<Button>().onClick.AddListener(() => Toggle(k));
        }
        bits = Label("", 30, Ink, new Vector2(0, -105), new Vector2(880, 40));
        sumText = Label("", 22, Ink, new Vector2(0, -150), new Vector2(880, 30));
        NewTarget();
    }

    void NewTarget()
    {
        for (int i = 0; i < 5; i++) on[i] = false;
        target = Random.Range(3, 32);
        targetText.text = $"目標：{target}";
        Paint();
    }

    void Toggle(int i)
    {
        if (!Running) return;
        clicks++;
        on[i] = !on[i];
        Paint();
        if (Sum() == target)
        {
            if (++done >= rounds) Finish(Stars(), $"{rounds} 個數字都做出來了！點了 {clicks} 下");
            else NewTarget();
        }
    }

    int Sum() { int s = 0; for (int i = 0; i < 5; i++) if (on[i]) s += Values[i]; return s; }

    // 最少要點幾下：每個數字的 1 的個數
    int Stars() => clicks <= rounds * 3 + 2 ? 3 : clicks <= rounds * 5 ? 2 : 1;

    void Paint()
    {
        string b = "";
        for (int i = 0; i < 5; i++) { bulbs[i].color = on[i] ? Gold : Paper; b += on[i] ? "1" : "0"; }
        bits.text = $"電腦看到的是：{b}";
        sumText.text = $"現在加起來 = {Sum()}";
    }

    protected override string Goal() => $"完成 {done} / {rounds} 個數字";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(done * 2 >= rounds ? 1 : 0, $"時間到：做出 {done} / {rounds} 個");

    public override void AutoPlay()
    {
        for (int i = 0; i < 5; i++)
            if (on[i] != ((target & Values[i]) != 0)) { Toggle(i); return; }
    }
}
