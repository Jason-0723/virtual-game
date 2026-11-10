using UnityEngine;
using UnityEngine.AI;

// 主管巡邏：照 JohnLemon 的 Ghost 寫法，用 NavMeshAgent 依序走過固定的 waypoint，走完一圈再從頭
// 不是自動追玩家的 AI，路線是固定的（設計文件：不要做主管自動尋路）
// 皮鞋聲是 3D 音效：AudioListener 在玩家身上，主管越近聲音越大（JohnLemon 的 Ghost 腳步聲）
[RequireComponent(typeof(NavMeshAgent))]
public class WaypointPatrol : MonoBehaviour
{
    public Transform[] waypoints;

    NavMeshAgent navMeshAgent;
    AudioSource footsteps;
    Animator animator;
    int m_CurrentWaypointIndex;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        footsteps = GetComponent<AudioSource>();
        if (footsteps)
        {
            footsteps.clip = ProceduralAudio.Footsteps();
            footsteps.loop = true;
            footsteps.Play();
        }
        navMeshAgent.SetDestination(waypoints[0].position);
    }

    void Update()
    {
        // 對話框打開時遊戲時間是停的，主管也要停
        DayManager day = DayManager.Instance;
        bool paused = day == null || day.WorldPaused;   // 詐騙支線時幹部照樣巡邏
        navMeshAgent.isStopped = paused;
        if (animator) animator.speed = paused ? 0 : 1;
        if (footsteps) footsteps.mute = paused;
        if (paused) return;

        // 快到了就換下一個點；% 讓最後一個點之後回到第一個
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance)
        {
            m_CurrentWaypointIndex = (m_CurrentWaypointIndex + 1) % waypoints.Length;
            navMeshAgent.SetDestination(waypoints[m_CurrentWaypointIndex].position);
        }
    }
}
