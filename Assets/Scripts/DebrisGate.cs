using UnityEngine;

/// <summary>
/// 길에 널린 철거 자재를 <b>그 판에만</b> 켠다.
///
/// 매 판 널려 있으면 그 판만의 성격이 사라지고, 첫 판부터 있으면 배우는 판이 어려워진다.
/// <see cref="AiRaceGate"/> 와 같은 방식이야 — 물건은 씬에 두고 켜고 끄기만 한다.
/// </summary>
public class DebrisGate : MonoBehaviour
{
    [Tooltip("자재들이 담긴 부모. 비워두면 씬에서 찾는다")]
    public Transform debris;

    [Tooltip("켜면 어느 판에서든 나온다 — 확인할 때만")]
    public bool alwaysOn;

    void Start() => Apply();

    /// <summary>레이스를 다시 시작할 때도 맞춰준다.</summary>
    public void Apply()
    {
        // 지금 판의 임무가 장애물일 때만. 임무를 모르면(다 모았거나 아직 없으면) 끈다.
        // <b>MissionManager 를 기다리지 않는다.</b> 그쪽 Start 가 늦게 돌면 이 판단이
        // 틀리고, 유저가 7번째 판에서 자재를 만났다(2026-09-17). 수집 기록만 보면 된다.
        bool wanted = alwaysOn || MissionManager.WantsDebris;

        if (debris != null)
        {
            foreach (Transform piece in debris) piece.gameObject.SetActive(wanted);
        }
        else
        {
            foreach (var piece in FindObjectsByType<RoadDebris>(FindObjectsSortMode.None))
                piece.gameObject.SetActive(wanted);
        }
    }
}
