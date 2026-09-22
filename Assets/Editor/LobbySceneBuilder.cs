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
    // 레고 느낌 잡기(2026-09-18). 굽도리는 벽보다 <b>조금만</b> 어둡게 —
    // 세면 얼룩으로 보이고 약하면 그늘로 보인다.
    static readonly Color ColWallFoot   = new Color32(0xD5, 0xCA, 0xB2, 0xFF);
    static readonly Color ColWallLine   = new Color32(0xB2, 0xA6, 0x8E, 0xFF);
    static readonly Color ColWallWood   = new Color32(0x7A, 0x58, 0x3E, 0xFF);
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

    [MenuItem("Racing/로비 씬 만들기", false, 2)]
    public static void BuildLobby()
    {
        Directory.CreateDirectory(SceneFolder);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeLighting();
        MakeFloor();
        MakePawMedallion(new Vector3(0f, 0.02f, 0f), 1f);
        MakeWalls();
        MakeBeams();
        MakeDoors();
        MakeCraft();
        MakeStoneLanterns();

        MakeBearStatue(new Vector3(0f, 0f, -11.5f));
        MakeReceptionDesk(new Vector3(11.5f, 0f, 4f));
        MakeBroadcastScreen(new Vector3(-17.4f, 3.6f, -2f));
        MakeBearSpeaker();
        MakeLanternStrings();

        var stands = MakeStands();
        var gate = MakeGate(new Vector3(0f, 0f, 10.5f));

        // 유저가 만든 소품 다섯. <b>곰보다 먼저</b> 놓아야 곰이 알아서 비켜선다.
        MakeLobbyProps();

        // ★ 붙박이 발자국은 <b>걷어냈다</b>(2026-09-22 유저: *"너무 더러워 보인다"*).
        // 안 사라지는 발자국은 «누가 지나갔다» 가 아니라 <b>바닥 얼룩</b>으로 읽힌다 —
        // 이제 <see cref="PawPrints"/> 가 <b>곰이 걸을 때</b> 남기고 2초 만에 지운다.
        // 곰은 제일 마지막에. 앞에서 세우면 받침대·출발문이 아직 없어서 그 자리를 비었다고 본다.
        MakeBears();
        AddPawPrints();
        AddSpeakerBounce(gate);

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

        // ---- 임시 몸 : 걸어가서 말을 걸 수 있게 ----
        // 캐릭터 모델이 나오면 Player 안의 상자만 갈아 끼우면 된다.
        var player = TestSceneBuilder.MakePlayer(new Vector3(0f, 0.1f, 11f), 180f);
        MakeTempBody(player.controller.transform);

        var entrance = new GameObject("WalkEntrance").transform;
        entrance.SetPositionAndRotation(new Vector3(0f, 0.1f, 11f), Quaternion.Euler(0f, 180f, 0f));

        var walk = rig.AddComponent<WalkMode>();
        walk.player = player.controller;
        walk.walkCamera = player.camera;
        walk.browseCamera = cam;
        walk.entrance = entrance;
        walk.pauseWhileWalking = new Behaviour[] { orbit, selector };

        // 씬에 저장될 때는 꺼 둔다. 카메라가 둘 다 켜져 있으면 AudioListener 가 둘이라
        // 씬을 열 때마다 경고가 뜬다 (런타임에는 WalkMode.Start 가 알아서 끈다).
        player.controller.gameObject.SetActive(false);

        // 몸이 생겼으니 곰은 <b>거리</b>로 판단한다. 카메라 각도로 고르던 건 몸이 없을 때의 임시방편.
        foreach (var bear in Object.FindObjectsByType<BearNpc>(FindObjectsSortMode.None))
            bear.lookTarget = player.controller.transform;

        // 문도 같은 몸을 본다. 둘러보기 모드에서는 몸이 꺼져 있어서 문이 안 잡힌다 —
        // 걸어가서 여는 문이니까 그게 맞아.
        foreach (var door in Object.FindObjectsByType<SceneDoor>(FindObjectsSortMode.None))
            door.visitor = player.controller.transform;

        var hud = rig.AddComponent<LobbyHUD>();
        hud.selector = selector;
        hud.gate = gate;
        hud.walk = walk;

        // 이야기 장면은 이 방 안에서 돈다. 전용 씬을 만들면 중앙홀이 두 벌이 되니까.
        StoryRigBuilder.EnsureRig();
        MuseumLook.RefineMaterials();   // 손으로 다듬을 필요 없이 구워 나올 때부터 마감이 붙어 있게
        MuseumLook.ApplyToOpenScene();

        EditorSceneManager.SaveScene(scene, LobbyPath);
        RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Lobby.unity(중앙홀) 생성 완료. F1 로비 · F2 트랙 · F3 전시실 · F4 캠퍼스.");
    }

    /// <summary>
    /// 임시 몸뚱이. <b>보이는 건 상자뿐</b>이고, 1인칭이라 평소엔 화면에 안 나온다 —
    /// 그림자와 씬 뷰에서 어디 있는지 보이라고 둔다. 콜라이더는 안 붙인다
    /// (충돌은 CharacterController 가 이미 맡는다).
    /// </summary>
    static void MakeTempBody(Transform parent)
    {
        var body = TestSceneBuilder.Cube(parent, "TempBody", Vector3.zero,
                                         new Vector3(0.5f, 1.2f, 0.35f), ColWoodDark, keepCollider: false);
        body.transform.localPosition = new Vector3(0f, 0.6f, 0f);

        var head = TestSceneBuilder.Cube(parent, "TempHead", Vector3.zero,
                                         new Vector3(0.42f, 0.42f, 0.42f), ColStone, keepCollider: false);
        head.transform.localPosition = new Vector3(0f, 1.45f, 0f);
    }

    /// <summary>
    /// 있는 씬만 골라 순서대로 빌드 설정에 넣는다.
    /// <b>F1 로비 · F2 트랙 · F3 전시실 · F4 캠퍼스.</b> (캠퍼스는 2026-09-17 에 붙었다)
    ///
    /// 이야기 장면은 씬이 아니라 로비 안에서 돈다(StoryStage) — 중앙홀을 두 벌로 만들지 않으려고.
    /// 새 씬은 <b>뒤에 붙인다</b>. 중간에 끼우면 외운 F 키가 전부 밀린다.
    /// 캠퍼스가 F4 로 들어간 건 테스트베드가 지워지면서 그 번호가 비어 있었기 때문이야.
    /// </summary>
    public static void RegisterScenes()
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var path in new[] { LobbyPath, TrackPath, GalleryPath, CampusSceneBuilder.CampusPath })
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
    /// <summary>
    /// 중앙홀 문 셋.
    ///
    /// 처음엔 둘이었는데 유저 지적으로 하나 늘렸다(2026-09-17):
    /// *"'밖으로' 가 트랙이면, 레이싱 끝나고 진짜 밖으로 나가는 방법은 없잖아."* 맞는 말이다.
    /// 문패가 곧 <b>지도</b>라서, 한 문이 두 가지를 뜻하면 홀이 어디로 이어지는지 알 수가 없다.
    ///
    /// <b>남쪽 = 경기장(트랙), 동쪽 = 전시실, 서쪽 = 캠퍼스.</b>
    /// 서쪽은 아직 갈 데가 없지만 <b>먼저 세워 둔다</b> — 미니게임이 들어올 자리고,
    /// 닫힌 문이 하나 보이는 게 "여긴 나중에 열린다" 는 가장 싼 예고편이야.
    /// 방송 화면(z −2)을 피해 z +8 에 둔다.
    /// </summary>
    /// <summary>문이 선 자리. 살창이 여기를 피해 간다.</summary>
    static readonly System.Collections.Generic.List<Vector3> doorSpots = new System.Collections.Generic.List<Vector3>();

    static void MakeDoors()
    {
        doorSpots.Clear();
        var root = new GameObject("Doors").transform;
        System.Func<Color, Material> mat = c => TestSceneBuilder.MaterialAsset(c, FlatMaterial.FinishFor(c));

        // 남쪽 — 밖으로. 문의 +Z 가 바깥쪽이라 홀 안을 보게 180도 돌린다.
        var south = new Vector3(0f, 0f, HallDepth * 0.5f - 0.25f);
        doorSpots.Add(south);
        // ★ 2026-09-22 유저: *"«오늘의 실사» 간판 없애고, «경기장» 글자가 나무 목재판에
        // 가려져서 안 보이니까 조정해."*
        //
        // 가린 것은 <b>출발문(StartGate)</b>이다 — 문은 z 14.75, 출발문은 z 10.5 라
        // 카메라에서 보면 출발문이 <b>앞에</b> 선다.
        //
        // ★★ 2차(같은 날): 현판을 6.15 로 올려도 여전히 가렸다. <b>재 보니 자리가 아예 없다</b> —
        // 낮으면 출발문 지붕·보, 높으면 한옥 들보, 어느 각도에서도 기둥(Col_S2)이 걸린다.
        // 그래서 <b>현판을 출발문으로 옮겼다</b>(<see cref="MakeGate"/>). 여기서는 뺀다 —
        // 문패가 둘이면 어느 쪽이 지도인지 모른다.
        //
        // > <b>«더 올린다» 로 두 번 실패하면 자리를 옮겨라.</b> 가리는 물건이 하나가 아니면
        // > 피할 높이가 없다 — 이 홀에는 셋이 겹쳐 있었다.
        HanokDoor.Build(root, south, Quaternion.Euler(0f, 180f, 0f), 4.2f, 4.6f, mat,
                        plaque: false);

        // 동쪽 — 전시실로. 접수대(x 11.5, z 4)를 피해 z -6 에.
        var east = new Vector3(HallWidth * 0.5f - 0.25f, 0f, -6f);
        doorSpots.Add(east);
        HanokDoor.Build(root, east, Quaternion.Euler(0f, 270f, 0f), 3.6f, 4.4f, mat,
                        plaque: true, buildingName: "전시실", department: "");

        // 서쪽 — 캠퍼스로. 2026-09-17 에 진짜로 열렸다(F4 캠퍼스 씬).
        var west = new Vector3(-HallWidth * 0.5f + 0.25f, 0f, 8f);
        doorSpots.Add(west);
        var westDoor = HanokDoor.Build(root, west, Quaternion.Euler(0f, 90f, 0f), 3.6f, 4.4f, mat,
                                       plaque: true, buildingName: "캠퍼스", department: "곰밥마당 · 별관");

        var toCampus = westDoor.AddComponent<SceneDoor>();
        toCampus.sceneIndex = 3;      // F4
        toCampus.label = "캠퍼스로";
        toCampus.range = 3.4f;
    }

    static void MakeWalls()
    {
        var root = new GameObject("Walls").transform;
        float halfW = HallWidth * 0.5f, halfD = HallDepth * 0.5f;

        Wall(root, "WallNorth", new Vector3(0f, 0f, -halfD - 0.25f), new Vector3(HallWidth + 1f, 1f, 0.5f));
        Wall(root, "WallSouth", new Vector3(0f, 0f,  halfD + 0.25f), new Vector3(HallWidth + 1f, 1f, 0.5f));
        Wall(root, "WallWest",  new Vector3(-halfW - 0.25f, 0f, 0f), new Vector3(0.5f, 1f, HallDepth + 1f));
        Wall(root, "WallEast",  new Vector3( halfW + 0.25f, 0f, 0f), new Vector3(0.5f, 1f, HallDepth + 1f));

        MakeWallCraft(root);
        MakeColumns(root);
    }

    /// <summary>
    /// <b>로비 벽의 레고 느낌을 잡는다.</b> 2026-09-18 유저: *"트랙이랑 로비의 레고 느낌도."*
    ///
    /// 크기 분포는 이미 95% 가 작은 것이라 **개수가 원인이 아니었다.**
    /// 캠퍼스에서 실제로 들었던 건 셋 중 <b>모서리와 굽도리</b>였고, 로비에는 그게 없었다:
    /// 벽 넉 장이 통짜 크림색이고 네 귀퉁이가 완벽한 직각이라 방이 상자 안쪽으로 보였다.
    ///
    /// 전부 <c>keepCollider: false</c> — 벽 콜라이더는 이미 있고, 마감이 물리를 바꾸면 안 된다.
    /// </summary>
    static void MakeWallCraft(Transform parent)
    {
        float halfW = HallWidth * 0.5f, halfD = HallDepth * 0.5f;
        const float foot = 0.85f;   // 굽도리 높이 — 사람 무릎께

        // ---- 굽도리 : 벽 아래만 짙은 나무. 색만 바꾸면 칠한 자국이고 턱이 있어야 재료가 바뀐다
        Trim(parent, "FootNorth", new Vector3(0f, foot * 0.5f, -halfD + 0.06f),
             new Vector3(HallWidth, foot, 0.12f), ColWallFoot);
        Trim(parent, "FootSouth", new Vector3(0f, foot * 0.5f, halfD - 0.06f),
             new Vector3(HallWidth, foot, 0.12f), ColWallFoot);
        Trim(parent, "FootWest", new Vector3(-halfW + 0.06f, foot * 0.5f, 0f),
             new Vector3(0.12f, foot, HallDepth), ColWallFoot);
        Trim(parent, "FootEast", new Vector3(halfW - 0.06f, foot * 0.5f, 0f),
             new Vector3(0.12f, foot, HallDepth), ColWallFoot);

        Trim(parent, "FootCapNorth", new Vector3(0f, foot, -halfD + 0.1f),
             new Vector3(HallWidth, 0.09f, 0.2f), ColWallLine);
        Trim(parent, "FootCapSouth", new Vector3(0f, foot, halfD - 0.1f),
             new Vector3(HallWidth, 0.09f, 0.2f), ColWallLine);
        Trim(parent, "FootCapWest", new Vector3(-halfW + 0.1f, foot, 0f),
             new Vector3(0.2f, 0.09f, HallDepth), ColWallLine);
        Trim(parent, "FootCapEast", new Vector3(halfW - 0.1f, foot, 0f),
             new Vector3(0.2f, 0.09f, HallDepth), ColWallLine);

        // ---- 모서리 기둥 : 네 귀퉁이를 세로로 덮는다 ----
        // <b>이게 제일 크게 듣는다.</b> 상자의 날 선 모서리 넷이 레고의 정체거든 —
        // 기둥으로 덮으면 그 선이 사라지고 면이 셋으로 갈린다(캠퍼스에서 확인한 것).
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                var at = new Vector3(sx * (halfW - 0.2f), 0f, sz * (halfD - 0.2f));

                Trim(parent, $"CornerPost_{sx}_{sz}", at + Vector3.up * (WallHeight * 0.5f),
                     new Vector3(0.52f, WallHeight, 0.52f), ColWallWood);
                Trim(parent, $"CornerCap_{sx}_{sz}", at + Vector3.up * (WallHeight - 0.18f),
                     new Vector3(0.72f, 0.24f, 0.72f), ColWallLine);
                Trim(parent, $"CornerFoot_{sx}_{sz}", at + Vector3.up * 0.18f,
                     new Vector3(0.74f, 0.36f, 0.74f), ColFloorStone);
            }

        // ---- 천장 쪽 돌림띠 : 벽이 천장에 그냥 닿으면 상자 뚜껑처럼 보인다 ----
        float top = WallHeight - 0.35f;
        Trim(parent, "RailNorth", new Vector3(0f, top, -halfD + 0.1f),
             new Vector3(HallWidth, 0.18f, 0.2f), ColWallWood);
        Trim(parent, "RailSouth", new Vector3(0f, top, halfD - 0.1f),
             new Vector3(HallWidth, 0.18f, 0.2f), ColWallWood);
        Trim(parent, "RailWest", new Vector3(-halfW + 0.1f, top, 0f),
             new Vector3(0.2f, 0.18f, HallDepth), ColWallWood);
        Trim(parent, "RailEast", new Vector3(halfW - 0.1f, top, 0f),
             new Vector3(0.2f, 0.18f, HallDepth), ColWallWood);
    }

    static void Trim(Transform parent, string name, Vector3 at, Vector3 size, Color color)
    {
        var go = TestSceneBuilder.Cube(parent, name, at, size, color, keepCollider: false);
        go.isStatic = true;
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
    // ==================================================================
    //  손맛 — 큰 상자 몇 개로는 절대 안 나오는 것들
    // ==================================================================
    /// <summary>
    /// 홀이 "성의 없어 보인다" 는 말을 들은 뒤(2026-09-16) 넣은 세 가지.
    /// 전부 얇은 상자인데, 셋 다 <b>눈이 크기를 재게 만드는</b> 물건이라 효과가 크다:
    ///
    ///   1. <b>바닥 줄눈</b> — 판 한 장은 크기를 가늠할 수 없어서 방이 작고 평평해 보인다
    ///   2. <b>창호</b> — 크림색 빈 벽이 제일 유니티 같다. 살창을 걸면 벽에 눈금이 생긴다
    ///   3. <b>주련</b> — 기둥에 세로 현판. 면적은 작은데 한국 건물이라는 신호가 세다
    ///
    /// 살창 뒤에는 발광 한지를 대서 바깥에서 빛이 드는 것처럼 보이게 했다.
    /// </summary>
    static void MakeCraft()
    {
        var root = new GameObject("Craft").transform;

        MakeFloorSeams(root);
        MakeLatticeWindows(root);
        MakeColumnPlaques(root);
    }

    /// <summary>바닥 줄눈. 3m 칸으로 나눈다 — 카트(전장 1.9m)가 두 칸을 안 넘는 크기야.</summary>
    static void MakeFloorSeams(Transform parent)
    {
        var root = new GameObject("FloorSeams").transform;
        root.SetParent(parent, false);

        const float step = 3f, width = 0.07f;
        float halfW = HallWidth * 0.5f - 1.2f, halfD = HallDepth * 0.5f - 1.2f;

        for (float x = -halfW; x <= halfW + 0.01f; x += step)
            Seam(root, $"SeamX_{x:0}", new Vector3(x, 0.006f, 0f), new Vector3(width, 0.01f, halfD * 2f));

        for (float z = -halfD; z <= halfD + 0.01f; z += step)
            Seam(root, $"SeamZ_{z:0}", new Vector3(0f, 0.006f, z), new Vector3(halfW * 2f, 0.01f, width));
    }

    static void Seam(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        var go = TestSceneBuilder.Cube(parent, name, position, scale, ColStone, keepCollider: false);
        go.isStatic = true;
    }

    /// <summary>
    /// 살창 넉 장씩. 민트 허리 패널(1.6m, 높이 2.2) 위, 처마 아래 빈 띠에 건다.
    /// </summary>
    static void MakeLatticeWindows(Transform parent)
    {
        var root = new GameObject("LatticeWindows").transform;
        root.SetParent(parent, false);

        float halfW = HallWidth * 0.5f, halfD = HallDepth * 0.5f;
        const float y = 4.5f, w = 4.2f, h = 2.4f;

        for (int i = -1; i <= 1; i++)
        {
            float x = i * (halfW * 0.55f);
            TryLattice(root, $"Win_N{i + 1}", new Vector3(x, y, -halfD + 0.18f), w, h, true);
            TryLattice(root, $"Win_S{i + 1}", new Vector3(x, y,  halfD - 0.18f), w, h, true);
        }
        for (int i = -1; i <= 1; i++)
        {
            float z = i * (halfD * 0.55f);
            TryLattice(root, $"Win_W{i + 1}", new Vector3(-halfW + 0.18f, y, z), w, h, false);
            TryLattice(root, $"Win_E{i + 1}", new Vector3( halfW - 0.18f, y, z), w, h, false);
        }
    }

    /// <summary>
    /// 문이 선 자리에는 살창을 안 건다. 유저 제보(2026-09-17): 전시실 문과 창이 붙어 있었다.
    /// 창(반폭 2.1)과 문(반폭 2.3)이 4.4m 안에 들면 반드시 겹친다 — <b>자리를 옮기는 게 아니라
    /// 한 장을 빼는 게 맞다.</b> 옮기면 창 간격이 들쭉날쭉해져서 그게 더 눈에 띈다.
    /// </summary>
    static void TryLattice(Transform parent, string name, Vector3 centre, float w, float h, bool alongX)
    {
        foreach (var door in doorSpots)
        {
            float apart = alongX ? Mathf.Abs(centre.x - door.x) : Mathf.Abs(centre.z - door.z);
            bool sameWall = alongX ? Mathf.Abs(centre.z - door.z) < 3f : Mathf.Abs(centre.x - door.x) < 3f;
            if (sameWall && apart < 4.6f) return;
        }
        Lattice(parent, name, centre, w, h, alongX);
    }

    /// <summary>살창 한 장 — 한지 + 테두리 + 세로살 다섯 + 가로살 셋.</summary>
    static void Lattice(Transform parent, string name, Vector3 centre, float w, float h, bool alongX)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.localPosition = centre;
        if (!alongX) root.localRotation = Quaternion.Euler(0f, 90f, 0f);

        // 한지 — 발광이라 바깥에서 빛이 드는 것처럼 보인다
        Piece(root, "Paper", new Vector3(0f, 0f, 0.06f), new Vector3(w, h, 0.05f), HanokRoof.Skylight);

        // 테두리
        Piece(root, "FrameT", new Vector3(0f,  h * 0.5f, 0f), new Vector3(w + 0.3f, 0.22f, 0.16f), ColWoodDark);
        Piece(root, "FrameB", new Vector3(0f, -h * 0.5f, 0f), new Vector3(w + 0.3f, 0.22f, 0.16f), ColWoodDark);
        Piece(root, "FrameL", new Vector3(-w * 0.5f, 0f, 0f), new Vector3(0.22f, h + 0.22f, 0.16f), ColWoodDark);
        Piece(root, "FrameR", new Vector3( w * 0.5f, 0f, 0f), new Vector3(0.22f, h + 0.22f, 0.16f), ColWoodDark);

        // 살 — 이 눈금이 살창을 살창으로 보이게 한다
        for (int i = 1; i <= 5; i++)
            Piece(root, $"BarV_{i}", new Vector3(Mathf.Lerp(-w * 0.5f, w * 0.5f, i / 6f), 0f, 0.01f),
                  new Vector3(0.09f, h, 0.13f), ColWoodLight);

        for (int i = 1; i <= 3; i++)
            Piece(root, $"BarH_{i}", new Vector3(0f, Mathf.Lerp(-h * 0.5f, h * 0.5f, i / 4f), 0.01f),
                  new Vector3(w, 0.09f, 0.13f), ColWoodLight);
    }

    static void Piece(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        var go = TestSceneBuilder.Cube(parent, name, position, scale, color, keepCollider: false);
        go.isStatic = true;
    }

    /// <summary>기둥에 거는 세로 현판. 글씨는 없지만 실루엣만으로 충분히 읽힌다.</summary>
    static void MakeColumnPlaques(Transform parent)
    {
        var root = new GameObject("Plaques").transform;
        root.SetParent(parent, false);

        float halfW = HallWidth * 0.5f - 0.6f;
        float halfD = HallDepth * 0.5f - 0.6f;

        for (int i = -2; i <= 2; i++)
        {
            float x = i * (halfW / 2.4f);
            Plaque(root, $"Plaque_N{i + 2}", new Vector3(x, 3.4f, -halfD + 0.42f));
            Plaque(root, $"Plaque_S{i + 2}", new Vector3(x, 3.4f,  halfD - 0.42f));
        }
    }

    static void Plaque(Transform parent, string name, Vector3 position)
    {
        Piece(parent, name, position, new Vector3(0.44f, 2.6f, 0.07f), ColWoodLight);
        Piece(parent, name + "_Cap", position + Vector3.up * 1.38f,
              new Vector3(0.58f, 0.16f, 0.13f), ColRibbon);
    }

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

    /// <summary>
    /// 천장을 덮는다. 예전엔 일부러 뚫어놨었는데(위에서 내려다보라고) 2026-09-16 에
    /// "하늘 안 보이게" 요청으로 덮었다. 대신 카메라가 벽 위로 못 올라가게 같이 묶었어 —
    /// 안 묶으면 지붕을 바깥에서 내려다보게 된다.
    /// </summary>
    static void MakeBeams()
    {
        var root = new GameObject("Beams").transform;

        HanokRoof.Build(root, Vector3.zero, HallWidth, HallDepth, WallHeight,
                        c => TestSceneBuilder.MaterialAsset(c, FlatMaterial.FinishFor(c)));

        // 벽 위를 두르는 청록 기와 띠 — 지붕 처마와 벽 사이를 메운다
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

        // ★ 접수대 위 전자시계(2026-09-18 유저: *"이 책상이 아무것도 없으니 허전한데"*).
        // <b>빈 오브젝트와 컴포넌트만 놓는다</b> — 모양은 `DeskClock` 이 실행할 때 짓는다.
        // 로비는 구워서 저장하는 씬이라 여기서 기하까지 구우면 «두 벌» 함정(2026-09-17)에 걸린다.
        // 그래서 <b>씬 뷰에는 안 보이고 ▶ 를 눌러야 보인다. 그게 맞는 동작이야.</b>
        var clock = new GameObject("DeskClock");
        clock.transform.SetParent(root, false);
        clock.transform.localPosition = new Vector3(-1.75f, 1.20f, 0.06f);   // 상판 위 왼쪽
        clock.AddComponent<DeskClock>();

        // ★ 유저가 만든 안내데스크 소품(2026-09-22). 다섯을 <b>미리 배치한 세트</b> 하나로 넣는다 —
        // 개별 FBX 다섯과 면 수가 같은데(612쿼드) 간격이 이미 맞춰져 있어서 내가 자리를 안 잡아도 된다.
        //
        // <b>접수대의 자식</b>으로 넣는다. 접수대가 yaw −28° 로 돌아가 있어서 월드 좌표로 적으면
        // 옮길 때마다 다시 계산해야 하고, 자식이면 상판 기준으로 «오른쪽 앞» 이라고 쓸 수 있다.
        // 상판 윗면이 <c>1.14 + 0.12/2 = 1.20</c> — 시계와 같은 높이다.
        var set = CampusBuilder.MyModel(root, "Assets/My blender/Museum_Reception_Set.fbx",
                                        "ReceptionProps", new Vector3(0.82f, 1.20f, 0.06f), 3.300f);
        if (set != null)
        {
            // ★★ <b>입장권 트레이는 뺀다.</b> 유저: *"입장권도 만들긴 했는데 우리는 입장권이
            // 코인 아니였나?"* — 확인해 보니 <b>이 게임에는 입장권도 코인도 없다.</b>
            // 기획서의 «코인 수집» 은 2026-09-16 에 «한 판 = 임무 하나 = 수집품 하나» 로 바뀌었고,
            // 지금 모으는 여덟 개는 <b>철거를 막을 증거</b>지 입장에 쓰는 표가 아니다.
            //
            // 접수대에 입장권이 놓여 있으면 «저걸 받아야 하나» 로 읽혀서 <b>없는 시스템을
            // 있는 것처럼</b> 보이게 한다 — 「준비 중」 미니게임을 셋으로 묶은 것과 같은 판단이야.
            // 유저가 지적한 «곰 박물관» 인쇄 글씨도 이걸 빼면 같이 사라진다.
            var tray = set.Find("Museum_Ticket_Tray");
            if (tray != null) Object.DestroyImmediate(tray.gameObject);
        }

        // 데스크 뒤 안내판
        TestSceneBuilder.Cube(root, "SignBoard", new Vector3(0f, 2.5f, -1.1f),
                              new Vector3(4.2f, 1.5f, 0.18f), ColWallMint, keepCollider: false);
        TestSceneBuilder.Cube(root, "SignFrame", new Vector3(0f, 2.5f, -1.2f),
                              new Vector3(4.5f, 1.75f, 0.12f), ColWoodDark, keepCollider: false);
    }

    /// <summary>
    /// <b>와플곰 스피커</b> — 유저가 블렌더로 만들어 온 모델(2026-09-22).
    /// <c>Assets/My blender/Bear_speaker.fbx</c>(956 tris · 404 쿼드 · 텍스처 0).
    /// 색은 유저가 준 기획 이미지의 <b>④ 민트 초크</b> — 세이지 민트 몸통 · 크림 와플 그릴 ·
    /// 밀크 초코 곰. <b>그릴은 스피커 망이 아니라 와플</b>이라 크림색이어야 한다.
    ///
    /// ★★ <b>경기장 문 양옆</b>(3차, 2026-09-22). 유저: *"경기장 양옆에 크게 씌워 놓으면
    /// 안 될까. FNF 식으로 하니까 잘 안 보이고. 각도도 플레이어가 보는 각도로 뒤집혀 있어야 돼."*
    ///
    /// 2차에서는 <b>서쪽 벽 방송 화면 양옆</b>에 뒀는데, 로비 궤도 카메라는 홀 가운데를
    /// 돌기 때문에 <b>옆벽은 거의 안 보인다.</b> 플레이어가 늘 보는 것은 <b>출발문</b>이다 —
    /// 거기 양옆에 세워야 «무대 장치» 가 된다.
    ///
    /// ★★★ <b>이 FBX 의 기준 앞면은 −Z 다.</b> 배치모드로 곰 얼굴이 향하는 방향을 재서 알았다
    /// (yaw 78° 일 때 얼굴이 (−0.99, −0.17) = 벽 쪽). 2차에서 «+Z 일 것» 이라 믿고 yaw 90 을
    /// 줬다가 <b>정확히 뒤통수를 보여 줬다.</b>
    ///
    /// > **앞면이 어디인지는 짐작하지 말고 재라.** 블렌더에서 180° 돌려 내보내도
    /// > FBX 축 변환이 한 번 더 걸려서 결과가 뒤집힌다 — <b>씬에서 나온 값</b>만 믿을 것.
    /// > 재는 법: 앞면에만 있는 조각(곰 얼굴)의 중심 − 몸통 중심.
    /// </summary>
    static void MakeBearSpeaker()
    {
        const string Fbx = "Assets/My blender/Bear_speaker.fbx";

        // 원본 비율 2.659(폭) × 4.461(키). MyModel 은 max(폭, 깊이) = 2.659 를 기준으로 줄이니
        // <b>원하는 키에서 targetWidth 를 거꾸로 구한다</b> — 손으로 적으면 모델을 새로 뽑을 때
        // 또 틀린다(밥그릇에서 배운 것).
        float Width(float height) => 2.659f / 4.461f * height;

        // 출발문은 (0, 0, 10.5) 에 기둥이 x ±3.0(폭 0.62) · 키 4.2m.
        // 기둥 바깥 끝이 ±3.31 이라 큰 통(반폭 0.83)을 ±4.4 에 두면 10cm 뜬다.
        const float gateZ = 10.1f;
        const float bigH = 2.8f, topH = 1.7f;

        for (int s = -1; s <= 1; s += 2)
        {
            // 문 전체(보까지) 가 x ±4.30 이라 큰 통(반폭 0.95)을 4.4 에 두면 <b>보에 물린다</b>
            // — 측정으로 잡았다. 5.5 면 바깥 끝이 4.55 라 25cm 뜬다.
            float x = s * 5.5f;

            // 앞면이 −Z 라 <b>yaw 0 이면 홀 안쪽</b>(플레이어 쪽)을 본다.
            // 거기에 ±14° 만 틀어 홀 가운데를 겨눈다 — 벽과 평행하면 «벽지» 고,
            // 살짝 틀면 «무대» 다.
            float yaw = s * 14f;

            Stack($"BearSpeaker_{(s < 0 ? "L" : "R")}_Big", new Vector3(x, 0f, gateZ), Width(bigH), yaw);
            // 위 칸은 <b>2cm 묻어서</b> 얹는다. 딱 맞추면 두 면이 같은 평면이라 지지직거린다.
            Stack($"BearSpeaker_{(s < 0 ? "L" : "R")}_Top", new Vector3(x, bigH - 0.02f, gateZ),
                  Width(topH), yaw);
        }

        void Stack(string name, Vector3 at, float targetWidth, float yaw)
        {
            var spk = CampusBuilder.MyModel(null, Fbx, name, at, targetWidth);
            if (spk == null) return;

            // ★ <b>덮어쓰지 않고 곱한다</b> — 임포트 축 회전을 지우면 모델이 눕는다(2026-09-21 곰).
            spk.localRotation = Quaternion.Euler(0f, yaw, 0f) * spk.localRotation;
        }
    }

    /// <summary>
    /// <b>유저가 만든 로비 소품 다섯</b>(2026-09-22): 기념품 매대 · 화분 · 벤치 · 신발장 · 우산꽂이.
    ///
    /// 받은 그대로 쓴다. 잰 값이 이미 실물 크기고 <b>바닥 y = 0</b> 이라 손댈 게 없었다 —
    /// 밥그릇(4.63m)·솥뚜껑(32,320쿼드) 때와 달리 고칠 것이 하나도 없는 첫 모델들이야.
    /// 합쳐서 <b>6,898 tris</b> 라 §7.6 예산(150~250k)에 티도 안 난다.
    ///
    /// <b>크기를 안 키운다.</b> 로비가 36 × 30m 라 실물 크기 가구가 작아 보일 수 있지만,
    /// <b>작아 보이는 게 맞다</b> — 벤치·신발장처럼 사람이 쓰는 물건은 크기가 고정돼 있어서
    /// 보는 사람이 <b>방 크기를 가늠하는 자</b>가 된다(전시실에서 배운 것). 키우면 그 자가 망가진다.
    ///
    /// 자리는 <b>동선</b>으로 잡았다. 로비는 한국 박물관 중앙홀이고, 문 셋이 각각 다른 데로 간다:
    /// <list type="bullet">
    /// <item><b>서문(캠퍼스)</b> = 밖으로 나가는 현관 → <b>신발장 · 우산꽂이</b>를 그 옆에</item>
    /// <item><b>동문(전시실)</b> 쪽 = 접수대가 있는 카운터 구역 → <b>기념품 매대</b></item>
    /// <item>홀 좌우 벽 = 받침대를 마주 보는 자리 → <b>벤치 둘</b></item>
    /// <item>문 옆 빈 벽 → <b>화분 셋</b></item>
    /// </list>
    /// 물건을 흩뿌리지 않고 <b>하는 일끼리 묶는다</b> — 급식소의 «식판 → 배식대 → 자리 → 반납대»
    /// 와 같은 생각이야. 묶여 있으면 설명이 필요 없다.
    /// </summary>
    static void MakeLobbyProps()
    {
        const string Dir = "Assets/My blender/";
        var root = new GameObject("LobbyProps").transform;

        // ★ 이 FBX 들의 기준 앞면은 <b>−Z</b> 다(곰 스피커와 같은 블렌더 익스포트).
        // 그래서 <c>yaw 0</c> 이면 홀 남쪽을 본다. 벽에 등을 붙이려면 벽 반대쪽으로 돌린다.
        // <b>짐작하지 말고 재라</b>던 그 규칙대로, 배치 뒤 검사에서 앞면을 다시 확인한다.
        void Put(string file, string name, Vector3 at, float width, float yaw)
        {
            var t = CampusBuilder.MyModel(root, Dir + file, name, at, width);
            if (t == null) return;
            // 덮어쓰지 않고 곱한다 — 임포트 축 회전을 지우면 모델이 눕는다(2026-09-21 곰).
            t.localRotation = Quaternion.Euler(0f, yaw, 0f) * t.localRotation;
        }

        // ── 서문(캠퍼스) 옆: 현관 ──  문이 z 6.2~9.8 를 쓰니 그 위로 비켜 세운다.
        Put("Museum_Shoe_Cabinet.fbx", "ShoeCabinet", new Vector3(-16.9f, 0f, 11.4f), 1.670f, 90f);
        Put("Museum_Umbrella_Stand.fbx", "UmbrellaStand", new Vector3(-16.9f, 0f, 12.9f), 0.486f, 270f);

        // ── 동벽: 접수대(11.5, 4) 와 한 줄로 서는 카운터 구역 ──
        // 2026-09-22 유저: *"기념품 크기가 너무 작아."* 맞다 — 이건 «사람이 쓰는 물건» 이 아니라
        // <b>간판에 가까운 시설물</b>이라 방 크기에 맞춰 커야 눈에 든다(현판에서 배운 것).
        // 폭 2.22 → <b>3.4m</b>(높이 3.77m). 천장이 7m 라 아직 여유가 있고, 벽에서 0.55m 뜬다.
        Put("Museum_Paw_Stand.fbx", "SouvenirStand", new Vector3(16.0f, 0f, 8.6f), 3.400f, 270f);

        // ── 좌우 벽: 받침대 원호(z −4 ~ −8.5)를 마주 보고 쉬는 자리 ──
        // 석등이 (±13, ±7~8) 이라 그 사이 빈 구간에 놓는다.
        Put("Museum_Bench.fbx", "Bench_W", new Vector3(-15.6f, 0f, 1.6f), 1.800f, 90f);
        Put("Museum_Bench.fbx", "Bench_E", new Vector3(15.6f, 0f, -1.8f), 1.800f, 270f);

        // ── 화분 셋: 문 옆 빈 벽 ──  남문 양옆은 스피커(x ±5.5)를 피해 바깥으로.
        // 2026-09-22 유저: *"화분에 곰 무늬가 안 보여, 각도가 돌아가 있어서."*
        // 화분은 <b>한 면에만 곰 얼굴</b>이 있는데 그 면이 벽을 보고 있었다 —
        // 카메라는 홀 안쪽(−Z 편)에 있으니 <b>무늬 면이 −Z 를 봐야</b> 한다.
        Put("Museum_Planter.fbx", "Planter_S1", new Vector3(-9.2f, 0f, 13.4f), 1.100f, 180f);
        Put("Museum_Planter.fbx", "Planter_S2", new Vector3(9.2f, 0f, 13.4f), 1.100f, 180f);
        Put("Museum_Planter.fbx", "Planter_E", new Vector3(16.2f, 0f, -3.4f), 1.100f, 270f);
    }

    /// <summary>
    /// 로비 곰 셋에게 <b>걸으면 남는 발자국</b>을 달아 준다.
    /// <see cref="MakeBears"/> 뒤에 부른다 — 곰이 아직 없으면 붙일 데가 없다.
    /// </summary>
    static void AddPawPrints()
    {
        foreach (var bear in Object.FindObjectsByType<BearNpc>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
        {
            if (bear.GetComponent<PawPrints>() != null) continue;
            var paws = bear.gameObject.AddComponent<PawPrints>();
            paws.tint = ColPaw;
            paws.pool = 4;      // 두세 발자국만 보이게 — 길게 남으면 다시 «얼룩» 이 된다
            paws.life = 1.9f;
        }
    }

    /// <summary>
    /// 스피커 귀에 <b>출발 신호 때 한 번 튕기는</b> 동작을 달아 준다. 귀는 FBX 안에서
    /// <b>따로 떨어진 오브젝트</b>(`EarL` · `EarR`)라 본도 클립도 필요 없다.
    ///
    /// 2026-09-22 고침: 전에는 <b>늘</b> 박자에 맞춰 흔들렸고 귀 둘이 반 박자씩 어긋나서
    /// 두더지 게임처럼 보였다. 이제 <see cref="StartGate.CountingDown"/> 이 켜지는
    /// 순간에만, <b>두 귀가 똑같이</b> 한 번 튕긴다.
    /// </summary>
    static void AddSpeakerBounce(StartGate gate)
    {
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
        {
            if (!tr.name.StartsWith("BearSpeaker") || tr.parent != null) continue;

            var ears = new System.Collections.Generic.List<Transform>();
            foreach (var kid in tr.GetComponentsInChildren<Transform>(true))
                if (kid.name.StartsWith("EarL") || kid.name.StartsWith("EarR")) ears.Add(kid);
            if (ears.Count == 0) continue;

            // ★ 움직일 물건이라 정적 배칭에 들어가면 안 된다 — 트랜스폼은 움직이는데
            // 그려지는 자리가 안 바뀐다(2026-09-18 캠퍼스 문에서 여섯 번 헤맨 그것).
            foreach (var e in ears) e.gameObject.isStatic = false;

            var b = tr.gameObject.AddComponent<SpeakerBounce>();
            b.ears = ears.ToArray();
            b.gate = gate;
        }
    }

    /// <summary>
    /// <b>천장에서 내려오는 청사초롱 줄.</b> 캠퍼스 축제(<see cref="CampusFestival"/>)가
    /// 쓰는 것과 <b>같은 문법</b>이라 한 번 배우면 두 곳에 다 통한다.
    ///
    /// 로비가 허전했던 이유는 <b>눈높이 위가 통째로 비어 있어서</b>다 — 벽과 천장 사이
    /// 3~6m 구간에 아무것도 없으면 방이 «천장 높은 빈 방» 으로 읽힌다.
    /// 등을 <b>줄로</b> 매달면 그 빈 칸이 채워지고, 색도 들어온다.
    /// </summary>
    static void MakeLanternStrings()
    {
        var root = new GameObject("LanternStrings").transform;
        // 두 줄을 홀을 가로질러 건다. 한 줄이면 «빨랫줄» 이고, 둘이면 «매달아 꾸몄다» 가 된다.
        for (int line = 0; line < 2; line++)
        {
            float z = line == 0 ? -4.5f : 6.5f;
            const float top = 5.9f, sag = 0.8f;
            const int span = 9;

            for (int i = 0; i <= span; i++)
            {
                float t = i / (float)span;
                float x = Mathf.Lerp(-HallWidth * 0.5f + 1.2f, HallWidth * 0.5f - 1.2f, t);
                // 줄은 가운데가 처진다 — 곧은 막대는 줄로 안 보인다(전시실 벨벳 로프와 같은 규칙).
                float y = top - Mathf.Sin(t * Mathf.PI) * sag;

                if (i < span)
                {
                    float t2 = (i + 1) / (float)span;
                    float x2 = Mathf.Lerp(-HallWidth * 0.5f + 1.2f, HallWidth * 0.5f - 1.2f, t2);
                    float y2 = top - Mathf.Sin(t2 * Mathf.PI) * sag;
                    var a = new Vector3(x, y, z);
                    var b = new Vector3(x2, y2, z);
                    var seg = TestSceneBuilder.Cube(root, $"Rope_{line}_{i}", (a + b) * 0.5f,
                                                    new Vector3((b - a).magnitude, 0.045f, 0.045f),
                                                    ColWoodDark, keepCollider: false);
                    seg.transform.rotation = Quaternion.LookRotation(Vector3.forward,
                                                                    Vector3.Cross(Vector3.forward, (b - a).normalized));
                    seg.transform.rotation = Quaternion.FromToRotation(Vector3.right, (b - a).normalized);
                }

                // 등은 한 칸 걸러 — 다 달면 줄이 안 보이고 «등 벽» 이 된다.
                if (i % 2 != 0 || i == 0 || i == span) continue;

                var lamp = new GameObject($"Lantern_{line}_{i}").transform;
                lamp.SetParent(root, false);
                lamp.position = new Vector3(x, y - 0.55f, z);

                TestSceneBuilder.Cube(lamp, "Cord", new Vector3(0f, 0.34f, 0f),
                                      new Vector3(0.03f, 0.4f, 0.03f), ColWoodDark, keepCollider: false);
                TestSceneBuilder.Cube(lamp, "CapTop", new Vector3(0f, 0.17f, 0f),
                                      new Vector3(0.30f, 0.06f, 0.30f), ColRoofTeal, keepCollider: false);
                // 등알은 <b>발광</b>으로. (#F5C069)이 석등 색이라 표에서 발광으로 잡힌다 —
                // 여기서는 그게 맞는 쓰임이다(화장실 바가지에 쓴 게 틀렸던 것).
                var body = Disc(lamp, "Body", Vector3.zero, new Vector3(0.36f, 0.16f, 0.36f),
                                ColLanternLit, false);
                body.GetComponent<Renderer>().sharedMaterial =
                    TestSceneBuilder.MaterialAsset(ColLanternLit, Finish.발광);
                TestSceneBuilder.Cube(lamp, "CapBot", new Vector3(0f, -0.17f, 0f),
                                      new Vector3(0.30f, 0.06f, 0.30f), ColRoofTeal, keepCollider: false);
                TestSceneBuilder.Cube(lamp, "Tassel", new Vector3(0f, -0.30f, 0f),
                                      new Vector3(0.06f, 0.22f, 0.06f), ColRibbon, keepCollider: false);
            }
        }
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

    // ==================================================================
    //  곰인형 NPC — 임시 배치
    // ==================================================================
    /// <summary>
    /// <c>Assets/NPC_bear</c> 에 있는 리깅된 FBX 를 찾아서 홀에 세운다.
    ///
    /// <b>파일 이름을 코드에 안 박는다.</b> 유저가 이름을 바꿔가며 여러 번 갈아끼우고 있어서,
    /// 폴더 안에서 <b>뼈가 들어 있는 FBX</b> 를 찾는 쪽이 안 깨진다.
    /// 여러 개면 제일 최근 것을 쓴다 — 보통 그게 마지막으로 다듬은 거니까.
    ///
    /// 정비 곰이 돌아다니며 말을 거는 건 아직이다(2026-09-16). 지금은 <b>움직이는지 보는 용도</b>라
    /// 제자리에 세워두고 숨쉬기·갸웃·팔 흔들기·쳐다보기만 돈다.
    /// </summary>
    /// <summary>곰인형 키울 배수. 모델이 1.0m 라 그대로 두면 홀에서 너무 작다.</summary>
    const float BearScale = 1.7f;

    /// <summary>
    /// <b>자리를 좌표로 찍지 않고 빈 곳을 찾아서 세운다.</b>
    ///
    /// 처음엔 손으로 좌표를 적었는데 두 번 다 틀렸다 — 한 번은 곰 조각상 안에,
    /// 다음엔 석등 위에 올라갔다(2026-09-16). 홀에 뭐가 있는지 외워서 피하는 건 안 되는 방법이야.
    /// 바닥에 격자를 깔고 <b>이미 놓인 콜라이더와 제일 멀리 떨어진 칸</b>을 고른다.
    /// 나중에 홀에 뭘 더 놓아도 곰이 알아서 비켜선다.
    ///
    /// 그래서 이 함수는 <b>빌드 순서의 맨 뒤</b>에 불러야 한다. 앞에서 부르면 받침대·출발문이
    /// 아직 없어서 그 자리를 비어 있다고 판단한다.
    /// </summary>
    static void MakeBears()
    {
        var model = FindRiggedBear();
        if (model == null)
        {
            Debug.Log("[로비] Assets/NPC_bear 에 뼈가 든 FBX 가 없어서 곰인형은 건너뛴다. " +
                      "FBX 를 넣고 로비를 다시 만들면 자동으로 세워져.");
            return;
        }

        // 여기 3f 는 <b>처음 세울 자리를 고를 때 필요한 여유</b>지 순찰 반경이 아니다.
        // 순찰은 아래에서 11m 로 따로 준다 — 출발점만 널널하면 된다.
        // ★ 2026-09-22 — 스피커 넷이 출발문 앞자리를 먹으면서 <b>세 번째 곰이 설 데가 없어졌다</b>
        // (측정: 3마리 → 2마리). 홀에 물건을 더할 때마다 이 숫자를 다시 봐야 한다 —
        // 여유를 3.0 → 2.6 으로 줄이면 셋이 다시 들어간다. 순찰 반경(11m)과는 다른 값이야.
        var spots = FindOpenSpots(3, patrolRadius: 2.6f);
        if (spots.Count == 0)
        {
            Debug.LogWarning("[로비] 곰을 세울 빈자리를 못 찾았어. 홀이 꽉 찼나?");
            return;
        }

        var root = new GameObject("BearNpcs").transform;

        for (int i = 0; i < spots.Count; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = $"BearNpc_{i + 1}";
            go.transform.SetParent(root, false);

            // 홀 가운데를 보게 세운다 — 벽을 보고 서 있으면 등만 보인다
            Vector3 toCentre = new Vector3(-spots[i].x, 0f, -spots[i].z);
            go.transform.SetPositionAndRotation(
                spots[i],
                toCentre.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCentre, Vector3.up)
                                              : Quaternion.identity);

            go.transform.localScale = Vector3.one * BearScale;

            var npc = go.AddComponent<BearNpc>();
            npc.noticeRange = 13f;   // 로비 카메라가 5~25m 에서 도니까 기본 5m 로는 안 걸린다

            // 2026-09-17 유저: "로비 전체를 다 걸으면 좋겠어." 3m 는 제자리걸음이었다.
            // 넓혀도 되는 이유는 <b>갈 자리를 눈에 보이는 것 기준으로 확인</b>하게 고쳤기 때문 —
            // 전에는 콜라이더만 봐서 석등 같은 장식 위로 걸어갔다. 지금은 스무 번 찔러보고 고른다.
            npc.patrolRadius = 11f;

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.5f, 0f);
            box.size = new Vector3(0.8f, 1.0f, 0.5f);   // 로컬 크기라 스케일을 따라간다
        }

        Debug.Log($"[로비] 곰인형 {spots.Count}마리 세웠어 — {AssetDatabase.GetAssetPath(model)}");
    }

    /// <summary>
    /// 바닥 격자를 훑어서 제일 널널한 칸을 고른다. 고른 칸끼리도 서로 떨어뜨린다 —
    /// 세 마리가 한구석에 몰려 있으면 순찰 반경이 겹쳐서 서로 밀친다.
    /// </summary>
    static System.Collections.Generic.List<Vector3> FindOpenSpots(int count, float patrolRadius)
    {
        var things = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var picks = new System.Collections.Generic.List<Vector3>();

        // 벽에서 3m 안쪽까지만. 순찰 반경까지 생각하면 그보다 더 들어와야 한다.
        float limitX = HallWidth * 0.5f - 3f - patrolRadius;
        float limitZ = HallDepth * 0.5f - 3f - patrolRadius;

        var scored = new System.Collections.Generic.List<(Vector3 at, float room)>();
        for (float x = -limitX; x <= limitX; x += 1.5f)
            for (float z = -limitZ; z <= limitZ; z += 1.5f)
            {
                var at = new Vector3(x, 0f, z);
                float room = Room(at, things);
                if (room > patrolRadius + 0.8f) scored.Add((at, room));
            }

        scored.Sort((a, b) => b.room.CompareTo(a.room));

        foreach (var (at, _) in scored)
        {
            if (picks.Count >= count) break;

            bool tooClose = false;
            foreach (var p in picks)
                // 순찰 반경 두 배 + 조금. 이보다 좁게 잡으면 세 마리가 다 안 들어가고,
                // 넓게 잡으면 순찰 원이 겹쳐서 서로 밀친다.
                if ((p - at).sqrMagnitude < (patrolRadius * 2.1f) * (patrolRadius * 2.1f)) tooClose = true;

            if (!tooClose) picks.Add(at);
        }
        return picks;
    }

    /// <summary>
    /// 이 자리에서 제일 가까운 물건까지의 거리.
    ///
    /// <b>콜라이더가 아니라 보이는 것으로 잰다.</b> 석등처럼 장식은 콜라이더가 없어서
    /// 콜라이더만 보면 "비어 있다" 고 나오고, 곰이 그 위에 서거나 뚫고 지나간다.
    /// 막히는 것만 피하면 되는 게 아니라 <b>겹쳐 보이지도 않아야</b> 한다.
    /// </summary>
    static float Room(Vector3 at, Renderer[] things)
    {
        float nearest = float.MaxValue;
        Vector3 chest = at + Vector3.up * 0.6f;

        foreach (var r in things)
        {
            if (r == null) continue;
            var b = r.bounds;
            if (b.max.y < 0.25f) continue;                    // 바닥판·문양은 밟고 다니면 된다
            if (b.size.x > 30f || b.size.z > 30f) continue;   // 벽·천장처럼 홀 전체를 덮는 것

            // 바운즈까지의 수평 거리. 높이는 안 본다 — 머리 위 보나 서까래는 안 피해도 된다.
            float dx = Mathf.Max(0f, Mathf.Max(b.min.x - chest.x, chest.x - b.max.x));
            float dz = Mathf.Max(0f, Mathf.Max(b.min.z - chest.z, chest.z - b.max.z));
            if (b.min.y > 2.2f) continue;                     // 높이 매달린 것

            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d < nearest) nearest = d;
        }
        return nearest;
    }

    /// <summary>NPC_bear 폴더에서 SkinnedMeshRenderer 가 들어 있는 FBX 중 제일 최근 것.</summary>
    static GameObject FindRiggedBear()
    {
        const string folder = "Assets/NPC_bear";
        if (!AssetDatabase.IsValidFolder(folder)) return null;

        GameObject best = null;
        System.DateTime newest = System.DateTime.MinValue;

        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null || go.GetComponentInChildren<SkinnedMeshRenderer>(true) == null) continue;

            EnsureBearImport(path);

            var stamp = File.GetLastWriteTimeUtc(path);
            if (stamp <= newest) continue;
            newest = stamp;
            best = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return best;
    }

    /// <summary>
    /// 임포트 설정. 카트에서 겪은 것과 같은 함정이 여기도 있다 —
    /// <b>materialLocation 이 External 이면 텍스처가 안 붙어서 새하얗게 나온다.</b>
    /// 애니메이션은 안 가져온다. 뼈는 코드로 돌리니까 클립이 필요 없어.
    ///
    /// 그런데 InPrefab 으로 바꿔도 <b>FBX 안에 박혀 있는 텍스처는 저절로 안 나온다.</b>
    /// 실제로 _BaseMap 이 비고 _BaseColor 가 순백색이라 곰이 하얗게 나왔다(2026-09-16).
    /// 그래서 여기서 텍스처를 폴더로 뽑아내고 다시 임포트한다.
    /// </summary>
    static void EnsureBearImport(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) return;

        bool changed = false;
        if (imp.materialLocation != ModelImporterMaterialLocation.InPrefab)
        { imp.materialLocation = ModelImporterMaterialLocation.InPrefab; changed = true; }
        if (imp.importCameras) { imp.importCameras = false; changed = true; }
        if (imp.importLights) { imp.importLights = false; changed = true; }
        if (imp.importAnimation) { imp.importAnimation = false; changed = true; }
        if (imp.animationType != ModelImporterAnimationType.Generic)
        { imp.animationType = ModelImporterAnimationType.Generic; changed = true; }
        if (!Mathf.Approximately(imp.globalScale, 1f)) { imp.globalScale = 1f; changed = true; }

        if (changed) imp.SaveAndReimport();

        ExtractBearTextures(imp, path);
    }

    /// <summary>
    /// FBX 안에 박힌 텍스처를 옆 폴더로 꺼낸다. 이미 꺼내져 있으면 아무 것도 안 한다.
    /// 노멀맵은 꺼낸 뒤에 <b>타입을 노멀맵으로 바꿔줘야</b> 한다 — 안 그러면 파랗게 칠해진다.
    /// </summary>
    static void ExtractBearTextures(ModelImporter imp, string path)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var renderer = model != null ? model.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
        var material = renderer != null ? renderer.sharedMaterial : null;

        // 이미 색 텍스처가 붙어 있으면 손대지 않는다
        if (material != null && material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
            return;

        string home = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = home + "/Textures";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(home, "Textures");

        if (!imp.ExtractTextures(folder))
        {
            Debug.LogWarning($"[로비] {Path.GetFileName(path)} 안에서 텍스처를 못 꺼냈어. " +
                             "곰이 하얗게 나올 거야.");
            return;
        }

        AssetDatabase.Refresh();

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guid);
            var texImp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (texImp == null) continue;

            bool isNormal = texPath.ToLowerInvariant().Contains("normal");
            var wanted = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (texImp.textureType == wanted) continue;

            texImp.textureType = wanted;
            texImp.SaveAndReimport();
        }

        imp.SaveAndReimport();
        Debug.Log($"[로비] {Path.GetFileName(path)} 의 텍스처를 {folder} 로 꺼냈어.");
    }
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

        // ★ 2026-09-22 유저: *"이거 경기장 문이야? 글씨도 가려져 있고 나무 막대도 더 커졌어.
        // 나무 막대를 아예 지워버리든가 조정을 하든가 해야지 아예 막아버리면 어떡해."*
        //
        // <b>보를 얇게 하고 용마루를 뺐다.</b> 벽 현판을 가리던 것이 이 셋이었고, 셋 다
        // 문간을 답답하게 만들고 있었다.
        TestSceneBuilder.Cube(go.transform, "Lintel", new Vector3(0f, 4.4f, 0f),
                              new Vector3(7.4f, 0.38f, 0.55f), ColWoodDark, keepCollider: false);

        // 청록 기와 지붕 — 한 겹만. 두 겹으로 얹으니 뒤의 «경기장» 현판이 통째로 가렸다.
        TestSceneBuilder.Cube(go.transform, "Roof", new Vector3(0f, 4.82f, 0f),
                              new Vector3(8.0f, 0.34f, 1.7f), ColRoofTeal, keepCollider: false);

        // ★★ <b>현판은 출발문에 단다.</b> 전에는 4m 뒤 벽 문에 달려 있었는데, 측정해 보니
        // <b>모든 카메라 자리에서 가려졌다</b>(출발문 지붕 · 기둥 · 들보). 현판을 더 올리면
        // 이번엔 한옥 지붕 들보에 걸린다 — 자리가 아예 없다.
        //
        // 누르는 물건에 이름을 붙이는 게 맞다: <b>클릭하는 것이 곧 «경기장»</b> 이고,
        // 그 앞에는 아무것도 없다. 벽 문에서는 현판을 뺐다(`plaque: false`) — 문패가 둘이면
        // 어느 쪽이 지도인지 모른다.
        //
        // ★ 방향: 로비 카메라는 <b>−Z 쪽</b>에 있다. 다른 현판들의 forward 가 전부
        // (0,0,−1) 인 것이 그 증거다. 게이트는 회전이 없어 제 +Z 가 <b>뒤쪽</b>이라,
        // 판을 180° 돌린 통에 담아야 글자가 앞을 본다.
        var mount = new GameObject("PlaqueMount").transform;
        mount.SetParent(go.transform, false);
        mount.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var plaque = TestSceneBuilder.Cube(mount, "Plaque", new Vector3(0f, 3.75f, 0.42f),
                                           new Vector3(3.6f, 0.95f, 0.14f), ColWallMint,
                                           keepCollider: false);

        var sign = plaque.AddComponent<BuildingSign>();
        sign.buildingName = "경기장";

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
        orbit.pitch = 16f;
        orbit.minPitch = 2f;
        // 천장을 덮었으니 카메라가 그 위로 올라가면 지붕 등짝만 보인다.
        // 받침점 1.6m + 13m x sin(20) = 6.0m < 천장 7m — 이 셋은 같이 움직여야 한다.
        orbit.maxPitch = 20f;
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
