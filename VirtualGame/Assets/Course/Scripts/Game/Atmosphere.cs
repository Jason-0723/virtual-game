using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 氣氛：
// 1. 壓力越高，畫面四角越暗、邊緣越糊（暗角＋色差）
// 2. 太陽依遊戲時鐘移動：09:00 斜射晨光 → 12:00 白光 → 18:00 橘色夕陽
public class Atmosphere : MonoBehaviour
{
    public Light sun;
    public Gradient sunColor;
    public AnimationCurve sunHeight = new AnimationCurve(new Keyframe(0, 22), new Keyframe(0.4f, 58), new Keyframe(1, 12));

    Vignette vignette;
    ChromaticAberration aberration;
    float baseVignette;

    void Start()
    {
        Volume v = FindFirstObjectByType<Volume>();
        if (v)
        {
            v.profile.TryGet(out vignette);   // .profile 是執行時的複本，改它不會動到專案檔
            v.profile.TryGet(out aberration);
            if (vignette) baseVignette = vignette.intensity.value;
        }
    }

    void Update()
    {
        DayManager day = DayManager.Instance;
        if (day == null) return;

        // 壓力 30 以下沒感覺，到 100 最明顯
        float s = Mathf.InverseLerp(30, 100, day.Stress);
        if (vignette) vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, baseVignette + 0.32f * s, Time.deltaTime * 2);
        if (aberration) aberration.intensity.value = Mathf.Lerp(aberration.intensity.value, 0.7f * s, Time.deltaTime * 2);

        // 太陽：高度照曲線、方向從東南轉到西南、顏色照漸層
        float t = day.Clock01;
        sun.transform.rotation = Quaternion.Euler(sunHeight.Evaluate(t), Mathf.Lerp(-70, 60, t), 0);
        sun.color = sunColor.Evaluate(t);
        sun.intensity = Mathf.Lerp(1.1f, 1.4f, Mathf.Sin(t * Mathf.PI));
    }
}
