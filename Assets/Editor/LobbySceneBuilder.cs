using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 로비 씬을 만든다. 걸어다니면서 캐릭터를 고르고 출발문으로 들어가면 트랙이 시작되는 곳.
///
/// 캐릭터 자리는 6개. 3명만 만들어도 나머지는 "coming soon" 으로 뜨니까 비어 보이지 않아.
/// **네 캐릭터 FBX 는 각 자리의 `ModelAnchor` 에 자식으로 넣으면 돼.**
///
/// 이 메뉴를 다시 누르면 Lobby.unity 를 **덮어쓴다.** 손으로 뭘 넣기 시작한 뒤엔 누르지 마.
/// </summary>
public static class LobbySceneBuilder
{
    const string SceneFolder = "Assets/Scenes";
    const string LobbyPath   = SceneFolder + "/Lobby.unity";
    const string TrackPath   = SceneFolder + "/Track.unity";
    const string TestbedPath = SceneFolder + "/Testbed.unity";

    // 디저트 랜드 팔레트
    static readonly Color ColFloor    = new Color32(0xF2, 0xE4, 0xC9, 0xFF);  // 바닐라 바닥
    static readonly Color ColFloorInlay= new Color32(0xE6, 0xC9, 0xA8, 0xFF); // 무늬
    static readonly Color ColFencePink = new Color32(0xF0, 0x9B, 0xB0, 0xFF); // 사탕 분홍
    static readonly Color ColFenceWhite= new Color32(0xFB, 0xF6, 0xEE, 0xFF);
    static readonly Color ColPedestal  = new Color32(0x8A, 0x7E, 0x74, 0xFF); // 초콜릿 받침
    static readonly Color ColGateBanner= new Color32(0x7F, 0xD4, 0xA8, 0xFF); // 멜론소다

    // 6명 자리의 임시 색 — 디저트 맛으로 구분
    static readonly (string name, Color color)[] Flavors =
    {
        ("Melon Soda", new Color32(0x7F, 0xD4, 0xA8, 0xFF)),
        ("Strawberry", new Color32(0xF0, 0x9B, 0xB0, 0xFF)),
        ("Lemon",      new Color32(0xF5, 0xD9, 0x82, 0xFF)),
        ("Grape",      new Color32(0xB4, 0x9B, 0xD8, 0xFF)),
        ("Chocolate",  new Color32(0x8B, 0x5E, 0x3C, 0xFF)),
        ("Mint",       new Color32(0x9B, 0xD8, 0xD0, 0xFF)),
    };

    const float FloorRadius = 18f;
    const float StandRadius = 9f;
    const float FenceRadius = 17.4f;

    [MenuItem("Racing/로비 씬 만들기")]
    public static void BuildLobby()
    {
        Directory.CreateDirectory(SceneFolder);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        TestSceneBuilder.MakeSun();
        MakeFloor();
        MakeFence();

        var stands = MakeStands();
        var gate = MakeGate(new Vector3(0f, 0f, 11f));

        // 자리들이 -Z 쪽에 늘어서 있으니, 플레이어는 그쪽을 보고 시작한다
        var player = TestSceneBuilder.MakePlayer(new Vector3(0f, 0.2f, 2.5f), 180f);
        gate.player = player.controller.transform;

        var rig = new GameObject("GameRig");
        rig.AddComponent<SceneNavigator>();

        var selector = rig.AddComponent<LobbySelector>();
        selector.player = player.controller.transform;
        selector.stands = stands;

        var hud = rig.AddComponent<LobbyHUD>();
        hud.selector = selector;
        hud.gate = gate;
        hud.player = player.controller;

        EditorSceneManager.SaveScene(scene, LobbyPath);
        RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Lobby.unity 생성 완료. F1 로비 / F2 트랙 / F3 테스트베드.");
    }

    /// <summary>있는 씬만 골라서 순서대로 빌드 설정에 넣는다. Lobby → Track → Testbed.</summary>
    public static void RegisterScenes()
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var path in new[] { LobbyPath, TrackPath, TestbedPath })
            if (File.Exists(path)) list.Add(new EditorBuildSettingsScene(path, true));

        EditorBuildSettings.scenes = list.ToArray();
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// 원기둥 모양이지만 콜라이더는 상자로 붙인다.
    ///
    /// 유니티 원기둥에는 CapsuleCollider 가 딸려오는데, 납작하게 눌러 놓으면
    /// 캡슐이 "지름만큼 커다란 공" 으로 변해서 바닥 역할을 전혀 못 한다.
    /// (반지름 18m 짜리 공 안에 서 있게 되니 계속 아래로 빠진다.)
    ///
    /// 원기둥 메시는 높이가 2 단위라서, BoxCollider 크기를 (1, 2, 1) 로 줘야
    /// 눈에 보이는 윗면과 실제로 밟히는 면이 같은 높이가 된다.
    /// </summary>
    static GameObject Disc(Transform parent, string name, Vector3 localPosition,
                           Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);

        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = TestSceneBuilder.MaterialAsset(color);
        return go;
    }

    static void MakeFloor()
    {
        // 윗면이 정확히 y = 0
        var floor = Disc(null, "Floor", new Vector3(0f, -0.5f, 0f),
                         new Vector3(FloorRadius * 2f, 0.5f, FloorRadius * 2f), ColFloor);
        floor.isStatic = true;

        // 가운데 원형 무늬 — 위치 감각을 잡아주는 용도
        var inlay = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        inlay.name = "FloorInlay";
        Object.DestroyImmediate(inlay.GetComponent<Collider>());
        inlay.transform.position = new Vector3(0f, 0.01f, 0f);
        inlay.transform.localScale = new Vector3(11f, 0.01f, 11f);
        inlay.GetComponent<Renderer>().sharedMaterial = TestSceneBuilder.MaterialAsset(ColFloorInlay);
    }

    /// <summary>가장자리 사탕 울타리. 장식이면서 동시에 떨어지는 걸 막는 벽이다.</summary>
    static void MakeFence()
    {
        var fence = new GameObject("Fence").transform;
        const int segments = 40;

        for (int i = 0; i < segments; i++)
        {
            Vector3 p0 = OnCircle((float)i / segments, FenceRadius);
            Vector3 p1 = OnCircle((float)(i + 1) / segments, FenceRadius);
            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 dir = p1 - p0;
            float len = dir.magnitude;

            var seg = TestSceneBuilder.Cube(fence, $"Fence_{i:00}", Vector3.zero,
                                            new Vector3(0.35f, 1.3f, len * 1.06f),
                                            i % 2 == 0 ? ColFencePink : ColFenceWhite);
            seg.transform.SetPositionAndRotation(mid + Vector3.up * 0.65f,
                                                 Quaternion.LookRotation(dir.normalized, Vector3.up));
            seg.isStatic = true;
        }
    }

    static CharacterStand[] MakeStands()
    {
        var root = new GameObject("CharacterStands").transform;
        var result = new CharacterStand[GameSelection.StandCount];

        for (int i = 0; i < GameSelection.StandCount; i++)
        {
            float t = GameSelection.StandCount == 1 ? 0.5f : (float)i / (GameSelection.StandCount - 1);
            float angle = Mathf.Lerp(205f, 335f, t) * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * StandRadius;

            var go = new GameObject($"Stand_{i + 1}_{Flavors[i].name.Replace(" ", "")}");
            go.transform.SetParent(root, false);
            // 가운데를 바라보게 세운다
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-pos.normalized, Vector3.up));

            // 받침대 — 지름 1.6, 높이 0.4. 바닥과 같은 이유로 콜라이더는 상자.
            var pedestal = Disc(go.transform, "Pedestal", new Vector3(0f, 0.2f, 0f),
                                new Vector3(1.6f, 0.2f, 1.6f), ColPedestal);

            // ★ 캐릭터 FBX 가 들어갈 자리
            var anchor = new GameObject("ModelAnchor").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, 0.4f, 0f);

            // 임시 자리표시 — 치비 서 있는 키 1.15m (규격서)
            var placeholder = TestSceneBuilder.Capsule(anchor, "Placeholder",
                                                       new Vector3(0f, 0.575f, 0f),
                                                       new Vector3(0.45f, 0.575f, 0.45f),
                                                       Flavors[i].color, keepCollider: false);

            var stand = go.AddComponent<CharacterStand>();
            stand.index = i;
            stand.displayName = Flavors[i].name;
            stand.modelAnchor = anchor;
            stand.placeholder = placeholder;
            stand.baseRenderer = pedestal.GetComponent<Renderer>();
            stand.baseColor = ColPedestal;

            result[i] = stand;
        }
        return result;
    }

    static StartGate MakeGate(Vector3 position)
    {
        var go = new GameObject("StartGate");
        go.transform.position = position;

        // 사탕 지팡이 기둥 두 개 + 위에 걸린 현수막
        for (int side = -1; side <= 1; side += 2)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = side < 0 ? "Post_L" : "Post_R";
            post.transform.SetParent(go.transform, false);
            post.transform.localPosition = new Vector3(side * 2.4f, 2f, 0f);
            post.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
            post.GetComponent<Renderer>().sharedMaterial = TestSceneBuilder.MaterialAsset(ColFencePink);
        }

        var banner = TestSceneBuilder.Cube(go.transform, "Banner", new Vector3(0f, 4.2f, 0f),
                                           new Vector3(5.4f, 0.9f, 0.3f), ColGateBanner,
                                           keepCollider: false);
        banner.isStatic = true;

        var gate = go.AddComponent<StartGate>();
        gate.targetScene = "Track";
        return gate;
    }

    static Vector3 OnCircle(float t01, float radius)
    {
        float a = t01 * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
    }
}
