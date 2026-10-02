using UnityEngine;

/// <summary>
/// 2바퀴째 <b>도로가 부분적으로 물에 잠긴다.</b> 2026-10-01 유저:
/// *"솔직히 말하면 난 도로 자체가 부분적으로 물이 잠겨 오르는 느낌을 상상하긴 했어."*
///
/// <b>그쪽이 맞다.</b> 처음엔 물기둥이 솟게 했는데, 그건 «길 위에 놓인 장애물» 이라
/// 결국 발판·자재와 같은 종류다. 잠긴 도로는 <b>길 자체가 달라지는 것</b>이라
/// 「같은 코스를 세 번 돌아 지루하다」에 정확히 답한다 — 2바퀴에는 <b>다른 길</b>을 달린다.
///
/// 한 구간이 세 겹이다. 이 프로젝트에서 «레고 같다» 를 벗는 방법은 늘 같았다 —
/// <b>물건을 늘리는 게 아니라 면을 겹치는 것</b>(굽도리 + 턱, 모서리 기둥).
///
/// | 겹 | 무엇 | 왜 |
/// |---|---|---|
/// | 젖은 노면 | 늘 깔려 있는 어두운 판 | <b>어디가 잠기는지 미리 보인다.</b> 예고 없는 물은 함정이다 |
/// | 물 | 올라왔다 내려가는 반투명 판 둘 | 한 장이면 «파란 판때기», 두 장이 어긋나야 «수면» 이다 |
/// | 포말 | 가장자리 밝은 띠 | 물과 마른 길의 <b>경계</b>가 보여야 피할 수 있다 |
///
/// ★ <b>속도를 깎되 튕기지 않는다.</b> 물을 지나가면 느려질 뿐이고, 깊을수록 더 느려진다 —
/// 날려 보내면 «맞은 쪽이 레이스를 포기» 하게 된다(2026-09-17 에 정한 선).
/// ★ <b>벽 부딪힘으로 안 센다.</b> 트리거라 충돌이 아예 안 일어난다 — 3판·8판이 물 때문에
/// 실패하면 안 된다.
/// </summary>
public class FloodZone : MonoBehaviour
{
    [Tooltip("물이 차고 빠지는 한 주기")]
    public float period = 7.5f;

    [Tooltip("구간마다 다르게 — 전부 같이 차면 «수영장» 이지 «길» 이 아니다")]
    public float phase;

    [Tooltip("제일 깊을 때")]
    public float deep = 0.30f;

    [Tooltip("다 빠져도 이만큼은 남는다 — 길이 젖어 있다는 표시")]
    public float shallow = 0.02f;

    [Tooltip("제일 깊을 때 1초에 남는 속도 비율. 1 이면 안 느려진다")]
    [Range(0.3f, 1f)] public float keepPerSecond = 0.70f;

    [Header("조각")]
    public Transform surface;      // 수면 — 위아래로 움직인다
    public Transform underLayer;   // 한 겹 아래 — 반 박자 어긋나게
    public GameObject foam;        // 가장자리 포말

    float baseY;
    float underY;

    /// <summary>지금 물 깊이(m).</summary>
    public float Depth { get; private set; }

    void Awake()
    {
        if (surface != null) baseY = surface.localPosition.y;
        if (underLayer != null) underY = underLayer.localPosition.y;
    }

    void Update()
    {
        if (RacePause.On) return;

        // ★ 차오르는 건 <b>빠르게</b>, 빠지는 건 천천히. 유저: *"올라오는 속도도 느리니까 좀 잡아주고."*
        //   사인파로 올리면 «느리게 차서 느리게 빠지는» 숨쉬기가 되고, 그건 길이 아니라 배경이다.
        //   차는 데 30%, 머무는 데 25%, 빠지는 데 45% 를 쓴다.
        float t = Mathf.Repeat((Time.time + phase) / Mathf.Max(1f, period), 1f);
        float k;
        if (t < 0.30f) k = Ease(t / 0.30f);                    // 차오른다
        else if (t < 0.55f) k = 1f;                            // 머문다
        else k = 1f - Ease((t - 0.55f) / 0.45f);               // 빠진다

        Depth = Mathf.Lerp(shallow, deep, k);

        if (surface != null)
        {
            var p = surface.localPosition;
            // 수면이 잘게 흔들린다 — 딱 멈춰 있으면 «판» 이고 흔들려야 «물» 이다
            float ripple = Mathf.Sin(Time.time * 2.3f + phase) * 0.012f * k;
            surface.localPosition = new Vector3(p.x, baseY + Depth + ripple, p.z);
        }
        if (underLayer != null)
        {
            var p = underLayer.localPosition;
            float ripple = Mathf.Sin(Time.time * 1.7f + phase + 2.1f) * 0.016f * k;
            underLayer.localPosition = new Vector3(p.x, underY + Depth * 0.72f + ripple, p.z);
        }
        if (foam != null && foam.activeSelf != (k > 0.12f)) foam.SetActive(k > 0.12f);
    }

    static float Ease(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - (1f - x) * (1f - x);     // 빠르게 붙었다 부드럽게 멎는다
    }

    void OnTriggerStay(Collider other)
    {
        if (Depth < 0.05f) return;
        var body = other.attachedRigidbody;
        if (body == null) return;
        var kart = body.GetComponent<KartController>();
        if (kart == null) return;

        // 얕으면 거의 안 느려지고 깊을수록 끈다 — 깊이가 곧 대가다
        float bite = Mathf.InverseLerp(shallow, deep, Depth);
        kart.Wade(Mathf.Lerp(1f, keepPerSecond, bite));
    }
}
