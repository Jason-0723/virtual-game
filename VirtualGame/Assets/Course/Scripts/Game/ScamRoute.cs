using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 詐騙園區支線（反詐教育）：Game Over 後點「看廣告復活」→ 高薪海外職缺廣告 → 被帶到園區
// 同一間辦公室重新上色（慘綠、鐵窗），出勤卡變成豬仔編號；玩家以為在寫「客服系統」，
// 從自己打出來的程式碼一步步發現是在騙人；最後選擇求救或順從，警察攻堅，結尾放 165 反詐騙專線
// 程式碼只寫到「顯示假數字」「擋住提領」這種概念，不寫任何真的能用的詐騙流程
public class ScamRoute : MonoBehaviour
{
    public Light sun;
    public Text cardTitle, cardShift, clockText;
    public Text[] statNames;      // 出勤卡上的四個數值名稱
    public Image[] statBars;

    public bool Active { get; private set; }
    public bool NeedsTyping { get; private set; }   // 給自動試玩：現在要回座位打字
    public bool AdShowing => ad != null && ad.activeSelf;

    GameObject ad, black;
    Text blackText;
    int typed, need;
    System.Action afterTyping;
    AudioSource sfx;

    const string Stage1 =
        "// 投資 App：會員餘額畫面\n" +
        "public class BalancePage : MonoBehaviour\n{\n" +
        "    public Text balanceText;\n" +
        "    float realDeposit;   // 會員真正存進來的錢\n\n" +
        "    void Refresh()\n    {\n" +
        "        // 幹部交代：畫面上的獲利乘以 8.5 倍，看起來很會賺\n" +
        "        balanceText.text = (realDeposit * 8.5f).ToString(\"N0\");\n" +
        "        // ……但這只是畫面上的數字，後台裡的錢早就被轉走了\n" +
        "    }\n}\n";

    const string Stage2 =
        "// 提領按鈕\n" +
        "public void OnClickWithdraw()\n{\n" +
        "    // 幹部交代：不管誰按提領，錢都不能出去\n" +
        "    ShowPopup(\"帳戶疑似洗錢已凍結，請再匯 50 萬保證金解鎖\");\n" +
        "    LockAccount(currentUser);\n" +
        "    // ……這不是客服系統。這是讓人越陷越深的陷阱。\n" +
        "}\n";

    const string Stage3 =
        "// 幹部看不到的地方\n" +
        "void WarnVictim()\n{\n" +
        "    ShowPopup(\"這是詐騙！不要匯款，快打 165！\");\n" +
        "    SendLog(\"園區位置已附上\", to: \"警方\");\n" +
        "}\n";

    void Start()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        DayManager.Instance.typing.onTyped += OnTyped;
    }

    // ───── 1. 廣告 ─────
    public void Begin()
    {
        Font f = DayManager.UiFont;
        ad = Ui.Rect("Ad", transform, new Color32(0x0f, 0x3d, 0x6e, 250), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000)).gameObject;
        RectTransform card = Ui.Rect("Card", ad.transform, new Color32(0xff, 0xd2, 0x3f, 255), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 680));
        Ui.Label(card, "廣告", 22, new Color32(0x55, 0x55, 0x55, 255), new Vector2(0, 1), new Vector2(20, -14), new Vector2(200, 30), f, TextAnchor.UpperLeft);
        Ui.Label(card, "海外頂尖科技園區・急徵 C# 工程師", 40, new Color32(0x0f, 0x3d, 0x6e, 255), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1000, 60), f, TextAnchor.MiddleCenter);
        Ui.Label(card, "年薪 300 萬", 110, new Color32(0xc2, 0x1f, 0x1f, 255), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(1000, 140), f, TextAnchor.MiddleCenter);
        Ui.Label(card, "免經驗　不加班　包機票　包住宿\n被開除也沒關係，我們只看潛力！", 34, new Color32(0x1f, 0x20, 0x1d, 255), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1000, 120), f, TextAnchor.MiddleCenter);
        Ui.Label(card, "名額只剩 3 位", 26, new Color32(0xc2, 0x1f, 0x1f, 255), new Vector2(0.5f, 0.5f), new Vector2(0, -130), new Vector2(1000, 40), f, TextAnchor.MiddleCenter);
        Ui.Button(card, "立即應徵（復活）", new Color32(0xc2, 0x1f, 0x1f, 255), Color.white, new Vector2(0, -230), new Vector2(460, 90), f, AcceptAd);
        ad.transform.SetAsLastSibling();
    }

    // ───── 2. 被帶走 ─────
    public void AcceptAd()
    {
        ad.SetActive(false);
        Active = true;
        DayManager.Instance.EnterScamMode();
        StartCoroutine(Transition());
    }

    IEnumerator Transition()
    {
        black = Ui.Rect("Black", transform, Color.black, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000)).gameObject;
        blackText = Ui.Label(black.transform, "", 40, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 300), DayManager.UiFont, TextAnchor.MiddleCenter);
        black.transform.SetAsLastSibling();
        Play(ProceduralAudio.Rumble());
        foreach (string line in new[] { "飛機起飛了。", "落地後，接你的人收走了你的護照。", "鐵門在你身後關上。" })
        {
            blackText.text = line;
            yield return new WaitForSeconds(1.6f);
        }
        Play(ProceduralAudio.Thud());
        Reskin();
        black.SetActive(false);
        Brief("幹部",
            "「歡迎加入！你的工作是寫『客服系統』。業績好，就能早點回家。」\n\n辦公室跟以前一樣，只是窗戶多了鐵欄，門口有人拿著電擊棒。\n\n回座位按 E，開始寫第一段程式。",
            Stage1, 160, AfterStage1);
    }

    // 同一間辦公室重新上色：慘綠燈光、低彩度、窗戶鐵欄；出勤卡改成豬仔編號
    void Reskin()
    {
        Atmosphere atmo = FindFirstObjectByType<Atmosphere>();
        if (atmo) atmo.enabled = false;
        sun.color = new Color32(0x9c, 0xd6, 0x9a, 255);
        sun.intensity = 0.7f;
        RenderSettings.ambientSkyColor = new Color32(0x7a, 0x99, 0x7f, 255);
        Volume v = FindFirstObjectByType<Volume>();
        if (v && v.profile.TryGet(out ColorAdjustments ca))
        {
            ca.saturation.value = -45;
            ca.colorFilter.Override(new Color32(0xc8, 0xe6, 0xc0, 255));
        }
        foreach (Transform w in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (w.name != "Window") continue;
            for (int i = -2; i <= 2; i++)
            {
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(bar.GetComponent<Collider>());
                bar.transform.position = w.position + new Vector3(0.08f, 0, i * 0.4f);
                bar.transform.localScale = new Vector3(0.05f, 0.75f, 0.05f);
                bar.GetComponent<Renderer>().material.color = new Color(0.1f, 0.1f, 0.1f);
            }
        }
        cardTitle.text = "編號 9527（護照已扣押）";
        cardShift.text = "上班 08:00 － 下班：沒有下班";
        clockText.text = "--:--";
        string[] names = { "業績", "恐懼", "幹部戒心", "求生欲" };
        for (int i = 0; i < statNames.Length && i < names.Length; i++) statNames[i].text = names[i];
        SetBars(0.1f, 0.6f, 0.3f, 0.8f);
    }

    void SetBars(params float[] v)
    {
        for (int i = 0; i < statBars.Length && i < v.Length; i++) statBars[i].fillAmount = v[i];
    }

    // ───── 3. 三段程式碼，一步步發現真相 ─────
    void Brief(string who, string text, string code, int chars, System.Action then)
    {
        DayManager.Instance.dialog.Show(who, text, new[] { "回座位寫程式" }, _ =>
        {
            DayManager.Instance.typing.LoadCode(code);
            typed = 0;
            need = chars;
            afterTyping = then;
            NeedsTyping = true;
            DayManager.Instance.objectiveText.text = "回座位按 E，打完這段程式";
            DayManager.Instance.objectiveNote.SetActive(true);
        });
    }

    void OnTyped(int n)
    {
        if (!NeedsTyping) return;
        typed += n;
        if (typed < need) return;
        NeedsTyping = false;
        DayManager.Instance.objectiveNote.SetActive(false);
        DayManager.Instance.typing.Close();
        afterTyping();
    }

    void AfterStage1()
    {
        SetBars(0.4f, 0.7f, 0.3f, 0.6f);
        Brief("幹部",
            "「不錯嘛。客人一直按『提領』，你去把那個按鈕『修好』。」\n\n你這才注意到，你剛寫的「餘額」，只是畫面上乘了 8.5 倍的假數字。",
            Stage2, 140, AfterStage2);
    }

    void AfterStage2()
    {
        SetBars(0.7f, 0.9f, 0.5f, 0.5f);
        DayManager.Instance.dialog.Show("後台跳出一個新帳號",
            "一個台灣的大學生，正準備把下學期的學費匯進來。\n\n他的大頭照，跟你學弟長得有點像。\n\n幹部在你身後走來走去。",
            new[] { "偷偷把畫面改成「這是詐騙！快打 165」，再把園區位置藏進記錄檔送出去", "照幹部說的，繼續改" },
            i =>
            {
                if (i == 0) Brief("你壓低身子", "趁幹部轉身，你開了一個新檔案。\n\n手在發抖，但你知道要寫什麼。", Stage3, 80, () => StartCoroutine(Raid(true)));
                else StartCoroutine(Raid(false));
            });
    }

    // ───── 4. 警察攻堅 ─────
    IEnumerator Raid(bool helped)
    {
        Play(ProceduralAudio.Siren());
        DayManager.Instance.dialog.Show("！！！", "「警察！全部趴下！不准動！」\n\n鐵門被撞開，紅藍警燈照進整間辦公室。", new[] { "⋯⋯" }, _ => { });
        for (float t = 0; t < 3f; t += 0.25f)
        {
            sun.color = (int)(t * 4) % 2 == 0 ? new Color(1, 0.15f, 0.15f) : new Color(0.2f, 0.35f, 1f);
            sun.intensity = 2f;
            DayManager.Instance.Shake(0.4f);
            yield return new WaitForSeconds(0.25f);
        }
        sun.color = Color.white;
        sun.intensity = 1f;
        while (DayManager.Instance.dialog.IsOpen) yield return null;

        string title = helped ? "結局：被救回來的人" : "結局：共犯";
        string body = helped
            ? "你留下的警告，讓那位大學生沒有匯款；你送出去的位置，讓警方找到了這裡。\n\n你以人口販運受害者的身分，被送回台灣。"
            : "你以詐欺共犯的身分被上銬帶走。\n\n你寫的每一行程式，都成了起訴書上的證據。技術無罪，但寫黑心程式會坐牢。";
        body += "\n\n世界上沒有「免經驗、高薪、包機票」的海外工程師職缺。\n看到可疑的海外徵才或投資網站，請撥 165 反詐騙諮詢專線。";
        DayManager.Instance.dialog.Show(title, body, new[] { "重新開始" }, _ => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }

    void Play(AudioClip c)
    {
        sfx.clip = c;
        sfx.Play();
    }
}
