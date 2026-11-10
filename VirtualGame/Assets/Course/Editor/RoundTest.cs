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
        Time.timeScale = 4;   // 加速，不影響規則
        var rng = new System.Random(policy + 7);
        var snapped = new HashSet<string>();
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
                }
                if (Time.realtimeSinceStartup >= nextAuto) { g.AutoPlay(); nextAuto = Time.realtimeSinceStartup + (policy == 0 ? 0f : 0.5f); }
                yield return null;
                continue;
            }
            if (d.WaitingChoice)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                if (policy == 0 && snapped.Add("choice")) Snap("campaign_choice");
                d.Pick(rng.Next(d.ChoiceCount));
                picks++;
            }
            else if (d.WaitingContinue)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                if (policy == 0 && d.Now == CampaignDirector.Phase.Map && snapped.Add("map")) Snap("campaign_map");
                if (policy == 0 && d.Now == CampaignDirector.Phase.Talk && snapped.Add("talk")) { yield return new WaitForSecondsRealtime(0.5f); Snap("campaign_talk"); }
                if (d.Now == CampaignDirector.Phase.Game) games++;
                d.Continue();
            }
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.5f);
        Snap("campaign_ending" + policy);
        Debug.Log($"[Campaign] policy {policy}：★ {d.TotalStars} / 36、主管信任 {d.Trust}、選了 {picks} 次 → {d.Ending}");
        Assert.GreaterOrEqual(picks, 12 + 5, "每關前和每天下班都要有選擇");
        if (policy == 0) Assert.GreaterOrEqual(d.TotalStars, 24, "玩得好卻拿不到星星，難度可能太高");
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
        var gamesKeys = new HashSet<string> { "bug", "diff", "load", "deploy", "triage", "phish" };
        var speakers = new HashSet<string> { "boss", "lewis", "suzie", "martha" };
        int levels = 0;
        foreach (var day in data.days)
        {
            Assert.IsNotEmpty(day.endTalk, day.name + " 沒有下班談話");
            foreach (var lv in day.levels)
            {
                levels++;
                Assert.IsTrue(gamesKeys.Contains(lv.game), lv.id + " 的小遊戲不存在：" + lv.game);
                Assert.IsTrue(speakers.Contains(lv.speaker), lv.id + " 的說話的人不存在：" + lv.speaker);
                Assert.GreaterOrEqual(lv.choices.Length, 2, lv.id + " 選項太少");
                Assert.Greater(lv.seconds, 0, lv.id + " 沒有時間");
            }
        }
        Assert.AreEqual(12, levels, "應該有 12 關");
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
