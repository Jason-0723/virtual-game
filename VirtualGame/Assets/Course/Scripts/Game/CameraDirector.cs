using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 鏡頭導演：平常用背後斜上方的跟隨鏡頭，對話框打開時切到臉部特寫
// 兩台都是 Cinemachine 虛擬鏡頭，切換的過渡由 Main Camera 上的 CinemachineBrain 處理；玩家不能自己轉鏡頭
// 特寫時順便打開景深（背景糊掉）和輪廓光（頭髮、肩膀邊緣亮起來）
public class CameraDirector : MonoBehaviour
{
    public CinemachineCamera followCam;
    public CinemachineCamera faceCam;
    public EventDialog dialog;
    public Animator player;
    public Light rimLight;

    DepthOfField dof;

    void Start()
    {
        // 臉部特寫對準頭骨；找不到頭骨（不是 Humanoid）就對準角色身上 1.6 公尺高的位置
        Transform head = player.GetBoneTransform(HumanBodyBones.Head);
        if (head == null)
        {
            head = new GameObject("HeadTarget").transform;
            head.SetParent(player.transform, false);
            head.localPosition = new Vector3(0, 1.6f, 0);
        }
        faceCam.LookAt = head;
        faceCam.gameObject.SetActive(false);
        Volume v = FindFirstObjectByType<Volume>();
        if (v) v.profile.TryGet(out dof);   // .profile 會複製一份，執行時改它不會動到專案裡的檔案
    }

    void Update()
    {
        // 同優先度時，最後啟用的鏡頭會接手
        bool talk = dialog.IsOpen;
        if (faceCam.gameObject.activeSelf == talk) return;
        faceCam.gameObject.SetActive(talk);
        if (dof) dof.active = talk;
        if (rimLight) rimLight.gameObject.SetActive(talk);
    }
}
