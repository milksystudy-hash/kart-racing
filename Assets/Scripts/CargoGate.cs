using UnityEngine;

/// <summary>
/// 코스에 흩어진 곰인형을 <b>그 판에만</b> 놓는다.
/// <see cref="DebrisGate"/> · <see cref="AdSignGate"/> 와 같은 방식이야.
///
/// 매 판 널려 있으면 그 판만의 성격이 사라지고, 싣지도 않는데 길에 인형이 굴러다니면
/// "저건 뭐지" 가 된다.
/// </summary>
public class CargoGate : MonoBehaviour
{
    [Tooltip("곰인형이 담긴 부모. 비워두면 씬에서 찾는다")]
    public Transform cargo;

    [Tooltip("켜면 어느 판에서든 나온다 — 확인할 때만")]
    public bool alwaysOn;

    public static bool ShouldStand => MissionManager.WantsCargo;

    void Start() => Apply();

    public void Apply()
    {
        bool wanted = alwaysOn || ShouldStand;

        if (cargo != null)
        {
            foreach (Transform one in cargo) one.gameObject.SetActive(wanted);
        }
        else
        {
            // 꺼진 것까지 찾는다 — 기본값은 꺼진 오브젝트를 건너뛰어서 한 번 꺼두면 못 켠다.
            foreach (var one in FindObjectsByType<ExhibitCargo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                one.gameObject.SetActive(wanted);
        }
    }
}
