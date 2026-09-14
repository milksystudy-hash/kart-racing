using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 타원 트랙을 코드로 만들어낸다. 노면 · 벽 · 연석 · 결승선 · 체크포인트까지.
///
/// 예전 TestTrackBootstrap 과 다른 점: **트랙 지오메트리만** 만든다.
/// 카트 · 플레이어 · 카메라 · HUD 는 씬에 진짜 오브젝트로 들어 있어서
/// 하이어라키에서 보이고, 인스펙터로 만질 수 있고, 저장도 된다.
///
/// 이건 어디까지나 임시 트랙이야. 나중에 진짜 코스를 만들면 이 오브젝트를 꺼버리면 돼
/// (체크 해제만 해도 트랙이 안 생긴다).
/// </summary>
public class TrackBuilder : MonoBehaviour
{
    [Header("코스 모양")]
    public float radiusX = 52f;
    public float radiusZ = 32f;
    public float roadWidth = 13f;
    [Range(24, 200)] public int segments = 96;
    [Range(2, 24)] public int checkpointCount = 8;

    [Header("벽")]
    public float wallHeight = 1.2f;
    public float wallThickness = 0.6f;

    [Header("색")]
    public Color roadColor = new Color32(0x4A, 0x4E, 0x48, 0xFF);
    public Color wallColor = new Color32(0x9C, 0xC4, 0x89, 0xFF);
    public Color kerbColor = new Color32(0xC9, 0x8A, 0x78, 0xFF);
    public Color lineColor = new Color32(0xF2, 0xF3, 0xEE, 0xFF);

    [Tooltip("끄면 트랙이 아예 안 생긴다. 빈 맵으로 테스트하고 싶을 때")]
    public bool buildOnAwake = true;

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

        BuildRoad();
        BuildStartLine();
        BuildCheckpoints();
    }

    void BuildRoad()
    {
        var road = new GameObject("Road").transform;   road.SetParent(built, false);
        var walls = new GameObject("Walls").transform; walls.SetParent(built, false);

        for (int i = 0; i < segments; i++)
        {
            Vector3 p0 = PointOnTrack((float)i / segments);
            Vector3 p1 = PointOnTrack((float)(i + 1) / segments);
            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 dir = p1 - p0;
            float len = dir.magnitude;
            if (len < 0.001f) continue;

            Quaternion rot = Quaternion.LookRotation(dir / len, Vector3.up);
            Vector3 side = Vector3.Cross(Vector3.up, dir / len);
            float span = len * 1.06f;   // 살짝 겹치게 해서 이음새 틈을 없앤다

            // 노면 — 윗면이 정확히 y = 0
            Block(road, $"Road_{i:00}", mid + Vector3.down * 0.1f, rot,
                  new Vector3(roadWidth, 0.2f, span), roadColor);

            Block(walls, $"WallOut_{i:00}", mid + side * (roadWidth * 0.5f) + Vector3.up * (wallHeight * 0.5f),
                  rot, new Vector3(wallThickness, wallHeight, span), wallColor);
            Block(walls, $"WallIn_{i:00}", mid - side * (roadWidth * 0.5f) + Vector3.up * (wallHeight * 0.5f),
                  rot, new Vector3(wallThickness, wallHeight, span), wallColor);

            // 코너가 눈에 들어오게 8칸마다 연석
            if (i % 8 < 4)
            {
                Block(road, $"Kerb_{i:00}", mid + side * (roadWidth * 0.5f - 0.7f) + Vector3.up * 0.005f,
                      rot, new Vector3(0.8f, 0.06f, span), kerbColor, noCollider: true);
            }
        }
    }

    void BuildStartLine()
    {
        Block(built, "StartLine", PointOnTrack(0f) + Vector3.up * 0.01f,
              Quaternion.LookRotation(TangentOnTrack(0f), Vector3.up),
              new Vector3(roadWidth, 0.04f, 1.2f), lineColor, noCollider: true);
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
            go.transform.SetPositionAndRotation(PointOnTrack(t) + Vector3.up * 2f,
                                                Quaternion.LookRotation(TangentOnTrack(t), Vector3.up));

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(roadWidth, 5f, 3f);

            go.AddComponent<Checkpoint>().index = i;
        }
    }

    // ------------------------------------------------------------------

    /// <summary>t 는 0~1. 결승선이 t = 0 이고, 거기서 카트는 +Z 를 본다.</summary>
    public Vector3 PointOnTrack(float t01)
    {
        float a = t01 * Mathf.PI * 2f;
        return transform.position + new Vector3(Mathf.Cos(a) * radiusX, 0f, Mathf.Sin(a) * radiusZ);
    }

    public Vector3 TangentOnTrack(float t01)
    {
        float a = t01 * Mathf.PI * 2f;
        return new Vector3(-Mathf.Sin(a) * radiusX, 0f, Mathf.Cos(a) * radiusZ).normalized;
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
        if (noCollider)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }
        return go;
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
