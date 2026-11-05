using UnityEngine;

// 第 8 週示範腳本 + 進階題：勾選 onlyWhenSelected 就只有被選中時才轉
public class Spin : MonoBehaviour
{
    public float degreesPerSecond = 45f;
    public bool onlyWhenSelected = false;

    private InfoTag infoTag;

    void Start()
    {
        infoTag = GetComponent<InfoTag>();
    }

    void Update()
    {
        if (onlyWhenSelected && (infoTag == null || !infoTag.IsSelected)) return;
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
