using UnityEngine;

/// <summary>
/// ★★★ <b>물건을 바닥에 앉힌다.</b> (2026-10-15)
///
/// ── 왜 한 군데로 모았나 ────────────────────────────────────────────────
/// 수연이 «자재가 공중에 떠 있다» 를 <b>세 번</b> 신고했고, 세 번 다 같은 함정이었다:
///
/// <code>
/// Physics.Raycast(내위치 + up * 5, down, ...)     ← 제 콜라이더를 맞는다
///   → floor.point.y 가 «바닥» 이 아니라 <b>제 윗면</b>
///   → 거기에 반높이를 또 더하니 <b>제 키만큼 솟는다</b>
///   → 되돌릴 때마다 그 높이로 돌아가니 영영 떠 있다
/// </code>
///
/// 위에서 아래로 쏘는 광선은 <b>언제나 자기 자신을 먼저 맞는다.</b> 이건 좌표를
/// 조절해서 될 일이 아니라 <b>자기를 빼고 재야</b> 하는 일이다. 그래서 그 «빼기» 를
/// 여기 한 군데에 두고, <see cref="RoadDebris"/> 와 <c>SkyDrop</c> 이 같이 쓴다.
///
/// ── 왜 «반높이» 가 아니라 «바닥까지의 거리» 인가 ────────────────────────
/// <c>bounds.extents.y</c> 는 <b>원점이 메시 한가운데에 있을 때만</b> 맞는다.
/// 유저가 만든 FBX 는 원점이 바닥인 것도 있고 가운데인 것도 있어서
/// (밥그릇은 바닥에서 −1.58m 였다), 반높이로 올리면 그만큼 묻히거나 뜬다.
/// <b>지금 원점에서 메시 맨 밑까지가 얼마인지</b>를 재면 원점이 어디 있든 맞는다.
/// </summary>
public static class Ground
{
    /// <summary>레이캐스트가 무시할 레이어. 카트가 쓰는 Ignore Raycast(2)만 뺀다.</summary>
    const int Mask = ~(1 << 2);

    /// <summary>
    /// <paramref name="at"/> 아래의 바닥 높이. ★ <paramref name="self"/> 와 그 자식들의
    /// 콜라이더는 <b>건너뛴다</b> — 그게 이 함수의 존재 이유다.
    /// </summary>
    public static bool FloorUnder(Vector3 at, Transform self, out float y, float up = 8f, float depth = 60f)
    {
        y = at.y;

        var hits = Physics.RaycastAll(at + Vector3.up * up, Vector3.down, depth, Mask);
        if (hits.Length == 0) return false;

        // 가까운 것부터 본다 — RaycastAll 은 순서를 보장하지 않는다.
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (self != null && h.collider.transform.IsChildOf(self)) continue;   // 나 자신
            y = h.point.y;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 원점에서 <b>메시 맨 밑</b>까지의 거리. 원점이 바닥이면 0, 한가운데면 반높이.
    /// 렌더러가 없으면 <paramref name="fallback"/>.
    /// </summary>
    public static float BottomOffset(Transform t, float fallback = 0.45f)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return fallback;

        var box = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) box.Encapsulate(renderers[i].bounds);

        return Mathf.Max(0f, t.position.y - box.min.y);
    }

    /// <summary>
    /// <paramref name="t"/> 를 제 바로 아래 바닥에 앉힌 좌표. 바닥을 못 찾으면
    /// <paramref name="want"/> 를 그대로 — <b>없는 것보다 낫다.</b>
    /// </summary>
    public static Vector3 Sit(Transform t, Vector3 want, float sink = 0f)
    {
        float lift = BottomOffset(t);
        if (!FloorUnder(want, t, out float floor)) return want;
        return new Vector3(want.x, floor + lift - sink, want.z);
    }
}
