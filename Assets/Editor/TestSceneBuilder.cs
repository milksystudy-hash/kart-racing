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
///   Track    — 환웅박물관 야외 캠퍼스 순환 트랙(회색 상자). 카트만 탄다.
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

    /// <summary>
    /// 카트를 규격(전장 1.5m)보다 얼마나 크게 보이게 할지.
    ///
    /// 규격 그대로면 화면에서 너무 작게 읽힌다 — 모형 카트 대회라는 설정이라 실제 치수에
    /// 얽매일 이유도 없다. 1.25 배면 전장 1.88m 로, 폭 7~11m 코스에서 카트 6대 폭이 된다
    /// (마리오 카트류가 5~6대). 콜라이더와 서스펜션도 같이 커져서 물리가 안 어긋난다.
    /// </summary>
    /// <summary>AI 카트를 몇 대 낼지. <b>0 이면 혼자 달린다.</b></summary>
    const int AiRacers = 3;

    const float KartScale = 1.25f;

    /// <summary>배율을 곱하기 전 서스펜션 높이. KartController 의 기본값과 같아야 한다.</summary>
    const float BaseRideHeight = 0.38f;

    // 규격서 팔레트
    static readonly Color ColGround   = new Color32(0x6A, 0x6E, 0x66, 0xFF);
    static readonly Color ColRefWhite = new Color32(0xE8, 0xEA, 0xE2, 0xFF);
    static readonly Color ColRefSage  = new Color32(0x9C, 0xC4, 0x89, 0xFF);
    static readonly Color ColRefClay  = new Color32(0xC9, 0x8A, 0x78, 0xFF);
    static readonly Color ColSeat     = new Color32(0x6E, 0x73, 0x70, 0xFF);
    static readonly Color ColTire     = new Color32(0x2B, 0x2D, 0x2B, 0xFF);
    static readonly Color ColSkin     = new Color32(0xEF, 0xE0, 0xBE, 0xFF);
    // 트랙 위 수집품 — 노란 신호색
    static readonly Color ColPickupGlow = new Color32(0xFF, 0xDB, 0x40, 0xFF);
    static readonly Color ColPickupItem = new Color32(0xF2, 0xE4, 0xC0, 0xFF);

    [MenuItem("Racing/트랙 씬 만들기", false, 1)]
    public static void BuildAll()
    {
        Directory.CreateDirectory(SceneFolder);
        Directory.CreateDirectory(MaterialFolder);

        BuildTrackScene();

        // 로비가 이미 있으면 지워지지 않게, 등록은 한 곳에서 처리한다
        LobbySceneBuilder.RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Track.unity 생성 완료. 씬은 F1 로비 / F2 트랙 / F3 전시실 셋뿐이다.");
    }

    /// <summary>
    /// 크기 재는 빈 맵. 평소엔 만들지 않는다 — 씬이 늘어나면 헷갈리기만 해서.
    /// 블렌더나 노마드에서 뽑은 모델이 실제로 얼마나 큰지 눈으로 볼 때만 잠깐 만들어 쓰고 지운다.
    /// </summary>
    static void BuildTestbedOnly_사용안함()
    {
        Directory.CreateDirectory(SceneFolder);
        Directory.CreateDirectory(MaterialFolder);

        BuildTestbedScene();
        LobbySceneBuilder.RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Testbed.unity 생성. 크기 확인이 끝나면 씬 파일을 지우면 된다 " +
                  "(빌드 설정에는 안 들어간다).");
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

        // 치비 캐릭터 서 있는 키 1.25m — 3등신 (규격)
        Capsule(root, "Ref_Chibi_1.25m", new Vector3(0.5f, 0.625f, 0f),
                new Vector3(0.48f, 0.625f, 0.48f), ColRefSage);

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

        // 야외 캠퍼스라 회색 바닥판 대신 CampusBuilder 가 잔디를 깐다
        var trackGo = new GameObject("Track");
        var track = trackGo.AddComponent<TrackBuilder>();
        trackGo.AddComponent<CampusBuilder>();

        // 결승선 위치와 방향은 TrackBuilder 가 계산해준다 (조절점을 바꿔도 따라온다)
        Vector3 startPos = track.StartPosition;
        Quaternion startRot = track.StartRotation;

        var kart = MakeKart(startPos, startRot);
        var kartCam = MakeKartCamera(kart);

        MakeAiKarts(track);

        // 레이스 씬에는 걸어다니는 플레이어를 두지 않는다.
        // 맵을 걸어서 확인하고 싶으면 Testbed 씬(F3)을 쓰면 돼.

        // 트랙에 수집품을 뿌리지 않는다(2026-09-16). 이제 임무를 깨면 바로 들어온다 —
        // 물건을 주우러 되돌아가는 게 레이싱의 본질을 흐린다는 유저 판단. MissionManager 참고.
        // MakeExhibitPickups(track);

        // 후처리·안티에일리어싱. 트랙만 이게 빠져 있어서 유독 "유니티 기본" 으로 보였다.
        MuseumLook.RefineMaterials();   // 손으로 다듬을 필요 없이 구워 나올 때부터 마감이 붙어 있게
        MuseumLook.ApplyToOpenScene();

        // 평균 14 m/s 는 최고속 22 에서 코너 감속을 감안한 어림값
        Debug.Log($"[Racing] 트랙 한 바퀴 {track.LapLength:0} m · 약 {track.LapLength / 14f:0} 초/랩 예상");

        var rig = new GameObject("GameRig");
        rig.AddComponent<SceneNavigator>();

        var tracker = rig.AddComponent<LapTracker>();
        tracker.kart = kart;
        tracker.progress = kart.GetComponent<RaceProgress>();
        tracker.checkpointCount = track.checkpointCount;
        tracker.totalLaps = 3;

        var standings = rig.AddComponent<RaceStandings>();
        standings.playerRacer = kart.GetComponent<RaceProgress>();

        // 길에 널린 철거 자재는 그 판에만. 물건은 씬에 두고 켜고 끄기만 한다.
        rig.AddComponent<DebrisGate>();

        // 골든베어 입간판도 마찬가지. 굽는 시점의 판단만으로는 부족했다 —
        // 트랙은 한 번 굽고 여덟 판을 같은 씬 안에서 이어서 돈다(2026-09-17 유저).
        var adGate = rig.AddComponent<AdSignGate>();
        var adRoot = GameObject.Find("AdSigns");
        if (adRoot != null) adGate.adSigns = adRoot.transform;

        // 코스에 흩어진 곰인형도 그 판에만. 임무 6이 감점제에서 플러스형으로 바뀌었다.
        var cargoGate = rig.AddComponent<CargoGate>();
        var cargoRoot = GameObject.Find("Cargo");
        if (cargoRoot != null) cargoGate.cargo = cargoRoot.transform;

        // AI 는 마지막 판에만. 카트는 씬에 두고 켜고 끄기만 한다.
        var gate = rig.AddComponent<AiRaceGate>();
        var aiRoot = GameObject.Find("AiKarts");
        if (aiRoot != null) gate.aiKarts = aiRoot.transform;

        // 캠퍼스 건물 열셋을 걸어서 점검할 수 있게. TAB 으로 내린다(2026-09-17 유저).
        var walker = MakePlayer(track.StartPosition + Vector3.up * 0.2f, 0f);

        var switcher = rig.AddComponent<PlayerModeSwitcher>();
        switcher.kart = kart;
        switcher.kartCamera = kartCam;
        switcher.player = walker.controller;
        switcher.playerCamera = walker.camera;

        // 트랙은 카트로 시작하니 걷는 몸은 꺼 둔 채로 저장한다 — AudioListener 가 둘이면 경고가 뜬다.
        walker.controller.gameObject.SetActive(false);

        // 임무 — 이게 있어야 레이스에 "실패" 가 생긴다. 장에 따라 조건이 저절로 바뀐다.
        var mission = rig.AddComponent<MissionManager>();
        mission.tracker = tracker;
        mission.kart = kart;

        // 캠퍼스가 이야기에 반응하게. 기하는 안 건드리고 현판 딱지와 빛만 바뀐다.
        rig.AddComponent<CampusMood>();

        // 왼쪽 아래 코스 지도. 3인칭 백뷰는 뒤를 볼 방법이 없어서 이게 없으면 순위가 안 읽힌다.
        var map = rig.AddComponent<MiniMap>();
        map.track = track;

        var hud = rig.AddComponent<TestHUD>();
        hud.map = map;
        hud.modeSwitcher = switcher;
        hud.kart = kart;
        hud.tracker = tracker;
        hud.standings = standings;
        hud.mission = mission;

        EditorSceneManager.SaveScene(scene, TrackPath);
    }

    // ==================================================================
    //  트랙 위 수집품
    // ==================================================================
    /// <summary>
    /// 전시품을 코스에 놓는다. 여덟 개를 전부 깔지만 **그 장의 것만 실제로 나타난다** —
    /// 한 경기에 하나씩 얻는 구조라, 한꺼번에 깔면 한 바퀴에 다 먹어버린다.
    ///
    /// 장마다 두 점씩이고, 그 둘은 코스 반대편에 놓는다. 좌우로도 번갈아 놓아서
    /// 안쪽 지름길로 가면 바깥 것을 놓치게 했다 — 선을 골라야 하는 이유가 된다.
    /// </summary>

    /// <summary>
    /// AI 카트 세 대. <b>지금은 만들어만 둔 것</b>이다(2026-09-16 유저 요청) —
    /// 달리기는 하지만 임무에는 안 걸려 있어서, 이기든 지든 진행에는 영향이 없다.
    /// 나중에 "AI 보다 먼저 들어오기" 를 아홉 번째 임무로 붙일 때 그때 연결한다.
    ///
    /// 끄고 싶으면 <see cref="AiRacers"/> 를 0 으로. 그러면 예전처럼 혼자 달린다.
    ///
    /// 플레이어가 고른 캐릭터는 빼고 나머지 셋이 나온다 — 같은 카트가 두 대 있으면
    /// 누가 나인지 헷갈린다.
    /// </summary>
    static void MakeAiKarts(TrackBuilder track)
    {
        if (AiRacers <= 0) return;

        var root = new GameObject("AiKarts").transform;

        // 출발선에 나란히. 차선은 플레이어와 안 겹치게 벌려둔다.
        float[] lanes = { -0.62f, 0.62f, -0.30f };
        int made = 0;

        // 누가 AI 가 될지는 여기서 정하지 않는다. 캐릭터는 씬을 구운 뒤에 로비에서 고르니까
        // 여기서 정하면 박제된다 — KartSkin.aiSlot 이 런타임에 고른다.
        while (made < AiRacers)
        {
            float lane = lanes[made % lanes.Length];
            Vector3 side = Vector3.Cross(Vector3.up, track.TangentOnPath(0f));
            // 뒤로 한 줄씩 물려 세운다. 나란히 세우면 출발하자마자 서로 밀친다.
            Vector3 back = track.TangentOnPath(0f) * -(3.2f + made * 3.4f);
            Vector3 at = track.StartPosition + side * (lane * track.WidthOnPath(0f) * 0.32f) + back;

            var made_kart = MakeKart(at, track.StartRotation);
            var go = made_kart.gameObject;
            go.name = $"AiKart_{made + 1}";
            go.transform.SetParent(root, false);

            // 플레이어 표시는 떼어낸다 — 이야기 수집품을 AI 가 주워가면 진행이 막힌다(PlayerKart 참고)
            var mark = go.GetComponent<PlayerKart>();
            if (mark != null) Object.DestroyImmediate(mark);

            // 플레이어가 고른 캐릭터를 뺀 나머지 중 몇 번째를 쓸지. 실제 배정은 런타임에.
            var skin = go.GetComponent<KartSkin>();
            if (skin != null) skin.aiSlot = made;

            // KartAi.Awake 에서도 끄지만 씬에도 꺼진 채로 저장해 둔다 —
            // 에디터에서는 Awake 가 안 도니까, 안 그러면 씬만 봐서는 AI 인지 알 수가 없다.
            made_kart.acceptPlayerInput = false;

            var ai = go.AddComponent<KartAi>();
            ai.track = track;
            ai.lane = lane;
            // 실력을 조금씩 다르게. 셋이 똑같으면 한 덩어리로 붙어다녀서 레이스로 안 보인다.
            // 2026-09-18 유저: *"내가 앞서 달려나갈 때 바로 뒤에 항상 진이 붙네. 이거 성격
            // 반영한 거야? 레이스에 다들 진심으로 달릴 수 있게 해줘."* 성격이 아니라 실력값이었다 —
            // 0.88 / 0.92 / 0.96 이라 <b>맨 앞 하나만 따라붙고 뒤의 둘은 구경</b>하고 있었어.
            // 이제 셋 다 거의 전력이고 차이는 한 끗이다(0.94 / 0.97 / 1.00).
            // <b>완전히 똑같이 주면 안 된다</b> — 셋이 한 덩어리로 붙어다녀서 레이스로 안 보인다.
            ai.skill = 0.94f + made * 0.03f;   // 0.94 / 0.97 / 1.00

            made++;
        }

        Debug.Log($"[Racing] AI 카트 {made}대. 지금은 임무와 무관하게 달리기만 한다.");
    }
    static void MakeExhibitPickups(TrackBuilder track)
    {
        var root = new GameObject("ExhibitPickups").transform;

        for (int i = 0; i < ExhibitCatalogue.Count; i++)
        {
            var item = ExhibitCatalogue.All[i];

            // 같은 장의 물건끼리 코스에 고르게 퍼지도록, 장 안에서의 순번으로 위치를 잡는다
            int inChapter = 0, chapterTotal = 0;
            for (int j = 0; j < ExhibitCatalogue.Count; j++)
            {
                if (ExhibitCatalogue.All[j].chapterIndex != item.chapterIndex) continue;
                if (j < i) inChapter++;
                chapterTotal++;
            }
            float t = (inChapter + 0.5f) / Mathf.Max(1, chapterTotal);

            Vector3 on = track.PointOnPath(t);
            Vector3 side = Vector3.Cross(Vector3.up, track.TangentOnPath(t));
            float lane = (i % 2 == 0 ? 1f : -1f) * track.WidthOnPath(t) * 0.26f;

            var go = new GameObject($"Pickup_{i + 1}_{item.id}");
            go.transform.SetParent(root, false);
            go.transform.position = on + side * lane + Vector3.up * 1.1f;

            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 2.2f;

            // 돌면서 떠다니는 부분. 주우면 이것만 사라지고 전광등이 올라온다.
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            MakePickupShape(visual, item.shape);

            // 멀리서도 보이라고 발밑에 노란 고리
            Capsule(visual, "Glow", new Vector3(0f, -0.55f, 0f),
                    new Vector3(0.9f, 0.06f, 0.9f), ColPickupGlow, keepCollider: false);

            var lightGo = new GameObject("IdleLight");
            lightGo.transform.SetParent(go.transform, false);
            var idle = lightGo.AddComponent<Light>();
            idle.type = LightType.Point;
            idle.color = ColPickupGlow;
            idle.range = 9f;
            idle.intensity = 3.2f;
            idle.shadows = LightShadows.None;

            var pickup = go.AddComponent<ExhibitPickup>();
            pickup.itemId = item.id;
            pickup.chapter = item.chapterIndex;
            pickup.visual = visual;
            pickup.idleLight = idle;
        }
    }

    static void MakePickupShape(Transform parent, ExhibitCatalogue.Shape shape)
    {
        switch (shape)
        {
            case ExhibitCatalogue.Shape.원반:
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Shape";
                Object.DestroyImmediate(disc.GetComponent<Collider>());
                disc.transform.SetParent(parent, false);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                disc.transform.localScale = new Vector3(0.8f, 0.06f, 0.8f);
                disc.GetComponent<Renderer>().sharedMaterial = MaterialAsset(ColPickupItem);
                break;

            case ExhibitCatalogue.Shape.종이:
                Cube(parent, "Shape", Vector3.zero, new Vector3(0.62f, 0.08f, 0.82f),
                     ColPickupItem, keepCollider: false);
                break;

            default:
                Cube(parent, "Shape", Vector3.zero, new Vector3(0.55f, 0.5f, 0.42f),
                     ColPickupItem, keepCollider: false);
                break;
        }
    }

    // ==================================================================
    //  조각들
    // ==================================================================
    /// <summary>
    /// 야외 햇빛과 하늘빛.
    ///
    /// 예전엔 해만 놓고 환경광을 유니티 기본값(하늘 상자)에 맡겼다. 실내 씬은 둘 다
    /// 직접 잡아뒀는데 야외만 안 잡아둬서, 기본 하늘의 강한 환경광에 후처리까지 얹혀
    /// <b>화면이 통째로 하얗게 떴다</b>. 그게 트랙만 유독 "유니티 기본" 으로 보이던 이유다.
    ///
    /// 안개도 켠다. 먼 곳이 옅어지는 것(공기 원근)은 야외가 진짜로 보이는 가장 큰 단서인데,
    /// 없으면 100m 밖 담장이 코앞 담장과 똑같이 선명해서 그림처럼 납작해 보인다.
    /// </summary>
    public static void MakeSun()
    {
        var go = new GameObject("Sun");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.89f);
        light.intensity = 1.0f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.72f;
        go.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

        // 위는 하늘빛(차갑게), 아래는 땅에서 되튄 빛(따뜻하게). 실제 야외가 그렇다.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        // 지붕을 덮은 뒤로 위에서 오는 빛은 파란 하늘이 아니라 나무 천장의 반사다(2026-09-16).
        RenderSettings.ambientSkyColor     = new Color(0.54f, 0.50f, 0.44f);
        RenderSettings.ambientEquatorColor = new Color(0.44f, 0.44f, 0.41f);
        RenderSettings.ambientGroundColor  = new Color(0.28f, 0.26f, 0.22f);

        // 먼 곳을 옅게. 담장 바깥(약 110m)까지 서서히 하늘색에 잠기게 잡았다.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        // 실내 먼지빛. 카메라 배경색과 같은 값이라 먼 벽이 배경으로 자연스럽게 녹는다 —
        // 두 색이 다르면 거기가 하늘처럼 띠로 보인다.
        RenderSettings.fogColor = new Color(0.55f, 0.51f, 0.45f);
        RenderSettings.fogStartDistance = 55f;
        RenderSettings.fogEndDistance = 230f;
    }

    static void MakeGround(float size)
    {
        // 윗면이 정확히 y = 0 에 오게 두께의 절반만큼 내린다
        float thickness = Mathf.Max(1f, size * 0.025f);
        var go = Cube(null, "Ground", new Vector3(0f, -thickness * 0.5f - 0.05f, 0f),
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
        // 기본값은 Skybox 라 유니티 기본 하늘이 그대로 나온다. 지붕 틈으로 그게 비치면
        // 지붕을 덮은 의미가 없어서 단색으로 바꾼다.
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.51f, 0.45f);   // 트랙 안개색과 같은 값
        cam.farClipPlane = 400f;
        cam.nearClipPlane = 0.05f;
        go.AddComponent<AudioListener>();
        go.AddComponent<UniversalAdditionalCameraData>();   // URP 는 이게 있어야 정상 렌더링
        go.tag = "MainCamera";
        return cam;
    }

    /// <summary>isPlayer 가 false 면 AI 카트다 — 이야기 수집품을 줍지 못한다.</summary>
    public static KartController MakeKart(Vector3 position, Quaternion rotation, bool isPlayer = true)
    {
        var go = new GameObject("Kart");
        go.transform.SetPositionAndRotation(position, rotation);

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 180f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 4f;

        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(1.0f, 0.45f, 1.4f) * KartScale;
        box.center = new Vector3(0f, -0.06f, 0f) * KartScale;

        // 카트 본체를 먼저 붙인다 — 모델을 얼마나 내려야 하는지(rideHeight)를 알아야 해서.
        var kart = go.AddComponent<KartController>();
        kart.groundMask = ~(1 << LayerIgnoreRaycast);
        kart.rideHeight = BaseRideHeight * KartScale;   // 커진 만큼 서스펜션도 같이 올린다

        var visual = new GameObject("KartVisual").transform;
        visual.SetParent(go.transform, false);
        visual.localScale = Vector3.one * KartScale;
        kart.visual = visual;

        // 진짜 모델이 있으면 그걸 쓰고, 없으면 회색 상자로 돌아간다.
        // 모델이 KartVisual **안에** 들어간다 — 껍데기(콜라이더·물리)는 그대로 두고 그림만 바뀐다.
        //
        // 내리는 값은 **배율을 곱하기 전** 값이다. KartVisual 이 이미 KartScale 배로 커져 있어서
        // 그 안의 local 값에 배율이 한 번 더 곱해진다 — 두 번 곱하면 땅에 파묻힌다.
        if (!AttachKartModel(visual, BaseRideHeight)) MakeGreyBoxKart(visual);

        // 캐릭터가 앉을 자리. 지금은 비어 있어도 되고, 3등신 FBX 가 오면 여기 자식으로 넣으면 된다.
        // 발끝이 원점인 모델이 그대로 앉은 키(0.95m)에 맞는다.
        var driverAnchor = new GameObject("DriverAnchor").transform;
        driverAnchor.SetParent(visual, false);
        driverAnchor.localPosition = new Vector3(0f, 0.16f, -0.06f);

        // 이야기 수집품은 이 표시가 붙은 카트만 주울 수 있다. AI 카트에는 안 붙인다.
        if (isPlayer) go.AddComponent<PlayerKart>();

        // 순위 계산용. 플레이어든 AI 든 한 대씩 달고 다닌다.
        var progress = go.AddComponent<RaceProgress>();
        progress.racerName = isPlayer ? "나" : go.name;

        // 바퀴 돌리기는 모델을 넣을 때 붙는다. 여기서 연결을 맞춰준다.
        var wheels = go.GetComponent<KartWheels>();
        if (wheels != null) wheels.kart = kart;

        SetLayerRecursive(go, LayerIgnoreRaycast);

        return kart;
    }

    // ------------------------------------------------------------------
    //  카트 모델
    // ------------------------------------------------------------------
    /// <summary>
    /// 캐릭터마다 어떤 카트를 타는지. <b>새 카트 FBX 를 만들면 여기 한 줄만 더하면 된다.</b>
    /// castId 는 Cast.cs 의 id 와 같아야 하고, 파일이 없는 줄은 조용히 건너뛴다.
    /// </summary>
    static readonly (string castId, string path)[] KartModels =
    {
        ("세진", "Assets/Cart_model/JIN_FIN_CART.fbx"),
        ("세운", "Assets/Cart_model/WOON_CART_FIN.fbx"),
        ("시우", "Assets/Cart_model/Siwoo_cart.fbx"),
        ("이감", "Assets/Cart_model/YI_gam_cart.fbx"),

        // ★ 악당 둘 — <b>결승에서만</b> 나온다(2026-09-18 유저: *"플레이어가 로비에서
        // 선택 못 하게 하고 마지막에만 나오게"*). 로비 쪽은 이미 막혀 있다:
        // 5·6번 받침대가 `CharacterStand.locked` 라 `Selectable` 이 false 고 이름도 "???" 야.
        // AI 쪽은 `KartSkin.AiCastId` 가 평소 판에서 이 둘을 후보에서 뺀다.
        ("개발업자", "Assets/Cart_model/BOSS_DEV_GOLD.fbx"),
        ("시의원",   "Assets/Cart_model/BOSS_COUNCIL_MAGENTA.fbx"),
    };

    /// <summary>
    /// 유저가 만든 카트 FBX 를 껍데기 안에 넣는다. 파일이 없으면 false 를 돌려주고
    /// 회색 상자로 돌아간다 — 모델이 없다고 씬 만들기가 실패하면 안 되니까.
    ///
    /// 여기서 하는 일은 셋뿐이다:
    ///   · 그림에 붙은 콜라이더를 전부 뗀다 (충돌은 루트 BoxCollider 하나만 맡는다)
    ///   · 바퀴마다 **카트와 축이 맞는 껍데기**를 씌운다 (모델 방향과 무관하게 굴리고 꺾으려고)
    ///   · KartWheels 에 그 껍데기와 바퀴를 꽂아준다
    /// </summary>
    static bool AttachKartModel(Transform visual, float rideHeight)
    {
        var wheels = visual.parent.gameObject.AddComponent<KartWheels>();
        var skinner = visual.parent.gameObject.AddComponent<KartSkin>();
        skinner.wheels = wheels;

        var skins = new System.Collections.Generic.List<KartSkin.Skin>();

        foreach (var (castId, path) in KartModels)
        {
            var skin = BuildSkin(visual, rideHeight, castId, path);
            if (skin != null) skins.Add(skin);
        }

        if (skins.Count == 0)
        {
            Object.DestroyImmediate(skinner);
            Object.DestroyImmediate(wheels);
            Debug.LogWarning("[Racing] 카트 FBX 를 하나도 못 찾아서 회색 상자로 만들었어.");
            return false;
        }

        skinner.skins = skins.ToArray();

        // 씬 안에서는 첫 번째만 보이게 해둔다. 실제로 어느 것이 켜질지는
        // 재생할 때 KartSkin 이 로비에서 고른 캐릭터를 보고 정한다.
        for (int i = 0; i < skins.Count; i++) skins[i].model.SetActive(i == 0);
        wheels.Bind(skins[0].steerPivots, skins[0].spinWheels, skins[0].steeringWheel);

        var names = "";
        foreach (var s in skins) names += s.castId + " ";
        Debug.Log($"[Racing] 카트 {skins.Count}대 준비: {names.Trim()} — 로비에서 고른 캐릭터의 것이 켜진다.");
        return true;
    }

    /// <summary>카트 FBX 하나를 껍데기 안에 넣고, 바퀴 껍데기까지 씌워서 돌려준다.</summary>
    static KartSkin.Skin BuildSkin(Transform visual, float rideHeight, string castId, string path)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null)
        {
            Debug.Log($"[Racing] {castId} 카트({System.IO.Path.GetFileName(path)})는 아직 없어서 건너뛴다.");
            return null;
        }

        EnsureModelImportSettings(path);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);

        // ★ <b>프리팹 인스턴스 안의 자식은 부모를 못 바꾼다.</b> 아래에서 바퀴를 `_Pivot` 으로
        // 옮기는데, 그게 매번 «Setting the parent of a transform which resides in a Prefab
        // instance is not possible» 로 <b>조용히 실패</b>하고 있었다 — 씬을 한 번 구울 때마다
        // 카트 수 × 바퀴 4개씩 에러가 쌓여서, 2026-09-18 에 콘솔에 96개가 찍혔다.
        //
        // 결과는 «에러만 많고 굴러는 간다» 가 아니다: <b>앞바퀴 조향 껍데기가 비어서
        // 핸들을 꺾어도 앞바퀴가 안 돌아간다.</b> 연결을 끊어서 평범한 오브젝트로 만든다 —
        // 어차피 이 안의 물건을 씬에서 옮기고 있으니 프리팹 연결을 유지할 이유가 없다.
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                                           InteractionMode.AutomatedAction);

        // 임포터가 정한 회전·스케일은 그대로 둔다. 여기서 1 로 덮으면
        // 단위 변환(100배)이 걸린 모델은 백분의 일로 쪼그라든다.
        instance.transform.SetParent(visual, false);

        // ★ 모델은 **바퀴 밑바닥이 원점**이고(규격대로), 카트 루트는 서스펜션 때문에
        // 지면에서 rideHeight 만큼 떠 있다. 그대로 넣으면 차가 그 높이만큼 공중에 뜬다.
        instance.transform.localPosition = new Vector3(0f, -rideHeight, 0f);

        // 그림에는 콜라이더를 두지 않는다. 모델을 갈아끼워도 물리가 안 바뀌게 (CLAUDE.md 규칙 2)
        foreach (var c in instance.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(c);

        var skin = new KartSkin.Skin
        {
            castId = castId,
            model = instance,
            steeringWheel = FindDeep(instance.transform, "Steering"),
        };

        var front = new System.Collections.Generic.List<Transform>();
        var spin = new System.Collections.Generic.List<Transform>();

        foreach (var name in new[] { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" })
        {
            var wheel = FindDeep(instance.transform, name);
            if (wheel == null) continue;

            // 카트와 축이 맞는 껍데기를 만들어 그 안에 바퀴를 넣는다.
            // 이렇게 해두면 모델이 어떤 방향으로 만들어졌든 X = 축, Y = 조향이 된다.
            // 껍데기를 모델 **안에** 둔다 — 카트를 끄면 바퀴 껍데기도 같이 꺼지게.
            var pivot = new GameObject(name + "_Pivot").transform;
            pivot.SetParent(instance.transform, false);
            pivot.SetPositionAndRotation(wheel.position, visual.rotation);
            wheel.SetParent(pivot, worldPositionStays: true);

            spin.Add(wheel);
            if (name.StartsWith("Wheel_F")) front.Add(pivot);
        }

        skin.steerPivots = front.ToArray();
        skin.spinWheels = spin.ToArray();

        // 규격과 얼마나 맞는지 찍어둔다. 다음에 모델을 다시 뽑았을 때 크기가 틀어지면 여기서 보인다.
        // 재는 동안 카트를 정면으로 돌려둔다 — 출발선 방향으로 돌아가 있으면 전폭과 전장이 섞여 나온다.
        var root = visual.parent;
        var saved = root.rotation;
        root.rotation = Quaternion.identity;

        var bounds = new Bounds(visual.position, Vector3.zero);
        bool first = true;
        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }

        root.rotation = saved;

        Debug.Log($"[Racing] {castId} 카트 {model.name}  " +
                  $"전폭 {bounds.size.x:0.00} / 높이 {bounds.size.y:0.00} / 전장 {bounds.size.z:0.00} m " +
                  $"(규격 1.10 × 1.50) · 바퀴 {spin.Count} · 앞바퀴 {front.Count} · " +
                  $"운전대 {(skin.steeringWheel != null ? "있음" : "없음")}");
        return skin;
    }

    /// <summary>
    /// FBX 임포트 설정을 고친다. 모델이 통째로 흰색으로 나오던 이유가 여기 있었다.
    ///
    /// 임포터 기본값이 <b>머티리얼을 프로젝트에서 찾아 쓰기(External)</b> 라서,
    /// 그 이름의 .mat 파일이 없으면 색을 못 찾고 기본 흰색으로 떨어진다.
    /// FBX 안에 색이 멀쩡히 들어 있는데도 그렇다 — 모델 잘못이 아니야.
    /// 안에 든 색을 그대로 쓰도록 바꾼다.
    ///
    /// 나중에 캐릭터마다 카트 색을 바꾸고 싶어지면(기획서 §3.5) 그때 머티리얼을
    /// 바깥으로 꺼내면 된다 — 인스펙터의 Materials 탭에서 Extract Materials.
    /// </summary>
    static void EnsureModelImportSettings(string path)
    {
        if (AssetImporter.GetAtPath(path) is not ModelImporter importer) return;

        bool changed = false;

        if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
        {
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            changed = true;
        }
        if (importer.materialImportMode == ModelImporterMaterialImportMode.None)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            changed = true;
        }
        // 카메라와 조명이 딸려 오면 씬에 쓰레기가 생긴다 (블렌더 임포터도 조명 든 FBX 에서 죽는다)
        if (importer.importCameras) { importer.importCameras = false; changed = true; }
        if (importer.importLights) { importer.importLights = false; changed = true; }

        if (!changed) return;

        importer.SaveAndReimport();
        Debug.Log($"[Racing] {System.IO.Path.GetFileName(path)} 임포트 설정을 고쳤어 — " +
                  "FBX 안의 색을 그대로 쓰도록. (전엔 밖에서 .mat 을 찾다가 못 찾아서 흰색이었다)");
    }

    /// <summary>이름에 해당 조각이 들어간 자식을 찾는다. 빌드할 때 한 번만 쓰고, 결과는 인스펙터에 꽂는다.</summary>
    static Transform FindDeep(Transform root, string contains)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.Contains(contains)) return t;
        return null;
    }

    /// <summary>모델이 없을 때 쓰는 예전 회색 상자 카트.</summary>
    static void MakeGreyBoxKart(Transform visual)
    {
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
    }

    public static KartCamera MakeKartCamera(KartController kart)
    {
        var go = new GameObject("KartCamera");
        SetUpCamera(go);
        // 레이스 씬엔 다른 카메라가 없으니 이게 MainCamera 다 (SetUpCamera 가 이미 태그함)

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
                           Color color, bool keepCollider = true, Finish? finish = null)
        => Primitive(parent, PrimitiveType.Cube, name, localPos, scale, color, keepCollider, finish);

    public static GameObject Capsule(Transform parent, string name, Vector3 localPos, Vector3 scale,
                              Color color, bool keepCollider = true, Finish? finish = null)
        => Primitive(parent, PrimitiveType.Capsule, name, localPos, scale, color, keepCollider, finish);

    public static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 localPos,
                                Vector3 scale, Color color, bool keepCollider = true,
                                Finish? finish = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        // 마감을 안 적으면 색으로 고른다 — 팔레트 색이 곧 재질이라(Surfaces.cs) 부르는 쪽마다
        // 일일이 적을 필요가 없다. 예전엔 여기서 전부 무광으로 만들어서 살창도 석등도 안 빛났다.
        go.GetComponent<Renderer>().sharedMaterial =
            MaterialAsset(color, finish ?? FlatMaterial.FinishFor(color));

        if (!keepCollider)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return go;
    }

    // 마감(Finish) 은 Scripts/Surfaces.cs 로 옮겼다 — 런타임(캠퍼스/트랙)도 같은 표를 써야 해서.

    /// <summary>단색 머티리얼을 진짜 .mat 에셋으로 만든다 (씬에 저장돼야 하니까).</summary>
    public static Material MaterialAsset(Color color) => MaterialAsset(color, Finish.무광);

    public static Material MaterialAsset(Color color, Finish finish)
    {
        string hex = ColorUtility.ToHtmlStringRGB(color);

        // 무광은 이름을 그대로 둔다 — 이미 만들어진 씬들이 Flat_XXXXXX.mat 을 가리키고 있어서,
        // 이름이 바뀌면 그 씬들의 재질이 통째로 끊어진다.
        string path = finish == Finish.무광
            ? $"{MaterialFolder}/Flat_{hex}.mat"
            : $"{MaterialFolder}/Flat_{hex}_{finish}.mat";

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
        ApplyFinish(mat, color, finish);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void ApplyFinish(Material mat, Color color, Finish finish)
    {
        // 광택 값은 Surfaces.cs 의 표 하나에서만 온다.
        float smoothness = finish.Smoothness();
        float metallic = finish.Metallic();

        var baseColor = finish == Finish.유리 ? new Color(color.r, color.g, color.b, 0.16f) : color;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

        if (finish == Finish.유리) MakeTransparent(mat);

        if (finish == Finish.발광)
        {
            // 블룸이 켜져 있으면 이게 실제로 눈부시게 번진다. 코브 조명·간판에 쓴다.
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2.2f);
        }
    }

    /// <summary>
    /// URP 의 투명 설정은 값 한 개로 안 끝난다. 표면 종류 · 블렌드 · 깊이 쓰기 · 키워드 · 렌더 큐를
    /// 전부 맞춰야 하고, 하나라도 빠지면 유리가 그냥 불투명한 흰 상자로 나온다.
    /// </summary>
    static void MakeTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1f);                       // 0 불투명 / 1 투명
        mat.SetFloat("_Blend", 0f);                         // Alpha
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }
}
