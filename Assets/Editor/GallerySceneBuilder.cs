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

    // 전시실은 로비보다 어둡다. 어두운 방에 전시품만 밝게 떠 있어야 박물관처럼 보인다.
    // 벽은 기획서 §4.4 가 지정한 짙은 남색 — 밝은 벽은 조명 웅덩이를 지워버린다.
    static readonly Color ColFloor     = new Color32(0x7A, 0x74, 0x6A, 0xFF);
    static readonly Color ColFloorTrim = new Color32(0x4A, 0x38, 0x28, 0xFF);
    static readonly Color ColWallNavy  = new Color32(0x3E, 0x4A, 0x6B, 0xFF);
    static readonly Color ColWallPanel = new Color32(0x31, 0x3B, 0x56, 0xFF);
    static readonly Color ColWoodDark  = new Color32(0x4E, 0x36, 0x26, 0xFF);
    static readonly Color ColRoofTeal  = new Color32(0x33, 0x52, 0x4C, 0xFF);
    static readonly Color ColStone     = new Color32(0x96, 0x92, 0x88, 0xFF);
    static readonly Color ColLantern   = new Color32(0xF5, 0xC0, 0x69, 0xFF);
    static readonly Color ColCaseGlass = new Color32(0xE8, 0xEF, 0xE8, 0xFF);
    static readonly Color ColFixture   = new Color32(0x1E, 0x20, 0x24, 0xFF);
    // 번호 스티커용 — 본관 박공의 곰 얼굴과 같은 색
    static readonly Color ColBearFace  = new Color32(0xE6, 0xDA, 0xC4, 0xFF);
    static readonly Color ColBearFur   = new Color32(0xA5, 0x75, 0x4A, 0xFF);
    static readonly Color ColRibbon    = new Color32(0xC4, 0x45, 0x3E, 0xFF);
    static readonly Color ColNumber    = new Color32(0x4A, 0x33, 0x26, 0xFF);

    // 전시품 목록은 ExhibitCatalogue 하나로 모았다.
    // 트랙에 놓는 수집품도 같은 목록을 읽어서, 주운 물건과 진열장이 어긋날 수가 없다.

    [MenuItem("Racing/전시실 씬 만들기", false, 3)]
    public static void BuildGallery()
    {
        Directory.CreateDirectory(SceneFolder);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeGalleryLighting();
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

        MakeReflectionProbe();
        MuseumLook.ApplyToOpenScene();   // 후처리 · 안티에일리어싱 — 이게 없으면 다 회색 상자로 보인다

        EditorSceneManager.SaveScene(scene, GalleryPath);
        LobbySceneBuilder.RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Gallery.unity 생성 완료. F1 로비 / F2 트랙 / F3 전시실 / F4 테스트베드.");
    }

    // ==================================================================
    //  조명 — 박물관 전시실의 핵심
    // ==================================================================
    /// <summary>
    /// 방 전체를 밝히지 않는다. 어둡게 깔고 전시품 위에만 빛을 떨어뜨린다 —
    /// 실제 박물관이 그렇게 하는 이유는 시선이 유물로 모이기 때문이야.
    ///
    /// 그림자는 기획서 §7.6 대로 <b>주요 조명 하나만</b> 드리운다 — 천창 빛만.
    /// 진열장 스포트 여덟 개까지 그림자를 켜면 약한 노트북에서 그림자 지도를 여덟 장 그려야 한다.
    /// 물건이 바닥에 붙어 보이는 건 SSAO(MuseumLook)가 훨씬 싸게 해준다.
    /// </summary>
    static void MakeGalleryLighting()
    {
        // 천장 너머로 스며드는 차가운 빛. 형태만 겨우 보이는 정도.
        var go = new GameObject("Skylight");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.74f, 0.79f, 0.90f);
        light.intensity = 0.85f;
        // 그림자 하나가 방을 통째로 살린다. 기둥과 진열장이 바닥에 자국을 남기면
        // 그 순간 "배치해 둔 상자" 가 아니라 "서 있는 물건" 으로 읽힌다.
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.62f;   // 박물관 천창은 확산광이라 그림자가 흐리다
        go.transform.rotation = Quaternion.Euler(72f, 18f, 0f);

        // 방이 보일 만큼은 밝게. 박물관은 어둑하지만 동굴은 아니다 —
        // 전시품이 도드라지는 건 방이 캄캄해서가 아니라 스포트가 더 밝아서야.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.40f, 0.43f, 0.52f);
        RenderSettings.ambientEquatorColor = new Color(0.30f, 0.31f, 0.36f);
        RenderSettings.ambientGroundColor  = new Color(0.18f, 0.17f, 0.18f);
        RenderSettings.fog = false;
    }

    /// <summary>진열장 바로 위에서 떨어지는 따뜻한 조명 한 점.</summary>
    static Light MakeCaseLight(Transform parent)
    {
        // 레일 조명 기구 — 빛만 허공에 떠 있으면 어색하다
        TestSceneBuilder.Cube(parent, "Fixture", new Vector3(0f, 4.35f, 0f),
                              new Vector3(0.22f, 0.3f, 0.22f), ColFixture, keepCollider: false,
                              finish: Finish.금속);

        // 전구 면. 발광 재질이라 블룸이 실제로 번진다 — 조명이 "켜져 있는" 것처럼 보이는 건 이것 때문이야
        TestSceneBuilder.Cube(parent, "Fixture_Lens", new Vector3(0f, 4.19f, 0f),
                              new Vector3(0.16f, 0.03f, 0.16f), ColLantern, keepCollider: false,
                              finish: Finish.발광);

        var go = new GameObject("CaseLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 4.15f, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // 똑바로 아래로

        var spot = go.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.87f, 0.68f);
        spot.intensity = 26f;
        spot.range = 7.5f;
        spot.spotAngle = 40f;
        spot.innerSpotAngle = 16f;
        spot.shadows = LightShadows.None;
        return spot;
    }

    // ==================================================================
    //  번호 스티커
    // ==================================================================
    /// <summary>
    /// 진열장 앞에 붙는 곰 얼굴 배지. 가운데에 1~8 번호가 찍혀 있다.
    /// 나중에 전시품을 넣을 때 "몇 번 진열장" 인지 바로 알아보라고 붙인 것.
    ///
    /// 숫자는 글꼴 대신 **막대 일곱 개(7세그먼트)** 로 그린다. URP 에서 옛날 TextMesh 는
    /// 셰이더가 없어서 자홍색으로 깨지는 일이 있는데, 도형으로 그리면 그 위험이 아예 없다.
    ///
    /// 네가 그린 진짜 2D 스티커로 바꾸려면 이 `NumberSticker` 오브젝트를 지우고
    /// 같은 자리에 이미지 평면을 넣으면 된다.
    /// </summary>
    static void MakeNumberSticker(Transform parent, int number)
    {
        var root = new GameObject("NumberSticker").transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(0f, 0.72f, 0.57f);   // 받침대 앞면

        // 귀는 얼굴 뒤로 살짝 물러나 있어야 삐져나온 것처럼 보인다
        FacingDisc(root, "Ear_L", new Vector3(-0.135f, 0.135f, -0.003f), 0.15f, ColBearFur);
        FacingDisc(root, "Ear_R", new Vector3( 0.135f, 0.135f, -0.003f), 0.15f, ColBearFur);
        FacingDisc(root, "Face",  Vector3.zero, 0.38f, ColBearFace);

        MakeSevenSegment(root, number, new Vector3(0f, 0.015f, 0.012f));

        // 본관 곰의 그 빨간 리본
        TestSceneBuilder.Cube(root, "Bow_L", new Vector3(-0.055f, -0.155f, 0.008f),
                              new Vector3(0.07f, 0.05f, 0.02f), ColRibbon, keepCollider: false);
        TestSceneBuilder.Cube(root, "Bow_R", new Vector3( 0.055f, -0.155f, 0.008f),
                              new Vector3(0.07f, 0.05f, 0.02f), ColRibbon, keepCollider: false);
        TestSceneBuilder.Cube(root, "Bow_Knot", new Vector3(0f, -0.155f, 0.012f),
                              new Vector3(0.035f, 0.035f, 0.02f), ColRibbon, keepCollider: false);
    }

    /// <summary>보는 쪽(+Z)을 향하는 납작한 원반. 원기둥을 눕혀서 만든다.</summary>
    static void FacingDisc(Transform parent, string name, Vector3 localPosition,
                           float diameter, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // 원기둥 축을 +Z 로
        go.transform.localScale = new Vector3(diameter, 0.008f, diameter);
        go.GetComponent<Renderer>().sharedMaterial = TestSceneBuilder.MaterialAsset(color);
    }

    /// <summary>
    /// 계산기 숫자처럼 막대 일곱 개로 1~8 을 그린다.
    ///   a 위 · b 오른위 · c 오른아래 · d 아래 · e 왼아래 · f 왼위 · g 가운데
    /// </summary>
    static void MakeSevenSegment(Transform parent, int digit, Vector3 origin)
    {
        bool[] on = digit switch
        {                //  a      b      c      d      e      f      g
            1 => new[] { false, true,  true,  false, false, false, false },
            2 => new[] { true,  true,  false, true,  true,  false, true  },
            3 => new[] { true,  true,  true,  true,  false, false, true  },
            4 => new[] { false, true,  true,  false, false, true,  true  },
            5 => new[] { true,  false, true,  true,  false, true,  true  },
            6 => new[] { true,  false, true,  true,  true,  true,  true  },
            7 => new[] { true,  true,  true,  false, false, false, false },
            _ => new[] { true,  true,  true,  true,  true,  true,  true  },   // 8
        };

        Vector3[] offsets =
        {
            new Vector3( 0f,     0.065f, 0f),   // a
            new Vector3( 0.043f, 0.033f, 0f),   // b
            new Vector3( 0.043f,-0.033f, 0f),   // c
            new Vector3( 0f,    -0.065f, 0f),   // d
            new Vector3(-0.043f,-0.033f, 0f),   // e
            new Vector3(-0.043f, 0.033f, 0f),   // f
            new Vector3( 0f,     0f,     0f),   // g
        };
        Vector3 horizontal = new Vector3(0.085f, 0.022f, 0.012f);
        Vector3 vertical   = new Vector3(0.022f, 0.070f, 0.012f);
        string[] names = { "a", "b", "c", "d", "e", "f", "g" };

        var root = new GameObject($"Digit_{digit}").transform;
        root.SetParent(parent, false);
        root.localPosition = origin;

        for (int i = 0; i < 7; i++)
        {
            if (!on[i]) continue;
            bool isHorizontal = i == 0 || i == 3 || i == 6;
            TestSceneBuilder.Cube(root, names[i], offsets[i],
                                  isHorizontal ? horizontal : vertical, ColNumber, keepCollider: false);
        }
    }

    // ==================================================================
    //  홀
    // ==================================================================
    static void MakeHall()
    {
        var root = new GameObject("Hall").transform;

        // 다듬은 석재 바닥. 살짝 윤이 나지만 거울은 아니다.
        // 여기서 광택(0.40 이상)을 쓰면 눈높이에서 바닥이 새까매진다 — 스침각에서는 반사가
        // 확산광을 눌러버리는데, 이 방은 천장이 어두워서 그 반사가 곧 검정이야. 실제로 그렇게 나왔다.
        var floor = TestSceneBuilder.Cube(root, "Floor", new Vector3(0f, -0.25f, 0f),
                                          new Vector3(HallWidth, 0.5f, HallDepth), ColFloor,
                                          finish: Finish.석재);
        floor.isStatic = true;

        MakeFloorTiles(root);

        // 가운데 통로를 나무로 깔아서 시선이 중앙으로 모이게
        TestSceneBuilder.Cube(root, "Runner", new Vector3(0f, 0.012f, 0f),
                              new Vector3(4.5f, 0.02f, HallDepth - 3f), ColFloorTrim, keepCollider: false,
                              finish: Finish.나무);

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

    /// <summary>
    /// 바닥 줄눈. 큰 판 하나는 크기를 가늠할 수 없어서 방이 작아 보이고 평평해 보인다.
    /// 석재 타일 간격이 보이면 눈이 거리를 재기 시작하고, 그 순간 방이 넓어진다.
    /// 텍스처 없이 살짝 어두운 띠만 깔았다 — FBX 바닥이 오면 통째로 지우면 된다.
    /// </summary>
    static void MakeFloorTiles(Transform parent)
    {
        var tiles = new GameObject("FloorSeams").transform;
        tiles.SetParent(parent, false);

        var seam = new Color(ColFloor.r * 0.82f, ColFloor.g * 0.82f, ColFloor.b * 0.84f);
        const float step = 3f;
        const float width = 0.06f;

        for (float x = -HallWidth * 0.5f + step; x < HallWidth * 0.5f; x += step)
            TestSceneBuilder.Cube(tiles, $"SeamX_{x:0}", new Vector3(x, 0.005f, 0f),
                                  new Vector3(width, 0.01f, HallDepth), seam, keepCollider: false,
                                  finish: Finish.석재).isStatic = true;

        for (float z = -HallDepth * 0.5f + step; z < HallDepth * 0.5f; z += step)
            TestSceneBuilder.Cube(tiles, $"SeamZ_{z:0}", new Vector3(0f, 0.005f, z),
                                  new Vector3(HallWidth, 0.01f, width), seam, keepCollider: false,
                                  finish: Finish.석재).isStatic = true;
    }

    /// <summary>
    /// 폭 넓은 바닥은 반사할 게 없으면 그냥 어두운 판이다. 반사 프로브가 방을 한 번 찍어 두면
    /// 광택 바닥에 기둥과 조명이 비친다. 실시간이지만 <b>시작할 때 한 번만</b> 굽는다.
    /// </summary>
    static void MakeReflectionProbe()
    {
        var go = new GameObject("ReflectionProbe");
        go.transform.position = new Vector3(0f, 2.4f, 0f);

        var probe = go.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.resolution = 128;
        probe.size = new Vector3(HallWidth + 2f, WallHeight + 2f, HallDepth + 2f);
        probe.center = new Vector3(0f, WallHeight * 0.5f - 2.4f, 0f);
        probe.intensity = 0.32f;
        probe.shadowDistance = 30f;
    }

    static void Wall(Transform parent, string name, Vector3 basePosition, Vector2 footprint)
    {
        var go = TestSceneBuilder.Cube(parent, name, basePosition + Vector3.up * (WallHeight * 0.5f),
                                       new Vector3(footprint.x, WallHeight, footprint.y), ColWallNavy);
        go.isStatic = true;

        bool alongX = footprint.x > footprint.y;
        Vector3 panel = alongX ? new Vector3(footprint.x - 1.5f, 2.1f, footprint.y + 0.12f)
                               : new Vector3(footprint.x + 0.12f, 2.1f, footprint.y - 1.5f);
        TestSceneBuilder.Cube(parent, name + "_Panel", basePosition + Vector3.up * 1.55f, panel,
                              ColWallPanel, keepCollider: false).isStatic = true;

        // 굽도리와 그림 레일. 벽이 바닥에서 그냥 솟은 판이면 방으로 안 읽힌다 —
        // 실제 방에는 바닥과 벽이 만나는 자리에 항상 뭔가가 한 겹 있고, 눈은 그걸 찾는다.
        Vector3 trim = alongX ? new Vector3(footprint.x + 0.04f, 1f, footprint.y + 0.18f)
                              : new Vector3(footprint.x + 0.18f, 1f, footprint.y + 0.04f);

        TestSceneBuilder.Cube(parent, name + "_Skirting",
                              basePosition + Vector3.up * 0.22f,
                              new Vector3(trim.x, 0.44f, trim.z), ColWoodDark,
                              keepCollider: false, finish: Finish.나무).isStatic = true;

        TestSceneBuilder.Cube(parent, name + "_Rail",
                              basePosition + Vector3.up * 2.78f,
                              new Vector3(trim.x, 0.13f, trim.z), ColWoodDark,
                              keepCollider: false, finish: Finish.나무).isStatic = true;

        // 벽 위 청록 기와 띠
        Vector3 cap = alongX ? new Vector3(footprint.x + 1.2f, 0.25f, footprint.y + 1.2f)
                             : new Vector3(footprint.x + 1.2f, 0.25f, footprint.y + 1.2f);
        TestSceneBuilder.Cube(parent, name + "_Cap", basePosition + Vector3.up * (WallHeight + 0.12f),
                              cap, ColRoofTeal, keepCollider: false).isStatic = true;
    }

    static void Column(Transform parent, string name, Vector3 position)
    {
        TestSceneBuilder.Cube(parent, name, position + Vector3.up * (WallHeight * 0.5f),
                              new Vector3(0.5f, WallHeight, 0.5f), ColWoodDark, keepCollider: false,
                              finish: Finish.나무).isStatic = true;
        TestSceneBuilder.Cube(parent, name + "_Base", position + Vector3.up * 0.16f,
                              new Vector3(0.8f, 0.32f, 0.8f), ColStone, keepCollider: false,
                              finish: Finish.석재).isStatic = true;

        // 기둥머리 한 겹 — 각진 막대가 천장에 그냥 꽂히면 가짜로 보인다
        TestSceneBuilder.Cube(parent, name + "_Capital", position + Vector3.up * (WallHeight - 0.62f),
                              new Vector3(0.72f, 0.2f, 0.72f), ColWoodDark, keepCollider: false,
                              finish: Finish.나무).isStatic = true;
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
                                  new Vector3(0.32f, 1.8f, 0.32f), ColStone, keepCollider: false,
                                  finish: Finish.석재);
            // 등갓 — 발광 재질이라 불이 실제로 들어온 것처럼 번진다
            TestSceneBuilder.Cube(go, "Housing", new Vector3(0f, 2.1f, 0f),
                                  new Vector3(0.7f, 0.6f, 0.7f), ColLantern, keepCollider: false,
                                  finish: Finish.발광);
            TestSceneBuilder.Cube(go, "Cap", new Vector3(0f, 2.5f, 0f),
                                  new Vector3(1.05f, 0.2f, 1.05f), ColRoofTeal, keepCollider: false,
                                  finish: Finish.석재);

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(go, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.intensity = 1.8f;
            light.range = 7f;
            light.shadows = LightShadows.None;
        }
    }

    // ==================================================================
    //  진열장
    // ==================================================================
    static GalleryCase[] MakeCases()
    {
        var root = new GameObject("DisplayCases").transform;
        var result = new GalleryCase[ExhibitCatalogue.Count];

        for (int i = 0; i < ExhibitCatalogue.Count; i++)
        {
            var item = ExhibitCatalogue.All[i];
            float angle = ((float)i / ExhibitCatalogue.Count) * Mathf.PI * 2f + Mathf.PI * 0.5f;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * CaseRadius;

            var go = new GameObject($"Case_{i + 1}_{item.id}");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-pos.normalized, Vector3.up));

            // 마우스 광선 판정 — 진열장 전체를 덮는다
            var pick = go.AddComponent<BoxCollider>();
            pick.size = new Vector3(1.5f, 2.6f, 1.5f);
            pick.center = new Vector3(0f, 1.3f, 0f);
            pick.isTrigger = true;

            var caseLight = MakeCaseLight(go.transform);
            MakeNumberSticker(go.transform, i + 1);

            TestSceneBuilder.Cube(go.transform, "Base", new Vector3(0f, 0.5f, 0f),
                                  new Vector3(1.1f, 1f, 1.1f), ColWoodDark, keepCollider: false,
                                  finish: Finish.나무);
            TestSceneBuilder.Cube(go.transform, "Base_Cap", new Vector3(0f, 1.02f, 0f),
                                  new Vector3(1.2f, 0.06f, 1.2f), ColStone, keepCollider: false,
                                  finish: Finish.석재);

            // 진짜 유리. 예전엔 투명 설정이 까다로워서 모서리 기둥 넷으로 대신했는데,
            // 진열장은 안이 비쳐야 진열장이다 — MaterialAsset 이 URP 투명 설정을 챙긴다.
            for (int side = 0; side < 4; side++)
            {
                float a = side * 90f * Mathf.Deg2Rad;
                var pane = TestSceneBuilder.Cube(go.transform, $"Glass_{side}",
                                                 new Vector3(Mathf.Sin(a) * 0.5f, 1.62f, Mathf.Cos(a) * 0.5f),
                                                 new Vector3(1.0f, 1.24f, 0.02f), ColCaseGlass,
                                                 keepCollider: false, finish: Finish.유리);
                pane.transform.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
            }

            // 유리를 잡아주는 금속 모서리 — 유리만 있으면 어디까지가 진열장인지 안 보인다
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    TestSceneBuilder.Cube(go.transform, $"Post_{sx}_{sz}",
                                          new Vector3(sx * 0.5f, 1.62f, sz * 0.5f),
                                          new Vector3(0.05f, 1.26f, 0.05f), ColFixture,
                                          keepCollider: false, finish: Finish.금속);

            TestSceneBuilder.Cube(go.transform, "CaseTop", new Vector3(0f, 2.28f, 0f),
                                  new Vector3(1.1f, 0.08f, 1.1f), ColFixture, keepCollider: false,
                                  finish: Finish.금속);

            // 명판 — 마우스를 올리면 색이 바뀌는 부분
            var plaque = TestSceneBuilder.Cube(go.transform, "Plaque", new Vector3(0f, 0.28f, 0.58f),
                                               new Vector3(0.8f, 0.22f, 0.06f), ColWallPanel,
                                               keepCollider: false, finish: Finish.금속);

            var anchor = new GameObject("ItemAnchor").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, 1.15f, 0f);

            var placeholder = MakePlaceholder(anchor, item.shape);

            var display = go.AddComponent<GalleryCase>();
            display.itemId = item.id;
            display.displayName = item.name;
            display.chapter = item.chapter;
            display.description = item.description;
            display.itemAnchor = anchor;
            display.placeholder = placeholder;
            display.itemRenderer = placeholder.GetComponent<Renderer>();
            display.plaqueRenderer = plaque.GetComponent<Renderer>();
            display.caseLight = caseLight;

            result[i] = display;
        }
        return result;
    }

    /// <summary>전시품 임시 모양. 물건 종류가 눈으로 구분되게 셋으로 나눴다.</summary>
    static GameObject MakePlaceholder(Transform parent, ExhibitCatalogue.Shape shape)
    {
        switch (shape)
        {
            case ExhibitCatalogue.Shape.원반:
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Placeholder";
                Object.DestroyImmediate(disc.GetComponent<Collider>());
                disc.transform.SetParent(parent, false);
                disc.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                disc.transform.localScale = new Vector3(0.42f, 0.03f, 0.42f);
                disc.GetComponent<Renderer>().sharedMaterial = TestSceneBuilder.MaterialAsset(Color.grey);
                return disc;

            case ExhibitCatalogue.Shape.종이:
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
