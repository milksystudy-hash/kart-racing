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
    /// <summary>
    /// ★★ 2026-09-22 유저: *"모션이 두 개밖에 없는 것 같다. 느릿한 움직임과 완전 빠른
    /// 움직임인데 <b>무슨 동작인지 구분이 안 되어 있다.</b>"* 맞는 지적이다.
    ///
    /// 전에는 셋이 <b>속도와 폭만</b> 달랐다. 그러면 «빠름/느림» 두 가지로만 읽히고
    /// «지금 뭘 하는 중인가» 는 안 보인다 — 진동을 늘 켜두면 그게 진동이 아니라
    /// 화면 상태가 되는 것과 같은 문제야.
    ///
    /// 그래서 <b>축을 갈랐다.</b> 셋이 서로 다른 방향으로 움직인다:
    ///
    /// | | 주로 쓰는 축 | 실루엣 |
    /// |---|---|---|
    /// | 힘주기 | <b>앞뒤 숙임</b> + 잘고 빠른 떨림 | 웅크린 채 부들부들, 가끔 크게 한 번 |
    /// | 한숨 | <b>위아래</b> 크고 느리게 + 뒤로 젖힘 | 천천히 부풀었다 꺼진다 |
    /// | 두리번 | <b>좌우 회전</b> | 몸은 가만, 고개만 왔다갔다. 가끔 흠칫 |
    ///
    /// 세 축(앞뒤 · 위아래 · 좌우)이 다르면 <b>한 프레임만 봐도</b> 어느 동작인지 갈린다.
    /// </summary>
    public enum Mood
    {
        한숨 = 0,   // 다 끝난 사람 — 크게 숨을 돌린다
        힘주기 = 1, // 웅크리고 부들부들. 가끔 크게 한 번
        두리번 = 2, // 몸은 가만, 좌우로 살핀다. 가끔 흠칫
    }

    [Tooltip("지금 동작. 문이 열릴 때마다 다시 뽑는다")]
    public Mood mood = Mood.한숨;

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

        // 동작마다 <b>쓰는 축이 다르다.</b> 숫자는 여기 한 군데에만 둔다.
        //   per      한 번 도는 데 걸리는 시간
        //   amp      위아래 폭(m)
        //   pitch    앞뒤 숙임(도). +가 앞으로
        //   yaw      좌우 회전 폭(도)
        //   yawPer   좌우 회전 주기
        //   tremble  잘게 떠는 폭(m)
        float per, amp, sq, pitch, yaw, yawPer, tremble, surgeGap, surgeSize;
        switch (mood)
        {
            case Mood.힘주기:
                // <b>앞으로 깊게 숙인 채</b> 잘고 빠르게 떤다. 좌우로는 안 움직인다 —
                // 힘주는 사람은 한 자세로 굳어 있지 둘러보지 않는다.
                per = period * 0.38f; amp = rise * 0.35f; sq = squash * 1.8f;
                pitch = 15f; yaw = 0f; yawPer = 1f; tremble = 0.007f;
                surgeGap = 2.4f; surgeSize = 2.4f;
                break;

            case Mood.두리번:
                // <b>몸은 거의 가만있고 좌우로만</b> 돈다. 위아래를 죽여야 «두리번» 으로 읽힌다.
                per = period * 2.4f; amp = rise * 0.12f; sq = squash * 0.35f;
                pitch = 0f; yaw = 26f; yawPer = 2.3f; tremble = 0f;
                surgeGap = 3.6f; surgeSize = 2.8f;   // 가끔 흠칫
                break;

            default:
                // <b>위아래로 크게</b> 부풀었다 꺼진다. 뒤로 살짝 젖히고 아주 느리게 갸웃.
                per = period * 1.6f; amp = rise * 1.4f; sq = squash * 1.2f;
                pitch = -5f; yaw = 6f; yawPer = 5.5f; tremble = 0f;
                surgeGap = 7f; surgeSize = 0.9f;
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

        // ★★ <b>회전은 «부모 기준» 으로 앞에 곱한다.</b>
        // 뒤에 곱하면(`base * tilt`) 모델의 <b>제 축</b>으로 도는데, 이 FBX 는 축 변환(−90° X)을
        // 달고 와서 로컬 Y 가 위가 아니다 — 좌우로 돌리려던 게 옆으로 넘어간다.
        // 앞에 곱하면 X = 앞뒤 숙임 · Y = 좌우 · Z = 갸웃으로 <b>보이는 그대로</b>다.
        float nod  = pitch * (0.55f + 0.45f * lift);
        float turn = yaw > 0.01f
                   ? Mathf.Sin(Time.time / yawPer * Mathf.PI * 2f + phase * 0.7f) * yaw
                   : 0f;
        transform.localRotation = Quaternion.Euler(nod, turn, 0f) * baseLocalRot;

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
