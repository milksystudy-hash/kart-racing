using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>코스에 흩어진 곰인형.</b> 지나가면 카트 뒤에 줄줄이 매달려 따라온다.
///
/// 2026-09-18. 유저: *"이 게임이 진짜 재밌긴 하려나."* 진단은 이랬다 —
/// 여덟 판이 전부 <b>"실수하지 마라"</b> 였다. 발판 다 밟기·벽 안 긁기·시간 안에·자재 피하기.
/// 전부 <b>감점제</b>라 잘해서 얻는 게 없고 못하면 잃기만 한다. 긴장은 되는데 신나지가 않아.
/// 유일하게 재밌던 판이 <b>광고판 부수기</b>였고, 그건 유일한 <b>플러스형</b>이었다.
///
/// 그래서 `무발판`(순수 감점제)을 이걸로 갈아 끼웠다. 이 판에서 하는 일은 하나 —
/// <b>모을수록 좋다.</b> 놓쳐도 실패가 아니고 목표치만 넘기면 된다.
///
/// <b>왜 관람객이 아니라 곰인형인가.</b> 처음에 "관람객 태우기" 로 썼다가 유저가 잡았다:
/// *"코스에 선 곰들은 이감이 친구지 관람객이 아니잖아."* 맞다. 곰인형 박물관에서
/// 곰이 관람객이면 곰이 곰을 보러 온 꼴이고, 무엇보다 <b>이 게임의 카트는 무인 모형</b>이라
/// 사람을 태울 수가 없다. 짐은 실을 수 있어 — 그래서 <b>창고에 처박아 둔 전시품</b>을
/// 전시실로 나르는 일이 됐다. 전시할 게 없으면 박물관이 아니니까.
///
/// <b>물리가 없다.</b> 매달린 인형은 카트 뒤 정해진 자리로 미끄러질 뿐이야 —
/// 리지드바디를 여덟 개 달면 카트에 부딪혀 레이스가 망가진다.
/// </summary>
public class ExhibitCargo : MonoBehaviour
{
    [Tooltip("이 속도(㎞/h) 아래로는 안 실린다 — 서서 줍는 걸 막는다")]
    public float minSpeedKph = 10f;

    [Tooltip("카트가 이 거리 안에 오면 실린다")]
    public float reach = 3.2f;

    [Tooltip("앞 인형과의 간격(m)")]
    public float spacing = 1.5f;

    /// <summary>지금까지 실은 수. 임무가 이걸 본다.</summary>
    public static int Loaded => cargo.Count;

    /// <summary>씬에 있는 전체 수. 목표치를 코드에 박지 않으려고.</summary>
    public static int CountInScene() => All().Length;

    static readonly List<ExhibitCargo> cargo = new();
    static ExhibitCargo[] all;

    static ExhibitCargo[] All()
    {
        if (all == null || all.Length == 0 || all[0] == null)
            all = FindObjectsByType<ExhibitCargo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return all;
    }

    /// <summary>판을 다시 시작할 때. 전부 제자리로 돌려놓는다.</summary>
    public static void ResetAll()
    {
        cargo.Clear();
        foreach (var c in All()) if (c != null) c.GoHome();
    }

    Vector3 home;
    Quaternion homeRotation;
    Transform follow;
    int slot = -1;
    float bobPhase;

    void Awake()
    {
        home = transform.position;
        homeRotation = transform.rotation;
        bobPhase = Random.value * 10f;

        // 게이트가 없는 씬(옛날에 구운 것)에서도 스스로 꺼진다.
        if (!CargoGate.ShouldStand && FindFirstObjectByType<CargoGate>() == null)
            gameObject.SetActive(false);
    }

    void GoHome()
    {
        follow = null;
        slot = -1;
        transform.SetPositionAndRotation(home, homeRotation);
    }

    void Update()
    {
        if (follow == null) { WaitForKart(); return; }

        // 카트 뒤 제 순번 자리로 미끄러진다. <b>한 줄로 서야</b> "끌고 간다" 가 보인다 —
        // 흩어져 따라오면 그냥 파티클로 보여.
        Vector3 target = follow.position - follow.forward * (spacing * (slot + 1));

        // 살짝 통통 튄다. 바닥에 딱 붙어 미끄러지면 끌려가는 게 아니라 붙어 있는 것처럼 보인다.
        target.y = home.y + Mathf.Abs(Mathf.Sin(Time.time * 7f + bobPhase)) * 0.22f;

        transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-6f * Time.deltaTime));

        // 달리는 쪽을 본다. 뒤로 끌려가는 곰은 무섭다.
        var look = follow.forward;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(look, Vector3.up), 6f * Time.deltaTime);
    }

    void WaitForKart()
    {
        var kart = PlayerKart.Current;
        if (kart == null) return;

        var body = kart.GetComponent<KartController>();
        if (body == null || Mathf.Abs(body.SpeedKph) < minSpeedKph) return;

        if (Vector3.Distance(kart.transform.position, transform.position) > reach) return;

        slot = cargo.Count;
        cargo.Add(this);
        follow = kart.transform;

        Toast.Show(RaceVoice.PickedUp(cargo.Count, CountInScene()));
    }
}
