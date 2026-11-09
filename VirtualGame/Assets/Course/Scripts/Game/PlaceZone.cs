using UnityEngine;

// 地點觸發區（會議室、座位、PM 座位⋯）：玩家在區域內就通知 DayManager
[RequireComponent(typeof(Collider))]
public class PlaceZone : MonoBehaviour
{
    public string placeId;        // 對應 events.json 的 place
    public string displayName;    // 畫面上顯示「請到：xxx」
    public GameObject marker;     // 地上的黃色光圈，有任務時才亮

    public bool PlayerInside { get; private set; }   // 打程式碼小遊戲要站在座位才能開始

    void OnTriggerEnter(Collider other) { if (other.GetComponent<SimpleWalker>() != null) PlayerInside = true; }
    void OnTriggerExit(Collider other) { if (other.GetComponent<SimpleWalker>() != null) PlayerInside = false; }

    void OnTriggerStay(Collider other)   // Stay：事件跳出時人已經站在那裡也算
    {
        if (other.GetComponent<SimpleWalker>() != null) DayManager.Instance.EnterPlace(placeId);
    }
}
