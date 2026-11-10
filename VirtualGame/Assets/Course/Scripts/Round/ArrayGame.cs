using UnityEngine;
using UnityEngine.UI;

// 陣列：一排編了號的置物櫃，編號從 0 開始！「外送單[3]」是第 4 格
// 前兩題櫃子下面有編號，之後編號藏起來，要自己從 0 數
public class ArrayGame : MonitorGame
{
    public override string Title => "陣列從 0 開始數";

    const int N = 8;
    int rounds, index, correct, mistakes, ask;
    readonly Text[] numbers = new Text[N];
    Text question, hint;

    protected override void Setup()
    {
        rounds = 6 + difficulty;
        index = correct = mistakes = 0;
        Label("「外送單」是一排置物櫃，編號從 0 開始。點出題目要的那一格", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        question = Label("", 34, Orange, new Vector2(0, 115), new Vector2(880, 50));
        for (int i = 0; i < N; i++)
        {
            int k = i;
            RectTransform r = Box(Paper, new Vector2((i - 3.5f) * 104, 0), new Vector2(92, 120));
            Label("櫃", 34, Ink, new Vector2(0, 10), new Vector2(90, 60), r).raycastTarget = false;
            r.gameObject.AddComponent<Button>().onClick.AddListener(() => Click(k));
            numbers[i] = Label(i.ToString(), 26, Blue, new Vector2((i - 3.5f) * 104, -85), new Vector2(90, 34));
        }
        hint = Label("", 20, Ink, new Vector2(0, -150), new Vector2(880, 30));
        Next();
    }

    void Next()
    {
        ask = Random.Range(0, N);
        question.text = $"拿出 外送單[{ask}]";
        foreach (Text t in numbers) t.enabled = index < 2;
    }

    void Click(int k)
    {
        if (!Running) return;
        if (k == ask) { correct++; hint.text = $"對！[{ask}] 是第 {ask + 1} 格。"; }
        else { mistakes++; hint.text = $"不對，[{ask}] 是從 0 數起的第 {ask + 1} 格"; }
        if (++index >= rounds) Done(); else Next();
    }

    void Done() => Finish(mistakes == 0 ? 3 : mistakes <= 2 ? 2 : correct > 0 ? 1 : 0, $"答對 {correct} / {rounds} 題");

    protected override string Goal() => $"第 {Mathf.Min(index + 1, rounds)} / {rounds} 題　答對 {correct}";
    protected override void Tick() { }
    protected override void TimeUp() => Done();
    public override void AutoPlay() => Click(ask);
}
