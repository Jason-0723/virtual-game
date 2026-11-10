using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

// 分類小遊戲：卡片一張張出現，在時限內選對分類（兩種玩法共用）
//  訊息分類：Slack 訊息 → 馬上處理／晚點再說
//  釣魚信辨識：電子郵件 → 安全／可疑
// 難度越高，每張卡片能想的時間越短、張數越多
public class SortGame : MonitorGame
{
    public bool phishing;   // false＝訊息分類、true＝釣魚信
    public override string Title => phishing ? "釣魚信大考驗" : "訊息分類";

    struct Card { public string text; public int answer; }

    static readonly Card[] Messages =
    {
        new Card { text = "客服：網站打不開了！！客人都在罵", answer = 0 },
        new Card { text = "主管：付款頁面有人被扣款兩次", answer = 0 },
        new Card { text = "監控：伺服器硬碟用量 98%，快滿了", answer = 0 },
        new Card { text = "設計師：這個按鈕可以改成藍色嗎？", answer = 1 },
        new Card { text = "PM：明天站會前把進度填一下", answer = 1 },
        new Card { text = "同事：你上午說的那個錯誤，修好了嗎？", answer = 1 },
        new Card { text = "福委：下週團建想吃燒肉還是火鍋？", answer = 1 },
        new Card { text = "群組：（有人傳了一張貓的照片）", answer = 1 },
        new Card { text = "業務：轉傳「震驚！工程師都在用的十個祕訣」", answer = 1 },
        new Card { text = "客服：結帳金額算錯，客人多付了錢", answer = 0 },
        new Card { text = "HR：記得這週五前填工時表", answer = 1 },
        new Card { text = "同事：茶水間的咖啡機修好了 ☕", answer = 1 },
    };

    static readonly Card[] Mails =
    {
        new Card { text = "it@fanfan.com.tw\n本週五晚上系統維護通知", answer = 0 },
        new Card { text = "hr@fanfan-bonus.xyz\n【急】年終獎金領取名單.exe", answer = 1 },
        new Card { text = "pm@fanfan.com.tw\n明天的會議改到下午兩點", answer = 0 },
        new Card { text = "security@g00gle-login.ru\n您的帳號即將停用，請立即輸入密碼", answer = 1 },
        new Card { text = "boss@fanfan.com.tw\n下週一早上的週報範本", answer = 0 },
        new Card { text = "ceo.fanfan@gmail.com\n我在開會，幫我買 10 張禮物卡，急", answer = 1 },
        new Card { text = "billing@aws-invoice-pay.top\n帳單逾期，點此付款以免停機", answer = 1 },
        new Card { text = "design@fanfan.com.tw\n新版首頁設計稿（雲端連結）", answer = 0 },
        new Card { text = "delivery@fanfan-shiping.cc\n您的包裹無法投遞，請下載附件", answer = 1 },
    };

    List<Card> deck;
    Text cardText;
    int index, correct;
    float cardUntil;

    string[] Choices => phishing ? new[] { "安全", "可疑" } : new[] { "馬上處理", "晚點再說" };

    protected override void Setup()
    {
        deck = new List<Card>(phishing ? Mails : Messages);
        for (int i = deck.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (deck[i], deck[j]) = (deck[j], deck[i]); }
        int count = Mathf.Min(deck.Count, 6 + difficulty);
        deck.RemoveRange(count, deck.Count - count);
        index = correct = 0;

        Label(phishing ? "這封信安全嗎？看寄件人的網址和內容" : "這則訊息要怎麼處理？", 22, Ink, new Vector2(0, 165), new Vector2(880, 34));
        RectTransform card = Box(Color.white, new Vector2(0, 50), new Vector2(760, 170));
        cardText = Label("", 30, Ink, Vector2.zero, new Vector2(720, 160), card);
        string[] ch = Choices;
        Color[] colors = phishing ? new[] { Green, Red } : new[] { Red, Blue };
        for (int i = 0; i < ch.Length; i++)
        {
            int k = i;
            float x = (i - (ch.Length - 1) / 2f) * 270;
            Btn(ch[i], colors[i], Color.white, new Vector2(x, -130), new Vector2(240, 70), () => Answer(k));
        }
        Show();
    }

    float PerCard => Mathf.Max(2.2f, 5f - difficulty * 0.6f);

    void Show()
    {
        cardText.text = deck[index].text;
        cardUntil = Time.time + PerCard;
        until = Time.time + PerCard * (deck.Count - index);   // 總時間跟著剩下的張數走
    }

    void Answer(int k)
    {
        if (!Running) return;
        if (k == deck[index].answer) correct++;
        index++;
        if (index >= deck.Count) { Done(); return; }
        Show();
    }

    void Done()
    {
        float r = (float)correct / deck.Count;
        Finish(r >= 0.95f ? 3 : r >= 0.75f ? 2 : r >= 0.5f ? 1 : 0, $"答對 {correct} / {deck.Count}");
    }

    protected override string Goal() => $"第 {Mathf.Min(index + 1, deck.Count)} / {deck.Count} 則　答對 {correct}";
    protected override void Tick() { if (Time.time >= cardUntil) Answer(-1); }   // 想太久＝沒處理
    protected override void TimeUp() => Done();
    public override void AutoPlay() => Answer(deck[index].answer);
}
