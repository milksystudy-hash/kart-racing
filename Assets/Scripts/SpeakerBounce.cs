using UnityEngine;

/// <summary>
/// <b>출발 신호가 떨어질 때 스피커 귀가 «떡처럼» 한 번 튕긴다.</b>
///
/// 2026-09-22 유저: *"레이스 시작하고 «경기 시작» 을 눌러서 트랙 씬으로 나가는 장면에서
/// 귀가 움직여야지, 지금 평소에 움직이고 있잖아. 심지어 두더지 게임처럼 움직이잖아.
/// 나는 떡처럼 한 번 튕겼다가 쫀득하게 돌아오는 연출을 원하고 있는데. 게다가 귀 양쪽
/// 균형도 안 맞는 것 같아."* — <b>세 가지가 전부 맞는 지적이었고, 원인도 셋이었다.</b>
///
/// <list type="number">
/// <item><b>늘 움직였다.</b> 박자에 맞춰 계속 흔들면 그건 «연출» 이 아니라 <b>화면 상태</b>가
/// 되고, 정작 출발할 때 아무 차이가 없다. 카트 주행 진동을 0 으로 내린 것과 같은 판단이야
/// (2026-09-17 «진동은 사건일 때만»). 이제 <see cref="StartGate.CountingDown"/> 이
/// 켜지는 <b>그 순간에만</b> 한 번 튕긴다.</item>
///
/// <item><b>귀 둘이 반 박자씩 어긋나 있었다</b>(<c>phase + i * 0.5</c>). 번갈아 오르내리니
/// 정확히 두더지 게임이고, 좌우 균형이 안 맞아 보이는 것도 당연했다. 이제 <b>두 귀가
/// 똑같이</b> 움직인다 — 같은 순간에 같은 만큼.</item>
///
/// <item>★ <b>귀 오브젝트의 원점이 스피커 바닥에 있었다.</b> 측정값:
/// <c>EarL 월드 (-5.50, 0.00, 10.10)</c> — 귀가 바닥 높이에 있다고 나온다. FBX 안에서
/// 귀 메시가 <b>제 원점을 안 갖고</b> 모델 원점을 그대로 쓰기 때문이야. 그 상태로
/// <c>localScale</c> 을 만지면 <b>바닥을 기준으로</b> 늘어나서 귀가 하늘로 날아간다.
/// 게다가 귀의 <b>자기 기준 위쪽은 Z</b>(측정: <c>(0,0,1)</c>)라 <c>localScale.y</c> 는
/// 아예 엉뚱한 축이었다.
/// <br/>
/// → 귀마다 <b>바운즈 한가운데에 빈 통</b>을 하나 두고 그 통을 움직인다. 카트 앞바퀴를
/// <c>_Pivot</c> 으로 감싼 것과 같은 방식이다.</item>
/// </list>
///
/// <b>«떡» 은 사인파가 아니다.</b> 사인파로 오르내리면 «바람에 흔들린다» 고,
/// <b>한 번 크게 튕겼다가 점점 잦아들어야</b> 쫀득한 것이 된다. 그래서
/// <c>sin(2πft) × e^(−dt)</c> 를 쓴다 — 0 에서 시작해 위로 튀고, 내려오면서 살짝 눌리고,
/// 두어 번 출렁이다 멎는다. 위로 뛸 때는 <b>가늘고 길게</b>, 눌릴 때는 <b>납작하고 넓게</b>
/// 만들어야 고무 같은 덩어리로 읽힌다(스쿼시 앤 스트레치).
/// </summary>
public class SpeakerBounce : MonoBehaviour
{
    [Tooltip("띠용거릴 귀들. 빌더가 채운다")]
    public Transform[] ears;

    [Tooltip("이 출발문이 카운트다운에 들어가면 한 번 튕긴다. 비우면 스스로 찾는다")]
    public StartGate gate;

    [Tooltip("튀어 오르는 높이(m)")]
    public float hop = 0.14f;

    [Tooltip("늘었다 눌리는 정도. 0.3 이면 최대 30% 늘어난다")]
    public float squash = 0.3f;

    [Tooltip("출렁이는 빠르기(Hz). 낮을수록 느긋하게 한 번 크게 튕긴다")]
    public float wobble = 1.45f;

    [Tooltip("잦아드는 속도. 높을수록 빨리 멎는다")]
    public float settle = 2.6f;

    [Tooltip("이 시간이 지나면 완전히 제자리(초)")]
    public float duration = 1.7f;

    Transform[] pivots;
    Vector3[] home;
    Vector3[] homeScale;
    float since = float.MaxValue;
    bool wasCounting;

    void Start()
    {
        if (gate == null) gate = FindFirstObjectByType<StartGate>();
        if (ears == null || ears.Length == 0) return;

        pivots = new Transform[ears.Length];
        home = new Vector3[ears.Length];
        homeScale = new Vector3[ears.Length];

        for (int i = 0; i < ears.Length; i++)
        {
            if (ears[i] == null) continue;

            // ★ 통은 <b>귀가 실제로 보이는 자리</b>(렌더러 바운즈 한가운데)에 둔다.
            // 오브젝트 좌표를 믿으면 안 된다 — 이 FBX 는 귀 원점이 모델 바닥에 있다.
            var r = ears[i].GetComponent<Renderer>();
            Vector3 at = r != null ? r.bounds.center : ears[i].position;

            var pivot = new GameObject($"{ears[i].name}_Pivot").transform;
            pivot.SetParent(ears[i].parent, false);
            pivot.position = at;
            pivot.rotation = Quaternion.identity;   // 제 Y 가 <b>월드 위쪽</b>이 되게
            ears[i].SetParent(pivot, true);

            // 움직일 물건이라 정적 배칭에 들어가면 안 된다 — 트랜스폼은 움직이는데
            // 그려지는 자리가 안 바뀐다(2026-09-18 캠퍼스 문에서 여섯 번 헤맨 그것).
            pivot.gameObject.isStatic = false;
            ears[i].gameObject.isStatic = false;

            pivots[i] = pivot;
            home[i] = pivot.position;
            homeScale[i] = pivot.localScale;
        }
    }

    /// <summary>바깥에서도 한 번 튕길 수 있게. 나중에 «음악이 시작될 때» 같은 데 쓴다.</summary>
    public void Thump() => since = 0f;

    void Update()
    {
        if (pivots == null) return;

        // 출발 카운트다운이 <b>시작되는 순간</b> 한 번. 켜져 있는 내내가 아니라 그 순간이다.
        bool counting = gate != null && gate.CountingDown;
        if (counting && !wasCounting) Thump();
        wasCounting = counting;

        if (since > duration) return;
        since += Time.deltaTime;

        // 0 에서 시작해 위로 크게 튀고, 내려오며 눌리고, 잦아든다.
        float e = Mathf.Sin(since * wobble * 2f * Mathf.PI) * Mathf.Exp(-settle * since);

        for (int i = 0; i < pivots.Length; i++)
        {
            if (pivots[i] == null) continue;

            pivots[i].position = home[i] + Vector3.up * (hop * e);

            // 위로 뛸 때 가늘고 길게, 눌릴 때 납작하고 넓게. 부피를 지키면 고무로 보인다.
            float s = 1f + squash * e;
            float w = 1f / Mathf.Sqrt(Mathf.Max(0.2f, s));
            pivots[i].localScale = new Vector3(homeScale[i].x * w,
                                               homeScale[i].y * s,
                                               homeScale[i].z * w);
        }

        if (since > duration)
            for (int i = 0; i < pivots.Length; i++)
                if (pivots[i] != null)
                {
                    pivots[i].position = home[i];
                    pivots[i].localScale = homeScale[i];
                }
    }
}
