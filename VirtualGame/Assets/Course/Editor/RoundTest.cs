using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// 闖關版的自動試玩：三天 12 關全部走完（地圖 → 對話選擇 → 螢幕小遊戲 → 結果 → 下班談話 → 結局），沿路截圖
// policy 0：每一關都玩得很好；policy 1：玩得慢、亂選，可能失敗跳關
public class RoundTest
{
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1;
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
    }

    [UnityTest, Timeout(900000)]
    public IEnumerator PlaysAllLevels([Values(0, 1)] int policy)
    {
        EditorSceneManager.OpenScene("Assets/Course/Scenes/Round1.unity");
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        yield return new EnterPlayMode();

        CampaignDirector d = Object.FindFirstObjectByType<CampaignDirector>();
        Assert.IsNotNull(d);
        foreach (SkinnedMeshRenderer smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None)) smr.updateWhenOffscreen = true;
        Time.timeScale = 4;   // 加速，不影響規則
        var rng = new System.Random(policy + 7);
        var snapped = new HashSet<string>();
        faceChecks = 0;
        faceProblems.Clear();
        lastFaceFail = null;
        float until = Time.realtimeSinceStartup + 600f;
        int games = 0, picks = 0;
        float nextAuto = 0;

        while (d.Now != CampaignDirector.Phase.Ending)
        {
            Assert.Less(Time.realtimeSinceStartup, until, "卡住了，最後的階段：" + d.Now);
            MonitorGame g = d.CurrentGame;
            if (g != null && g.Running)
            {
                if (policy == 0 && snapped.Add("game_" + g.GetType().Name + (g is SortGame s && s.phishing ? "_phish" : "")))
                {
                    yield return new WaitForSecondsRealtime(0.4f);
                    Snap("campaign_" + g.Title);
                    if (snapped.Add("headcheck")) HeadDoesNotCoverMonitor();
                }
                if (Time.realtimeSinceStartup >= nextAuto) { g.AutoPlay(); nextAuto = Time.realtimeSinceStartup + (policy == 0 ? 0f : 0.5f); }
                yield return null;
                continue;
            }
            if (d.WaitingChoice)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                if (policy == 0 && snapped.Add("choice")) Snap("campaign_choice");
                FaceAboveSubtitle(d);
                d.Pick(rng.Next(d.ChoiceCount));
                picks++;
            }
            else if (d.WaitingContinue)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                if (policy == 0 && d.Now == CampaignDirector.Phase.Map && snapped.Add("map")) Snap("campaign_map");
                if (policy == 0 && d.Now == CampaignDirector.Phase.Talk && snapped.Add("talk")) { yield return new WaitForSecondsRealtime(0.5f); Snap("campaign_talk"); }
                if (d.Now == CampaignDirector.Phase.Talk) { yield return new WaitForSecondsRealtime(0.3f); FaceAboveSubtitle(d); }
                if (d.Now == CampaignDirector.Phase.Game) games++;
                d.Continue();
            }
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.5f);
        Snap("campaign_ending" + policy);
        Debug.Log($"[Campaign] policy {policy}：★ {d.TotalStars} / {d.MaxStars}、主管信任 {d.Trust}、選了 {picks} 次 → {d.Ending}");
        if (d.Ending == "結局：面試沒過") { Assert.AreEqual(1, policy, "玩得好卻沒通過面試"); yield return new ExitPlayMode(); yield break; }
        Assert.GreaterOrEqual(picks, 25 + 5, "每關前和每天下班都要有選擇");
        Assert.IsEmpty(faceProblems, "臉被字幕列擋住或超出畫面：\n" + string.Join("\n", faceProblems));
        Assert.Greater(faceChecks, 20, "臉和字幕列的檢查應該做很多次");
        if (policy == 0) Assert.GreaterOrEqual(d.TotalStars, d.MaxStars * 2 / 3, "玩得好卻拿不到星星，難度可能太高");
        StringAssert.StartsWith("結局", d.Ending);
        yield return new ExitPlayMode();
    }

    // 選項打亂：同一組選項洗 30 次，第一個位置至少換過人，而且元素都還在
    [Test]
    public void ChoicesAreShuffled()
    {
        var firsts = new HashSet<string>();
        var r = new System.Random(1);
        for (int i = 0; i < 30; i++)
        {
            var list = new List<string> { "誠實", "找藉口", "求助" };
            CampaignDirector.Shuffle(list, r);
            CollectionAssert.AreEquivalent(new[] { "誠實", "找藉口", "求助" }, list);
            firsts.Add(list[0]);
        }
        Assert.AreEqual(3, firsts.Count, "洗牌後第一個選項應該三個都出現過");
    }

    [Test]
    public void LevelsJsonIsConsistent()
    {
        var data = JsonUtility.FromJson<CampaignDirector.Data>(Resources.Load<TextAsset>("levels").text);
        var gamesKeys = new HashSet<string> { "bug", "diff", "load", "deploy", "triage", "phish", "backup", "password", "plan", "bisect", "firewall", "checklist", "jargon", "restart", "route",
            "robot", "variable", "binary", "array", "loop", "queue", "stack", "bubble", "tree", "cipher" };
        var speakers = new HashSet<string> { "boss", "lewis", "suzie", "martha" };
        int levels = 0;
        foreach (var day in data.days)
        {
            if (!day.interview) Assert.IsNotEmpty(day.endTalk, day.name + " 沒有下班談話");
            foreach (var lv in day.levels)
            {
                levels++;
                Assert.IsTrue(gamesKeys.Contains(lv.game), lv.id + " 的小遊戲不存在：" + lv.game);
                Assert.IsTrue(speakers.Contains(lv.speaker), lv.id + " 的說話的人不存在：" + lv.speaker);
                Assert.GreaterOrEqual(lv.choices.Length, 2, lv.id + " 選項太少");
                Assert.Greater(lv.seconds, 0, lv.id + " 沒有時間");
            }
        }
        Assert.AreEqual(25, levels, "應該有 25 關（面試 10 ＋ 上班 15）");
        Assert.IsTrue(data.days[0].interview, "第一章應該是面試");
        Assert.AreEqual(10, data.days[0].levels.Length, "面試應該有 10 題");
        // 越後面越難：難度不能往下掉
        int last = -1;
        foreach (var day in data.days) foreach (var lv in day.levels) { Assert.GreaterOrEqual(lv.diff, last, lv.id + " 比前一關簡單"); last = lv.diff; }
        var used = new HashSet<string>();
        foreach (var day in data.days) foreach (var lv in day.levels) Assert.IsTrue(used.Add(lv.game), lv.id + " 的小遊戲跟前面重複了：" + lv.game);
    }

    // 對話時，正在講話的人的臉（下巴到頭頂）要整個在字幕列上面、畫面裡面
    static int faceChecks;
    static readonly List<string> faceProblems = new List<string>();
    static string lastFaceFail;
    static void FaceAboveSubtitle(CampaignDirector d)
    {
        if (d.Focus == null || d.SubtitleTop <= 0) return;
        Camera cam = Camera.main;
        Vector3 head = HeadTop(d.Focus.GetComponentInParent<Animator>());
        float chin = cam.WorldToViewportPoint(head - Vector3.up * 0.25f).y;
        float top = cam.WorldToViewportPoint(head).y;
        var brain = cam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
        string where = $"頭頂 {head} 鏡頭 {brain.ActiveVirtualCamera?.Name}{(brain.IsBlending ? "（轉場中）" : "")} 在 {cam.transform.position}";
        faceChecks++;
        // 角色剛被搬位置的那一格，網格外框還沒更新會誤報；同一台鏡頭連續兩次有問題才算數
        string key = brain.ActiveVirtualCamera?.Name;
        if (chin > d.SubtitleTop + 0.02f && top < 1.0f) { lastFaceFail = null; return; }
        if (lastFaceFail != key) { lastFaceFail = key; return; }
        faceProblems.Add($"{d.Focus.root.name}：下巴 {chin:F2}、頭頂 {top:F2}、字幕列頂 {d.SubtitleTop:F2}，{where}");
        if (faceProblems.Count <= 4) Snap("fail_face" + faceProblems.Count);
    }

    // 頭頂的世界座標：角色網格「現在這個姿勢」的外框最高點
    // （你的掃描模型「頭骨」的位置和看到的頭對不起來，所以直接看網格外框；要先開 updateWhenOffscreen 外框才會跟著動作更新）
    static Vector3 HeadTop(Animator a)
    {
        Vector3 best = a.transform.position;
        foreach (SkinnedMeshRenderer smr in a.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Bounds b = smr.bounds;
            if (b.max.y > best.y) best = new Vector3(b.center.x, b.max.y, b.center.z);
        }
        return best;
    }

    // 頭不能擋到螢幕：把你角色的頭（半徑約 12 公分）和螢幕四個角投影到畫面上，兩者不能重疊；螢幕也要夠大
    static void HeadDoesNotCoverMonitor()
    {
        Camera cam = Camera.main;
        Vector3 headTop = HeadTop(GameObject.Find("Player").GetComponent<Animator>());
        var corners = new Vector3[4];
        ((RectTransform)GameObject.Find("Monitor UI").transform).GetWorldCorners(corners);
        Vector2 min = Vector2.one * 9, max = -Vector2.one * 9;
        foreach (Vector3 c in corners) { Vector3 v = cam.WorldToViewportPoint(c); min = Vector2.Min(min, v); max = Vector2.Max(max, v); }
        Vector3 h = cam.WorldToViewportPoint(headTop - Vector3.up * 0.12f);
        float r = Mathf.Abs(cam.WorldToViewportPoint(headTop - Vector3.up * 0.12f + cam.transform.right * 0.12f).x - h.x);
        float dx = Mathf.Max(min.x - h.x, 0, h.x - max.x), dy = Mathf.Max(min.y - h.y, 0, h.y - max.y);
        Debug.Log($"[Campaign] 螢幕在畫面上 x {min.x:F2}～{max.x:F2}、y {min.y:F2}～{max.y:F2}；頭在 ({h.x:F2}, {h.y:F2}) 半徑 {r:F2}");
        Assert.Greater(Mathf.Sqrt(dx * dx + dy * dy), r * 0.9f, "你角色的頭擋到螢幕了");
        Assert.Greater(max.x - min.x, 0.45f, "螢幕在畫面上太小，按鈕會看不清楚");
    }

    static void Snap(string name)
    {
        Camera cam = Camera.main;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases) if (c.renderMode == RenderMode.ScreenSpaceOverlay) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 0.4f; }
        var rt = new RenderTexture(960, 540, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        cam.targetTexture = null;
        RenderTexture.active = null;
        foreach (var c in canvases) if (c.renderMode == RenderMode.ScreenSpaceCamera) c.renderMode = RenderMode.ScreenSpaceOverlay;
        System.IO.Directory.CreateDirectory("../screenshots");
        System.IO.File.WriteAllBytes($"../screenshots/{name}.png", tex.EncodeToPNG());
    }
}
