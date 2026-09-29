using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// <b>이젤 화판</b> — 재주관에 <b>이미 서 있는</b> 소품(<c>In_이젤</c> 안의 <c>Canvas</c> 면)에
/// 그림을 올리고, 마우스를 그 면 위의 좌표로 바꿔 준다.
///
/// 철곰관 반응훈련벽과 <b>같은 생각</b>이다 — 씬을 갈아타지 않고 <b>방 안의 물건을 게임판으로</b>
/// 쓴다. 게임을 위해 방을 한 벌 더 지으면 소품을 고칠 때마다 두 군데가 어긋난다.
///
/// ━━ ★★ 축을 짐작하지 않고 잰다 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
///
/// 이 프로젝트에서 «블렌더에서 이랬으니 유니티도 이럴 것» 이 <b>네 번</b> 틀렸다(곰이 눕고,
/// 스피커가 뒤돌고, 소품 앞면이 두 번). 그래서 트랜스폼의 축을 아예 안 쓴다 —
/// <b>화판 메시의 꼭짓점</b>에서 평면과 가로·세로를 직접 뽑는다.
///
/// - 법선: 꼭짓점 셋으로 만드는 <b>제일 넓은</b> 삼각형에서
/// - 세로: <b>월드의 위쪽</b>을 그 평면에 눌러 붙인 것 — 화판은 세워 둔 물건이니까
/// - 가로: 그 둘의 외적. 부호는 <b>카메라에서 봤을 때 오른쪽</b>이 되게 뒤집는다
///
/// ━━ ★ 도형의 y 는 <b>아래로</b> 간다 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
///
/// <see cref="Tracing.Shapes"/> 를 그려 보면 기와가 가운데서 <b>y 가 작아지고</b>(위로 솟고)
/// 음표 머리가 y 가 큰 쪽에 있다 — 그림 좌표계(y 아래)로 찍은 값이다. UV 의 v 는 <b>위로</b>
/// 가니까 <b>둘을 바꿔 줘야 한다.</b> 안 바꾸면 모든 도형이 <b>위아래로 뒤집혀</b> 뜨는데,
/// 그건 «판정이 이상하다» 로 보이지 «뒤집혔다» 로 안 보인다.
/// </summary>
public class TracingBoard : MonoBehaviour
{
    /// <summary>화판 그림의 한 변(픽셀). 512 면 0.002 폭의 선이 1픽셀이다.</summary>
    const int Tex = 512;

    /// <summary>윤곽선을 이만큼 굵게 찍는다(픽셀 반지름).</summary>
    const int GuideThick = 3;

    /// <summary>내가 그린 선의 굵기(픽셀 반지름).</summary>
    const int InkThick = 4;

    static readonly Color32 Paper = new Color32(0xF3, 0xEC, 0xDC, 0xFF);
    static readonly Color32 Guide = new Color32(0xC0, 0xB2, 0x97, 0xFF);   // 흐린 윤곽
    static readonly Color32 Ink = new Color32(0x3A, 0x2E, 0x24, 0xFF);   // 내 선
    static readonly Color32 Good = new Color32(0x5D, 0x7A, 0x4E, 0xFF);   // 윤곽 위에 얹힌 선
    static readonly Color32 Cursor = new Color32(0xC4, 0x45, 0x3E, 0xFF);   // 붓 끝

    TracingGame game;
    Renderer canvas;
    Material paint;
    Texture2D sheet;
    Color32[] pixels;
    Color32[] basePixels;
    bool dirty;

    // 화판 평면 — 왼쪽 아래 모서리와 가로·세로 벡터(길이가 곧 화판 크기)
    Vector3 origin, across, up, normal;
    bool bound;

    Camera cam, previous;
    int drawnUpto;          // 화면에 이미 찍은 점 개수
    int lastRound = -1;
    Vector2 tip = new Vector2(-1f, -1f);

    void Start()
    {
        game = GetComponent<TracingGame>();
        bound = Bind();
        if (game != null) game.BoardBound = bound;

        if (!bound)
        {
            Debug.LogWarning("[따라 그리기] 이젤 화판을 못 찾았다 — 재주관에 <b>In_이젤</b> 이 있고 "
                           + "그 안에 <b>Canvas</b> 면이 있어야 한다. 화면만으로 진행한다.");
            return;
        }

        BuildSheet();
        BuildCamera();
    }

    void OnDestroy()
    {
        if (cam != null) Destroy(cam.gameObject);
        if (previous != null) previous.enabled = true;

        // 화판을 원래 재질로 돌려놓는다. 안 돌려주면 <b>캠퍼스를 돌아다닐 때도</b>
        // 내가 그린 그림이 이젤에 남는다 — 그건 게임이 끝났다는 걸 부정하는 그림이야.
        if (canvas != null && original != null) canvas.sharedMaterial = original;
        if (paint != null) Destroy(paint);
        if (sheet != null) Destroy(sheet);
    }

    Material original;

    // ── 찾기와 재기 ───────────────────────────────────────────────────────────

    /// <summary>
    /// 이젤의 화판을 찾는다. <b>이름 둘로 찾는다</b> — 방에 세우는 이름(<c>In_이젤</c>)은
    /// <see cref="CampusBuilder"/> 가 정하니까 바뀔 수 있지만, <c>Canvas</c> 는 FBX 안의
    /// 이름이라 안 바뀐다. 안전훈련이 <c>Target_1</c> 로 거슬러 올라가는 것과 같은 방식.
    /// </summary>
    bool Bind()
    {
        Transform found = null;

        var easel = GameObject.Find("In_이젤");
        if (easel != null)
            foreach (var t in easel.GetComponentsInChildren<Transform>(true))
                if (Named(t.name)) { found = t; break; }

        if (found == null)
        {
            // 이젤 이름이 바뀌었을 때 — 씬 전체에서 <c>Canvas</c> 면을 찾는다.
            foreach (var mf in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if (Named(mf.name)) { found = mf.transform; break; }
        }
        if (found == null) return false;

        canvas = found.GetComponent<Renderer>();
        var mesh = found.GetComponent<MeshFilter>();
        if (canvas == null || mesh == null || mesh.sharedMesh == null) return false;

        return Measure(found, mesh.sharedMesh);
    }

    /// <summary>
    /// <c>Canvas</c> 인가. 묶음마다 부품 이름 앞에 파일 이름이 붙어 오기도 해서
    /// (<c>05_Grand_Records_Wall__Archive_Door_1</c>) <b><c>__</c> 뒤도 본다</b> —
    /// 2026-09-29 웅지관에서 이것 때문에 앞면을 여태 못 재고 있었다.
    /// </summary>
    static bool Named(string name)
    {
        int cut = name.LastIndexOf("__", System.StringComparison.Ordinal);
        string part = cut >= 0 ? name.Substring(cut + 2) : name;
        return part.StartsWith("Canvas", System.StringComparison.Ordinal);
    }

    bool Measure(Transform t, Mesh mesh)
    {
        var v = mesh.vertices;
        if (v.Length < 3) return false;

        var w = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++) w[i] = t.TransformPoint(v[i]);

        // 평면 안의 방향 둘 — 제일 먼 꼭짓점, 그리고 그 선에서 제일 벗어난 꼭짓점
        Vector3 a = Vector3.zero;
        float best = 0f;
        for (int i = 1; i < w.Length; i++)
        {
            Vector3 d = w[i] - w[0];
            if (d.sqrMagnitude > best) { best = d.sqrMagnitude; a = d; }
        }
        Vector3 b = Vector3.zero;
        best = 0f;
        for (int i = 1; i < w.Length; i++)
        {
            Vector3 d = w[i] - w[0];
            Vector3 off = d - Vector3.Project(d, a);
            if (off.sqrMagnitude > best) { best = off.sqrMagnitude; b = off; }
        }
        if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f) return false;

        normal = Vector3.Cross(a, b).normalized;

        // 세로는 <b>월드의 위</b>를 평면에 눌러 붙인 것. 화판은 세워 둔 물건이다.
        up = Vector3.up - normal * Vector3.Dot(Vector3.up, normal);
        if (up.sqrMagnitude < 1e-4f) up = b;            // 눕혀 놓은 화판이면 그냥 b 를 쓴다
        up.Normalize();
        across = Vector3.Cross(up, normal).normalized;

        // 법선이 <b>사람이 서 있는 쪽</b>을 보게 한다 — 모델 축이 어떻든 못 틀린다
        Vector3 eye = Camera.main != null ? Camera.main.transform.position : t.position + Vector3.forward;
        if (Vector3.Dot(normal, eye - t.position) < 0f) { normal = -normal; across = -across; }

        float loU = float.MaxValue, hiU = float.MinValue;
        float loV = float.MaxValue, hiV = float.MinValue;
        foreach (var p in w)
        {
            float u = Vector3.Dot(p - w[0], across), q = Vector3.Dot(p - w[0], up);
            loU = Mathf.Min(loU, u); hiU = Mathf.Max(hiU, u);
            loV = Mathf.Min(loV, q); hiV = Mathf.Max(hiV, q);
        }
        float sizeU = hiU - loU, sizeV = hiV - loV;
        if (sizeU < 0.05f || sizeV < 0.05f) return false;

        origin = w[0] + across * loU + up * loV;
        across *= sizeU;      // 이제 길이가 곧 화판 가로
        up *= sizeV;
        return true;
    }

    // ── 그림판 ────────────────────────────────────────────────────────────────

    void BuildSheet()
    {
        sheet = new Texture2D(Tex, Tex, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        pixels = new Color32[Tex * Tex];
        basePixels = new Color32[Tex * Tex];

        original = canvas.sharedMaterial;

        // ★ <b>빛을 안 받는 재질로 그린다.</b> 방 조명은 발광 재질이라 실제로는 아무것도
        // 안 비추는데(철곰관에서 겪은 것), 화판이 어두우면 <b>흐린 윤곽이 안 보여서</b>
        // 「뭘 따라 그리라는 건지 모르겠다」가 된다. 그림은 읽히는 게 전부다.
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        paint = unlit != null ? new Material(unlit)
              : original != null ? new Material(original)
              : new Material(Shader.Find("Universal Render Pipeline/Lit"));

        if (paint.HasProperty("_BaseMap")) paint.SetTexture("_BaseMap", sheet);
        if (paint.HasProperty("_BaseColor")) paint.SetColor("_BaseColor", Color.white);
        if (paint.HasProperty("_MainTex")) paint.SetTexture("_MainTex", sheet);
        canvas.sharedMaterial = paint;
    }

    /// <summary>이번 도형의 «흐린 윤곽» 을 깔고 종이로 덮는다.</summary>
    void ResetSheet()
    {
        for (int i = 0; i < basePixels.Length; i++) basePixels[i] = Paper;

        var shape = game.Shape;
        Vector2 prev = Tracing.At(shape, 0f);
        for (int i = 1; i <= 240; i++)
        {
            Vector2 now = Tracing.At(shape, i / 240f);
            Line(basePixels, prev, now, Guide, GuideThick);
            prev = now;
        }

        System.Array.Copy(basePixels, pixels, pixels.Length);
        drawnUpto = 0;
        dirty = true;
    }

    void Update()
    {
        if (!bound || game == null) return;

        // ★ 판이 끝나면 화판을 <b>지우지 않는다.</b> 마지막 회차가 끝나면 Round 가 4가 되는데,
        // 그때도 «회차가 바뀌었다» 로 보고 종이를 비우면 <b>결과 화면 뒤에서 내 그림이 사라진다</b> —
        // 방금 뭘 그렸는지 보면서 점수를 읽는 게 이 게임의 마지막 장면이야.
        bool over = game.Flow != null && game.Flow.Now == MinigameFlow.Step.끝;
        if (lastRound != game.Round && !over) { lastRound = game.Round; ResetSheet(); }

        Trace();
        Stamp();

        // ★ <b>붓 끝은 종이에 안 남는다.</b> 처음엔 커서도 <c>pixels</c> 에 찍었는데,
        // 그러면 마우스를 움직인 자리가 전부 빨갛게 남아서 <b>«그린 선» 과 구별이 안 됐다.</b>
        // 종이(<c>pixels</c>)는 «진짜 그린 것» 만 들고, 커서는 <b>텍스처에만</b> 얹는다.
        if (dirty)
        {
            sheet.SetPixels32(pixels);
            brushAt = -1;          // 종이를 통째로 다시 올렸으니 지울 자국도 없다
            dirty = false;
        }
        Brush();
        sheet.Apply(false);
    }

    // ── 붓 끝 ─────────────────────────────────────────────────────────────────

    const int BrushR = 4;
    const int BrushBox = BrushR * 2 + 1;
    readonly Color32[] brushBuf = new Color32[BrushBox * BrushBox];
    int brushAt = -1, brushX, brushY;

    /// <summary>지난 자국을 지우고 지금 자리에 찍는다. 작은 네모 둘이라 값이 거의 안 든다.</summary>
    void Brush()
    {
        if (brushAt >= 0) Blit(brushX, brushY, false);
        brushAt = -1;
        if (tip.x < 0f) return;

        int cx = Mathf.RoundToInt(tip.x * (Tex - 1));
        int cy = Mathf.RoundToInt((1f - tip.y) * (Tex - 1));
        brushX = Mathf.Clamp(cx - BrushR, 0, Tex - BrushBox);
        brushY = Mathf.Clamp(cy - BrushR, 0, Tex - BrushBox);
        Blit(brushX, brushY, true);
        brushAt = 1;
    }

    /// <summary><paramref name="mark"/> 면 붓 자국을, 아니면 <b>종이 원래 색</b>을 올린다.</summary>
    void Blit(int x0, int y0, bool mark)
    {
        for (int y = 0; y < BrushBox; y++)
            for (int x = 0; x < BrushBox; x++)
            {
                int dx = x - BrushR, dy = y - BrushR;
                int r2 = dx * dx + dy * dy;
                // 속이 빈 동그라미 — 꽉 찬 점은 <b>내가 그리는 선을 가린다</b>
                bool on = mark && r2 <= BrushR * BrushR && r2 >= (BrushR - 1) * (BrushR - 1);
                brushBuf[y * BrushBox + x] = on ? Cursor : pixels[(y0 + y) * Tex + x0 + x];
            }
        sheet.SetPixels32(x0, y0, BrushBox, BrushBox, brushBuf);
    }

    /// <summary>마우스를 화판 위의 한 점으로.</summary>
    void Trace()
    {
        var mouse = Mouse.current;
        if (mouse == null || cam == null) return;

        Vector2 screen = mouse.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screen);

        float denom = Vector3.Dot(ray.direction, normal);
        if (Mathf.Abs(denom) < 1e-5f) return;
        float hit = Vector3.Dot(origin - ray.origin, normal) / denom;
        if (hit <= 0f) return;

        Vector3 on = ray.GetPoint(hit) - origin;
        float u = Vector3.Dot(on, across) / across.sqrMagnitude;
        float v = Vector3.Dot(on, up) / up.sqrMagnitude;
        if (u < 0f || u > 1f || v < 0f || v > 1f) { tip = new Vector2(-1f, -1f); return; }

        // ★ 도형 좌표는 y 가 <b>아래로</b> 간다. UV 의 v 는 위로 간다. 여기서 뒤집는다.
        tip = new Vector2(u, 1f - v);

        if (mouse.leftButton.isPressed) game.Paint(tip);
    }

    /// <summary>새로 찍힌 점만 종이에 옮긴다. 매 프레임 전부 다시 그리면 512² 를 낭비한다.</summary>
    void Stamp()
    {
        var pts = game.Drawn;
        if (pts.Count == drawnUpto) return;

        // ★ 점이 <see cref="Tracing.MaxPoints"/> 를 넘으면 <b>앞에서 버려진다.</b> 그러면
        // 인덱스가 통째로 밀려서 «새로 찍힌 점» 만 이어 그리는 게 거짓말이 된다 —
        // 그때는 바닥부터 다시 그린다. 18초짜리 한 회차에서 거의 안 일어난다.
        if (pts.Count < drawnUpto || pts.Count >= Tracing.MaxPoints)
        {
            System.Array.Copy(basePixels, pixels, pixels.Length);
            drawnUpto = 0;
        }

        if (drawnUpto == 0 && pts.Count > 0) Dot(pixels, pts[0], Ink, InkThick);

        for (int i = Mathf.Max(1, drawnUpto); i < pts.Count; i++)
        {
            // 윤곽에 붙은 선은 <b>초록</b>으로 찍는다 — 점수식을 화면으로 설명하는 자리다.
            // 숫자로만 «붙음 0.72» 라고 하면 <b>어디가 틀렸는지 알 수가 없다.</b>
            var col = Tracing.Near(game.Shape, pts[i]) <= Tracing.Tolerance ? Good : Ink;
            Line(pixels, pts[i - 1], pts[i], col, InkThick);
        }

        drawnUpto = pts.Count;
        dirty = true;
    }

    // ── 픽셀 ──────────────────────────────────────────────────────────────────

    static void Dot(Color32[] buf, Vector2 at, Color32 c, int r)
    {
        int cx = Mathf.RoundToInt(at.x * (Tex - 1));
        int cy = Mathf.RoundToInt((1f - at.y) * (Tex - 1));   // 도형 y(아래) → 픽셀 행(위)
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            {
                if (x * x + y * y > r * r) continue;
                int px = cx + x, py = cy + y;
                if (px < 0 || px >= Tex || py < 0 || py >= Tex) continue;
                buf[py * Tex + px] = c;
            }
    }

    static void Line(Color32[] buf, Vector2 a, Vector2 b, Color32 c, int r)
    {
        float far = Vector2.Distance(a, b);
        int steps = Mathf.Max(1, Mathf.CeilToInt(far * Tex));
        for (int i = 0; i <= steps; i++)
            Dot(buf, Vector2.Lerp(a, b, i / (float)steps), c, r);
    }

    // ── 카메라 ────────────────────────────────────────────────────────────────

    void BuildCamera()
    {
        previous = Camera.main;
        if (previous == null) previous = FindFirstObjectByType<Camera>();

        var go = new GameObject("TracingCamera");
        cam = go.AddComponent<Camera>();

        // 배경색·안개·컬링 마스크를 물려받는다. 새로 만들면 하늘이 유니티 기본값으로
        // 돌아가서 이 화면만 딴 게임처럼 보인다(급식·안전훈련 카메라와 같은 이유).
        if (previous != null) cam.CopyFrom(previous);
        cam.fieldOfView = 40f;

        var extra = go.GetComponent<UniversalAdditionalCameraData>();
        if (extra == null) extra = go.AddComponent<UniversalAdditionalCameraData>();
        extra.renderPostProcessing = true;
        extra.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        // ★ 거리를 손으로 안 적는다. 화판 크기와 <b>지금 화면비</b>에서 거꾸로 구한다 —
        // 이젤을 새로 뽑아 화판이 커져도, 창을 납작하게 열어도 화판이 화면에 다 든다.
        float vTan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float hTan = vTan * Mathf.Max(0.5f, cam.aspect);
        const float margin = 0.12f;
        float need = Mathf.Max((up.magnitude * 0.5f + margin) / vTan,
                               (across.magnitude * 0.5f + margin) / hTan);

        Vector3 centre = origin + across * 0.5f + up * 0.5f;
        go.transform.position = centre + normal * Mathf.Clamp(need, 0.8f, 4.0f);
        go.transform.LookAt(centre, Vector3.up);

        // AudioListener 는 안 단다. 둘이면 경고가 뜬다(다른 두 게임에서 세운 규칙).
        if (previous != null) previous.enabled = false;
    }
}
