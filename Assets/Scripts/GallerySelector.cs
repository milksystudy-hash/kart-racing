using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 전시실에서 마우스로 진열장을 고른다. 로비와 같은 방식이라 조작을 새로 배울 게 없다.
///
/// 카메라를 돌리려고 끄는 중에는 반응하지 않는다 — 그 판단은 LobbyOrbitCamera 가 해준다.
/// </summary>
public class GallerySelector : MonoBehaviour
{
    public Camera galleryCamera;
    public LobbyOrbitCamera orbit;

    [Tooltip("비워두면 씬에서 알아서 다 찾는다")]
    public GalleryCase[] cases;

    /// <summary>커서가 가리키는 진열장.</summary>
    public GalleryCase Hovered { get; private set; }
    /// <summary>클릭해서 설명을 펼쳐 둔 진열장.</summary>
    public GalleryCase Opened { get; private set; }

    public int CollectedCount { get; private set; }
    public int TotalCount => cases != null ? cases.Length : 0;

    void Awake()
    {
        if (cases == null || cases.Length == 0)
            cases = FindObjectsByType<GalleryCase>(FindObjectsSortMode.None);

        if (galleryCamera == null) galleryCamera = Camera.main;
        if (orbit == null && galleryCamera != null) orbit = galleryCamera.GetComponent<LobbyOrbitCamera>();

        RecountCollected();
    }

    void Update()
    {
        // ★★ 2026-10-02 유저: *"아이템을 돌리려고 마우스를 패널 밖으로 끌면,
        //   커서가 지나가는 진열장마다 «클릭해서 보기» 가 뜬다."*
        //   <b>«고르기» 보다 먼저 막아야 한다.</b> 전에는 Hovered 를 먼저 구하고
        //   클릭만 막았는데, 그러면 <b>이름표는 계속 바뀌었다.</b>
        //   지금 마우스를 쓰는 건 뷰어라 <b>이 프레임엔 아무것도 안 가리킨 것</b>으로 둔다.
        if (UiFocus.MouseOverPanel)
        {
            if (Hovered != null)
            {
                Hovered = null;
                foreach (var c in cases) if (c != null) c.SetHighlighted(false);
            }
            return;
        }

        Hovered = (orbit != null && orbit.IsDragging) ? null : CaseUnderCursor();

        foreach (var c in cases)
            if (c != null) c.SetHighlighted(c == Hovered);

        bool clicked = orbit != null
            ? orbit.ClickedWithoutDragging
            : (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame);

        if (clicked) Opened = Hovered;   // 빈 곳을 클릭하면 설명이 닫힌다
    }

    GalleryCase CaseUnderCursor()
    {
        if (galleryCamera == null || Mouse.current == null) return null;

        Vector2 screen = Mouse.current.position.ReadValue();
        if (screen.x < 0f || screen.y < 0f || screen.x > Screen.width || screen.y > Screen.height)
            return null;

        Ray ray = galleryCamera.ScreenPointToRay(screen);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<GalleryCase>();
    }

    /// <summary>수집 상태가 바뀐 뒤에 부르면 진열장들이 다시 칠해진다.</summary>
    public void RecountCollected()
    {
        CollectedCount = 0;
        foreach (var c in cases)
        {
            if (c == null) continue;
            c.Refresh();
            if (c.IsCollected) CollectedCount++;
        }
    }
}
