using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 自動試玩：機器人從打卡玩到結局，確認不會卡住、不會報錯
// 命令列：Unity -batchmode -projectPath VirtualGame -runTests -testPlatform EditMode -testResults results.xml
public class PlaythroughTest
{
    [UnityTest, Timeout(300000)]
    public IEnumerator PlaysToAnEnding([Values(0, 1, 2)] int policy)
    {
        EditorSceneManager.OpenScene("Assets/Course/Scenes/Office.unity");
        // 進 Play 時不重載程式，測試裡的變數才不會被清掉；結束後還原
        bool optWas = EditorSettings.enterPlayModeOptionsEnabled;
        EnterPlayModeOptions opts = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        yield return new EnterPlayMode();
        EditorSettings.enterPlayModeOptionsEnabled = optWas;
        EditorSettings.enterPlayModeOptions = opts;

        DayManager day = UnityEngine.Object.FindFirstObjectByType<DayManager>();
        EventDialog dialog = UnityEngine.Object.FindFirstObjectByType<EventDialog>();
        GameObject player = GameObject.Find("Player");
        Assert.IsNotNull(day); Assert.IsNotNull(dialog); Assert.IsNotNull(player);
        day.minutesPerSecond = 60f;   // 一秒過一小時
        var rng = new System.Random(policy);

        float timeout = Time.realtimeSinceStartup + 120f;
        string lastTitle = "";
        while (!day.Ended)
        {
            Assert.Less(Time.realtimeSinceStartup, timeout, "卡住了，最後的對話框：" + lastTitle);
            if (dialog.IsOpen)
            {
                lastTitle = dialog.titleText.text;
                Button[] shown = Array.FindAll(dialog.optionButtons, b => b.gameObject.activeSelf);
                int pick = policy == 0 ? 0 : policy == 1 ? shown.Length - 1 : rng.Next(shown.Length);
                shown[pick].onClick.Invoke();
            }
            else if (day.PendingPlace != null)
            {
                PlaceZone zone = Array.Find(UnityEngine.Object.FindObjectsByType<PlaceZone>(FindObjectsSortMode.None), z => z.placeId == day.PendingPlace);
                Assert.IsNotNull(zone, "events.json 的地點在場景裡找不到：" + day.PendingPlace);
                // 像玩家走進去一樣：先站到區域旁邊，再用 CharacterController 走進去
                CharacterController cc = player.GetComponent<CharacterController>();
                cc.enabled = false;
                player.transform.position = new Vector3(zone.transform.position.x, 0.05f, zone.transform.position.z) - Vector3.forward * 0.1f;
                cc.enabled = true;
                cc.Move(Vector3.forward * 0.1f);
            }
            yield return null;
        }

        // 結局對話框要出現，而且標題是結局
        Assert.IsTrue(dialog.IsOpen);
        StringAssert.StartsWith("結局", dialog.titleText.text);
        Debug.Log($"[Playthrough] policy {policy} → {dialog.titleText.text}");
        yield return new ExitPlayMode();
    }
}
