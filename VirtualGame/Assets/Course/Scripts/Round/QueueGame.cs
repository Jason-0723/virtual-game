using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 佇列（Queue）：先來的先服務。客人身上有號碼牌，散在店裡；每次都要點號碼最小（最早來）的那位
public class QueueGame : MonitorGame
{
    public override string Title => "先來先服務（佇列）";

    readonly List<(int no, Button b)> waiting = new List<(int, Button)>();
    int nextNo, served, mistakes, need;
    float nextSpawn;
    Text hint;

    protected override void Setup()
    {
        waiting.Clear();
        nextNo = 1; served = mistakes = 0;
        need = 10 + difficulty * 2;
        nextSpawn = Time.time;
        Label("點號碼牌最小的客人：最早來的要先服務", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        hint = Label("", 20, Red, new Vector2(0, -165), new Vector2(880, 28));
        for (int i = 0; i < 3; i++) Spawn();
    }

    void Spawn()
    {
        int no = nextNo++;
        Vector2 pos;
        int guard = 0;
        do pos = new Vector2(Random.Range(-390f, 390f), Random.Range(-120f, 130f));
        while (waiting.Exists(w => Vector2.Distance(((RectTransform)w.b.transform).anchoredPosition, pos) < 110) && ++guard < 30);
        Button b = Btn($"{no} 號", Paper, Ink, pos, new Vector2(110, 60), () => Serve(no));
        waiting.Add((no, b));
    }

    protected override void Tick()
    {
        if (Time.time >= nextSpawn && nextNo <= need && waiting.Count < 7)
        {
            nextSpawn = Time.time + Mathf.Max(0.6f, 1.4f - difficulty * 0.2f);
            Spawn();
        }
    }

    void Serve(int no)
    {
        if (!Running) return;
        int first = waiting[0].no;   // 清單照到店順序排，第一個就是最早來的
        if (no != first) { mistakes++; hint.text = $"{first} 號比較早來，他不高興了！"; return; }
        Destroy(waiting[0].b.gameObject);
        waiting.RemoveAt(0);
        hint.text = "";
        if (++served >= need) Finish(mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1, $"{need} 位客人都服務完了，插隊 {mistakes} 次");
        else if (waiting.Count == 0) Spawn();
    }

    protected override string Goal() => $"服務 {served} / {need} 位　插隊 {mistakes} 次";
    protected override void TimeUp() => Finish(served * 2 >= need ? 1 : 0, $"時間到：服務了 {served} / {need} 位");
    public override void AutoPlay() { if (waiting.Count > 0) Serve(waiting[0].no); }
}
