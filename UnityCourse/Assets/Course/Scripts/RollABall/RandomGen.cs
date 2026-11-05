using System.Collections;
using UnityEngine;

// 掛在 PickUpGen 上：負責在執行中生成 pickup
public class RandomGen : MonoBehaviour
{
    public GameObject pickUpPrefab;
    public float range = 3.7f;        // 生成範圍（地板是 10x10，扣掉牆）
    public float minDelay = 2f;
    public float maxDelay = 7f;

    // 延遲一段隨機時間後生成一個
    public void spawnNew()
    {
        StartCoroutine(spawnAfterTime());
    }

    // 立刻生成 s_times 個
    public void spawnNow(int s_times)
    {
        for (int i = 0; i < s_times; i++)
        {
            Spawn();
        }
    }

    IEnumerator spawnAfterTime()
    {
        yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        Spawn();
    }

    void Spawn()
    {
        GameObject nb = Instantiate(pickUpPrefab, this.transform);
        nb.transform.position = new Vector3(Random.Range(-range, range), 0.5f, Random.Range(-range, range));
    }
}
