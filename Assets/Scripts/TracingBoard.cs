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
    // ★ 종이 대비 <b>1.78:1 → 2.50:1</b>. 이 게임은 «남은 윤곽을 찾아서 덧그리는 것» 이라
    // 윤곽이 안 보이면 할 일이 안 보인다. 그렇다고 내 선(10.9:1)만큼 진하면 안 된다 —
    // 덧그린 자리와 아직 안 그린 자리가 구별이 안 되니까.
    static readonly Color32 Guide = new Color32(0xA6, 0x95, 0x75, 0xFF);   // 흐린 윤곽

    /// <summary>
    /// ★ <b>«초록이 되는 범위» 를 눈에 보이게 깐다.</b> 유저: *"검은선과 초록선 기준이 뭐여
    /// 자기 맘대로 같네."* 기준은 <see cref="Tracing.Tolerance"/> 하나로 분명했는데
    /// <b>화면에 안 보이니 규칙이 없는 것과 같았다.</b> 이 띠 안에 그으면 초록이다.
    /// </summary>
    static readonly Color32 Band = new Color32(0xE9, 0xE0, 0xCB, 0xFF);
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

    /// <summary>카메라와 화판 사이라서 잠깐 감춘 것들. <see cref="HideBlockers"/> 참고.</summary>
    readonly System.Collections.Generic.List<Renderer> hidden =
        new System.Collections.Generic.List<Renderer>();

    void OnDestroy()
    {
        // ★ <b>푸는 자리는 여기 한 군데.</b> 판이 어떻게 끝나든 지나간다 —
        // 안 되돌리면 캠퍼스에 <b>안 보이는 입간판</b>이 남는다.
        foreach (var r in hidden) if (r != null) r.enabled = true;
        hidden.Clear();

        if (cam != null) Destroy(cam.gameObject);
        if (previous != null) previous.enabled = true;

        // 화판을 돌려놓는다. 안 돌려주면 <b>캠퍼스를 돌아다닐 때도</b>
        // 내가 그린 그림이 이젤에 남는다 — 그건 게임이 끝났다는 걸 부정하는 그림이야.
        if (canvas != null) canvas.enabled = true;
        if (surface != null) Destroy(surface);
        if (sheetMesh != null) Destroy(sheetMesh);
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

    /// <summary>
    /// 화판이 놓인 평면을 잰다 — <c>origin</c>(왼아래 귀퉁이) · <c>across</c>(가로 전체) ·
    /// <c>up</c>(세로 전체) · <c>normal</c>(보는 사람 쪽).
    ///
    /// ★★★ <b><c>mesh.vertices</c> 를 쓰면 안 된다.</b> 임포트 설정의 <c>Read/Write Enabled</c>
    /// 가 꺼져 있으면(이 프로젝트의 기본값이다) <b>플레이 모드와 빌드에서 못 읽는다</b> —
    /// <c>«Not allowed to access vertices on mesh 'Canvas'»</c> 가 뜨고 <see cref="Bind"/> 가
    /// 실패해서 <b>카메라도 그림판도 안 만들어진다.</b> 그게 2026-09-30 의 «먹통» 이었다.
    ///
    /// ★ <b>에디터에서는 읽힌다.</b> 그래서 에디터로 돌린 자가점검은 «화판 찾음 True» 로
    /// 멀쩡히 통과했다 — <b>런타임 제약은 런타임에서만 드러난다.</b>
    /// <c>mesh.bounds</c> 는 Read/Write 와 상관없이 언제나 읽을 수 있다.
    /// </summary>
    bool Measure(Transform t, Mesh mesh)
    {
        Bounds lb = mesh.bounds;
        Vector3 size = lb.size;

        // 화판은 납작한 판이다 — <b>제일 얇은 로컬 축이 곧 두께</b>고, 나머지 둘이 판 면이다.
        int thin = size.x <= size.y && size.x <= size.z ? 0 : (size.y <= size.z ? 1 : 2);
        int i1 = (thin + 1) % 3, i2 = (thin + 2) % 3;
        if (size[i1] < 1e-4f || size[i2] < 1e-4f) return false;

        Vector3 e1 = t.TransformVector(Unit(i1) * size[i1]);
        Vector3 e2 = t.TransformVector(Unit(i2) * size[i2]);
        if (e1.sqrMagnitude < 1e-6f || e2.sqrMagnitude < 1e-6f) return false;

        normal = Vector3.Cross(e1, e2).normalized;

        // 법선이 <b>사람이 서 있는 쪽</b>을 보게 한다 — 모델 축이 어떻든 못 틀린다.
        // ★ 가로축보다 <b>먼저</b> 정한다. 가로를 법선에서 뽑으니 순서가 거꾸로면 또 뒤집힌다.
        Vector3 eye = Camera.main != null ? Camera.main.transform.position : t.position + Vector3.forward;
        if (Vector3.Dot(normal, eye - t.position) < 0f) normal = -normal;

        // 세로는 <b>월드의 위</b>를 평면에 눌러 붙인 것. 화판은 세워 둔 물건이다.
        up = Vector3.up - normal * Vector3.Dot(Vector3.up, normal);
        if (up.sqrMagnitude < 1e-4f) up = e2;            // 눕혀 놓은 화판이면 판 축을 쓴다
        up.Normalize();

        // ★★ <b>Cross(normal, up)</b> 이라야 화면에서 <b>오른쪽</b>이다.
        // 전에는 <c>Cross(up, normal)</c> 이라 가로가 왼쪽을 향했고, 그래서 도형이
        // <b>좌우로 뒤집힌 채</b> 그려졌다(음표 머리가 반대쪽에 있었다).
        // 커서는 같은 축을 쓰니 따라오긴 했지만, 보이는 그림이 거울상이면 그건 다른 도형이다.
        across = Vector3.Cross(normal, up).normalized;

        // 판이 차지하는 가로·세로. 두 모서리 벡터를 <b>각 축에 눌러 더한다</b> —
        // 판이 기울어 있어도 맞는다.
        float sizeU = Mathf.Abs(Vector3.Dot(e1, across)) + Mathf.Abs(Vector3.Dot(e2, across));
        float sizeV = Mathf.Abs(Vector3.Dot(e1, up)) + Mathf.Abs(Vector3.Dot(e2, up));
        if (sizeU < 1e-3f || sizeV < 1e-3f) return false;

        origin = t.TransformPoint(lb.center) - across * (sizeU * 0.5f) - up * (sizeV * 0.5f);
        across *= sizeU;
        up *= sizeV;
        return true;
    }

    static Vector3 Unit(int i) => i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;

    // ── 그림판 ────────────────────────────────────────────────────────────────

    void BuildSheet()
    {
        sheet = new Texture2D(Tex, Tex, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        pixels = new Color32[Tex * Tex];
        basePixels = new Color32[Tex * Tex];

        // ★★★ <b>화판 메시의 UV 를 쓰지 않는다.</b>
        //
        // 실제로 재 보니(2026-09-30) 이 FBX 는 <c>u</c> 가 로컬 −X, <c>v</c> 가 로컬 +Z 인데
        // 내가 바운즈에서 뽑은 across 는 Z, up 은 X 였다 — <b>u 와 v 가 서로 바뀌어</b>
        // 그림이 대각선으로 뒤집힌 채 나왔다. 커서와 선이 어긋나고, 판정은 커서 자리로 하니
        // <b>초록/검정도 제멋대로로 보였다</b>(유저 신고 둘이 같은 원인이었다).
        //
        // <c>mesh.uv</c> 는 <c>mesh.vertices</c> 와 마찬가지로 런타임에 못 읽는다.
        // 그러니 <b>내가 쿼드를 만들어</b> UV 를 직접 박는다 — 짐작할 것이 없어지고,
        // 유저가 이젤을 새로 뽑아 넣어도 안 틀린다.
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

        BuildSurface();
    }

    GameObject surface;

    /// <summary>
    /// 그림이 실제로 그려지는 면. <b>원래 화판 앞 4mm</b> 에 내 쿼드를 세우고
    /// 원래 화판은 잠깐 감춘다(<see cref="OnDestroy"/> 에서 되돌린다).
    ///
    /// UV 는 <c>origin</c>→(0,0) · <c>+across</c>→(1,0) · <c>+up</c>→(0,1) 로 <b>내가 박는다.</b>
    /// 그래서 레이가 맞은 (u,v) 가 곧 텍스처 자리이고, <b>커서 밑에 선이 나온다.</b>
    /// </summary>
    void BuildSurface()
    {
        surface = new GameObject("TracingSurface");
        surface.transform.SetParent(canvas.transform, false);

        Vector3 o = origin + normal * 0.004f;        // z-파이팅을 피할 만큼만 앞으로
        var mesh = new Mesh { name = "TracingSheet" };
        mesh.vertices = new[]
        {
            surface.transform.InverseTransformPoint(o),
            surface.transform.InverseTransformPoint(o + across),
            surface.transform.InverseTransformPoint(o + across + up),
            surface.transform.InverseTransformPoint(o + up),
        };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f),
                          new Vector2(1f, 1f), new Vector2(0f, 1f) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = surface.AddComponent<MeshRenderer>();
        mr.sharedMaterial = paint;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // 삼각형이 반대로 돌아 뒤통수가 보이면 <b>아무 것도 안 그려진다</b> — 그 자리에서 뒤집는다.
        if (Vector3.Dot(surface.transform.TransformDirection(mesh.normals[0]), normal) < 0f)
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };

        sheetMesh = mesh;
        canvas.enabled = false;                       // 원래 화판은 잠깐 감춘다
    }

    Mesh sheetMesh;

    /// <summary>이번 도형의 «흐린 윤곽» 을 깔고 종이로 덮는다.</summary>
    void ResetSheet()
    {
        for (int i = 0; i < basePixels.Length; i++) basePixels[i] = Paper;

        var shape = game.Shape;

        // ① 허용 범위를 먼저 넓게 깐다 — 판정과 <b>같은 값</b>에서 두께를 뽑는다.
        //    숫자를 손으로 적으면 Tolerance 를 손볼 때 띠만 옛날 폭으로 남는다.
        int band = Mathf.RoundToInt(Tracing.Tolerance * Tex);
        Vector2 prev = Tracing.At(shape, 0f);
        for (int i = 1; i <= 240; i++)
        {
            Vector2 now = Tracing.At(shape, i / 240f);
            Line(basePixels, prev, now, Band, band);
            prev = now;
        }

        // ② 그 위에 가는 윤곽선 — «어디를 따라가라» 는 이 선이 말한다.
        prev = Tracing.At(shape, 0f);
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

        HideBlockers(centre);
    }

    /// <summary>
    /// ★★ <b>카메라와 화판 사이에 있는 것을 잠깐 감춘다.</b>
    ///
    /// 2026-09-30 측정: 재주관의 <b>미니게임 입간판이 이젤 정면 0.35m</b> 에 서 있어서
    /// 화판(2.25m)을 통째로 가렸다 — 게임을 켜면 <b>갈색 벽만 가득 찼고</b>, 그게
    /// 「한 번도 못 해봤다」의 정체였다. 화판·카메라·점수는 전부 멀쩡했다.
    ///
    /// 입간판 자리를 손으로 옮기는 걸로 고치면 <b>유저가 소품을 하나 더 놓는 순간 또 가려진다</b>
    /// (이 프로젝트에서 «좌표로 찍지 마라» 를 로비 곰·건물 배치에서 이미 배웠다). 그래서 잰다.
    /// </summary>
    void HideBlockers(Vector3 centre)
    {
        if (cam == null) return;

        Vector3 eye = cam.transform.position, fwd = cam.transform.forward;

        // 0.25m 를 빼는 건 <b>이젤 자신을 남기려고</b>다. 틀·받침은 화판과 거의 같은 깊이라
        // 여유 없이 자르면 화판만 허공에 뜬 그림이 된다.
        float deep = Vector3.Dot(centre - eye, fwd) - 0.25f;
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);

        foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude,
                                                      FindObjectsSortMode.None))
        {
            if (r == null || r == canvas || !r.enabled) continue;
            if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;

            // 바운즈를 <b>보는 방향으로 눌러</b> 제일 가까운 면을 구한다.
            Vector3 e = r.bounds.extents;
            float half = Mathf.Abs(fwd.x) * e.x + Mathf.Abs(fwd.y) * e.y + Mathf.Abs(fwd.z) * e.z;
            if (Vector3.Dot(r.bounds.center - eye, fwd) - half >= deep) continue;

            r.enabled = false;
            hidden.Add(r);
        }

        if (hidden.Count > 0)
            Debug.Log($"[따라 그리기] 화판을 가리던 {hidden.Count}개를 잠깐 감췄다 " +
                      $"(제일 가까운 것부터: {hidden[0].name}).");
    }
}
