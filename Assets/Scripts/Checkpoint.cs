using UnityEngine;

/// <summary>
/// 트랙 위에 일정 간격으로 놓이는 통과 지점. 0번이 결승선이다.
/// 콜라이더는 반드시 Is Trigger 여야 하고, 카트에는 Rigidbody 가 있어야 감지된다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("트랙을 따라 0, 1, 2 ... 순서대로. 0번 = 결승선")]
    public int index;

    [Tooltip("코스 밖으로 떨어졌을 때 여기로 되돌린다")]
    public Transform respawnPoint;

    LapTracker tracker;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (respawnPoint == null) respawnPoint = transform;
    }

    void OnTriggerEnter(Collider other)
    {
        // 카트 본체든 자식 콜라이더든 다 잡히게
        var kart = other.GetComponentInParent<KartController>();
        if (kart == null) return;

        // LapTracker 가 나보다 늦게 만들어질 수 있어서 여기서 찾는다
        if (tracker == null) tracker = FindFirstObjectByType<LapTracker>();
        if (tracker == null) return;

        tracker.PassCheckpoint(index, this);
    }
}
