using UnityEngine;

/// <summary>
/// <b>위아래로 들썩인다.</b> 트랜스폼 하나만 움직이는 가장 싼 애니메이션.
///
/// ★★ <b>본(뼈)은 필요 없다</b> (2026-09-21 유저 질문: *"몸을 위아래로 들썩거리기를 원하는데
/// 그럼 본까지는 필요없나."*). 맞다 — 본이 필요한 건 <b>부위마다 다르게 움직일 때</b>다.
/// 몸 전체가 같이 오르내리는 동작은 <b>오브젝트 하나를 옮기면 끝</b>이고,
/// 이 프로젝트는 이미 전부 그렇게 한다:
///
/// <list type="bullet">
/// <item>카트가 코너에서 기우는 것 — 트랜스폼 회전</item>
/// <item>로비 곰의 숨쉬기 — 본이 있지만 «스케일» 은 트랜스폼</item>
/// <item>급식 손님의 깡충 — 트랜스폼 위치</item>
/// </list>
///
/// AnimationClip 도 안 만든다. 클립은 <b>트랜스폼 경로를 이름으로 기억</b>해서
/// 모델을 갈아끼우면 깨지는데(CLAUDE.md 규칙 4), 코드로 돌리면 그 함정이 없다.
///
/// <b>눌림(squash)을 같이 준다.</b> 위아래로만 움직이면 «엘리베이터» 로 보이고,
/// 올라갈 때 늘어나고 내려올 때 눌려야 <b>힘을 쓰는</b> 것으로 읽힌다.
///
/// ★ 2026-09-21 <b>기분을 셋으로 나눴다.</b> 유저: *"지금 너무 스파 마사지 받는 사람처럼
/// 둥둥 편안하게 뜨고 있는 것 같은데, 문 열 때마다 모션이 다르면 어때."* 맞는 지적이고,
/// 한 가지 리듬만 계속 돌면 그건 «움직임» 이 아니라 <b>화면 상태</b>가 된다
/// (주행 진동을 늘 켜두면 안 되는 것과 같은 이유).
///
/// ⚠ <b>앞발은 못 든다.</b> 이 곰은 본이 없는 단일 메시라 부위를 따로 못 움직인다.
/// 대신 <b>리듬 · 기울기 · 떨림 · 눌림</b> 넷으로 «힘주는 중» 과 «편한 중» 을 가른다 —
/// 실루엣이 바뀌는 게 아니라 <b>박자가 바뀌는</b> 연기다.
/// </summary>
public class BobMotion : MonoBehaviour
{
    public enum Mood
    {
        편안 = 0,   // 느리고 크게 둥실 — 다 끝난 사람
        힘주기 = 1, // 앞으로 숙이고 잘게 떤다. 가끔 크게 한 번
        참는중 = 2, // 거의 안 움직이다가 이따금 움찔
    }

    [Tooltip("지금 기분. 문이 열릴 때마다 다시 뽑는다")]
    public Mood mood = Mood.편안;

    [Tooltip("이 문이 «열림» 으로 바뀔 때마다 기분을 다시 뽑는다. 비우면 안 바뀐다")]
    public HingedDoor watchDoor;

    [Tooltip("한 번 들썩이는 데 걸리는 시간(초). 짧을수록 다급해 보인다")]
    public float period = 0.9f;

    [Tooltip("위아래 폭(m)")]
    public float rise = 0.045f;

    [Tooltip("눌리는 정도. 0 이면 위아래로만 움직인다")]
    public float squash = 0.05f;

    [Tooltip("여럿이 같이 있을 때 박자를 어긋나게. 0 이면 이름으로 알아서 흩는다")]
    public float phase;

    [Tooltip("가끔 한 번씩 크게. 0 이면 늘 같은 크기로 들썩인다")]
    public float surgeEvery = 5.5f;

    Vector3 baseLocalPos;
    Vector3 baseLocalScale;
    Quaternion baseLocalRot;
    bool doorWasOpen;
    float moodSince;

    // ★ <b>눌림은 «위쪽» 축에 줘야 한다.</b> 블렌더 FBX 는 축 변환 회전을 달고 오기 때문에
    // 모델의 로컬 Y 가 화면의 위가 아니다 — 이 곰은 로컬 Z 가 위다.
    // 그냥 localScale.y 를 누르면 <b>키가 아니라 깊이</b>가 눌려서 눌림이 안 보인다.
    int upAxis = 1;

    void Awake()
    {
        baseLocalPos = transform.localPosition;
        baseLocalScale = transform.localScale;

        // ★★ <b>임포트 회전을 덮어쓰지 않는다.</b> 블렌더 FBX 는 축 변환(−90° X)을 루트
        // 회전으로 들고 오는데, 그걸 지우면 <b>모델이 눕는다</b>(2026-09-21에 실제로 그랬다).
        // 기울임은 언제나 <b>원래 회전에 곱해서</b> 준다.
        baseLocalRot = transform.localRotation;

        // 박자를 안 주면 여럿이 한 몸처럼 움직인다 — 이름에서 흩는다(씬을 안 구워도 된다).
        if (Mathf.Approximately(phase, 0f))
            phase = Mathf.Abs(name.GetHashCode() % 997) / 997f * Mathf.PI * 2f;

        moodSince = -99f;

        Vector3 localUp = Quaternion.Inverse(baseLocalRot) * Vector3.up;
        localUp = new Vector3(Mathf.Abs(localUp.x), Mathf.Abs(localUp.y), Mathf.Abs(localUp.z));
        upAxis = localUp.x > localUp.y && localUp.x > localUp.z ? 0
               : (localUp.z > localUp.y ? 2 : 1);
    }

    /// <summary>기분을 바꾼다. 같은 게 연달아 나오면 «안 바뀌었다» 로 보여서 한 번 다시 뽑는다.</summary>
    public void Reroll()
    {
        var pick = (Mood)Random.Range(0, 3);
        if (pick == mood) pick = (Mood)(((int)pick + 1) % 3);
        mood = pick;
        moodSince = Time.time;
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        // 문이 «닫힘 → 열림» 으로 넘어가는 순간에만 다시 뽑는다.
        // 열려 있는 동안 계속 뽑으면 매 프레임 자세가 바뀌어서 고장으로 보인다.
        if (watchDoor != null)
        {
            if (watchDoor.Open && !doorWasOpen) Reroll();
            doorWasOpen = watchDoor.Open;
        }

        // 기분마다 박자 · 폭 · 기울기 · 떨림이 다르다. 숫자는 여기 한 군데에만 둔다.
        float per, amp, sq, lean, tremble, surgeGap, surgeSize;
        switch (mood)
        {
            case Mood.힘주기:
                // 잘고 빠르게 떨다가 가끔 크게 한 번. 앞으로 숙인다.
                per = period * 0.42f; amp = rise * 0.45f; sq = squash * 1.6f;
                lean = 7f; tremble = 0.006f; surgeGap = 2.6f; surgeSize = 2.2f;
                break;
            case Mood.참는중:
                // 거의 멈춰 있다가 이따금 움찔. <b>안 움직이는 시간</b>이 긴장을 만든다.
                per = period * 1.9f; amp = rise * 0.3f; sq = squash * 0.6f;
                lean = 3f; tremble = 0.002f; surgeGap = 4.2f; surgeSize = 3.4f;
                break;
            default:
                // 느리고 크게 둥실. 다 끝난 사람.
                per = period * 1.35f; amp = rise * 1.15f; sq = squash;
                lean = -3f; tremble = 0f; surgeGap = 6.5f; surgeSize = 1.2f;
                break;
        }

        float t = Time.time / Mathf.Max(0.05f, per) * Mathf.PI * 2f + phase;

        // 기본 들썩임. sin 을 그대로 쓰면 위아래가 같아서 «떠 있는» 느낌이라,
        // 제곱해서 <b>아래에 오래 머물고 위로 튄다</b> — 힘주는 동작은 그렇게 생겼다.
        float raw = (Mathf.Sin(t) + 1f) * 0.5f;
        float lift = raw * raw;

        // 가끔 한 번씩 크게. 늘 같은 크기로 움직이면 기계가 된다.
        float surge = 1f;
        if (surgeEvery > 0.1f && surgeGap > 0.1f)
        {
            float cycle = Mathf.Repeat(Time.time + phase, surgeGap);
            if (cycle < 0.55f) surge = 1f + Mathf.Sin(cycle / 0.55f * Mathf.PI) * surgeSize;
        }

        Vector3 offset = Vector3.up * (lift * amp * surge);

        // 떨림 — 펄린 노이즈라 매 프레임 튀지 않고 «부들부들» 로 읽힌다.
        if (tremble > 0.0001f)
        {
            float n = Time.time * 26f;
            offset.x += (Mathf.PerlinNoise(n, 0.3f) - 0.5f) * tremble * 2f * surge;
            offset.z += (Mathf.PerlinNoise(0.7f, n) - 0.5f) * tremble * 2f * surge;
        }

        transform.localPosition = baseLocalPos + offset;

        // 기울임은 <b>원래 회전에 곱한다.</b> 덮어쓰면 FBX 축 회전이 날아가서 곰이 눕는다.
        if (Mathf.Abs(lean) > 0.01f)
            transform.localRotation = baseLocalRot * Quaternion.Euler(lean * (0.6f + 0.4f * lift), 0f, 0f);

        if (sq > 0.0001f)
        {
            // 올라갈 때 늘고 내려올 때 눌린다. 부피가 유지되게 좌우도 반대로 준다.
            float s = 1f + (lift - 0.5f) * sq * surge;
            float wide = 1f / Mathf.Sqrt(s);
            var sc = new Vector3(baseLocalScale.x * wide, baseLocalScale.y * wide, baseLocalScale.z * wide);
            sc[upAxis] = baseLocalScale[upAxis] * s;
            transform.localScale = sc;
        }
    }
}
