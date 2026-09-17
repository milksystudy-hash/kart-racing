using UnityEngine;

/// <summary>
/// 골든베어 입간판을 <b>그 판에만</b> 세운다.
///
/// 2026-09-17 유저: *"광고판 임무 이후의 판에도 골든베어가 붙어 있더라."*
/// 자재(<see cref="DebrisGate"/>)와 같은 규칙이야 — 판마다 널려 있으면 그 판만의 성격이
/// 사라지고, 이미 철거한 걸로 되어 있는 광고가 다음 판에 다시 서 있으면 이야기도 어긋난다.
///
/// 씬을 구울 때 안 세우는 <c>TrackBuilder.AdSignsCleared</c> 만으로는 부족했다 —
/// 트랙은 한 번 굽고 여덟 판을 <b>같은 씬 안에서</b> 이어서 도니까, 굽는 시점의 판단이
/// 그 뒤로 계속 남는다. 물건은 씬에 두고 <b>켜고 끄기만</b> 한다.
/// </summary>
public class AdSignGate : MonoBehaviour
{
    [Tooltip("입간판이 담긴 부모. 비워두면 씬에서 찾는다")]
    public Transform adSigns;

    [Tooltip("켜면 어느 판에서든 나온다 — 확인할 때만")]
    public bool alwaysOn;

    /// <summary>간판이 서 있어야 하나. <see cref="AdBoard"/> 도 스스로 이걸 본다.</summary>
    public static bool ShouldStand => MissionManager.WantsAdSigns;

    void Start() => Apply();

    /// <summary>레이스를 다시 시작할 때도 맞춰준다 — 그 사이에 판이 넘어갔을 수 있다.</summary>
    public void Apply()
    {
        bool wanted = alwaysOn || ShouldStand;

        if (adSigns != null)
        {
            foreach (Transform sign in adSigns) sign.gameObject.SetActive(wanted);
        }
        else
        {
            // <b>꺼진 것까지 찾는다.</b> 기본값은 꺼진 오브젝트를 건너뛰어서, 한 번 꺼두면
            // 같은 씬에서 영영 다시 못 켠다 — 자재가 그래서 4번 판에 안 나왔다.
            foreach (var sign in FindObjectsByType<AdBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                sign.gameObject.SetActive(wanted);
        }
    }
}
