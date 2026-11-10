using Unity.AI.Navigation;
using UnityEngine;

// 開場時：
// 1. 烘焙 NavMesh（主管巡邏要用；場景是程式產生的，所以執行時烘焙，不另存檔）再讓主管出場
// 2. 放辦公室環境音：冷氣嗡嗡聲（2D）、同事座位的打字聲（3D，走近才聽得清楚）
public class OfficeAudioAndNav : MonoBehaviour
{
    public NavMeshSurface surface;
    public GameObject boss;
    public AudioSource hum;
    public AudioSource[] keyboards;

    void Awake()
    {
        surface.BuildNavMesh();
        if (boss) boss.SetActive(true);   // NavMeshAgent 要在 NavMesh 建好之後才能啟用
    }

    void Start()
    {
        hum.clip = ProceduralAudio.Hum();
        hum.Play();
        AudioClip keys = ProceduralAudio.Keyboard();
        foreach (AudioSource k in keyboards)
        {
            k.clip = keys;
            k.time = Random.Range(0f, keys.length);   // 每個人從不同地方開始，才不會同步
            k.Play();
        }
    }
}
