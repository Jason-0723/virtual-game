using System.Linq;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// 《工程師的一天》辦公室：暖色調、紙張質感 UI（參考出勤卡設計）
// 選單 Course/4. 建立辦公室遊戲場景；命令列 -executeMethod OfficeBuilder.Build
public static class OfficeBuilder
{
    const string ScenePath = "Assets/Course/Scenes/Office.unity";
    static Font font;

    // 配色：紙、墨、森林綠、海軍藍、磚紅、琥珀
    static readonly Color Paper = Hex("#efebe2"), Cream = Hex("#f4efe4"), Ink = Hex("#1f201d"), Dim = Hex("#55564f");
    static readonly Color Green = Hex("#1e3a2d"), Navy = Hex("#1a2033"), Red = Hex("#b5442c"), Amber = Hex("#c08a2e"), Line = Hex("#c9c2b2");

    [MenuItem("Course/4. 建立辦公室遊戲場景（工程師的一天）")]
    public static void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // 動態字型，中文會借用系統字型
        WriteTypingCode();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── 材質 ──
        Material floor = Mat("Office_Floor", Hex("#b8a587"), 0.15f);
        Material wall = Mat("Office_Wall", Hex("#efe9dc"), 0.05f);
        Material accent = Mat("Office_Accent", Green, 0.05f);
        Material wood = Mat("Office_Wood", Hex("#c9a77c"), 0.35f);
        Material dark = Mat("Office_Dark", Hex("#2a2b2e"), 0.4f);
        Material screen = Mat("Office_Screen", Hex("#1d3b5c"), 0.8f, Hex("#3d7fc4") * 1.6f);
        Material glass = Mat("Office_Glass", new Color(0.75f, 0.88f, 0.95f, 0.35f), 0.9f, default, true);
        Material window = Mat("Office_Window", Hex("#cfe6f5"), 0.9f, Hex("#fff3d6") * 1.2f);
        Material chair = Mat("Office_Chair", Navy, 0.2f);
        Material pot = Mat("Office_Pot", Hex("#b5652c"), 0.1f);
        Material leaf = Mat("Office_Leaf", Hex("#3f6b3a"), 0.1f);
        Material mine = Mat("Office_MyDesk", Hex("#e7c56a"), 0.35f);
        Material board = Mat("Office_Board", Color.white, 0.6f);
        Material marker = Mat("Office_Marker", Hex("#ffd34d"), 0.2f, Hex("#ffc21a") * 3f);

        // ── 格局（公尺，20×14）──
        Box("Floor", new Vector3(0, -0.05f, 0), new Vector3(20, 0.1f, 14), floor);
        Box("Hallway", new Vector3(0, -0.07f, 0), new Vector3(60, 0.1f, 50), Mat("Office_Hallway", Hex("#4a443c"), 0.2f));   // 牆外，鏡頭照到才不會是一片空
        Box("Wall N", new Vector3(0, 1.5f, 7), new Vector3(20, 3, 0.2f), wall);
        Box("Wall N Accent", new Vector3(-5, 0.6f, 6.88f), new Vector3(10, 1.2f, 0.05f), accent);
        Box("Wall S", new Vector3(0, 0.4f, -7), new Vector3(20, 0.8f, 0.2f), wall);        // 南牆矮，鏡頭看得進來
        Box("Wall E", new Vector3(10, 1.5f, 0), new Vector3(0.2f, 3, 14), wall);
        Box("Wall W", new Vector3(-10, 1.5f, 0), new Vector3(0.2f, 3, 14), wall);
        for (int i = 0; i < 4; i++) Box("Window", new Vector3(-9.88f, 1.7f, -4.5f + i * 3), new Vector3(0.05f, 1.4f, 2.2f), window);

        // 工位區：3×2，前排最左的黃色桌子是你的
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < 3; c++)
            {
                Vector3 p = new Vector3(-7 + c * 2.5f, 0, -3 + r * 3f);
                bool isMine = r == 0 && c == 0;
                Desk(isMine ? "My Desk" : "Desk", p, isMine ? mine : wood, dark, screen, chair);
                if (!isMine && (r + c) % 2 == 0) Box("Mug", p + new Vector3(0.55f, 0.82f, -0.1f), new Vector3(0.08f, 0.12f, 0.08f), pot);
            }
        Box("Partition", new Vector3(-4.5f, 0.6f, -1.5f), new Vector3(7.4f, 1.2f, 0.06f), accent);
        Zone("Desk", "你的座位（黃色桌子）", new Vector3(-7, 1, -4.1f), new Vector3(2, 2, 1.6f), marker);

        // 會議室：右上角玻璃房，門在西側
        Box("Meeting Glass S", new Vector3(6.5f, 1.5f, 2), new Vector3(7, 3, 0.06f), glass);
        Box("Meeting Glass W", new Vector3(3, 1.5f, 5.5f), new Vector3(0.06f, 3, 3), glass);
        Box("Meeting Table", new Vector3(6.5f, 0.74f, 4.5f), new Vector3(3, 0.06f, 1.4f), wood);
        Box("Meeting Table Base", new Vector3(6.5f, 0.37f, 4.5f), new Vector3(0.4f, 0.74f, 0.4f), dark);
        for (int i = 0; i < 3; i++)
        {
            Chair(new Vector3(5.5f + i, 0, 3.4f), chair, 0);
            Chair(new Vector3(5.5f + i, 0, 5.6f), chair, 180);
        }
        Box("Whiteboard", new Vector3(6.5f, 1.6f, 6.86f), new Vector3(3, 1.2f, 0.04f), board);
        Zone("Meeting", "會議室（右上角玻璃房）", new Vector3(6, 1, 4.5f), new Vector3(5, 2, 4), marker);

        // PM 座位：右下角
        Desk("PM Desk", new Vector3(7, 0, -4.5f), wood, dark, screen, chair);
        Plant(new Vector3(8.6f, 0, -5.8f), pot, leaf, 1.2f);
        Zone("PM", "PM 的座位（右下角）", new Vector3(7, 1, -5.6f), new Vector3(2.4f, 2, 1.4f), marker);

        // 茶水間：左上角
        Box("Pantry Counter", new Vector3(-8, 0.45f, 6.4f), new Vector3(3.4f, 0.9f, 0.8f), wood);
        Box("Coffee Machine", new Vector3(-8.8f, 1.15f, 6.4f), new Vector3(0.45f, 0.5f, 0.4f), dark);
        Box("Fridge", new Vector3(-6, 0.9f, 6.4f), new Vector3(0.9f, 1.8f, 0.8f), board);
        Zone("Pantry", "茶水間（左上角）", new Vector3(-7.5f, 1, 5.2f), new Vector3(3, 2, 1.6f), marker);
        Plant(new Vector3(-9.2f, 0, 3.5f), pot, leaf, 1f);
        Plant(new Vector3(2.4f, 0, -6.2f), pot, leaf, 0.9f);
        Plant(new Vector3(2.4f, 0, 2.6f), pot, leaf, 1.1f);

        // ── 光與後製 ──
        Light sun = Object.FindFirstObjectByType<Light>();
        sun.color = Hex("#ffe8c7");
        sun.intensity = 1.3f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50, -35, 0);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("#d9d2c3");
        RenderSettings.ambientEquatorColor = Hex("#a89c86");
        RenderSettings.ambientGroundColor = Hex("#5a5246");
        PostProcess();

        // ── 角色 ──
        GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Course/Character/Player.fbx"));
        hero.name = "Player";
        hero.transform.position = new Vector3(0, 0, -4.5f);
        Material skin = AssetDatabase.LoadAssetAtPath<Material>("Assets/Course/Character/Player.mat");
        foreach (Renderer rd in hero.GetComponentsInChildren<Renderer>()) rd.sharedMaterial = skin;
        CharacterController cc = hero.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 0.9f, 0); cc.height = 1.8f; cc.radius = 0.3f;
        Selection.activeGameObject = hero;
        CourseSceneBuilder.SetupWalker();
        hero.GetComponent<SimpleWalker>().moveSpeed = 2.2f;

        Camera cam = Camera.main;
        cam.transform.position = hero.transform.position + new Vector3(0, 3.4f, -4.2f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        CinemachineBrain brain = cam.gameObject.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.7f);

        // 跟隨鏡頭：角色背後斜上方，跟著角色轉向，看向胸口
        CinemachineCamera follow = VirtualCam("Follow Cam", hero.transform, 50, new Vector3(0, 3.4f, -4.2f), Vector2.zero);
        follow.LookAt = Target(hero.transform, "ChestTarget", 1.2f);
        // 臉部特寫：在角色正前方，臉放在畫面左邊三分之一，右邊留給對話框
        CinemachineCamera face = VirtualCam("Face Cam", hero.transform, 32, new Vector3(0.25f, 1.65f, 1.3f), new Vector2(-0.22f, 0));

        DayManager day = BuildUI();
        CameraDirector director = day.gameObject.AddComponent<CameraDirector>();
        director.followCam = follow;
        director.faceCam = face;
        director.dialog = day.dialog;
        director.player = hero.GetComponent<Animator>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("[Course] 辦公室場景完成：" + ScenePath + "，按 ▶ 開始上班");
    }

    // 打程式碼小遊戲的內容＝這個遊戲自己的程式（彩蛋）；.cs 不能直接放 Resources，所以合併成 txt
    static void WriteTypingCode()
    {
        const string dir = "Assets/Course/Scripts/Game/";
        var sb = new System.Text.StringBuilder();
        foreach (string f in new[] { "DayManager.cs", "TypingTerminal.cs", "EventDialog.cs", "CameraDirector.cs", "PlaceZone.cs" })
            sb.Append("// ===== ").Append(f).Append(" =====\n").Append(System.IO.File.ReadAllText(dir + f).Replace("\r", "")).Append("\n\n");
        System.IO.File.WriteAllText("Assets/Course/Resources/typing_code.txt", sb.ToString());
        AssetDatabase.ImportAsset("Assets/Course/Resources/typing_code.txt");
    }

    static CinemachineCamera VirtualCam(string name, Transform follow, float fov, Vector3 offset, Vector2 screenPos)
    {
        GameObject g = new GameObject(name);
        g.transform.position = follow.position + follow.rotation * offset;
        CinemachineCamera vc = g.AddComponent<CinemachineCamera>();
        vc.Follow = follow;
        vc.LookAt = follow;
        vc.Lens.FieldOfView = fov;
        CinemachineFollow f = g.AddComponent<CinemachineFollow>();
        f.FollowOffset = offset;
        f.TrackerSettings.BindingMode = BindingMode.LockToTargetWithWorldUp;   // 跟著角色轉，但不跟著歪
        f.TrackerSettings.PositionDamping = new Vector3(0.6f, 0.4f, 0.6f);
        f.TrackerSettings.RotationDamping = new Vector3(0, 1.2f, 0);
        CinemachineRotationComposer rc = g.AddComponent<CinemachineRotationComposer>();
        rc.Composition.ScreenPosition = screenPos;
        rc.Damping = new Vector2(0.4f, 0.4f);
        return vc;
    }

    static Transform Target(Transform parent, string name, float height)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = new Vector3(0, height, 0);
        return t;
    }

    // ───────────── UI：出勤卡＋便條紙＋紙張對話框 ─────────────
    static DayManager BuildUI()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Transform root = canvasGo.transform;

        // 出勤卡（左上）
        RectTransform card = Panel("TimeCard", root, Cream, new Vector2(0, 1), new Vector2(30, -30), new Vector2(430, 330));
        Border(card.gameObject);
        Label("Title Card", card, 30, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(24, -16), new Vector2(380, 44), Ink).text = "員工出勤卡";
        Label("Shift", card, 20, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(24, -58), new Vector2(380, 30), Dim).text = "上班 09:00 － 下班 18:00";
        Text clock = Label("Clock", card, 56, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-24, -14), new Vector2(200, 70), Red);
        clock.fontStyle = FontStyle.Bold;
        string[] names = { "進度", "壓力", "主管信任", "生活" };
        Color[] colors = { Green, Red, Navy, Amber };
        Image[] bars = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            float y = -112 - i * 52;
            Label("StatName", card, 22, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(24, y), new Vector2(110, 36), Ink).text = names[i];
            RectTransform bg = Panel("BarBg", card, Hex("#ddd5c4"), new Vector2(0, 1), new Vector2(140, y - 8), new Vector2(260, 20));
            Image fill = Panel("BarFill", bg, colors[i], new Vector2(0, 0.5f), Vector2.zero, new Vector2(260, 20)).GetComponent<Image>();
            fill.sprite = WhiteSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            bars[i] = fill;
        }

        // 任務便條紙（上方中央）
        RectTransform note = Panel("ObjectiveNote", root, Hex("#f6dc7a"), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(560, 110));
        note.localRotation = Quaternion.Euler(0, 0, -1.5f);
        Text objective = Label("Objective", note, 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 100), Ink);

        Label("Help", root, 20, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(30, 24), new Vector2(900, 36), Cream).text = "W 前進　A／D 轉向　走到發光的黃色圓圈　空檔回座位按 E 工作";

        // 對話框（紙張）
        // 對話時鏡頭會切到臉部特寫，所以紙放右邊、背景只微暗，左邊露出角色的臉
        GameObject dim = Panel("DialogPanel", root, new Color(0.08f, 0.07f, 0.06f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000)).gameObject;
        RectTransform paper = Panel("Paper", dim.transform, Paper, new Vector2(0.5f, 0.5f), new Vector2(380, 0), new Vector2(1060, 780));
        Border(paper.gameObject);
        Panel("Stripe", paper, Red, new Vector2(0, 1), Vector2.zero, new Vector2(1060, 10));

        EventDialog dlg = canvasGo.AddComponent<EventDialog>();
        dlg.panel = dim;
        dlg.titleText = Label("Title", paper, 40, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(50, -40), new Vector2(960, 60), Red);
        Panel("Rule", paper, Line, new Vector2(0, 1), new Vector2(50, -108), new Vector2(960, 2));
        dlg.bodyText = Label("Body", paper, 27, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(50, -128), new Vector2(960, 360), Ink);
        dlg.bodyText.lineSpacing = 1.15f;
        dlg.optionButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            RectTransform b = Panel("Option" + (i + 1), paper, Color.white, new Vector2(0.5f, 0), new Vector2(0, 40 + (2 - i) * 84), new Vector2(960, 70));
            Border(b.gameObject);
            Button btn = b.gameObject.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = Hex("#f6dc7a");
            cb.pressedColor = Hex("#e7c56a");
            btn.colors = cb;
            Label("Text", b, 26, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(10, 0), new Vector2(900, 66), Ink);
            dlg.optionButtons[i] = btn;
        }

        // 打程式碼小遊戲：深色編輯器畫面＋完成度條；座位旁的提示
        RectTransform hint = Panel("WorkHint", root, Navy, new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(420, 64));
        Label("Text", hint, 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 60), Cream).text = "按 E 開始工作";
        RectTransform term = Panel("Terminal", root, Hex("#1e1e1e"), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1240, 760));
        Border(term.gameObject);
        RectTransform titleBar = Panel("TitleBar", term, Hex("#323233"), new Vector2(0.5f, 1), Vector2.zero, new Vector2(1240, 44));
        Text fileText = Label("File", titleBar, 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 40), Hex("#cccccc"));
        Text code = Label("Code", term, 24, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(30, -62), new Vector2(1180, 590), Hex("#9cdcfe"));
        code.horizontalOverflow = HorizontalWrapMode.Overflow;   // 程式碼不換行，太長就切掉
        Label("Hint", term, 20, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(30, 52), new Vector2(800, 30), Hex("#8a8a8a")).text = "隨便按鍵盤就會打出程式　Esc 離開";
        RectTransform tBarBg = Panel("BarBg", term, Hex("#3c3c3c"), new Vector2(0, 0), new Vector2(30, 18), new Vector2(1180, 16));
        Image tBar = Panel("BarFill", tBarBg, Hex("#4ec9b0"), new Vector2(0, 0.5f), Vector2.zero, new Vector2(1180, 16)).GetComponent<Image>();
        tBar.sprite = WhiteSprite();
        tBar.type = Image.Type.Filled;
        tBar.fillMethod = Image.FillMethod.Horizontal;
        TypingTerminal typing = canvasGo.AddComponent<TypingTerminal>();
        typing.panel = term.gameObject;
        typing.codeText = code;
        typing.fileText = fileText;
        typing.barFill = tBar;
        typing.hint = hint.gameObject;
        typing.desk = Object.FindObjectsByType<PlaceZone>(FindObjectsSortMode.None).First(z => z.placeId == "Desk");
        typing.dialog = dlg;
        dim.transform.SetAsLastSibling();   // 對話框永遠蓋在最上面

        DayManager day = new GameObject("DayManager").AddComponent<DayManager>();
        day.typing = typing;
        day.dialog = dlg;
        day.clockText = clock;
        day.statBars = bars;
        day.objectiveText = objective;
        day.objectiveNote = note.gameObject;
        return day;
    }

    // ───────────── 道具 ─────────────
    static void Desk(string name, Vector3 p, Material top, Material dark, Material screen, Material chair)
    {
        Box(name, p + new Vector3(0, 0.74f, 0), new Vector3(1.6f, 0.05f, 0.8f), top);
        Box("Desk Leg", p + new Vector3(-0.72f, 0.37f, 0), new Vector3(0.05f, 0.74f, 0.7f), dark);
        Box("Desk Leg", p + new Vector3(0.72f, 0.37f, 0), new Vector3(0.05f, 0.74f, 0.7f), dark);
        Box("Monitor", p + new Vector3(0, 1.05f, 0.22f), new Vector3(0.62f, 0.38f, 0.03f), dark);
        Box("Screen", p + new Vector3(0, 1.05f, 0.2f), new Vector3(0.56f, 0.32f, 0.01f), screen);
        Box("Monitor Stand", p + new Vector3(0, 0.84f, 0.24f), new Vector3(0.06f, 0.16f, 0.06f), dark);
        Box("Keyboard", p + new Vector3(0, 0.775f, -0.12f), new Vector3(0.42f, 0.02f, 0.14f), dark);
        Chair(p + new Vector3(0, 0, -0.75f), chair, 0);
    }

    static void Chair(Vector3 p, Material m, float yaw)
    {
        GameObject c = new GameObject("Chair");
        c.transform.position = p;
        Box("Seat", p + new Vector3(0, 0.45f, 0), new Vector3(0.48f, 0.08f, 0.46f), m).transform.SetParent(c.transform, true);
        Box("Back", p + new Vector3(0, 0.78f, -0.21f), new Vector3(0.46f, 0.6f, 0.06f), m).transform.SetParent(c.transform, true);
        Box("Pole", p + new Vector3(0, 0.22f, 0), new Vector3(0.05f, 0.44f, 0.05f), m).transform.SetParent(c.transform, true);
        c.transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    static void Plant(Vector3 p, Material pot, Material leaf, float h)
    {
        GameObject potGo = Prim(PrimitiveType.Cylinder, "Pot", p + new Vector3(0, 0.2f, 0), new Vector3(0.4f, 0.2f, 0.4f), pot);
        potGo.isStatic = true;
        GameObject l = Prim(PrimitiveType.Sphere, "Leaves", p + new Vector3(0, 0.4f + h * 0.4f, 0), new Vector3(0.7f, h * 0.9f, 0.7f), leaf);
        Object.DestroyImmediate(l.GetComponent<Collider>());
    }

    static GameObject Box(string name, Vector3 pos, Vector3 size, Material m)
    {
        GameObject g = Prim(PrimitiveType.Cube, name, pos, size, m);
        g.isStatic = true;
        return g;
    }

    static GameObject Prim(PrimitiveType t, string name, Vector3 pos, Vector3 size, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = name;
        g.transform.position = pos;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static void Zone(string id, string display, Vector3 pos, Vector3 size, Material markerMat)
    {
        GameObject g = new GameObject("Zone " + id);
        g.transform.position = pos;
        BoxCollider bc = g.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = size;
        PlaceZone z = g.AddComponent<PlaceZone>();
        z.placeId = id;
        z.displayName = display;
        GameObject ring = Prim(PrimitiveType.Cylinder, "Marker", new Vector3(pos.x, 0.02f, pos.z), new Vector3(1.3f, 0.01f, 1.3f), markerMat);
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        ring.transform.SetParent(g.transform, true);
        ring.SetActive(false);
        z.marker = ring;
    }

    static void PostProcess()
    {
        const string path = "Assets/Course/Materials/OfficeVolume.asset";
        AssetDatabase.DeleteAsset(path);
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        Bloom bloom = profile.Add<Bloom>(true); bloom.intensity.Override(0.6f); bloom.threshold.Override(1.1f);
        Vignette v = profile.Add<Vignette>(true); v.intensity.Override(0.28f);
        Tonemapping tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
        ColorAdjustments ca = profile.Add<ColorAdjustments>(true); ca.postExposure.Override(0.3f); ca.saturation.Override(-8f);
        AssetDatabase.CreateAsset(profile, path);
        foreach (VolumeComponent c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        Volume vol = new GameObject("Global Volume").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
    }

    static Material Mat(string name, Color c, float smooth, Color emission = default, bool transparent = false)
    {
        string path = "Assets/Course/Materials/" + name + ".mat";
        if (!AssetDatabase.IsValidFolder("Assets/Course/Materials")) AssetDatabase.CreateFolder("Assets/Course", "Materials");
        AssetDatabase.DeleteAsset(path);
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        if (emission != default)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
        }
        if (transparent)
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // ───────────── UI 小工具 ─────────────
    static RectTransform Panel(string name, Transform parent, Color c, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        g.GetComponent<Image>().color = c;
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Border(GameObject g)
    {
        Outline o = g.AddComponent<Outline>();
        o.effectColor = Line;
        o.effectDistance = new Vector2(2, -2);
        Shadow s = g.AddComponent<Shadow>();
        s.effectColor = new Color(0, 0, 0, 0.25f);
        s.effectDistance = new Vector2(6, -6);
    }

    // 純白方塊圖：內建 UISprite 有圓角陰影，當進度條會有黑邊
    static Sprite WhiteSprite()
    {
        const string path = "Assets/Course/Materials/White.png";
        if (!System.IO.File.Exists(path))
        {
            Texture2D t = new Texture2D(4, 4);
            t.SetPixels(new Color[16].Select(_ => Color.white).ToArray());
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Text Label(string name, Transform parent, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box, Color color)
    {
        GameObject g = new GameObject(name, typeof(RectTransform), typeof(Text));
        g.transform.SetParent(parent, false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        Text t = g.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }
}
