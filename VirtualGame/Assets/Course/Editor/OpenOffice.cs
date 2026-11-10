using UnityEditor.SceneManagement;

// 開 Unity 時直接打開辦公室場景：Unity.exe -projectPath VirtualGame -executeMethod OpenOffice.Open
public static class OpenOffice
{
    public static void Open() => EditorSceneManager.OpenScene("Assets/Course/Scenes/Office.unity");
}
