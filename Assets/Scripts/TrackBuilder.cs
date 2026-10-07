using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 환웅박물관 순환 트랙을 코드로 만든다. 노면 · 벽 · 구역별 바닥색 · 체크포인트 · 결승선.
///
/// 기획서 §4.1 대로 **트랙은 하나뿐**이고, 챕터마다 문과 차단봉과 수집품 위치만 바꿔
/// 다른 사건처럼 보이게 한다. 그래서 이 스크립트는 뼈대(길과 벽)만 만들고,
/// 챕터별 배치는 나중에 따로 얹는다.
///
/// 길은 조절점 12개를 지나는 부드러운 곡선(캣멀-롬)으로 만든다. 각진 모서리가 생기면
/// 카트가 벽에 걸려서 코너를 못 도는데, 곡선으로 뽑으면 그 문제가 안 생긴다.
///
/// 지금은 회색 상자다. 구역마다 바닥색이 달라서 어디를 달리는지 구분만 되면 된 거야.
/// </summary>
public class TrackBuilder : MonoBehaviour
{
    /// <summary>
    /// 야외 캠퍼스를 도는 여섯 구간. 레퍼런스 그림의 담장 안쪽 도로를 따라간다.
    /// (기획서 §4.2 는 실내 6구역이었지만 2026-09-14 에 야외로 바꾸기로 했다.)
    /// </summary>
    public enum Zone { 본관앞, 서편전시동, 북서담장, 정문앞, 동편연못, 매표소굽이 }

    [System.Serializable]
    public struct ControlPoint
    {
        public Vector3 position;
        public float width;
        public Zone zone;
        public ControlPoint(float x, float z, float width, Zone zone)
        {
            position = new Vector3(x, 0f, z);
            this.width = width;
            this.zone = zone;
        }
    }

    /// <summary>
    /// 순환 코스의 조절점. 순서대로 한 바퀴를 돈다.
    ///
    /// <b>남쪽 변 다섯 점(12·13·0·1·2)은 일부러 z 를 똑같이 맞췄다</b>(2026-09-16). 캣멀-롬 곡선은
    /// 한 점의 접선이 <b>양옆 점</b>으로 정해지기 때문에, 점 두 개만 나란히 놔서는 직선이 안 나온다.
    /// 네 점이면 가운데 한 구간만 곧고, <b>다섯 점이어야 두 구간이 이어져</b> 결승선 양쪽이 다 직선이 된다.
    ///
    /// 직선이 왜 필요했냐면 — 전에는 제일 긴 직선이 <b>10 m</b> 였고 25 m 넘는 직선이 하나도 없었다.
    /// 그러면 최고 속도를 쓸 일이 없고, 추월할 자리도 없고, 매 바퀴가 "계속 꺾기" 한 가지 리듬이 된다.
    /// 결승선을 이 직선 한가운데 둬서 <b>직선 → 브레이크 → 코너 → 가속</b> 리듬이 생기게 했다.
    /// </summary>
    public static readonly ControlPoint[] Path =
    {
        new ControlPoint(  0f, -78f, 12.0f, Zone.본관앞),      // 출발 · 결승 — 직선 한가운데
        new ControlPoint(-32f, -78f, 10.5f, Zone.본관앞),      // 직선
        new ControlPoint(-64f, -78f,  9.5f, Zone.서편전시동),  // 직선 끝, 브레이킹 존
        new ControlPoint(-78f, -50f,  8.0f, Zone.서편전시동),  // 첫 코너
        new ControlPoint(-68f, -14f,  7.5f, Zone.서편전시동),
        new ControlPoint(-54f,  18f,  7.5f, Zone.북서담장),    // 안쪽으로 파고드는 S 자
        new ControlPoint(-62f,  50f,  8.0f, Zone.북서담장),
        new ControlPoint(-24f,  74f,  9.5f, Zone.정문앞),      // 한옥 정문 앞
        new ControlPoint( 20f,  74f,  9.5f, Zone.정문앞),
        new ControlPoint( 56f,  54f,  8.0f, Zone.동편연못),
        new ControlPoint( 70f,  18f,  7.0f, Zone.동편연못),    // 연못과 돌다리, 제일 좁다
        new ControlPoint( 64f, -24f,  7.5f, Zone.매표소굽이),
        new ControlPoint( 62f, -78f,  9.0f, Zone.매표소굽이),  // 마지막 코너 탈출 → 직선 진입
        new ControlPoint( 32f, -78f, 10.0f, Zone.본관앞),      // 직선
    };

    [Header("만들기")]
    [Tooltip("끄면 트랙이 아예 안 생긴다. 빈 맵으로 테스트하고 싶을 때")]
    public bool buildOnAwake = true;

    // 캠퍼스 씬은 <b>같은 코스를 길로만</b> 쓴다. 길을 없애면 캠퍼스 한가운데가 빈터가 되고,
    // 발판과 체크포인트를 남기면 걷다가 밟혀서 "여기서도 레이스를 하나" 로 읽힌다.
    [Tooltip("끄면 가속 발판과 체크포인트를 안 만든다 — 걸어다니는 씬용")]
    public bool raceFurniture = true;
    [Tooltip("조절점 하나당 몇 조각으로 쪼갤지. 높을수록 곡선이 부드럽지만 오브젝트가 늘어난다")]
    [Range(4, 24)] public int segmentsPerControl = 10;

    [Header("벽")]
    // 2.6m 짜리 벽은 카트(높이 0.73m)보다 세 배 넘게 높아서 터널처럼 보인다.
    // 1.2m 로 낮추면 넘어가진 않으면서 바깥 풍경이 보인다 — 실제 서킷 가드레일도 이 정도야.
    public float wallHeight = 0.85f;
    public float wallThickness = 0.8f;

    [Header("체크포인트")]
    [Range(4, 24)] public int checkpointCount = 12;

    // ---- 노면은 야외 아스팔트라 전부 비슷한 색이고, 구간 구분은 가드레일 색으로 준다 ----
    static readonly Color[] ZoneFloor =
    {
        new Color32(0x54, 0x53, 0x51, 0xFF),   // 본관앞     — 광장 쪽이라 조금 밝게
        new Color32(0x4C, 0x4B, 0x4A, 0xFF),   // 서편전시동
        new Color32(0x4A, 0x4A, 0x4C, 0xFF),   // 북서담장
        new Color32(0x56, 0x55, 0x52, 0xFF),   // 정문앞
        new Color32(0x4A, 0x4C, 0x4C, 0xFF),   // 동편연못
        new Color32(0x4E, 0x4C, 0x49, 0xFF),   // 매표소굽이
    };

    /// <summary>도로 양옆 가드레일. 레퍼런스의 나무 울타리와 돌담 색.</summary>
    static readonly Color[] ZoneWall =
    {
        new Color32(0x7A, 0x58, 0x3E, 0xFF),   // 본관앞     — 나무 울타리
        new Color32(0xA8, 0xA4, 0x98, 0xFF),   // 서편전시동 — 돌담
        new Color32(0xA8, 0xA4, 0x98, 0xFF),   // 북서담장   — 돌담
        new Color32(0x7A, 0x58, 0x3E, 0xFF),   // 정문앞     — 나무 울타리
        new Color32(0x8E, 0x9A, 0x8C, 0xFF),   // 동편연못   — 이끼 낀 돌
        new Color32(0x7A, 0x58, 0x3E, 0xFF),   // 매표소굽이 — 나무 울타리
    };

    static readonly Color ColKerb    = new Color32(0xC4, 0x45, 0x3E, 0xFF);   // 연석 빨강
    static readonly Color ColKerbAlt = new Color32(0xEF, 0xE7, 0xD6, 0xFF);   // 연석 크림 — 번갈아
    static readonly Color ColPostCap = new Color32(0x4E, 0x7A, 0x70, 0xFF);   // 기둥 머리 청록 기와
    // 담장 기단(2026-09-18). 두 돌색을 번갈아 — 담장을 따라 달릴 때 속도가 보인다.
    // 곰인형 — 로비 곰 조각상과 같은 색(2026-09-18)
    static readonly Color ColBearFur  = new Color32(0xA5, 0x75, 0x4A, 0xFF);
    static readonly Color ColBearFace = new Color32(0xE6, 0xDA, 0xC4, 0xFF);
    static readonly Color ColWallBase    = new Color32(0x8E, 0x89, 0x7E, 0xFF);
    static readonly Color ColWallBaseAlt = new Color32(0x9C, 0x96, 0x8A, 0xFF);
    static readonly Color ColWallBaseCap = new Color32(0x6F, 0x6A, 0x60, 0xFF);
    // 악당 광고 — 기획서 §4.4 대로 박물관 팔레트와 일부러 부딪히는 금색/자홍색.
    // Cast 의 개발업자·시의원 색과 같은 값이다.
    static readonly Color ColAdGold    = new Color32(0xC9, 0xA2, 0x27, 0xFF);
    static readonly Color ColAdMagenta = new Color32(0xB0, 0x40, 0x7F, 0xFF);
    static readonly Color ColAdFrame   = new Color32(0x3A, 0x36, 0x32, 0xFF);
    static readonly Color ColArchWood  = new Color32(0x6B, 0x4A, 0x33, 0xFF);

    static readonly Color ColLine     = new Color32(0xF2, 0xF3, 0xEE, 0xFF);
    static readonly Color ColLineDark = new Color32(0x2E, 0x2C, 0x2A, 0xFF);

    // 가속 발판 — 회색 아스팔트 위에서 멀리서도 튀어야 해서 팔레트 중 제일 센 색을 쓴다
    // 바퀴마다 달라지는 노면 (2026-10-01)
    static readonly Color ColWater      = new Color32(0x7E, 0xB4, 0xBE, 0xFF);   // 수면
    static readonly Color ColWaterDeep  = new Color32(0x3E, 0x6A, 0x74, 0xFF);   // 한 겹 아래 — 깊이가 보인다
    static readonly Color ColWetRoad    = new Color32(0x4A, 0x4C, 0x4A, 0xFF);   // 젖은 노면
    static readonly Color ColFoam       = new Color32(0xE4, 0xEE, 0xEC, 0xFF);   // 가장자리 포말
    static readonly Color ColBumpSoil   = new Color32(0x6E, 0x62, 0x4E, 0xFF);   // 솟아오른 흙
    static readonly Color ColBumpSlab   = new Color32(0xA8, 0xA4, 0x9A, 0xFF);   // 깨진 포석
    static readonly Color ColBumpSlabAlt= new Color32(0x8E, 0x89, 0x7E, 0xFF);
    static readonly Color ColBumpEdge   = new Color32(0x58, 0x4E, 0x3E, 0xFF);

    static readonly Color ColBoostPad   = new Color32(0x2E, 0x4C, 0x7A, 0xFF);
    static readonly Color ColBoostArrow = new Color32(0xFF, 0xD1, 0x3C, 0xFF);

    /// <summary>
    /// 결승선 위치. 카트를 여기에 놓으면 된다.
    /// 서스펜션 높이보다 넉넉히 띄운다 — 낮게 놓으면 노면에 파묻힌 채로 시작한다.
    /// (카트를 1.25배로 키우면서 서스펜션이 0.38 에서 0.475 로 올라갔다.)
    /// </summary>
    public Vector3 StartPosition => transform.position + PointOnPath(0f) + Vector3.up * 0.6f;
    public Quaternion StartRotation => Quaternion.LookRotation(TangentOnPath(0f), Vector3.up);

    /// <summary>한 바퀴 길이(m). 랩타임을 가늠할 때 쓴다.</summary>
    public float LapLength => MeasureLength();

    Transform built;

    void Awake()
    {
        if (buildOnAwake) Build();
    }

    /// <summary>
    /// 재생 중이 아니면 Destroy 가 "edit mode 에서 부르면 안 된다" 고 에러를 낸다.
    /// 에디터에서 Build() 를 눌러 코스 모양을 확인할 때 콘솔이 에러로 막히지 않게.
    /// </summary>
    static void Discard(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }

    public void Build()
    {
        if (built != null) Discard(built.gameObject);

        built = new GameObject("~TrackGeometry").transform;
        built.SetParent(transform, false);

        BuildSurface();
        BuildStartLine();
        if (raceFurniture) BuildBoostPads();
        // ★★ 2026-10-06 유저: *"첫 판 돌 때 물에 잠긴다 이런 거 넣지 말자. 플레이어가 알아서
        //   하게 냅두자. 어차피 한 판밖에 그런 기믹 없고, 지금 속도도 별로 안 줄어서
        //   그렇지 이건 떡밥이나 기분 나쁜 요소로 넘기자."* <b>맞는 판단이다.</b>
        //   한 판에만 있는 조작 규칙은 <b>배울 가치가 없는 규칙</b>이다 — 그 판에서만
        //   쓰고 버리니까 플레이어는 «왜 갑자기 미끄럽지» 로만 읽는다.
        //   침수와 둔덕은 <b>길에 그대로 둔다</b>(떡밥). 다만 <b>속도를 깎지는 않는다.</b>
        //   되살리려면 이 줄의 false 를 raceFurniture 로 바꾸면 된다.
        if (false) BuildLapHazards();
        BuildAdBoards(built);
        BuildAdSigns(built);
        BuildDebris(built);
        BuildCargo(built);
        BuildFinishArch(built);
        if (raceFurniture) BuildCheckpoints();
    }

    // ------------------------------------------------------------------
    //  바퀴마다 달라지는 노면 — 2바퀴 물기둥 · 3바퀴 울퉁불퉁
    // ------------------------------------------------------------------
    /// <summary>
    /// <b>잠기는 구간.</b> <c>(시작 t, 끝 t, 차선 왼끝, 차선 오른끝)</c>.
    ///
    /// 가속 발판(0.11 · 0.26 · 0.45 · 0.63 · 0.88)을 <b>하나도 안 품는다</b> —
    /// 발판을 밟는 순간 물에 끌리면 그건 장치가 아니라 사고야.
    ///
    /// 차선을 섞는다. <b>네 곳이 다 전폭이면 «피할 수 없는 벌»</b> 이고,
    /// 다 반폭이면 한쪽으로만 달리면 그만이다. 둘을 번갈아 둬야 «길을 고른다» 가 된다.
    /// </summary>
    static readonly (float t0, float t1, float laneA, float laneB)[] Floods =
    {
        (0.150f, 0.215f, -1.00f,  0.30f),   // 서편 진입 — 왼쪽만 잠긴다
        (0.330f, 0.395f, -1.00f,  1.00f),   // 전폭. 여긴 피할 데가 없다
        (0.520f, 0.585f,  0.10f,  1.00f),   // 연못 옆 — 오른쪽만. 연못에서 넘친 걸로 읽힌다
        (0.700f, 0.765f, -1.00f,  1.00f),   // 전폭
    };

    /// <summary>
    /// 둔덕 자리. <b>«랜덤» 으로 보이되 자리는 고정</b>이다 — 매번 달라지면 외울 수가 없고,
    /// 외울 수 없는 장애물은 실력이 아니라 운이다. 차선을 섞어서 «가운데로만 달리면 그만» 도 막는다.
    /// </summary>
    static readonly (float t, float lane)[] RoadBumps =
    {
        (0.03f, -0.25f), (0.08f,  0.32f), (0.13f,  0.00f), (0.17f, -0.38f),
        (0.22f,  0.28f), (0.29f, -0.30f), (0.35f,  0.36f), (0.42f,  0.00f),
        (0.50f, -0.34f), (0.53f,  0.30f), (0.60f, -0.26f), (0.66f,  0.34f),
        (0.74f,  0.00f), (0.78f, -0.32f), (0.85f,  0.28f), (0.93f, -0.30f),
    };

    const float BumpRise = 0.065f;    // 둔덕이 노면 위로 나온 높이

    /// <summary>
    /// 2바퀴 잠긴 도로와 3바퀴 둔덕을 <b>한 번에 지어 두고 꺼 둔다.</b>
    /// 켜고 끄는 건 <see cref="LapHazards"/> 가 한다.
    /// </summary>
    void BuildLapHazards()
    {
        var root = new GameObject("LapHazards").transform;
        root.SetParent(built, false);

        var floods = new GameObject("Floods").transform;
        floods.SetParent(root, false);
        for (int i = 0; i < Floods.Length; i++)
        {
            var (t0, t1, la, lb) = Floods[i];

            var zone = new GameObject($"Flood_{i + 1}").transform;
            zone.SetParent(floods, false);
            zone.position = transform.position + PointOnPath((t0 + t1) * 0.5f);

            // 젖은 노면 — <b>늘 깔려 있다.</b> 어디가 잠기는지 미리 보여야 길을 고를 수 있다
            Sheet(zone, "Wet", t0, t1, la, lb, 0.018f, FlatMaterial.Get(ColWetRoad));

            // 포말 — 물과 마른 길의 경계. 양 끝과 (반폭이면) 옆구리에
            var foam = new GameObject("Foam").transform;
            foam.SetParent(zone, false);
            Sheet(foam, "FoamIn", t0, t0 + 0.006f, la, lb, 0.030f, FlatMaterial.Get(ColFoam));
            Sheet(foam, "FoamOut", t1 - 0.006f, t1, la, lb, 0.030f, FlatMaterial.Get(ColFoam));
            if (la > -0.98f || lb < 0.98f)
            {
                if (la > -0.98f) Sheet(foam, "FoamL", t0, t1, la, la + 0.08f, 0.030f, FlatMaterial.Get(ColFoam));
                if (lb < 0.98f) Sheet(foam, "FoamR", t0, t1, lb - 0.08f, lb, 0.030f, FlatMaterial.Get(ColFoam));
            }

            // 물 — <b>두 겹.</b> 한 장이면 «파란 판때기» 고, 두 장이 어긋나야 «수면» 이 된다
            var under = Sheet(zone, "Under", t0, t1, la, lb, 0.055f, FlatMaterial.Water(ColWaterDeep, 0.52f));
            var top = Sheet(zone, "Surface", t0, t1, la, lb, 0.075f, FlatMaterial.Water(ColWater, 0.38f));

            // 잠기는 판정 — 수면 높이까지만. 위로 지나가는 건 안 잡는다
            var box = zone.gameObject.AddComponent<BoxCollider>();
            var p0 = PointOnPath(t0);
            var p1 = PointOnPath(t1);
            float span = Vector3.Distance(p0, p1) + 14f;
            box.isTrigger = true;
            box.size = new Vector3(span, 1.0f, span);
            box.center = new Vector3(0f, 0.5f, 0f);

            var fz = zone.gameObject.AddComponent<FloodZone>();
            fz.surface = top.transform;
            fz.underLayer = under.transform;
            fz.foam = foam.gameObject;
            fz.phase = i * 1.9f;        // 네 곳이 같이 차면 «수영장» 이지 «길» 이 아니다
        }
        floods.gameObject.SetActive(false);

        var bumps = new GameObject("Bumps").transform;
        bumps.SetParent(root, false);
        for (int i = 0; i < RoadBumps.Length; i++)
        {
            var (t, lane) = RoadBumps[i];
            Vector3 forward = TangentOnPath(t);
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            float width = WidthOnPath(t);
            Vector3 at = transform.position + PointOnPath(t) + side * (lane * width * 0.5f);
            var rot = Quaternion.LookRotation(forward, Vector3.up);

            var cluster = new GameObject($"Bump_{i + 1}").transform;
            cluster.SetParent(bumps, false);
            cluster.SetPositionAndRotation(at, rot);
            cluster.gameObject.AddComponent<RoadBump>();   // 카트가 «벽이 아니다» 로 읽는 표식

            // 한 자리에 둘씩 — 하나만 두면 «과속방지턱» 이고, 둘이 어긋나야 «울퉁불퉁» 이 된다
            for (int k = 0; k < 2; k++)
            {
                float along = (k == 0 ? -1.6f : 1.9f);
                float off = (k == 0 ? -0.7f : 0.8f) * (i % 2 == 0 ? 1f : -1f);
                Hump(cluster, $"Hump_{k}", at + forward * along + side * off, rot,
                     3.0f - k * 0.5f, 2.2f, i * 7 + k);
            }
        }
        bumps.gameObject.SetActive(false);

        var gate = root.gameObject.AddComponent<LapHazards>();
        gate.floods = floods.gameObject;
        gate.bumps = bumps.gameObject;
    }

    /// <summary>
    /// 코스를 따라 깔리는 <b>판</b>. 길이 휘어 있어서 상자 하나로는 못 덮는다 —
    /// 네모를 늘어놓으면 코너 바깥에 쐐기 구멍이 생긴다(2026-09-15 에 길에서 겪은 그것).
    /// 그래서 <b>경로를 따라 띠 메시</b>를 짠다.
    /// </summary>
    GameObject Sheet(Transform parent, string name, float t0, float t1,
                     float laneA, float laneB, float lift, Material material)
    {
        int steps = Mathf.Max(2, Mathf.CeilToInt((t1 - t0) * 240f));
        var verts = new Vector3[(steps + 1) * 2];
        var tris = new int[steps * 6];
        Vector3 origin = parent.position;

        for (int i = 0; i <= steps; i++)
        {
            float t = Mathf.Lerp(t0, t1, i / (float)steps);
            Vector3 c = transform.position + PointOnPath(t) + Vector3.up * lift;
            Vector3 side = Vector3.Cross(Vector3.up, TangentOnPath(t));
            float half = WidthOnPath(t) * 0.5f;
            verts[i * 2] = c + side * (laneA * half) - origin;
            verts[i * 2 + 1] = c + side * (laneB * half) - origin;
        }
        for (int i = 0; i < steps; i++)
        {
            int v = i * 2, k = i * 6;
            tris[k] = v; tris[k + 1] = v + 2; tris[k + 2] = v + 1;
            tris[k + 3] = v + 1; tris[k + 4] = v + 2; tris[k + 5] = v + 3;
        }

        var mesh = new Mesh { name = name };
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    /// <summary>
    /// 둔덕 하나. <b>부딪히는 건 완만한 돔, 보이는 건 깨져 솟은 포석</b>이다.
    ///
    /// 돔만 두면 «유니티 기본 구» 로 보인다(유저: *"레고 느낌"*). 그렇다고 기울어진 판을
    /// 콜라이더로 쓰면 모서리가 서서 카트가 걸린다. <b>물리는 돔, 그림은 판</b>으로 가른다 —
    /// 카트 그림을 껍데기 안에 넣는 것과 같은 규칙이야.
    ///
    /// ★ 구 프리미티브의 <c>SphereCollider</c> 를 그대로 두면 안 된다. 비균등 스케일에서
    /// 반지름이 <b>제일 큰 축</b>으로 잡혀서 길 한가운데 거대한 공이 생긴다
    /// (CLAUDE.md 의 «납작하게 누른 캡슐» 과 같은 함정).
    /// </summary>
    void Hump(Transform parent, string name, Vector3 at, Quaternion rot, float wide, float longways, int seed)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(at + Vector3.up * (BumpRise - 0.16f), rot);
        go.transform.localScale = new Vector3(wide, 0.32f, longways);
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(ColBumpSoil);
        go.isStatic = true;

        var sphere = go.GetComponent<SphereCollider>();
        if (sphere != null) Discard(sphere);
        var mesh = go.AddComponent<MeshCollider>();
        mesh.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;

        // 깨져 솟은 포석 — 같은 돔 위에 <b>기울어진 판 넷</b>. 전부 콜라이더 없음.
        var rng = new System.Random(seed * 977 + 13);
        float Next(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        for (int k = 0; k < 4; k++)
        {
            float u = Next(-0.34f, 0.34f), v = Next(-0.34f, 0.34f);
            Vector3 c = at + rot * new Vector3(u * wide, 0f, v * longways);
            float rise = BumpRise * (1f - (Mathf.Abs(u) + Mathf.Abs(v)));
            var tilt = rot * Quaternion.Euler(Next(-16f, 16f), Next(-40f, 40f), Next(-16f, 16f));
            Block(parent, $"{name}_Slab{k}", c + Vector3.up * Mathf.Max(0.02f, rise), tilt,
                  new Vector3(Next(0.8f, 1.5f), 0.07f, Next(0.7f, 1.3f)),
                  k % 2 == 0 ? ColBumpSlab : ColBumpSlabAlt, noCollider: true);
        }

        // 가장자리 — 어디가 솟았는지 눈에 들어와야 피할 수 있다
        Block(parent, name + "_Edge", at + Vector3.up * 0.012f, rot,
              new Vector3(wide * 1.06f, 0.02f, longways * 1.06f), ColBumpEdge, noCollider: true);
    }

    // ------------------------------------------------------------------
    //  가속 발판
    // ------------------------------------------------------------------
    /// <summary>
    /// 코스를 도는 길에서 <b>고를 게 있는 길</b>로 바꾸는 장치.
    ///
    /// 자리를 코스 한가운데가 아니라 <b>한쪽 차선</b>에 둔다. 그래야 밟으려고 선을 바꾸게 되고,
    /// 그 선이 다음 코너에 좋은 선이 아닐 때 판단할 게 생긴다. 가운데 놓으면 그냥 지나가다 먹는다.
    ///
    /// t 값(0~1)은 한 바퀴에서의 위치다. 자리를 옮기고 싶으면 이 표만 고치면 되고,
    /// 나중에 네가 코스 모양(Path)을 바꿔도 발판이 알아서 따라간다.
    /// </summary>
    static readonly (float t, float lane)[] BoostPads =
    {
        // 직선이 생기면서 자리를 다시 골랐다(2026-09-16). 직선 위의 발판은 그냥 지나가다 먹으니
        // 재미가 없다 — <b>직선 끝, 브레이킹 존 직전</b>에 두면 "밟고 들어갈래 말래" 가 생긴다.
        (0.11f,  0.55f),   // 직선 끝 — 밟으면 빠른데 브레이킹이 늦어진다
        (0.26f, -0.50f),   // 서편 첫 코너 탈출
        (0.45f,  0.00f),   // 정문앞 넓은 구간 — 여긴 한가운데라 누구나 먹는다
        (0.63f, -0.55f),   // 연못 돌다리 안쪽 라인, 제일 좁은 곳
        (0.88f,  0.50f),   // 마지막 코너 탈출 — 직선 진입 속도가 걸린다
    };

    void BuildBoostPads()
    {
        var root = new GameObject("BoostPads").transform;
        root.SetParent(built, false);

        for (int i = 0; i < BoostPads.Length; i++)
        {
            var (t, lane) = BoostPads[i];

            Vector3 forward = TangentOnPath(t);
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            float width = WidthOnPath(t);
            Vector3 centre = transform.position + PointOnPath(t) + side * (lane * width * 0.5f);

            var pad = new GameObject($"BoostPad_{i + 1}");
            pad.transform.SetParent(root, false);
            pad.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(forward, Vector3.up));

            // 밟는 판정 — 낮게 스치듯 지나가도 잡히게 위로 넉넉히
            var box = pad.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(PadWidth, 2.2f, PadLength);
            box.center = new Vector3(0f, 1.1f, 0f);

            pad.AddComponent<BoostPad>();

            // 바닥판
            Block(pad.transform, "Surface", centre + Vector3.up * 0.03f,
                  pad.transform.rotation, new Vector3(PadWidth, 0.06f, PadLength),
                  ColBoostPad, noCollider: true);

            // 화살표 — 빠르게 지나가도 "앞으로 민다" 는 게 읽히게 세 겹
            for (int c = 0; c < 3; c++)
            {
                float z = (c - 1) * (PadLength * 0.27f);
                Chevron(pad.transform, $"Chevron_{c}", centre + Vector3.up * 0.05f + forward * z,
                        pad.transform.rotation);
            }
        }
    }

    const float PadWidth = 3.4f;
    const float PadLength = 6f;

    /// <summary>앞을 가리키는 꺾인 화살표 하나. 막대 두 개를 V 자로 세워서 만든다.</summary>
    void Chevron(Transform parent, string name, Vector3 centre, Quaternion rotation)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var go = Block(parent, $"{name}_{(s < 0 ? "L" : "R")}", centre,
                           rotation, new Vector3(PadWidth * 0.52f, 0.05f, 0.5f),
                           ColBoostArrow, noCollider: true);

            // 비틀어 V 자로. Block 은 월드 기준으로 놓으니 여기서도 월드 회전을 준다.
            go.transform.rotation = rotation * Quaternion.Euler(0f, s * 34f, 0f);
            go.transform.position = centre + rotation * new Vector3(s * PadWidth * 0.21f, 0f, 0f);
        }
    }

    // ------------------------------------------------------------------
    //  노면과 벽
    // ------------------------------------------------------------------
    /// <summary>
    /// 노면과 벽을 <b>끊기지 않는 하나의 면</b>으로 만든다.
    ///
    /// 예전에는 네모 상자를 이어 붙였는데, 코너에서 상자끼리 각도가 벌어지면
    /// <b>바깥쪽에 쐐기 모양 틈</b>이 생긴다. 가운데를 겹쳐도 바깥은 안 덮인다 —
    /// 폭 10m 코스가 한 조각에 15도씩 꺾이면 바깥 가장자리에 1m 넘는 구멍이 난다.
    /// 카트가 트랙 중간에서 떨어지던 게 이거였어.
    ///
    /// 이제 가장자리 점을 먼저 다 구해서 삼각형으로 잇는다. 틈이 생길 자리가 아예 없고,
    /// 덤으로 오브젝트가 500개에서 20개 아래로 줄어든다.
    /// </summary>
    void BuildSurface()
    {
        var road = new GameObject("Road").transform;   road.SetParent(built, false);
        var walls = new GameObject("Walls").transform; walls.SetParent(built, false);
        var kerbs = new GameObject("Kerbs").transform; kerbs.SetParent(built, false);

        int total = Path.Length * segmentsPerControl;

        // 한 바퀴를 돌며 양쪽 가장자리 점을 먼저 모은다 (마지막에 처음으로 되돌아와 닫는다)
        var outer = new Vector3[total + 1];
        var inner = new Vector3[total + 1];
        var zones = new int[total + 1];

        for (int i = 0; i <= total; i++)
        {
            float t = (float)(i % total) / total;
            Vector3 p = PointOnPath(t);
            Vector3 side = Vector3.Cross(Vector3.up, TangentOnPath(t));
            float half = WidthOnPath(t) * 0.5f;

            outer[i] = p + side * half;
            inner[i] = p - side * half;
            zones[i] = ZoneOnPath(t);
        }

        // 구간(Zone)마다 색이 달라서 조각을 나눈다. 경계에서는 한 칸씩 겹쳐서 이어지게 한다.
        int start = 0;
        for (int i = 1; i <= total; i++)
        {
            if (i < total && zones[i] == zones[start]) continue;

            int end = Mathf.Min(i + 1, total);   // 한 칸 더 — 구간 사이가 벌어지지 않게
            int zone = zones[start];

            Ribbon(road, $"Road_{zone}_{start:000}", outer, inner, start, end,
                   Vector3.zero, Vector3.zero, ZoneFloor[zone]);

            // 벽은 가장자리에서 위로 세운 띠. 안쪽을 향하게 뒤집어 준다.
            Ribbon(walls, $"WallOuter_{zone}_{start:000}", outer, outer, start, end,
                   Vector3.zero, Vector3.up * wallHeight, ZoneWall[zone], flip: true);
            Ribbon(walls, $"WallInner_{zone}_{start:000}", inner, inner, start, end,
                   Vector3.up * wallHeight, Vector3.zero, ZoneWall[zone], flip: true);

            start = i;
            if (start >= total) break;
        }

        BuildKerbs(kerbs, outer, inner, total);
        BuildWarningStripe(walls, outer, inner, total);
        BuildWallBase(walls, outer, inner, total);
        BuildRailPosts(walls, outer, inner, zones, total);
    }

    /// <summary>
    /// 가드레일 기둥. 벽이 매끈한 띠 하나면 "코드로 뽑은 면" 으로 보인다 —
    /// 일정 간격으로 기둥이 서 있어야 사람이 세운 울타리로 읽히고, 달릴 때 속도감도 생긴다
    /// (지나가는 기둥이 눈에 박자를 만든다).
    /// </summary>

    /// <summary>
    /// 벽 꼭대기를 두르는 <b>빨강·크림 경고 띠</b>.
    ///
    /// 유저가 "뭐가 벽이고 뭐가 인테리어인지 구분이 안 돼서 부딪힘 횟수가 랜덤으로 깎인다" 고
    /// 했다(2026-09-16). 맞는 지적이야 — 벽 색을 구간마다 나무·돌담·이끼로 다르게 칠해놨더니
    /// <b>벽이 풍경처럼 보였다.</b> 예쁘긴 한데 "부딪히면 아픈 것" 이라는 신호가 사라진 거지.
    ///
    /// 그래서 규칙을 하나 만든다: <b>이 띠가 있으면 부딪히는 것, 없으면 장식.</b>
    /// 광고판·나무·석등·가드레일 기둥은 콜라이더가 없으니 띠도 없다.
    /// 실제 서킷 방호벽이 빨강·흰색으로 칠해져 있는 것과 같은 이유야.
    ///
    /// 벽 색은 구간마다 그대로 둔다 — 띠는 꼭대기에만 얹히니까 박물관 분위기는 안 깨진다.
    /// </summary>
    /// <summary>
    /// <b>담장 아래 돌 기단.</b> 2026-09-18 유저: *"트랙이랑 로비의 레고 느낌을 손봐 줘."*
    ///
    /// 트랙은 작은 조각 비율이 57% 라 <b>개수가 원인이 아니었다.</b> 담장이 위아래로
    /// 통짜 한 색이라 <b>긴 상자 한 줄</b>로 보인 것이고, 캠퍼스 건물에서 효과를 본
    /// 굽도리를 그대로 두른다 — 아래 0.22m 만 돌색이면 담장이 담장이 된다.
    ///
    /// 경고 띠와 <b>같은 방식으로 3cm 밀어낸다.</b> 벽 면과 같은 평면에 놓으면
    /// 깊이값이 겹쳐 번쩍거린다(2026-09-16 에 이미 겪었다).
    /// 콜라이더는 안 단다 — 띠 조각까지 세지면 "벽 2번" 이 "벽 4번" 이 된다.
    /// </summary>
    void BuildWallBase(Transform parent, Vector3[] outer, Vector3[] inner, int total)
    {
        const float baseHeight = 0.22f;
        const float standOff = 0.032f;   // 경고 띠(0.03)보다 2mm 더 — 둘이 같은 평면이면 또 번쩍인다

        var baseOuter = new Vector3[total + 1];
        var baseInner = new Vector3[total + 1];
        for (int k = 0; k <= total; k++)
        {
            Vector3 toRoad = (inner[k] - outer[k]).normalized;
            baseOuter[k] = outer[k] + toRoad * standOff;
            baseInner[k] = inner[k] - toRoad * standOff;
        }

        var foot = Vector3.zero;
        var cap = Vector3.up * baseHeight;
        var lip = Vector3.up * (baseHeight + 0.05f);

        // 기단 몸통 — 한 덩어리로 두르면 또 한 색이니 <b>돌 두 색을 번갈아</b> 놓는다.
        // 8칸마다 바뀌는데, 이게 담장을 따라 걷거나 달릴 때 <b>속도가 보이게</b> 한다.
        const int block = 8;
        for (int i = 0; i < total; i += block)
        {
            int to = Mathf.Min(i + block, total);
            var color = (i / block) % 2 == 0 ? ColWallBase : ColWallBaseAlt;

            Ribbon(parent, $"BaseOuter_{i:000}", baseOuter, baseOuter, i, to, foot, cap, color,
                   flip: true, collider: false);
            Ribbon(parent, $"BaseInner_{i:000}", baseInner, baseInner, i, to, cap, foot, color,
                   flip: true, collider: false);
        }

        // 기단 윗선 — 턱이 있어야 다른 재료로 읽힌다. 색만 바뀌면 칠한 자국이다.
        Ribbon(parent, "BaseCapOuter", baseOuter, baseOuter, 0, total, cap, lip, ColWallBaseCap,
               flip: true, collider: false);
        Ribbon(parent, "BaseCapInner", baseInner, baseInner, 0, total, lip, cap, ColWallBaseCap,
               flip: true, collider: false);
    }

    void BuildWarningStripe(Transform parent, Vector3[] outer, Vector3[] inner, int total)
    {
        const float bandHeight = 0.2f;
        const int stripeLength = 4;          // 몇 칸마다 색이 바뀌는지

        // <b>벽 면과 같은 평면에 놓으면 안 된다.</b> 처음에 그렇게 만들었더니 빨강과 벽색이
        // 픽셀마다 번갈아 찍혀서 "빨간 줄이 회색으로 번쩍거린다" 는 보고가 왔다(2026-09-16).
        // 깊이값이 똑같으면 GPU 는 어느 쪽이 앞인지 정할 방법이 없고, 카메라가 조금만 움직여도
        // 반올림 결과가 뒤집힌다. 고칠 방법은 하나뿐 — <b>떼어 놓는 것</b>.
        const float standOff = 0.03f;        // 코스 쪽으로 3cm. 눈에는 안 보이고 깊이는 확실히 갈린다
        const float capRise  = 0.01f;        // 벽 꼭대기보다 1cm 높게 — 띠가 벽을 덮는 것처럼 보인다

        // 띠를 얹을 자리: 벽 선에서 코스 안쪽으로 밀어낸 선. 미는 방향이 점마다 다르니
        // (코너에서는 벽이 기울어 있다) Ribbon 의 고정 오프셋으로는 안 되고 점을 새로 구해야 한다.
        var bandOuter = new Vector3[total + 1];
        var bandInner = new Vector3[total + 1];
        for (int k = 0; k <= total; k++)
        {
            Vector3 toRoad = (inner[k] - outer[k]).normalized;
            bandOuter[k] = outer[k] + toRoad * standOff;
            bandInner[k] = inner[k] - toRoad * standOff;
        }

        var low = Vector3.up * (wallHeight - bandHeight);
        var top = Vector3.up * (wallHeight + capRise);

        for (int i = 0; i < total; i += stripeLength)
        {
            int to = Mathf.Min(i + stripeLength, total);
            var color = (i / stripeLength) % 2 == 0 ? ColKerb : ColKerbAlt;

            // <b>콜라이더를 달면 안 된다.</b> 띠는 벽 면 바로 앞에 있어서, 콜라이더가 있으면
            // 벽을 한 번 긁을 때 벽 조각과 띠 조각이 <b>각각</b> 세진다.
            // 실제로 그래서 "벽 2번" 이어야 할 게 "벽 4번" 으로 떴다(2026-09-16).
            Ribbon(parent, $"StripeOuter_{i:000}", bandOuter, bandOuter, i, to, low, top, color, flip: true, collider: false);
            Ribbon(parent, $"StripeInner_{i:000}", bandInner, bandInner, i, to, top, low, color, flip: true, collider: false);
        }
    }
    /// <summary>
    /// 광고판 임무를 이미 깼나. 그 판의 상품이 들어와 있으면 깬 것이다.
    ///
    /// ★ 전에는 <c>const int adMission = 6</c> 이 여기 박혀 있었다. 판 순서를 바꾸는 순간
    /// 조용히 <b>다른 판</b>을 가리켜서, 광고판이 엉뚱한 판에 사라진다.
    /// 번호는 <see cref="MissionManager"/> 가 순서표에서 직접 찾는다.
    /// </summary>
    public static bool AdSignsCleared => MissionManager.AlreadyCleared(MissionManager.Goal.광고판);

    void BuildRailPosts(Transform parent, Vector3[] outer, Vector3[] inner, int[] zones, int total)
    {
        const int every = 5;   // 조각 다섯 개마다 하나 — 대략 4m 간격

        for (int i = 0; i < total; i += every)
        {
            Vector3 along = (outer[(i + 1) % total] - outer[i]).normalized;
            if (along.sqrMagnitude < 0.5f) continue;

            var rot = Quaternion.LookRotation(along, Vector3.up);
            var color = ZoneWall[zones[i]];
            float h = wallHeight + 0.35f;   // 벽보다 조금 높게 — 기둥 머리가 보이게

            foreach (var edge in new[] { outer[i], inner[i] })
            {
                Block(parent, $"Post_{i:000}", edge + Vector3.up * (h * 0.5f), rot,
                      new Vector3(0.26f, h, 0.26f), color, noCollider: true);

                // 기둥 머리 — 한옥 기둥처럼 한 겹 얹는다
                Block(parent, $"PostCap_{i:000}", edge + Vector3.up * (h + 0.05f), rot,
                      new Vector3(0.38f, 0.1f, 0.38f), ColPostCap, noCollider: true);
            }
        }
    }

    /// <summary>
    /// 두 줄의 점을 삼각형으로 잇는다. 같은 줄을 두 번 넘기고 높이만 다르게 주면 벽이 된다.
    /// </summary>
    void Ribbon(Transform parent, string name, Vector3[] a, Vector3[] b, int from, int to,
                Vector3 offsetA, Vector3 offsetB, Color color, bool flip = false,
                bool collider = true, float lift = 0f)
    {
        int count = to - from + 1;
        if (count < 2) return;

        var verts = new Vector3[count * 2];
        var tris = new int[(count - 1) * 6];
        Vector3 up = Vector3.up * lift;

        for (int i = 0; i < count; i++)
        {
            verts[i * 2]     = a[from + i] + offsetA + up;
            verts[i * 2 + 1] = b[from + i] + offsetB + up;
        }

        for (int i = 0; i < count - 1; i++)
        {
            int v = i * 2, t = i * 6;
            if (flip)
            {
                tris[t] = v;     tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }
            else
            {
                tris[t] = v;     tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                tris[t + 3] = v + 1; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
            }
        }

        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.isStatic = true;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = FlatMaterial.Get(color);

        // 노면과 벽은 정적 지형이라 MeshCollider 가 맞다.
        // (카트와 캐릭터에는 절대 붙이지 않는다 — CLAUDE.md 의 회색상자 교체 규칙)
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    /// <summary>코너가 눈에 들어오게 연석을 띄엄띄엄. 장식이라 충돌체는 없다.</summary>
    void BuildKerbs(Transform parent, Vector3[] outer, Vector3[] inner, int total)
    {
        const int on = 5, period = 10;

        for (int i = 0; i < total; i += period)
        {
            int to = Mathf.Min(i + on, total);
            if (to - i < 2) continue;

            // 빨강·크림을 번갈아. 한 색으로 쭉 가면 띠 하나로 뭉쳐 보인다
            bool alt = (i / period) % 2 == 1;

            // 가장자리에서 안쪽으로 살짝 들여 그린다
            var a = new Vector3[to + 1];
            var b = new Vector3[to + 1];
            for (int k = i; k <= to; k++)
            {
                Vector3 dir = (inner[k] - outer[k]).normalized;
                a[k] = outer[k];
                b[k] = outer[k] + dir * 0.7f;
            }
            Ribbon(parent, $"KerbOuter_{i:000}", a, b, i, to, Vector3.zero, Vector3.zero,
                   alt ? ColKerbAlt : ColKerb, collider: false, lift: 0.02f);

            for (int k = i; k <= to; k++)
            {
                Vector3 dir = (outer[k] - inner[k]).normalized;
                a[k] = inner[k] + dir * 0.7f;
                b[k] = inner[k];
            }
            Ribbon(parent, $"KerbInner_{i:000}", a, b, i, to, Vector3.zero, Vector3.zero,
                   alt ? ColKerbAlt : ColKerb, collider: false, lift: 0.02f);
        }
    }

    /// <summary>
    /// 결승선. 흰 띠 하나였는데 체커 무늬로 바꿨다 —
    /// 레이싱 게임에서 "여기가 결승선" 을 말 없이 알려주는 건 이 무늬뿐이다.
    /// </summary>
    void BuildStartLine()
    {
        var root = new GameObject("StartLine").transform;
        root.SetParent(built, false);

        Vector3 centre = transform.position + PointOnPath(0f) + Vector3.up * 0.01f;
        Vector3 side = Vector3.Cross(Vector3.up, TangentOnPath(0f));
        float width = WidthOnPath(0f);

        const int columns = 14;
        const int rows = 2;
        float cell = width / columns;

        for (int c = 0; c < columns; c++)
            for (int r = 0; r < rows; r++)
            {
                // 체커는 가로·세로 합이 홀수인 칸만 어둡게 칠하면 된다
                if ((c + r) % 2 == 0) continue;

                Vector3 p = centre
                          + side * ((c + 0.5f) * cell - width * 0.5f)
                          + TangentOnPath(0f) * ((r + 0.5f) * cell - rows * cell * 0.5f);

                // 칸을 <b>딱 맞추지 않고 3% 줄인다.</b> 딱 맞추면 바깥쪽 칸의 옆면이 바탕의
                // 옆면과 같은 평면이 되어 그 선이 번쩍거린다. 줄여두면 칸 사이에 크림색 줄눈도
                // 생겨서 체커가 한 덩어리로 안 뭉친다.
                Block(root, $"Check_{c}_{r}", p, StartRotation,
                      new Vector3(cell * 0.94f, 0.04f, cell * 0.94f), ColLineDark, noCollider: true);
            }

        // 바탕 — 어두운 칸이 이 위에 얹힌다. <b>트랙 폭을 넘기면 안 된다</b>:
        // 한 번 넓혀봤더니 바탕 끝면이 벽 면과 같은 평면이 되어 거기가 번쩍거렸다.
        Block(root, "Base", centre + Vector3.down * 0.005f, StartRotation,
              new Vector3(width, 0.04f, rows * cell), ColLine, noCollider: true);
    }


    // ------------------------------------------------------------------
    //  코스 장식 — 기획서 §4.4
    // ------------------------------------------------------------------
    /// <summary>
    /// 골든베어 리조트 광고판. <b>기획서 §4.4 가 시킨 유일한 '튀는 색'</b>인데 트랙에 하나도 없었다.
    ///
    /// 박물관 팔레트(크림·남색·나무)와 <b>일부러 부딪히는</b> 금색 + 자홍색이다. 색이 튀는 게
    /// 실수가 아니라 연출이야 — 이 캠퍼스에 어울리지 않는 것이 들어와 있다는 걸 색으로 말한다.
    /// 그래서 색은 <see cref="Cast"/> 의 개발업자·시의원 색을 그대로 가져온다. 악당이 입은 색을
    /// 광고판이 똑같이 입고 있으면, 나중에 이야기에서 둘을 연결할 때 설명이 필요 없다.
    ///
    /// 길 <b>바깥</b>에 세운다. 콜라이더가 없어서 카트가 스쳐도 걸리지 않는다.
    /// </summary>
    void BuildAdBoards(Transform parent)
    {
        var root = new GameObject("AdBoards").transform;
        root.SetParent(parent, false);

        // (t, 어느 쪽 길가인지) — 코너 바깥이라 달리면서 정면으로 보게 되는 자리들
        (float t, float side)[] spots =
        {
            (0.04f,  1f),   // 결승 직선 — 제일 오래 보인다
            (0.18f, -1f),
            (0.35f,  1f),
            (0.52f, -1f),   // 정문앞 — 한옥 정문 옆이라 제일 안 어울린다
            (0.71f,  1f),
            (0.93f, -1f),
        };

        for (int i = 0; i < spots.Length; i++)
        {
            var (t, side) = spots[i];
            Vector3 forward = TangentOnPath(t);
            Vector3 across = Vector3.Cross(Vector3.up, forward) * side;
            Vector3 at = transform.position + PointOnPath(t) + across * (WidthOnPath(t) * 0.5f + 3.2f);
            var facing = Quaternion.LookRotation(-across, Vector3.up);

            var board = new GameObject($"Ad_{i}").transform;
            board.SetParent(root, false);
            board.SetPositionAndRotation(at, facing);

            // 기둥 둘
            for (int s = -1; s <= 1; s += 2)
                Block(board, $"Post_{s}", at + facing * new Vector3(s * 2.4f, 1.6f, 0f), facing,
                      new Vector3(0.32f, 3.2f, 0.32f), ColAdFrame, noCollider: true);

            // 판 — 금색 바탕에 자홍색 띠. 둘 다 악당 색이다
            Block(board, "Panel", at + Vector3.up * 4.1f, facing,
                  new Vector3(6.4f, 3.0f, 0.22f), ColAdGold, noCollider: true);
            // 폭을 판(6.4)보다 <b>조금 좁게</b> 잡는다. 딱 맞추면 좌우 옆면이 판의 옆면과 같은
            // 평면이 되어 그 모서리가 금색·자홍색으로 번쩍거린다 — 벽 경고 띠와 같은 병이야.
            // 포스터. 2026-09-30 에 받은 그림이 <b>1832 × 859 = 비율 2.133</b> 인데
            // 판(6.4 × 3.0)이 정확히 같은 비율이라 <b>판을 꽉 채운다</b> — 늘어나지도, 여백도 없다.
            // ★ 간판의 앞면은 <b>+z</b> 다. at 이 코스 <b>바깥</b>으로 밀린 자리고
            // facing 은 −across(=코스 쪽)를 보니까, facing 의 +z 가 곧 코스 쪽이다.
            // 2026-09-30 까지 −0.14 로 두어 <b>포스터가 판 뒤에 숨어 있었다</b> —
            // 트랙에서는 «그림이 없다», 캠퍼스에서는 «뒤에서 본 거울상» 으로 보였다.
            Poster(board, "Poster", at + Vector3.up * 4.1f + facing * new Vector3(0f, 0f, 0.14f),
                   facing, new Vector2(6.1f, 2.86f), "GoldenBear_01");

            // 자홍 띠는 뺐다 — 포스터가 판을 덮어서 어디에 둬도 겹친다.
            // 기획서 §4.4 의 «금색 + 자홍» 은 포스터 자체가 이미 입고 있다.
            Block(board, "Frame", at + Vector3.up * 5.7f, facing,
                  new Vector3(6.9f, 0.34f, 0.34f), ColAdFrame, noCollider: true);

            // 글씨 대신 곰 실루엣 한 덩어리 — 멀리서도 "저 회사" 로 읽히게
            Block(board, "Mark", at + Vector3.up * 4.4f + facing * Vector3.forward * -0.2f, facing,
                  new Vector3(1.5f, 1.5f, 0.1f), ColAdMagenta, noCollider: true);

            // ★ 이 판은 <b>레이싱을 다 끝내면 내려간다</b>(<see cref="CampusVictory"/>).
            // <c>isStatic</c> 을 달아 두면 구워서 저장하는 씬(캠퍼스)에서 <b>정적 배칭에
            // 합쳐져</b> 트랜스폼을 옮겨도 그려지는 자리가 안 바뀐다 — 문을 여섯 번 고치고서야
            // 잡은 그 함정이야(2026-09-18). 판 여섯 개 × 다섯 조각이라 드로우콜도 무시할 만하다.
            foreach (var piece in board.GetComponentsInChildren<Transform>(true))
                piece.gameObject.isStatic = false;
        }
    }

    /// <summary>
    /// 부술 수 있는 골든베어 입간판. 큰 광고판은 길 <b>바깥</b>이라 못 닿으니까
    /// 코스 <b>안쪽 갓길</b>에 작은 걸 따로 세운다.
    ///
    /// 자리는 발판과 같은 사고방식이야 — <b>갓길에 두면 레이싱 라인을 포기해야 닿는다.</b>
    /// 한가운데 두면 그냥 지나가다 부숴져서 고를 게 없어진다. 좌우로 번갈아 둬서
    /// 전부 부수려면 코스를 지그재그로 돌게 된다.
    ///
    /// <b>한 번 부수면 그 판이 끝날 때까지 그대로 부서져 있다.</b> 바퀴마다 되살리면
    /// 발판전부 임무와 똑같아지고, 세 바퀴에 나눠 챙길 여유도 없어진다.
    /// </summary>
    static readonly (float t, float lane)[] AdSigns =
    {
        // 발판 자리(0.11 · 0.26 · 0.45 · 0.63 · 0.88)와 겹치지 않게 사이사이에 둔다
        (0.04f, -0.72f),   // 결승 직선 — 첫 판에 뭘 하는 건지 바로 보인다
        (0.17f,  0.74f),
        (0.33f, -0.74f),
        (0.39f,  0.72f),
        (0.52f, -0.72f),   // 정문앞 — 한옥 정문 옆이라 제일 안 어울린다
        (0.58f,  0.74f),
        (0.70f, -0.74f),
        (0.81f,  0.72f),
    };

    /// <summary>
    /// 길에 널린 철거 자재. <b>장애물 임무에서만</b> 쓰고 평소엔 꺼져 있다
    /// (<see cref="DebrisGate"/>). 드럼통과 파이프 — 치면 날아간다.
    ///
    /// 자리는 <b>코스 한가운데를 비켜</b> 놓는다. 한가운데 놓으면 피할 길이 하나뿐이라
    /// 외우기 게임이 되고, 가장자리에만 놓으면 그냥 가운데로 달리면 된다.
    /// 왼쪽·오른쪽·가운데를 섞어서 <b>매번 다른 쪽으로 틀게</b> 만든다.
    /// </summary>
    static readonly (float t, float lane)[] Debris =
    {
        (0.07f, -0.35f), (0.09f,  0.40f),
        (0.20f,  0.30f), (0.23f, -0.45f),
        (0.31f,  0.00f),
        (0.42f, -0.40f), (0.44f,  0.35f),
        (0.55f,  0.25f),
        (0.66f, -0.30f), (0.68f,  0.40f),
        (0.79f,  0.00f),
        (0.86f, -0.35f), (0.91f,  0.30f),
    };

    void BuildDebris(Transform parent)
    {
        var root = new GameObject("RoadDebris").transform;
        root.SetParent(parent, false);

        for (int i = 0; i < Debris.Length; i++)
        {
            var (t, lane) = Debris[i];

            Vector3 forward = TangentOnPath(t);
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            Vector3 at = transform.position + PointOnPath(t) + side * (lane * WidthOnPath(t) * 0.5f);

            bool drum = i % 3 != 2;
            var go = new GameObject(drum ? $"Drum_{i}" : $"Pipe_{i}");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(at + Vector3.up * (drum ? 0.45f : 0.2f),
                                                Quaternion.LookRotation(forward, Vector3.up)
                                                * Quaternion.Euler(0f, i * 37f, drum ? 0f : 90f));

            var box = go.AddComponent<BoxCollider>();
            box.size = drum ? new Vector3(0.8f, 0.9f, 0.8f) : new Vector3(0.4f, 0.4f, 2.4f);

            go.AddComponent<RoadDebris>();

            // 그림은 자식으로. 콜라이더는 위에 하나만 둔다.
            var art = GameObject.CreatePrimitive(drum ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            art.name = "Art";
            art.transform.SetParent(go.transform, false);
            art.transform.localScale = drum ? new Vector3(0.8f, 0.45f, 0.8f)
                                            : new Vector3(0.4f, 0.4f, 2.4f);
            art.GetComponent<Renderer>().sharedMaterial =
                FlatMaterial.Get(drum ? ColKerb : ColPostCap);
            Discard(art.GetComponent<Collider>());
        }
    }

    /// <summary>
    /// <b>코스에 흩어진 곰인형.</b> 창고에서 굴러 나온 전시품이고, 실어서 결승선까지 나른다.
    ///
    /// 자리는 발판·광고판과 같은 사고방식 — <b>갓길이라 레이싱 라인을 포기해야 닿는다.</b>
    /// 좌우로 번갈아 둬서 다 실으려면 지그재그로 돈다. 대신 광고판과 달리
    /// <b>목표치만 넘기면 되니까</b> 몇 개는 버려도 된다 — 그게 이 판이 마음 편한 이유야.
    /// </summary>
    static readonly (float t, float lane)[] Cargo =
    {
        (0.07f, -0.62f), (0.15f,  0.58f),
        (0.24f,  0.60f), (0.33f, -0.58f),
        (0.47f, -0.60f), (0.58f,  0.62f),
        (0.72f,  0.58f), (0.84f, -0.60f),
    };

    /// <summary>구 하나. 곰인형처럼 둥근 걸 만들 때 쓴다 — 콜라이더는 안 단다.</summary>
    void Ball(Transform parent, string name, Vector3 localPosition, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Discard(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color);
    }

    void BuildCargo(Transform parent)
    {
        var root = new GameObject("Cargo").transform;
        root.SetParent(parent, false);

        for (int i = 0; i < Cargo.Length; i++)
        {
            var (t, lane) = Cargo[i];

            Vector3 forward = TangentOnPath(t);
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            Vector3 at = transform.position + PointOnPath(t) + side * (lane * WidthOnPath(t) * 0.5f);

            var go = new GameObject($"Bear_{i}");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(at + Vector3.up * 0.1f,
                                                Quaternion.LookRotation(-forward, Vector3.up));

            go.AddComponent<ExhibitCargo>();

            // 곰인형 — 몸 · 머리 · 귀 둘 · 리본. 로비 곰 조각상과 같은 색을 쓴다.
            Ball(go.transform, "Body", new Vector3(0f, 0.34f, 0f),
                 new Vector3(0.62f, 0.58f, 0.62f), ColBearFur);
            Ball(go.transform, "Head", new Vector3(0f, 0.78f, 0.02f),
                 new Vector3(0.5f, 0.48f, 0.5f), ColBearFur);
            Ball(go.transform, "Muzzle", new Vector3(0f, 0.72f, 0.2f),
                 new Vector3(0.24f, 0.2f, 0.2f), ColBearFace);

            for (int s = -1; s <= 1; s += 2)
            {
                Ball(go.transform, $"Ear_{s}", new Vector3(s * 0.19f, 0.99f, 0f),
                     new Vector3(0.2f, 0.2f, 0.12f), ColBearFur);
                Ball(go.transform, $"Arm_{s}", new Vector3(s * 0.33f, 0.4f, 0.04f),
                     new Vector3(0.22f, 0.3f, 0.22f), ColBearFur);
            }

            Ball(go.transform, "Ribbon", new Vector3(0f, 0.6f, 0.16f),
                 new Vector3(0.3f, 0.12f, 0.16f), ColKerb);
        }
    }

    void BuildAdSigns(Transform parent)
    {
        var root = new GameObject("AdSigns").transform;
        root.SetParent(parent, false);

        // <b>임무를 깨서 이미 부순 뒤라면 안 세운다.</b> 유저: "임무에서 다 없애면
        // 그 뒤로는 안 나와야지, 이미 부숴서 없다는 설정이야."
        // 상품(수집품)이 곧 기록이라 따로 저장할 게 없다 — 목록 7번째가 광고판 임무의 상품.
        //
        // ★★ 2026-10-07 — <b>굽는 중에는 건너뛰면 안 된다.</b> 캠퍼스 씬은 한 번 구워서
        // 저장하는 씬이라, 굽는 사람의 <b>수집 기록이 그대로 씬에 박힌다.</b> 수연이 7번째
        // 상품을 받은 상태에서 캠퍼스를 다시 구웠더니 <b>입간판 여덟이 통째로 빠진 채로</b>
        // 저장됐다 — 그러면 새로 받은 사람의 캠퍼스에도 영영 안 선다.
        //
        // 트랙 씬은 Awake 에 지으니 그때의 기록이 맞는 기록이고, 캠퍼스는 아니다.
        // 켜고 끄는 건 <see cref="AdSignGate"/> 와 <see cref="AdBoard"/> 가 런타임에 한다 —
        // 「물건이 스스로 판단할 수 있게 만들어라」가 여기서도 답이야.
        if (Application.isPlaying && AdSignsCleared) return;

        for (int i = 0; i < AdSigns.Length; i++)
        {
            var (t, lane) = AdSigns[i];

            Vector3 forward = TangentOnPath(t);
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            Vector3 at = transform.position + PointOnPath(t) + side * (lane * WidthOnPath(t) * 0.5f);
            var facing = Quaternion.LookRotation(-side * Mathf.Sign(lane), Vector3.up);

            var sign = new GameObject($"AdSign_{i}");
            sign.transform.SetParent(root, false);
            sign.transform.SetPositionAndRotation(at, facing);

            // 뚫고 지나가는 느낌이어야 하니 트리거. 충돌체면 벽 부딪힘으로 세지고 카트가 튕긴다.
            var box = sign.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.4f, 3.2f, 1.2f);
            box.center = new Vector3(0f, 1.6f, 0f);

            var ad = sign.AddComponent<AdBoard>();
            ad.pieceColor = ColAdGold;

            // 부서질 때 통째로 끌 수 있게 그림은 자식 하나에 모아둔다
            var art = new GameObject("Visual").transform;
            art.SetParent(sign.transform, false);
            ad.visual = art;

            // 2026-09-17 유저: "나중에 포스터 PNG 를 붙일 건데 너무 작아서 안 보인다."
            // 판을 1.3 → <b>2.2</b> 로. 포스터를 붙일 자리는 넉넉해야 그림이 산다.
            // 대신 갓길에서 벽까지 여유가 0.64m 뿐이라 자리를 안쪽(0.72)으로 당겼다.
            Block(art, "Leg_L", at + facing * new Vector3(-0.8f, 0.45f, 0f), facing,
                  new Vector3(0.14f, 0.9f, 0.14f), ColAdFrame, noCollider: true);
            Block(art, "Leg_R", at + facing * new Vector3(0.8f, 0.45f, 0f), facing,
                  new Vector3(0.14f, 0.9f, 0.14f), ColAdFrame, noCollider: true);

            Block(art, "Panel", at + facing * new Vector3(0f, 2f, 0f), facing,
                  new Vector3(2.2f, 2.2f, 0.09f), ColAdGold, noCollider: true);
            // 포스터가 <b>코스 쪽(+z)</b>으로 나와야 보인다. 간판의 +z 가 코스 가운데를 향한다.
            // 2026-09-17 에 판을 1.3 → 2.2 로 넓힌 게 <b>바로 이걸 붙이려고</b>였다.
            // 그림이 1254 × 1254 <b>정사각</b>이라 판(2.2)을 거의 꽉 채운다.
            // 자홍 띠는 뺐다 — 포스터가 그 자리를 덮는다.
            Poster(art, "Poster", at + facing * new Vector3(0f, 2f, 0.07f), facing,
                   new Vector2(2.0f, 2.0f), "GoldenBear_02");
        }
    }

    /// <summary>
    /// 결승선 위를 가로지르는 아치와 직선 양옆 현수막.
    /// 결승선이 바닥 무늬뿐이면 지나갔는지도 모른다 — <b>머리 위로 뭔가 지나가야</b> 한 바퀴가 끝난 게 느껴진다.
    /// </summary>
    void BuildFinishArch(Transform parent)
    {
        var root = new GameObject("FinishArch").transform;
        root.SetParent(parent, false);

        Vector3 forward = TangentOnPath(0f);
        Vector3 across = Vector3.Cross(Vector3.up, forward);
        Vector3 centre = transform.position + PointOnPath(0f);
        var facing = Quaternion.LookRotation(forward, Vector3.up);
        float half = WidthOnPath(0f) * 0.5f + 1.4f;

        for (int s = -1; s <= 1; s += 2)
        {
            Block(root, $"ArchPost_{s}", centre + across * (s * half) + Vector3.up * 3.4f, facing,
                  new Vector3(0.55f, 6.8f, 0.55f), ColArchWood, noCollider: true);
            Block(root, $"ArchFoot_{s}", centre + across * (s * half) + Vector3.up * 0.25f, facing,
                  new Vector3(1.1f, 0.5f, 1.1f), ColPostCap, noCollider: true);
        }

        // 들보와 청기와 — 로비·전시실 지붕과 같은 재료라 같은 건물로 읽힌다
        Block(root, "ArchBeam", centre + Vector3.up * 6.9f, facing,
              new Vector3(half * 2f + 1.2f, 0.6f, 0.8f), ColArchWood, noCollider: true);
        Block(root, "ArchTile", centre + Vector3.up * 7.4f, facing,
              new Vector3(half * 2f + 2.4f, 0.4f, 1.6f), ColPostCap, noCollider: true);
        Block(root, "ArchSign", centre + Vector3.up * 6.0f, facing,
              new Vector3(half * 1.1f, 0.9f, 0.2f), ColLine, noCollider: true);

        // 직선 양옆 현수막 — 달리는 동안 옆으로 흘러가서 속도가 느껴진다
        for (int i = 0; i < 8; i++)
        {
            float t = Mathf.Repeat(0.955f + i * 0.012f, 1f);
            Vector3 f = TangentOnPath(t);
            Vector3 a = Vector3.Cross(Vector3.up, f);
            Vector3 p = transform.position + PointOnPath(t);
            var look = Quaternion.LookRotation(f, Vector3.up);
            float edge = WidthOnPath(t) * 0.5f + 1.1f;

            for (int s = -1; s <= 1; s += 2)
            {
                Block(root, $"Flagpole_{i}_{s}", p + a * (s * edge) + Vector3.up * 1.6f, look,
                      new Vector3(0.14f, 3.2f, 0.14f), ColArchWood, noCollider: true);
                Block(root, $"Flag_{i}_{s}", p + a * (s * edge) + Vector3.up * 2.7f, look,
                      new Vector3(0.12f, 1.1f, 1.0f), i % 2 == 0 ? ColKerb : ColPostCap, noCollider: true);
            }
        }
    }
    void BuildCheckpoints()
    {
        var holder = new GameObject("Checkpoints").transform;
        holder.SetParent(built, false);

        for (int i = 0; i < checkpointCount; i++)
        {
            float t = (float)i / checkpointCount;
            var go = new GameObject($"Checkpoint_{i}");
            go.transform.SetParent(holder, false);
            go.transform.SetPositionAndRotation(
                transform.position + PointOnPath(t) + Vector3.up * 2f,
                Quaternion.LookRotation(TangentOnPath(t), Vector3.up));

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(WidthOnPath(t), 5f, 3f);

            go.AddComponent<Checkpoint>().index = i;
        }
    }

    // ------------------------------------------------------------------
    //  길 계산 — 캣멀-롬 곡선 (조절점을 부드럽게 통과하는 곡선)
    // ------------------------------------------------------------------

    /// <summary>t 는 0~1. 0 이 결승선이고 한 바퀴 돌면 다시 0.</summary>
    public Vector3 PointOnPath(float t01)
    {
        int n = Path.Length;
        float scaled = Mathf.Repeat(t01, 1f) * n;
        int i = Mathf.FloorToInt(scaled);
        float f = scaled - i;

        return CatmullRom(Path[Wrap(i - 1, n)].position, Path[Wrap(i, n)].position,
                          Path[Wrap(i + 1, n)].position, Path[Wrap(i + 2, n)].position, f);
    }

    public Vector3 TangentOnPath(float t01)
    {
        const float step = 0.002f;
        Vector3 a = PointOnPath(t01 - step);
        Vector3 b = PointOnPath(t01 + step);
        Vector3 d = b - a;
        return d.sqrMagnitude < 1e-6f ? Vector3.forward : d.normalized;
    }

    public float WidthOnPath(float t01)
    {
        int n = Path.Length;
        float scaled = Mathf.Repeat(t01, 1f) * n;
        int i = Mathf.FloorToInt(scaled);
        float f = scaled - i;
        // 폭은 구역 경계에서 부드럽게 넘어가게
        return Mathf.Lerp(Path[Wrap(i, n)].width, Path[Wrap(i + 1, n)].width, Mathf.SmoothStep(0f, 1f, f));
    }

    public int ZoneOnPath(float t01)
    {
        int n = Path.Length;
        int i = Mathf.FloorToInt(Mathf.Repeat(t01, 1f) * n);
        return (int)Path[Wrap(i, n)].zone;
    }

    float MeasureLength()
    {
        float total = 0f;
        const int samples = 400;
        Vector3 previous = PointOnPath(0f);
        for (int i = 1; i <= samples; i++)
        {
            Vector3 current = PointOnPath((float)i / samples);
            total += Vector3.Distance(previous, current);
            previous = current;
        }
        return total;
    }

    static int Wrap(int i, int n) => ((i % n) + n) % n;

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1)
                     + (-p0 + p2) * t
                     + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                     + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    // ------------------------------------------------------------------
    static readonly Dictionary<string, Material> posterMats = new();

    /// <summary>
    /// 그림이 붙은 얇은 판. 골든베어 광고에 쓴다.
    ///
    /// 왜 <see cref="Block"/> 위에 따로 얹는가 — 광고판 몸통은 금색 <b>단색</b>이라
    /// 그 머티리얼은 <see cref="FlatMaterial"/> 이 <b>여러 물건과 나눠 쓴다.</b>
    /// 거기에 텍스처를 꽂으면 담장·석등까지 골든베어 포스터가 된다.
    ///
    /// ★★ <b>큐브를 쓰지 않는다.</b> 유니티 기본 큐브의 UV 가 면마다 어느 쪽으로 붙는지는
    /// 짐작할 수밖에 없고, 실제로 그림이 <b>좌우로 뒤집혀</b> 나왔다(2026-09-30 유저 신고).
    /// 대신 <b>쿼드를 직접 만들어 UV 를 내가 박는다</b> — 따라 그리기 화판에서 이미 쓴 방법이다.
    ///
    /// 로컬 +z 가 <b>보는 사람 쪽</b>이고, 그 사람에게 <b>화면 오른쪽은 로컬 −x</b> 다
    /// (유니티는 왼손 좌표계라 Cross(up, 시선) 이 오른쪽인데 시선이 −z 다).
    /// 그래서 u 는 −x 로, v 는 +y 로 자란다. 와인딩은 앞을 보는 쪽이 시계방향이라
    /// (3,2,1)(3,1,0) — 계산하면 법선이 정확히 +z 로 나온다.
    /// </summary>
    GameObject Poster(Transform parent, string name, Vector3 position, Quaternion rotation,
                      Vector2 size, string texture)
    {
        if (!posterMats.TryGetValue(texture, out var mat) || mat == null)
        {
            // ★★ 2026-10-07 — <b>그림을 못 찾으면 «흰 판» 이 된다.</b> 수연이 파일 이름을
            // `GoldenBear` → `GoldenBear_01` 로 바꾸자 코스의 간판 열넷이 전부 새하얗게 나왔다.
            // 흰 판은 <b>«칠하다 만 것»</b> 으로 보여서, 아예 안 붙은 것보다 훨씬 나쁘다.
            //
            // 그래서 둘을 같이 한다 — <b>이름을 몇 가지 더 찾아보고</b>, 그래도 없으면
            // <b>포스터를 아예 안 만든다</b>(밑의 금색 판이 그대로 보여서 «광고판» 으로 읽힌다).
            var tex = LoadAd(texture);
            if (tex == null)
            {
                Debug.LogWarning($"[광고] Resources/Ads/{texture} 을 못 찾았다 — 포스터 없이 금색 판만 세운다.");
                posterMats[texture] = null;
                return null;
            }

            mat = new Material(Surface.Lit()) { name = "Ad_" + texture };
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.22f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.22f);
            posterMats[texture] = mat;
        }
        if (mat == null) return null;

        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(position, rotation);
        go.AddComponent<MeshFilter>().sharedMesh = PosterQuad(size);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.isStatic = true;
        return go;
    }

    /// <summary>
    /// 광고 그림을 찾는다. <b>이름이 조금 바뀌어도 찾아낸다</b> —
    /// 2026-10-07 에 `GoldenBear` 가 `GoldenBear_01` 로 바뀌면서 간판이 통째로 하얘졌다.
    ///
    /// 찾는 순서: ① 적힌 이름 그대로 → ② 꼬리 숫자를 떼고 → ③ `_01`~`_04` 를 붙여서.
    /// <b>그래도 없으면 null</b> 이고, 부르는 쪽이 포스터를 안 만든다.
    /// </summary>
    static Texture2D LoadAd(string texture)
    {
        var tex = Resources.Load<Texture2D>("Ads/" + texture);
        if (tex != null) return tex;

        string stem = System.Text.RegularExpressions.Regex.Replace(texture, @"_(\d+|Sq)$", "");
        tex = Resources.Load<Texture2D>("Ads/" + stem);
        if (tex != null) return tex;

        for (int i = 1; i <= 4 && tex == null; i++)
            tex = Resources.Load<Texture2D>($"Ads/{stem}_{i:00}");
        return tex;
    }

    static readonly Dictionary<Vector2, Mesh> posterQuads = new();

    /// <summary>
    /// 포스터 한 장짜리 쿼드. 같은 크기는 한 번만 만든다.
    /// </summary>
    static Mesh PosterQuad(Vector2 size)
    {
        if (posterQuads.TryGetValue(size, out var got) && got != null) return got;

        float hx = size.x * 0.5f, hy = size.y * 0.5f;
        var mesh = new Mesh { name = $"Poster_{size.x:0.##}x{size.y:0.##}" };
        mesh.vertices = new[]
        {
            new Vector3( hx, -hy, 0f),   // 화면 왼쪽 아래
            new Vector3(-hx, -hy, 0f),   // 화면 오른쪽 아래
            new Vector3(-hx,  hy, 0f),   // 화면 오른쪽 위
            new Vector3( hx,  hy, 0f),   // 화면 왼쪽 위
        };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f),
                          new Vector2(1f, 1f), new Vector2(0f, 1f) };
        mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
        mesh.triangles = new[] { 3, 2, 1, 3, 1, 0 };
        mesh.RecalculateBounds();
        posterQuads[size] = mesh;
        return mesh;
    }

    GameObject Block(Transform parent, string name, Vector3 position, Quaternion rotation,
                     Vector3 scale, Color color, bool noCollider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color);
        go.isStatic = true;
        if (noCollider)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Discard(c);
        }
        return go;
    }

    void OnDrawGizmosSelected()
    {
        // 씬 뷰에서 코스 모양을 미리 볼 수 있게
        Gizmos.color = Color.yellow;
        Vector3 previous = transform.position + PointOnPath(0f);
        for (int i = 1; i <= 160; i++)
        {
            Vector3 current = transform.position + PointOnPath((float)i / 160f);
            Gizmos.DrawLine(previous, current);
            previous = current;
        }
    }
}
