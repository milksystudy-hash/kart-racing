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
    /// <b>남쪽 변 네 점은 일부러 z 를 똑같이 맞췄다</b>(2026-09-16). 캣멀-롬 곡선은
    /// 한 점의 접선이 <b>양옆 점</b>으로 정해지기 때문에, 점 두 개만 나란히 놔서는 직선이 안 나온다.
    /// 네 점(13·0·1·2)을 한 줄에 놓아야 가운데 구간이 진짜로 곧게 뻗는다.
    ///
    /// 직선이 왜 필요했냐면 — 전에는 제일 긴 직선이 <b>10 m</b> 였고 25 m 넘는 직선이 하나도 없었다.
    /// 그러면 최고 속도를 쓸 일이 없고, 추월할 자리도 없고, 매 바퀴가 "계속 꺾기" 한 가지 리듬이 된다.
    /// 결승선을 이 직선 한가운데 둬서 <b>직선 → 브레이크 → 코너 → 가속</b> 리듬이 생기게 했다.
    /// </summary>
    public static readonly ControlPoint[] Path =
    {
        new ControlPoint(  0f, -78f, 12.0f, Zone.본관앞),      // 출발 · 결승 — 직선 한가운데
        new ControlPoint(-32f, -78f, 10.5f, Zone.본관앞),      // 직선
        new ControlPoint(-62f, -78f,  9.5f, Zone.서편전시동),  // 직선 끝, 브레이킹 존
        new ControlPoint(-78f, -52f,  8.0f, Zone.서편전시동),  // 첫 코너
        new ControlPoint(-68f, -16f,  7.5f, Zone.서편전시동),
        new ControlPoint(-54f,  16f,  7.5f, Zone.북서담장),    // 안쪽으로 파고드는 S 자
        new ControlPoint(-62f,  50f,  8.0f, Zone.북서담장),
        new ControlPoint(-24f,  74f,  9.5f, Zone.정문앞),      // 한옥 정문 앞
        new ControlPoint( 20f,  74f,  9.5f, Zone.정문앞),
        new ControlPoint( 56f,  54f,  8.0f, Zone.동편연못),
        new ControlPoint( 70f,  18f,  7.0f, Zone.동편연못),    // 연못과 돌다리, 좁다
        new ControlPoint( 62f, -22f,  7.5f, Zone.매표소굽이),
        new ControlPoint( 46f, -60f,  8.5f, Zone.매표소굽이),  // 직선으로 떨어지는 마지막 코너
        new ControlPoint( 30f, -78f, 10.0f, Zone.본관앞),      // 직선 진입
    };

    [Header("만들기")]
    [Tooltip("끄면 트랙이 아예 안 생긴다. 빈 맵으로 테스트하고 싶을 때")]
    public bool buildOnAwake = true;
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
    // 악당 광고 — 기획서 §4.4 대로 박물관 팔레트와 일부러 부딪히는 금색/자홍색.
    // Cast 의 개발업자·시의원 색과 같은 값이다.
    static readonly Color ColAdGold    = new Color32(0xC9, 0xA2, 0x27, 0xFF);
    static readonly Color ColAdMagenta = new Color32(0xB0, 0x40, 0x7F, 0xFF);
    static readonly Color ColAdFrame   = new Color32(0x3A, 0x36, 0x32, 0xFF);
    static readonly Color ColArchWood  = new Color32(0x6B, 0x4A, 0x33, 0xFF);

    static readonly Color ColLine     = new Color32(0xF2, 0xF3, 0xEE, 0xFF);
    static readonly Color ColLineDark = new Color32(0x2E, 0x2C, 0x2A, 0xFF);

    // 가속 발판 — 회색 아스팔트 위에서 멀리서도 튀어야 해서 팔레트 중 제일 센 색을 쓴다
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
        BuildBoostPads();
        BuildAdBoards(built);
        BuildFinishArch(built);
        BuildCheckpoints();
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
        BuildRailPosts(walls, outer, inner, zones, total);
    }

    /// <summary>
    /// 가드레일 기둥. 벽이 매끈한 띠 하나면 "코드로 뽑은 면" 으로 보인다 —
    /// 일정 간격으로 기둥이 서 있어야 사람이 세운 울타리로 읽히고, 달릴 때 속도감도 생긴다
    /// (지나가는 기둥이 눈에 박자를 만든다).
    /// </summary>
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

                Block(root, $"Check_{c}_{r}", p, StartRotation,
                      new Vector3(cell, 0.04f, cell), ColLineDark, noCollider: true);
            }

        // 바탕 — 어두운 칸이 이 위에 얹힌다
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
            Block(board, "Stripe", at + Vector3.up * 3.0f + facing * Vector3.forward * -0.14f, facing,
                  new Vector3(6.4f, 0.8f, 0.1f), ColAdMagenta, noCollider: true);
            Block(board, "Frame", at + Vector3.up * 5.7f, facing,
                  new Vector3(6.9f, 0.34f, 0.34f), ColAdFrame, noCollider: true);

            // 글씨 대신 곰 실루엣 한 덩어리 — 멀리서도 "저 회사" 로 읽히게
            Block(board, "Mark", at + Vector3.up * 4.4f + facing * Vector3.forward * -0.2f, facing,
                  new Vector3(1.5f, 1.5f, 0.1f), ColAdMagenta, noCollider: true);
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
