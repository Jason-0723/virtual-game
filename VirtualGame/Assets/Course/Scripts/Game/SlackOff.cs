using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 摸魚：在座位工作時按 Tab，螢幕切成隨機一個「App」（迷因板、購物網、看盤、人力銀行、手遊、刷題網）
// 每次打開抽一則內容（events.json 的 slack），效果有好有壞：看迷因降壓、買股票可能跌停壓力暴增
// 同一則內容 12 秒內重開不會再算一次，避免狂按 Tab 刷數值
// 主管靠近時：螢幕浮出他的反光人影、耳邊響起心跳聲；被 Observer 看到正在摸魚就會被抓
public class SlackOff : MonoBehaviour
{
    public TypingTerminal typing;
    public GameObject memePanel;
    public Text memeTitle, memeBody;
    public Image reflection;          // 螢幕上的主管反光人影
    public Transform boss;
    public Transform player;
    public float warnDistance = 8f;   // 主管進到這個距離，人影開始浮出來
    public float heartDistance = 4f;  // 主管進到這個距離，開始聽到心跳
    public float cooldown = 12f;

    AudioSource heart;
    float nextNew;
    int last = -1;

    public bool IsSlacking => memePanel.activeSelf;

    void Start()
    {
        memePanel.SetActive(false);
        heart = gameObject.AddComponent<AudioSource>();
        heart.clip = ProceduralAudio.Heartbeat();
        heart.loop = true;
        heart.volume = 0;
        heart.Play();
    }

    void Update()
    {
        DayManager day = DayManager.Instance;
        float d = boss && boss.gameObject.activeInHierarchy ? Vector3.Distance(boss.position, player.position) : 999f;
        heart.volume = day != null && !day.WorldPaused ? Mathf.Clamp01(1 - d / heartDistance) * 0.8f : 0;

        if (!typing.IsOpen)
        {
            memePanel.SetActive(false);
            SetReflection(0);
            return;
        }
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame)
        {
            if (IsSlacking) memePanel.SetActive(false);
            else Open();
        }
        // 反光人影：距離 warnDistance 時全透明，貼到身後時最清楚
        SetReflection(Mathf.Clamp01(1 - d / warnDistance) * 0.6f);
    }

    // 打開摸魚畫面：冷卻過了就抽一則新的並算效果，沒過就顯示上一則
    public void Open()
    {
        memePanel.SetActive(true);
        DayManager day = DayManager.Instance;
        var items = day.Data.slack;
        if (items == null || items.Length == 0) return;
        if (Time.time >= nextNew || last < 0)
        {
            int i;
            do i = Random.Range(0, items.Length); while (items.Length > 1 && i == last);
            last = i;
            nextNew = Time.time + cooldown;
            DayManager.SlackItem it = items[i];
            day.AddStats(it.progress, it.stress, it.trust, it.life);
            memeTitle.text = it.app;
            memeBody.text = it.text + "\n\n" + DayManager.Delta(it.progress, it.stress, it.trust, it.life) + "\n\n（按 Tab 切回工作）";
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
