using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 加密（凱薩密碼）：每個英文字母都往後移了幾格。轉動密碼輪，把字移回來，看出原本的單字就送出
public class CipherGame : MonitorGame
{
    public override string Title => "破解密碼訊息";

    static readonly string[] Words = { "PIZZA", "COFFEE", "BURGER", "NOODLE", "SUSHI", "PASTA", "SALAD", "CAKE", "TACO", "RAMEN" };
    int rounds, index, mistakes, key, shift;
    string word, secret;
    Text secretText, decoded, shiftText, hint;

    protected override void Setup()
    {
        rounds = 3 + Mathf.Min(2, difficulty);
        index = mistakes = 0;
        Label("訊息被加密了：每個字母都往後移了幾格。按 ◀ ▶ 移回來，看懂了就送出", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        Label("收到的密文", 20, Ink, new Vector2(0, 125), new Vector2(300, 30));
        secretText = Label("", 44, Red, new Vector2(0, 80), new Vector2(880, 60));
        Btn("◀", Paper, Ink, new Vector2(-260, -5), new Vector2(90, 70), () => Turn(-1)).GetComponentInChildren<Text>().fontSize = 34;
        Btn("▶", Paper, Ink, new Vector2(260, -5), new Vector2(90, 70), () => Turn(1)).GetComponentInChildren<Text>().fontSize = 34;
        decoded = Label("", 48, Green, new Vector2(0, -5), new Vector2(400, 70));
        shiftText = Label("", 20, Ink, new Vector2(0, -60), new Vector2(880, 30));
        Btn("就是這個字！", Gold, Ink, new Vector2(0, -115), new Vector2(240, 56), Submit);
        hint = Label("", 20, Red, new Vector2(0, -165), new Vector2(880, 28));
        Next();
    }

    static string Shift(string s, int k)
    {
        var c = s.ToCharArray();
        for (int i = 0; i < c.Length; i++) c[i] = (char)('A' + ((c[i] - 'A' + k) % 26 + 26) % 26);
        return new string(c);
    }

    void Next()
    {
        word = Words[Random.Range(0, Words.Length)];
        key = Random.Range(2, 6 + difficulty * 3);
        secret = Shift(word, key);
        secretText.text = secret;
        shift = 0;
        Show();
    }

    void Show()
    {
        decoded.text = Shift(secret, -shift);
        shiftText.text = $"往回移了 {shift} 格";
    }

    void Turn(int d)
    {
        if (!Running) return;
        shift = (shift + d + 26) % 26;
        Show();
    }

    void Submit()
    {
        if (!Running) return;
        if (Shift(secret, -shift) != word) { mistakes++; hint.text = "這還不是一個看得懂的字"; return; }
        hint.text = $"破解了！密鑰是 {key}";
        if (++index >= rounds) Finish(mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1, $"破解 {rounds} 則訊息，猜錯 {mistakes} 次");
        else Next();
    }

    protected override string Goal() => $"第 {Mathf.Min(index + 1, rounds)} / {rounds} 則　猜錯 {mistakes} 次";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(index * 2 >= rounds ? 1 : 0, $"時間到：破解 {index} / {rounds} 則");
    public override void AutoPlay() { if (shift != key) Turn(1); else Submit(); }
}
