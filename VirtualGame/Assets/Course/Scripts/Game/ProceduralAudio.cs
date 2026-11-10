using UnityEngine;

// 用程式產生的音效：不用下載音效檔
// 皮鞋聲、鍵盤聲、冷氣嗡嗡聲、Slack 的「叮」
public static class ProceduralAudio
{
    const int Rate = 44100;

    static AudioClip Make(string name, float seconds, System.Func<float, System.Random, float> wave)
    {
        int n = (int)(Rate * seconds);
        var data = new float[n];
        var rng = new System.Random(name.GetHashCode());
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate, rng), -1f, 1f);
        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // 皮鞋走路：每 0.55 秒一聲「喀」（短促的低頻敲擊＋一點雜音），整段可以循環
    public static AudioClip Footsteps() => Make("BossSteps", 1.1f, (t, r) =>
    {
        float local = t % 0.55f;
        float env = Mathf.Exp(-local * 60f);
        return env * (0.6f * Mathf.Sin(2 * Mathf.PI * 140f * local) + 0.4f * ((float)r.NextDouble() * 2 - 1));
    });

    // 遠處同事打字：隨機的短「答」聲，3 秒循環
    public static AudioClip Keyboard() => Make("Keyboard", 3f, (t, r) =>
    {
        float slot = t % 0.09f;
        bool hit = Mathf.PerlinNoise(Mathf.Floor(t / 0.09f) * 0.73f, 3.1f) > 0.45f;
        return hit ? Mathf.Exp(-slot * 400f) * ((float)r.NextDouble() * 2 - 1) * 0.5f : 0f;
    });

    // 冷氣和電腦風扇的低沉嗡嗡聲：低頻正弦＋很小的雜音，2 秒循環（頻率取整數倍，接縫才不會喀一聲）
    public static AudioClip Hum() => Make("Hum", 2f, (t, r) =>
        0.25f * Mathf.Sin(2 * Mathf.PI * 60f * t) + 0.1f * Mathf.Sin(2 * Mathf.PI * 120f * t) + 0.03f * ((float)r.NextDouble() * 2 - 1));

    // Slack 通知：兩個上升的短音
    public static AudioClip Ding() => Make("Ding", 0.4f, (t, r) =>
    {
        float f = t < 0.12f ? 880f : 1320f;
        float local = t < 0.12f ? t : t - 0.12f;
        return 0.5f * Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-local * 12f);
    });
}
