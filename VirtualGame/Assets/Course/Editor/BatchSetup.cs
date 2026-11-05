using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 命令列用：Unity -batchmode -executeMethod BatchSetup.SetupAnimations
// 把 Mixamo 動作檔設成 Humanoid + Loop Time（第 9 週步驟 2）
public static class BatchSetup
{
    public static void SetupAnimations()
    {
        foreach (string path in new[] { "Assets/Course/Animation/Idle.fbx", "Assets/Course/Animation/Walking.fbx" })
        {
            ModelImporter mi = (ModelImporter)AssetImporter.GetAtPath(path);
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            ModelImporterClipAnimation[] clips = mi.defaultClipAnimations;
            foreach (var c in clips) c.loopTime = true;
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }
    }

    // 命令列用：-executeMethod BatchSetup.SetupCharacter
    // 角色設 Humanoid → 建走路測試場景 → 掛狀態機與 SimpleWalker（第 9 週交付）
    public static void SetupCharacter()
    {
        SetupAnimations();
        const string fbx = "Assets/Course/Character/Player.fbx";
        ModelImporter mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
        mi.animationType = ModelImporterAnimationType.Human;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.SaveAndReimport();

        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbx);
        Debug.Log("[Batch] Player avatar human=" + (avatar && avatar.isHuman) + " valid=" + (avatar && avatar.isValid));

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3, 1, 3);

        GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        hero.name = "Player";
        // FBX 轉兩手後內嵌貼圖會掉，改用獨立貼圖做 URP 材質
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Course/Character/Player_BaseColor.png"));
        mat.SetFloat("_Smoothness", 0.2f);
        AssetDatabase.CreateAsset(mat, "Assets/Course/Character/Player.mat");
        foreach (Renderer r in hero.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
        Selection.activeGameObject = hero;
        CourseSceneBuilder.SetupWalker();

        Camera cam = Camera.main;
        cam.transform.position = new Vector3(0, 2.2f, -4f);
        cam.transform.rotation = Quaternion.Euler(15, 0, 0);
        cam.gameObject.AddComponent<CameraController>().player = hero;

        if (!AssetDatabase.IsValidFolder("Assets/Course/Scenes")) AssetDatabase.CreateFolder("Assets/Course", "Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Course/Scenes/Week9_Walk.unity");
    }
}
