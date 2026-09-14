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
        new ControlPoint(  0f, -78f, 18f, Zone.본관앞),       // 출발 · 결승 (곰 본관 정면)
        new ControlPoint(-34f, -70f, 14f, Zone.본관앞),
        new ControlPoint(-62f, -46f, 12f, Zone.서편전시동),
        new ControlPoint(-50f, -14f, 11f, Zone.서편전시동),   // 안쪽으로 파고드는 S 자
        new ControlPoint(-66f,  18f, 11f, Zone.북서담장),
        new ControlPoint(-46f,  50f, 12f, Zone.북서담장),
        new ControlPoint(  0f,  72f, 16f, Zone.정문앞),       // 한옥 정문
        new ControlPoint( 44f,  54f, 12f, Zone.동편연못),
        new ControlPoint( 58f,  22f, 10f, Zone.동편연못),     // 연못과 돌다리, 좁다
        new ControlPoint( 48f, -10f, 10f, Zone.매표소굽이),
        new ControlPoint( 62f, -40f, 11f, Zone.매표소굽이),   // 다시 바깥으로 밀리는 S 자
        new ControlPoint( 32f, -72f, 14f, Zone.본관앞),
    };

    [Header("만들기")]
    [Tooltip("끄면 트랙이 아예 안 생긴다. 빈 맵으로 테스트하고 싶을 때")]
    public bool buildOnAwake = true;
    [Tooltip("조절점 하나당 몇 조각으로 쪼갤지. 높을수록 곡선이 부드럽지만 오브젝트가 늘어난다")]
    [Range(4, 24)] public int segmentsPerControl = 10;

    [Header("벽")]
    public float wallHeight = 2.6f;
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

    public void Build()
    {
        if (built != null) Destroy(built.gameObject);

        built = new GameObject("~TrackGeometry").transform;
        built.SetParent(transform, false);

        BuildSurface();
        BuildStartLine();
        BuildCheckpoints();
    }

    // ------------------------------------------------------------------
    //  노면과 벽
    // ------------------------------------------------------------------
    void BuildSurface()
    {
        var road = new GameObject("Road").transform;   road.SetParent(built, false);
        var walls = new GameObject("Walls").transform; walls.SetParent(built, false);

        int total = Path.Length * segmentsPerControl;

        for (int i = 0; i < total; i++)
        {
            float t0 = (float)i / total;
            float t1 = (float)(i + 1) / total;

            Vector3 p0 = PointOnPath(t0);
            Vector3 p1 = PointOnPath(t1);
            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 dir = p1 - p0;
            float len = dir.magnitude;
            if (len < 0.001f) continue;

            Quaternion rot = Quaternion.LookRotation(dir / len, Vector3.up);
            Vector3 side = Vector3.Cross(Vector3.up, dir / len);
            float width = WidthOnPath(t0);
            int zone = ZoneOnPath(t0);
            float span = len * 1.08f;   // 살짝 겹치게 해서 이음새 틈을 없앤다

            // 노면 — 윗면이 정확히 y = 0
            Block(road, $"Road_{i:000}", mid + Vector3.down * 0.1f, rot,
                  new Vector3(width, 0.2f, span), ZoneFloor[zone]);

            // 양쪽 벽
            Block(walls, $"WallL_{i:000}", mid + side * (width * 0.5f) + Vector3.up * (wallHeight * 0.5f),
                  rot, new Vector3(wallThickness, wallHeight, span), ZoneWall[zone]);
            Block(walls, $"WallR_{i:000}", mid - side * (width * 0.5f) + Vector3.up * (wallHeight * 0.5f),
                  rot, new Vector3(wallThickness, wallHeight, span), ZoneWall[zone]);

            // 코너가 눈에 들어오게 연석을 띄엄띄엄
            if (i % 10 < 5)
            {
                Block(road, $"KerbL_{i:000}", mid + side * (width * 0.5f - 0.55f) + Vector3.up * 0.005f,
                      rot, new Vector3(0.7f, 0.06f, span), ColKerb, noCollider: true);
                Block(road, $"KerbR_{i:000}", mid - side * (width * 0.5f - 0.55f) + Vector3.up * 0.005f,
                      rot, new Vector3(0.7f, 0.06f, span), ColKerb, noCollider: true);
            }
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
            if (c != null) Destroy(c);
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
