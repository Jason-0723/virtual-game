using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 上線時機：游標在軌道上來回跑，停在綠色區域時按「上線！」（或空白鍵），連續成功 3 次
// 失敗 3 次就翻車；難度越高綠區越窄、游標越快
public class DeployGame : MonitorGame
{
    public override string Title => "按下上線鍵";

    RectTransform zone, cursor;
    Text lights;
    float pos, dir = 1, zoneStart, width;
    int hits, fails;
    const float TrackW = 800;

    protected override void Setup()
    {
        hits = fails = 0; pos = 0; dir = 1;
        width = Mathf.Max(8, 24 - difficulty * 4);
        Label("游標停在綠色區域時按「上線！」（或空白鍵），連續成功 3 次", 22, Ink, new Vector2(0, 160), new Vector2(880, 34));
        RectTransform track = Box(new Color32(0xdd, 0xd5, 0xc4, 255), new Vector2(0, 50), new Vector2(TrackW, 80));
        zone = Box(new Color32(0xb8, 0xec, 0xc9, 255), Vector2.zero, new Vector2(10, 80), track);
        cursor = Box(Ink, Vector2.zero, new Vector2(10, 100), track);
        lights = Label("", 40, Ink, new Vector2(0, -50), new Vector2(400, 60));
        Btn("上線！", Orange, Color.white, new Vector2(0, -140), new Vector2(300, 70), Press);
        NewZone();
        Light();
    }

    void NewZone()
    {
        zoneStart = Random.Range(5f, 95f - width);
        zone.anchoredPosition = new Vector2((zoneStart + width / 2 - 50) / 100 * TrackW, 0);
        zone.sizeDelta = new Vector2(width / 100 * TrackW, 80);
    }

    void Light() => lights.text = new string('●', hits) + new string('×', fails) + new string('○', Mathf.Max(0, 3 - hits - fails));

    protected override string Goal() => $"上線成功 {hits} / 3（失敗 {fails} 次）";

    protected override void Tick()
    {
        pos += dir * (55 + difficulty * 15) * Time.deltaTime;
        if (pos > 100) { pos = 100; dir = -1; }
        if (pos < 0) { pos = 0; dir = 1; }
        cursor.anchoredPosition = new Vector2((pos - 50) / 100 * TrackW, 0);
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Press();
    }

    bool InZone => pos >= zoneStart && pos <= zoneStart + width;

    void Press()
    {
        if (!Running) return;
        if (InZone) { hits++; NewZone(); } else fails++;
        Light();
        if (hits >= 3) Finish(fails == 0 ? 3 : fails == 1 ? 2 : 1, $"上線成功！失敗了 {fails} 次");
        else if (fails >= 3) Finish(0, "連續失敗，網站在上線那一刻掛了");
    }

    protected override void TimeUp() => Finish(hits >= 2 ? 1 : 0, "時間到，大家還在等你按下上線");

    public override void AutoPlay() { if (InZone) Press(); }
}
