using UnityEngine;

// 讓 pickup 自己旋轉；乘上 Time.deltaTime 讓速度變成「每秒」而不是「每格」
public class Rotator : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(new Vector3(15, 30, 45) * Time.deltaTime);
    }
}
