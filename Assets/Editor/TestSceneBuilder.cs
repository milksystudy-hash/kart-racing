using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 테스트용 씬 두 개를 만들어서 저장하고 빌드 설정에 등록한다.
///
///   Testbed  — 아무것도 없는 회색 맵. 블렌더에서 만든 걸 던져 넣고 크기를 눈으로 확인하는 곳.
///   Track    — 지난주에 만든 타원 트랙. 걸어서 둘러보다가 Tab 으로 카트를 탈 수 있다.
///
/// 상단 메뉴 [Racing] 에서 언제든 다시 만들 수 있어. 다만 **다시 만들면 그 씬에
/// 네가 손으로 넣어둔 건 사라지니까**, 작업을 시작한 뒤에는 함부로 누르지 마.
/// </summary>
public static class TestSceneBuilder
{
    const string SceneFolder    = "Assets/Scenes";
    const string MaterialFolder = "Assets/Materials";
    const string TestbedPath    = SceneFolder + "/Testbed.unity";
    const string TrackPath      = SceneFolder + "/Track.unity";

    const int LayerIgnoreRaycast = 2;   // 유니티 기본 레이어. 카트 서스펜션이 자기를 안 때리게

    // 규격서 팔레트
    static readonly Color ColGround   = new Color32(0x6A, 0x6E, 0x66, 0xFF);
    static readonly Color ColRefWhite = new Color32(0xE8, 0xEA, 0xE2, 0xFF);
    static readonly Color ColRefSage  = new Color32(0x9C, 0xC4, 0x89, 0xFF);
    static readonly Color ColRefClay  = new Color32(0xC9, 0x8A, 0x78, 0xFF);
    static readonly Color ColSeat     = new Color32(0x6E, 0x73, 0x70, 0xFF);
    static readonly Color ColTire     = new Color32(0x2B, 0x2D, 0x2B, 0xFF);
    static readonly Color ColSkin     = new Color32(0xEF, 0xE0, 0xBE, 0xFF);

    [MenuItem("Racing/테스트 씬 두 개 다시 만들기")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(SceneFolder);
        Directory.CreateDirectory(MaterialFolder);

        BuildTestbedScene();
        BuildTrackScene();

        // 로비가 이미 있으면 지워지지 않게, 등록은 한 곳에서 처리한다
        LobbySceneBuilder.RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Testbed.unity / Track.unity 생성 완료. 씬 순서는 F1 로비 / F2 트랙 / F3 테스트베드.");
    }

    // ==================================================================
    //  씬 1 — 빈 테스트 맵
    // ==================================================================
    static void BuildTestbedScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeSun();
        MakeGround(80f);
        MakeScaleReferences();

        var player = MakePlayer(new Vector3(0f, 0.1f, -6f), 0f);
        var rig = new GameObject("GameRig");
        rig.AddComponent<SceneNavigator>();

        var switcher = rig.AddComponent<PlayerModeSwitcher>();
        switcher.player = player.controller;
        switcher.playerCamera = player.camera;

        var hud = rig.AddComponent<TestHUD>();
        hud.modeSwitcher = switcher;
        hud.player = player.controller;

        EditorSceneManager.SaveScene(scene, TestbedPath);
    }

    /// <summary>
    /// 블렌더에서 만든 게 실제로 얼마나 큰지 눈으로 재는 기준물들.
    /// 규격서 치수와 똑같이 맞춰뒀으니, 네 모델을 옆에 놓고 비교하면 된다.
    /// </summary>
    static void MakeScaleReferences()
    {
        var root = new GameObject("ScaleReferences").transform;

        // 1m 정육면체 — 모든 크기의 기준
        Cube(root, "Ref_1m_Cube", new Vector3(-4f, 0.5f, 0f), Vector3.one, ColRefWhite);

        // 사람 키 1.7m
        Capsule(root, "Ref_Human_1.7m", new Vector3(-1.5f, 0.85f, 0f),
                new Vector3(0.5f, 0.85f, 0.5f), ColRefSage);

        // 치비 캐릭터 서 있는 키 1.15m (규격서)
        Capsule(root, "Ref_Chibi_1.15m", new Vector3(0.5f, 0.575f, 0f),
                new Vector3(0.45f, 0.575f, 0.45f), ColRefSage);

        // 카트 크기 상자 — 전장 1.5 / 전폭 1.1 / 높이 0.55 (규격서)
        Cube(root, "Ref_Kart_1.5x1.1x0.55", new Vector3(3f, 0.275f, 0f),
             new Vector3(1.1f, 0.55f, 1.5f), ColRefClay);

        // 10m 마다 기둥 — 거리 감각을 잡으려고
        var posts = new GameObject("DistancePosts_10m").transform;
        posts.SetParent(root, false);
        for (int i = 1; i <= 4; i++)
        {
            Cube(posts, $"Post_{i * 10}m", new Vector3(i * 10f, 1f, 0f),
                 new Vector3(0.2f, 2f, 0.2f), ColRefWhite);
        }
    }

    // ==================================================================
    //  씬 2 — 트랙
    // ==================================================================
    static void BuildTrackScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeSun();
        MakeGround(400f);

        var trackGo = new GameObject("Track");
        var track = trackGo.AddComponent<TrackBuilder>();

        // 결승선 위치와 방향. TrackBuilder 의 기본값과 같은 계산.
        var startPos = new Vector3(track.radiusX, 0f, 0f);

        var kart = MakeKart(startPos + Vector3.up * 0.38f, Quaternion.identity);
        var kartCam = MakeKartCamera(kart);

        // 플레이어는 결승선 옆 인도에 서서 시작한다
        var player = MakePlayer(startPos + new Vector3(0f, 0.1f, -4f), 0f);

        var rig = new GameObject("GameRig");
        rig.AddComponent<SceneNavigator>();

        var tracker = rig.AddComponent<LapTracker>();
        tracker.kart = kart;
        tracker.checkpointCount = track.checkpointCount;
        tracker.totalLaps = 3;

        var switcher = rig.AddComponent<PlayerModeSwitcher>();
        switcher.player = player.controller;
        switcher.playerCamera = player.camera;
        switcher.kart = kart;
        switcher.kartCamera = kartCam;

        var hud = rig.AddComponent<TestHUD>();
        hud.modeSwitcher = switcher;
        hud.player = player.controller;
        hud.kart = kart;
        hud.tracker = tracker;

        EditorSceneManager.SaveScene(scene, TrackPath);
    }

    // ==================================================================
    //  조각들
    // ==================================================================
    public static void MakeSun()
    {
        var go = new GameObject("Sun");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.97f, 0.9f);
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
    }

    static void MakeGround(float size)
    {
        // 윗면이 정확히 y = 0 에 오게 두께의 절반만큼 내린다
        float thickness = Mathf.Max(1f, size * 0.025f);
        var go = Cube(null, "Ground", new Vector3(0f, -thickness * 0.5f, 0f),
                      new Vector3(size, thickness, size), ColGround);
        go.isStatic = true;
    }

    public struct PlayerRig { public FirstPersonController controller; public Camera camera; }

    public static PlayerRig MakePlayer(Vector3 position, float yaw)
    {
        var go = new GameObject("Player");
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.slopeLimit = 50f;
        cc.stepOffset = 0.4f;

        var camGo = new GameObject("PlayerCamera");
        camGo.transform.SetParent(go.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);   // 눈높이
        var cam = SetUpCamera(camGo);

        var fpc = go.AddComponent<FirstPersonController>();
        fpc.cameraPivot = camGo.transform;

        return new PlayerRig { controller = fpc, camera = cam };
    }

    public static Camera SetUpCamera(GameObject go)
    {
        var cam = go.AddComponent<Camera>();
        cam.backgroundColor = new Color(0.62f, 0.70f, 0.74f);
        cam.farClipPlane = 400f;
        cam.nearClipPlane = 0.05f;
        go.AddComponent<AudioListener>();
        go.AddComponent<UniversalAdditionalCameraData>();   // URP 는 이게 있어야 정상 렌더링
        go.tag = "MainCamera";
        return cam;
    }

    static KartController MakeKart(Vector3 position, Quaternion rotation)
    {
        var go = new GameObject("Kart");
        go.transform.SetPositionAndRotation(position, rotation);

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 180f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 4f;

        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(1.0f, 0.45f, 1.4f);
        box.center = new Vector3(0f, -0.06f, 0f);

        var visual = new GameObject("KartVisual").transform;
        visual.SetParent(go.transform, false);

        Cube(visual, "Body",     new Vector3(0f, -0.06f, 0f),  new Vector3(1.1f, 0.28f, 1.5f), ColRefSage, false);
        Cube(visual, "SeatBack", new Vector3(0f, 0.30f, -0.36f), new Vector3(0.52f, 0.45f, 0.10f), ColSeat, false);
        Cube(visual, "Nose",     new Vector3(0f, -0.02f, 0.74f), new Vector3(0.85f, 0.16f, 0.22f), ColSeat, false);

        string[] names = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };
        Vector3[] offsets = {
            new Vector3(-0.50f, -0.18f,  0.55f), new Vector3( 0.50f, -0.18f,  0.55f),
            new Vector3(-0.50f, -0.18f, -0.55f), new Vector3( 0.50f, -0.18f, -0.55f),
        };
        for (int i = 0; i < 4; i++)
        {
            var wheel = Primitive(visual, PrimitiveType.Cylinder, names[i], offsets[i],
                                  new Vector3(0.4f, 0.1f, 0.4f), ColTire, false);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        var driver = new GameObject("Driver").transform;
        driver.SetParent(visual, false);
        Capsule(driver, "Torso", new Vector3(0f, 0.33f, -0.10f), new Vector3(0.34f, 0.26f, 0.34f), ColRefSage, false);
        Primitive(driver, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.76f, -0.10f),
                  new Vector3(0.42f, 0.42f, 0.42f), ColSkin, false);

        var kart = go.AddComponent<KartController>();
        kart.visual = visual;
        kart.groundMask = ~(1 << LayerIgnoreRaycast);
        SetLayerRecursive(go, LayerIgnoreRaycast);

        return kart;
    }

    static KartCamera MakeKartCamera(KartController kart)
    {
        var go = new GameObject("KartCamera");
        SetUpCamera(go);
        go.tag = "Untagged";   // MainCamera 태그는 플레이어 카메라가 갖는다

        var follow = go.AddComponent<KartCamera>();
        follow.target = kart.transform;
        follow.kart = kart;

        go.SetActive(false);   // 카트를 탈 때만 켜진다
        return follow;
    }

    // ------------------------------------------------------------------
    //  프리미티브 + 머티리얼 에셋
    // ------------------------------------------------------------------
    public static GameObject Cube(Transform parent, string name, Vector3 localPos, Vector3 scale,
                           Color color, bool keepCollider = true)
        => Primitive(parent, PrimitiveType.Cube, name, localPos, scale, color, keepCollider);

    public static GameObject Capsule(Transform parent, string name, Vector3 localPos, Vector3 scale,
                              Color color, bool keepCollider = true)
        => Primitive(parent, PrimitiveType.Capsule, name, localPos, scale, color, keepCollider);

    public static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 localPos,
                                Vector3 scale, Color color, bool keepCollider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = MaterialAsset(color);

        if (!keepCollider)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return go;
    }

    /// <summary>단색 머티리얼을 진짜 .mat 에셋으로 만든다 (씬에 저장돼야 하니까).</summary>
    public static Material MaterialAsset(Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGB(color);
        string path = $"{MaterialFolder}/Flat_{hex}.mat";

        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        // 폴더가 없으면 CreateAsset 이 그냥 실패한다. 부르는 쪽마다 챙기지 말고 여기서 보장한다.
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Shader shader = null;
        if (GraphicsSettings.defaultRenderPipeline != null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }
}
