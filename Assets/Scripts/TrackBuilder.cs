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

    /// <summary>순환 코스의 조절점. 순서대로 한 바퀴를 돈다.</summary>
    public static readonly ControlPoint[] Path =
    {
        new ControlPoint(  0f, -78f, 11.0f, Zone.본관앞),      // 출발 · 결승 (곰 본관 정면)
        new ControlPoint(-34f, -70f,  9.0f, Zone.본관앞),
        new ControlPoint(-62f, -46f,  8.0f, Zone.서편전시동),
        new ControlPoint(-50f, -14f,  7.5f, Zone.서편전시동),  // 안쪽으로 파고드는 S 자
        new ControlPoint(-66f,  18f,  7.5f, Zone.북서담장),
        new ControlPoint(-46f,  50f,  8.0f, Zone.북서담장),
        new ControlPoint(  0f,  72f, 10.0f, Zone.정문앞),      // 한옥 정문
        new ControlPoint( 44f,  54f,  8.0f, Zone.동편연못),
        new ControlPoint( 58f,  22f,  7.0f, Zone.동편연못),    // 연못과 돌다리, 좁다
        new ControlPoint( 48f, -10f,  7.0f, Zone.매표소굽이),
        new ControlPoint( 62f, -40f,  7.5f, Zone.매표소굽이),  // 다시 바깥으로 밀리는 S 자
        new ControlPoint( 32f, -72f,  9.0f, Zone.본관앞),
    };

    [Header("만들기")]
    [Tooltip("끄면 트랙이 아예 안 생긴다. 빈 맵으로 테스트하고 싶을 때")]
    public bool buildOnAwake = true;
    [Tooltip("조절점 하나당 몇 조각으로 쪼갤지. 높을수록 곡선이 부드럽지만 오브젝트가 늘어난다")]
    [Range(4, 24)] public int segmentsPerControl = 10;

    [Header("벽")]
    // 2.6m 짜리 벽은 카트(높이 0.73m)보다 세 배 넘게 높아서 터널처럼 보인다.
    // 1.2m 로 낮추면 넘어가진 않으면서 바깥 풍경이 보인다 — 실제 서킷 가드레일도 이 정도야.
    public float wallHeight = 1.2f;
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

    static readonly Color ColKerb = new Color32(0xC4, 0x45, 0x3E, 0xFF);
    static readonly Color ColLine = new Color32(0xF2, 0xF3, 0xEE, 0xFF);

    // 가속 발판 — 회색 아스팔트 위에서 멀리서도 튀어야 해서 팔레트 중 제일 센 색을 쓴다
    static readonly Color ColBoostPad   = new Color32(0x2E, 0x4C, 0x7A, 0xFF);
    static readonly Color ColBoostArrow = new Color32(0xFF, 0xD1, 0x3C, 0xFF);

    /// <summary>결승선 위치. 카트를 여기에 놓으면 된다 (지면에서 0.38m 띄운 높이).</summary>
    public Vector3 StartPosition => transform.position + PointOnPath(0f) + Vector3.up * 0.38f;
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
        (0.13f,  0.55f),   // 본관앞 직선 — 출발 직후 첫 가속
        (0.28f, -0.50f),   // 서편전시동 코너 탈출
        (0.47f,  0.00f),   // 정문앞 넓은 구간 — 여긴 한가운데라 누구나 먹는다
        (0.62f, -0.55f),   // 동편연못 안쪽 라인
        (0.86f,  0.50f),   // 매표소굽이 뒤 마지막 직선
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
                   ColKerb, collider: false, lift: 0.02f);

            for (int k = i; k <= to; k++)
            {
                Vector3 dir = (outer[k] - inner[k]).normalized;
                a[k] = inner[k] + dir * 0.7f;
                b[k] = inner[k];
            }
            Ribbon(parent, $"KerbInner_{i:000}", a, b, i, to, Vector3.zero, Vector3.zero,
                   ColKerb, collider: false, lift: 0.02f);
        }
    }

    void BuildStartLine()
    {
        Block(built, "StartLine", transform.position + PointOnPath(0f) + Vector3.up * 0.01f,
              StartRotation, new Vector3(WidthOnPath(0f), 0.04f, 1.4f), ColLine, noCollider: true);
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

/// <summary>
/// 런타임에 만드는 단색 머티리얼. 지금 렌더 파이프라인(URP/빌트인)에 맞는 셰이더를 골라준다.
/// 같은 색은 한 번만 만들어서 돌려 쓴다.
/// </summary>
public static class FlatMaterial
{
    static readonly System.Collections.Generic.Dictionary<Color, Material> cache = new();

    public static Material Get(Color color)
    {
        if (cache.TryGetValue(color, out var cached) && cached != null) return cached;

        Shader shader = null;
        if (GraphicsSettings.defaultRenderPipeline != null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        var mat = new Material(shader) { name = $"Flat_{ColorUtility.ToHtmlStringRGB(color)}" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.08f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        cache[color] = mat;
        return mat;
    }
}
