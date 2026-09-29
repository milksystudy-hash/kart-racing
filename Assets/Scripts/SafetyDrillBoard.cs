using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// <b>안전 점검 훈련 - 판이 도는 동안의 방.</b> 철곰관 반응훈련벽의 곰발 판 아홉 장
/// (<c>Target_1</c> ~ <c>Target_9</c>)을 찾아 불을 켜고 끈다.
/// 규칙은 <see cref="SafetyDrill"/>, 화면은 <see cref="SafetyDrillHUD"/>, 여기는 <b>3D</b> 만.
///
/// <b>새로 만드는 기하가 0개다</b>(기획서 §2). 유저가 만든 대형 소품 열 점 중
/// 04 반응훈련벽만 부품이 아홉 개로 갈려 있고, 그게 곧 게임판이다.
/// 여기서 얹는 것은 <b>판 위에 뜨는 불빛과 번호</b>뿐이야.
///
/// <b>씬에 저장되는 게 0이다.</b> 불빛도 카메라도 실행 중에 만들고 나갈 때 지운다.
///
/// ━━ 2026-09-29, 유저가 처음 해보고 준 것 세 줄 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
/// *"이러면 사람들이 뭔 번호인지 그런걸 어떻게 알아."* · *"초록색 네모 칸도 좀 보기 어려워"* ·
/// *"눌러도 점수에 변경이 없어."*
///
/// 셋이 <b>같은 병</b>이다 — <b>화면이 손에 아무 대답을 안 했다.</b> 보통 반응게임이 하는 것을
/// 하나도 안 하고 있었어:
/// <list type="number">
/// <item><b>키 이름을 과녁에 써 둔다.</b> 안 쓰면 «어디를 누르나» 가 게임의 내용이 돼 버린다 —
///       그건 반응속도 훈련이 아니라 자리 외우기다. 이제 판마다 가운데에 번호패가 박혀 있다.</item>
/// <item><b>켜짐은 «색» 이 아니라 «밝기와 크기» 로 말한다.</b> 벽이 청록이라 연한 초록 네모는
///       배경에 묻혔다. 판 <b>전체</b>가 동그랗게, 훨씬 밝게, 튀어나오듯 켜진다.</item>
/// <item><b>누른 것은 무조건 보여준다.</b> 꺼진 판을 눌러도 그 판이 한 번 깜빡이고,
///       맞추면 판 위에 <c>+2</c> 가 떠오른다. 점수판은 화면 구석이라 <b>벽을 보는 동안
///       아무도 안 본다.</b></item>
/// </list>
/// </summary>
public class SafetyDrillBoard : MonoBehaviour
{
    /// <summary>반응훈련벽. <see cref="CampusBuilder"/> 가 이 이름으로 세운다.</summary>
    const string WallName = "In_반응훈련벽";

    // ── 색 ────────────────────────────────────────────────────────────────────
    //
    // ★★ <b>.mat 에셋에 런타임으로 색을 쓰지 않는다</b>(기획서 §7). 디스크 파일이 바뀌어
    // 다른 씬까지 따라간다 - 판의 색은 <b>오브젝트를 켜고 끄기로만</b> 바꾼다.
    // <see cref="FlatMaterial"/> 은 런타임에 `new Material` 을 만들어 캐시할 뿐이라 안전하다.
    //
    // ★ <b>초록을 노랑 쪽으로 밀었다.</b> 철곰관 벽과 반응벽 몸통이 <b>청록(#4E7A70)</b> 이라
    // 순한 초록은 배경과 색상이 이웃이고 밝기도 비슷해서 «켜졌는지» 가 안 보인다
    // (2026-09-29 유저: "초록색 네모 칸도 좀 보기 어려워"). 색상도 밝기도 멀리 떼어 놓는다.

    static readonly Color ColGreen = new Color32(0x4C, 0xE8, 0x3A, 0xFF);   // 켜짐 — 밝은 연두
    static readonly Color ColRed   = new Color32(0xFF, 0x3A, 0x28, 0xFF);   // 건드리면 안 되는 것
    static readonly Color ColFlash = new Color32(0xFF, 0xF3, 0xC4, 0xFF);   // 맞게 눌렀을 때
    static readonly Color ColStop  = new Color32(0x2E, 0x2A, 0x24, 0xFF);   // 판이 멈춘 동안
    static readonly Color ColStray = new Color32(0x8C, 0x82, 0x76, 0xFF);   // 꺼진 판을 눌렀을 때
    static readonly Color ColEdge  = new Color32(0x24, 0x1A, 0x16, 0xFF);   // 테두리 — 벽과 떼어 놓는다
    static readonly Color ColBadge = new Color32(0xF6, 0xEC, 0xD6, 0xFF);   // 번호패 종이
    static readonly Color ColInk   = new Color32(0x24, 0x1A, 0x16, 0xFF);   // 번호 글자
    static readonly Color ColGain  = new Color32(0xFF, 0xE7, 0xA0, 0xFF);   // 떠오르는 «+2»

    /// <summary>맞게 누른 판이 번쩍이는 시간.</summary>
    const float FlashSeconds = 0.20f;

    /// <summary>꺼진 판을 눌렀을 때 깜빡이는 시간. <b>짧게</b> — 벌이 아니라 «눌렸다» 는 대답이야.</summary>
    const float StraySeconds = 0.13f;

    /// <summary>«+2» 가 떠올랐다 사라지는 시간.</summary>
    const float GainSeconds = 0.65f;

    /// <summary>빨강을 밟았을 때 화면이 흔들리는 시간.</summary>
    const float ShakeSeconds = 0.35f;
    const float ShakeAmount = 0.055f;

    /// <summary>판 앞에 얹는 원판 두께.</summary>
    const float DiscThick = 0.018f;

    SafetyDrill game;
    Transform wall;
    Transform rig;          // 불빛을 담는 통 - 나갈 때 이거 하나만 지우면 된다
    Camera cam;
    Camera previous;
    Vector3 camHome;

    /// <summary>판 <b>i</b> (키 i+1) 에 얹은 것들.</summary>
    class Lamp
    {
        public Transform green, red, flash, stop, stray;
        public TextMesh gain;
        public Vector3 gainHome;
        public int gainShown = -1;
    }
    readonly Lamp[] lamps = new Lamp[SafetyDrill.Pads];

    // 주변광은 되돌려 놔야 한다. 캠퍼스 전체가 쓰는 값이야(급식과 같은 자리).
    Color ambientWas;
    UnityEngine.Rendering.AmbientMode ambientModeWas;
    bool ambientSaved;

    // 잰 값 - 카메라와 격자가 같이 쓴다
    Vector3 front, right;
    Vector3 gridCentre;
    float gridW, gridH;

    void Awake()
    {
        game = GetComponent<SafetyDrill>();

        wall = FindWall();
        if (wall == null)
        {
            // ★ <b>여기서 포기하지 않는다.</b> 벽을 못 찾았다고 게임이 통째로 못 하는 것이
            // 되면 안 된다 - 화면이 3×3 판을 대신 그린다(기획서 §7 「막히면 빠져나오는
            // 안전장치」). 급식은 줄이 교착됐을 때 이걸 안 해 뒀다가 한 번 갇혔다.
            Debug.LogWarning($"[안전훈련] 반응훈련벽({WallName})을 못 찾았다. 철곰관 밖에서 "
                           + "시작했거나 씬이 벽보다 오래된 것이다. 화면 판으로 대신한다.");
            return;
        }

        var pads = FindPads(wall);
        if (pads.Count != SafetyDrill.Pads)
        {
            Debug.LogWarning($"[안전훈련] 곰발 판을 {pads.Count}/9 밖에 못 찾았다 - 부품 이름"
                           + "(Target_1~Target_9)이 바뀌었는지 확인해라. 화면 판으로 대신한다.");
            return;
        }

        Measure(wall, pads);
        var ordered = ByKey(pads);

        rig = new GameObject("SafetyDrillLamps").transform;
        for (int i = 0; i < SafetyDrill.Pads; i++) lamps[i] = BuildLamp(ordered[i], i);

        BuildCamera();
        BuildLight();
        LiftAmbient();

        if (game != null) game.BoardBound = true;
    }

    /// <summary>
    /// <b>푸는 자리는 한 군데.</b> 카메라를 안 돌려주면 훈련이 끝난 뒤에도 화면이 벽을 보고
    /// 있고, 주변광을 안 되돌리면 캠퍼스 전체가 밝은 채로 남는다(급식에서 세운 규칙 그대로).
    /// </summary>
    void OnDestroy()
    {
        if (previous != null) previous.enabled = true;
        if (cam != null) Destroy(cam.gameObject);
        if (rig != null) Destroy(rig.gameObject);

        if (ambientSaved)
        {
            RenderSettings.ambientMode = ambientModeWas;
            RenderSettings.ambientLight = ambientWas;
        }
    }

    // ── 벽 찾기 ───────────────────────────────────────────────────────────────

    /// <summary>
    /// 이름으로 찾는다. 이 프로젝트는 이름 찾기를 피하는 게 원칙이지만 <b>의도한 예외</b>다 -
    /// 인스펙터로 꽂으려면 씬을 다시 구워야 하고, 그게 이 미니게임이 피하려던 바로 그것이야
    /// (급식이 <c>InServeTop</c> 을 찾는 것과 같은 자리).
    ///
    /// 이름이 바뀌었을 때를 대비해 <b>부품에서 거슬러 올라가는 길</b>도 둔다 -
    /// <c>Target_1</c> 은 FBX 안의 이름이라 <see cref="CampusBuilder"/> 를 고쳐도 안 바뀐다.
    /// </summary>
    static Transform FindWall()
    {
        var byName = GameObject.Find(WallName);
        if (byName != null) return byName.transform;

        var probe = GameObject.Find("Target_1");
        if (probe == null) return null;

        // Target_1 위로 올라가면서 <b>아홉 장이 전부 들어오는</b> 첫 조상을 고른다
        for (var t = probe.transform.parent; t != null; t = t.parent)
            if (FindPads(t).Count == SafetyDrill.Pads) return t;

        return probe.transform.parent;
    }

    /// <summary>깊이를 안 따지고 <c>Target_1</c> ~ <c>Target_9</c> 를 모은다.</summary>
    static List<Transform> FindPads(Transform root)
    {
        var all = root.GetComponentsInChildren<Transform>(true);
        var found = new List<Transform>();

        for (int n = 1; n <= SafetyDrill.Pads; n++)
        {
            string want = "Target_" + n;
            foreach (var t in all)
            {
                if (t.name != want) continue;
                found.Add(t);
                break;
            }
        }
        return found;
    }

    // ── 재기 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// ★★ <b>앞면을 짐작하지 않고 잰다.</b> 이 프로젝트에서 «블렌더에서 이랬으니 유니티도
    /// 이럴 것» 이 두 번 틀렸다(곰이 눕고 스피커가 뒤돌았다). 그래서 트랜스폼의 축을 아예
    /// 안 쓴다 - <b>곰발 판 아홉 장이 만드는 평면</b>에서 법선을 뽑는다.
    ///
    /// 방향(앞/뒤)은 <b>플레이어가 서 있는 쪽</b>으로 정한다. 플레이어는 방에 들어와서
    /// 벽을 마주 보고 E 를 눌렀으니 그쪽이 앞면이다 - 모델의 축이 어떻든 못 틀린다.
    /// </summary>
    void Measure(Transform root, List<Transform> pads)
    {
        var at = new Vector3[pads.Count];
        for (int i = 0; i < pads.Count; i++) at[i] = Centre(pads[i]);

        // 평면 안의 방향 둘을 고른다 - 제일 먼 판, 그리고 그 선에서 제일 벗어난 판
        Vector3 a = Vector3.zero, b = Vector3.zero;
        float bestA = 0f, bestB = 0f;
        for (int i = 1; i < at.Length; i++)
        {
            Vector3 v = at[i] - at[0];
            if (v.sqrMagnitude > bestA) { bestA = v.sqrMagnitude; a = v; }
        }
        for (int i = 1; i < at.Length; i++)
        {
            Vector3 v = at[i] - at[0];
            float off = Vector3.Cross(a.normalized, v).sqrMagnitude;
            if (off > bestB) { bestB = off; b = v; }
        }

        Vector3 normal = Vector3.Cross(a, b);
        normal.y = 0f;   // 벽은 서 있다. 법선은 수평이야

        if (normal.sqrMagnitude < 1e-6f)
        {
            // 아홉 장이 한 줄로 서 있다는 뜻이라 평면이 안 나온다. 트랜스폼으로 물러선다
            normal = root.forward;
            normal.y = 0f;
            if (normal.sqrMagnitude < 1e-6f) normal = Vector3.forward;
        }
        front = normal.normalized;

        // 앞뒤 - <b>보는 사람 쪽</b>이 앞
        float side = Vector3.Dot(Viewer() - at[0], front);
        float bulge = Vector3.Dot(Middle(at) - Centre(root), front);   // 판이 벽보다 튀어나온 쪽

        // 보는 사람이 벽면 위에 딱 서 있다(있을 수 없지만)
        if (Mathf.Abs(side) < 0.05f) side = bulge;
        if (side < 0f) front = -front;

        // ★ 둘이 어긋나면 <b>벽이 뒤돌아 서 있다.</b> 불빛은 어차피 보는 사람 쪽에 붙이니까
        // 게임은 되지만, 곰발 판이 옆 벽을 보고 있다는 뜻이라 그림이 이상하다.
        // <see cref="CampusBuilder"/> 의 철곰관 소품 열 점이 <b>같이</b> 180도 돌아가 있을 거야.
        if (Mathf.Abs(bulge) > 0.005f && Vector3.Dot(front, Middle(at) - Centre(root)) < 0f)
            Debug.LogWarning("[안전훈련] 반응훈련벽이 <b>뒤돌아</b> 서 있다 - 곰발 판이 방 "
                           + "반대쪽을 본다. 불빛은 보는 사람 쪽에 붙여 게임은 되지만, "
                           + "CampusBuilder.Cheolgom 의 앞면 판정을 한 번 보고 씬을 다시 구워라.");

        // 카메라가 벽을 마주 볼 때의 <b>화면 오른쪽</b>. 격자의 좌우가 이걸로 정해진다
        right = Vector3.Cross(Vector3.up, -front).normalized;

        float rMin = float.MaxValue, rMax = float.MinValue;
        float uMin = float.MaxValue, uMax = float.MinValue;
        float fMax = float.MinValue;
        foreach (var p in pads)
        {
            Span(p, right, ref rMin, ref rMax);
            Span(p, Vector3.up, ref uMin, ref uMax);
            float f0 = float.MaxValue, f1 = float.MinValue;
            Span(p, front, ref f0, ref f1);
            if (f1 > fMax) fMax = f1;
        }

        gridW = rMax - rMin;
        gridH = uMax - uMin;
        gridCentre = right * ((rMin + rMax) * 0.5f)
                   + Vector3.up * ((uMin + uMax) * 0.5f)
                   + front * fMax;

        Debug.Log($"[안전훈련] 곰발 판 9장 · 격자 {gridW:0.00} × {gridH:0.00}m · "
                + $"앞면 {front} · 벽 '{root.name}'");
    }

    /// <summary>보는 사람의 자리. 걸어다니는 몸이 먼저, 없으면 카메라.</summary>
    static Vector3 Viewer()
    {
        var player = FindFirstObjectByType<FirstPersonController>();
        if (player != null) return player.transform.position;
        return Camera.main != null ? Camera.main.transform.position : Vector3.zero;
    }

    static Vector3 Middle(Vector3[] v)
    {
        Vector3 sum = Vector3.zero;
        foreach (var p in v) sum += p;
        return v.Length > 0 ? sum / v.Length : Vector3.zero;
    }

    /// <summary>이 오브젝트가 그리는 것 전부의 한가운데.</summary>
    static Vector3 Centre(Transform t)
    {
        Bounds all = default;
        bool any = false;
        foreach (var r in t.GetComponentsInChildren<Renderer>())
        {
            if (!any) { all = r.bounds; any = true; } else all.Encapsulate(r.bounds);
        }
        return any ? all.center : t.position;
    }

    /// <summary>
    /// 이 오브젝트가 <paramref name="axis"/> 방향으로 차지하는 구간을 넓힌다.
    ///
    /// ★ <b><see cref="Renderer.bounds"/> 를 쓰면 안 된다.</b> 그건 축에 나란한 상자라,
    /// 건물이 yaw 55도로 돌아가 있으면 납작한 판도 두툼한 상자로 잡힌다. 메시의
    /// <b>제 좌표계 상자 꼭짓점 여덟 개</b>를 월드로 옮겨서 재야 실제 크기가 나온다.
    /// </summary>
    static void Span(Transform t, Vector3 axis, ref float min, ref float max)
    {
        foreach (var r in t.GetComponentsInChildren<Renderer>())
        {
            Bounds local = r.localBounds;
            Vector3 c = local.center, e = local.extents;
            Matrix4x4 m = r.transform.localToWorldMatrix;

            for (int k = 0; k < 8; k++)
            {
                var corner = new Vector3(
                    c.x + ((k & 1) == 0 ? -e.x : e.x),
                    c.y + ((k & 2) == 0 ? -e.y : e.y),
                    c.z + ((k & 4) == 0 ? -e.z : e.z));
                float d = Vector3.Dot(m.MultiplyPoint3x4(corner), axis);
                if (d < min) min = d;
                if (d > max) max = d;
            }
        }
    }

    /// <summary>
    /// <b>키 번호를 짐작하지 않고 잰다.</b> 판을 높이로 세 줄, 줄 안에서 화면 왼쪽부터
    /// 정렬해서 <b>1·2·3 아랫줄 / 4·5·6 / 7·8·9 윗줄</b> 로 붙인다(기획서 §3).
    ///
    /// FBX 안의 <c>Target_n</c> 번호를 그대로 믿지 않는 이유: 그건 블렌더에서 만든 순서지
    /// 화면에 보이는 자리가 아니다. <b>키패드 생김새와 벽이 어긋나면</b> 이 게임은 그 자리에서
    /// 못 하는 게임이 된다.
    /// </summary>
    Transform[] ByKey(List<Transform> pads)
    {
        var sorted = new List<Transform>(pads);
        sorted.Sort((x, y) => Centre(y).y.CompareTo(Centre(x).y));   // 높은 것부터

        var byKey = new Transform[SafetyDrill.Pads];
        for (int row = 0; row < 3; row++)
        {
            var line = sorted.GetRange(row * 3, 3);
            line.Sort((x, y) => Vector3.Dot(Centre(x), right).CompareTo(Vector3.Dot(Centre(y), right)));

            // row 0 이 제일 윗줄 -> 키 7·8·9 (0부터 세면 6·7·8)
            int baseKey = (2 - row) * 3;
            for (int col = 0; col < 3; col++) byKey[baseKey + col] = line[col];
        }
        return byKey;
    }

    // ── 판 하나에 얹는 것 ─────────────────────────────────────────────────────

    /// <summary>
    /// <b>곰발 판은 동그라미다.</b> 그 위에 네모를 얹으면 «판이 켜졌다» 가 아니라
    /// «판 위에 뭔가 붙었다» 로 보인다(2026-09-29 유저 지적). 판을 <b>덮는 원판</b>으로 바꾼다.
    ///
    /// ★★ <b>번호패는 판 «위」에 단다</b>(2026-09-29 2차 — *"곰발바닥 패널이 안보이니까 조금
    /// 위로 올려서 숫자판과 곰 발바닥이 보이게 해줘"*). 가운데에 박으니 <b>아홉 장 전부</b>
    /// 발바닥이 번호패에 가려 있었다 — 불이 들어온 한 장만 잠깐 가려지는 것과 달리, 이건
    /// <b>늘</b> 가려진다. 유저가 만든 것을 덮는 UI 는 <b>UI 쪽이 비켜야 한다.</b>
    /// 줄 간격이 1.86 지름이라 판과 판 사이가 비어 있고, 번호패는 거기에 앉는다.
    ///
    /// 층 순서(판 면에서 앞으로):
    /// <code>
    /// 0.008 테두리 원판(어둡게 - 청록 벽에서 떼어 놓는다)
    /// 0.014 색 원판(초록 / 빨강 / 크림)
    /// 0.022 빨강 위의 X
    /// 0.030 번호패(크림) · 0.038 번호 글자  — <b>판 위쪽 0.62지름 자리</b>, 늘 켜져 있다
    /// 0.046 멈춤 · 헛손질
    /// </code>
    /// </summary>
    Lamp BuildLamp(Transform pad, int index)
    {
        float rMin = float.MaxValue, rMax = float.MinValue;
        float uMin = float.MaxValue, uMax = float.MinValue;
        float fMin = float.MaxValue, fMax = float.MinValue;
        Span(pad, right, ref rMin, ref rMax);
        Span(pad, Vector3.up, ref uMin, ref uMax);
        Span(pad, front, ref fMin, ref fMax);

        // 곰발 판의 지름. 가로세로 중 작은 쪽을 쓴다 — 크게 잡으면 옆 판을 침범한다
        float d = Mathf.Max(0.05f, Mathf.Min(rMax - rMin, uMax - uMin));

        // 판마다 통을 하나 둔다. 통의 <b>로컬 +Z 가 앞면</b>이라 아래 좌표가 전부 단순해진다
        var seat = new GameObject($"Pad{index + 1}").transform;
        seat.SetParent(rig, false);
        seat.SetPositionAndRotation(
            right * ((rMin + rMax) * 0.5f)
            + Vector3.up * ((uMin + uMax) * 0.5f)
            + front * fMax,
            Quaternion.LookRotation(front, Vector3.up));

        // ★ <b>초록이 빨강보다 크다.</b> 색맹 여부와 상관없이 «누를 것» 이 «건드리면 안 되는 것»
        // 보다 커야 손이 먼저 반응한다 — 색상 하나에 두 가지 뜻을 다 싣지 않는다.
        var lamp = new Lamp
        {
            green = State(seat, "Green", d, 1.08f, ColGreen),
            red   = State(seat, "Red",   d, 0.98f, ColRed),
            flash = State(seat, "Flash", d, 1.08f, ColFlash),
        };

        // ★ <b>빨강에는 X 를 긋는다.</b> 초록·빨강만으로 가르면 적록색각인 사람에게 이 게임은
        // «운» 이 된다. 게다가 곰발 판 <b>자체에 붉은 것이 섞여 있어서</b> 색만으로는 «원래
        // 붉은 판» 과 «켜진 빨강» 이 안 갈린다. 「건드리지 마라」 는 원래 X 로 쓰는 표시야.
        // ★ X 는 <b>번호패 바깥으로</b> 나와야 보인다. 처음엔 길이를 0.86 으로 잡았는데,
        // 번호패 반지름이 0.20 이라 <b>팔이 패 뒤에 거의 다 숨었다</b>(프리뷰 렌더에서 잡았다).
        // 팔이 판 지름만큼 뻗으면 번호를 가운데 두고 X 가 또렷하게 걸린다.
        float bar = d * 0.105f;
        float diag = d * 1.00f;
        for (int s = -1; s <= 1; s += 2)
        {
            var cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cross.name = s > 0 ? "BarA" : "BarB";
            Strip(cross);
            cross.transform.SetParent(lamp.red, false);
            cross.transform.localPosition = new Vector3(0f, 0f, 0.022f);
            cross.transform.localRotation = Quaternion.Euler(0f, 0f, s * 45f);
            cross.transform.localScale = new Vector3(diag, bar, DiscThick * 0.6f);
            cross.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(ColEdge);
        }

        // 멈춤·헛손질은 판만 덮는다. 번호패는 <b>이름표</b>라 판이 죽어도 남아 있어야 한다
        lamp.stop  = Solo(seat, "Stop",  0.046f, d * 1.03f, ColStop,  Finish.무광);
        lamp.stray = Solo(seat, "Stray", 0.046f, d * 1.03f, ColStray, Finish.무광);

        // ── 번호패 — 이 게임에서 제일 중요한 한 가지 ──────────────────────────
        //
        // ★★ 반응게임의 과녁에는 <b>누를 키가 적혀 있다.</b> 안 적으면 «어디를 누르나» 가
        // 게임의 내용이 돼서, 반응속도가 아니라 <b>자리 외우기</b>를 시험하게 된다.
        //
        // ★★ <b>판 위쪽에 단다.</b> 가운데에 박으면 아홉 장 전부 곰발바닥이 늘 가려진다 —
        // 유저가 만든 것을 덮는 UI 는 UI 쪽이 비켜야 한다(2026-09-29 2차).
        // 줄 간격이 지름의 1.86 배라 판 위쪽 0.62 자리가 비어 있다.
        //
        // 테를 두르는 이유: 곰발 판이 <b>크림 · 황토 · 붉은 갈색</b> 세 가지로 구워져 있고
        // 벽은 청록이라, 크림 패만 놓으면 어디서는 «패» 로 안 읽힌다. 어두운 테 한 겹이면
        // 어느 바탕 위에서도 같은 모양으로 보인다.
        const float badgeUp = 0.62f;
        Lift(Disc(seat, "BadgeRim", 0.028f, d * 0.50f, ColEdge, Finish.무광), d * badgeUp);
        Lift(Disc(seat, "Badge", 0.030f, d * 0.42f, ColBadge, Finish.무광), d * badgeUp);
        Lift(Label(seat, "Num", (index + 1).ToString(), 0.038f, d * 0.30f, ColInk).transform,
             d * badgeUp);

        // 맞췄을 때 떠오르는 «+2». <b>번호패 위</b>에서 시작해 더 위로 올라가며 사라진다 —
        // 번호패와 겹치면 둘 다 안 읽힌다
        lamp.gain = Label(seat, "Gain", "+2", 0.055f, d * 0.34f, ColGain);
        lamp.gainHome = new Vector3(0f, d * 1.02f, 0.055f);
        lamp.gain.transform.localPosition = lamp.gainHome;
        lamp.gain.gameObject.SetActive(false);

        return lamp;
    }

    /// <summary>만들어 둔 것을 판 위쪽으로 올린다. 만들 때 y 를 받게 고치면 부르는 데가 다 지저분해진다.</summary>
    static Transform Lift(Transform t, float up)
    {
        var at = t.localPosition;
        t.localPosition = new Vector3(at.x, at.y + up, at.z);
        return t;
    }

    /// <summary>테두리 원판 + 색 원판을 한 통에 담는다. 통째로 켜고 끄고, 통째로 튀어나온다.</summary>
    Transform State(Transform seat, string name, float d, float span, Color color)
    {
        var group = new GameObject(name).transform;
        group.SetParent(seat, false);

        // 테두리를 <b>어둡게</b> 깐다. 청록 벽 위에 밝은 원판만 놓으면 경계가 흐려서
        // 크기를 눈이 못 잡는다 — 어두운 테를 두르면 «동그란 것 하나» 로 딱 읽힌다.
        Disc(group, "Edge", 0.008f, d * span, ColEdge, Finish.무광);
        Disc(group, "Face", 0.014f, d * (span - 0.14f), color, Finish.발광);

        group.gameObject.SetActive(false);
        return group;
    }

    /// <summary>혼자 켜지고 꺼지는 원판 하나.</summary>
    Transform Solo(Transform seat, string name, float ahead, float d, Color color, Finish finish)
    {
        var t = Disc(seat, name, ahead, d, color, finish);
        t.gameObject.SetActive(false);
        return t;
    }

    /// <summary>
    /// 앞면을 보는 납작한 원판.
    ///
    /// ★ Quad 가 아니라 <b>원기둥</b>이다. Quad 는 한쪽 면만 있어서 «어느 쪽이 앞인가» 가
    /// 또 하나의 짐작거리가 되는데, 이 프로젝트가 방향으로 틀린 게 이미 두 번이야.
    /// <see cref="Quaternion.FromToRotation"/> 으로 <b>원기둥의 축(로컬 +Y)을 앞면(+Z)에</b>
    /// 맞춘다 — 오일러 각을 손으로 적으면 부호에서 또 틀린다.
    /// </summary>
    Transform Disc(Transform parent, string name, float ahead, float d, Color color, Finish finish)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Strip(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, ahead);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
        // 기본 원기둥은 지름 1 · 높이 2 라 y 배율이 «두께의 절반» 이다
        go.transform.localScale = new Vector3(d, DiscThick * 0.5f, d);
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color, finish);
        return go.transform;
    }

    /// <summary>
    /// 월드에 세운 글자. <see cref="BuildingSign"/> 가 현판에 쓰는 것과 <b>같은 재질</b>
    /// (<c>Racing/PlaqueText</c> — 깊이 검사를 켠 셰이더)이라 벽을 통과해서 보이지 않는다.
    ///
    /// ★ <b>180도 돌린다.</b> TextMesh 는 그대로 두면 보는 쪽에서 좌우가 뒤집혀 보인다 —
    /// 현판이 전부 거울 글씨로 나왔던 그 자리다(2026-09-17).
    /// </summary>
    TextMesh Label(Transform seat, string name, string body, float ahead, float height, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(seat, false);
        go.transform.localPosition = new Vector3(0f, 0f, ahead);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var text = go.AddComponent<TextMesh>();
        text.font = Resources.Load<Font>(BuildingSign.PlaqueFontName);
        text.text = body;
        text.fontSize = 120;                      // 크게 굽고 characterSize 로 줄인다
        text.characterSize = height * 10f / 120f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;

        if (text.font != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = BuildingSign.TextMaterial(text.font);
        return text;
    }

    /// <summary>불빛은 부딪히는 물건이 아니다. 기본 도형은 콜라이더를 달고 태어난다.</summary>
    static void Strip(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c == null) return;
        if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
    }

    // ── 카메라와 빛 ───────────────────────────────────────────────────────────

    void BuildCamera()
    {
        previous = Camera.main;
        if (previous == null) previous = FindFirstObjectByType<Camera>();

        var go = new GameObject("SafetyDrillCamera");
        cam = go.AddComponent<Camera>();

        // 배경색·안개·컬링 마스크를 그대로 물려받는다. 새로 만들면 하늘이 유니티 기본값으로
        // 돌아가서 이 화면만 딴 게임처럼 보인다(급식 카메라와 같은 이유).
        if (previous != null) cam.CopyFrom(previous);
        cam.fieldOfView = 42f;

        var extra = go.GetComponent<UniversalAdditionalCameraData>();
        if (extra == null) extra = go.AddComponent<UniversalAdditionalCameraData>();
        extra.renderPostProcessing = true;
        extra.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        // AudioListener 는 안 단다. 둘이면 경고가 뜬다(급식·이야기 카메라에서 세운 규칙).
        //
        // ★ <b>거리를 손으로 안 적는다.</b> 격자 크기와 지금 화면비에서 거꾸로 구한다 -
        // 벽을 새로 뽑아 크기가 달라져도, 창을 납작하게 열어도 판 아홉 장이 화면에 다 든다.
        // 손으로 3m 를 박으면 <b>둘 중 하나에서 반드시 잘린다</b>(HUD 고정 좌표로 세 번 겹친 것).
        float vTan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float hTan = vTan * Mathf.Max(0.5f, cam.aspect);
        const float margin = 0.30f;   // 판이 화면에서 크게 보여야 «과녁» 이 된다
        float need = Mathf.Max((gridH * 0.5f + margin) / vTan, (gridW * 0.5f + margin) / hTan);

        // 너무 붙으면 판이 화면을 넘고, 너무 물러서면 방 건너편 벽을 뚫는다
        float dist = Mathf.Clamp(need, 2.0f, 6.0f);

        camHome = gridCentre + front * dist;
        go.transform.position = camHome;
        go.transform.LookAt(gridCentre);

        if (previous != null) previous.enabled = false;
    }

    /// <summary>
    /// 벽에 드는 빛. 철곰관 천장등은 <b>발광 재질</b>이라 실제로는 아무것도 안 비춘다 -
    /// 그대로 두면 판이 주변광으로만 보여서 초록과 빨강이 둘 다 탁하게 나온다.
    /// </summary>
    void BuildLight()
    {
        var go = new GameObject("DrillLamp");
        go.transform.SetParent(rig, false);
        go.transform.position = gridCentre + front * 2.2f + Vector3.up * 0.9f;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.95f, 0.86f);
        light.intensity = 2.1f;
        light.range = 7f;
        light.shadows = LightShadows.None;   // 기획서 §7.6 - 실시간 그림자는 주요 조명 하나만
    }

    /// <summary>
    /// 주변광을 조금 올린다. 급식과 <b>같은 방식으로 올리기만</b> 한다 - 절대값을 박으면
    /// 캠퍼스가 차가울 때(폐과)와 따뜻할 때 중 한쪽에서 반드시 어색해진다.
    /// </summary>
    void LiftAmbient()
    {
        ambientModeWas = RenderSettings.ambientMode;
        ambientWas = RenderSettings.ambientLight;
        ambientSaved = true;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(ambientWas, new Color(0.62f, 0.60f, 0.56f), 0.5f);
    }

    // ── 매 프레임 ─────────────────────────────────────────────────────────────

    void LateUpdate()
    {
        if (game == null || rig == null) return;

        bool frozen = game.Frozen;
        bool playing = game.Now == SafetyDrill.Phase.진행;

        for (int i = 0; i < SafetyDrill.Pads; i++)
        {
            var lamp = lamps[i];
            if (lamp == null || lamp.green == null) continue;

            var state = playing ? game.LampOf(i) : SafetyDrill.Lamp.꺼짐;
            bool flash = playing && Time.time - game.HitAt(i) < FlashSeconds;
            bool stray = playing && !frozen && Time.time - game.StrayAt(i) < StraySeconds;

            // 멈춘 동안에는 <b>판 전체가 어두워진다.</b> 「0.8초 동안 판 전체가 멈춘다」 를
            // 화면에서 한 번에 읽히게 하는 자리야 - 점수만 안 오르면 고장으로 읽힌다.
            lamp.stop.gameObject.SetActive(frozen && playing);
            lamp.stray.gameObject.SetActive(stray);
            lamp.flash.gameObject.SetActive(flash && !frozen);
            lamp.green.gameObject.SetActive(!frozen && !flash && state == SafetyDrill.Lamp.초록);
            lamp.red.gameObject.SetActive(!frozen && !flash && state == SafetyDrill.Lamp.빨강);

            Gain(lamp, i, playing);
        }

        Shake();
    }

    // ★ <b>켜질 때 커지는 연출은 뺐다</b>(2026-09-29 2차 유저: *"작아졌다가 채워지는 밝음이
    // 아니라 그냥 밝아졌다가 꺼지는 연출로"*). 판이 <b>0.85초</b> 밖에 안 켜져 있는 게임에서
    // 0.11초를 «자라는 데» 쓰면, 그 동안은 과녁이 아직 제 크기가 아니다 — 눈에 띄라고 넣은
    // 것이 정작 <b>겨눌 것을 흔든다.</b> 켜짐은 켜짐이어야 한다.

    /// <summary>
    /// 맞춘 판 위로 <b>«+2» 가 떠오른다.</b>
    ///
    /// 2026-09-29 유저: *"눌러도 점수에 변경이 없어."* 점수판은 화면 <b>오른쪽 위 구석</b>인데
    /// 이 게임을 하는 동안 눈은 <b>벽 가운데</b>에 박혀 있다 - 구석에서 숫자가 2 올라가는 건
    /// 안 보이는 것과 같다. <b>대답은 손이 닿은 자리에서 나와야 한다.</b>
    /// </summary>
    void Gain(Lamp lamp, int i, bool playing)
    {
        float since = playing ? Time.time - game.HitAt(i) : 99f;
        if (since < 0f || since >= GainSeconds)
        {
            if (lamp.gain.gameObject.activeSelf)
            {
                lamp.gain.gameObject.SetActive(false);
                lamp.gainShown = -1;
            }
            return;
        }

        int points = game.GainAt(i);
        if (lamp.gainShown != points)
        {
            // 글자는 <b>바뀔 때만</b> 넣는다. TextMesh 는 대입할 때마다 메시를 다시 짠다
            lamp.gain.text = "+" + points;
            lamp.gainShown = points;
        }
        if (!lamp.gain.gameObject.activeSelf) lamp.gain.gameObject.SetActive(true);

        float t = since / GainSeconds;
        lamp.gain.transform.localPosition = lamp.gainHome + Vector3.up * (t * 0.16f);

        var c = ColGain;
        c.a = 1f - t * t;               // 끝에서 빨리 사라진다 - 오래 남으면 다음 판을 가린다
        lamp.gain.color = c;            // TextMesh.color 는 정점 색이라 .mat 에셋을 안 건드린다
    }

    /// <summary>
    /// 빨강을 밟았을 때만 화면이 한 번 흔들린다. <b>늘 흔들면 그건 진동이 아니라 화면
    /// 상태가 된다</b>(기획서 §6). 핑계는 10 매트 운반대야.
    ///
    /// <see cref="ScreenEffects"/> 가 꺼져 있으면 안 흔든다 - 흔들림은 멀미를 타는 사람에게
    /// 제일 먼저 걸리는 것이고, 그 스위치는 이미 접근성 설정으로 만들어 뒀다(2026-09-17).
    /// </summary>
    void Shake()
    {
        if (cam == null) return;

        float since = Time.time - game.SlipAt;
        if (since < 0f || since > ShakeSeconds || !ScreenEffects.On)
        {
            cam.transform.position = camHome;
            return;
        }

        float fade = 1f - since / ShakeSeconds;
        float amount = ShakeAmount * fade * fade;
        cam.transform.position = camHome
            + right * (Mathf.Sin(since * 58f) * amount)
            + Vector3.up * (Mathf.Sin(since * 41f) * amount * 0.6f);
    }
}
