using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// 試玩關卡的自動試玩：開場 → 螢幕抓蟲 → 走到主管旁邊 → 三回合對話 → 結果；沿路截圖
public class RoundTest
{
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
    }

    [UnityTest, Timeout(200000)]
    public IEnumerator PlaysOneRound([Values(0, 1)] int policy)
    {
        EditorSceneManager.OpenScene("Assets/Course/Scenes/Round1.unity");
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        yield return new EnterPlayMode();

        RoundDirector d = Object.FindFirstObjectByType<RoundDirector>();
        MonitorBugHunt hunt = Object.FindFirstObjectByType<MonitorBugHunt>();
        Assert.IsNotNull(d); Assert.IsNotNull(hunt);
        yield return new WaitForSecondsRealtime(1f);
        if (policy == 0) Snap("round_intro");
        Assert.AreEqual(RoundDirector.Phase.Intro, d.Now);
        d.Continue();   // 開始工作
        yield return new WaitForSecondsRealtime(2f);
        Assert.AreEqual(RoundDirector.Phase.Work, d.Now);
        Assert.IsTrue(hunt.Running, "螢幕上的抓蟲沒有開始");
        if (policy == 0) Snap("round_desk");

        // policy 0 全部抓完；policy 1 只抓 5 隻，等時間到
        int target = policy == 0 ? 99 : 5;
        float until = Time.realtimeSinceStartup + 40f;
        while (hunt.Running && Time.realtimeSinceStartup < until)
        {
            if (hunt.Caught < target) hunt.SquashOne();
            yield return new WaitForSecondsRealtime(0.1f);
        }
        Assert.IsFalse(hunt.Running, "抓蟲沒有結束");

        until = Time.realtimeSinceStartup + 20f;
        while (d.Now != RoundDirector.Phase.Talk && Time.realtimeSinceStartup < until) yield return null;
        Assert.AreEqual(RoundDirector.Phase.Talk, d.Now, "沒有走到談話");
        yield return new WaitForSecondsRealtime(1.2f);
        if (policy == 0) Snap("round_boss");

        until = Time.realtimeSinceStartup + 60f;
        int lines = 0;
        bool snappedPlayer = false;
        while (d.Now == RoundDirector.Phase.Talk && Time.realtimeSinceStartup < until)
        {
            if (d.WaitingChoice) { yield return new WaitForSecondsRealtime(0.5f); if (policy == 0 && lines == 1) Snap("round_choice"); d.Pick(policy == 0 ? 0 : 1); }
            else if (d.WaitingContinue)
            {
                yield return new WaitForSecondsRealtime(0.9f);
                if (policy == 0 && !snappedPlayer && lines == 1) { Snap("round_player"); snappedPlayer = true; }
                d.Continue(); lines++;
            }
            yield return null;
        }
        Assert.AreEqual(RoundDirector.Phase.Result, d.Now, "對話沒有走完");
        yield return new WaitForSecondsRealtime(1f);
        Snap("round_result" + policy);
        Debug.Log($"[Round] policy {policy}：抓到 {hunt.Caught} 隻、主管信任 {d.Trust}、台詞 {lines} 句");
        if (policy == 0) Assert.Greater(d.Trust, 60); else Assert.Less(d.Trust, 50);
        yield return new ExitPlayMode();
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
