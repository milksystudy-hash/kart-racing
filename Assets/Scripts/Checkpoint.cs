using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 트랙에 일정 간격으로 놓이는 통과 지점. 0번이 결승선이다.
///
/// 지나간 카트의 <see cref="RaceProgress"/> 에 알려주기만 한다 — 플레이어든 AI 든 똑같이.
/// 콜라이더는 반드시 Is Trigger 여야 하고, 카트에는 Rigidbody 가 있어야 감지된다.
///
/// **맵을 직접 만들어도 이 부분은 그대로 쓴다.** 코스를 따라 순서대로 놓고 index 만 매기면,
/// 랩 카운트도 순위도 임무 판정도 전부 그 위에 얹힌다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("트랙을 따라 0, 1, 2 ... 순서대로. 0번 = 결승선")]
    public int index;

    [Tooltip("코스 밖으로 떨어졌을 때 여기로 되돌린다")]
    public Transform respawnPoint;

    /// <summary>씬에 놓인 체크포인트 전부. 순위 계산이 "다음 관문" 위치를 찾을 때 쓴다.</summary>
    static readonly List<Checkpoint> registry = new();

    public static Checkpoint ByIndex(int index)
    {
        foreach (var checkpoint in registry)
            if (checkpoint != null && checkpoint.index == index) return checkpoint;
        return null;
    }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (respawnPoint == null) respawnPoint = transform;
    }

    void OnEnable() => registry.Add(this);
    void OnDisable() => registry.Remove(this);

    void OnTriggerEnter(Collider other)
    {
        // 카트 본체든 자식 콜라이더든 다 잡히게
        var progress = other.GetComponentInParent<RaceProgress>();
        if (progress == null) return;

        progress.PassCheckpoint(index, this);
    }
}
