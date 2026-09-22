using UnityEngine;

/// <summary>
/// <b>«지금 E 를 누르면 뭐가 잡히나» 를 한 자리에서 채점한다.</b>
///
/// 2026-09-22 유저: *"화장실 칸막이 문 열 때에도 계속 세면대 물 끄기만 보인다.
/// 오른쪽 칸막이 문 열고 싶은데 왼쪽 칸막이 문이 나와. 보는 시선에 따라 판단해 줘."*
///
/// 원인은 <b>거리만 봤기 때문</b>이다. 8 × 7m 짜리 화장실에서는 세면대·칸 문 둘·건물 문이
/// 전부 3m 안에 들어와서, 거리만으로는 <b>무엇을 보고 서 있든 같은 답</b>이 나온다.
/// 사람은 «가까운 것» 이 아니라 <b>«보고 있는 것»</b> 을 집으려고 한다.
///
/// 그래서 점수는 <b>거리 × 각도 벌점</b>이다:
/// <list type="bullet">
/// <item><b>거리</b>는 물건의 <b>제일 가까운 면</b>까지 — 문은 폭이 2m 라 경첩에서 재면
///       오른쪽 끝에 서 있을 때 2m 를 손해 본다(2026-09-21 에 이미 한 번 고쳤다).</item>
/// <item><b>각도</b>는 물건의 <b>한가운데</b>까지 — 가장 가까운 면으로 재면 넓은 물건의
///       모서리를 가리키게 돼서 정면으로 보고 있어도 «옆» 으로 잡힌다.</item>
/// </list>
///
/// <b>등지고 있으면 아예 안 잡힌다</b>(<see cref="MaxAngle"/>). 뒤통수로 문을 여는 것은
/// 편한 게 아니라 <b>왜 이게 잡혔는지 모르는 것</b>이고, 후보가 넷이나 되는 방에서는
/// 그게 곧 «패널이 제멋대로» 로 읽힌다.
///
/// 점수는 <b>종류가 달라도 비교할 수 있다</b> — 수도꼭지·칸 문·건물 문이 같은 자로 재야
/// <see cref="CampusHUD"/> 가 «무엇을 띄울지» 를 고를 수 있다. 전에는 종류에 순서를
/// 박아 놔서(수도꼭지가 문보다 먼저) <b>물을 잠글 때까지 문을 못 열었다.</b>
/// </summary>
public static class Reach
{
    /// <summary>이 각도를 넘으면 «등지고 있다» 로 본다.</summary>
    public const float MaxAngle = 85f;

    /// <summary>
    /// 각도 벌점의 세기. 40 이면 <b>40도 벗어날 때마다 거리가 두 배로 세진다.</b>
    /// 1m 앞 정면(1.0)이 2m 앞 40도(4.0)를 이기고, 같은 거리면 정면이 항상 이긴다.
    /// </summary>
    const float AngleCost = 40f;

    /// <summary>
    /// 잡을 수 있으면 true 와 점수(<b>작을수록 앞</b>). <paramref name="near"/> 는 거리를 잴
    /// 제일 가까운 점, <paramref name="center"/> 는 각도를 잴 한가운데다.
    /// </summary>
    public static bool Score(Transform who, Vector3 near, Vector3 center, float range, out float score)
    {
        score = float.MaxValue;
        if (who == null) return false;

        float distance = Vector3.Distance(who.position, near);
        if (distance > range) return false;

        // 바로 코앞이면 각도가 의미가 없다 — 몸 안에 든 물건은 어느 쪽으로 서든 «그것» 이다.
        Vector3 toward = center - who.position;
        toward.y = 0f;   // 위아래는 안 본다. 발밑 배수구와 천장등이 각도로 밀려나면 이상하다
        float angle = toward.sqrMagnitude < 0.09f ? 0f : Vector3.Angle(Flat(who.forward), toward);
        if (angle > MaxAngle) return false;

        score = distance * (1f + angle / AngleCost);
        return true;
    }

    /// <summary>점 하나짜리 물건 — 거리와 각도를 같은 점에서 잰다.</summary>
    public static bool Score(Transform who, Vector3 at, float range, out float score)
        => Score(who, at, at, range, out score);

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < 0.0001f ? Vector3.forward : v.normalized;
    }
}
