using UnityEngine;

/// <summary>
/// 물건이 <b>사라지고 나타나는 방식</b> 하나. 이 프로젝트의 전후 연출은 전부
/// <c>SetActive</c> 토글이라 <b>한 프레임에 툭</b> 바뀌었다 —
/// 유저(2026-09-28): *"연출 좀 신경써줘. 우리가 만든 건 너무 아마추어 같아."*
///
/// 맞는 말이고, 상용 게임과 갈리는 건 <b>무엇이 바뀌느냐가 아니라 어떻게 바뀌느냐</b>다.
/// 셋을 고친다:
///
/// <list type="number">
/// <item><b>스냅이 아니라 움직임.</b> 사라질 때 가라앉으며 오므라들고, 나타날 때
///       위에서 내려오며 <b>살짝 넘어갔다 돌아온다</b>(ease-out-back). 눈은
///       «없어졌다» 보다 <b>«치워졌다»</b> 를 훨씬 잘 읽는다.</item>
/// <item>★ <b>순서.</b> 열두 개가 같은 프레임에 바뀌면 «설정을 만졌다» 지만,
///       0.1초씩 어긋나면 <b>«지금 일어나는 일»</b> 이 된다. 셋 중 이게 제일 크게 듣는다.</item>
/// <item><b>씬을 열 때는 스냅.</b> 들어가자마자 물건이 우수수 튀어나오면 연출이 아니라
///       <b>로딩이 덜 된 것</b>으로 보인다. 처음 상태는 <see cref="Snap"/> 로 박는다.</item>
/// </list>
///
/// <b>재질은 건드리지 않는다</b> — 발광 면은 `.mat` 에셋이라 런타임에 색을 쓰면
/// 디스크 파일이 바뀌어 다른 씬까지 따라간다(<see cref="GalleryLights"/> 에서 정한 규칙).
/// 크기와 자리만 움직인다.
///
/// ⚠ <b>정적 배칭이 걸린 물건은 트랜스폼을 옮겨도 화면이 안 바뀐다</b>
/// (문을 여섯 번 고치고서야 잡은 것, 2026-09-18). 그런 물건은 스스로 알아채고
/// <b>순서만 지킨 채 제자리에서 켜고 끈다</b> — 움직임은 없어도 «하나씩» 은 남는다.
/// </summary>
[DisallowMultipleComponent]
public class Reveal : MonoBehaviour
{
    public const float ShowSeconds = 0.44f;
    public const float HideSeconds = 0.30f;

    /// <summary>순서를 줄 때 쓰는 기본 간격. 이보다 촘촘하면 «한꺼번에» 로 보인다.</summary>
    public const float Step = 0.11f;

    bool show;
    float begin, span, sink;
    Vector3 baseScale, basePos;
    bool batched;

    // ---- 부르는 쪽 ---------------------------------------------------------

    /// <summary>애니메이션 없이 그 상태로 박는다. <b>씬을 열 때는 반드시 이쪽.</b></summary>
    public static void Snap(Transform target, bool visible)
    {
        if (target == null) return;

        var r = target.GetComponent<Reveal>();
        if (r != null) { r.Restore(); Destroy(r); }

        if (target.gameObject.activeSelf != visible) target.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 움직이며 바꾼다. <paramref name="delay"/> 로 순서를 준다.
    /// 이미 그 상태면 아무 일도 안 한다 — 매 프레임 불러도 안전하다.
    /// </summary>
    /// <param name="sink">가라앉거나 내려오는 거리(m). 큰 물건일수록 크게.</param>
    public static void Play(Transform target, bool visible, float delay = 0f, float sink = 0.8f)
    {
        if (target == null) return;

        var r = target.GetComponent<Reveal>();

        // 이미 그 상태로 끝나 있으면 건드리지 않는다
        if (r == null && target.gameObject.activeSelf == visible) return;
        if (r != null && r.show == visible) return;

        // 끌 때든 켤 때든 <b>Update 가 돌아야</b> 하니 일단 켠다
        if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);

        if (r == null) r = target.gameObject.AddComponent<Reveal>();
        else r.Restore();

        r.Begin(visible, delay, sink);
    }

    // ---- 안쪽 ---------------------------------------------------------------

    void Begin(bool visible, float delay, float dist)
    {
        show = visible;
        sink = dist;
        span = visible ? ShowSeconds : HideSeconds;
        begin = Time.time + Mathf.Max(0f, delay);

        baseScale = transform.localScale;
        basePos = transform.localPosition;

        // 합쳐진 메시는 아무리 옮겨도 그려지는 자리가 안 바뀐다. 확인해 두고 포기한다.
        batched = false;
        foreach (var rend in GetComponentsInChildren<Renderer>(true))
            if (rend.isPartOfStaticBatch) { batched = true; break; }

        Frame(0f);
    }

    void Update()
    {
        float p = span <= 0f ? 1f : Mathf.Clamp01((Time.time - begin) / span);
        if (Time.time < begin) p = 0f;

        Frame(p);
        if (p < 1f) return;

        Restore();
        if (!show) gameObject.SetActive(false);
        Destroy(this);
    }

    void Frame(float p)
    {
        if (batched) return;   // 움직여도 안 보인다 — 순서만 지키고 기다린다

        // 켜질 때는 <b>끝에서 살짝 넘어갔다 돌아온다.</b> 그 한 번의 되돌아옴이
        // «놓였다» 를 «떨어졌다» 로 바꾼다. 꺼질 때는 반대로 끝이 빨라야 «치워졌다» 다.
        float e = show ? Back(p) : p * p;
        float k = show ? e : 1f - e;

        transform.localScale = baseScale * Mathf.Lerp(0.04f, 1f, k);
        transform.localPosition = basePos + Vector3.up * (show ? (1f - e) * sink : -e * sink * 0.45f);
    }

    /// <summary>
    /// ease-out-back — 1 을 <b>2.7% 만</b> 넘었다가 돌아온다.
    ///
    /// ★ 두 계수는 <b>`c3 = c1 + 1` 이어야 p = 0 에서 정확히 0 이 된다.</b>
    /// 2.10 / 1.38 로 뒀다가 <c>Back(0) = 0.28</c> 이 나왔다 — 그러면 켜질 때
    /// <b>28% 크기로 툭 나타난 뒤</b> 커지는 거라 «등장» 이 아니라 «끊긴 것» 으로 보인다.
    /// 교과서값(1.70158)은 이 크기에서 너무 튀어서 1.10 으로 낮췄다.
    /// </summary>
    static float Back(float p)
    {
        float u = p - 1f;
        return 1f + 2.10f * u * u * u + 1.10f * u * u;
    }

    void Restore()
    {
        if (baseScale == Vector3.zero) return;
        transform.localScale = baseScale;
        transform.localPosition = basePos;
    }

    void OnDisable() => Restore();
}
