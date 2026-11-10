using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 搶救備份：硬碟快壞了，檔案一個個冒出來、幾秒後就消失；點下重要的檔案救起來，不要點到病毒
public class BackupGame : MonitorGame
{
    public override string Title => "搶救備份";

    static readonly string[] Good = { "訂單資料", "客戶名單", "菜單照片", "本月財報", "合約掃描", "員工通訊錄", "優惠券設定", "外送員名單" };
    static readonly string[] Bad = { "免費加速器.exe", "中獎通知.zip", "不明檔案.scr", "破解版.exe" };

    readonly List<(RectTransform r, float dies, bool good)> files = new List<(RectTransform, float, bool)>();
    int need, saved, mistakes;
    float nextSpawn;

    protected override void Setup()
    {
        files.Clear();
        need = 8 + difficulty * 2;
        saved = mistakes = 0;
        nextSpawn = Time.time;
        Label("硬碟快壞了！點重要的檔案救起來，別點到紅色的病毒", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
    }

    float Life => Mathf.Max(1.3f, 2.6f - difficulty * 0.35f);

    protected override string Goal() => $"救回 {saved} / {need}　點到病毒 {mistakes} 次";

    protected override void Tick()
    {
        if (Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + Mathf.Max(0.35f, 0.7f - difficulty * 0.08f);
            bool good = Random.value > 0.3f;
            string name = good ? Good[Random.Range(0, Good.Length)] : Bad[Random.Range(0, Bad.Length)];
            Vector2 pos = new Vector2(Random.Range(-360f, 360f), Random.Range(-160f, 130f));
            RectTransform r = Box(good ? new Color32(0xe8, 0xf1, 0xff, 255) : new Color32(0xff, 0xd9, 0xd4, 255), pos, new Vector2(170, 64));
            Label((good ? "■ " : "▲ ") + name, 20, good ? Ink : Red, Vector2.zero, new Vector2(160, 60), r).raycastTarget = false;
            var entry = (r, Time.time + Life, good);
            r.gameObject.AddComponent<Button>().onClick.AddListener(() => Click(r));
            files.Add(entry);
        }
        for (int i = files.Count - 1; i >= 0; i--)
            if (Time.time >= files[i].dies) { Destroy(files[i].r.gameObject); files.RemoveAt(i); }
    }

    void Click(RectTransform r)
    {
        if (!Running) return;
        int i = files.FindIndex(f => f.r == r);
        if (i < 0) return;
        if (files[i].good) saved++; else mistakes++;
        Destroy(r.gameObject);
        files.RemoveAt(i);
        if (saved >= need) Finish(mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1, $"救回 {saved} 個檔案，點到病毒 {mistakes} 次");
    }

    protected override void TimeUp()
    {
        float rate = (float)saved / need;
        Finish(rate >= 0.7f ? (mistakes <= 1 ? 2 : 1) : rate >= 0.4f ? 1 : 0, $"時間到：救回 {saved} / {need}，點到病毒 {mistakes} 次");
    }

    public override void AutoPlay()
    {
        var f = files.Find(x => x.good);
        if (f.r) Click(f.r);
    }
}
