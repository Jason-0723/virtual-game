using System.Collections.Generic;
using UnityEngine;

// 設定強密碼：每回合三個密碼，選最安全的那一個
// 安全的密碼：夠長、混合不同字元、不用生日名字這種別人猜得到的東西
public class PasswordGame : MonitorGame
{
    public override string Title => "設定強密碼";

    // 每回合：最強的放第一個（顯示時會打亂）
    static readonly string[][] Rounds =
    {
        new[] { "Blue-Rice-Panda-42!", "123456", "password" },
        new[] { "雞排飯要加辣-每天3份!", "jason0723", "fanfan2026" },
        new[] { "t7#Kq!9vLm2$", "qwerty123", "Fanfan2026" },
        new[] { "Moon_Taxi_88_Coffee?", "iloveyou", "Jason!0723" },
        new[] { "Rq8!mZ2#pW5&", "00000000", "abc12345" },
        new[] { "Tiger*Noodle*Rain*7", "admin", "fanfan_admin" },
        new[] { "週五不要上線!!2026", "11111111", "boss1234" },
    };

    List<string[]> rounds;
    readonly List<(UnityEngine.UI.Button b, bool right)> buttons = new List<(UnityEngine.UI.Button, bool)>();
    UnityEngine.UI.Text hint;
    int index, correct;

    protected override void Setup()
    {
        rounds = new List<string[]>(Rounds);
        CampaignDirector.Shuffle(rounds, new System.Random());
        int count = Mathf.Min(rounds.Count, 5 + difficulty);
        rounds.RemoveRange(count, rounds.Count - count);
        index = correct = 0;
        Label("哪一個密碼最安全？（夠長、混合字元、別人猜不到）", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        hint = Label("", 20, Ink, new Vector2(0, -170), new Vector2(880, 30));
        Show();
    }

    void Show()
    {
        foreach (var (b, _) in buttons) Destroy(b.gameObject);
        buttons.Clear();
        var order = new List<int> { 0, 1, 2 };
        CampaignDirector.Shuffle(order, new System.Random());
        for (int i = 0; i < 3; i++)
        {
            int k = order[i];
            var b = Btn(rounds[index][k], Paper, Ink, new Vector2(0, 90 - i * 90), new Vector2(620, 72), () => Answer(k == 0));
            b.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 28;
            buttons.Add((b, k == 0));
        }
    }

    void Answer(bool right)
    {
        if (!Running) return;
        if (right) correct++;
        hint.text = right ? "對！越長越亂越安全。" : $"不對，最安全的是「{rounds[index][0]}」";
        index++;
        if (index >= rounds.Count) { Done(); return; }
        Show();
    }

    void Done()
    {
        float r = (float)correct / rounds.Count;
        Finish(r >= 0.95f ? 3 : r >= 0.7f ? 2 : r >= 0.4f ? 1 : 0, $"選對 {correct} / {rounds.Count} 題");
    }

    protected override string Goal() => $"第 {Mathf.Min(index + 1, rounds.Count)} / {rounds.Count} 題　選對 {correct}";
    protected override void Tick() { }
    protected override void TimeUp() => Done();
    public override void AutoPlay() { var x = buttons.Find(b => b.right); if (x.b) x.b.onClick.Invoke(); }
}
