using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// 試玩關卡（Round1.unity）：沿用辦公室場景，拿掉三天制的系統，改成
// 角色坐在座位用電腦（螢幕上抓蟲）→ 走到主管旁邊談話（三回合選擇）
// 選單 Course/5.；命令列 -executeMethod RoundBuilder.Build（會先重建 Office.unity）
public static class RoundBuilder
{
    const string RoundPath = "Assets/Course/Scenes/Round1.unity";
    static readonly Vector3 MyDesk = new Vector3(-7, 0, -3);

    [MenuItem("Course/5. 建立試玩關卡（座位工作＋和主管談話）")]
    public static void Build()
    {
        OfficeBuilder.Build();

        // ── 拿掉三天制的系統：DayManager、整個 HUD、跟隨鏡頭 ──
        Object.DestroyImmediate(GameObject.Find("DayManager"));
        Object.DestroyImmediate(Object.FindFirstObjectByType<Canvas>().gameObject);
        foreach (string n in new[] { "Follow Cam", "Face Cam" }) Object.DestroyImmediate(GameObject.Find(n));

        // ── 角色：坐在自己的座位上打字 ──
        GameObject hero = GameObject.Find("Player");
        Object.DestroyImmediate(hero.GetComponent<SimpleWalker>());
        Object.DestroyImmediate(hero.GetComponent<CharacterController>());
        hero.transform.SetPositionAndRotation(MyDesk + new Vector3(0, 0, -0.78f), Quaternion.identity);
        Animator heroAnim = hero.GetComponent<Animator>();
        heroAnim.applyRootMotion = false;

        // ── 主管：不巡邏了，站在辦公區等你 ──
        GameObject boss = Object.FindFirstObjectByType<OfficeAudioAndNav>().boss;   // 主管預設是隱藏的，GameObject.Find 找不到
        boss.SetActive(true);
        foreach (var c in new Component[] { boss.GetComponent<WaypointPatrol>(), boss.GetComponent<NavMeshAgent>(), boss.GetComponent<AudioSource>() })
            if (c) Object.DestroyImmediate(c);
        Transform pov = boss.transform.Find("PointOfView");
        if (pov) Object.DestroyImmediate(pov.gameObject);
        Vector3 bossPos = new Vector3(2.4f, 0, 0.6f), heroTalkPos = new Vector3(1.0f, 0, 0.6f);
        boss.transform.SetPositionAndRotation(bossPos, Quaternion.Euler(0, -90, 0));
        Animator bossAnim = boss.GetComponent<Animator>();
        bossAnim.applyRootMotion = false;

        // ── 動畫：坐著打字（同事的 Typing）、站著、說話、點頭、搖頭、生氣 ──
        AnimationClip typing = Clip("Assets/Course/Characters/Coworker_Lewis.fbx", true);
        AnimationClip idle = Clip("Assets/Course/Animation/Idle.fbx", true);
        AnimationClip talk = Clip("Assets/Course/Animation/Boss/Boss_Talk.fbx", true);
        AnimationClip nod = Clip("Assets/Course/Animation/Boss/Boss_Nod.fbx", false);
        AnimationClip shake = Clip("Assets/Course/Animation/Boss/Boss_Shake.fbx", false);
        AnimationClip angry = Clip("Assets/Course/Animation/Boss/Boss_Angry.fbx", false);
        heroAnim.runtimeAnimatorController = Controller("Assets/Course/Animation/RoundPlayer.controller", ("Idle", idle), ("Typing", typing), ("Talk", talk));
        bossAnim.runtimeAnimatorController = Controller("Assets/Course/Animation/RoundBoss.controller", ("Idle", idle), ("Talk", talk), ("Nod", nod), ("Shake", shake), ("Angry", angry));

        // ── 你的螢幕放大，上面貼一塊可以點的 App 畫面 ──
        Vector3 screenCenter = MyDesk + new Vector3(0, 1.16f, 0.2f);
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(r.transform.position, MyDesk + new Vector3(0, 1.05f, 0.21f)) > 0.1f) continue;
            if (r.name == "Monitor") { r.transform.position = screenCenter + new Vector3(0, 0, 0.02f); r.transform.localScale = new Vector3(1.0f, 0.6f, 0.03f); }
            if (r.name == "Screen") { r.transform.position = screenCenter; r.transform.localScale = new Vector3(0.94f, 0.54f, 0.01f); }
        }
        (RectTransform area, Text status) = MonitorCanvas(screenCenter - new Vector3(0, 0, 0.008f));

        // ── 鏡頭：肩膀後面、兩人中景、你的臉、主管的臉（都是固定位置） ──
        CinemachineCamera desk = FixedCam("Desk Cam", new Vector3(-6.6f, 1.55f, -4.65f), MyDesk + new Vector3(0, 1.0f, 0.15f), 50);
        CinemachineCamera two = FixedCam("Two Shot", new Vector3(1.7f, 1.55f, -2.3f), new Vector3(1.7f, 1.25f, 0.6f), 50);
        // 兩人背後那盆盆栽會卡在畫面正中間，試玩關卡裡拿掉
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if ((t.name == "Pot" || t.name == "Leaves") && Vector2.Distance(new Vector2(t.position.x, t.position.z), new Vector2(2.4f, 2.6f)) < 0.3f)
                Object.DestroyImmediate(t.gameObject);
        CinemachineCamera pFace = FixedCam("Player Face Cam", new Vector3(2.05f, 1.6f, 0.15f), heroTalkPos + Vector3.up * 1.58f, 35);
        CinemachineCamera bFace = FixedCam("Boss Face Cam", new Vector3(1.35f, 1.62f, 0.15f), bossPos + Vector3.up * 1.62f, 35);

        // 白天主管站在你座位旁邊（右後方），下班才到辦公區談話
        Vector3 bossDesk = MyDesk + new Vector3(1.3f, 0, -1.3f);
        Quaternion bossDeskRot = Quaternion.LookRotation(new Vector3(-1.3f, 0, 0.52f));
        Vector3 seatPos = MyDesk + new Vector3(0, 0, -0.78f);
        CinemachineCamera deskFace = FixedCam("Desk Face Cam", seatPos + new Vector3(0.85f, 1.3f, 0.6f), seatPos + new Vector3(0, 1.22f, 0.05f), 38);
        CinemachineCamera bDeskFace = FixedCam("Boss Desk Face Cam", bossDesk + new Vector3(-0.84f, 1.42f, 0.6f), bossDesk + Vector3.up * 1.58f, 38);   // 主管說話會低頭，鏡頭放低一點才拍得到臉
        // 玩小遊戲時的鏡頭：從右肩上方看螢幕，螢幕佔大半個畫面；你的頭在畫面左下角邊緣，不會擋到螢幕上的按鈕
        CinemachineCamera gameCam = FixedCam("Game Cam", MyDesk + new Vector3(0.45f, 1.62f, -1.05f), MyDesk + new Vector3(0.02f, 1.12f, 0.2f), 40);

        // 同事：坐在位子上邊打字邊跟你說話（鏡頭從側前方拍臉）；頭上有浮空名牌（第 10 週）
        Animator Coworker(string name, string label, out CinemachineCamera cam)
        {
            GameObject c = GameObject.Find(name);
            Vector3 p = c.transform.position;
            cam = FixedCam(label + " Face Cam", p + new Vector3(0.85f, 1.3f, 0.55f), p + new Vector3(0, 1.22f, 0.05f), 38);
            NameTag(c.transform, label, 1.75f);
            return c.GetComponent<Animator>();
        }
        Animator lewis = Coworker("Coworker_Lewis", "學長 阿傑", out CinemachineCamera lewisCam);
        Animator suzie = Coworker("Coworker_Suzie", "設計師 小美", out CinemachineCamera suzieCam);
        Animator martha = Coworker("Coworker_Martha", "PM 瑪莎", out CinemachineCamera marthaCam);
        NameTag(boss.transform, "主管 Joe", 2.15f);

        // ── 導演 ──
        var director = new GameObject("Campaign Director").AddComponent<CampaignDirector>();
        director.player = heroAnim;
        director.boss = bossAnim;
        director.lewis = lewis; director.suzie = suzie; director.martha = martha;
        director.seat = new GameObject("Seat (Player)").transform;
        director.seat.SetPositionAndRotation(seatPos, Quaternion.identity);
        director.bossDeskSpot = new GameObject("Boss Desk Spot").transform;
        director.bossDeskSpot.SetPositionAndRotation(bossDesk, bossDeskRot);
        director.talkSpotPlayer = new GameObject("Talk Spot (Player)").transform;
        director.talkSpotPlayer.SetPositionAndRotation(heroTalkPos, Quaternion.Euler(0, 90, 0));
        director.talkSpotBoss = new GameObject("Talk Spot (Boss)").transform;
        director.talkSpotBoss.SetPositionAndRotation(bossPos, Quaternion.Euler(0, -90, 0));
        director.gameCam = gameCam;
        director.deskCam = desk; director.deskFace = deskFace; director.bossDeskFace = bDeskFace;
        director.twoShot = two; director.playerFace = pFace; director.bossFace = bFace;
        director.lewisFace = lewisCam; director.suzieFace = suzieCam; director.marthaFace = marthaCam;
        director.monitorArea = area;
        director.monitorStatus = status;

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, RoundPath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RoundPath, true), new EditorBuildSettingsScene("Assets/Course/Scenes/Office.unity", true) };
        Debug.Log("[Course] 試玩關卡完成：" + RoundPath + "，按 ▶ 開始");
    }

    // Mixamo 動作檔：設成 Humanoid（才能套到你的角色和主管身上）、設定要不要循環
    static AnimationClip Clip(string path, bool loop)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(path);
        bool dirty = mi.animationType != ModelImporterAnimationType.Human;
        if (dirty)
        {
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }
        ModelImporterClipAnimation[] clips = mi.clipAnimations.Length > 0 ? mi.clipAnimations : mi.defaultClipAnimations;
        foreach (var c in clips)
        {
            if (c.loopTime != loop || !c.lockRootPositionXZ) dirty = true;
            c.loopTime = loop;
            c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;   // 原地動作，不要自己走掉
            c.keepOriginalPositionY = true;
        }
        if (dirty) { mi.clipAnimations = clips; mi.SaveAndReimport(); }
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
    }

    static AnimatorController Controller(string path, params (string name, AnimationClip clip)[] states)
    {
        AssetDatabase.DeleteAsset(path);
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        foreach (var (name, clip) in states) sm.AddState(name).motion = clip;
        sm.defaultState = sm.states[0].state;
        return ctrl;
    }

    static CinemachineCamera FixedCam(string name, Vector3 pos, Vector3 lookAt, float fov)
    {
        var g = new GameObject(name);
        g.transform.position = pos;
        g.transform.LookAt(lookAt);
        var cam = g.AddComponent<CinemachineCamera>();
        cam.Lens.FieldOfView = fov;
        g.AddComponent<CinemachineImpulseListener>();
        g.SetActive(false);
        return cam;
    }

    // 螢幕上的 App 畫面：World Space Canvas，0.94 × 0.54 公尺
    static (RectTransform, Text) MonitorCanvas(Vector3 center)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var g = new GameObject("Monitor UI", typeof(Canvas), typeof(GraphicRaycaster));
        g.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)g.transform;
        rt.sizeDelta = new Vector2(940, 540);
        rt.position = center;
        rt.localScale = Vector3.one * 0.001f;
        rt.rotation = Quaternion.identity;   // 面向 -Z（角色坐的那一側）

        Image bg = Child<Image>("Background", rt, new Vector2(940, 540), Vector2.zero);
        bg.color = new Color32(0xfb, 0xfa, 0xf7, 255);
        Image header = Child<Image>("Header", rt, new Vector2(940, 70), new Vector2(0, 235));
        header.color = new Color32(0xef, 0x6c, 0x33, 255);
        Text title = Child<Text>("Title", header.rectTransform, new Vector2(900, 60), Vector2.zero);
        title.font = font; title.fontSize = 34; title.color = Color.white; title.alignment = TextAnchor.MiddleLeft; title.text = "  飯飯外送　測試版";
        Text status = Child<Text>("Status", rt, new Vector2(900, 40), new Vector2(0, -250));
        status.font = font; status.fontSize = 26; status.color = new Color32(0x23, 0x21, 0x1d, 255); status.alignment = TextAnchor.MiddleCenter;
        RectTransform area = new GameObject("Bug Area", typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(rt, false);
        area.sizeDelta = new Vector2(900, 400);
        area.anchoredPosition = new Vector2(0, -10);

        return (area, status);
    }

    // 浮空名牌（第 10 週）：World Space Canvas，寬 300 高 80，縮放 0.005，掛 Billboard 永遠面向鏡頭
    static void NameTag(Transform owner, string text, float height)
    {
        var g = new GameObject("Name Tag", typeof(Canvas), typeof(Billboard));
        g.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)g.transform;
        rt.SetParent(owner, false);
        rt.sizeDelta = new Vector2(300, 80);
        rt.localPosition = Vector3.up * height;
        rt.localScale = Vector3.one * 0.005f / Mathf.Max(0.0001f, owner.lossyScale.x);
        Image bg = Child<Image>("Background", rt, new Vector2(300, 80), Vector2.zero);
        bg.color = new Color(0.07f, 0.07f, 0.09f, 0.75f);
        Text t = Child<Text>("Text", rt, new Vector2(290, 70), Vector2.zero);
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 40; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.text = text;
    }

    static T Child<T>(string name, RectTransform parent, Vector2 size, Vector2 pos) where T : Component
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(T));
        var rt = (RectTransform)g.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return g.GetComponent<T>();
    }
}
