using UnityEditor.SceneManagement;

// 開 Unity 時直接打開場景：Unity.exe -projectPath VirtualGame -executeMethod OpenOffice.OpenRound
public static class OpenOffice
{
    public static void Open() => EditorSceneManager.OpenScene("Assets/Course/Scenes/Office.unity");
    public static void OpenRound() => EditorSceneManager.OpenScene("Assets/Course/Scenes/Round1.unity");
}
