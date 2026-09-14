using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tab 으로 "걸어다니기 ↔ 카트 타기" 를 오간다.
///
/// 트랙을 걸어서 둘러본 다음 바로 타고 달려볼 수 있어서, 맵을 만들면서 확인하기에 제일 편하다.
/// 씬에 카트가 없으면 그냥 걷기만 된다 — 빈 맵에서도 문제없이 동작해.
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
        Apply(false);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.tabKey.wasPressedThisFrame && HasKart)
            Apply(!InKart);
    }

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

    /// <summary>다른 스크립트에서 강제로 태우거나 내리게 할 때.</summary>
    public void SetMode(bool intoKart) => Apply(intoKart);
}
