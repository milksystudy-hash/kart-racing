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
    public float tiltDegrees = 15f;
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
    public float maxTurn = 55f;
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

        Breathe(now);
        Look(target, near);
        Tilt(now);
        Wave(now);
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
        if (armRight != null) armRight.localRotation = armRightRest * Quaternion.Euler(swing, 0f, 0f);
        if (armLeft != null)  armLeft.localRotation  = armLeftRest * Quaternion.Euler(swing * 0.15f, 0f, 0f);
    }
}
