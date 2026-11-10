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
        var rng = new System.Random(policy);

        day.minutesPerSecond = 0;
        if (policy == 0)   // 拍兩張鏡頭畫面給人看：開場對話（臉部特寫）、走路（背後跟隨）
        {
            yield return new WaitForSecondsRealtime(1.5f);
            Snap("face", ui: false);   // 景深會連 UI 一起糊（只有截圖時 UI 才在 3D 裡），所以特寫只拍場景
            dialog.optionButtons[0].onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.5f);
            Snap("follow");

            // 打程式碼：走到座位 → 出現「按 E」提示 → 打 100 下，進度要上升
            TypingTerminal typing = UnityEngine.Object.FindFirstObjectByType<TypingTerminal>();
            yield return WalkInto(player, "Desk");
            Assert.IsTrue(typing.hint.activeSelf, "站在座位卻沒有「按 E 開始工作」");
            int before = day.Progress;
            typing.Open();
            for (int k = 0; k < 100; k++) typing.TypeKey(4);
            yield return null;
            Snap("typing");
            Assert.Greater(day.Progress, before, "打字沒有讓進度上升");
            Assert.IsTrue(typing.codeText.text.Contains("class"), "螢幕上沒有打出程式碼");
            typing.Close();
        }
        day.minutesPerSecond = 60f;   // 一秒過一小時

        float timeout = Time.realtimeSinceStartup + 120f;
        string lastTitle = "";
        var seen = new System.Collections.Generic.List<string>();
        while (!day.Ended)
        {
            Assert.Less(Time.realtimeSinceStartup, timeout, "卡住了，最後的對話框：" + lastTitle);
            if (dialog.IsOpen)
            {
                lastTitle = dialog.titleText.text;
                if (!lastTitle.StartsWith("後來") && lastTitle != "工程師的一天") seen.Add(lastTitle);
                Button[] shown = Array.FindAll(dialog.optionButtons, b => b.gameObject.activeSelf);
                int pick = policy == 0 ? 0 : policy == 1 ? shown.Length - 1 : rng.Next(shown.Length);
                shown[pick].onClick.Invoke();
            }
            else if (day.PendingPlace != null)
            {
                yield return WalkInto(player, day.PendingPlace);
                continue;
            }
            yield return null;
        }

        // 先出現因果時間軸，按「看結局」才出現結局
        TimelineView timeline = UnityEngine.Object.FindFirstObjectByType<TimelineView>();
        Assert.IsTrue(timeline.IsOpen, "下班後沒有出現時間軸");
        Assert.AreEqual(seen.Count, day.Records.Count, "有選擇沒被記錄");
        foreach (var r in day.Records)
            if (r.optionId == "caught-sorry" || r.optionId == "caught-delay")
                Assert.IsNotEmpty(r.causes, "說謊被抓包卻沒連到站會");
        yield return null;
        Snap("timeline" + policy);
        Debug.Log($"[Playthrough] policy {policy} 時間軸：{day.Records.Count} 筆、{timeline.LinkCount} 條紅線");
        timeline.Continue();

        // 結局對話框要出現，而且標題是結局
        Assert.IsTrue(dialog.IsOpen);
        StringAssert.StartsWith("結局", dialog.titleText.text);
        Debug.Log($"[Playthrough] policy {policy} → {dialog.titleText.text}（技術債 {day.TechDebt}）：" + string.Join(" / ", seen));
        yield return new ExitPlayMode();
    }

    // 像玩家走進去一樣：先站到區域旁邊，再用 CharacterController 走進去
    static IEnumerator WalkInto(GameObject player, string placeId)
    {
        PlaceZone zone = Array.Find(UnityEngine.Object.FindObjectsByType<PlaceZone>(FindObjectsSortMode.None), z => z.placeId == placeId);
        Assert.IsNotNull(zone, "events.json 的地點在場景裡找不到：" + placeId);
        CharacterController cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = new Vector3(zone.transform.position.x, 0.05f, zone.transform.position.z) - Vector3.forward * 0.1f;
        cc.enabled = true;
        cc.Move(Vector3.forward * 0.1f);
        yield return new WaitForFixedUpdate();
        yield return null;
    }

    // 主鏡頭畫面存成 專案資料夾/screenshots/<名稱>.png（不含 UI）
    static void Snap(string name, bool ui = true)
    {
        Camera cam = Camera.main;
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        canvas.enabled = ui;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;   // 讓 UI 也拍得進去
        canvas.worldCamera = cam;
        canvas.planeDistance = 0.5f;
        var rt = new RenderTexture(960, 540, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        cam.targetTexture = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.enabled = true;
        RenderTexture.active = null;
        System.IO.Directory.CreateDirectory("../screenshots");
        System.IO.File.WriteAllBytes($"../screenshots/{name}.png", tex.EncodeToPNG());
    }
}
