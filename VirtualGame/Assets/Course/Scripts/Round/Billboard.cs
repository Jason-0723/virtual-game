using UnityEngine;

// 浮空名牌永遠面向鏡頭（第 10 週）：不要用 LookAt，LookAt 攝影機會讓文字左右顛倒
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main) transform.forward = Camera.main.transform.forward;
    }
}
