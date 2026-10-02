using UnityEngine;

/// <summary>
/// <b>바퀴마다 코스가 달라진다.</b> 2026-10-01 강사님 피드백(유저 전달):
/// *"트랙이 너무 똑같고 지루하다."*
///
/// 진단: 임무는 여덟 가지인데 <b>코스를 도는 경험은 한 가지</b>였다. 같은 길을 세 번 도는데
/// 세 번 다 같으니, 2·3바퀴는 1바퀴의 반복일 뿐이다. 발판을 매 바퀴 초기화하고
/// 광고판을 매 바퀴 다시 세운 것과 <b>같은 생각</b>을 노면에 적용한다.
///
/// | 바퀴 | 코스 |
/// |---|---|
/// | 1 | 늘 하던 그대로 — <b>배우는 바퀴</b>. 여기서 장치를 넣으면 뭘 하는 게임인지 못 배운다 |
/// | 2 | <b>도로가 부분적으로 잠긴다</b>(<see cref="FloodZone"/>) — 길 자체가 달라진다 |
/// | 3 | 노면이 <b>울퉁불퉁</b>해진다 — 자리는 고정이라 외울 수 있다 |
///
/// ★ <b>쌓지 않고 바꾼다.</b> 2바퀴 물 + 3바퀴 물·둔덕으로 하면 3바퀴가
/// «2바퀴 + 조금» 이 된다. 갈아끼워야 <b>세 바퀴가 각자 다른 바퀴</b>가 된다.
///
/// ★ <b>기하는 한 번만 짓고 켜고 끈다.</b> 지웠다 만들면 매 바퀴 자리가 달라져서
/// 외울 수가 없고, 외울 수 없는 장애물은 운이지 실력이 아니다.
/// 찾을 때는 <c>FindObjectsInactive.Include</c> — 한 번 끈 건 그냥은 다시 못 찾는다.
/// </summary>
public class LapHazards : MonoBehaviour
{
    [Tooltip("2바퀴 — 잠긴 도로")]
    public GameObject floods;

    [Tooltip("3바퀴 — 울퉁불퉁한 노면")]
    public GameObject bumps;

    [Tooltip("바퀴가 넘어갈 때 화면에 알린다")]
    public bool announce = true;

    RaceProgress player;
    int shown = -1;

    void Start() => Apply(Lap(), quiet: true);

    void Update()
    {
        int lap = Lap();
        if (lap != shown) Apply(lap, quiet: false);
    }

    int Lap()
    {
        if (player == null || !player.isActiveAndEnabled)
        {
            player = null;
            foreach (var p in FindObjectsByType<RaceProgress>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (p.GetComponent<PlayerKart>() != null) { player = p; break; }
        }
        return player != null ? player.Lap : 1;
    }

    void Apply(int lap, bool quiet)
    {
        shown = lap;

        bool wet = lap == 2;
        bool rough = lap >= 3;

        if (floods != null && floods.activeSelf != wet) floods.SetActive(wet);
        if (bumps != null && bumps.activeSelf != rough) bumps.SetActive(rough);

        if (quiet || !announce) return;

        // 바퀴가 바뀌는 그 순간에 알려 준다 — 코너를 돌다 처음 보면 그건 함정이다
        if (wet) Toast.Show("2바퀴   길이 잠겼다 — 물에 들어가면 느려진다");
        else if (rough) Toast.Show("3바퀴   노면이 솟았다");
    }
}
