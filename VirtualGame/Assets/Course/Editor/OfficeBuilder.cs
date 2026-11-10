using System.Linq;
using Unity.Cinemachine;
using Unity.AI.Navigation;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
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
    const int WallLayer = 8;   // 牆：給鏡頭的 Deoccluder 判斷用

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
        // 材質分層（Gemini 建議）：會反光的螢幕、玻璃比較亮滑，布、牆、木頭比較霧
        Material floor = Mat("Office_Floor", Color.white, 0.25f);
        floor.SetTexture("_BaseMap", WoodFloorTexture());
        floor.SetTextureScale("_BaseMap", new Vector2(12.5f, 8.75f));   // 一張圖 1.6 公尺見方
        Material wall = Mat("Office_Wall", Hex("#eae6df"), 0.15f);
        Material accent = Mat("Office_Accent", Green, 0.08f);
        Material wood = Mat("Office_Wood", Hex("#c9a77c"), 0.35f);
        Material dark = Mat("Office_Dark", Hex("#2a2b2e"), 0.4f);
        Material screen = Mat("Office_Screen", Hex("#1d3b5c"), 0.85f, Hex("#3d7fc4") * 1.6f);
        Material glass = Mat("Office_Glass", new Color(0.75f, 0.88f, 0.95f, 0.25f), 0.95f, default, true);
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
        Box("Wall N", new Vector3(0, 1.5f, 7), new Vector3(20, 3, 0.2f), wall).layer = WallLayer;
        Box("Wall N Accent", new Vector3(-5, 0.6f, 6.88f), new Vector3(10, 1.2f, 0.05f), accent);
        Box("Wall S", new Vector3(0, 0.4f, -7), new Vector3(20, 0.8f, 0.2f), wall).layer = WallLayer;        // 南牆矮，鏡頭看得進來
        Box("Wall E", new Vector3(10, 1.5f, 0), new Vector3(0.2f, 3, 14), wall).layer = WallLayer;
        Box("Wall W", new Vector3(-10, 1.5f, 0), new Vector3(0.2f, 3, 14), wall).layer = WallLayer;
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

        // 機房：右側牆邊三座機櫃（會閃的綠燈），週三跳電時要跑過來
        Material rackLed = Mat("Office_RackLed", Hex("#3cff7a"), 0.5f, Hex("#3cff7a") * 3f);
        for (int i = 0; i < 3; i++)
        {
            Vector3 rp = new Vector3(9.4f, 0, -1.6f + i * 0.9f);
            Box("Server Rack", rp + new Vector3(0, 1f, 0), new Vector3(0.7f, 2f, 0.8f), dark);
            for (int k = 0; k < 4; k++) Box("Rack Led", rp + new Vector3(-0.36f, 0.5f + k * 0.4f, 0.2f), new Vector3(0.02f, 0.04f, 0.15f), rackLed);
        }
        Zone("Server", "機房（右側牆邊）", new Vector3(8.4f, 1, -0.7f), new Vector3(1.6f, 2, 2.8f), marker);
        Plant(new Vector3(-9.2f, 0, 3.5f), pot, leaf, 1f);
        Plant(new Vector3(2.4f, 0, -6.2f), pot, leaf, 0.9f);
        Plant(new Vector3(2.4f, 0, 2.6f), pot, leaf, 1.1f);

        // ── 光與後製 ──
        Light sun = Object.FindFirstObjectByType<Light>();
        sun.color = Hex("#ffe8c7");
        sun.intensity = 1.3f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50, -35, 0);
        // 室內環境光：上方日光燈冷白、中間暖灰、地面深灰棕，背光處才不會死黑
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex("#d8e2dc");
        RenderSettings.ambientEquatorColor = Hex("#b8b4ac");
        RenderSettings.ambientGroundColor = Hex("#5a554c");
        RenderSettings.skybox = null;   // 室內不要天空，相機背景改暖灰（見下方）
        Ceiling();
        ReflectionProbe probe = new GameObject("Reflection Probe").AddComponent<ReflectionProbe>();
        probe.transform.position = new Vector3(0, 1.5f, 0);
        probe.size = new Vector3(20, 3.2f, 14);
        probe.boxProjection = true;
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;   // 開始時拍一次，之後不耗效能
        PostProcess();

        // ── 角色 ──
        SmoothCharacter();
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
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Hex("#c5c0b8");   // 特寫時牆外是暖灰，加上景深就像室內深處
        cam.transform.position = hero.transform.position + new Vector3(0, 3.4f, -4.2f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        CinemachineBrain brain = cam.gameObject.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.7f);

        // 跟隨鏡頭：角色背後斜上方，跟著角色轉向，看向胸口
        CinemachineCamera follow = VirtualCam("Follow Cam", hero.transform, 50, new Vector3(0, 3.4f, -4.2f), Vector2.zero);
        follow.LookAt = Target(hero.transform, "ChestTarget", 1.2f);
        // 角色靠牆時鏡頭會被牆擋住 → Deoccluder：牆擋在中間就把鏡頭繞到看得到角色的地方（只看第 8 層「牆」）
        CinemachineDeoccluder deocc = follow.gameObject.AddComponent<CinemachineDeoccluder>();
        deocc.CollideAgainst = 1 << WallLayer;
        deocc.AvoidObstacles.Enabled = true;
        deocc.AvoidObstacles.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PreserveCameraDistance;   // 維持距離、從牆頂繞上去；直接拉近會貼到角色背上
        deocc.AvoidObstacles.CameraRadius = 0.2f;
        deocc.AvoidObstacles.Damping = 0.2f;
        deocc.AvoidObstacles.DampingWhenOccluded = 0.1f;
        deocc.MinimumDistanceFromTarget = 0.1f;   // 比這更近的障礙物不理會；角色貼牆時牆離胸口約 0.3 公尺，所以要比 0.3 小
        // 臉部特寫：在角色正前方，臉放在畫面左邊三分之一，右邊留給對話框
        CinemachineCamera face = VirtualCam("Face Cam", hero.transform, 32, new Vector3(0.25f, 1.65f, 1.3f), new Vector2(-0.22f, 0));

        DayManager day = BuildUI();
        CameraDirector director = day.gameObject.AddComponent<CameraDirector>();
        director.followCam = follow;
        director.faceCam = face;
        director.dialog = day.dialog;
        director.player = hero.GetComponent<Animator>();
        // 輪廓光：角色斜後上方一盞不投影的聚光燈，特寫時打亮頭髮和肩膀邊緣
        Light rim = new GameObject("Rim Light").AddComponent<Light>();
        rim.type = LightType.Spot;
        rim.transform.SetParent(hero.transform, false);
        rim.transform.localPosition = new Vector3(-0.7f, 2.4f, -0.9f);
        rim.transform.LookAt(hero.transform.position + Vector3.up * 1.5f);
        rim.color = Hex("#fff1dc");
        rim.intensity = 3f;
        rim.range = 4f;
        rim.spotAngle = 50f;
        rim.shadows = LightShadows.None;
        rim.gameObject.SetActive(false);
        director.rimLight = rim;
        People(hero, day, sun);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("[Course] 辦公室場景完成：" + ScenePath + "，按 ▶ 開始上班");
    }

    // 打程式碼小遊戲的內容＝這個遊戲自己的程式（彩蛋）；.cs 不能直接放 Resources，所以合併成 txt
    static void WriteTypingCode()
    {
        const string dir = "Assets/Course/Scripts/Game/";
        var sb = new System.Text.StringBuilder();
        // TypingTerminal.cs 裡有 <color> 字樣，打出來會打亂上色，所以不放
        foreach (string f in new[] { "DayManager.cs", "EventDialog.cs", "CameraDirector.cs", "PlaceZone.cs" })
            sb.Append("// ===== ").Append(f).Append(" =====\n").Append(System.IO.File.ReadAllText(dir + f).Replace("\r", "")).Append("\n\n");
        System.IO.File.WriteAllText("Assets/Course/Resources/typing_code.txt", sb.ToString());
        AssetDatabase.ImportAsset("Assets/Course/Resources/typing_code.txt");
    }

    // 天花板：朝下的單面 Plane，上方的跟隨鏡頭看穿它、下方的特寫鏡頭看得到；不投影，窗外的光照得進來
    static void Ceiling()
    {
        Material ceil = Mat("Office_Ceiling", Hex("#e9e6df"), 0.1f);
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Plane);
        c.name = "Ceiling";
        c.transform.position = new Vector3(0, 3f, 0);
        c.transform.rotation = Quaternion.Euler(180, 0, 0);
        c.transform.localScale = new Vector3(2, 1, 1.4f);   // Plane 是 10×10 公尺
        Object.DestroyImmediate(c.GetComponent<Collider>());
        Renderer r = c.GetComponent<Renderer>();
        r.sharedMaterial = ceil;
        r.shadowCastingMode = ShadowCastingMode.Off;
        // 日光燈管：朝下的發光 Quad，配 Bloom 看起來會亮
        Material lamp = Mat("Office_Lamp", Hex("#f0f8ff"), 0.5f, Hex("#f0f8ff") * 2.5f);
        for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Lamp";
                q.transform.position = new Vector3(x * 6, 2.98f, z * 4);
                q.transform.rotation = Quaternion.Euler(-90, 0, 0);
                q.transform.localScale = new Vector3(1.2f, 0.25f, 1);
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.GetComponent<Renderer>().sharedMaterial = lamp;
                q.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
    }

    // 角色：法線重算並平滑（消掉臉上一格一格的稜角）；材質霧面＋用自己的貼圖微微自發光，背光時五官才不會黑掉
    static void SmoothCharacter()
    {
        const string fbx = "Assets/Course/Character/Player.fbx";
        ModelImporter mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
        if (mi.importNormals != ModelImporterNormals.Calculate || mi.normalSmoothingAngle != 70)
        {
            mi.importNormals = ModelImporterNormals.Calculate;
            mi.normalSmoothingAngle = 70;
            mi.SaveAndReimport();
        }
        Material skin = AssetDatabase.LoadAssetAtPath<Material>("Assets/Course/Character/Player.mat");
        skin.SetFloat("_Smoothness", 0.1f);
        skin.EnableKeyword("_EMISSION");
        skin.SetTexture("_EmissionMap", skin.GetTexture("_BaseMap"));
        skin.SetColor("_EmissionColor", Hex("#3a3a3a"));
        skin.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(skin);
    }

    // 程式畫的木地板：一排排長短不同、顏色略有差異的木板＋木紋，不用另外下載貼圖
    static Texture2D WoodFloorTexture()
    {
        const string path = "Assets/Course/Materials/WoodFloor.png";
        if (!System.IO.File.Exists(path))
        {
            const int size = 512, rows = 8;
            var tex = new Texture2D(size, size);
            var rng = new System.Random(7);
            Color baseCol = Hex("#b8a587");
            for (int row = 0; row < rows; row++)
            {
                int y0 = row * size / rows;
                int x = -rng.Next(0, 200);
                while (x < size)
                {
                    int len = rng.Next(160, 300);
                    float tone = 0.88f + (float)rng.NextDouble() * 0.2f;
                    float seed = (float)rng.NextDouble() * 100;
                    for (int px = Mathf.Max(0, x); px < Mathf.Min(size, x + len); px++)
                        for (int py = y0; py < y0 + size / rows; py++)
                        {
                            bool seam = py == y0 || px == x || px == x + 1;
                            float grain = Mathf.PerlinNoise(seed + px * 0.01f, py * 0.35f) * 0.12f - 0.06f;
                            Color c = baseCol * (tone + grain);
                            if (seam) c *= 0.7f;
                            c.a = 1;
                            tex.SetPixel(px, py, c);
                        }
                    x += len;
                }
            }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ───────────── 人：同事、主管、環境音、日光與壓力畫面 ─────────────
    static void People(GameObject hero, DayManager day, Light sun)
    {
        // 聲音從角色的位置聽（JohnLemon：Audio Listener 從相機移到玩家身上），主管越近皮鞋聲越大
        Object.DestroyImmediate(Camera.main.GetComponent<AudioListener>());
        hero.AddComponent<AudioListener>();
        hero.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        // 同事：坐在其他座位打字（Mixamo 的 Typing 動畫）；座位旁放 3D 打字聲
        var keyboards = new System.Collections.Generic.List<AudioSource>();
        (string fbx, Vector3 desk)[] seats =
        {
            ("Coworker_Lewis", new Vector3(-4.5f, 0, -3)),
            ("Coworker_Suzie", new Vector3(-2f, 0, 0)),
            ("Coworker_Martha", new Vector3(-7f, 0, 0)),
        };
        foreach (var (fbx, desk) in seats)
        {
            GameObject c = MixamoCharacter(fbx, desk + new Vector3(0, 0, -0.78f));
            if (c == null) continue;
            AudioSource k = c.AddComponent<AudioSource>();
            Spatial(k, 0.35f, 8f);
            keyboards.Add(k);
        }

        // 主管：Mixamo 的 Joe，照 JohnLemon 的 Ghost 做法 —— NavMeshAgent＋WaypointPatrol＋前方視線 Observer
        GameObject boss = MixamoCharacter("Boss_Joe", new Vector3(-8.6f, 0, -5.6f)) ?? new GameObject("Boss");
        boss.name = "Boss";
        NavMeshAgent agent = boss.AddComponent<NavMeshAgent>();
        agent.speed = 1.1f;
        agent.angularSpeed = 240;
        agent.radius = 0.3f;
        agent.stoppingDistance = 0.3f;
        Spatial(boss.AddComponent<AudioSource>(), 0.9f, 9f);
        WaypointPatrol patrol = boss.AddComponent<WaypointPatrol>();
        Vector3[] route = { new Vector3(-8.6f, 0, -5.6f), new Vector3(4.5f, 0, -5.6f), new Vector3(1.2f, 0, 1.4f), new Vector3(-8.6f, 0, 1.4f) };
        Transform routeRoot = new GameObject("Boss Waypoints").transform;
        patrol.waypoints = new Transform[route.Length];
        for (int i = 0; i < route.Length; i++)
        {
            patrol.waypoints[i] = new GameObject("Waypoint (" + i + ")").transform;
            patrol.waypoints[i].SetParent(routeRoot);
            patrol.waypoints[i].position = route[i];
        }
        // 視線範圍：主管前方和兩側的膠囊觸發區（JohnLemon 的 PointOfView）
        GameObject pov = new GameObject("PointOfView");
        pov.transform.SetParent(boss.transform, false);
        pov.transform.localPosition = new Vector3(0, 1.4f, 0);
        CapsuleCollider eye = pov.AddComponent<CapsuleCollider>();
        eye.isTrigger = true;
        eye.direction = 2;   // 沿 Z 軸（主管面向的方向）
        eye.center = new Vector3(0, 0, 1.2f);
        eye.radius = 2f;
        eye.height = 6f;
        Observer observer = pov.AddComponent<Observer>();
        observer.player = hero.transform;
        SlackOff slack = day.typing.GetComponent<SlackOff>();
        observer.slackOff = slack;
        slack.boss = boss.transform;
        slack.player = hero.transform;
        boss.SetActive(false);   // 等 NavMesh 建好才出場（OfficeAudioAndNav）

        // NavMesh 用碰撞體來算，地點觸發區不算障礙物
        GameObject nav = new GameObject("Navigation & Ambience");
        NavMeshSurface surface = nav.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        OfficeAudioAndNav oan = nav.AddComponent<OfficeAudioAndNav>();
        oan.surface = surface;
        oan.boss = boss;
        oan.hum = nav.AddComponent<AudioSource>();
        oan.hum.loop = true;
        oan.hum.volume = 0.12f;
        oan.keyboards = keyboards.ToArray();

        // 氣氛：壓力暗角＋太陽隨時鐘
        Atmosphere atmo = day.gameObject.AddComponent<Atmosphere>();
        atmo.sun = sun;
        atmo.sunColor = new Gradient();
        atmo.sunColor.SetKeys(
            new[] { new GradientColorKey(Hex("#ffd6a0"), 0), new GradientColorKey(Hex("#fff4e6"), 0.4f), new GradientColorKey(Hex("#ffe0b8"), 0.8f), new GradientColorKey(Hex("#ff8a4a"), 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
    }

    static void Spatial(AudioSource a, float volume, float maxDistance)
    {
        a.loop = true;
        a.volume = volume;
        a.spatialBlend = 1;   // 3D 音效：距離越遠越小聲
        a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = 1;
        a.maxDistance = maxDistance;
        a.playOnAwake = false;
    }

    // Mixamo 下載的角色（含動畫）：設成 Humanoid、動畫循環，做一個只有這段動畫的 Animator
    // 檔案不在就回 null（還沒下載也能建場景）
    static GameObject MixamoCharacter(string name, Vector3 pos)
    {
        string path = "Assets/Course/Characters/" + name + ".fbx";
        ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
        if (mi == null) return null;
        if (mi.animationType != ModelImporterAnimationType.Human || mi.defaultClipAnimations.Length == 0 || !mi.clipAnimations.All(c => c.loopTime))
        {
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            ModelImporterClipAnimation[] clips = mi.defaultClipAnimations;
            foreach (var c in clips) { c.loopTime = true; c.lockRootPositionXZ = true; c.keepOriginalPositionY = true; }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }
        // 貼圖包在 FBX 裡，抽出來材質才有顏色（只做一次）
        string texDir = "Assets/Course/Characters/" + name + "_Textures";
        if (!AssetDatabase.IsValidFolder(texDir))
        {
            AssetDatabase.CreateFolder("Assets/Course/Characters", name + "_Textures");
            mi.ExtractTextures(texDir);
            AssetDatabase.Refresh();
            mi.SaveAndReimport();
        }
        if (mi.materialSearch != ModelImporterMaterialSearch.Everywhere)
        {
            mi.materialSearch = ModelImporterMaterialSearch.Everywhere;   // 貼圖資料夾不叫 Textures，要讓它到處找
            mi.SaveAndReimport();
        }
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
        string ctrlPath = "Assets/Course/Characters/" + name + ".controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(ctrlPath, clip);
        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        g.name = name;
        SkinMixamo(g, texDir);
        g.transform.position = pos;
        Animator anim = g.GetComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        return g;
    }

    // Mixamo 的貼圖抽出來後，導入的材質不會自動接上，所以自己做 URP 材質：
    // 1001 是身體衣服、1002 是頭髮（有透明，要開 Alpha Clipping）
    static void SkinMixamo(GameObject g, string texDir)
    {
        string[] pngs = System.IO.Directory.GetFiles(texDir, "*_Diffuse.png");
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                bool hair = mats[i] && mats[i].name.ToLower().Contains("hair");
                string diffuse = pngs.FirstOrDefault(f => f.Contains(hair ? "_1002_" : "_1001_")) ?? pngs.FirstOrDefault();
                if (diffuse == null) continue;
                diffuse = diffuse.Replace('\\', '/');
                string matPath = diffuse.Replace("_Diffuse.png", "_URP.mat");
                Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (m == null)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse));
                    string normal = diffuse.Replace("_Diffuse", "_Normal");
                    if (System.IO.File.Exists(normal))
                    {
                        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(normal);
                        if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                        m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                        m.EnableKeyword("_NORMALMAP");
                    }
                    m.SetFloat("_Smoothness", 0.25f);
                    if (hair)
                    {
                        m.SetFloat("_AlphaClip", 1);
                        m.SetFloat("_Cutoff", 0.5f);
                        m.EnableKeyword("_ALPHATEST_ON");
                    }
                    AssetDatabase.CreateAsset(m, matPath);
                }
                mats[i] = m;
            }
            r.sharedMaterials = mats;
        }
    }

    // 主管反光人影：頭＋肩膀的剪影，邊緣柔和
    static Sprite SilhouetteSprite()
    {
        const string path = "Assets/Course/Materials/Silhouette.png";
        if (!System.IO.File.Exists(path))
        {
            const int w = 150, h = 210;
            var t = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float head = 1 - Mathf.Clamp01((Vector2.Distance(new Vector2(x, y), new Vector2(75, 150)) - 32) / 6f);
                    float sx = (x - 75) / 70f, sy = (y - 10) / 95f;   // 肩膀：下半部的半橢圓
                    float shoulders = y < 105 ? 1 - Mathf.Clamp01((Mathf.Sqrt(sx * sx + Mathf.Pow(Mathf.Max(0, sy), 2)) - 1) * 12) : 0;
                    float neck = Mathf.Abs(x - 75) < 16 && y > 95 && y < 125 ? 1 : 0;
                    t.SetPixel(x, y, new Color(0.05f, 0.05f, 0.06f, Mathf.Max(head, Mathf.Max(shoulders, neck))));
                }
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
        g.AddComponent<CinemachineImpulseListener>();
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
        canvasGo.GetComponent<Canvas>().pixelPerfect = true;   // 中文小字比較銳利
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
        Text dayLabel = Label("Day", card, 22, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-24, -80), new Vector2(200, 30), Dim);
        Text cardTitle = card.Find("Title Card").GetComponent<Text>();
        Text cardShift = card.Find("Shift").GetComponent<Text>();
        Text[] statNames = new Text[4];
        string[] names = { "進度", "壓力", "主管信任", "生活" };
        Color[] colors = { Green, Red, Navy, Amber };
        Image[] bars = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            float y = -112 - i * 52;
            statNames[i] = Label("StatName", card, 22, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(24, y), new Vector2(110, 36), Ink);
            statNames[i].text = names[i];
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
        Text code = Label("Code", term, 24, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(30, -62), new Vector2(1180, 590), Hex("#d4d4d4"));
        code.supportRichText = true;   // 語法上色用
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
        // 摸魚畫面（Tab 切換）：蓋在程式碼上面的「迷因板」
        RectTransform meme = Panel("MemePanel", term, Hex("#f2efe9"), new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(1240, 712));
        Text memeTitle = Label("Title", meme, 30, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(40, -24), new Vector2(1100, 44), Ink);
        Text memeBody = Label("Memes", meme, 28, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(40, -90), new Vector2(1160, 560), Dim);
        // 主管的反光人影：浮在螢幕右邊，越近越清楚（SlackOff 控制透明度）
        Image reflection = Panel("BossReflection", term, new Color(0, 0, 0, 0), new Vector2(1, 0.5f), new Vector2(-40, -20), new Vector2(300, 420)).GetComponent<Image>();
        reflection.sprite = SilhouetteSprite();
        reflection.raycastTarget = false;
        SlackOff slackOff = canvasGo.AddComponent<SlackOff>();
        slackOff.typing = typing;
        slackOff.memePanel = meme.gameObject;
        slackOff.reflection = reflection;
        slackOff.memeTitle = memeTitle;
        slackOff.memeBody = memeBody;
        Label("TabHint", term, 20, TextAnchor.MiddleRight, new Vector2(1, 0), new Vector2(-30, 52), new Vector2(500, 30), Hex("#8a8a8a")).text = "Tab 摸魚（小心主管）";

        // Slack 訊息卡片（右上角）：範本，SlackFeed 複製出來用
        RectTransform slackCard = Panel("SlackCard", root, Color.white, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(460, 92));
        Border(slackCard.gameObject);
        Panel("Stripe", slackCard, Hex("#4a154b"), new Vector2(0, 0.5f), Vector2.zero, new Vector2(8, 92));
        Label("Channel", slackCard, 18, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(24, -10), new Vector2(420, 26), Hex("#4a154b")).text = "# 工作群組";
        Label("Text", slackCard, 22, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(24, -38), new Vector2(420, 50), Ink);
        canvasGo.AddComponent<SlackFeed>().cardTemplate = slackCard;

        // 待辦倒數：出勤卡下方，一件一列（TaskBoard 複製範本）
        RectTransform taskRow = Panel("TaskRow", root, new Color(0.96f, 0.94f, 0.89f, 0.95f), new Vector2(0, 1), new Vector2(30, -380), new Vector2(560, 54));
        Border(taskRow.gameObject);
        Label("Label", taskRow, 21, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(16, -4), new Vector2(530, 40), Ink);
        Image taskBar = Panel("Bar", taskRow, Amber, new Vector2(0, 0), new Vector2(0, 0), new Vector2(560, 6)).GetComponent<Image>();
        taskBar.sprite = WhiteSprite();
        taskBar.type = Image.Type.Filled;
        taskBar.fillMethod = Image.FillMethod.Horizontal;
        TaskBoard taskBoard = canvasGo.AddComponent<TaskBoard>();
        taskBoard.rowTemplate = taskRow;
        GameOverView gameOver = canvasGo.AddComponent<GameOverView>();
        ScamRoute scam = canvasGo.AddComponent<ScamRoute>();
        scam.cardTitle = cardTitle;
        scam.cardShift = cardShift;
        scam.clockText = clock;
        scam.statNames = statNames;
        scam.statBars = bars;
        scam.sun = Object.FindFirstObjectByType<Light>();

        dim.transform.SetAsLastSibling();   // 對話框永遠蓋在最上面

        DayManager day = new GameObject("DayManager").AddComponent<DayManager>();
        day.typing = typing;
        day.timeline = canvasGo.AddComponent<TimelineView>();
        day.tasks = taskBoard;
        day.gameOverView = gameOver;
        day.scam = scam;
        day.dayText = dayLabel;
        // 鏡頭震動來源（緊急事件、Game Over、警察攻堅）；兩台虛擬鏡頭上的 ImpulseListener 會接收
        day.impulse = day.gameObject.AddComponent<CinemachineImpulseSource>();
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
        g.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
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
        // 景深：平常關著，臉部特寫時由 CameraDirector 打開，背後的方塊牆會糊掉
        DepthOfField dof = profile.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(1.3f);
        dof.focalLength.Override(60f);
        dof.aperture.Override(2.8f);
        dof.active = false;
        // 色差：壓力高時畫面邊緣糊掉（Atmosphere 控制強度）
        ChromaticAberration chroma = profile.Add<ChromaticAberration>(true);
        chroma.intensity.Override(0f);
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
