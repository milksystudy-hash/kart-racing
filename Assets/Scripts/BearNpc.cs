using UnityEngine;

/// <summary>
/// 박물관 곰인형 NPC — 숨 쉬고, 가끔 고개를 갸웃하고, 팔을 흔들고, 가까이 가면 쳐다본다.
///
/// <b>AnimationClip 을 안 쓴다.</b> 뼈 트랜스폼을 코드로 돌릴 뿐이야.
/// 클립은 오브젝트 경로에 묶여 있어서 모델을 다시 뽑으면 조용히 깨진다(CLAUDE.md 4번 규칙).
/// 카트가 코너에서 기우는 것도 같은 방식이고, 이 프로젝트엔 아직 클립이 하나도 없다.
///
/// <b>축은 실제로 재서 정했다</b>(2026-09-16, 유니티에 넣고 30도씩 돌려 끝점 이동을 측정):
///   머리 — 로컬 X 끄덕 · 로컬 Y 좌우 보기 · 로컬 Z 갸웃
///   팔   — 로컬 X 위아래 · 로컬 Y 는 뼈 길이 방향이라 아무 일도 안 일어난다
/// 블렌더에서 Arm_L 이던 팔이 유니티에서는 <b>-X 쪽</b>에 온다(축 변환 때문). 이름만 그럴 뿐
/// 좌우 대칭으로 다루니 문제는 없어.
/// </summary>
public class BearNpc : MonoBehaviour
{
    [Header("뼈 (비워두면 이름으로 한 번 찾아서 채운다)")]
    public Transform body;
    public Transform head;
    public Transform armLeft;
    public Transform armRight;

    [Header("숨쉬기")]
    [Tooltip("몸통이 오르내리는 높이(m). 곰인형이라 아주 조금만")]
    public float breathHeight = 0.015f;
    public float breathSpeed = 1.5f;

    [Header("고개 갸웃")]
    // 2026-09-17 유저: "곰들의 볼이 찌그러지게 움직여서 신경이 쓰인다."
    // 머리 본 하나에 볼까지 물려 있어서 많이 돌리면 얼굴이 눌린다. 자동 웨이트의 한계라
    // 스키닝을 다시 칠하지 않는 한 <b>덜 돌리는 것</b>이 답이다 — 15도는 인형에 과했다.
    public float tiltDegrees = 7f;
    [Tooltip("갸웃하는 간격(초) 최소·최대")]
    public Vector2 tiltEvery = new Vector2(5f, 11f);
    public float tiltHold = 1.6f;

    [Header("팔 흔들기")]
    public float waveDegrees = 30f;
    public float waveSpeed = 7f;
    [Tooltip("흔드는 간격(초) 최소·최대. 가까이 오면 바로 한 번 흔든다")]
    public Vector2 waveEvery = new Vector2(7f, 15f);
    public float waveHold = 1.8f;

    [Header("쳐다보기")]
    [Tooltip("비워두면 메인 카메라를 본다")]
    public Transform lookTarget;
    [Tooltip("이 거리 안에 들어오면 쳐다보고, 처음 들어온 순간 손을 흔든다")]
    public float noticeRange = 5f;
    [Tooltip("고개를 좌우로 최대 몇 도까지 돌릴지")]
    public float maxTurn = 34f;
    public float turnSpeed = 4f;

    Quaternion bodyRest, headRest, armLeftRest, armRightRest;
    Vector3 bodyRestPosition;

    float tiltAt, tiltUntil, tiltSign = 1f;
    float waveAt, waveUntil;
    float yaw;
    bool noticed;
    bool ready;

    void Awake()
    {
        AutoBind();

        if (body != null)
        {
            bodyRest = body.localRotation;
            bodyRestPosition = body.localPosition;
        }
        if (head != null) headRest = head.localRotation;
        if (armLeft != null) armLeftRest = armLeft.localRotation;
        if (armRight != null) armRightRest = armRight.localRotation;

        // 같은 자리에 여럿 세워두면 전부 같은 박자로 숨쉬어서 기계처럼 보인다.
        float offset = Random.value * 10f;
        tiltAt = Time.time + offset;
        waveAt = Time.time + offset + 2f;

        StartPatrol();

        ready = head != null || body != null;
        if (!ready)
            Debug.LogWarning("[곰인형] 뼈를 못 찾았어. FBX 안의 Head/Body/Arm_L/Arm_R 을 " +
                             "인스펙터에 끌어다 넣어줘.", this);
    }

    /// <summary>
    /// 인스펙터가 비어 있을 때만 이름으로 찾는다. <b>채워져 있으면 손대지 않는다</b> —
    /// 이름으로 찾는 걸 기본으로 삼으면 모델 이름만 바꿔도 조용히 망가진다(CLAUDE.md).
    /// 여기서는 FBX 를 갓 끌어다 놨을 때 곧바로 움직이는 게 더 중요해서 예외로 뒀어.
    /// </summary>
    void AutoBind()
    {
        if (body != null && head != null && armLeft != null && armRight != null) return;

        bool found = false;
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (body == null && t.name.EndsWith("Body")) { body = t; found = true; }
            else if (head == null && t.name.EndsWith("Head")) { head = t; found = true; }
            else if (armLeft == null && t.name.EndsWith("Arm_L")) { armLeft = t; found = true; }
            else if (armRight == null && t.name.EndsWith("Arm_R")) { armRight = t; found = true; }
        }

        if (found)
            Debug.Log("[곰인형] 뼈를 이름으로 찾아서 채웠어. 인스펙터에 한 번 저장해두면 " +
                      "나중에 모델 이름이 바뀌어도 안전해.", this);
    }

    void Update()
    {
        if (!ready) return;

        float now = Time.time;
        Transform target = lookTarget != null ? lookTarget
                         : (Camera.main != null ? Camera.main.transform : null);

        bool near = target != null &&
                    (target.position - transform.position).sqrMagnitude < noticeRange * noticeRange;

        // 처음 다가온 순간 한 번 반갑게 — 계속 흔들면 인사가 아니라 고장 난 인형이다
        if (near && !noticed)
        {
            noticed = true;
            waveUntil = now + waveHold;
        }
        else if (!near)
        {
            noticed = false;
        }

        Patrol(now);
        OfferTalk(target, target != null ? Vector3.Distance(target.position, transform.position) : 999f);

        Breathe(now);
        Look(target, near);
        Tilt(now);
        Wave(now);
    }


    // ==================================================================
    //  순찰 + 혼잣말  ★ 임시 (2026-09-16). 지울 때 BearLines.cs 와 같이 걷어낸다
    // ==================================================================
    [Header("순찰 ★임시")]
    [Tooltip("끄면 제자리에 서 있는다")]
    public bool patrol = true;
    [Tooltip("처음 선 자리에서 이만큼 안에서만 돌아다닌다(m)")]
    public float patrolRadius = 4.5f;
    public float walkSpeed = 0.7f;
    public float turnToWalkSpeed = 3f;
    [Tooltip("한 곳에 도착해서 쉬는 시간(초)")]
    public Vector2 restEvery = new Vector2(1.5f, 4f);

    [Header("말 걸기 ★임시")]
    [Tooltip("이 거리 안이면 '말 걸기' 표시가 뜬다(m)")]
    public float talkRange = 3.5f;

    [Tooltip("조종하는 사람이 없을 때 — 카메라가 이 각도 안으로 보고 있으면 말을 걸 수 있다")]
    public float lookAngle = 16f;

    [Tooltip("조종하는 사람이 없을 때 — 이 거리 안이어야 한다")]
    public float lookDistance = 32f;

    [Tooltip("돌아다닐 때 다른 물건과 이만큼은 떨어져 있어야 한다(m)")]
    public float clearance = 1.1f;

    Vector3 home, walkTarget, lastPlace;
    float restUntil, blockedFor;
    bool walking;

    void StartPatrol()
    {
        home = transform.position;
        walkTarget = home;
        lastPlace = home;
        restUntil = Time.time + Random.Range(restEvery.x, restEvery.y);
    }

    void Patrol(float now)
    {
        if (!patrol) return;

        if (!walking)
        {
            if (now < restUntil) return;
            walkTarget = PickSpot();
            walking = true;
            return;
        }

        Vector3 flat = walkTarget - transform.position;
        flat.y = 0f;

        if (flat.sqrMagnitude < 0.09f)
        {
            walking = false;
            restUntil = now + Random.Range(restEvery.x, restEvery.y);
            return;
        }

        transform.position += flat.normalized * walkSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              Quaternion.LookRotation(flat, Vector3.up),
                                              1f - Mathf.Exp(-turnToWalkSpeed * Time.deltaTime));

        // 실제로 나아가고 있나. 뭔가에 막히면 위치가 안 변한다 —
        // 로비에서 곰 한 마리가 곰 조각상 안에 갇혀 있었다(2026-09-16).
        if ((transform.position - lastPlace).sqrMagnitude < 0.0001f) blockedFor += Time.deltaTime;
        else { blockedFor = 0f; lastPlace = transform.position; }

        if (blockedFor > 0.8f)
        {
            blockedFor = 0f;
            walkTarget = home;          // 막히면 일단 처음 자리로 돌아간다
            if ((home - transform.position).sqrMagnitude < 0.25f) walking = false;
        }
    }

    /// <summary>
    /// 처음 선 자리 둘레에서 <b>비어 있는</b> 곳을 고른다. 반경만 보고 아무 데나 고르면
    /// 그 안에 조각상이나 받침대가 있을 때 그리로 걸어가 박힌다.
    /// 몇 번 찾아보고 다 막혀 있으면 그냥 제자리에 선다 — 억지로 가느니 서 있는 게 낫다.
    /// </summary>
    Vector3 PickSpot()
    {
        // 여러 번 찔러본다. 반경이 넓어질수록 막힌 자리를 뽑을 확률도 같이 오른다.
        for (int tries = 0; tries < 24; tries++)
        {
            Vector2 r = Random.insideUnitCircle * patrolRadius;
            Vector3 spot = home + new Vector3(r.x, 0f, r.y);
            if (IsClear(spot) && FarFromOtherBears(spot)) return spot;
        }
        return transform.position;
    }

    [Tooltip("다른 곰과 이만큼은 떨어져서 걷는다(m)")]
    public float bearSpacing = 7f;

    /// <summary>
    /// 곰끼리 붙어 다니지 않게. 유저: *"곰돌이들이 너무 붙어 있는 것 같다."*
    ///
    /// 자리를 처음 잡을 때는 6.7m 씩 벌려 놨는데, <b>걸을 때는 서로를 안 보고 있었다</b> —
    /// 셋이 같은 구석으로 걸어가면 거기서 뭉친다. 콜라이더로는 안 잡힌다(닿기 전에 이미
    /// 붙어 보이니까). 갈 자리를 고를 때 미리 본다.
    /// </summary>
    bool FarFromOtherBears(Vector3 spot)
    {
        foreach (var other in FindObjectsByType<BearNpc>(FindObjectsSortMode.None))
        {
            if (other == this) continue;

            // 상대가 걸어가는 <b>목적지</b>와도 비교한다. 지금 자리만 보면 둘이 같은 곳으로
            // 걸어가는 중일 때 못 걸러낸다.
            if ((other.transform.position - spot).sqrMagnitude < bearSpacing * bearSpacing) return false;
            if (other.walking && (other.walkTarget - spot).sqrMagnitude < bearSpacing * bearSpacing) return false;
        }
        return true;
    }

    /// <summary>
    /// 그 자리에 나 말고 다른 게 있나.
    ///
    /// <b>콜라이더만 보면 안 된다.</b> 석등 같은 장식은 콜라이더가 없어서 "비었다" 로 나오고,
    /// 곰이 그 위나 밑에 가서 낀다(2026-09-17 유저 제보 — 리본 곰 아래 석상에 자주 갇혔다).
    /// 자리 잡을 때와 <b>같은 규칙</b>이야: 막히는 것만 피하면 되는 게 아니라 겹쳐 보이지도 않아야 한다.
    /// </summary>
    bool IsClear(Vector3 spot)
    {
        var hits = Physics.OverlapSphere(spot + Vector3.up * 0.5f, clearance);
        foreach (var h in hits)
        {
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (h.isTrigger) continue;
            // 바닥은 당연히 걸린다. 위에서 눌러 만든 바닥판은 넓고 낮으니 높이로 가린다.
            if (h.bounds.max.y < 0.2f) continue;
            return false;
        }

        var box = new Bounds(spot + Vector3.up * 0.5f, new Vector3(clearance * 2f, 1f, clearance * 2f));
        foreach (var solid in Solids())
            if (solid.Intersects(box)) return false;

        return true;
    }

    // 눈에 보이는 장애물 목록. 한 번만 모아 둔다 — 매번 씬을 훑으면 곰 셋이서 비싸진다.
    static Bounds[] solids;

    static Bounds[] Solids()
    {
        if (solids != null) return solids;

        var list = new System.Collections.Generic.List<Bounds>();
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.GetComponentInParent<BearNpc>() != null) continue;   // 곰끼리는 콜라이더로 본다

            var b = r.bounds;
            if (b.max.y < 0.35f) continue;                      // 바닥·줄눈
            if (b.min.y > 2.6f) continue;                       // 천장·들보·현판
            if (b.size.x < 0.3f && b.size.z < 0.3f) continue;   // 실오라기 같은 것
            if (b.size.x > 25f || b.size.z > 25f) continue;     // 벽 한 장 통째 — 콜라이더가 이미 있다
            list.Add(b);
        }
        solids = list.ToArray();
        return solids;
    }

    /// <summary>걸을 때 팔을 번갈아 흔든다. 팔이 가만히 있으면 미끄러지는 것처럼 보인다.</summary>
    float WalkSwing(float now) => walking ? Mathf.Sin(now * walkSpeed * 9f) * 18f : 0f;

    // ------------------------------------------------------------------
    //  말 걸기 — 동물의 숲처럼 <b>버튼을 눌러야</b> 말한다
    // ------------------------------------------------------------------
    /// <summary>
    /// 가까이 갔다고 저절로 떠들면 지나갈 때마다 말이 튀어나와서 금방 시끄러워진다.
    /// 유저 요청(2026-09-16)대로 <b>말 걸 수 있다는 표시만 띄우고</b>, 누르면 그때 말한다.
    ///
    /// 표시는 <b>제일 가까운 한 마리<\/b>에게만 뜬다. 셋이 몰려 있을 때 누구한테 거는 건지
    /// 헷갈리면 안 되니까.
    /// </summary>
    public static BearNpc Nearest { get; private set; }

    /// <summary>이번에 말한 줄. 말풍선 대신 화면 알림으로 띄운다.</summary>
    public string LastLine { get; private set; } = "";

    static float nearestDistance;
    static int nearestFrame = -1;

    /// <summary>매 프레임 "내가 제일 가까운가" 를 겨룬다. 제일 가까운 놈만 표시를 얻는다.</summary>
    /// <summary>
    /// 말 걸 상대 고르기.
    ///
    /// <b>로비에는 조종하는 사람이 없다.</b> 궤도 카메라뿐이라 카메라와 곰 사이가 늘 5~25m 고,
    /// 3.5m 라는 거리 조건은 <b>영영 안 맞는다</b> — 그래서 말이 안 걸렸다(2026-09-17 유저 제보).
    ///
    /// 걸어다니는 아바타가 있으면 거리로 고르는 게 맞지만, 궤도 카메라에서는
    /// <b>"보고 있는 것"</b> 이 곧 "가까이 간 것"이다. 화면 가운데에 둔 곰에게 말을 건다.
    /// </summary>
    void OfferTalk(Transform target, float distance)
    {
        if (nearestFrame != Time.frameCount)
        {
            nearestFrame = Time.frameCount;
            nearestDistance = float.MaxValue;
            Nearest = null;
        }

        if (target == null) return;

        // 아바타가 있으면 거리로, 없으면(궤도 카메라) 화면 가운데에 가까운 순서로 고른다.
        float score;
        if (lookTarget != null)
        {
            if (distance > talkRange) return;
            score = distance;
        }
        else
        {
            Vector3 toMe = transform.position + Vector3.up * 0.7f - target.position;
            float angle = Vector3.Angle(target.forward, toMe);
            if (angle > lookAngle || toMe.magnitude > lookDistance) return;
            score = angle;
        }

        if (score >= nearestDistance) return;
        distance = score;

        nearestDistance = distance;
        Nearest = this;
    }

    /// <summary>말을 건다. 대사 한 줄을 뱉고, 말한 쪽을 쳐다보며 손을 든다.</summary>
    public void Talk()
    {
        LastLine = BearLines.Random();
        Toast.Show(LastLine);
        waveUntil = Time.time + waveHold;

        // 말 거는 동안은 안 돌아다닌다. 말하다 말고 걸어가면 이상해.
        walking = false;
        restUntil = Time.time + 2.5f;
    }

    void Breathe(float now)
    {
        if (body == null) return;
        float t = Mathf.Sin(now * breathSpeed);
        body.localPosition = bodyRestPosition + Vector3.up * (t * 0.5f + 0.5f) * breathHeight;
    }

    /// <summary>가까이 오면 고개를 그쪽으로 돌린다. 몸은 안 돌린다 — 인형이 발을 떼면 무섭다.</summary>
    void Look(Transform target, bool near)
    {
        if (head == null) return;

        float wanted = 0f;
        if (near && target != null)
        {
            Vector3 local = transform.InverseTransformPoint(target.position);
            wanted = Mathf.Clamp(Mathf.Atan2(local.x, Mathf.Max(0.01f, local.z)) * Mathf.Rad2Deg,
                                 -maxTurn, maxTurn);
        }
        yaw = Mathf.Lerp(yaw, wanted, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    void Tilt(float now)
    {
        if (head == null) return;

        if (now >= tiltAt)
        {
            tiltUntil = now + tiltHold;
            tiltSign = Random.value < 0.5f ? -1f : 1f;
            tiltAt = now + Random.Range(tiltEvery.x, tiltEvery.y);
        }

        // 0 -> 1 -> 0 으로 한 번 부드럽게 갔다 온다. 각도를 툭 넣으면 목이 부러진 것처럼 보인다
        float k = tiltUntil > now ? Mathf.Sin((1f - (tiltUntil - now) / tiltHold) * Mathf.PI) : 0f;
        float tilt = tiltDegrees * tiltSign * Mathf.SmoothStep(0f, 1f, k);

        // 로컬 Y 가 좌우, 로컬 Z 가 갸웃 — 재서 확인한 값이다
        head.localRotation = headRest * Quaternion.Euler(0f, yaw, tilt);
    }

    void Wave(float now)
    {
        if (now >= waveAt)
        {
            waveUntil = now + waveHold;
            waveAt = now + Random.Range(waveEvery.x, waveEvery.y);
        }

        float k = waveUntil > now ? (waveUntil - now) / waveHold : 0f;
        float envelope = Mathf.Sin(Mathf.Clamp01(1f - k) * Mathf.PI);   // 서서히 들었다 내린다
        float swing = Mathf.Sin(now * waveSpeed) * waveDegrees * envelope;

        // 한쪽만 흔든다. 양팔을 같이 흔들면 인사가 아니라 만세가 된다.
        // 걸을 때는 양팔이 번갈아 — 인사 스윙 위에 얹는다
        float step = WalkSwing(now);
        if (armRight != null) armRight.localRotation = armRightRest * Quaternion.Euler(swing + step, 0f, 0f);
        if (armLeft != null)  armLeft.localRotation  = armLeftRest * Quaternion.Euler(swing * 0.15f - step, 0f, 0f);
    }
}
