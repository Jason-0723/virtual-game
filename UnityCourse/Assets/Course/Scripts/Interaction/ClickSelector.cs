using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

// 第 8 週：滑鼠點選 → 高亮 → 資訊面板
public class ClickSelector : MonoBehaviour
{
    public Camera cam;                         // 不指定就用 Main Camera
    public GameObject infoPanel;               // 資訊面板（沒選東西時隱藏）
    public TMP_Text titleText;                 // 面板上的名稱
    public TMP_Text descText;                  // 面板上的描述
    public Color highlightColor = Color.yellow;
    public float maxDistance = 1000f;

    private InfoTag current;                   // 目前選中的物件

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;   // 這一格沒按左鍵就結束

        // 點在 UI 上（例如面板）時不要穿透去選 3D 物件
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // 從相機經過滑鼠位置射出一條射線
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            // 打到的可能是子物件的 Collider，所以往父物件找 InfoTag
            InfoTag tag = hit.collider.GetComponentInParent<InfoTag>();
            if (tag != null)
            {
                Select(tag);
                return;
            }
        }
        Deselect();   // 點到空白處 = 取消選取
    }

    void Select(InfoTag tag)
    {
        if (tag == current) return;
        Deselect();
        current = tag;
        current.SetHighlight(true, highlightColor);

        if (infoPanel != null) infoPanel.SetActive(true);
        if (titleText != null) titleText.text = tag.displayName;
        if (descText != null) descText.text = tag.description;
    }

    void Deselect()
    {
        if (current != null)
        {
            current.SetHighlight(false, highlightColor);
            current = null;
        }
        if (infoPanel != null) infoPanel.SetActive(false);
    }
}
