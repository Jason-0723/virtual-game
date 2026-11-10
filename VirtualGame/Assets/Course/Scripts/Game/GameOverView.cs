using System;
using UnityEngine;
using UnityEngine.UI;

// Game Over 畫面：開除原因＋兩個選擇——「重來這一天」或「看一則廣告復活」（其實是詐騙集團的徵才廣告）
// 畫面在執行時才畫（跟 TimelineView 一樣），所以場景裡只要掛這個元件
public class GameOverView : MonoBehaviour
{
    GameObject panel;
    Action onRetry, onAd;

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Show(string title, string reason, Action retry, Action ad)
    {
        onRetry = retry;
        onAd = ad;
        if (panel) Destroy(panel);
        Font font = DayManager.UiFont;

        panel = Ui.Rect("GameOver", transform, new Color(0.12f, 0.02f, 0.02f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000)).gameObject;
        Ui.Label(panel.transform, "GAME OVER", 30, new Color32(0xe0, 0x6a, 0x50, 255), new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(1200, 50), font, TextAnchor.MiddleCenter);
        Ui.Label(panel.transform, title, 64, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1200, 90), font, TextAnchor.MiddleCenter);
        Ui.Label(panel.transform, reason, 28, new Color(1, 1, 1, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1100, 300), font, TextAnchor.MiddleCenter);

        Ui.Button(panel.transform, "重來這一天", new Color(0.9f, 0.9f, 0.88f), new Color32(0x1f, 0x20, 0x1d, 255), new Vector2(-260, -230), new Vector2(420, 90), font, Retry);
        // 廣告按鈕故意做得很誘人：亮黃色、會閃
        Button adButton = Ui.Button(panel.transform, "▶ 看一則廣告，立即復活", new Color32(0xff, 0xc9, 0x1f, 255), new Color32(0x1f, 0x20, 0x1d, 255), new Vector2(260, -230), new Vector2(420, 90), font, Ad);
        adButton.gameObject.AddComponent<Blink>();
        panel.transform.SetAsLastSibling();
    }

    public void Retry() { panel.SetActive(false); onRetry?.Invoke(); }
    public void Ad() { panel.SetActive(false); onAd?.Invoke(); }

    // 讓按鈕一閃一閃（大小呼吸）
    class Blink : MonoBehaviour
    {
        void Update() => transform.localScale = Vector3.one * (1 + 0.05f * Mathf.Sin(Time.unscaledTime * 6));
    }
}

// 執行時畫 UI 的小工具（錨點、位置、大小都用 1920×1080 的參考解析度）
public static class Ui
{
    public static RectTransform Rect(string name, Transform parent, Color c, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        g.GetComponent<Image>().color = c;
        var rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static Text Label(Transform parent, string text, int size, Color c, Vector2 anchor, Vector2 pos, Vector2 box, Font font, TextAnchor align)
    {
        var g = new GameObject("Text", typeof(RectTransform), typeof(Text));
        g.transform.SetParent(parent, false);
        var rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        Text t = g.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = c;
        t.text = text;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Button Button(Transform parent, string text, Color bg, Color fg, Vector2 pos, Vector2 size, Font font, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform r = Rect("Button", parent, bg, new Vector2(0.5f, 0.5f), pos, size);
        Label(r, text, 28, fg, new Vector2(0.5f, 0.5f), Vector2.zero, size, font, TextAnchor.MiddleCenter);
        Button b = r.gameObject.AddComponent<Button>();
        b.onClick.AddListener(onClick);
        return b;
    }
}
