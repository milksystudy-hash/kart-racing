using UnityEngine;

/// <summary>
/// ★ <b>책 모형 앞에 서면 안내가 뜬다.</b> (2026-10-13)
///
/// 제작자: *"다가가면 B 버튼 떠서 상호작용할 수 있게."*
///
/// 여는 일은 <see cref="StoryAlbum"/> 이 한다 — 이건 <b>«여기 그 책이 있다» 를 알려주는
/// 표지판</b>일 뿐이다. 그래서 <c>B</c> 는 멀리서도 먹고(발바닥 버튼과 같은 키),
/// 가까이 오면 <b>책 옆에서도</b> 보인다.
///
/// ★ 키를 여기서 또 듣지 않는다. 한 키를 두 군데서 들으면 한 번 눌러 둘이 반응한다 —
///   접수대 시계에서 이미 겪은 자리다(2026-09-22).
/// </summary>
public class AlbumStand : MonoBehaviour
{
    /// <summary>이 거리 안에 들어오면 안내가 뜬다.</summary>
    public float range = 3.2f;

    /// <summary>지금 책 앞에 서 있나. 로비 HUD 가 이걸 보고 안내를 그린다.</summary>
    public static bool Near { get; private set; }

    Transform visitor;

    void Update()
    {
        var who = Walker();
        Near = who != null && Vector3.Distance(who.position, transform.position) <= range;
    }

    Transform Walker()
    {
        if (visitor != null && visitor.gameObject.activeInHierarchy) return visitor;
        var fp = Object.FindFirstObjectByType<FirstPersonController>();
        visitor = fp != null && fp.gameObject.activeInHierarchy ? fp.transform : null;
        return visitor;
    }

    void OnDisable() => Near = false;
}
