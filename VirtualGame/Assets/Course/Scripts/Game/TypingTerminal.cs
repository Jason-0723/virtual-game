using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 打程式碼小遊戲：站在自己座位按 E 開始，之後按任何鍵螢幕就多打出 3～5 個字
// 打出來的是這個遊戲自己的 C# 程式（OfficeBuilder 建場景時把幾個 .cs 合併成 Resources/typing_code.txt）
public class TypingTerminal : MonoBehaviour
{
    public GameObject panel;        // 螢幕畫面
    public Text codeText;
    public Text fileText;           // 視窗標題：現在打到哪個檔案
    public Image barFill;           // 這份程式完成度
    public GameObject hint;         // 「按 E 開始工作」
    public PlaceZone desk;
    public EventDialog dialog;
    public int charsPerProgress = 60;   // 每打這麼多字，進度 +1
    public int visibleLines = 16;

    string code;
    int typed;
    int sinceProgress;

    public bool IsOpen => panel.activeSelf;

    // VS Code 配色：註解綠、字串橘、關鍵字藍、大寫開頭的型別青；用一個 regex 一次掃，註解和字串裡的字不會再被上色
    static readonly Regex Token = new Regex(
        @"(?<c>//[^\n]*)|(?<s>""[^""\n]*""?)|(?<k>\b(?:using|public|private|static|readonly|class|struct|void|int|float|bool|string|var|new|return|if|else|for|foreach|while|in|out|null|true|false|this|const)\b)|(?<t>\b[A-Z]\w*\b)");

    public static string Highlight(string code) => Token.Replace(code, m =>
    {
        string color = m.Groups["c"].Success ? "#6a9955" : m.Groups["s"].Success ? "#ce9178" : m.Groups["k"].Success ? "#569cd6" : "#4ec9b0";
        return $"<color={color}>{m.Value}</color>";
    });

    void Awake()
    {
        code = Resources.Load<TextAsset>("typing_code").text.Replace("\r", "");
        panel.SetActive(false);
        hint.SetActive(false);
    }

    // 放在 Start：DayManager.Awake 會把所有字換成正黑體，這裡要在它之後再換回等寬字
    void Start()
    {
        // 等寬字型像真的編輯器；中文註解借正黑體
        codeText.font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Microsoft JhengHei", "Courier New" }, 24);
    }

    void Update()
    {
        DayManager day = DayManager.Instance;
        bool canWork = desk.PlayerInside && !dialog.IsOpen && !day.Ended;
        Keyboard kb = Keyboard.current;

        if (!IsOpen)
        {
            hint.SetActive(canWork);
            if (canWork && kb != null && kb.eKey.wasPressedThisFrame) Open();
            return;
        }
        if (!canWork || (kb != null && kb.escapeKey.wasPressedThisFrame)) { Close(); return; }
        // 專案只開新版 Input System，舊的 Input.anyKeyDown 會報錯；這行是同樣的意思
        if (kb != null && kb.anyKey.wasPressedThisFrame) TypeKey(Random.Range(3, 6));
    }

    public void Open()
    {
        panel.SetActive(true);
        hint.SetActive(false);
        Refresh();
    }

    public void Close() => panel.SetActive(false);

    public void TypeKey(int chars)
    {
        typed += chars;
        sinceProgress += chars;
        if (typed >= code.Length) typed = 0;          // 打完就從頭再來一份
        while (sinceProgress >= charsPerProgress)
        {
            sinceProgress -= charsPerProgress;
            DayManager.Instance.AddProgress(1);
        }
        Refresh();
    }

    void Refresh()
    {
        string shown = code.Substring(0, typed);
        // 只顯示最後幾行，像編輯器一直往下捲
        int cut = shown.Length;
        for (int n = 0; n < visibleLines && cut > 0; n++) cut = shown.LastIndexOf('\n', cut - 1);
        codeText.text = Highlight(shown.Length == 0 ? "" : shown.Substring(cut + 1)) + "▌";
        // 標題列顯示目前打到的檔名（從完整程式找，檔名才打一半時也不會出錯）
        int header = code.LastIndexOf("// ===== ", Mathf.Max(0, typed - 1));
        int nameEnd = header < 0 ? -1 : code.IndexOf(" =====", header + 9);
        fileText.text = (nameEnd < 0 ? "" : code.Substring(header + 9, nameEnd - header - 9) + " — ") + "Visual Studio Code";
        barFill.fillAmount = (float)typed / code.Length;
    }
}
