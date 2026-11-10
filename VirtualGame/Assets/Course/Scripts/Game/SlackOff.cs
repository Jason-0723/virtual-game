using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 摸魚：在座位工作時按 Tab，螢幕切成迷因畫面，壓力慢慢下降；再按一次 Tab 切回來
// 主管靠近時，螢幕上會浮出他的反光人影（越近越清楚）；被 Observer 看到正在摸魚就會被抓
public class SlackOff : MonoBehaviour
{
    public TypingTerminal typing;
    public GameObject memePanel;
    public Image reflection;          // 螢幕上的主管反光人影
    public Transform boss;
    public Transform player;
    public float secondsPerStress = 1.5f;   // 摸魚時每幾秒壓力 -1
    public float warnDistance = 8f;          // 主管進到這個距離，人影開始浮出來

    float timer;

    public bool IsSlacking => memePanel.activeSelf;

    void Start() => memePanel.SetActive(false);

    void Update()
    {
        if (!typing.IsOpen)
        {
            memePanel.SetActive(false);
            SetReflection(0);
            return;
        }
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame) memePanel.SetActive(!memePanel.activeSelf);

        // 反光人影：距離 warnDistance 時全透明，貼到身後時最清楚
        float d = boss ? Vector3.Distance(boss.position, player.position) : 999f;
        SetReflection(Mathf.Clamp01(1 - d / warnDistance) * 0.6f);

        if (!IsSlacking) return;
        timer += Time.deltaTime;
        if (timer >= secondsPerStress)
        {
            timer = 0;
            DayManager.Instance.AddStress(-1);
        }
    }

    void SetReflection(float alpha)
    {
        Color c = reflection.color;
        c.a = alpha;
        reflection.color = c;
    }

    public void GetCaught()
    {
        memePanel.SetActive(false);
        typing.Close();
        DayManager.Instance.Interrupt("主管站在你身後",
            "你一回頭，主管就站在那裡，眼睛盯著你的螢幕。\n\n「⋯⋯這個是今天的進度嗎？」",
            "摸魚被主管抓到", 10, -15);
    }
}
