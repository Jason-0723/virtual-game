using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 變數：一個貼了名字的盒子，裡面的數字會被一行一行的程式改掉
// 看完程式，選出盒子裡最後的數字
public class VariableGame : MonitorGame
{
    public override string Title => "變數是什麼";

    int rounds, index, correct, answer;
    readonly List<Button> buttons = new List<Button>();
    Text code, hint;

    protected override void Setup()
    {
        rounds = 4 + difficulty;
        index = correct = 0;
        Label("「訂單數」是一個盒子。程式從上往下跑完，盒子裡是多少？", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        Box(new Color32(0x2b, 0x21, 0x18, 255), new Vector2(-170, 40), new Vector2(480, 230));
        code = Label("", 26, new Color32(0xb8, 0xec, 0xc9, 255), new Vector2(-170, 40), new Vector2(440, 210), null, TextAnchor.MiddleLeft);
        hint = Label("", 20, Ink, new Vector2(0, -160), new Vector2(880, 30));
        Next();
    }

    void Next()
    {
        foreach (Button b in buttons) Destroy(b.gameObject);
        buttons.Clear();
        int v = Random.Range(1, 6);
        var lines = new List<string> { $"訂單數 = {v}" };
        for (int i = 0; i < 2 + Mathf.Min(2, difficulty); i++)
        {
            int k = Random.Range(1, 5);
            switch (Random.Range(0, 3))
            {
                case 0: v += k; lines.Add($"訂單數 = 訂單數 + {k}"); break;
                case 1: v *= 2; lines.Add("訂單數 = 訂單數 × 2"); break;
                default: k = Mathf.Min(k, v); v -= k; lines.Add($"訂單數 = 訂單數 - {k}"); break;
            }
        }
        answer = v;
        code.text = string.Join("\n", lines);
        var opts = new List<int> { v, v + Random.Range(1, 4), Mathf.Max(0, v - Random.Range(1, 4)) };
        if (opts[2] == v) opts[2] = v + 4;
        CampaignDirector.Shuffle(opts, new System.Random());
        for (int i = 0; i < 3; i++)
        {
            int o = opts[i];
            Button b = Btn(o.ToString(), Paper, Ink, new Vector2(270, 110 - i * 75), new Vector2(200, 62), () => Answer(o));
            b.GetComponentInChildren<Text>().fontSize = 32;
            buttons.Add(b);
        }
    }

    void Answer(int o)
    {
        if (!Running) return;
        if (o == answer) correct++;
        hint.text = o == answer ? "對！變數就是會被改來改去的盒子。" : $"不對，最後是 {answer}";
        if (++index >= rounds) Done(); else Next();
    }

    void Done()
    {
        float r = (float)correct / rounds;
        Finish(r >= 0.95f ? 3 : r >= 0.7f ? 2 : r >= 0.4f ? 1 : 0, $"答對 {correct} / {rounds} 題");
    }

    protected override string Goal() => $"第 {Mathf.Min(index + 1, rounds)} / {rounds} 題　答對 {correct}";
    protected override void Tick() { }
    protected override void TimeUp() => Done();
    public override void AutoPlay() => Answer(answer);
}
