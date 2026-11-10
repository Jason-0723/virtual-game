using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Slack 訊息轟炸：右上角每隔一段時間跳出一張訊息卡片，配「叮」一聲，幾秒後自己消失
// 最多同時 3 張，排隊往下推；每則訊息壓力 +1（被打斷也是壓力）
public class SlackFeed : MonoBehaviour
{
    public RectTransform cardTemplate;      // 隱藏的範本卡片，複製出來用
    public Vector2 interval = new Vector2(25, 50);   // 每隔幾秒（真實時間）來一則
    public float showSeconds = 6f;

    static readonly string[] Messages =
    {
        "PM：那個按鈕可以再大一點嗎？",
        "業務：客戶說畫面怪怪的，但說不出哪裡怪",
        "主管：@channel 下午三點臨時會議",
        "同事：有人動了測試資料庫嗎 😱",
        "HR：記得填這週的工時表喔～",
        "PM：老闆看了 demo，有一些小想法",
        "系統通知：伺服器 CPU 使用率 92%",
        "同事：可以幫我看一下這個錯誤嗎？很快",
        "業務：今天可以先給客戶一個版本嗎 🙏",
        "主管：明天早上十點前要交週報",
    };

    readonly List<(RectTransform card, float until)> live = new List<(RectTransform, float)>();
    AudioSource ding;
    float next;

    void Start()
    {
        cardTemplate.gameObject.SetActive(false);
        ding = gameObject.AddComponent<AudioSource>();
        ding.clip = ProceduralAudio.Ding();
        ding.volume = 0.5f;
        next = Time.time + Random.Range(interval.x, interval.y);
    }

    void Update()
    {
        DayManager day = DayManager.Instance;
        bool paused = day == null || day.Ended || day.WorldPaused;
        if (paused) { next += Time.deltaTime; return; }   // 對話時不打擾，也不讓訊息過期

        if (Time.time >= next)
        {
            next = Time.time + Random.Range(interval.x, interval.y);
            Push(Messages[Random.Range(0, Messages.Length)]);
            day.AddStress(1);
        }
        for (int i = live.Count - 1; i >= 0; i--)
            if (Time.time > live[i].until) { Destroy(live[i].card.gameObject); live.RemoveAt(i); }
        Layout();
    }

    public void Push(string text)
    {
        RectTransform card = Instantiate(cardTemplate, cardTemplate.parent);
        card.gameObject.SetActive(true);
        card.Find("Text").GetComponent<Text>().text = text;
        card.anchoredPosition = cardTemplate.anchoredPosition + new Vector2(400, 0);   // 從右邊滑進來
        live.Insert(0, (card, Time.time + showSeconds));
        if (live.Count > 3) { Destroy(live[3].card.gameObject); live.RemoveAt(3); }
        ding.Play();
    }

    // 新的在最上面；每張往目標位置滑過去
    void Layout()
    {
        for (int i = 0; i < live.Count; i++)
        {
            Vector2 target = cardTemplate.anchoredPosition - new Vector2(0, i * (cardTemplate.sizeDelta.y + 12));
            live[i].card.anchoredPosition = Vector2.Lerp(live[i].card.anchoredPosition, target, Time.deltaTime * 10);
        }
    }
}
