using System;
using UnityEngine;
using UnityEngine.UI;

// 事件對話框：標題＋內文＋最多 3 個選項，選完回呼
public class EventDialog : MonoBehaviour
{
    public GameObject panel;
    public Text titleText;
    public Text bodyText;
    public Button[] optionButtons;

    public bool IsOpen => panel.activeSelf;

    void Awake()
    {
        panel.SetActive(false);
    }

    public void Show(string title, string body, string[] options, Action<int> onPick)
    {
        titleText.text = title;
        bodyText.text = body;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            Button b = optionButtons[i];
            b.gameObject.SetActive(i < options.Length);
            if (i >= options.Length) continue;
            b.GetComponentInChildren<Text>(true).text = options[i];   // true：面板還關著時也找得到
            b.onClick.RemoveAllListeners();
            int pick = i;   // 迴圈變數要先複製，不然每顆按鈕都會拿到最後一個 i
            b.onClick.AddListener(() => { panel.SetActive(false); onPick(pick); });
        }
        panel.SetActive(true);
    }
}
