using UnityEngine;

/// <summary>
/// 화면 왼쪽 아래 <b>코스 지도</b>. 누가 앞이고 누가 뒤인지 한눈에 보인다.
///
/// 유저(2026-09-17): *"뒤에 어떤 차가 있는지 알려주는 지도. 이러면 너무 마리오카트 파쿠리 같나."*
/// <b>아니야.</b> 코스 지도는 1982년 폴 포지션부터 거의 모든 레이싱 게임에 있는 물건이고,
/// 특정 게임의 발명이 아니다. 오히려 이게 없으면 <b>뒤를 볼 방법이 아예 없어서</b>
/// 3인칭 백뷰에서 추월당하는 걸 알 수가 없다. 백미러를 그리는 것보다 싸고 잘 읽힌다.
///
/// 코스 선은 <b>한 번만 구워서 텍스처에 담는다.</b> 매 프레임 60개 선분을 IMGUI 로 그리면
/// 회전 때문에 비싸고 계단이 진다. 구워두면 그리는 건 텍스처 한 장 + 점 몇 개뿐이야.
/// </summary>
public class MiniMap : MonoBehaviour
{
    public TrackBuilder track;

    [Tooltip("구워낼 지도 한 변의 픽셀")]
    public int resolution = 160;

    public Texture2D Texture { get; private set; }

    Vector2 min, size;

    void Start()
    {
        if (track == null) track = FindFirstObjectByType<TrackBuilder>();
        if (track != null) Bake();
    }

    /// <summary>월드 위치를 지도 안의 0~1 좌표로. 지도가 없으면 (-1,-1).</summary>
    public Vector2 ToMap(Vector3 world)
    {
        if (Texture == null || size.x <= 0f) return new Vector2(-1f, -1f);
        return new Vector2((world.x - min.x) / size.x, (world.z - min.y) / size.y);
    }

    void Bake()
    {
        const int samples = 420;
        var points = new Vector2[samples];

        var lo = new Vector2(float.MaxValue, float.MaxValue);
        var hi = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < samples; i++)
        {
            Vector3 p = track.transform.position + track.PointOnPath(i / (float)samples);
            points[i] = new Vector2(p.x, p.z);
            lo = Vector2.Min(lo, points[i]);
            hi = Vector2.Max(hi, points[i]);
        }

        // 가장자리 여백 — 점이 테두리에 붙으면 잘려 보인다
        float pad = Mathf.Max(hi.x - lo.x, hi.y - lo.y) * 0.08f;
        lo -= Vector2.one * pad;
        hi += Vector2.one * pad;

        // 가로세로를 같은 비율로 — 다르면 코스가 찌그러져서 실제 모양과 안 맞는다
        float span = Mathf.Max(hi.x - lo.x, hi.y - lo.y);
        Vector2 centre = (lo + hi) * 0.5f;
        min = centre - Vector2.one * span * 0.5f;
        size = Vector2.one * span;

        int n = resolution;
        Texture = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

        var clear = new Color(0f, 0f, 0f, 0f);
        var pixels = new Color[n * n];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        // 코스는 두 겹으로 — 어두운 테두리 위에 밝은 선. 한 겹이면 배경에 묻힌다.
        var edge = new Color(0.29f, 0.23f, 0.17f, 0.85f);
        var road = new Color(0.94f, 0.90f, 0.82f, 0.95f);

        for (int i = 0; i < samples; i++)
        {
            Vector2 a = Norm(points[i]) * (n - 1);
            Vector2 b = Norm(points[(i + 1) % samples]) * (n - 1);
            Dot(pixels, n, a, b, 2.6f, edge);
        }
        for (int i = 0; i < samples; i++)
        {
            Vector2 a = Norm(points[i]) * (n - 1);
            Vector2 b = Norm(points[(i + 1) % samples]) * (n - 1);
            Dot(pixels, n, a, b, 1.2f, road);
        }

        Texture.SetPixels(pixels);
        Texture.Apply();
    }

    Vector2 Norm(Vector2 world) => new Vector2((world.x - min.x) / size.x, (world.y - min.y) / size.y);

    /// <summary>선분 하나를 굵기 r 로 찍는다. 점을 촘촘히 뿌리는 게 브레젠험보다 짧고 충분하다.</summary>
    static void Dot(Color[] pixels, int n, Vector2 a, Vector2 b, float r, Color color)
    {
        int steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(a, b) * 2f));
        for (int s = 0; s <= steps; s++)
        {
            Vector2 p = Vector2.Lerp(a, b, s / (float)steps);
            int x0 = Mathf.FloorToInt(p.x - r), x1 = Mathf.CeilToInt(p.x + r);
            int y0 = Mathf.FloorToInt(p.y - r), y1 = Mathf.CeilToInt(p.y + r);

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    if (x < 0 || y < 0 || x >= n || y >= n) continue;
                    if ((new Vector2(x, y) - p).sqrMagnitude > r * r) continue;
                    pixels[y * n + x] = color;
                }
        }
    }
}
