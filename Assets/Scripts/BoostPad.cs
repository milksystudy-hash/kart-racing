using UnityEngine;

/// <summary>
/// 밟으면 밀어주는 가속 발판 — 마리오 카트의 그 화살표 판.
///
/// 이게 있으면 코스가 "그냥 도는 길" 에서 <b>고를 게 있는 길</b>로 바뀐다.
/// 발판을 밟으려면 선을 살짝 바꿔야 하고, 그 선이 코너에 좋은 선이 아닐 수도 있으니까.
/// 그래서 트랙 자체는 그대로여도 매 바퀴가 덜 지루해진다.
///
/// 드리프트 부스트와는 <b>따로 논다</b> — KartController.ApplyBoost 로 직접 먹인다.
/// 드리프트로 모으던 게이지가 있으면 그건 정리된다(두 배로 터지지 않게).
///
/// 이 스크립트는 판을 그리지 않는다. 생김새는 TrackBuilder 가 만들고,
/// 여기는 <b>밟았을 때 무슨 일이 일어나는지</b>만 맡는다. 나중에 FBX 발판으로 바꿔도
/// 이 스크립트는 그대로 쓴다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class BoostPad : MonoBehaviour
{
    [Header("세기")]
    [Tooltip("최고 속도에 얼마나 얹어줄지 (m/s). 드리프트 부스트가 보통 4~8 정도")]
    public float boostAmount = 7f;

    [Tooltip("몇 초 동안 밀어줄지")]
    public float duration = 1.4f;

    [Header("다시 밟기")]
    [Tooltip("같은 카트가 이 발판을 다시 먹기까지의 시간. 발판 위에서 비비는 걸 막는다")]
    public float retriggerDelay = 1f;

    // 카트별로 따로 센다. 나중에 AI 카트가 늘어나도 서로 방해하지 않게.
    readonly System.Collections.Generic.Dictionary<KartController, float> lastTaken = new();

    /// <summary>이번 바퀴에 <b>플레이어가</b> 밟았는지. "발판 전부" 임무가 이걸 센다.</summary>
    public bool TakenByPlayer { get; private set; }

    static BoostPad[] all;

    /// <summary>씬에 깔린 발판 수. 트랙을 바꿔도 임무 조건이 알아서 따라온다.</summary>
    public static int CountInScene() => All().Length;

    public static int TakenCount()
    {
        int n = 0;
        foreach (var pad in All()) if (pad != null && pad.TakenByPlayer) n++;
        return n;
    }

    public static void ClearTaken()
    {
        foreach (var pad in All()) if (pad != null) pad.TakenByPlayer = false;
    }

    // 매 프레임 찾으면 비싸다. 한 번 찾아 두고, 씬이 바뀌어 사라졌으면 다시 찾는다.
    static BoostPad[] All()
    {
        if (all == null || all.Length == 0 || all[0] == null)
            all = FindObjectsByType<BoostPad>(FindObjectsSortMode.None);
        return all;
    }

    void Reset()
    {
        var box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    void OnTriggerEnter(Collider other) => Trigger(other);

    // 발판 위를 스치듯 지나가면 Enter 가 한 프레임도 안 잡히는 일이 있다.
    // Stay 로도 받아서 놓치지 않게 한다 — 중복은 retriggerDelay 가 막는다.
    void OnTriggerStay(Collider other) => Trigger(other);

    void Trigger(Collider other)
    {
        var kart = other.GetComponentInParent<KartController>();
        if (kart == null) return;

        // 이미 부스트 중이면 다시 안 먹인다.
        // 이게 없으면 발판 위에서 벽에 박힌 카트한테 Stay 가 1초마다 부스트를 계속 먹여서,
        // "풀린다!" 가 영영 안 끝나고 게이지가 톱니처럼 오르내리고, 앞으로 미는 힘이
        // 계속 걸려서 조작이 죽는다. 실제로 그렇게 됐다(2026-09-16).
        if (kart.IsBoosting) return;

        // 서 있거나 뒤로 가는 카트한테는 안 먹인다.
        // 발판은 앞으로 밀어주는 물건이라, 후진 중에 먹으면 뒤로 튀어나간다. 그건 발판이 아니라 함정이야.
        if (kart.SpeedKph < 5f) return;

        // 발판이 가리키는 쪽으로 달릴 때만. 역주행으로 밟으면 그냥 지나간다.
        if (Vector3.Dot(kart.transform.forward, transform.forward) < 0.3f) return;

        if (lastTaken.TryGetValue(kart, out float when) && Time.time - when < retriggerDelay) return;
        lastTaken[kart] = Time.time;

        kart.ApplyBoost(boostAmount, duration);

        // 임무 판정은 플레이어 것만 센다. AI 가 밟은 걸 같이 세면 가만히 있어도 임무가 깨진다.
        if (kart.GetComponent<PlayerKart>() != null) TakenByPlayer = true;
    }
}
