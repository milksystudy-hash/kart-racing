using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// <b>오늘의 급식 - 판이 도는 동안의 방.</b> 카메라를 배식대로 돌리고 곰 손님을 줄 세운다.
/// 규칙은 <see cref="Canteen"/>, 화면은 <see cref="CanteenHUD"/>, 여기는 <b>3D</b> 만.
///
/// 방 자체의 꾸밈(조명 · 김 · 메뉴판 · 소품)은 <see cref="CanteenDressing"/> 가 <b>상시</b>
/// 맡는다 - 게임을 안 해도 급식실로 보여야 하니까. 여기서 또 켜면 등이 두 겹이 된다.
///
/// <b>좌표를 손으로 박지 않는다.</b> 배식대(<c>InServeTop</c>)를 찾아 그 자세에서 줄과
/// 카메라 자리를 뽑는다 - 건물이 10도 돌아가 있고, 곰밥마당을 옮기면 손으로 적은 좌표는
/// 전부 틀린다(로비 곰 자리에서 두 번 겪었다).
///
/// <b>씬에 저장되는 게 0이다.</b> 손님도 카메라도 실행 중에 만들고 나갈 때 지운다.
/// </summary>
public class CanteenStage : MonoBehaviour
{
    /// <summary>줄에 세우는 손님 수. 뒤에서 걸어 들어오는 자리가 하나 더 붙는다.</summary>
    const int Slots = 4;

    /// <summary>손님 간격. 곰 어깨 폭이 0.50 이라 1.3 이면 붙어 보이지 않는다.</summary>
    const float Spacing = 1.3f;

    /// <summary>배식대에서 방 쪽으로 이만큼 떨어져 선다.</summary>
    const float CounterGap = 1.55f;

    const float WalkSpeed = 2.2f;

    /// <summary>배식대 상판 윗면. <c>InServeTop</c> 이 y 1.03 에 두께 0.08 이라 1.07.</summary>
    const float CounterTop = 1.07f;

    Canteen game;
    Transform counter;
    Camera cam;
    Camera previous;
    Transform herd;
    Transform fixtures;

    readonly List<Customer> customers = new List<Customer>();
    readonly List<Transform> bowls = new List<Transform>();
    readonly List<Renderer> bowlSkins = new List<Renderer>();
    readonly Material[] bowlCalm = new Material[CanteenOrder.Slots];
    readonly Material[] bowlLit = new Material[CanteenOrder.Slots];

    Transform bubble;
    readonly List<Transform> bubbleDots = new List<Transform>();

    int servedSeen;
    int traySeen = -1;
    int orderSeen = -1;

    // 주변광은 되돌려 놔야 한다. 캠퍼스 전체가 쓰는 값이야.
    Color ambientWas;
    UnityEngine.Rendering.AmbientMode ambientModeWas;
    bool ambientSaved;

    class Customer
    {
        public Transform body;      // 땅에 붙는 자리
        public Transform rig;       // 숨쉬기와 깡충 - 자리와 분리해야 서로 안 싸운다
        public Transform tray;      // 받아 든 식판. 줄에 서 있는 동안은 꺼져 있다
        public int slot;            // 0 이 맨 앞. -1 이면 받고 나가는 중
        public Vector3 target;
        public float phase;         // 곰마다 숨 쉬는 박자를 어긋나게
        public float hopAt = -99f;
        public float hopPower = 1f; // 딱 맞췄을 때 더 크게 뛴다
        public float build = 1f;    // 체격 - 다섯이 똑같으면 «복사본 다섯» 으로 보인다

        // ★ 2026-09-22 — 받은 것과 표정.
        public Transform[] food = new Transform[CanteenOrder.Slots];  // 식판 칸마다 하나
        public Transform[] brow = new Transform[2];                   // 눈썹 - 슬플 때만 켠다
        public Transform[] eye  = new Transform[2];
        public bool sad;            // 빈 식판으로 나가는 중
    }

    /// <summary>
    /// 그릇이 «퍼 올려진» 순간. 색만 바뀌면 표를 누른 것이고,
    /// <b>튀어올랐다 내려앉아야</b> 손으로 담은 게 된다.
    /// </summary>
    readonly float[] bowlPopAt = new float[CanteenOrder.Slots];

    // ---- 자리 --------------------------------------------------------------

    /// <summary>배식대의 <b>긴 쪽</b>. 크기가 (2.1, 0.08, 11.4) 라 로컬 Z 가 줄이 서는 방향이다.</summary>
    Vector3 Along => counter != null ? counter.forward : Vector3.forward;

    /// <summary>배식대에서 <b>방 쪽</b>. 배식대가 왼쪽 벽에 붙어 있어 +X 가 방이다.</summary>
    Vector3 ToRoom => counter != null ? counter.right : Vector3.right;

    Vector3 CounterFoot => counter != null
        ? new Vector3(counter.position.x, 0f, counter.position.z)
        : transform.position;

    /// <summary>
    /// 줄 i 번째가 설 자리. 0 이 <b>지금 받는 손님</b>이다.
    ///
    /// ★ <b>동선은 식판 → 배식대 → 반납대</b> 순이고, 재보니 실제 좌표가 이렇다:
    /// 식판 스탠드 <c>Along +6.4</c> · 반납대 <c>Along −6.6</c>.
    /// 그러니까 줄은 <b>Along 이 줄어드는 쪽</b>으로 나아간다 - 처음엔 이걸 거꾸로 잡아서
    /// 손님이 받고 나서 식판 쌓인 쪽으로 되돌아 걸어갔다.
    /// </summary>
    Vector3 SlotAt(int i) =>
        CounterFoot + ToRoom * CounterGap + Along * (0.6f + i * Spacing);

    /// <summary>받고 나가는 자리 - 반납대와 탁자 쪽.</summary>
    Vector3 ExitSpot => CounterFoot + ToRoom * 7.5f + Along * -4.5f;

    /// <summary>
    /// 배식대 위 그릇 다섯 개가 놓이는 자리.
    ///
    /// <b>맨 앞 손님(Along 0.6) 을 가운데 두고 좌우로 편다.</b> 배식대 가운데에 깔면
    /// 그릇이 2·3·4번 손님 앞에 놓여서 정작 받는 손님이 그릇을 등지고 선다.
    /// <c>ToRoom * 0.45</c> 는 상판의 <b>방 쪽 가장자리</b>(반폭 1.05) - 카메라가 방 쪽이라
    /// 벽 쪽에 두면 손님 어깨에 가린다.
    /// </summary>
    Vector3 BowlAt(int i) =>
        CounterFoot + ToRoom * 0.45f + Along * (1.84f - i * 0.62f) + Vector3.up * CounterTop;

    // ---- 살고 죽기 ---------------------------------------------------------

    void Awake()
    {
        game = GetComponent<Canteen>();
        counter = FindCounter();

        if (counter != null)
        {
            herd = new GameObject("CanteenCustomers").transform;
            fixtures = new GameObject("CanteenFixtures").transform;

            for (int i = 0; i <= Slots; i++)
            {
                var c = MakeCustomer(i);
                c.target = SlotAt(i);
                c.body.position = c.target;
                FaceCounter(c.body);
                customers.Add(c);
            }

            // 조명 · 김 · 가림막은 CanteenDressing 이 상시 맡는다. 여기서는 판이 돌 때만
            // 필요한 것들만 - 맨 앞 손님 얼굴에 드는 빛과, 주변광 살짝 올리기.
            BuildBowls();
            BuildFloorMarks();
            BuildBubble();
            FaceLamp();
            LiftAmbient();
            BuildCamera();
        }

        servedSeen = game != null ? game.Served : 0;
    }

    /// <summary>
    /// <b>푸는 자리는 한 군데.</b> 카메라를 안 돌려주면 배식이 끝난 뒤에도 화면이 배식대를
    /// 보고 있고, 주변광을 안 되돌리면 캠퍼스 전체가 밝은 채로 남는다.
    /// </summary>
    void OnDestroy()
    {
        if (previous != null) previous.enabled = true;
        if (cam != null) Destroy(cam.gameObject);
        if (herd != null) Destroy(herd.gameObject);
        if (fixtures != null) Destroy(fixtures.gameObject);

        if (ambientSaved)
        {
            RenderSettings.ambientMode = ambientModeWas;
            RenderSettings.ambientLight = ambientWas;
        }
    }

    /// <summary>
    /// 주변광을 조금 <b>따뜻하게</b> 올린다. 점광원만으로는 빛이 닿는 데만 밝고 나머지는
    /// 새까매서 그게 제일 인공적이다 - 실제 방은 벽에 튕긴 빛으로 구석까지 은은하게 밝다.
    /// 유니티에는 실시간 반사광이 없으니 <b>주변광이 그 대역</b>이야.
    ///
    /// <see cref="CampusMood"/> 도 같은 값을 쓰는데 그쪽은 <b>수집품 수가 바뀔 때만</b> 쓴다.
    /// 배식하는 동안에는 수집이 안 일어나니 서로 안 싸운다.
    /// </summary>
    void LiftAmbient()
    {
        ambientModeWas = RenderSettings.ambientMode;
        ambientWas = RenderSettings.ambientLight;
        ambientSaved = true;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        // 원래 값에서 <b>올리기만</b> 한다. 절대값을 박으면 캠퍼스가 차가울 때(폐과)와
        // 따뜻할 때 중 한쪽에서 반드시 어색해진다.
        RenderSettings.ambientLight = Color.Lerp(ambientWas, new Color(0.72f, 0.66f, 0.56f), 0.5f);
    }

    // ---- 배식대 찾기 -------------------------------------------------------

    /// <summary>
    /// 이름으로 찾는다. 이 프로젝트는 이름 찾기를 피하는 게 원칙이지만
    /// (<see cref="BearNpc"/> 의 AutoBind 와 같은) <b>의도한 예외</b>다 -
    /// 인스펙터로 꽂으려면 씬을 다시 구워야 하고, 그게 이 미니게임이 피하려던 바로 그것이야.
    /// 측정: <c>InServeTop</c> 은 씬에 <b>1개</b>뿐이라 이름으로 찾아도 안전하다.
    ///
    /// 못 찾으면 <b>아무 것도 안 만든다</b>. 틀린 자리에 세우는 것보다 화면만 뜨는 게 낫다.
    /// </summary>
    static Transform FindCounter()
    {
        var found = GameObject.Find("InServeTop");
        if (found != null) return found.transform;

        Debug.LogWarning("[급식] 배식대(InServeTop)를 못 찾았다. 곰밥마당 밖에서 시작했거나 "
                       + "씬이 배식대보다 오래된 것이다. 손님과 카메라는 건너뛴다.");
        return null;
    }

    /// <summary>
    /// 맨 앞 손님 얼굴에 드는 빛. <b>등은 안 보여준다</b> - 약해서 어디서 오는지 아무도
    /// 안 묻고, 방의 갓등은 이미 <see cref="CanteenDressing"/> 가 달아 놨다.
    /// </summary>
    void FaceLamp()
    {
        var go = new GameObject("FaceLamp");
        go.transform.SetParent(fixtures, false);
        go.transform.position = SlotAt(0) + ToRoom * 1.2f + Vector3.up * 2.1f;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1.00f, 0.94f, 0.86f);
        light.intensity = 0.85f;
        light.range = 5.5f;
        light.shadows = LightShadows.None;
    }

    // ---- 배식대 위 그릇 ----------------------------------------------------

    public static readonly Color[] DishColors =
    {
        new Color32(0xFA, 0xF4, 0xE4, 0xFF),   // 밥   - 흰밥
        new Color32(0xC9, 0x8A, 0x4B, 0xFF),   // 국   - 된장빛
        new Color32(0xC4, 0x45, 0x3E, 0xFF),   // 김치 - 붉은색
        new Color32(0x6F, 0x9E, 0x5C, 0xFF),   // 반찬 - 나물
        new Color32(0xE6, 0xA8, 0xC0, 0xFF),   // 후식 - 분홍
    };

    /// <summary>
    /// 배식대 위에 그릇 다섯. <b>화면의 식판 칸과 같은 것이 방에도 있어야</b>
    /// 숫자키를 눌렀을 때 "저게 담긴 거구나" 가 눈으로 온다 - HUD 만 바뀌면 표 조작에 그친다.
    ///
    /// 여기가 유저의 음식 FBX 가 들어올 자리다. 지금은 그릇만 있고, 모델이 오면 위에 얹는다.
    /// </summary>
    void BuildBowls()
    {
        for (int i = 0; i < CanteenOrder.Slots; i++)
        {
            var root = new GameObject($"Bowl_{i}").transform;
            root.SetParent(fixtures, false);
            root.position = BowlAt(i);
            root.rotation = Quaternion.LookRotation(ToRoom, Vector3.up);

            // 놋그릇 - 받침은 늘 같은 색이라 담긴 것과 구분된다
            Ball(root, "Dish", Vector3.up * 0.045f, new Vector3(0.40f, 0.13f, 0.40f),
                 new Color32(0xB9, 0x9A, 0x5E, 0xFF), Finish.금속);

            // 내용물 - 이게 켜졌다 꺼졌다 한다
            var food = Ball(root, "Food", Vector3.up * 0.10f, new Vector3(0.30f, 0.12f, 0.30f),
                            DishColors[i], Finish.무광);

            bowlCalm[i] = FlatMaterial.Get(DishColors[i], Finish.무광);
            bowlLit[i] = FlatMaterial.Get(DishColors[i], Finish.발광);

            bowls.Add(root);
            bowlSkins.Add(food.GetComponent<Renderer>());
        }
    }

    /// <summary>
    /// 담긴 칸은 빛난다. 색만 바꾸면 비스듬한 각도에서 구분이 안 된다.
    /// <b>새로 담긴 칸은 시각을 적어 둔다</b> — 튀어오르는 건 <see cref="BounceBowls"/> 가 매 프레임 그린다.
    /// </summary>
    void SyncBowls()
    {
        if (game == null) return;

        for (int i = 0; i < bowls.Count; i++)
        {
            bool filled = (game.Tray & (1 << i)) != 0;
            bool was = (traySeen & (1 << i)) != 0;

            if (bowlSkins[i] != null)
                bowlSkins[i].sharedMaterial = filled ? bowlLit[i] : bowlCalm[i];

            // 방금 담긴 칸만 튄다. 이미 담겨 있던 칸까지 튀면 누를 때마다 전부 흔들린다.
            if (filled && !was) bowlPopAt[i] = Time.time;
        }
    }

    /// <summary>
    /// 그릇이 톡 튀어올랐다 내려앉는다. <b>«담았다» 를 만드는 건 색이 아니라 움직임이다</b> —
    /// 숫자키를 눌렀을 때 화면에서 물건이 움직여야 손으로 뜬 것으로 읽힌다.
    /// </summary>
    void BounceBowls()
    {
        const float pop = 0.34f;   // 튀는 시간

        for (int i = 0; i < bowls.Count; i++)
        {
            if (bowls[i] == null) continue;

            bool filled = (game.Tray & (1 << i)) != 0;
            float rest = filled ? 0.07f : 0f;

            float since = Time.time - bowlPopAt[i];
            float lift = rest;
            if (since >= 0f && since < pop)
            {
                // 위로 확 갔다가 제자리로. sin 한 번이면 «튀었다» 가 된다.
                float t = since / pop;
                lift = rest + Mathf.Sin(t * Mathf.PI) * 0.16f;
            }

            bowls[i].position = BowlAt(i) + Vector3.up * lift;
        }
    }

    // ---- 바닥 대기선 -------------------------------------------------------

    /// <summary>
    /// 줄 서는 자리를 바닥에 찍는다. <b>곰이 왜 거기 서 있는지</b>가 보여야 «줄» 이 되고,
    /// 안 그러면 다섯 마리가 우연히 일렬로 선 것처럼 보인다.
    ///
    /// <b>맨 앞 자리만 색을 다르게 한다.</b> 지금 받는 손님이 어느 칸인지가
    /// 화면에서 한눈에 잡혀야, 말풍선을 안 봐도 «저 곰» 이 된다.
    /// </summary>
    void BuildFloorMarks()
    {
        for (int i = 0; i <= Slots; i++)
        {
            bool front = i == 0;
            var mark = Slab(fixtures, $"Stand_{i}", Vector3.zero,
                            new Vector3(0.62f, 0.012f, 0.44f),
                            front ? new Color32(0xC4, 0x45, 0x3E, 0xFF)
                                  : new Color32(0x8E, 0x8A, 0x80, 0xFF));
            // 바닥에서 12mm — 딱 0 이면 마루와 같은 평면이라 지지직거린다(2026-09-17 규칙).
            mark.position = SlotAt(i) + Vector3.up * 0.012f;
            mark.rotation = Quaternion.LookRotation(ToRoom, Vector3.up);
        }
    }

    // ---- 말풍선 ------------------------------------------------------------

    /// <summary>
    /// 맨 앞 손님 머리 위 말풍선. 주문을 <b>그릇 색 점</b>으로 보여준다.
    ///
    /// 글자를 안 쓰는 이유: 주문은 이미 화면에 글자로 떠 있다. 여기까지 글자면 같은 말을
    /// 두 번 하는 거고, 무엇보다 <b>색 점은 배식대 그릇과 바로 이어져</b> 읽는 시간이 0 이다.
    /// </summary>
    void BuildBubble()
    {
        bubble = new GameObject("OrderBubble").transform;
        bubble.SetParent(fixtures, false);

        Ball(bubble, "Pad", Vector3.zero, new Vector3(0.78f, 0.34f, 0.10f), Color.white, Finish.발광);
        Ball(bubble, "Tail", new Vector3(-0.16f, -0.20f, 0f), new Vector3(0.12f, 0.12f, 0.08f),
             Color.white, Finish.발광);

        for (int i = 0; i < CanteenOrder.Slots; i++)
        {
            // ★ 로컬 +X 가 보는 사람의 <b>오른쪽</b>이다 - 말풍선의 forward 를
            // "카메라에서 멀어지는 쪽" 으로 잡아 카메라와 같은 방향을 보게 하기 때문이야.
            // (진열장 숫자가 거울상이던 건 그것들이 180도 돌아 카메라를 <b>마주 봐서</b>였다.)
            var dot = Ball(bubble, $"Dot_{i}", Vector3.zero, Vector3.one * 0.11f,
                           DishColors[i], Finish.무광);
            dot.localPosition = new Vector3(0f, 0f, -0.06f);   // 판보다 카메라 쪽으로
            bubbleDots.Add(dot);
        }
    }

    /// <summary>주문이 바뀌면 점을 다시 놓는다. 개수가 달라지니 <b>가운데 정렬</b>이어야 한다.</summary>
    void SyncBubble()
    {
        if (bubble == null || game == null) return;
        if (game.Queue.Count == 0) return;

        var order = game.Queue[0];

        int shown = 0;
        for (int i = 0; i < bubbleDots.Count; i++)
            if (order.Needs((Dish)i)) shown++;

        const float gap = 0.15f;
        float left = -(shown - 1) * 0.5f * gap;

        int n = 0;
        for (int i = 0; i < bubbleDots.Count; i++)
        {
            bool want = order.Needs((Dish)i);
            bubbleDots[i].gameObject.SetActive(want);
            if (!want) continue;

            bubbleDots[i].localPosition = new Vector3(left + n * gap, 0f, -0.06f);
            n++;
        }

        // 점이 많을수록 판이 넓어진다. 고정 폭이면 다섯 개짜리 주문이 판 밖으로 넘친다.
        var pad = bubble.Find("Pad");
        if (pad != null) pad.localScale = new Vector3(0.24f + shown * 0.17f, 0.30f, 0.08f);
    }

    /// <summary>말풍선은 맨 앞 손님을 따라다니고 늘 카메라를 본다.</summary>
    void MoveBubble()
    {
        if (bubble == null) return;

        Customer front = null;
        foreach (var c in customers)
            if (c.slot == 0) { front = c; break; }

        // 받고 나가는 순간과 다음 손님이 오는 사이에는 아무도 0번이 아니다. 그때는 숨긴다.
        // 마감 화면에서도 숨긴다 - 결과 패널 뒤로 주문이 떠 있으면 아직 받을 게 있어 보인다.
        bool show = game != null && game.Now == Canteen.Phase.진행
                 && front != null && front.body != null
                 && Vector3.Distance(front.body.position, SlotAt(0)) < 0.45f;

        if (bubble.gameObject.activeSelf != show) bubble.gameObject.SetActive(show);
        if (!show) return;

        bubble.position = front.body.position + Vector3.up * 1.52f;

        if (cam != null)
        {
            Vector3 away = bubble.position - cam.transform.position;
            away.y = 0f;   // 눕히지 않는다 - 위아래로 기울면 판이 찌그러져 보인다
            if (away.sqrMagnitude > 0.0001f)
                bubble.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }

    // ---- 카메라 ------------------------------------------------------------

    void BuildCamera()
    {
        previous = Camera.main;
        if (previous == null) previous = FindFirstObjectByType<Camera>();

        var go = new GameObject("CanteenCamera");
        cam = go.AddComponent<Camera>();

        // 배경색, 안개, 컬링 마스크를 그대로 물려받는다.
        // 새로 만들면 하늘이 유니티 기본값으로 돌아가서 이 화면만 딴 게임처럼 보인다.
        if (previous != null) cam.CopyFrom(previous);
        cam.fieldOfView = 42f;

        // 후처리를 안 켜면 색감이 달라진다(MuseumLook 이 카메라에 걸어주는 것과 같은 설정).
        var extra = go.GetComponent<UniversalAdditionalCameraData>();
        if (extra == null) extra = go.AddComponent<UniversalAdditionalCameraData>();
        extra.renderPostProcessing = true;
        extra.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        // AudioListener 는 안 단다. 둘이면 경고가 뜬다(StoryCamera 에서 세운 규칙과 같다).
        //
        // ★ <b>확 당겼다.</b> 전에는 5.4m 뒤에서 2.35m 높이로 내려다봤는데, 그러면
        // <b>방이 주인공</b>이 된다 — 30 × 19m 짜리 빈 바닥과 8.8m 천장이 화면의 절반이야.
        // 요리 게임 화면은 <b>작업대</b>가 주인공이다. 2.9m 뒤, 눈높이 1.75m 로 내려와서
        // 그릇 · 손님 · 차양이 한 화면에 꽉 차게 잡는다.
        go.transform.position = CounterFoot + ToRoom * 3.0f + Along * -2.0f + Vector3.up * 1.75f;
        go.transform.LookAt(CounterFoot + ToRoom * 0.95f + Along * 0.9f + Vector3.up * 1.0f);

        if (previous != null) previous.enabled = false;
    }

    // ---- 매 프레임 ---------------------------------------------------------

    void Update()
    {
        if (game == null || counter == null) return;

        // ENTER 로 다시 시작하면 Served 가 0 으로 돌아간다. 그때는 줄을 통째로 다시 세운다 -
        // 안 그러면 손님들이 지난 판의 자리에 그대로 서 있는다.
        if (game.Served < servedSeen)
        {
            servedSeen = game.Served;
            Restart();
        }
        else if (game.Served != servedSeen)
        {
            servedSeen = game.Served;
            Advance();
        }

        if (game.Tray != traySeen)
        {
            // ★ <b>SyncBowls 가 «직전 상태» 를 봐야</b> 방금 담긴 칸만 튀게 할 수 있다.
            // traySeen 을 먼저 갱신하면 전후가 같아져서 아무 것도 안 튄다.
            SyncBowls();
            traySeen = game.Tray;
        }

        // 손님이 바뀌면 말풍선 내용도 바뀐다.
        int order = game.Queue.Count > 0 ? game.Queue[0].Wanted : 0;
        if (order != orderSeen)
        {
            orderSeen = order;
            SyncBubble();
        }

        BounceBowls();
        Walk();
        MoveBubble();
        ReportArrival();
    }

    /// <summary>
    /// ★ <b>맨 앞 손님이 자리에 섰는지 규칙 쪽에 알려준다</b>(2026-09-22).
    /// 유저: *"곰돌이들이 떠나고 새 곰돌이들 오는 동안에도 ENTER 를 누르면 점수가 올라간다."*
    /// 화면에서 걸어오고 있는데 규칙만 먼저 다음 손님을 세워 두면 그런 일이 생긴다 —
    /// <b>보이는 것이 규칙이어야 한다.</b> 말풍선이 쓰는 잣대(0.45m)를 그대로 쓴다.
    /// </summary>
    void ReportArrival()
    {
        if (game == null) return;

        Customer front = null;
        foreach (var c in customers)
            if (c.slot == 0) { front = c; break; }

        bool arrived = front != null && front.body != null
                    && Vector3.Distance(front.body.position, SlotAt(0)) < 0.45f;
        game.SetFrontArrived(arrived);
    }

    /// <summary>
    /// ★★ <b>줄을 앞으로 당긴다</b>(2026-09-22).
    ///
    /// 유저: *"급식실 게임에 «다른 손님이 오는 중» 하고 곰인형들이 아예 오다가 멈춘다."*
    /// <b>교착이었다.</b> <see cref="Advance"/> 는 «받을 때마다» 슬롯을 하나씩 당기는데,
    /// 빨리 다섯 번 내보내면 <b>다섯이 전부 나가는 중(slot −1)</b>이 돼서 0번이 빈다.
    /// 그러면 «맨 앞 손님이 도착했나» 가 영영 false 고, 도착을 못 하니 받을 수도 없다 —
    /// 슬롯을 당기는 게 «받기» 뿐이라 <b>스스로 못 빠져나온다.</b>
    ///
    /// 그래서 <b>매 프레임 줄을 정리한다.</b> 서 있는 손님을 0, 1, 2… 로 다시 매기면
    /// 하나라도 줄에 있는 한 0번이 반드시 채워진다. 걷는 것은 `MoveTowards` 가 알아서 한다.
    ///
    /// <b>«받기» 에만 걸려 있던 일을 매 프레임 도는 곳으로 옮기는 것</b>이 이 프로젝트가
    /// 교착을 푸는 방식이다(문에 바닥값을 준 것 · 브리핑 카드의 25초 안전장치와 같다).
    /// </summary>
    void Compact()
    {
        inLine.Clear();
        foreach (var c in customers) if (c.slot >= 0) inLine.Add(c);
        inLine.Sort((a, b) => a.slot.CompareTo(b.slot));

        for (int i = 0; i < inLine.Count; i++)
        {
            if (inLine[i].slot == i) continue;
            inLine[i].slot = i;
            inLine[i].target = SlotAt(i);
        }
    }

    readonly List<Customer> inLine = new List<Customer>();

    void Walk()
    {
        Compact();
        float step = WalkSpeed * Time.deltaTime;

        foreach (var c in customers)
        {
            if (c.body == null) continue;

            c.body.position = Vector3.MoveTowards(c.body.position, c.target, step);

            // 자리까지 다 간 손님은 줄 맨 뒤로 돌려보낸다.
            // 매번 새로 만들고 지우면 쓰레기가 쌓인다 - 곰 하나가 열일곱 조각이라 싸지 않다.
            if (c.slot < 0 && Vector3.Distance(c.body.position, c.target) < 0.15f)
            {
                c.slot = Slots;
                c.body.position = SlotAt(Slots + 2);   // 식판 쌓인 쪽에서 걸어 들어온다
                c.target = SlotAt(Slots);
                FaceCounter(c.body);

                // 다시 줄을 서는 거니 식판은 내려놓은 셈이고, 표정도 돌아온다.
                if (c.tray != null) c.tray.gameObject.SetActive(false);
                SetSad(c, false);
            }

            Breathe(c);
        }
    }

    /// <summary>
    /// 숨쉬기와 깡충. <b>가만히 선 인형은 소품으로 보인다</b> - 아주 조금만 움직여도
    /// "기다리는 손님" 이 된다(<see cref="BearNpc"/> 가 로비에서 하는 것과 같은 판단).
    ///
    /// 자리(<c>body</c>)와 흔들림(<c>rig</c>)을 갈라 놨다. 한 트랜스폼에 둘을 같이 쓰면
    /// 걷는 동안 숨쉬기가 위치를 밀어서 목적지에 영영 못 닿는다.
    /// </summary>
    void Breathe(Customer c)
    {
        if (c.rig == null) return;

        float t = Time.time + c.phase;
        float puff = 1f + Mathf.Sin(t * 1.9f) * 0.018f;

        // 받고 나가는 순간 한 번 깡충. 고맙다는 표시가 있어야 배식이 응답으로 느껴진다.
        // ★ <b>딱 맞췄으면 더 높이 뛴다.</b> 판정이 HUD 글자로만 뜨면 화면을 봐야 알 수 있는데,
        // 손님이 크게 뛰면 <b>배식대를 보고 있어도</b> 잘했는지가 전달된다.
        float hop = 0f;
        float since = Time.time - c.hopAt;
        if (since >= 0f && since < 0.5f)
            hop = Mathf.Sin(since / 0.5f * Mathf.PI) * 0.16f * c.hopPower;

        // ★ 슬프면 <b>어깨가 처진다.</b> 표정만 바꾸면 멀리서 안 보이는데,
        // 몸이 조금 내려앉고 앞으로 숙으면 실루엣으로도 읽힌다.
        float slump = c.sad ? -0.05f : 0f;
        c.rig.localRotation = Quaternion.Euler(c.sad ? 9f : 0f, 0f, 0f);

        // 체격은 곰마다 다르다. 숨쉬기 배율에 곱해서 넣어야 둘이 안 싸운다.
        c.rig.localPosition = new Vector3(0f, hop + slump, 0f);
        c.rig.localScale = new Vector3(puff, 2f - puff, puff) * c.build;   // 부풀면 살짝 눌린다
    }

    /// <summary>줄을 처음 상태로. 다시 시작할 때만 쓴다.</summary>
    void Restart()
    {
        for (int i = 0; i < customers.Count; i++)
        {
            var c = customers[i];
            c.slot = i;
            c.target = SlotAt(i);
            c.hopAt = -99f;
            if (c.tray != null) c.tray.gameObject.SetActive(false);
            SetSad(c, false);
            if (c.body == null) continue;
            c.body.position = c.target;
            FaceCounter(c.body);
        }
        SyncBowls();
        orderSeen = -1;   // 다음 프레임에 말풍선을 다시 채우게 한다
    }

    void Advance()
    {
        foreach (var c in customers)
        {
            if (c.slot == 0)
            {
                c.slot = -1;
                c.target = ExitSpot;          // 식판 들고 반납대 쪽 자리로 간다
                c.hopAt = Time.time;

                // 판정을 몸으로 옮긴다. 딱 맞으면 껑충, 어긋나면 시늉만.
                bool perfect = game != null && game.Verdict != null && game.Verdict.StartsWith("딱 맞음");

                // ★★ <b>받은 것만 식판에 올린다</b>(2026-09-22).
                // 유저: *"밥과 김치만 원하는 곰돌이들이라도 채워질 때는 잔반 5개 꽉 채워진 것 같이
                // 보인다."* 맞다 — 전에는 다섯 개를 통째로 켜고 껐다. 그러면 <b>무엇을 줬든
                // 화면은 같은 그림</b>이라, 잘 담았는지 틀렸는지가 눈에 안 보인다.
                int got = game != null ? game.LastServedTray : 0;
                for (int i = 0; i < CanteenOrder.Slots; i++)
                    if (c.food[i] != null) c.food[i].gameObject.SetActive((got & (1 << i)) != 0);

                // ★ <b>빈 식판이면 슬픈 표정으로 나간다</b>(유저 요청).
                // 아무것도 못 받았는데 똑같이 깡충 뛰며 나가면 «그래도 됐구나» 로 읽힌다.
                SetSad(c, got == 0);
                c.hopPower = got == 0 ? 0f : (perfect ? 1.9f : 0.6f);

                // ★ <b>받은 게 눈에 남아야 한다.</b> 손님이 그냥 걸어가면 «내보냈다» 가
                // 화면에 안 보이고 점수만 오른다 - 식판을 들려 보내면 그 한 번이 결과가 된다.
                // 빈 식판도 <b>들려 보낸다</b> — 빈 판을 들고 가는 게 제일 또렷한 «못 받았다» 야.
                if (c.tray != null) c.tray.gameObject.SetActive(true);
            }
            else if (c.slot > 0)
            {
                c.slot--;
                c.target = SlotAt(c.slot);
            }
        }
    }

    /// <summary>
    /// <b>표정.</b> 눈썹을 켜고 눈을 가늘게 눌러서 «시무룩» 을 만든다.
    /// 조각을 더 만들지 않고 <b>이미 있는 것의 크기와 각도</b>만 바꾼다 —
    /// 손님이 다섯이라 새 조각을 얹으면 그대로 다섯 배다.
    /// </summary>
    static void SetSad(Customer c, bool sad)
    {
        if (c == null) return;
        c.sad = sad;

        for (int i = 0; i < 2; i++)
        {
            if (c.brow[i] != null) c.brow[i].gameObject.SetActive(sad);

            // 눈은 <b>납작하게</b>. 동그란 눈에 눈썹만 얹으면 화난 얼굴이 된다.
            if (c.eye[i] != null)
                c.eye[i].localScale = sad ? new Vector3(0.055f, 0.028f, 0.04f)
                                          : new Vector3(0.055f, 0.06f, 0.04f);
        }
    }

    void FaceCounter(Transform t)
    {
        Vector3 look = -ToRoom;
        look.y = 0f;
        if (look.sqrMagnitude > 0.0001f) t.rotation = Quaternion.LookRotation(look, Vector3.up);
    }

    // ---- 곰 손님 만들기 ----------------------------------------------------

    static readonly Color[] Coats =
    {
        new Color32(0xC8, 0x9F, 0x6E, 0xFF),   // 갈색
        new Color32(0xE2, 0xC9, 0xA8, 0xFF),   // 크림
        new Color32(0x8C, 0x6A, 0x4E, 0xFF),   // 진갈색
        new Color32(0xD9, 0xAE, 0xA0, 0xFF),   // 분홍빛
        new Color32(0xA8, 0x8C, 0x6C, 0xFF),   // 흐린 갈색
    };

    static readonly Color[] Scarves =
    {
        new Color32(0xC4, 0x45, 0x3E, 0xFF),
        new Color32(0x4E, 0x7A, 0x70, 0xFF),
        new Color32(0xD2, 0x88, 0x18, 0xFF),
        new Color32(0x6B, 0x7F, 0xB0, 0xFF),
        new Color32(0x9B, 0x6B, 0x8E, 0xFF),
    };

    /// <summary>
    /// <b>구와 캡슐로만 짓는다.</b> 유저: *"레고 느낌 나는데 블렌더 SHADE SMOOTH 같이 안 되나."*
    ///
    /// 유니티 Cube 에는 평활화를 걸 수 없다 - 큐브 메시는 면마다 노멀이 갈라져 있어서
    /// (정점 24개) 노멀을 평균내면 상자가 뭉개진 덩어리가 된다. 대신 <b>Sphere 와 Capsule 은
    /// 원래 부드럽게 셰이딩된 메시</b>라, 그걸로 짓는 게 여기서의 Shade Smooth 다.
    /// 곰인형은 어차피 둥근 물건이라 이쪽이 맞기도 하다.
    ///
    /// 조각 열일곱. 키 1.09m - 배식대 상판이 1.07 이라 <b>곰 머리가 상판 언저리</b>에 와서
    /// "받으러 온 손님" 으로 읽힌다.
    /// </summary>
    Customer MakeCustomer(int index)
    {
        var root = new GameObject($"Customer_{index}").transform;
        root.SetParent(herd, false);

        var rig = new GameObject("Rig").transform;
        rig.SetParent(root, false);

        Color coat = Coats[index % Coats.Length];
        Color snout = Color.Lerp(coat, Color.white, 0.42f);
        Color dark = new Color32(0x36, 0x2A, 0x22, 0xFF);
        Color scarf = Scarves[index % Scarves.Length];

        // 몸 - 아래가 볼록한 인형 체형. 완전한 구 하나는 공처럼 보인다.
        Ball(rig, "Hip", new Vector3(0f, 0.30f, 0f), new Vector3(0.52f, 0.46f, 0.44f), coat);
        Ball(rig, "Chest", new Vector3(0f, 0.52f, 0.01f), new Vector3(0.44f, 0.40f, 0.38f), coat);
        Ball(rig, "Belly", new Vector3(0f, 0.36f, 0.14f), new Vector3(0.28f, 0.26f, 0.20f), snout);

        // 목도리 - 손님마다 색이 달라야 <b>줄에 선 다섯이 다섯 명</b>으로 보인다.
        Ball(rig, "Scarf", new Vector3(0f, 0.63f, 0.01f), new Vector3(0.36f, 0.12f, 0.33f), scarf);

        // 머리
        Ball(rig, "Head", new Vector3(0f, 0.83f, 0.01f), new Vector3(0.42f, 0.40f, 0.40f), coat);
        Ball(rig, "Snout", new Vector3(0f, 0.775f, 0.165f), new Vector3(0.21f, 0.16f, 0.17f), snout);
        Ball(rig, "Nose", new Vector3(0f, 0.80f, 0.245f), new Vector3(0.08f, 0.06f, 0.05f), dark);

        var eyes = new Transform[2];
        var brows = new Transform[2];

        for (int s = -1; s <= 1; s += 2)
        {
            int e = (s + 1) / 2;   // −1 → 0, +1 → 1

            Ball(rig, $"Ear_{s}", new Vector3(s * 0.155f, 1.00f, 0.00f), new Vector3(0.17f, 0.17f, 0.11f), coat);
            Ball(rig, $"EarIn_{s}", new Vector3(s * 0.155f, 1.00f, -0.03f), new Vector3(0.09f, 0.09f, 0.08f), snout);
            eyes[e] = Ball(rig, $"Eye_{s}", new Vector3(s * 0.095f, 0.865f, 0.175f), new Vector3(0.055f, 0.06f, 0.04f), dark);

            // ★ 눈썹 — <b>평소에는 꺼져 있다.</b> 슬플 때만 켜서 바깥쪽이 내려간
            // «╲ ╱» 모양을 만든다. 표정을 바꾸는 제일 싼 방법이고, 입은 구로는 못 그린다
            // (곡선이 안 나온다) — 눈썹 각도가 훨씬 또렷하다.
            brows[e] = Slab(rig, $"Brow_{s}", new Vector3(s * 0.10f, 0.925f, 0.165f),
                            new Vector3(0.085f, 0.016f, 0.03f), dark);
            brows[e].localRotation = Quaternion.Euler(0f, 0f, s * 20f);
            brows[e].gameObject.SetActive(false);

            // 팔은 살짝 앞으로 - 식판을 받으려고 내민 자세
            Limb(rig, $"Arm_{s}", new Vector3(s * 0.27f, 0.47f, 0.05f),
                 new Vector3(s * 14f, 0f, s * 10f), new Vector2(0.15f, 0.30f), coat);
            Limb(rig, $"Leg_{s}", new Vector3(s * 0.13f, 0.13f, 0f),
                 Vector3.zero, new Vector2(0.18f, 0.26f), coat);
        }

        // ---- 받아 든 식판 ----
        // 팔 사이 높이(0.42)에 놓는다 - 팔이 앞으로 내밀어져 있어서 «받쳐 든» 것으로 읽힌다.
        // <b>줄에 서 있는 동안은 꺼져 있다.</b> 내보낸 뒤에만 켜서 결과를 눈에 남긴다.
        var tray = new GameObject("Tray").transform;
        tray.SetParent(rig, false);
        tray.localPosition = new Vector3(0f, 0.42f, 0.30f);

        Slab(tray, "TrayPlate", Vector3.zero, new Vector3(0.40f, 0.03f, 0.28f),
             new Color32(0xB9, 0x9A, 0x5E, 0xFF), Finish.금속);
        // 칸막이 한 줄 - 판때기 한 장은 식판으로 안 보인다
        Slab(tray, "TrayRib", new Vector3(0f, 0.02f, 0f), new Vector3(0.40f, 0.02f, 0.02f),
             new Color32(0x9A, 0x7E, 0x48, 0xFF), Finish.금속);

        // 담긴 것 — <b>칸마다 하나씩, 받은 것만 켠다.</b> 자리는 고정이라
        // «밥 · 김치» 를 받으면 1번과 3번 칸에만 올라간다. 다섯을 통째로 켜고 끄면
        // 무엇을 줬든 같은 그림이 되고, 그건 «잔반 꽉 찬 식판» 으로 읽힌다(유저 지적).
        var food = new Transform[CanteenOrder.Slots];
        for (int i = 0; i < CanteenOrder.Slots; i++)
        {
            food[i] = Ball(tray, $"TrayFood_{i}", new Vector3(-0.15f + i * 0.075f, 0.04f, 0.05f),
                           new Vector3(0.09f, 0.05f, 0.09f), DishColors[i]);
            food[i].gameObject.SetActive(false);
        }

        tray.gameObject.SetActive(false);

        return new Customer
        {
            body = root,
            rig = rig,
            tray = tray,
            food = food,
            brow = brows,
            eye = eyes,
            slot = index,
            phase = index * 1.7f,   // 다섯이 같이 숨 쉬면 기계처럼 보인다
            // ★ 체격도 어긋나게. 털색과 목도리만 다르면 <b>같은 곰을 다섯 번 칠한 것</b>으로 보인다 —
            // 크기가 섞여야 «다섯 마리» 가 된다(나무를 세 가지로 나눈 것과 같은 이유).
            build = Builds[index % Builds.Length],
        };
    }

    /// <summary>체격. ±8% 면 나란히 섰을 때 눈에 띄고 키 차이가 어색하진 않다.</summary>
    static readonly float[] Builds = { 1.00f, 0.92f, 1.08f, 0.96f, 1.04f };

    /// <summary>납작한 판 - 식판처럼 상자가 맞는 것에만.</summary>
    static Transform Slab(Transform parent, string name, Vector3 at, Vector3 size,
                          Color color, Finish finish = Finish.무광)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        return Dress(go, parent, name, at, Vector3.zero, size, color, finish);
    }

    // ---- 프리미티브 도우미 -------------------------------------------------

    /// <summary>구 하나. <b>부드럽게 셰이딩된 메시</b>라 모서리가 안 선다.</summary>
    static Transform Ball(Transform parent, string name, Vector3 at, Vector3 size,
                          Color color, Finish finish = Finish.무광)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        return Dress(go, parent, name, at, Vector3.zero, size, color, finish);
    }

    /// <summary>
    /// 캡슐 하나 - 팔다리. 캡슐 메시는 높이 2, 반지름 0.5 라
    /// <c>size = (굵기, 길이)</c> 를 받아 스케일로 바꿔 준다. 이렇게 안 하면 호출부마다
    /// 2로 나누는 계산이 흩어지고, 한 군데를 빠뜨리면 그 팔만 길어진다.
    /// </summary>
    static Transform Limb(Transform parent, string name, Vector3 at, Vector3 tilt,
                          Vector2 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        return Dress(go, parent, name, at, tilt,
                     new Vector3(size.x, size.y * 0.5f, size.x), color, Finish.무광);
    }

    static Transform Dress(GameObject go, Transform parent, string name, Vector3 at,
                           Vector3 tilt, Vector3 size, Color color, Finish finish)
    {
        go.name = name;

        // 콜라이더를 떼지 않으면 손님이 플레이어를 밀고 문을 막는다.
        // 눌린 캡슐/구 콜라이더는 엉뚱하게 부풀기까지 한다(CLAUDE.md).
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localRotation = Quaternion.Euler(tilt);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color, finish);
        return go.transform;
    }
}
