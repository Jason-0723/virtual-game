using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 擋下假流量：一排請求從左邊流向右邊的伺服器；點掉紅色的「攻擊」，讓綠色的真客人通過
// 攻擊流到伺服器 → 負載上升；點到真客人 → 客人被擋掉。伺服器負載滿了就掛，撐到時間結束就過關
public class FirewallGame : MonitorGame
{
    public override string Title => "擋下假流量";

    readonly List<(RectTransform r, bool attack)> packets = new List<(RectTransform, bool)>();
    float load, nextSpawn;
    int blocked, wronged;
    Image loadBar;

    protected override void Setup()
    {
        packets.Clear();
        load = 0; blocked = wronged = 0;
        nextSpawn = Time.time;
        Label("點掉紅色的攻擊，讓綠色的真客人通過！", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        Box(new Color32(0x2b, 0x21, 0x18, 255), new Vector2(400, 0), new Vector2(60, 300));
        Label("伺\n服\n器", 22, Color.white, new Vector2(400, 0), new Vector2(50, 200));
        RectTransform bg = Box(new Color32(0xdd, 0xd5, 0xc4, 255), new Vector2(0, -170), new Vector2(600, 18));
        loadBar = Box(Green, new Vector2(-300, 0), new Vector2(0, 18), bg).GetComponent<Image>();
        loadBar.rectTransform.pivot = new Vector2(0, 0.5f);
    }

    protected override string Goal() => $"擋下 {blocked} 個攻擊　誤擋客人 {wronged}　伺服器負載 {Mathf.RoundToInt(load)}%";

    protected override void Tick()
    {
        if (Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + Mathf.Max(0.3f, 0.65f - difficulty * 0.08f);
            bool attack = Random.value < 0.55f;
            float y = Random.Range(-120f, 130f);
            RectTransform r = Box(attack ? Red : Green, new Vector2(-430, y), new Vector2(150, 44));
            Label(attack ? "攻擊！" : "客人訂單", 20, Color.white, Vector2.zero, new Vector2(140, 40), r).raycastTarget = false;
            r.gameObject.AddComponent<Button>().onClick.AddListener(() => Click(r));
            packets.Add((r, attack));
        }
        float speed = 170 + difficulty * 40;
        for (int i = packets.Count - 1; i >= 0; i--)
        {
            RectTransform r = packets[i].r;
            r.anchoredPosition += new Vector2(speed * Time.deltaTime, 0);
            if (r.anchoredPosition.x > 340)
            {
                if (packets[i].attack) load += 14;
                Destroy(r.gameObject);
                packets.RemoveAt(i);
            }
        }
        load = Mathf.Max(0, load - 4 * Time.deltaTime);
        loadBar.rectTransform.sizeDelta = new Vector2(6 * Mathf.Min(100, load), 18);
        loadBar.color = load > 70 ? Red : load > 40 ? Gold : Green;
        if (load >= 100) Finish(0, "攻擊塞爆了伺服器，網站掛了");
    }

    void Click(RectTransform r)
    {
        if (!Running) return;
        int i = packets.FindIndex(p => p.r == r);
        if (i < 0) return;
        if (packets[i].attack) blocked++; else wronged++;
        Destroy(r.gameObject);
        packets.RemoveAt(i);
    }

    protected override void TimeUp() =>
        Finish(wronged <= 1 && load < 50 ? 3 : wronged <= 4 ? 2 : 1, $"撐住了！擋下 {blocked} 個攻擊，誤擋 {wronged} 位客人");

    public override void AutoPlay()
    {
        var p = packets.Find(x => x.attack);
        if (p.r) Click(p.r);
    }
}
