using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 上方選單 Course/... 一鍵建立課程場景
public static class CourseSceneBuilder
{
    const string Root = "Assets/Course";

    // ───────────────────────── Roll a Ball ─────────────────────────
    [MenuItem("Course/1. 建立 Roll a Ball 場景")]
    public static void BuildRollABall()
    {
        if (!CheckTmp()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(Root, "Materials");
        EnsureFolder(Root, "Prefabs");
        EnsureFolder(Root, "Scenes");
        EnsureTag("PickUp");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Material bgMat     = MakeMat("background", new Color32(128, 128, 128, 255), 0.25f);
        Material playerMat = MakeMat("player",     new Color32(0, 140, 255, 255), 0.5f);
        Material wallMat   = MakeMat("wall",       new Color32(90, 60, 40, 255), 0.1f);
        Material pickupMat = MakeMat("pickup",     new Color32(255, 200, 0, 255), 0.5f);

        // 地板（Plane 預設 10x10）
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.GetComponent<Renderer>().sharedMaterial = bgMat;

        // 玩家
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player";
        player.transform.position = new Vector3(0, 0.5f, 0);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        player.AddComponent<Rigidbody>();
        PlayerController pc = player.AddComponent<PlayerController>();

        // 四面牆
        GameObject walls = new GameObject("Walls");
        MakeWall("N wall", new Vector3(0, 0.5f, 5.25f),  new Vector3(11, 1, 0.5f), walls, wallMat);
        MakeWall("S wall", new Vector3(0, 0.5f, -5.25f), new Vector3(11, 1, 0.5f), walls, wallMat);
        MakeWall("E wall", new Vector3(5.25f, 0.5f, 0),  new Vector3(0.5f, 1, 10), walls, wallMat);
        MakeWall("W wall", new Vector3(-5.25f, 0.5f, 0), new Vector3(0.5f, 1, 10), walls, wallMat);

        // PickUp prefab：Trigger + Kinematic（不受物理引擎控制，效能最好）
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temp.name = "PickUp";
        temp.tag = "PickUp";
        temp.transform.rotation = Quaternion.Euler(45, 45, 45);
        temp.transform.localScale = Vector3.one * 0.5f;
        temp.GetComponent<Renderer>().sharedMaterial = pickupMat;
        temp.GetComponent<BoxCollider>().isTrigger = true;
        Rigidbody prb = temp.AddComponent<Rigidbody>();
        prb.useGravity = false;
        prb.isKinematic = true;
        temp.AddComponent<Rotator>();
        GameObject pickupPrefab = PrefabUtility.SaveAsPrefabAsset(temp, Root + "/Prefabs/PickUp.prefab");
        Object.DestroyImmediate(temp);

        // 生成器
        GameObject gen = new GameObject("PickUpGen");
        RandomGen rg = gen.AddComponent<RandomGen>();
        rg.pickUpPrefab = pickupPrefab;

        // 相機與光
        Camera cam = Camera.main;
        cam.transform.position = new Vector3(0, 5, -10);
        cam.transform.rotation = Quaternion.Euler(25, 0, 0);
        cam.gameObject.AddComponent<CameraController>().player = player;

        Light light = Object.FindObjectOfType<Light>();
        if (light != null)
        {
            light.transform.rotation = Quaternion.Euler(50, 50, 0);
            light.color = Color.white;
        }

        // UI
        Canvas canvas = MakeCanvas();
        TextMeshProUGUI countText = MakeText("CountText", canvas.transform, "Count: 0", 36,
            new Vector2(0, 1), new Vector2(10, -10), new Vector2(400, 60), TextAlignmentOptions.TopLeft);
        TextMeshProUGUI winText = MakeText("WinText", canvas.transform, "You WIN!", 96,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 200), TextAlignmentOptions.Center);
        winText.color = new Color32(255, 220, 0, 255);

        pc.countText = countText;
        pc.winTextObject = winText.gameObject;
        pc.spawner = gen;

        string path = Root + "/Scenes/RollABall.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[Course] Roll a Ball 場景完成：" + path + "，按 ▶ Play 用 WASD/方向鍵 玩");
    }

    // ───────────────────────── 第 8 週互動 ─────────────────────────
    [MenuItem("Course/2. 把點選互動加到目前場景（先在 Hierarchy 選取物件）")]
    public static void AddInteraction()
    {
        if (!CheckTmp()) return;

        List<GameObject> targets = new List<GameObject>(Selection.gameObjects);
        if (targets.Count == 0)
        {
            if (!EditorUtility.DisplayDialog("Course",
                "Hierarchy 裡沒有選取物件。\n要建立 3 個示範物件嗎？\n（之後可以換成你的掃描模型：選取它們再按一次這個選單）",
                "建立示範物件", "取消")) return;

            targets.Add(Demo(PrimitiveType.Cube,     "示範方塊", new Vector3(-2, 0.5f, 3)));
            targets.Add(Demo(PrimitiveType.Sphere,   "示範球",   new Vector3(0, 0.5f, 3)));
            targets.Add(Demo(PrimitiveType.Cylinder, "示範圓柱", new Vector3(2, 1f, 3)));
        }

        foreach (GameObject go in targets)
        {
            Undo.RegisterFullObjectHierarchyUndo(go, "Add InfoTag");
            if (go.GetComponentInChildren<Collider>() == null) FitBoxCollider(go);
            InfoTag tag = go.GetComponent<InfoTag>();
            if (tag == null) tag = go.AddComponent<InfoTag>();
            if (tag.displayName == "物件名稱") tag.displayName = go.name;
        }

        // Selector + 資訊面板（只建立一次）
        if (Object.FindObjectOfType<ClickSelector>() == null)
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) canvas = MakeCanvas();

            GameObject panel = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            RectTransform prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1, 1);
            prt.anchoredPosition = new Vector2(-20, -20);
            prt.sizeDelta = new Vector2(420, 220);

            TextMeshProUGUI title = MakeText("Title", panel.transform, "名稱", 36,
                new Vector2(0, 1), new Vector2(16, -12), new Vector2(390, 50), TextAlignmentOptions.TopLeft);
            TextMeshProUGUI desc = MakeText("Description", panel.transform, "描述", 24,
                new Vector2(0, 1), new Vector2(16, -70), new Vector2(390, 140), TextAlignmentOptions.TopLeft);
            title.color = Color.yellow;

            GameObject selectorGo = new GameObject("Selector");
            ClickSelector sel = selectorGo.AddComponent<ClickSelector>();
            sel.infoPanel = panel;
            sel.titleText = title;
            sel.descText = desc;
            Undo.RegisterCreatedObjectUndo(selectorGo, "Create Selector");
            Undo.RegisterCreatedObjectUndo(panel, "Create InfoPanel");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.objects = targets.ToArray();
        Debug.Log("[Course] 已設定 " + targets.Count + " 個可點選物件。到 Inspector 的 InfoTag 填名稱與描述，然後 ▶ Play 點點看。");
        Debug.LogWarning("[Course] 提醒：中文要正常顯示，TMP 需要中文字型（見 README）。");
    }

    // ───────────────────────── 第 9 週角色 ─────────────────────────
    [MenuItem("Course/3. 角色接上 Idle／Walking 狀態機（先在 Hierarchy 選取角色）")]
    public static void SetupWalker()
    {
        GameObject hero = Selection.activeGameObject;
        if (hero == null || hero.scene.name == null)
        {
            EditorUtility.DisplayDialog("Course", "先把角色 FBX 拖進場景，在 Hierarchy 選取它再按這個選單。", "OK");
            return;
        }

        // Mixamo 下載的檔名含 Idle / Walking，動作本身在 FBX 裡叫 mixamo.com，所以用檔名找
        AnimationClip idle = FindClip("idle");
        AnimationClip walk = FindClip("walk");
        if (idle == null || walk == null)
        {
            EditorUtility.DisplayDialog("Course",
                "找不到動作檔。\n請把 Mixamo 下載的 Idle.fbx、Walking.fbx 拖進 Assets（檔名要含 Idle / Walk）。", "OK");
            return;
        }

        // 只提醒不代改：Humanoid 和 Loop Time 是本週要自己設定的重點
        foreach (AnimationClip c in new[] { idle, walk })
        {
            string p = AssetDatabase.GetAssetPath(c);
            ModelImporter mi = AssetImporter.GetAtPath(p) as ModelImporter;
            if (mi != null && mi.animationType != ModelImporterAnimationType.Human)
                Debug.LogWarning("[Course] " + p + " 的 Rig 不是 Humanoid → 角色會維持 T-pose 不動");
            if (!AnimationUtility.GetAnimationClipSettings(c).loopTime)
                Debug.LogWarning("[Course] " + p + " 沒勾 Loop Time → 走三步就停住");
        }

        EnsureFolder(Root, "Animation");
        string path = Root + "/Animation/HeroAnimator.controller";
        AssetDatabase.DeleteAsset(path);
        var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("isWalking", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;
        var idleState = sm.AddState("Idle");       // 第一個加入的是起始狀態（橘色）
        idleState.motion = idle;
        var walkState = sm.AddState("Walking");
        walkState.motion = walk;

        // 取消 Has Exit Time：放開 W 立刻停；Duration 0.2：站 → 走有過渡
        var toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.2f;
        toWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "isWalking");
        var toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.2f;
        toIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "isWalking");

        Undo.RegisterFullObjectHierarchyUndo(hero, "Setup Walker");
        Animator anim = hero.GetComponent<Animator>();
        if (anim == null) anim = hero.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;              // 移動交給程式
        if (hero.GetComponent<SimpleWalker>() == null) hero.AddComponent<SimpleWalker>();
        if (anim.avatar == null || !anim.avatar.isHuman)
            Debug.LogWarning("[Course] 角色的 Avatar 不是 Humanoid → 到角色 FBX 的 Rig 頁籤改 Humanoid + Create From This Model");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(hero.scene);
        Debug.Log("[Course] 狀態機完成：" + path + "。▶ Play 後 W 走、A/D 轉；腳在滑就調 SimpleWalker 的 moveSpeed");
    }

    static AnimationClip FindClip(string keyword)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Model"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileNameWithoutExtension(p).ToLower().Contains(keyword)) continue;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(p))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) return c;
        }
        return null;
    }

    // ───────────────────────── 工具函數 ─────────────────────────
    static bool CheckTmp()
    {
        if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return true;
        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        EditorUtility.DisplayDialog("Course", "需要先匯入 TMP Essential Resources。\n匯入完成後，再按一次剛剛的選單。", "OK");
        return false;
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent))
        {
            string p = Path.GetDirectoryName(parent).Replace('\\', '/');
            AssetDatabase.CreateFolder(p, Path.GetFileName(parent));
        }
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    static void EnsureTag(string tag)
    {
        SerializedObject tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tm.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tm.ApplyModifiedProperties();
    }

    static Material MakeMat(string name, Color color, float smoothness)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material m = new Material(shader);
        m.color = color;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void MakeWall(string name, Vector3 pos, Vector3 scale, GameObject parent, Material mat)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.SetParent(parent.transform);
        w.transform.position = pos;
        w.transform.localScale = scale;
        w.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Canvas MakeCanvas()
    {
        GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
        return canvas;
    }

    static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size,
        Vector2 anchor, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        return t;
    }

    static GameObject Demo(PrimitiveType type, string name, Vector3 pos)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        Undo.RegisterCreatedObjectUndo(go, "Create Demo");
        return go;
    }

    // 掃描模型通常沒有 Collider：依照所有子 Renderer 的範圍自動包一個 Box Collider
    static void FitBoxCollider(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        BoxCollider bc = go.AddComponent<BoxCollider>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        Vector3 s = go.transform.lossyScale;
        bc.center = go.transform.InverseTransformPoint(b.center);
        bc.size = new Vector3(b.size.x / s.x, b.size.y / s.y, b.size.z / s.z);
    }
}
