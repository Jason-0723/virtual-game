using UnityEngine;

// 鏡頭跟隨：不能直接把相機設成球的子物件，因為球會滾動旋轉
public class CameraController : MonoBehaviour
{
    public GameObject player;
    private Vector3 offset;

    void Start()
    {
        offset = transform.position - player.transform.position;   // 記住一開始的相對距離
    }

    // LateUpdate：等所有物件這一格都移動完再跟上，畫面才不會抖
    void LateUpdate()
    {
        transform.position = player.transform.position + offset;
    }
}
