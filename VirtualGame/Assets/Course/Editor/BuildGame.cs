using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 打包成 Windows 執行檔（給 Moodle 繳交或給別人玩）：輸出到專案資料夾的 Build/工程師的一天/
// 命令列：-executeMethod BuildGame.Windows（Unity 要先關掉）；或選單 Course/6.
public static class BuildGame
{
    [MenuItem("Course/6. 打包 Windows 執行檔")]
    public static void Windows()
    {
        PlayerSettings.productName = "工程師的一天";
        PlayerSettings.companyName = "Jason";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        BuildReport r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Course/Scenes/Round1.unity" },
            locationPathName = "../Build/工程師的一天/工程師的一天.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });
        Debug.Log($"[Course] 打包結果：{r.summary.result}，大小 {r.summary.totalSize / 1048576} MB");
        if (r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
