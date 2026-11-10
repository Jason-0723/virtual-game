using UnityEngine;

// 主管的視線：照 JohnLemon 的 Gargoyle／Ghost 寫法
// PointOfView 上的觸發區（主管前方）先判斷「玩家在不在範圍內」，在範圍內才用射線確認中間沒有東西擋著
// 看到玩家正在摸魚 → 被抓到：主管信任大扣
public class Observer : MonoBehaviour
{
    public Transform player;
    public SlackOff slackOff;
    public float cooldown = 20f;    // 抓到一次之後，隔一段時間才會再抓

    bool m_IsPlayerInRange;
    float m_NextCatch;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == player) m_IsPlayerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform == player) m_IsPlayerInRange = false;
    }

    void Update()
    {
        if (!m_IsPlayerInRange || !slackOff.IsSlacking || Time.time < m_NextCatch) return;
        // Vector3.up：瞄玩家的身體而不是腳底
        Vector3 direction = player.position - transform.position + Vector3.up;
        Ray ray = new Ray(transform.position, direction);
        // 忽略觸發區（例如座位的地點範圍），不然射線會先打到它
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 30f, ~0, QueryTriggerInteraction.Ignore) && raycastHit.collider.transform == player)
        {
            m_NextCatch = Time.time + cooldown;
            slackOff.GetCaught();
        }
    }
}
