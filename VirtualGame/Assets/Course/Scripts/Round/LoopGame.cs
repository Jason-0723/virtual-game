using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 迴圈：不用一格一格畫，選「要重複的那一段」和「重複幾次」，按執行就畫出整排
public class LoopGame : MonitorGame
{
    public override string Title => "用迴圈畫圖案";

    static readonly string[] Bodies = { "■□", "●●▲", "★", "▲■■", "◆○", "■▲●" };
    int rounds, index, mistakes, count = 1;
    string target, answerBody, body;
    int answerCount;
    readonly List<Button> bodyButtons = new List<Button>();
    Text targetText, preview, codeText, hint;

    protected override void Setup()
    {
        rounds = 3 + difficulty / 2;
        index = mistakes = 0;
        Label("目標排好的餐點，用「重複 N 次」一次做出來", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        targetText = Label("", 30, Ink, new Vector2(0, 125), new Vector2(880, 44));
        Label("重複的內容：", 20, Ink, new Vector2(-330, 60), new Vector2(200, 30));
        Label("重複幾次：", 20, Ink, new Vector2(-330, -15), new Vector2(200, 30));
        for (int n = 1; n <= 6; n++)
        {
            int k = n;
            Btn(n.ToString(), Paper, Ink, new Vector2(-180 + (n - 1) * 70, -15), new Vector2(60, 50), () => { count = k; Show(); });
        }
        codeText = Label("", 22, Blue, new Vector2(0, -70), new Vector2(880, 30));
        preview = Label("", 26, Ink, new Vector2(-60, -120), new Vector2(700, 40));
        Btn("執行 ▶", Green, Color.white, new Vector2(370, -120), new Vector2(120, 50), Run);
        hint = Label("", 20, Red, new Vector2(0, -165), new Vector2(880, 28));
        Next();
    }

    void Next()
    {
        foreach (Button b in bodyButtons) Destroy(b.gameObject);
        bodyButtons.Clear();
        var pool = new List<string>(Bodies);
        CampaignDirector.Shuffle(pool, new System.Random());
        answerBody = pool[0];
        answerCount = Random.Range(3, 6 + Mathf.Min(1, difficulty));
        target = "";
        for (int i = 0; i < answerCount; i++) target += answerBody;
        targetText.text = "目標：" + target;
        var opts = pool.GetRange(0, 3);
        CampaignDirector.Shuffle(opts, new System.Random());
        for (int i = 0; i < 3; i++)
        {
            string o = opts[i];
            bodyButtons.Add(Btn(o, Paper, Ink, new Vector2(-150 + i * 180, 60), new Vector2(160, 50), () => { body = o; Show(); }));
        }
        body = null; count = 1;
        Show();
    }

    string Make() { string s = ""; for (int i = 0; i < count; i++) s += body; return s; }

    void Show()
    {
        codeText.text = $"重複 {count} 次：{{ 放上 {body ?? "？"} }}";
        preview.text = body == null ? "" : "會做出：" + Make();
    }

    void Run()
    {
        if (!Running || body == null) return;
        if (Make() != target) { mistakes++; hint.text = "跟目標不一樣，再調整看看"; return; }
        hint.text = "";
        if (++index >= rounds) Finish(mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1, $"{rounds} 個圖案都做出來了，錯了 {mistakes} 次");
        else Next();
    }

    protected override string Goal() => $"第 {Mathf.Min(index + 1, rounds)} / {rounds} 個圖案　錯 {mistakes} 次";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(index * 2 >= rounds ? 1 : 0, $"時間到：做出 {index} / {rounds} 個");
    public override void AutoPlay() { body = answerBody; count = answerCount; Run(); }
}
