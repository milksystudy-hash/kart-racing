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
    static readonly Color ColDustCloth = new Color32(0xDE, 0xD7, 0xC6, 0xFF);
    static readonly Color ColDustFold  = new Color32(0xC3, 0xBA, 0xA6, 0xFF);
    static readonly Color ColNotice    = new Color32(0xC2, 0x3B, 0x2E, 0xFF);
    static readonly Color ColNoticeInk = new Color32(0xF3, 0xEC, 0xDE, 0xFF);
    static readonly Color ColTapeWarn  = new Color32(0xD8, 0xB0, 0x2A, 0xFF);
    static readonly Color ColCarpet    = new Color32(0x9B, 0x2F, 0x2A, 0xFF);
    static readonly Color ColBannerBg  = new Color32(0xF1, 0xE8, 0xD3, 0xFF);
    static readonly Color ColLeaf      = new Color32(0x5B, 0x7A, 0x4E, 0xFF);

    // 전시품 목록은 ExhibitCatalogue 하나로 모았다.
    // 트랙에 놓는 수집품도 같은 목록을 읽어서, 주운 물건과 진열장이 어긋날 수가 없다.

    [MenuItem("Racing/전시실 씬 만들기", false, 3)]
    public static void BuildGallery()
    {
        Directory.CreateDirectory(SceneFolder);

        // ★ 유저가 손으로 놓은 것은 다시 세워 준다 — 캠퍼스와 같은 방식(2026-09-23).
        //   빌더가 만들지 않은 루트는 건드리지 않는다.
        var mine = MyProps.Collect(GalleryPath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeGalleryLighting();
        MakeHall();
        MakeDoor();
        MakeLanterns();
        MakeDressing();

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

        // 다 모으기 전 / 다 모은 뒤 — 방 자체가 달라진다
        var mood = rig.AddComponent<GalleryMood>();
        mood.beforeThings = MakeClosedNotice();
        mood.afterThings = MakeReopenedDressing();

        MakeReflectionProbe();
        MuseumLook.RefineMaterials();   // 손으로 다듬을 필요 없이 구워 나올 때부터 마감이 붙어 있게
        MuseumLook.ApplyToOpenScene();   // 후처리 · 안티에일리어싱 — 이게 없으면 다 회색 상자로 보인다

        MyProps.Restore(mine);
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
        var rig = new GameObject("Lighting");
        var lights = rig.AddComponent<GalleryLights>();

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

        // 전시품을 다 모으면 켜지는 천장 등. 평소엔 세기 0 이라 있는 줄도 모른다.
        var ceiling = new GameObject("CeilingLights").transform;
        var lamps = new System.Collections.Generic.List<Light>();
        for (int ix = -1; ix <= 1; ix++)
            for (int iz = -1; iz <= 1; iz++)
            {
                var lampGo = new GameObject("Ceiling_" + (ix + 1) + "_" + (iz + 1));
                lampGo.transform.SetParent(ceiling, false);
                lampGo.transform.position = new Vector3(ix * 9.5f, WallHeight - 1.1f, iz * 8f);

                var lamp = lampGo.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.color = new Color(1f, 0.93f, 0.80f);   // 백열등 — 천창의 찬 빛과 대비된다
                lamp.range = 22f;   // 아홉 개가 겹쳐서 방 전체를 고르게 채운다
                lamp.intensity = 0f;
                lamp.shadows = LightShadows.None;   // 그림자는 천창 하나만 (기획서 §7.6)
                lamps.Add(lamp);
            }

        lights.skylight = light;
        lights.ceilingLights = lamps.ToArray();
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
    /// <b>개관 기념 단체 사진.</b> 전시실 뒷벽에 거는 큰 액자 한 장.
    ///
    /// ★★ 복선은 「곰이 많다」가 아니다 — 곰은 지금도 급식실에도 있고 코스에도 서 있다.
    /// 복선은 <b>곰과 사람이 나란히 찍혀 있다</b>는 것이다:
    /// <list type="number">
    /// <item>지금 이 박물관에는 <b>사람이 한 명도 없다.</b> 제1장 제목이 「사라진 관람객」이야</item>
    /// <item>그리고 <b>곰은 사람 앞에서 움직이면 안 된다</b> — 경기에 무인 카트를 내보내는
    ///       이유가 그거다. 그런데 사진에서는 <b>같이 서서 찍었다.</b> 예전엔 그래도 됐다는 뜻</item>
    /// </list>
    /// 설명은 한 줄도 안 한다. 플레이어가 이미 배운 규칙 둘이 사진 한 장과 안 맞을 뿐이야.
    ///
    /// 글자는 안 쓴다 — 실루엣만으로 사람과 곰이 갈려야 한다(사람은 키가 크고 귀가 없다).
    /// </summary>
    static void MakeOpeningPhoto(Transform parent, Vector3 at)
    {
        const float fw = 6.0f, fh = 2.4f;

        var root = new GameObject("OpeningPhoto").transform;
        root.SetParent(parent, false);
        root.localPosition = at;

        // 층은 <b>뒤에서 앞으로</b> 쌓는다. 액자판은 통짜 상자라 그림을 뒤에 두면 안 보인다
        // (바로 위 Frame_0~5 가 그 실수를 하고 있었다 — 같이 고쳤다).
        TestSceneBuilder.Cube(root, "Frame", Vector3.zero,
                              new Vector3(fw + 0.20f, fh + 0.20f, 0.10f), ColWoodDark,
                              keepCollider: false, finish: Finish.나무);
        // ★ 2026-10-06 처음에 <b>ColNotice / ColNoticeInk 를 거꾸로</b> 썼다 —
        //   그 둘은 「철거 통지서」의 <b>빨강 바탕과 그 위의 크림 글씨</b>지 종이와 잉크가 아니다.
        //   그래서 액자에 빨간 테가 둘리고 사람들이 <b>허옇게</b> 떴다. 색은 이름이 아니라
        //   <b>값을 보고</b> 골라야 한다(ColNotice = #C23B2E).
        TestSceneBuilder.Cube(root, "Mat", new Vector3(0f, 0f, 0.055f),
                              new Vector3(fw, fh, 0.03f), ColDustCloth,
                              keepCollider: false, finish: Finish.무광);
        TestSceneBuilder.Cube(root, "Paper", new Vector3(0f, 0.05f, 0.075f),
                              new Vector3(fw - 0.34f, fh - 0.50f, 0.02f), ColBearFace,
                              keepCollider: false, finish: Finish.무광);

        // 줄마다 조금씩 앞으로 — 뒷줄이 앞줄에 가려야 «줄 서서 찍은 사진» 이 된다
        for (int row = 0; row < 3; row++)
        {
            int count = row == 0 ? 11 : 10;
            float y = 0.50f - row * 0.44f;
            float z = 0.090f + row * 0.006f;
            float span = fw - 1.0f;

            for (int i = 0; i < count; i++)
            {
                float x = -span * 0.5f + span * i / (count - 1f);
                string tag = $"{row}_{i}";

                // 서른한 중 일곱이 사람이다. 한 덩어리로 몰리지 않게 줄마다 어긋나게 섞는다
                if ((row * 7 + i * 3) % 5 == 0)
                {
                    // 사람 — <b>머리 하나가 더 크다.</b> 귀가 없는 것만으로는 안 갈린다
                    FacingDisc(root, $"P_{tag}_Head", new Vector3(x, y + 0.145f, z), 0.17f, ColWoodDark);
                    TestSceneBuilder.Cube(root, $"P_{tag}_Body", new Vector3(x, y - 0.075f, z - 0.002f),
                                          new Vector3(0.26f, 0.34f, 0.016f), ColWoodDark,
                                          keepCollider: false, finish: Finish.무광);
                }
                else
                {
                    // 곰 — 낮고 둥글다. ★ 귀는 <b>머리 밖으로 확실히 나와야</b> 곰으로 읽힌다
                    //   (처음엔 ±0.085 라 2.5cm 밖에 안 나와서 그냥 동그란 머리였다)
                    FacingDisc(root, $"B_{tag}_Ear_L", new Vector3(x - 0.105f, y + 0.095f, z - 0.004f),
                               0.110f, ColWoodDark);
                    FacingDisc(root, $"B_{tag}_Ear_R", new Vector3(x + 0.105f, y + 0.095f, z - 0.004f),
                               0.110f, ColWoodDark);
                    FacingDisc(root, $"B_{tag}_Head", new Vector3(x, y, z), 0.215f, ColWoodDark);
                    TestSceneBuilder.Cube(root, $"B_{tag}_Body", new Vector3(x, y - 0.165f, z - 0.002f),
                                          new Vector3(0.23f, 0.20f, 0.016f), ColWoodDark,
                                          keepCollider: false, finish: Finish.무광);
                }
            }
        }

        // 아래 여백의 손글씨 — 글자는 안 쓴다. <b>적혀 있다는 것</b>만 보이면 된다
        for (int i = 0; i < 2; i++)
            TestSceneBuilder.Cube(root, $"Caption_{i}",
                                  new Vector3(-0.7f + i * 1.5f, -0.80f, 0.095f),
                                  new Vector3(i == 0 ? 1.2f : 0.8f, 0.035f, 0.014f), ColWoodDark,
                                  keepCollider: false, finish: Finish.무광);

        // 액자 조명 — 다른 액자와 같은 갓. 이 사진은 <b>불이 들어와 있어야</b> 눈이 간다
        var lamp = TestSceneBuilder.Cube(root, "PhotoLamp",
                                         new Vector3(0f, fh * 0.5f + 0.26f, 0.18f),
                                         new Vector3(1.8f, 0.08f, 0.22f), ColFixture,
                                         keepCollider: false, finish: Finish.금속);
        lamp.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);

        // ★ 갓 안쪽 발광 띠. 이 사진은 <b>1판부터 읽혀야 하는 복선</b>이라
        //   방이 어두운 동안에도 여기만 밝아야 한다. ColLantern 은 색이 곧 발광이라
        //   실시간 조명을 하나도 안 쓴다(§7.6).
        TestSceneBuilder.Cube(root, "PhotoBulb", new Vector3(0f, fh * 0.5f + 0.20f, 0.21f),
                              new Vector3(1.7f, 0.05f, 0.09f), ColLantern,
                              keepCollider: false, finish: Finish.발광);
    }

    /// <summary>
    /// <b>아홉 번째 진열장.</b> 번호는 9 인데 <b>이름표가 비어 있고, 영영 안 채워진다.</b>
    ///
    /// ★★ 수집품은 여덟 개다. 그런데 칸이 아홉이면 플레이어는 <b>센다.</b>
    /// 「내가 하나 못 찾았나」로 안 읽히게 신호를 셋 겹쳐 뒀다:
    /// <list type="bullet">
    /// <item><b>덮개가 바닥에 벗겨져 떨어져 있다</b> — 나머지 일곱은 쓰고 있는데.
    ///       «아직 안 열었다» 가 아니라 <b>«누가 이미 열었다»</b></item>
    /// <item><b>조명이 없다.</b> 8/8 에 방 전체가 켜져도 이 칸만 안 켜진다</item>
    /// <item><b>명판에 글자가 없다.</b> 여덟 칸은 다 이름과 장이 적혀 있다</item>
    /// </list>
    /// 마우스로 집히지도 않는다(콜라이더 없음) — 전시 목록에 없는 칸이야.
    /// 바로 위 개관 사진 아래에 세운다. 둘을 잇는 건 플레이어 몫이다.
    /// </summary>
    static void MakeNinthCase(Transform parent, Vector3 at)
    {
        var go = new GameObject("Case_9_empty");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;          // 뒷벽 쪽이라 그대로 방 한가운데를 본다
        var t = go.transform;

        MakeNumberSticker(t, 9);

        TestSceneBuilder.Cube(t, "Base", new Vector3(0f, 0.5f, 0f),
                              new Vector3(1.1f, 1f, 1.1f), ColWoodDark,
                              keepCollider: false, finish: Finish.나무);
        TestSceneBuilder.Cube(t, "Base_Cap", new Vector3(0f, 1.02f, 0f),
                              new Vector3(1.2f, 0.06f, 1.2f), ColStone,
                              keepCollider: false, finish: Finish.석재);

        for (int side = 0; side < 4; side++)
        {
            float a = side * 90f * Mathf.Deg2Rad;
            var pane = TestSceneBuilder.Cube(t, $"Glass_{side}",
                                             new Vector3(Mathf.Sin(a) * 0.5f, 1.62f, Mathf.Cos(a) * 0.5f),
                                             new Vector3(1.0f, 1.24f, 0.02f), ColCaseGlass,
                                             keepCollider: false, finish: Finish.유리);
            pane.transform.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
        }

        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                TestSceneBuilder.Cube(t, $"Post_{sx}_{sz}", new Vector3(sx * 0.5f, 1.62f, sz * 0.5f),
                                      new Vector3(0.05f, 1.26f, 0.05f), ColFixture,
                                      keepCollider: false, finish: Finish.금속);

        TestSceneBuilder.Cube(t, "CaseTop", new Vector3(0f, 2.28f, 0f),
                              new Vector3(1.1f, 0.08f, 1.1f), ColFixture,
                              keepCollider: false, finish: Finish.금속);

        TestSceneBuilder.Cube(t, "Plaque", new Vector3(0f, 0.28f, 0.58f),
                              new Vector3(0.8f, 0.22f, 0.06f), ColWallPanel,
                              keepCollider: false, finish: Finish.금속);

        // 벗겨져 바닥에 떨어진 덮개. 조금 비뚤어야 «벗겨진 것» 으로 보인다
        var cloth = TestSceneBuilder.Cube(t, "DustCover_Fallen", new Vector3(0.95f, 0.07f, 0.30f),
                                          new Vector3(1.20f, 0.14f, 0.95f), ColDustCloth,
                                          keepCollider: false, finish: Finish.무광);
        cloth.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
        var fold = TestSceneBuilder.Cube(t, "DustCover_Fold", new Vector3(0.78f, 0.16f, 0.52f),
                                         new Vector3(0.72f, 0.12f, 0.52f), ColDustFold,
                                         keepCollider: false, finish: Finish.무광);
        fold.transform.localRotation = Quaternion.Euler(0f, -14f, 0f);
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
            9 => new[] { true,  true,  true,  true,  false, true,  true  },
            _ => new[] { true,  true,  true,  true,  true,  true,  true  },   // 8
        };

        // ★ x 부호가 <b>음수가 오른쪽</b>이다. 진열장은 방 가운데를 보게 180도 돌아 있어서,
        // 물체의 로컬 +X 가 보는 사람 <b>왼쪽</b>에 온다. 그대로 그리면 숫자가 통째로
        // 거울상이 된다 — 유저가 "4가 뒤집혔다" 고 한 게 이거야(2026-09-17).
        // 현판 글씨를 180도 돌려 단 것과 같은 병이고, 여기는 도형이라 각도가 아니라 부호로 푼다.
        Vector3[] offsets =
        {
            new Vector3( 0f,     0.065f, 0f),   // a 위
            new Vector3(-0.043f, 0.033f, 0f),   // b 오른위
            new Vector3(-0.043f,-0.033f, 0f),   // c 오른아래
            new Vector3( 0f,    -0.065f, 0f),   // d 아래
            new Vector3( 0.043f,-0.033f, 0f),   // e 왼아래
            new Vector3( 0.043f, 0.033f, 0f),   // f 왼위
            new Vector3( 0f,     0f,     0f),   // g 가운데
        };
        Vector3 horizontal = new Vector3(0.085f, 0.022f, 0.012f);
        Vector3 vertical   = new Vector3(0.022f, 0.070f, 0.012f);
        string[] names = { "a", "b", "c", "d", "e", "f", "g" };

        var root = new GameObject($"Digit_{digit}").transform;
        root.SetParent(parent, false);

        // ★★ 2026-10-02 유저: *"3번은 글자가 가운데인데 1번은 너무 오른쪽으로 간다."*
        //   <b>7세그먼트 「1」은 오른쪽 막대 둘(b·c)만 켜진다.</b> 다른 숫자는 가로 막대가
        //   칸 전체를 채워서 가운데로 보이는데, 1 은 글자가 칸의 한쪽 끝에만 있다.
        //   계산기에서는 자릿수가 맞아야 하니 그게 맞지만, <b>여기는 한 자리짜리 스티커</b>라
        //   칸이 아니라 <b>글자</b>가 가운데여야 한다. b·c 가 x −0.043 이니 그만큼 되민다.
        root.localPosition = origin + (digit == 1 ? new Vector3(0.043f, 0f, 0f) : Vector3.zero);

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
    /// <summary>
    /// 로비로 돌아가는 문 하나. 유저: *"다들 문이 없는데 어떻게 들어가고 나간 거야."*
    /// 전시실은 <b>들어왔다가 나가는 방</b>인데 출입구가 없으면 방이 아니라 상자로 보인다.
    /// 남쪽 벽 한가운데 — 전시 케이스는 반지름 8.2m 원형으로 놓여 있어서 벽은 비어 있다.
    /// </summary>
    static void MakeDoor()
    {
        var root = new GameObject("Doors").transform;
        var door = HanokDoor.Build(root, new Vector3(0f, 0f, HallDepth * 0.5f - 0.25f),
                                   Quaternion.Euler(0f, 180f, 0f), 3.8f, 4.4f,
                                   c => TestSceneBuilder.MaterialAsset(c, FlatMaterial.FinishFor(c)),
                                   plaque: true, buildingName: "중앙홀", department: "돌아가기");

        // 2026-09-17 유저: 전시실에서 로비로 돌아갈 길이 없었다(F1 은 개발용이라 빌드에서 꺼진다).
        // 전시실은 <b>걸어다니는 씬이 아니라</b> 궤도 카메라라 "다가가서 E" 가 안 된다 —
        // 그래서 거리를 넉넉히 주고 카메라를 기준으로 본다.
        var back = door.AddComponent<SceneDoor>();
        back.sceneIndex = 0;          // 로비
        back.label = "중앙홀로";
        back.range = 40f;             // 방이 30×26 이라 어디서든 닿는다
    }

    /// <summary>
    /// 박물관처럼 보이게 하는 물건들. 유저: *"사각형 구조물이 많고 단색이라 레고 냄새가 난다."*
    ///
    /// 레고처럼 보이는 이유는 상자가 많아서가 아니라 <b>크기가 다 비슷해서</b>다.
    /// 30cm 보다 작은 게 하나도 없으면 눈이 크기를 잴 기준을 못 찾고, 그러면 방 전체가
    /// 장난감으로 보인다. 그래서 여기 넣는 건 대부분 <b>작은 것</b>이야 —
    /// 라벨, 콘센트, 환기구, 소화기. 실제 박물관에는 다 있고, 없으면 그게 더 이상하다.
    ///
    /// 큰 것 셋(로프·벤치·액자)은 <b>사람 크기</b>를 알려준다. 벤치 옆에 서 보면
    /// 방이 얼마나 큰지 바로 안다 — 그게 상자 백 개보다 낫다.
    /// </summary>
    static void MakeDressing()
    {
        var root = new GameObject("Dressing").transform;
        float halfW = HallWidth * 0.5f, halfD = HallDepth * 0.5f;

        // ---- 벨벳 로프 : 전시 케이스 앞을 두른다. 이것 하나로 "전시실" 이 된다 ----
        const int posts = 12;
        const float ropeR = 6.1f;
        for (int i = 0; i < posts; i++)
        {
            float a = i / (float)posts * Mathf.PI * 2f;
            var at = new Vector3(Mathf.Cos(a) * ropeR, 0f, Mathf.Sin(a) * ropeR);

            TestSceneBuilder.Cube(root, $"PostBase_{i}", at + Vector3.up * 0.03f,
                                  new Vector3(0.34f, 0.06f, 0.34f), ColFixture,
                                  keepCollider: false, finish: Finish.금속).isStatic = true;
            TestSceneBuilder.Cube(root, $"Post_{i}", at + Vector3.up * 0.48f,
                                  new Vector3(0.07f, 0.9f, 0.07f), ColLantern,
                                  keepCollider: false, finish: Finish.금속).isStatic = true;

            // 기둥 사이를 잇는 줄. 가운데가 처지게 두 토막으로 꺾는다 — 곧은 막대는 로프로 안 보인다.
            float b = (i + 1) / (float)posts * Mathf.PI * 2f;
            var next = new Vector3(Mathf.Cos(b) * ropeR, 0f, Mathf.Sin(b) * ropeR);
            var mid = (at + next) * 0.5f;
            float span = Vector3.Distance(at, next) * 0.52f;
            var face = Quaternion.LookRotation(next - at, Vector3.up);

            for (int h = 0; h < 2; h++)
            {
                Vector3 from = h == 0 ? at : mid;
                Vector3 to = h == 0 ? mid : next;
                var seg = TestSceneBuilder.Cube(root, $"Rope_{i}_{h}", (from + to) * 0.5f + Vector3.up * (h == 0 ? 0.78f : 0.78f),
                                                new Vector3(0.05f, 0.05f, span), ColRibbon, keepCollider: false);
                seg.transform.rotation = Quaternion.LookRotation(to - from, Vector3.up)
                                       * Quaternion.Euler(h == 0 ? 7f : -7f, 0f, 0f);
                seg.isStatic = true;
            }
        }

        // ---- 관람 벤치 : 사람 크기를 알려주는 제일 싼 물건 ----
        for (int i = -1; i <= 1; i += 2)
        {
            var at = new Vector3(i * 3.1f, 0f, 0f);
            TestSceneBuilder.Cube(root, $"BenchTop_{i}", at + Vector3.up * 0.43f,
                                  new Vector3(0.62f, 0.09f, 2.6f), ColWoodDark,
                                  keepCollider: false, finish: Finish.나무).isStatic = true;
            for (int e = -1; e <= 1; e += 2)
                TestSceneBuilder.Cube(root, $"BenchLeg_{i}_{e}", at + new Vector3(0f, 0.2f, e * 1.05f),
                                      new Vector3(0.5f, 0.4f, 0.12f), ColFixture,
                                      keepCollider: false, finish: Finish.금속).isStatic = true;
        }

        // ---- 벽 액자 : 빈 벽이 제일 큰 문제다 ----
        var frames = new (float x, float z, float yaw)[]
        {
            (-halfW + 0.35f,  6f, 90f), (-halfW + 0.35f, -6f, 90f),
            ( halfW - 0.35f,  6f, 270f), ( halfW - 0.35f, -6f, 270f),
            (-7f, -halfD + 0.35f, 0f), ( 7f, -halfD + 0.35f, 0f),
        };
        for (int i = 0; i < frames.Length; i++)
        {
            var (x, z, yaw) = frames[i];
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var at = new Vector3(x, 2.55f, z);
            bool tall = i % 2 == 0;
            float fw = tall ? 1.1f : 1.6f, fh = tall ? 1.5f : 1.05f;

            Place(root, $"Frame_{i}", at, rot, new Vector3(fw + 0.16f, fh + 0.16f, 0.09f),
                  ColWoodDark, Finish.나무);
            // ★ 2026-10-06 <b>부호가 반대였다.</b> 액자판은 속이 빈 테두리가 아니라
            //   <b>두께 0.09 의 통짜 상자</b>인데, 그림을 로컬 −Z(벽 쪽)에 뒀으니
            //   <b>여섯 장이 전부 판 뒤에 숨어</b> 있었다. 벽에 나무 판때기 여섯 장만 걸려 있던 거야.
            Place(root, $"Mat_{i}", at + rot * new Vector3(0f, 0f, 0.05f),
                  rot, new Vector3(fw, fh, 0.03f), ColCaseGlass, Finish.무광);
            Place(root, $"Art_{i}", at + rot * new Vector3(0f, 0f, 0.07f),
                  rot, new Vector3(fw - 0.24f, fh - 0.24f, 0.02f),
                  i % 3 == 0 ? ColRibbon : (i % 3 == 1 ? ColRoofTeal : ColBearFur), Finish.무광);

            // 액자 조명 — 그림 위에 얹은 작은 갓
            Place(root, $"ArtLamp_{i}", at + rot * new Vector3(0f, fh * 0.5f + 0.24f, 0.16f),
                  rot * Quaternion.Euler(28f, 0f, 0f), new Vector3(fw * 0.5f, 0.07f, 0.2f),
                  ColFixture, Finish.금속);
        }

        // ★★ 2026-10-06 ── 복선 둘 ──────────────────────────────────────
        // 진열장 키가 2.3m 라 사진을 낮게 걸면 <b>덮개 쓴 칸에 아랫줄이 가린다</b>(렌더로 잡았다)
        MakeOpeningPhoto(root, new Vector3(0f, 3.25f, -halfD + 0.35f));
        MakeNinthCase(root, new Vector3(-3.2f, 0f, -halfD + 1.8f));

        // ---- 안내 배너 : 문 옆에 세운 입간판 ----
        var bannerAt = new Vector3(-3.4f, 0f, halfD - 2.2f);
        Place(root, "BannerPost", bannerAt + Vector3.up * 1.1f, Quaternion.identity,
              new Vector3(0.08f, 2.2f, 0.08f), ColFixture, Finish.금속);
        Place(root, "BannerFoot", bannerAt + Vector3.up * 0.03f, Quaternion.identity,
              new Vector3(0.5f, 0.06f, 0.5f), ColFixture, Finish.금속);
        Place(root, "Banner", bannerAt + new Vector3(0f, 1.55f, -0.05f), Quaternion.identity,
              new Vector3(0.9f, 1.3f, 0.04f), ColCaseGlass, Finish.무광);

        // ---- 작은 것들 : 레고 냄새를 빼는 건 사실 이쪽이다 ----
        // 크기 기준이 30cm 짜리 하나뿐이면 방이 장난감으로 보인다. 10cm 짜리가 있어야 한다.
        Place(root, "Extinguisher", new Vector3(halfW - 0.5f, 0.32f, halfD - 1.4f),
              Quaternion.identity, new Vector3(0.16f, 0.5f, 0.16f), ColRibbon, Finish.광택);
        Place(root, "ExtinguisherSign", new Vector3(halfW - 0.36f, 1.35f, halfD - 1.4f),
              Quaternion.Euler(0f, 270f, 0f), new Vector3(0.22f, 0.3f, 0.02f), ColRibbon, Finish.무광);

        for (int i = -1; i <= 1; i += 2)
        {
            Place(root, $"Socket_{i}", new Vector3(i * (halfW - 0.32f), 0.32f, 2.5f),
                  Quaternion.Euler(0f, i > 0 ? 270f : 90f, 0f),
                  new Vector3(0.14f, 0.1f, 0.02f), ColCaseGlass, Finish.무광);

            Place(root, $"Vent_{i}", new Vector3(i * (halfW - 0.3f), WallHeight - 0.9f, -4f),
                  Quaternion.Euler(0f, i > 0 ? 270f : 90f, 0f),
                  new Vector3(0.7f, 0.4f, 0.04f), ColFixture, Finish.금속);
        }

        // 바닥 관람 동선 표시 — 얇은 띠 두 줄. 눈이 어디로 걸어야 하는지 알려준다.
        for (int i = -1; i <= 1; i += 2)
            Place(root, $"Guide_{i}", new Vector3(i * 2.3f, 0.016f, 0f), Quaternion.identity,
                  new Vector3(0.06f, 0.01f, HallDepth - 6f), ColFloorTrim, Finish.무광);
    }

    /// <summary>회전이 필요한 조각 하나. Cube 는 회전을 안 받아서 한 번 더 감싼다.</summary>
    static GameObject Place(Transform parent, string name, Vector3 at, Quaternion rot,
                            Vector3 size, Color color, Finish finish)
    {
        var go = TestSceneBuilder.Cube(parent, name, at, size, color, keepCollider: false, finish: finish);
        go.transform.rotation = rot;
        go.isStatic = true;
        return go;
    }

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

        // 기둥과 지붕. 천장은 2026-09-16 에 덮었다 — 천창 한 칸만 남겨서
        // 이 방의 빛이 어디서 오는지 눈에 보이게 했다.
        var frame = new GameObject("Frame").transform;
        frame.SetParent(root, false);

        HanokRoof.Build(frame, Vector3.zero, HallWidth, HallDepth, WallHeight,
                        c => TestSceneBuilder.MaterialAsset(c, FlatMaterial.FinishFor(c)),
                        skylightSize: 6f);
        for (int i = -2; i <= 2; i++)
        {
            float x = i * (halfW - 1f) / 2.2f;
            Column(frame, $"Col_N{i + 2}", new Vector3(x, 0f, -halfD + 0.8f));
            Column(frame, $"Col_S{i + 2}", new Vector3(x, 0f,  halfD - 0.8f));
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
    /// <summary>
    /// 유저가 만든 전시품. <b>한 줄 추가하면 그 칸에 들어간다</b> —
    /// <see cref="TestSceneBuilder.KartModels"/> 와 같은 방식이다.
    /// 파일이 없으면 임시 도형이 그대로 서니, 여덟 개를 한꺼번에 기다릴 필요가 없다.
    /// </summary>
    static readonly System.Collections.Generic.Dictionary<string, string> ExhibitModels = new()
    {
        { "coin",      "Assets/My blender/Exhibits/Exhibit_1_coin.fbx" },
        { "ledger",    "Assets/My blender/Exhibits/Exhibit_2_ledger.fbx" },
        { "survey",    "Assets/My blender/Exhibits/Exhibit_3_survey.fbx" },
        { "marker",    "Assets/My blender/Exhibits/Exhibit_4_marker.fbx" },
        { "signature", "Assets/My blender/Exhibits/Exhibit_5_signature.fbx" },
        { "contract",  "Assets/My blender/Exhibits/Exhibit_6_contract.fbx" },
        { "recorder",  "Assets/My blender/Exhibits/Exhibit_7_recorder.fbx" },
        { "blueprint", "Assets/My blender/Exhibits/Exhibit_8_blueprint.fbx" },
    };

    /// <summary>
    /// 받침 돌판 윗면. 진열장 로컬 y 다 — <c>Base_Cap</c> 이 1.02 에 두께 0.06 이라 1.05.
    /// 유저 FBX 는 원점이 바닥이라 여기 그대로 놓으면 «올려놓은» 게 된다.
    /// </summary>
    const float CapTop = 1.05f;

    /// <summary>
    /// 전시품을 <b>받침에서 얼마나 띄울지</b>. 2026-10-02 유저: *"코인이 안에 박힌 것 같다."*
    /// 유리 안쪽이 1.00~2.24 라 0.34 를 띄우면 물건이 <b>유리 한가운데</b>에 온다.
    /// </summary>
    const float ItemLift = 0.34f;

    /// <summary>
    /// 전시품을 키우는 배율. 유저가 보내준 코인이 가로 0.348 m 인데, 유리 안쪽 0.95 m 에서
    /// 8 m 떨어져 보면 작다. <b>박물관 전시품은 원래 «크게 보이게» 전시한다.</b>
    /// 1.7배면 0.59 m — 유리 안에서 사방 0.18 m 가 남는다.
    /// </summary>
    const float ItemScale = 1.7f;

    /// <summary>
    /// 전시품 FBX 를 칸에 세운다. 없으면 null 을 돌려주고 임시 도형이 그대로 쓰인다.
    /// 임포트 설정도 여기서 맞춘다 — 유저에게 인스펙터를 시키지 않는다(기획서 §9.3).
    /// </summary>
    static GameObject MakeExhibitModel(Transform caseRoot, string id,
                                       out Quaternion viewRotation, out bool isFlat)
    {
        viewRotation = Quaternion.identity;
        isFlat = false;
        if (!ExhibitModels.TryGetValue(id, out string path)) return null;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[전시실] {id} 모델을 못 찾았다 — {path}. 임시 도형으로 둔다.");
            return null;
        }

        // 재질이 밖에 있으면(External) 모델이 <b>새하얗게</b> 나온다 — 카트·곰에서 두 번 겪었다
        if (AssetImporter.GetAtPath(path) is ModelImporter importer
            && importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
        {
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        // ★ 바로 언팩한다 — 이름·자리만 줘도 그건 «오버라이드» 라,
        //   나중에 다른 모델이 리임포트를 돌리면 조용히 되돌아간다(2026-09-22 스피커).
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        go.name = "RealItem";
        go.transform.SetParent(caseRoot, false);
        go.transform.localPosition = new Vector3(0f, CapTop + ItemLift, 0f);

        // ★ 회전과 스케일을 <b>대입하지 않고 얹는다.</b> 임포트한 FBX 는 축 변환을
        //   루트 트랜스폼으로 들고 오는데, 덮어쓰면 모델이 눕거나 1만 분의 1 이 된다
        //   (2026-09-21 곰 · 2026-09-22 로비 소품에서 각각 겪었다).
        var t = go.transform;
        t.localScale = Vector3.Scale(t.localScale, Vector3.one * ItemScale);

        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

        // ★★ <b>납작한 것은 세워서 보여준다 — 각도는 찍지 말고 재서 고른다.</b>
        //   여덟 중 다섯이 서류·설계도라 두께가 2~6cm 다. 눕혀 놓고 돌리면
        //   <b>옆을 지날 때마다 사라진다</b> — 박물관이 문서를 눕히지 않고
        //   비스듬히 세워 전시하는 이유도 같다.
        //
        //   처음엔 −62도를 그냥 박았더니 <b>모델마다 결과가 달랐다</b>(세 개가 옆으로 섰다).
        //   FBX 축 변환이 모델마다 달리 들어와서, 같은 각도가 같은 자세를 만들지 않는다.
        //   후보 각도를 다 돌려 보고 <b>보는 사람 쪽 넓이가 제일 큰 것</b>을 고른다 —
        //   이 프로젝트에서 «앞면은 짐작하지 말고 재라» 로 두 번 배운 그 방법이야.
        // ★★ 2026-10-06 <b>세워 놓는 물건이 등을 돌리고 있었다</b>
        //   (유저: *"전시실 오브젝트들 기본 상태가 뒤로 돌려져 있다"*).
        //   납작한 서류는 윗면이 곧 «앞» 이라 상관없는데, 표식·녹음기처럼 <b>서 있는 물건</b>은
        //   앞뒤가 있고 그게 <b>−Z(벽 쪽)</b> 를 보고 있었다. 각도를 찍지 말고 <b>재서</b> 돌린다 —
        //   정점이 몰린 쪽이 앞이다(부품 이름이 없는 묶음에 쓰는 그 방법).
        bool flat = IsFlatItem(go.transform, caseRoot);
        if (!flat && FrontSkewZ(go.transform, caseRoot) < 0f)
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * go.transform.localRotation;

        // ★ 기울이기 <b>전</b> 자세를 적어 둔다 — 돋보기는 이걸 쓴다(GalleryCase.viewRotation)
        viewRotation = go.transform.localRotation;
        isFlat = flat;

        TiltFlatItem(go.transform, caseRoot);
        CentreInGlass(go.transform, caseRoot);
        // 떠서 천천히 돈다 — 어두운 방에서 움직이는 건 이것뿐이라 눈이 여기로 온다
        go.AddComponent<ExhibitSpin>();
        return go;
    }

    /// <summary>
    /// 진열장 기준 전시품 상자 — <b>정점을 직접 돌려서</b> 잰다.
    ///
    /// ★★ 2026-10-06 <b>여기가 진짜 범인이었다.</b> 전에는 <c>Renderer.bounds</c>(월드 AABB)의
    /// 꼭짓점 여덟 개를 진열장 좌표로 내려 썼는데, 진열장이 <b>45도씩 돌아가 있어서</b>
    /// 월드 AABB 가 이미 부풀어 있고 그걸 다시 내리면 <b>두 번 부푼다.</b>
    /// 측정값이 그걸 그대로 보여줬다 — 장부가 0.772 × 0.102 × 0.566 인데
    /// <b>1.337 × 0.102 × 1.337</b> 로 나왔다(0.772 + 0.566 = 1.338, 45도의 교과서 증상).
    ///
    /// 그래서 «유리를 넘친다» 는 판정이 <b>멀쩡한 자세를 전부 떨어뜨렸고</b>, 남은 자세 중에
    /// 제일 넓은 걸 고르다 보니 여덟 개가 제각각 뒤집혀 섰다.
    /// 월드 AABB 는 회전한 오브젝트에서 언제나 거짓말한다 — 이 프로젝트에서 다섯 번째다.
    /// </summary>
    static Bounds LocalBox(Transform item, Transform caseRoot)
    {
        Vector3 lo = Vector3.one * 1e9f, hi = -Vector3.one * 1e9f;
        foreach (var mf in item.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;
            foreach (var v in mesh.vertices)
            {
                Vector3 p = caseRoot.InverseTransformPoint(mf.transform.TransformPoint(v));
                lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p);
            }
        }
        var box = new Bounds(); box.SetMinMax(lo, hi);
        return box;
    }

    /// <summary>
    /// 납작한 전시품만 <b>보는 사람 쪽으로 눕혀 세운다.</b> 나머지는 <b>손도 안 댄다.</b>
    ///
    /// ★★ 2026-10-06 <b>여덟 개가 전부 거꾸로 섰다</b>(유저: *"전시실에 전시된 모든
    /// 오브젝트들이 거꾸로 뒤집혀져 있다"*). 앞 판은 후보 각도에 <b>±90 과 Z축 굴리기</b>를
    /// 넣어 두고 «정면에서 본 넓이가 제일 큰 자세» 를 골랐는데, 그 점수는
    /// <b>위아래를 구분하지 않는다</b> — 뒤집힌 자세가 더 넓으면 뒤집힌 게 이긴다.
    /// 동전은 −50도가 제일 넓어서 바닥을 보이며 기울었고, 두꺼운 것들도 다 끌려갔다.
    ///
    /// 고치는 방향은 <b>후보를 늘리는 게 아니라 줄이는 것</b>이다:
    /// <list type="bullet">
    /// <item><b>굴리기(Z축)를 없앤다.</b> 전시품을 옆으로 눕힐 이유가 하나도 없다</item>
    /// <item><b>뒤로 넘기지 않는다.</b> X축 <b>양수</b>로만 — 윗면이 보는 사람 쪽으로 온다
    ///       (음수로 돌리면 바닥을 보여주게 된다. 여덟 중 다섯이 그랬다)</item>
    /// <item><b>납작한 것만 건드린다.</b> 두께가 가로의 45% 를 넘으면 그냥 세워 둔다</item>
    /// </list>
    ///
    /// 블렌더에서 재 보면 여덟 개 전부 <b>바닥이 z = 0</b> 이다 — 즉 임포트한 그대로가
    /// 이미 바로 선 자세고, 그게 <c>home</c> 이다. 건드릴수록 나빠지는 쪽이었어.
    /// </summary>
    static void TiltFlatItem(Transform item, Transform caseRoot)
    {
        if (item.GetComponentsInChildren<Renderer>(true).Length == 0) return;

        Quaternion home = item.localRotation;

        // 두껍거나 키가 있는 것(동전·표식·녹음기)은 그대로 세워 둔다
        if (!IsFlatItem(item, caseRoot)) return;

        // 세울수록 잘 보이지만 유리를 넘으면 안 된다 — 큰 각도부터 보고 처음 들어가는 것을 쓴다
        foreach (float a in new[] { 68f, 55f, 42f, 30f })
        {
            item.localRotation = Quaternion.Euler(a, 0f, 0f) * home;
            Vector3 s = LocalBox(item, caseRoot).size;
            if (s.x <= 0.92f && s.y <= 1.05f) return;
        }

        item.localRotation = home;   // 어느 각도도 안 들어가면 눕힌 채로 둔다
    }

    /// <summary>
    /// <b>납작한가</b> — 두께가 가로의 45% 를 못 넘으면 서류로 본다.
    /// 세우는 것(<see cref="TiltFlatItem"/>)과 돋보기 자세가 같은 판정을 써야 어긋나지 않는다.
    /// </summary>
    static bool IsFlatItem(Transform item, Transform caseRoot)
    {
        Vector3 s = LocalBox(item, caseRoot).size;
        return s.y < 0.45f * Mathf.Max(s.x, s.z);
    }

    /// <summary>
    /// 정점 무게중심이 상자 한가운데에서 <b>앞뒤(진열장 z)로 얼마나 쏠렸나</b>.
    /// 양수면 지금 앞(방 안쪽, +Z)을 보고 있는 것. 이 묶음은 한 파일이 메시 한 덩이라
    /// «앞면에만 있는 부품» 이 없어서 이 방법밖에 없다.
    /// </summary>
    static float FrontSkewZ(Transform item, Transform caseRoot)
    {
        double sum = 0.0; int n = 0;
        foreach (var mf in item.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            foreach (var v in mf.sharedMesh.vertices)
            { sum += caseRoot.InverseTransformPoint(mf.transform.TransformPoint(v)).z; n++; }
        }
        if (n == 0) return 0f;
        return (float)(sum / n) - LocalBox(item, caseRoot).center.z;
    }

    /// <summary>
    /// 전시품을 <b>유리 한가운데에 띄운다.</b> 유리 안쪽이 1.00~2.24 라 가운데가 1.62 다.
    ///
    /// 자리를 «바닥에서 얼마» 로 잡으면 물건마다 높이가 달라 <b>어떤 건 박히고 어떤 건 뜬다</b>
    /// (유저: *"코인이 안에 박힌 것 같다"*). 세워서 기울이면 더 벌어진다 —
    /// 기울인 뒤 <b>실제 상자의 한가운데</b>를 재서 거기에 맞추면 여덟 개가 같은 높이에 뜬다.
    /// </summary>
    const float GlassMid = 1.62f;

    static void CentreInGlass(Transform item, Transform caseRoot)
    {
        Bounds b = LocalBox(item, caseRoot);
        if (b.size == Vector3.zero) return;
        item.localPosition += new Vector3(0f, GlassMid, 0f) - b.center;
    }

    static GalleryCase[] MakeCases()
    {
        var root = new GameObject("DisplayCases").transform;
        var result = new GalleryCase[ExhibitCatalogue.Count];

        for (int i = 0; i < ExhibitCatalogue.Count; i++)
        {
            var item = ExhibitCatalogue.All[i];
            // <b>시계방향</b>으로 1번부터(2026-09-17 유저). 각도를 더하면 반시계라
            // 문으로 들어와서 오른쪽을 보면 8번이 먼저 나온다 — 번호를 따라가려면
            // 왼쪽으로 돌아야 해서 읽는 순서와 걷는 순서가 어긋났다.
            // 1번은 문 맞은편(북쪽), 거기서 오른쪽으로 돌면 2·3·4… 가 차례로 나온다.
            float angle = Mathf.PI * 0.5f - ((float)i / ExhibitCatalogue.Count) * Mathf.PI * 2f;
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
            var realModel = MakeExhibitModel(go.transform, item.id, out var viewRot, out var viewFlat);

            var display = go.AddComponent<GalleryCase>();
            display.itemId = item.id;
            display.displayName = item.name;
            display.chapter = item.chapter;
            display.description = item.description;
            display.itemAnchor = anchor;
            display.placeholder = placeholder;
            display.realModel = realModel;
            display.viewRotation = viewRot;
            display.itemIsFlat = viewFlat;
            display.itemRenderer = placeholder.GetComponent<Renderer>();
            display.plaqueRenderer = plaque.GetComponent<Renderer>();
            display.caseLight = caseLight;

            // 아직 못 모은 진열장을 덮는 흰 천. 유리보다 한 뼘 크게 씌워야 "덮었다" 로 보인다.
            var cover = new GameObject("DustCover").transform;
            cover.SetParent(go.transform, false);
            TestSceneBuilder.Cube(cover, "Cloth", new Vector3(0f, 1.7f, 0f),
                                  new Vector3(1.28f, 1.34f, 1.28f), ColDustCloth, keepCollider: false);
            TestSceneBuilder.Cube(cover, "Fold", new Vector3(0f, 1.02f, 0f),
                                  new Vector3(1.36f, 0.09f, 1.36f), ColDustFold, keepCollider: false);
            TestSceneBuilder.Cube(cover, "Hem", new Vector3(0f, 2.39f, 0f),
                                  new Vector3(1.2f, 0.06f, 1.2f), ColDustFold, keepCollider: false);
            display.dustCover = cover.gameObject;

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
    //  다 모으기 전 / 다 모은 뒤
    // ==================================================================
    /// <summary>
    /// <b>폐관 중.</b> 문 옆에 붉은 철거 통지서, 문 앞에 노란 출입 금지 띠, 구석에 싸둔 상자.
    ///
    /// 불이 꺼져 있는 <b>이유</b>가 방 안에 있어야 한다. 어둡기만 하면 조명 설정으로 보이고,
    /// 통지서가 붙어 있으면 "이 방은 닫혔다" 가 된다. 글씨는 안 쓴다 —
    /// 붉은 종이에 흰 줄 두 개면 통지서로 읽히고, 달리 읽힐 것도 없다.
    /// </summary>
    static GameObject[] MakeClosedNotice()
    {
        var root = new GameObject("ClosedNotice").transform;
        float wall = HallDepth * 0.5f - 0.3f;

        // 문 왼쪽 벽에 붙은 통지서 — 눈높이
        TestSceneBuilder.Cube(root, "Notice", new Vector3(-3.4f, 1.75f, wall),
                              new Vector3(0.9f, 1.25f, 0.04f), ColNotice, keepCollider: false);
        for (int i = 0; i < 4; i++)
            TestSceneBuilder.Cube(root, $"NoticeLine_{i}", new Vector3(-3.4f, 2.05f - i * 0.22f, wall - 0.03f),
                                  new Vector3(0.62f, 0.07f, 0.02f), ColNoticeInk, keepCollider: false);

        // 문 앞을 가로지르는 출입 금지 띠 두 줄
        for (int i = 0; i < 2; i++)
            TestSceneBuilder.Cube(root, $"Tape_{i}", new Vector3(0f, 1.15f + i * 0.5f, wall - 0.9f),
                                  new Vector3(5.2f, 0.11f, 0.03f), ColTapeWarn, keepCollider: false);

        // 구석에 싸둔 이삿짐 상자 — 방이 비워지는 중이라는 신호
        var boxAt = new[] { new Vector3(-10.6f, 0f, -9.4f), new Vector3(10.4f, 0f, -9.8f) };
        for (int b = 0; b < boxAt.Length; b++)
            for (int s = 0; s < 3; s++)
                TestSceneBuilder.Cube(root, $"Crate_{b}_{s}",
                                      boxAt[b] + new Vector3(s * 0.12f, 0.33f + s * 0.66f, s * 0.1f),
                                      new Vector3(1.05f - s * 0.12f, 0.64f, 0.9f - s * 0.1f),
                                      ColWoodDark, keepCollider: false, finish: Finish.나무);

        return new[] { root.gameObject };
    }

    /// <summary>
    /// <b>재개관.</b> 문 위 현수막, 문에서 방 가운데로 깔린 붉은 카펫, 화분 둘.
    /// 여덟 개를 다 모은 대가가 불빛 하나로만 끝나면 허전하다 — 방이 <b>손님을 받는 방</b>이 된다.
    /// </summary>
    static GameObject[] MakeReopenedDressing()
    {
        var root = new GameObject("Reopened").transform;
        float wall = HallDepth * 0.5f - 0.3f;

        // 문 위 현수막 — 붉은 리본 두 줄이 걸린 크림색 천
        TestSceneBuilder.Cube(root, "Banner", new Vector3(0f, 5.1f, wall - 0.08f),
                              new Vector3(7.4f, 1.15f, 0.05f), ColBannerBg, keepCollider: false);
        TestSceneBuilder.Cube(root, "BannerTop", new Vector3(0f, 5.72f, wall - 0.12f),
                              new Vector3(7.6f, 0.12f, 0.05f), ColCarpet, keepCollider: false);
        TestSceneBuilder.Cube(root, "BannerBottom", new Vector3(0f, 4.48f, wall - 0.12f),
                              new Vector3(7.6f, 0.12f, 0.05f), ColCarpet, keepCollider: false);

        // 문에서 방 가운데까지 붉은 카펫. 바닥보다 2cm 만 띄운다 — 같은 높이면 번쩍거린다.
        TestSceneBuilder.Cube(root, "Carpet", new Vector3(0f, 0.02f, wall * 0.5f),
                              new Vector3(3.1f, 0.03f, wall), ColCarpet, keepCollider: false);

        // 문 양옆 화분
        for (int s = -1; s <= 1; s += 2)
        {
            TestSceneBuilder.Cube(root, $"Pot_{s}", new Vector3(s * 2.6f, 0.34f, wall - 0.7f),
                                  new Vector3(0.62f, 0.68f, 0.62f), ColWoodDark, keepCollider: false,
                                  finish: Finish.나무);
            TestSceneBuilder.Cube(root, $"Bush_{s}", new Vector3(s * 2.6f, 1.06f, wall - 0.7f),
                                  new Vector3(0.9f, 0.86f, 0.9f), ColLeaf, keepCollider: false);
        }

        return new[] { root.gameObject };
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
        orbit.pitch = 16f;
        orbit.minPitch = 2f;
        // 받침점 1.4m + 12m x sin(20) = 5.5m < 천장 6.5m. 천장을 덮은 뒤로 이게 상한이야.
        orbit.maxPitch = 20f;
        orbit.idleDelay = 6f;
        return orbit;
    }
}
