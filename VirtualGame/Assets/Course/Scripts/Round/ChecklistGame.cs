using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 上線前檢查：步驟按鈕被打亂了，照正確的順序一個一個點
// 備份 → 請人檢查 → 跑測試 → 通知大家 → 上線 → 觀察；點錯順序會扣分
public class ChecklistGame : MonitorGame
{
    public override string Title => "上線前檢查";

    static readonly string[] Steps = { "備份資料庫", "請同事檢查", "跑自動測試", "通知客服和業務", "按下上線", "盯著錯誤通知" };

    List<string> steps;
    readonly List<Button> buttons = new List<Button>();
    int next, mistakes;
    Text done;

    protected override void Setup()
    {
        int count = Mathf.Min(Steps.Length, 4 + difficulty);
        // 難度低時拿掉「請同事檢查」和「盯著錯誤通知」，只留最基本的
        steps = new List<string>(Steps);
        if (count < 6) steps.Remove("盯著錯誤通知");
        if (count < 5) steps.Remove("請同事檢查");
        next = mistakes = 0;
        buttons.Clear();
        Label("上線前要照順序做：哪一步先？點出正確的順序", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        done = Label("", 22, Green, new Vector2(0, -170), new Vector2(880, 34));
        var shuffled = new List<string>(steps);
        CampaignDirector.Shuffle(shuffled, new System.Random());
        for (int i = 0; i < shuffled.Count; i++)
        {
            string s = shuffled[i];
            float x = (i % 3 - 1) * 290, y = 90 - (i / 3) * 100;
            Button b = Btn(s, Paper, Ink, new Vector2(x, y), new Vector2(270, 80), () => Click(s));
            b.name = s;
            buttons.Add(b);
        }
    }

    void Click(string s)
    {
        if (!Running) return;
        if (s == steps[next])
        {
            Button b = buttons.Find(x => x.name == s);
            b.interactable = false;
            b.GetComponent<Image>().color = new Color32(0xb8, 0xec, 0xc9, 255);
            next++;
            done.text = string.Join(" → ", steps.GetRange(0, next));
            if (next >= steps.Count) Finish(mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1, $"照順序做完了！點錯 {mistakes} 次");
        }
        else mistakes++;
    }

    protected override string Goal() => $"完成 {next} / {steps.Count} 步　點錯 {mistakes} 次";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(next >= steps.Count - 1 ? 1 : 0, $"時間到：做到第 {next} 步");
    public override void AutoPlay() => Click(steps[next]);
}
