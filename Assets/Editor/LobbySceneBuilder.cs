using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 로비 씬 — 환웅박물관 중앙홀 내부.
///
/// 참고 그림(곰인형 박물관 외관)에 맞춘 재료: 청록 기와, 크림 벽에 민트 패널,
/// 짙은 목재 기둥과 보, 석재 바닥, 석등, 그리고 곳곳의 발바닥 문양.
///
/// 걸어다니지 않는다. 카메라가 홀 가운데를 중심으로 돌고, 마우스로 캐릭터를 고른다.
/// 천장은 일부러 안 덮었다 — 보만 걸어두면 위에서 내려다볼 수 있어서 홀 전체가 보인다.
///
/// 이 메뉴를 다시 누르면 Lobby.unity 를 **덮어쓴다.** 손으로 뭘 넣기 시작한 뒤엔 누르지 마.
/// </summary>
public static class LobbySceneBuilder
{
    const string SceneFolder = "Assets/Scenes";
    const string LobbyPath   = SceneFolder + "/Lobby.unity";
    const string TrackPath   = SceneFolder + "/Track.unity";
    const string GalleryPath = SceneFolder + "/Gallery.unity";

    // ---- 홀 크기 ----
    const float HallWidth = 36f;   // X
    const float HallDepth = 30f;   // Z
    const float WallHeight = 7f;

    // ---- 참고 그림에서 뽑은 색 ----
    static readonly Color ColFloorStone = new Color32(0xC6, 0xC0, 0xB2, 0xFF);
    static readonly Color ColFloorTrim  = new Color32(0x8A, 0x6A, 0x48, 0xFF);
    static readonly Color ColWallCream  = new Color32(0xEF, 0xE7, 0xD6, 0xFF);
    static readonly Color ColWallMint   = new Color32(0xB4, 0xCD, 0xBC, 0xFF);
    static readonly Color ColWoodDark   = new Color32(0x6B, 0x4A, 0x33, 0xFF);
    static readonly Color ColWoodLight  = new Color32(0xA8, 0x78, 0x4C, 0xFF);
    static readonly Color ColRoofTeal   = new Color32(0x4E, 0x7A, 0x70, 0xFF);
    static readonly Color ColStone      = new Color32(0xB0, 0xAC, 0xA0, 0xFF);
    static readonly Color ColLanternLit = new Color32(0xF5, 0xC0, 0x69, 0xFF);
    static readonly Color ColBearFur    = new Color32(0xA5, 0x75, 0x4A, 0xFF);
    static readonly Color ColBearMuzzle = new Color32(0xE2, 0xD2, 0xB4, 0xFF);
    static readonly Color ColBearDark   = new Color32(0x4A, 0x33, 0x26, 0xFF);
    static readonly Color ColRibbon     = new Color32(0xC4, 0x45, 0x3E, 0xFF);
    static readonly Color ColPaw        = new Color32(0x9A, 0x8E, 0x80, 0xFF);
    static readonly Color ColScreen     = new Color32(0x1B, 0x22, 0x2E, 0xFF);
    static readonly Color ColPedestal   = new Color32(0xB0, 0xAC, 0xA0, 0xFF);

    /// <summary>기획서 §3.5 레이서 자리. 5·6번은 잠긴 자리.</summary>
    /// <summary>
    /// 캐릭터 자리. <b>색은 여기 안 적는다</b> — Cast.cs 에서 가져온다.
    ///
    /// 예전엔 여기와 Cast.cs 두 군데에 색이 따로 적혀 있었고, 둘이 서로 달랐다.
    /// 로비 받침대의 세운은 주황, 대화창 이름표의 세운은 초록, 실제 카트는 하늘색이었다.
    /// 색이 캐릭터를 가리키는 표시인데 자리마다 다르면 표시 구실을 못 한다.
    ///
    /// castId 는 Cast.cs 의 id 와 같아야 한다. id 쪽은 저장된 선택값이 걸려 있어서 안 바꾼다.
    /// </summary>
    static readonly (string id, string castId, string name, bool locked)[] Racers =
    {
        ("Igam",     "이감",     "정이감",   false),
        ("Siwoo",    "시우",     "한시우",   false),
        ("Sewoon",   "세운",     "한세운",   false),
        ("Sejin",    "세진",     "한세진",   false),
        ("Developer","개발업자", "개발업자", true),
        ("Council",  "시의원",   "시의원",   true),
    };

    [MenuItem("Racing/씬 하나만 다시 만들기/로비", false, 101)]
    public static void BuildLobby()
    {
        Directory.CreateDirectory(SceneFolder);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeLighting();
        MakeFloor();
        MakePawMedallion(new Vector3(0f, 0.02f, 0f), 1f);
        MakeWalls();
        MakeBeams();
        MakeStoneLanterns();

        MakeBearStatue(new Vector3(0f, 0f, -11.5f));
        MakeReceptionDesk(new Vector3(11.5f, 0f, 4f));
        MakeBroadcastScreen(new Vector3(-17.4f, 3.6f, -2f));

        var stands = MakeStands();
        var gate = MakeGate(new Vector3(0f, 0f, 10.5f));
        var orbit = MakeOrbitCamera();
        var cam = orbit.GetComponent<Camera>();

        gate.lobbyCamera = cam;
        gate.orbit = orbit;

        var rig = new GameObject("GameRig");
        rig.AddComponent<SceneNavigator>();

        var selector = rig.AddComponent<LobbySelector>();
        selector.lobbyCamera = cam;
        selector.orbit = orbit;
        selector.stands = stands;

        var hud = rig.AddComponent<LobbyHUD>();
        hud.selector = selector;
        hud.gate = gate;

        // 이야기 장면은 이 방 안에서 돈다. 전용 씬을 만들면 중앙홀이 두 벌이 되니까.
        StoryRigBuilder.EnsureRig();
        MuseumLook.ApplyToOpenScene();

        EditorSceneManager.SaveScene(scene, LobbyPath);
        RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Lobby.unity(중앙홀) 생성 완료. F1 로비 / F2 트랙 / F3 테스트베드.");
    }

    /// <summary>
    /// 있는 씬만 골라 순서대로 빌드 설정에 넣는다.
    /// <b>F1 로비 · F2 트랙 · F3 전시실.</b> 씬은 이 셋뿐이다 (2026-09-15 정리).
    ///
    /// 이야기 장면은 씬이 아니라 로비 안에서 돈다(StoryStage) — 중앙홀을 두 벌로 만들지 않으려고.
    /// 새 씬을 만들 일이 생기면 <b>뒤에 붙인다</b>. 중간에 끼우면 외운 F 키가 전부 밀린다.
    /// </summary>
    public static void RegisterScenes()
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var path in new[] { LobbyPath, TrackPath, GalleryPath })
            if (File.Exists(path)) list.Add(new EditorBuildSettingsScene(path, true));

        EditorBuildSettings.scenes = list.ToArray();
    }

    // ==================================================================
    //  조명 — 실내라 등불 느낌으로 따뜻하게
    // ==================================================================
    static void MakeLighting()
    {
        var go = new GameObject("Sun");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.86f);
        light.intensity = 1.0f;
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(52f, 24f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.62f, 0.60f, 0.55f);
        RenderSettings.ambientEquatorColor = new Color(0.48f, 0.45f, 0.40f);
        RenderSettings.ambientGroundColor  = new Color(0.26f, 0.23f, 0.20f);
        RenderSettings.fog = false;
    }

    // ==================================================================
    //  바닥
    // ==================================================================
    static void MakeFloor()
    {
        var root = new GameObject("Floor").transform;

        // 석재 바닥. 윗면이 y = 0
        var slab = TestSceneBuilder.Cube(root, "FloorSlab", new Vector3(0f, -0.25f, 0f),
                                         new Vector3(HallWidth, 0.5f, HallDepth), ColFloorStone);
        slab.isStatic = true;

        // 가장자리 나무 테두리 — 바닥 판 위에 얇게 얹는다
        float halfW = HallWidth * 0.5f, halfD = HallDepth * 0.5f;
        Strip(root, "TrimNorth", new Vector3(0f, 0.01f, -halfD + 1f), new Vector3(HallWidth, 0.02f, 2f));
        Strip(root, "TrimSouth", new Vector3(0f, 0.01f,  halfD - 1f), new Vector3(HallWidth, 0.02f, 2f));
        Strip(root, "TrimWest",  new Vector3(-halfW + 1f, 0.01f, 0f), new Vector3(2f, 0.02f, HallDepth));
        Strip(root, "TrimEast",  new Vector3( halfW - 1f, 0.01f, 0f), new Vector3(2f, 0.02f, HallDepth));
    }

    static void Strip(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        var go = TestSceneBuilder.Cube(parent, name, position, scale, ColFloorTrim, keepCollider: false);
        go.isStatic = true;
    }

    /// <summary>바깥 광장과 같은 발바닥 문양. 홀 한가운데 바닥에 새긴다.</summary>
    static void MakePawMedallion(Vector3 center, float scale)
    {
        var root = new GameObject("PawMedallion").transform;
        root.position = center;

        // 바깥 원반
        Disc(root, "Ring", Vector3.zero, new Vector3(11f * scale, 0.004f, 11f * scale), ColStone, false);
        Disc(root, "Inner", new Vector3(0f, 0.004f, 0f),
             new Vector3(8.4f * scale, 0.004f, 8.4f * scale), ColFloorStone, false);

        // 발바닥 — 큰 발볼 하나 + 발가락 네 개
        Disc(root, "PawPad", new Vector3(0f, 0.01f, -0.5f * scale),
             new Vector3(3.1f * scale, 0.004f, 2.6f * scale), ColPaw, false);

        float[] toeX = { -1.75f, -0.6f, 0.6f, 1.75f };
        float[] toeZ = { 1.25f, 1.95f, 1.95f, 1.25f };
        for (int i = 0; i < 4; i++)
        {
            Disc(root, $"PawToe_{i}", new Vector3(toeX[i] * scale, 0.01f, toeZ[i] * scale),
                 new Vector3(1.15f * scale, 0.004f, 1.15f * scale), ColPaw, false);
        }
    }

    // ==================================================================
    //  벽 · 기둥 · 보
    // ==================================================================
    static void MakeWalls()
    {
        var root = new GameObject("Walls").transform;
        float halfW = HallWidth * 0.5f, halfD = HallDepth * 0.5f;

        Wall(root, "WallNorth", new Vector3(0f, 0f, -halfD - 0.25f), new Vector3(HallWidth + 1f, 1f, 0.5f));
        Wall(root, "WallSouth", new Vector3(0f, 0f,  halfD + 0.25f), new Vector3(HallWidth + 1f, 1f, 0.5f));
        Wall(root, "WallWest",  new Vector3(-halfW - 0.25f, 0f, 0f), new Vector3(0.5f, 1f, HallDepth + 1f));
        Wall(root, "WallEast",  new Vector3( halfW + 0.25f, 0f, 0f), new Vector3(0.5f, 1f, HallDepth + 1f));

        MakeColumns(root);
    }

    /// <summary>크림 벽 + 아래쪽 민트 패널 한 겹. 참고 그림의 벽 구성.</summary>
    static void Wall(Transform parent, string name, Vector3 basePosition, Vector3 footprint)
    {
        var go = TestSceneBuilder.Cube(parent, name,
                                       basePosition + Vector3.up * (WallHeight * 0.5f),
                                       new Vector3(footprint.x, WallHeight, footprint.z), ColWallCream);
        go.isStatic = true;

        // 민트 허리 패널 — 벽 안쪽으로 살짝 튀어나오게
        bool alongX = footprint.x > footprint.z;
        Vector3 panelScale = alongX ? new Vector3(footprint.x - 1.5f, 2.2f, footprint.z + 0.12f)
                                    : new Vector3(footprint.x + 0.12f, 2.2f, footprint.z - 1.5f);

        var panel = TestSceneBuilder.Cube(parent, name + "_Panel",
                                          basePosition + Vector3.up * 1.6f, panelScale,
                                          ColWallMint, keepCollider: false);
        panel.isStatic = true;
    }

    /// <summary>한옥 목재 기둥. 벽을 따라 일정 간격으로 세운다.</summary>
    static void MakeColumns(Transform parent)
    {
        var root = new GameObject("Columns").transform;
        root.SetParent(parent, false);

        float halfW = HallWidth * 0.5f - 0.6f;
        float halfD = HallDepth * 0.5f - 0.6f;

        for (int i = -2; i <= 2; i++)
        {
            float x = i * (halfW / 2.4f);
            Column(root, $"Col_N{i + 2}", new Vector3(x, 0f, -halfD));
            Column(root, $"Col_S{i + 2}", new Vector3(x, 0f,  halfD));
        }
        for (int i = -1; i <= 1; i++)
        {
            float z = i * (halfD / 1.6f);
            Column(root, $"Col_W{i + 1}", new Vector3(-halfW, 0f, z));
            Column(root, $"Col_E{i + 1}", new Vector3( halfW, 0f, z));
        }
    }

    static void Column(Transform parent, string name, Vector3 position)
    {
        var col = TestSceneBuilder.Cube(parent, name, position + Vector3.up * (WallHeight * 0.5f),
                                        new Vector3(0.55f, WallHeight, 0.55f), ColWoodDark,
                                        keepCollider: false);
        col.isStatic = true;

        // 주춧돌
        var footing = TestSceneBuilder.Cube(parent, name + "_Base", position + Vector3.up * 0.18f,
                                            new Vector3(0.85f, 0.36f, 0.85f), ColStone,
                                            keepCollider: false);
        footing.isStatic = true;
    }

    /// <summary>천장은 덮지 않고 보만 건다. 위에서 홀 안을 들여다볼 수 있게.</summary>
    static void MakeBeams()
    {
        var root = new GameObject("Beams").transform;
        float halfD = HallDepth * 0.5f;

        for (int i = -3; i <= 3; i++)
        {
            float z = i * (halfD / 3.6f);
            var beam = TestSceneBuilder.Cube(root, $"Beam_{i + 3}",
                                             new Vector3(0f, WallHeight - 0.35f, z),
                                             new Vector3(HallWidth, 0.45f, 0.6f), ColWoodDark,
                                             keepCollider: false);
            beam.isStatic = true;
        }

        // 벽 위를 두르는 청록 기와 띠. 가운데는 뚫려 있어야 위에서 홀 안이 보인다 —
        // 판 하나로 덮으면 카메라를 올렸을 때 화면이 통째로 막힌다.
        float halfW = HallWidth * 0.5f + 0.6f;
        float capW = HallWidth + 1.2f, capD = HallDepth + 1.2f;
        float y = WallHeight + 0.12f;

        RoofTrim(root, "RoofTrim_N", new Vector3(0f, y, -(HallDepth * 0.5f + 0.35f)), new Vector3(capW, 0.25f, 1.3f));
        RoofTrim(root, "RoofTrim_S", new Vector3(0f, y,  (HallDepth * 0.5f + 0.35f)), new Vector3(capW, 0.25f, 1.3f));
        RoofTrim(root, "RoofTrim_W", new Vector3(-halfW + 0.25f, y, 0f), new Vector3(1.3f, 0.25f, capD));
        RoofTrim(root, "RoofTrim_E", new Vector3( halfW - 0.25f, y, 0f), new Vector3(1.3f, 0.25f, capD));
    }

    static void RoofTrim(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        var go = TestSceneBuilder.Cube(parent, name, position, scale, ColRoofTeal, keepCollider: false);
        go.isStatic = true;
    }

    // ==================================================================
    //  석등 — 바깥 정원에 있던 그 등
    // ==================================================================
    static void MakeStoneLanterns()
    {
        var root = new GameObject("StoneLanterns").transform;

        Vector3[] spots =
        {
            new Vector3(-13f, 0f, -8f), new Vector3( 13f, 0f, -8f),
            new Vector3(-13f, 0f,  7f), new Vector3( 13f, 0f,  7f),
        };

        for (int i = 0; i < spots.Length; i++)
        {
            var go = new GameObject($"Lantern_{i + 1}");
            go.transform.SetParent(root, false);
            go.transform.position = spots[i];

            TestSceneBuilder.Cube(go.transform, "Base", new Vector3(0f, 0.15f, 0f),
                                  new Vector3(0.9f, 0.3f, 0.9f), ColStone, keepCollider: false);
            TestSceneBuilder.Cube(go.transform, "Shaft", new Vector3(0f, 0.95f, 0f),
                                  new Vector3(0.34f, 1.3f, 0.34f), ColStone, keepCollider: false);
            TestSceneBuilder.Cube(go.transform, "Housing", new Vector3(0f, 1.9f, 0f),
                                  new Vector3(0.78f, 0.62f, 0.78f), ColLanternLit, keepCollider: false);
            TestSceneBuilder.Cube(go.transform, "Cap", new Vector3(0f, 2.32f, 0f),
                                  new Vector3(1.15f, 0.22f, 1.15f), ColRoofTeal, keepCollider: false);

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.84f, 0.58f);
            light.intensity = 2.2f;
            light.range = 11f;
            light.shadows = LightShadows.None;   // 저사양 노트북 기준(기획서 §7.6)
        }
    }

    // ==================================================================
    //  대형 곰 조형물 — 정면 박공의 곰 얼굴을 입체로
    // ==================================================================
    static void MakeBearStatue(Vector3 position)
    {
        var root = new GameObject("BearStatue").transform;
        root.position = position;

        // 석재 좌대
        Disc(root, "Plinth", new Vector3(0f, 0.3f, 0f), new Vector3(5.4f, 0.3f, 5.4f), ColStone, false);
        Disc(root, "PlinthTop", new Vector3(0f, 0.62f, 0f), new Vector3(4.6f, 0.06f, 4.6f), ColFloorStone, false);

        var bear = new GameObject("Bear").transform;
        bear.SetParent(root, false);
        bear.localPosition = new Vector3(0f, 0.66f, 0f);

        Ball(bear, "Leg_L",  new Vector3(-0.85f, 0.55f,  0.35f), new Vector3(1.0f, 1.0f, 1.1f), ColBearFur);
        Ball(bear, "Leg_R",  new Vector3( 0.85f, 0.55f,  0.35f), new Vector3(1.0f, 1.0f, 1.1f), ColBearFur);
        Ball(bear, "Body",   new Vector3(0f, 1.85f, 0f),         new Vector3(2.6f, 2.7f, 2.3f), ColBearFur);
        Ball(bear, "Arm_L",  new Vector3(-1.55f, 2.15f, 0.35f),  new Vector3(0.95f, 1.5f, 0.95f), ColBearFur);
        Ball(bear, "Arm_R",  new Vector3( 1.55f, 2.15f, 0.35f),  new Vector3(0.95f, 1.5f, 0.95f), ColBearFur);
        Ball(bear, "Head",   new Vector3(0f, 3.85f, 0f),         new Vector3(2.2f, 2.1f, 2.0f), ColBearFur);
        Ball(bear, "Ear_L",  new Vector3(-0.85f, 4.75f, -0.1f),  new Vector3(0.85f, 0.85f, 0.6f), ColBearFur);
        Ball(bear, "Ear_R",  new Vector3( 0.85f, 4.75f, -0.1f),  new Vector3(0.85f, 0.85f, 0.6f), ColBearFur);
        Ball(bear, "Muzzle", new Vector3(0f, 3.55f, 0.85f),      new Vector3(1.1f, 0.85f, 0.8f), ColBearMuzzle);
        Ball(bear, "Nose",   new Vector3(0f, 3.72f, 1.22f),      new Vector3(0.34f, 0.26f, 0.26f), ColBearDark);
        Ball(bear, "Eye_L",  new Vector3(-0.55f, 4.12f, 0.86f),  new Vector3(0.22f, 0.26f, 0.18f), ColBearDark);
        Ball(bear, "Eye_R",  new Vector3( 0.55f, 4.12f, 0.86f),  new Vector3(0.22f, 0.26f, 0.18f), ColBearDark);

        // 빨간 리본 — 외관 박공에 달려 있던 그거
        Ball(bear, "Bow_L",   new Vector3(-0.78f, 2.72f, 1.02f), new Vector3(0.85f, 0.62f, 0.36f), ColRibbon);
        Ball(bear, "Bow_R",   new Vector3( 0.78f, 2.72f, 1.02f), new Vector3(0.85f, 0.62f, 0.36f), ColRibbon);
        Ball(bear, "BowKnot", new Vector3(0f, 2.72f, 1.10f),     new Vector3(0.42f, 0.42f, 0.34f), ColRibbon);
    }

    // ==================================================================
    //  안내 데스크 · 중계 화면
    // ==================================================================
    static void MakeReceptionDesk(Vector3 position)
    {
        var root = new GameObject("ReceptionDesk").transform;
        root.position = position;
        root.rotation = Quaternion.Euler(0f, -28f, 0f);

        TestSceneBuilder.Cube(root, "Counter", new Vector3(0f, 0.55f, 0f),
                              new Vector3(5.2f, 1.1f, 1.0f), ColWoodLight);
        TestSceneBuilder.Cube(root, "CounterTop", new Vector3(0f, 1.14f, 0.06f),
                              new Vector3(5.6f, 0.12f, 1.3f), ColWoodDark, keepCollider: false);
        TestSceneBuilder.Cube(root, "SideWing", new Vector3(3.1f, 0.55f, -1.5f),
                              new Vector3(1.0f, 1.1f, 3.0f), ColWoodLight);
        TestSceneBuilder.Cube(root, "SideWingTop", new Vector3(3.1f, 1.14f, -1.5f),
                              new Vector3(1.3f, 0.12f, 3.3f), ColWoodDark, keepCollider: false);

        // 데스크 뒤 안내판
        TestSceneBuilder.Cube(root, "SignBoard", new Vector3(0f, 2.5f, -1.1f),
                              new Vector3(4.2f, 1.5f, 0.18f), ColWallMint, keepCollider: false);
        TestSceneBuilder.Cube(root, "SignFrame", new Vector3(0f, 2.5f, -1.2f),
                              new Vector3(4.5f, 1.75f, 0.12f), ColWoodDark, keepCollider: false);
    }

    static void MakeBroadcastScreen(Vector3 position)
    {
        var root = new GameObject("BroadcastScreen").transform;
        root.position = position;

        TestSceneBuilder.Cube(root, "Frame", Vector3.zero,
                              new Vector3(0.3f, 4.2f, 7.6f), ColWoodDark, keepCollider: false);
        TestSceneBuilder.Cube(root, "Panel", new Vector3(0.22f, 0f, 0f),
                              new Vector3(0.12f, 3.6f, 7.0f), ColScreen, keepCollider: false);
        TestSceneBuilder.Cube(root, "Valance", new Vector3(0.1f, 2.35f, 0f),
                              new Vector3(0.5f, 0.35f, 8.2f), ColRoofTeal, keepCollider: false);
    }

    // ==================================================================
    //  캐릭터 자리 여섯
    // ==================================================================
    static CharacterStand[] MakeStands()
    {
        var root = new GameObject("CharacterStands").transform;
        var result = new CharacterStand[Racers.Length];
        const float arcRadius = 8.5f;

        for (int i = 0; i < Racers.Length; i++)
        {
            var racer = Racers[i];
            float t = (float)i / (Racers.Length - 1);
            float angle = Mathf.Lerp(208f, 332f, t) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * arcRadius;

            var go = new GameObject($"Stand_{i + 1}_{racer.id}");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-pos.normalized, Vector3.up));

            // 마우스 광선이 맞을 판정 상자 — 받침대와 캐릭터를 통째로 덮는다
            var pick = go.AddComponent<BoxCollider>();
            pick.size = new Vector3(1.9f, 2.4f, 1.9f);
            pick.center = new Vector3(0f, 1.2f, 0f);
            pick.isTrigger = true;

            // 석재 받침대
            var pedestal = Disc(go.transform, "Pedestal", new Vector3(0f, 0.22f, 0f),
                                new Vector3(1.7f, 0.22f, 1.7f), ColPedestal, true);
            Disc(go.transform, "PedestalTrim", new Vector3(0f, 0.46f, 0f),
                 new Vector3(1.9f, 0.04f, 1.9f), ColWoodDark, false);

            var anchor = new GameObject("ModelAnchor").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, 0.48f, 0f);

            // 임시 자리표시 — 치비 서 있는 키 1.15m (규격서)
            // 색은 Cast.cs 가 정한다. 잠긴 자리(개발업자·시의원)도 거기 색을 그대로 쓴다.
            var placeholder = TestSceneBuilder.Capsule(anchor, "Placeholder",
                                                       new Vector3(0f, 0.575f, 0f),
                                                       new Vector3(0.45f, 0.575f, 0.45f),
                                                       Cast.ColorOf(racer.castId), keepCollider: false);

            var stand = go.AddComponent<CharacterStand>();
            stand.index = i;
            stand.displayName = racer.name;
            stand.castId = racer.castId;
            stand.locked = racer.locked;
            stand.modelAnchor = anchor;
            stand.placeholder = placeholder;
            stand.baseRenderer = pedestal.GetComponent<Renderer>();
            stand.baseColor = ColPedestal;

            result[i] = stand;
        }
        return result;
    }

    // ==================================================================
    //  출발 게이트 — 바깥 정문과 같은 한옥 문
    // ==================================================================
    static StartGate MakeGate(Vector3 position)
    {
        var go = new GameObject("StartGate");
        go.transform.position = position;

        for (int side = -1; side <= 1; side += 2)
        {
            TestSceneBuilder.Cube(go.transform, side < 0 ? "Post_L" : "Post_R",
                                  new Vector3(side * 3.0f, 2.1f, 0f),
                                  new Vector3(0.62f, 4.2f, 0.62f), ColWoodDark);
            TestSceneBuilder.Cube(go.transform, side < 0 ? "Footing_L" : "Footing_R",
                                  new Vector3(side * 3.0f, 0.2f, 0f),
                                  new Vector3(1.0f, 0.4f, 1.0f), ColStone, keepCollider: false);
        }

        TestSceneBuilder.Cube(go.transform, "Lintel", new Vector3(0f, 4.4f, 0f),
                              new Vector3(7.4f, 0.55f, 0.8f), ColWoodDark, keepCollider: false);

        // 청록 기와 지붕
        var roof = TestSceneBuilder.Cube(go.transform, "Roof", new Vector3(0f, 4.95f, 0f),
                                         new Vector3(8.6f, 0.5f, 2.2f), ColRoofTeal,
                                         keepCollider: false);
        TestSceneBuilder.Cube(go.transform, "RoofRidge", new Vector3(0f, 5.28f, 0f),
                              new Vector3(7.2f, 0.28f, 1.5f), ColRoofTeal, keepCollider: false);

        // 현판
        var plaque = TestSceneBuilder.Cube(go.transform, "Plaque", new Vector3(0f, 3.75f, 0.42f),
                                           new Vector3(3.0f, 0.8f, 0.14f), ColWallMint,
                                           keepCollider: false);

        // 클릭 판정 — 문간 전체
        var pick = go.AddComponent<BoxCollider>();
        pick.size = new Vector3(6.6f, 4.4f, 1.6f);
        pick.center = new Vector3(0f, 2.2f, 0f);
        pick.isTrigger = true;

        var gate = go.AddComponent<StartGate>();
        gate.targetScene = "Track";
        gate.highlightRenderer = plaque.GetComponent<Renderer>();
        gate.idleColor = ColWallMint;
        return gate;
    }

    // ==================================================================
    //  카메라
    // ==================================================================
    static LobbyOrbitCamera MakeOrbitCamera()
    {
        var pivot = new GameObject("CameraPivot").transform;
        pivot.position = new Vector3(0f, 1.6f, -1f);

        var go = new GameObject("LobbyCamera");
        var cam = TestSceneBuilder.SetUpCamera(go);
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f);
        cam.farClipPlane = 120f;

        var orbit = go.AddComponent<LobbyOrbitCamera>();
        orbit.pivot = pivot;
        orbit.distance = 12f;
        orbit.minDistance = 7f;
        orbit.maxDistance = 13f;   // 더 멀어지면 카메라가 벽을 뚫고 나간다
        orbit.yaw = 0f;
        orbit.pitch = 24f;
        orbit.minPitch = 4f;
        orbit.maxPitch = 55f;
        return orbit;
    }

    // ==================================================================
    //  도우미
    // ==================================================================
    /// <summary>
    /// 원기둥 모양에 상자 콜라이더. 유니티 원기둥의 CapsuleCollider 는 납작하게 누르면
    /// 커다란 공으로 변해서 바닥 역할을 못 한다. 원기둥 메시가 2단위 높이라 크기는 (1,2,1).
    /// </summary>
    static GameObject Disc(Transform parent, string name, Vector3 localPosition,
                           Vector3 scale, Color color, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        if (keepCollider) go.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);

        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = TestSceneBuilder.MaterialAsset(color);
        return go;
    }

    static GameObject Ball(Transform parent, string name, Vector3 localPosition,
                           Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = TestSceneBuilder.MaterialAsset(color);
        return go;
    }
}
