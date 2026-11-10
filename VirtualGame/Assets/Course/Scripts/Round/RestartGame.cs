using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 照順序重啟：六台伺服器的燈會照某個順序閃一次，記住後照同樣的順序點下去
// 三回合，一回合比一回合長；點錯這回合就失敗
public class RestartGame : MonitorGame
{
    public override string Title => "照順序重啟伺服器";

    static readonly string[] Names = { "資料庫", "快取", "後端", "網站", "付款", "通知" };

    readonly List<Image> lights = new List<Image>();
    readonly List<int> seq = new List<int>();
    int round, pos, cleared;
    float nextFlash;
    int flashIndex;      // 正在示範第幾個；示範完 = -1，換你點
    Text hint;

    float Step => Mathf.Max(0.32f, 0.7f - difficulty * 0.07f);

    protected override void Setup()
    {
        lights.Clear();
        round = cleared = 0;
        Label("看清楚燈閃的順序，再照同樣的順序點伺服器", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        hint = Label("", 22, Ink, new Vector2(0, -150), new Vector2(880, 34));
        for (int i = 0; i < Names.Length; i++)
        {
            int k = i;
            RectTransform r = Box(new Color32(0x2b, 0x21, 0x18, 255), new Vector2((i - 2.5f) * 145, 30), new Vector2(125, 200));
            Label(Names[i], 22, Color.white, new Vector2(0, -70), new Vector2(120, 34), r).raycastTarget = false;
            Image light = Box(Color.gray, new Vector2(0, 30), new Vector2(70, 70), r).GetComponent<Image>();
            light.raycastTarget = false;
            r.gameObject.AddComponent<Button>().onClick.AddListener(() => Click(k));
            lights.Add(light);
        }
        NewRound();
    }

    void NewRound()
    {
        seq.Clear();
        int len = 3 + difficulty / 2 + round;
        for (int i = 0; i < len; i++) seq.Add(Random.Range(0, Names.Length));
        pos = 0;
        flashIndex = 0;
        nextFlash = Time.time + 0.6f;
        hint.text = $"第 {round + 1} 回合：看燈⋯⋯（{len} 個）";
        foreach (Image l in lights) l.color = Color.gray;
    }

    protected override void Tick()
    {
        if (flashIndex < 0 || Time.time < nextFlash) return;
        foreach (Image l in lights) l.color = Color.gray;
        // 每一步：亮 0.7 個節拍、暗 0.3 個節拍，連續兩次同一台也看得出來
        int step = flashIndex / 2;
        if (step >= seq.Count) { flashIndex = -1; hint.text = "換你了！照順序點"; return; }
        if (flashIndex % 2 == 0) { lights[seq[step]].color = Gold; nextFlash = Time.time + Step * 0.7f; }
        else nextFlash = Time.time + Step * 0.3f;
        flashIndex++;
    }

    void Click(int k)
    {
        if (!Running || flashIndex >= 0) return;
        foreach (Image l in lights) l.color = Color.gray;
        if (k != seq[pos])
        {
            lights[k].color = Red;
            hint.text = $"順序錯了！應該是「{Names[seq[pos]]}」";
            NextRoundOrFinish();
            return;
        }
        lights[k].color = Green;
        if (++pos >= seq.Count) { cleared++; NextRoundOrFinish(); }
    }

    void NextRoundOrFinish()
    {
        if (++round >= 3) { Finish(cleared, $"三回合成功 {cleared} 次"); return; }
        NewRound();
        nextFlash = Time.time + 1.0f;
    }

    protected override string Goal() => $"第 {Mathf.Min(round + 1, 3)} / 3 回合　成功 {cleared}";
    protected override void TimeUp() => Finish(cleared, $"時間到：成功 {cleared} 回合");
    public override void AutoPlay() { if (flashIndex < 0 && pos < seq.Count) Click(seq[pos]); }
}
