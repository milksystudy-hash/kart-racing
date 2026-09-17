using UnityEngine;

/// <summary>
/// 씬이 시작할 때 조작 방식을 정한다 — 카트가 있으면 타고, 없으면 걷는다.
///
/// 트랙 씬은 카트만, Testbed 씬은 걷기만 쓴다. 한 씬에서 둘을 오가지 않는 이유는
/// 조작이 섞이면 헷갈리기 때문 — 레이스 중에 마우스 시점과 점프가 끼어들면 안 된다.
/// </summary>
public class PlayerModeSwitcher : MonoBehaviour
{
    [Header("걸어다니기")]
    public FirstPersonController player;
    public Camera playerCamera;

    [Header("카트 (없으면 비워둬도 됨)")]
    public KartController kart;
    public KartCamera kartCamera;

    [Tooltip("카트에서 내릴 때 카트 옆 몇 미터에 서는지")]
    public float exitSideOffset = 1.6f;

    public bool InKart { get; private set; }
    public bool HasKart => kart != null;

    void Start()
    {
        // 카트가 있는 씬(트랙)이면 처음부터 타고 시작한다.
        // 레이스인데 걸어다니는 상태로 떨어지면, 점프하다 비행 모드에 갇히거나
        // 마우스 시점이 끼어들어서 헷갈린다. 둘러보고 싶으면 Tab 으로 내리면 돼.
        Apply(HasKart);
    }

    /// <summary>
    /// 걷기 ↔ 타기. <b>2026-09-17 에 다시 열었다.</b>
    ///
    /// 전에 막아뒀던 이유는 "레이스 도중에 내릴 일이 없다" 였는데, 이제 내릴 일이 생겼다 —
    /// 곰에게 <b>다가가서</b> 말을 걸어야 하고(유저: 지금은 멀리서도 걸린다),
    /// 캠퍼스 건물 열셋을 <b>걸어서 점검</b>해야 한다. 미니게임도 걸어서 들어갈 자리다.
    ///
    /// Testbed 씬은 없어졌으니(씬 셋으로 줄임) 걸어서 볼 방법이 이것뿐이야.
    /// </summary>
    public void SetMode(bool intoKart) => Apply(intoKart);

    public void Toggle() => Apply(!InKart);

    void Apply(bool intoKart)
    {
        if (intoKart && !HasKart) intoKart = false;
        InKart = intoKart;

        if (player != null)
        {
            player.ControlEnabled = !intoKart;
            // 카트를 타면 몸통을 통째로 숨긴다 (카트 콜라이더와 부딪히지 않게)
            player.gameObject.SetActive(!intoKart);
        }
        if (playerCamera != null) playerCamera.gameObject.SetActive(!intoKart);
        if (kartCamera != null) kartCamera.gameObject.SetActive(intoKart);

        if (!intoKart && kart != null && player != null)
            PlacePlayerBesideKart();

        CursorLock.Lock();
    }

    void PlacePlayerBesideKart()
    {
        Vector3 beside = kart.transform.position
                       + kart.transform.right * exitSideOffset
                       + Vector3.up * 1.2f;
        player.Teleport(beside, kart.transform.eulerAngles.y);
    }

}
