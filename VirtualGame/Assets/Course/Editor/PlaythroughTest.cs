using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 自動試玩：機器人從週一玩到週五（或被開除後走詐騙支線），確認不會卡住、不會報錯，順便截圖
// 命令列：sh run-tests.sh（Unity 要先關掉）
public class PlaythroughTest
{
    DayManager day;
    EventDialog dialog;
    GameObject player;
    TypingTerminal typing;
    GameOverView gameOver;
    ScamRoute scam;
    TimelineView timeline;

    // 測試失敗時也要離開 Play 模式，不然下一個測試會拿到上一個的舊場景
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;   // 還原：你自己在 Unity 按 ▶ 時照常重載程式
    }

    // 進 Play 時不重載程式，測試裡的變數才不會被清掉（EnterPlayMode 必須在測試最外層 yield，包進子函式框架不認）
    static void PrepareScene()
    {
        EditorSceneManager.OpenScene("Assets/Course/Scenes/Office.unity");
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
    }

    void FindObjects()
    {
        day = UnityEngine.Object.FindFirstObjectByType<DayManager>();
        dialog = UnityEngine.Object.FindFirstObjectByType<EventDialog>();
        player = GameObject.Find("Player");
        typing = UnityEngine.Object.FindFirstObjectByType<TypingTerminal>();
        gameOver = UnityEngine.Object.FindFirstObjectByType<GameOverView>();
        scam = UnityEngine.Object.FindFirstObjectByType<ScamRoute>();
        timeline = UnityEngine.Object.FindFirstObjectByType<TimelineView>();
        Assert.IsNotNull(day); Assert.IsNotNull(dialog); Assert.IsNotNull(player);
        Assert.IsNotNull(gameOver); Assert.IsNotNull(scam); Assert.IsNotNull(timeline);
    }

    [UnityTest, Timeout(400000)]
    public IEnumerator PlaysToAnEnding([Values(0, 1, 2)] int policy)
    {
        PrepareScene();
        yield return new EnterPlayMode();
        FindObjects();
        var rng = new System.Random(policy);

        day.minutesPerSecond = 0;
        if (policy == 0) yield return FeatureChecks();
        day.minutesPerSecond = 60f;   // 一秒過一小時，三天約 30 秒

        float timeout = Time.realtimeSinceStartup + 240f;
        string lastTitle = "";
        int deaths = 0, days = 0;
        var seen = new System.Collections.Generic.List<string>();
        while (!timeline.IsOpen && !(dialog.IsOpen && dialog.titleText.text.StartsWith("結局")))
        {
            Assert.Less(Time.realtimeSinceStartup, timeout, "卡住了，最後的對話框：" + lastTitle);
            if (gameOver.IsOpen)
            {
                // 前兩次重來這一天，第三次走廣告（詐騙支線）
                deaths++;
                if (deaths <= 2) gameOver.Retry(); else gameOver.Ad();
            }
            else if (scam.AdShowing) scam.AcceptAd();
            else if (dialog.IsOpen)
            {
                lastTitle = dialog.titleText.text;
                if (lastTitle.Contains("・下班")) days++;
                if (!lastTitle.StartsWith("後來")) seen.Add(lastTitle);
                Button[] shown = Array.FindAll(dialog.optionButtons, b => b.gameObject.activeSelf);
                int pick = policy == 0 ? 0 : policy == 1 ? shown.Length - 1 : rng.Next(shown.Length);
                shown[pick].onClick.Invoke();
            }
            else if (scam.Active && scam.NeedsTyping) typing.TypeKey(5);
            else if (day.PendingPlace != null) { yield return WalkInto(player, day.PendingPlace); continue; }
            yield return null;
        }

        if (timeline.IsOpen)
        {
            Assert.GreaterOrEqual(days, 2, "三天制：應該經過週一、週三下班");
            yield return null;
            Snap("timeline" + policy);
            Debug.Log($"[Playthrough] policy {policy} 時間軸：{day.Records.Count} 筆、{timeline.LinkCount} 條紅線、被開除 {deaths} 次");
            timeline.Continue();
        }
        Assert.IsTrue(dialog.IsOpen);
        StringAssert.StartsWith("結局", dialog.titleText.text);
        Debug.Log($"[Playthrough] policy {policy} → {dialog.titleText.text}（技術債 {day.TechDebt}，被開除 {deaths} 次）：" + string.Join(" / ", seen));
        yield return new ExitPlayMode();
    }

    // 被開除 → 看廣告 → 詐騙園區三段程式 → 選擇求救 → 警察攻堅 → 165 結局
    [UnityTest, Timeout(200000)]
    public IEnumerator ScamRouteReachesAntiFraudEnding()
    {
        PrepareScene();
        yield return new EnterPlayMode();
        FindObjects();
        day.minutesPerSecond = 0;
        yield return null;
        dialog.optionButtons[0].onClick.Invoke();   // 打卡上班

        day.GameOver("當場開除", "測試：選到死罪選項。");
        yield return null;
        Assert.IsTrue(gameOver.IsOpen, "沒有出現 Game Over 畫面");
        Snap("gameover");
        gameOver.Ad();
        yield return null;
        Assert.IsTrue(scam.AdShowing, "按了看廣告卻沒有廣告");
        Snap("scam_ad");
        scam.AcceptAd();

        float timeout = Time.realtimeSinceStartup + 60f;
        bool snapped = false;
        while (!(dialog.IsOpen && dialog.titleText.text.StartsWith("結局")))
        {
            Assert.Less(Time.realtimeSinceStartup, timeout, "詐騙支線卡住了");
            if (dialog.IsOpen) dialog.optionButtons[0].onClick.Invoke();   // 第一個選項＝偷偷求救
            else if (scam.NeedsTyping)
            {
                if (!snapped)
                {
                    yield return WalkInto(player, "Desk");
                    typing.Open();
                    typing.TypeKey(60);
                    yield return null;
                    Snap("scam_typing");
                    snapped = true;
                }
                typing.TypeKey(5);
            }
            yield return null;
        }
        Snap("scam_ending");
        StringAssert.Contains("被救回來", dialog.titleText.text);
        StringAssert.Contains("165", dialog.bodyText.text);
        yield return new ExitPlayMode();
    }

    // 只在 policy 0 跑一次：截圖、鏡頭撞牆、打字、長行截斷、摸魚 App、主管查哨、待辦
    IEnumerator FeatureChecks()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        Snap("face", ui: false);   // 景深會連 UI 一起糊（只有截圖時 UI 才在 3D 裡），所以特寫只拍場景
        dialog.optionButtons[0].onClick.Invoke();
        yield return new WaitForSecondsRealtime(1.5f);
        Snap("follow");
        // 從高處看整間辦公室：同事、主管、機房（相機位置當下設定、立刻拍，Cinemachine 來不及改）
        Transform camT = Camera.main.transform;
        camT.position = new Vector3(-3f, 4.2f, -9.5f);
        camT.LookAt(new Vector3(-4f, 0.6f, -1.5f));
        Snap("office");

        // 鏡頭撞牆：角色背對北牆站在牆邊，鏡頭要被拉回牆內（北牆在 z = 7）
        CharacterController cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.SetPositionAndRotation(new Vector3(-2f, 0, 6.3f), Quaternion.Euler(0, 180, 0));
        cc.enabled = true;
        yield return new WaitForSecondsRealtime(2f);
        Snap("wall");
        Vector3 chest = player.transform.position + Vector3.up * 1.2f;
        Assert.IsFalse(Physics.Linecast(chest, Camera.main.transform.position, 1 << 8), "角色靠牆時，鏡頭和角色之間被牆擋住了");
        Assert.Greater(Vector3.Distance(chest, Camera.main.transform.position), 1.5f, "鏡頭貼在角色身上了");

        // 打程式碼：走到座位 → 出現「按 E」提示 → 打 100 下，進度要上升；長行要被截斷
        yield return WalkInto(player, "Desk");
        Assert.IsTrue(typing.hint.activeSelf, "站在座位卻沒有「按 E 開始工作」");
        int before = day.Progress;
        typing.Open();
        for (int k = 0; k < 100; k++) typing.TypeKey(4);
        yield return null;
        Snap("typing");
        Assert.Greater(day.Progress, before, "打字沒有讓進度上升");
        Assert.IsTrue(typing.codeText.text.Contains("class"), "螢幕上沒有打出程式碼");
        StringAssert.EndsWith("…", TypingTerminal.Clip(new string('x', 120)), "太長的行沒有被截斷");

        // 摸魚：按 Tab 抽一個 App；主管走到身後 → 螢幕浮出人影 → 被抓
        SlackOff slack = UnityEngine.Object.FindFirstObjectByType<SlackOff>();
        UnityEngine.AI.NavMeshAgent boss = GameObject.Find("Boss").GetComponent<UnityEngine.AI.NavMeshAgent>();
        boss.Warp(player.transform.position - Vector3.forward * 1.6f);
        yield return null;
        yield return null;
        Assert.Greater(slack.reflection.color.a, 0.3f, "主管就在身後，螢幕卻沒有反光人影");
        Assert.IsFalse(dialog.IsOpen, "沒在摸魚卻被主管抓");
        slack.Open();   // 等同按 Tab
        Assert.IsFalse(string.IsNullOrEmpty(slack.memeTitle.text), "摸魚畫面沒有抽到內容");
        Snap("slacking");
        float until = Time.realtimeSinceStartup + 3f;
        while (!dialog.IsOpen && Time.realtimeSinceStartup < until) yield return null;
        Assert.IsTrue(dialog.IsOpen && dialog.titleText.text.Contains("主管站在你身後"), "摸魚時主管在身後卻沒有被抓");
        dialog.optionButtons[0].onClick.Invoke();
        Assert.IsFalse(slack.IsSlacking);
        boss.Warp(new Vector3(4.5f, 0, -5.6f));   // 把主管送走，後面的試玩才不受影響

        // 待辦倒數：加一件機房跳電，走進機房就完成
        TaskBoard tasks = UnityEngine.Object.FindFirstObjectByType<TaskBoard>();
        tasks.Add("server-run");
        yield return null;
        Assert.AreEqual(1, tasks.Count);
        Snap("tasks");
        yield return WalkInto(player, "Server");
        Assert.AreEqual(0, tasks.Count, "走進機房，機房待辦沒有完成");
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

    // 主鏡頭畫面存成 專案資料夾/screenshots/<名稱>.png
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
