using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 訂單分流：晚餐尖峰，訂單一直湧進來；點一台伺服器，把排在最前面的訂單交給它
// 伺服器會慢慢消化負載，任何一台滿了就當機；排隊的訂單太多也算當機。撐到時間結束就過關
public class LoadGame : MonitorGame
{
    public override string Title => "晚餐尖峰分流";

    static readonly (string name, float load)[] Kinds = { ("便當", 18), ("披薩", 30), ("飲料", 10), ("團購", 45) };
    readonly List<(string name, float load)> queue = new List<(string, float)>();
    float[] loads;
    Image[] bars;
    Text queueText;
    float nextSpawn;
    int served;

    protected override void Setup()
    {
        queue.Clear(); served = 0;
        int n = 3;
        loads = new float[n];
        bars = new Image[n];
        Label("點一台伺服器，把最左邊的訂單交給它。任何一台滿了，App 就當機！", 20, Ink, new Vector2(0, 175), new Vector2(880, 30));
        RectTransform q = Box(new Color32(0xff, 0xf3, 0xe6, 255), new Vector2(0, 120), new Vector2(860, 56));
        queueText = Label("", 24, Ink, Vector2.zero, new Vector2(840, 50), q, TextAnchor.MiddleLeft);
        for (int i = 0; i < n; i++)
        {
            int k = i;
            float x = (i - 1) * 280;
            Button b = Btn($"伺服器 {i + 1}", Paper, Ink, new Vector2(x, -150), new Vector2(240, 60), () => Assign(k));
            RectTransform bg = Box(new Color32(0xdd, 0xd5, 0xc4, 255), new Vector2(x, -10), new Vector2(80, 200));
            Image fill = Box(Green, new Vector2(0, -100), new Vector2(80, 0), bg).GetComponent<Image>();
            fill.rectTransform.pivot = new Vector2(0.5f, 0);
            fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(0.5f, 0);
            fill.rectTransform.anchoredPosition = Vector2.zero;
            bars[i] = fill;
        }
        nextSpawn = Time.time;
    }

    protected override string Goal() => $"撐住尖峰！已處理 {served} 張訂單";

    protected override void Tick()
    {
        float every = Mathf.Max(0.35f, 0.85f - difficulty * 0.12f);
        if (Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + every;
            queue.Add(Kinds[Random.Range(0, Kinds.Length)]);
            if (queue.Count > 6) { Finish(served >= 12 ? 1 : 0, "訂單堆太多沒人處理，App 卡住了"); return; }
        }
        for (int i = 0; i < loads.Length; i++)
        {
            loads[i] = Mathf.Max(0, loads[i] - 20 * Time.deltaTime);
            bars[i].rectTransform.sizeDelta = new Vector2(80, 200 * Mathf.Min(1, loads[i] / 100));
            bars[i].color = loads[i] > 75 ? Red : loads[i] > 50 ? Gold : Green;
        }
        var names = new System.Text.StringBuilder();
        for (int i = 0; i < queue.Count; i++) names.Append(i == 0 ? $"【{queue[i].name}】 " : queue[i].name + " ");
        queueText.text = queue.Count > 0 ? names.ToString() : "等待訂單⋯";
    }

    void Assign(int i)
    {
        if (!Running || queue.Count == 0) return;
        loads[i] += queue[0].load;
        queue.RemoveAt(0);
        served++;
        if (loads[i] >= 100) Finish(served >= 15 ? 1 : 0, $"伺服器 {i + 1} 塞爆了，App 當機");
    }

    protected override void TimeUp() => Finish(served >= 20 ? 3 : 2, $"撐過尖峰！處理了 {served} 張訂單");

    public override void AutoPlay()
    {
        if (queue.Count == 0) return;
        int best = 0;
        for (int i = 1; i < loads.Length; i++) if (loads[i] < loads[best]) best = i;
        if (loads[best] + queue[0].load < 100) Assign(best);
    }
}
