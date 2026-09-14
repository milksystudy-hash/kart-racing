using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 전시실 씬을 만든다 — 레이스에서 모은 증거와 기념품을 진열하는 곳.
///
/// 상점의 자리를 대신하는 컨텐츠다. 사는 게 아니라 **모은 걸 보는** 방식이라
/// 돈·구매·재고 같은 시스템이 하나도 필요 없고, 기획서 §3.4 의 해금 보상이
/// 실제로 어디에 쌓이는지를 보여주는 장소가 된다.
///
/// 조작은 로비와 똑같다 (LobbyOrbitCamera 재사용) — 끌어서 둘러보고, 클릭해서 읽는다.
///
/// 이 메뉴를 다시 누르면 Gallery.unity 를 **덮어쓴다.**
/// </summary>
public static class GallerySceneBuilder
{
    const string SceneFolder = "Assets/Scenes";
    const string GalleryPath = SceneFolder + "/Gallery.unity";

    const float HallWidth = 30f;
    const float HallDepth = 26f;
    const float WallHeight = 6.5f;
    const float CaseRadius = 8.2f;

    // 로비와 같은 재료 — 같은 건물 안이니까
    static readonly Color ColFloor     = new Color32(0xC6, 0xC0, 0xB2, 0xFF);
    static readonly Color ColFloorTrim = new Color32(0x8A, 0x6A, 0x48, 0xFF);
    static readonly Color ColWallCream = new Color32(0xEF, 0xE7, 0xD6, 0xFF);
    static readonly Color ColWallMint  = new Color32(0xB4, 0xCD, 0xBC, 0xFF);
    static readonly Color ColWoodDark  = new Color32(0x6B, 0x4A, 0x33, 0xFF);
    static readonly Color ColRoofTeal  = new Color32(0x4E, 0x7A, 0x70, 0xFF);
    static readonly Color ColStone     = new Color32(0xB0, 0xAC, 0xA0, 0xFF);
    static readonly Color ColLantern   = new Color32(0xF5, 0xC0, 0x69, 0xFF);
    static readonly Color ColCaseGlass = new Color32(0xD6, 0xE4, 0xDC, 0xFF);

    enum Shape { 원반, 종이, 상자 }

    /// <summary>전시품 목록. 전부 기획서의 줄거리에서 나오는 물건들이다.</summary>
    static readonly (string id, string name, string chapter, Shape shape, string desc)[] Items =
    {
        ("coin",      "기념 코인",        "제1장 · 사라진 관람객", Shape.원반,
         "박물관 입장 때 나눠주던 코인. 뒷면에 관람 일자가 찍혀 있어서, 모으면 그날 누가 다녀갔는지가 드러난다."),
        ("ledger",    "관람 기록부",      "제1장 · 사라진 관람객", Shape.종이,
         "코인에서 복원한 방문객 명단. 시에서 발표한 관람객 수보다 훨씬 많은 이름이 적혀 있었다."),
        ("survey",    "안전진단서 원본",  "제2장 · 조작된 안전진단", Shape.종이,
         "원본의 결론은 '보수 필요' 였다. 공개된 사본에는 '즉시 철거' 로 바뀌어 있었다."),
        ("marker",    "붉은 철거 표식",   "제2장 · 조작된 안전진단", Shape.상자,
         "개발업자 측이 트랙에 세워 둔 표식. 부딪혀 뜯어보니 안쪽에 서류 조각이 접혀 있었다."),
        ("signature", "관장의 서명",      "제3장 · 관장의 서명",    Shape.종이,
         "조건부 매각 문서에 남은 서명. 비리를 계획하지는 않았지만, 사실을 숨긴 대가가 여기 남았다."),
        ("contract",  "비밀 계약서",      "제3장 · 관장의 서명",    Shape.종이,
         "시의원과 개발업자 사이의 이면 계약. 선거 지원과 이권이 항목으로 적혀 있다."),
        ("recorder",  "중계 기록 장치",   "마지막 장 · 철거 전야",  Shape.상자,
         "어두워진 트랙을 가로질러 결승선까지 옮긴 장치. 이것으로 전말이 시 전역에 생중계됐다."),
        ("blueprint", "골든베어 조감도",  "프롤로그 · 철거 통지서", Shape.종이,
         "박물관 자리에 세우려던 리조트 조감도. 없애지 않고 전시실에 남겨 두기로 했다."),
    };

    [MenuItem("Racing/전시실 씬 만들기")]
    public static void BuildGallery()
    {
        Directory.CreateDirectory(SceneFolder);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        TestSceneBuilder.MakeSun();
        MakeHall();
        MakeLanterns();

        var cases = MakeCases();
        var orbit = MakeOrbitCamera();

        var rig = new GameObject("GameRig");
        rig.AddComponent<SceneNavigator>();

        var selector = rig.AddComponent<GallerySelector>();
        selector.galleryCamera = orbit.GetComponent<Camera>();
        selector.orbit = orbit;
        selector.cases = cases;

        var hud = rig.AddComponent<GalleryHUD>();
        hud.selector = selector;

        EditorSceneManager.SaveScene(scene, GalleryPath);
        LobbySceneBuilder.RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Gallery.unity 생성 완료. F1 로비 / F2 트랙 / F3 전시실 / F4 테스트베드.");
    }

    // ==================================================================
    //  홀
    // ==================================================================
    static void MakeHall()
    {
        var root = new GameObject("Hall").transform;

        var floor = TestSceneBuilder.Cube(root, "Floor", new Vector3(0f, -0.25f, 0f),
                                          new Vector3(HallWidth, 0.5f, HallDepth), ColFloor);
        floor.isStatic = true;

        // 가운데 통로를 나무로 깔아서 시선이 중앙으로 모이게
        TestSceneBuilder.Cube(root, "Runner", new Vector3(0f, 0.01f, 0f),
                              new Vector3(4.5f, 0.02f, HallDepth - 3f), ColFloorTrim, keepCollider: false);

        float halfW = HallWidth * 0.5f, halfD = HallDepth * 0.5f;
        Wall(root, "WallN", new Vector3(0f, 0f, -halfD - 0.25f), new Vector3(HallWidth + 1f, 0.5f));
        Wall(root, "WallS", new Vector3(0f, 0f,  halfD + 0.25f), new Vector3(HallWidth + 1f, 0.5f));
        Wall(root, "WallW", new Vector3(-halfW - 0.25f, 0f, 0f), new Vector3(0.5f, HallDepth + 1f));
        Wall(root, "WallE", new Vector3( halfW + 0.25f, 0f, 0f), new Vector3(0.5f, HallDepth + 1f));

        // 기둥과 보 — 천장은 덮지 않아서 위에서 내려다볼 수 있다
        var frame = new GameObject("Frame").transform;
        frame.SetParent(root, false);
        for (int i = -2; i <= 2; i++)
        {
            float x = i * (halfW - 1f) / 2.2f;
            Column(frame, $"Col_N{i + 2}", new Vector3(x, 0f, -halfD + 0.8f));
            Column(frame, $"Col_S{i + 2}", new Vector3(x, 0f,  halfD - 0.8f));
        }
        for (int i = -3; i <= 3; i++)
        {
            float z = i * (halfD - 1f) / 3.4f;
            var beam = TestSceneBuilder.Cube(frame, $"Beam_{i + 3}", new Vector3(0f, WallHeight - 0.35f, z),
                                             new Vector3(HallWidth, 0.4f, 0.55f), ColWoodDark,
                                             keepCollider: false);
            beam.isStatic = true;
        }
    }

    static void Wall(Transform parent, string name, Vector3 basePosition, Vector2 footprint)
    {
        var go = TestSceneBuilder.Cube(parent, name, basePosition + Vector3.up * (WallHeight * 0.5f),
                                       new Vector3(footprint.x, WallHeight, footprint.y), ColWallCream);
        go.isStatic = true;

        bool alongX = footprint.x > footprint.y;
        Vector3 panel = alongX ? new Vector3(footprint.x - 1.5f, 2.1f, footprint.y + 0.12f)
                               : new Vector3(footprint.x + 0.12f, 2.1f, footprint.y - 1.5f);
        TestSceneBuilder.Cube(parent, name + "_Panel", basePosition + Vector3.up * 1.55f, panel,
                              ColWallMint, keepCollider: false).isStatic = true;

        // 벽 위 청록 기와 띠
        Vector3 cap = alongX ? new Vector3(footprint.x + 1.2f, 0.25f, footprint.y + 1.2f)
                             : new Vector3(footprint.x + 1.2f, 0.25f, footprint.y + 1.2f);
        TestSceneBuilder.Cube(parent, name + "_Cap", basePosition + Vector3.up * (WallHeight + 0.12f),
                              cap, ColRoofTeal, keepCollider: false).isStatic = true;
    }

    static void Column(Transform parent, string name, Vector3 position)
    {
        TestSceneBuilder.Cube(parent, name, position + Vector3.up * (WallHeight * 0.5f),
                              new Vector3(0.5f, WallHeight, 0.5f), ColWoodDark, keepCollider: false)
                        .isStatic = true;
        TestSceneBuilder.Cube(parent, name + "_Base", position + Vector3.up * 0.16f,
                              new Vector3(0.8f, 0.32f, 0.8f), ColStone, keepCollider: false)
                        .isStatic = true;
    }

    static void MakeLanterns()
    {
        var root = new GameObject("Lanterns").transform;
        Vector3[] spots =
        {
            new Vector3(-11f, 0f, -9f), new Vector3(11f, 0f, -9f),
            new Vector3(-11f, 0f,  9f), new Vector3(11f, 0f,  9f),
        };

        for (int i = 0; i < spots.Length; i++)
        {
            var go = new GameObject($"Lantern_{i + 1}").transform;
            go.SetParent(root, false);
            go.position = spots[i];

            TestSceneBuilder.Cube(go, "Shaft", new Vector3(0f, 0.9f, 0f),
                                  new Vector3(0.32f, 1.8f, 0.32f), ColStone, keepCollider: false);
            TestSceneBuilder.Cube(go, "Housing", new Vector3(0f, 2.1f, 0f),
                                  new Vector3(0.7f, 0.6f, 0.7f), ColLantern, keepCollider: false);
            TestSceneBuilder.Cube(go, "Cap", new Vector3(0f, 2.5f, 0f),
                                  new Vector3(1.05f, 0.2f, 1.05f), ColRoofTeal, keepCollider: false);

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(go, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.intensity = 2f;
            light.range = 10f;
            light.shadows = LightShadows.None;
        }
    }

    // ==================================================================
    //  진열장
    // ==================================================================
    static GalleryCase[] MakeCases()
    {
        var root = new GameObject("DisplayCases").transform;
        var result = new GalleryCase[Items.Length];

        for (int i = 0; i < Items.Length; i++)
        {
            var item = Items[i];
            float angle = ((float)i / Items.Length) * Mathf.PI * 2f + Mathf.PI * 0.5f;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * CaseRadius;

            var go = new GameObject($"Case_{i + 1}_{item.id}");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-pos.normalized, Vector3.up));

            // 마우스 광선 판정 — 진열장 전체를 덮는다
            var pick = go.AddComponent<BoxCollider>();
            pick.size = new Vector3(1.5f, 2.6f, 1.5f);
            pick.center = new Vector3(0f, 1.3f, 0f);
            pick.isTrigger = true;

            TestSceneBuilder.Cube(go.transform, "Base", new Vector3(0f, 0.5f, 0f),
                                  new Vector3(1.1f, 1f, 1.1f), ColWoodDark, keepCollider: false);

            // 유리장 대신 모서리 기둥 넷 + 윗판 — 투명 처리 없이도 진열장으로 읽힌다
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    TestSceneBuilder.Cube(go.transform, $"Post_{sx}_{sz}",
                                          new Vector3(sx * 0.47f, 1.62f, sz * 0.47f),
                                          new Vector3(0.07f, 1.24f, 0.07f), ColCaseGlass,
                                          keepCollider: false);
            TestSceneBuilder.Cube(go.transform, "CaseTop", new Vector3(0f, 2.28f, 0f),
                                  new Vector3(1.06f, 0.1f, 1.06f), ColCaseGlass, keepCollider: false);

            // 명판 — 마우스를 올리면 색이 바뀌는 부분
            var plaque = TestSceneBuilder.Cube(go.transform, "Plaque", new Vector3(0f, 0.62f, 0.58f),
                                               new Vector3(0.8f, 0.26f, 0.06f), ColWallMint,
                                               keepCollider: false);

            var anchor = new GameObject("ItemAnchor").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, 1.15f, 0f);

            var placeholder = MakePlaceholder(anchor, item.shape);

            var display = go.AddComponent<GalleryCase>();
            display.itemId = item.id;
            display.displayName = item.name;
            display.chapter = item.chapter;
            display.description = item.desc;
            display.itemAnchor = anchor;
            display.placeholder = placeholder;
            display.itemRenderer = placeholder.GetComponent<Renderer>();
            display.plaqueRenderer = plaque.GetComponent<Renderer>();

            result[i] = display;
        }
        return result;
    }

    /// <summary>전시품 임시 모양. 물건 종류가 눈으로 구분되게 셋으로 나눴다.</summary>
    static GameObject MakePlaceholder(Transform parent, Shape shape)
    {
        switch (shape)
        {
            case Shape.원반:
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Placeholder";
                Object.DestroyImmediate(disc.GetComponent<Collider>());
                disc.transform.SetParent(parent, false);
                disc.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                disc.transform.localScale = new Vector3(0.42f, 0.03f, 0.42f);
                disc.GetComponent<Renderer>().sharedMaterial = TestSceneBuilder.MaterialAsset(Color.grey);
                return disc;

            case Shape.종이:
                return TestSceneBuilder.Cube(parent, "Placeholder", new Vector3(0f, 0.03f, 0f),
                                             new Vector3(0.44f, 0.04f, 0.6f), Color.grey,
                                             keepCollider: false);

            default:
                return TestSceneBuilder.Cube(parent, "Placeholder", new Vector3(0f, 0.18f, 0f),
                                             new Vector3(0.4f, 0.36f, 0.3f), Color.grey,
                                             keepCollider: false);
        }
    }

    // ==================================================================
    static LobbyOrbitCamera MakeOrbitCamera()
    {
        var pivot = new GameObject("CameraPivot").transform;
        pivot.position = new Vector3(0f, 1.4f, 0f);

        var go = new GameObject("GalleryCamera");
        var cam = TestSceneBuilder.SetUpCamera(go);
        cam.backgroundColor = new Color(0.09f, 0.10f, 0.12f);
        cam.farClipPlane = 100f;

        var orbit = go.AddComponent<LobbyOrbitCamera>();
        orbit.pivot = pivot;
        orbit.distance = 11f;
        orbit.minDistance = 5f;
        orbit.maxDistance = 12f;
        orbit.pitch = 20f;
        orbit.minPitch = 2f;
        orbit.maxPitch = 55f;
        orbit.idleDelay = 6f;
        return orbit;
    }
}
