using UnityEngine;

/// <summary>
/// 로비에서 고른 캐릭터의 카트로 갈아입힌다.
///
/// 카트 모델을 씬 만들 때 하나로 박아두면, 누굴 골라도 그 카트로만 달리게 된다.
/// (실제로 세운이를 골라도 세진 카트가 나왔다.)
///
/// 그래서 있는 카트를 <b>전부</b> 껍데기 안에 넣어두고, 시작할 때 고른 것만 켠다.
/// 카트 한 대가 3천 삼각형 남짓이라 네 대를 다 들고 있어도 부담이 없고,
/// 이렇게 하면 레이스 도중에 바꾸는 것도 공짜다.
///
/// 어떤 카트가 있는지는 씬을 만들 때 정해져서 여기 목록으로 꽂힌다 —
/// 새 카트 FBX 를 만들면 TestSceneBuilder 의 표에 한 줄만 더하면 된다.
/// </summary>
public class KartSkin : MonoBehaviour
{
    [System.Serializable]
    public class Skin
    {
        [Tooltip("Cast.cs 의 id — 이감 / 시우 / 세운 / 세진")]
        public string castId = "";
        public GameObject model;

        [Tooltip("앞바퀴를 감싼 껍데기")]
        public Transform[] steerPivots;
        public Transform[] spinWheels;
        public Transform steeringWheel;
    }

    [Header("연결")]
    public KartWheels wheels;
    public Skin[] skins;

    [Tooltip("고른 캐릭터의 카트가 아직 없을 때 쓸 카트")]
    public string fallbackCastId = "세진";

    [Tooltip("AI 카트용 순번. -1 이면 로비에서 고른 캐릭터를 따라간다(플레이어 카트)")]
    public int aiSlot = -1;

    public string CurrentCastId { get; private set; } = "";

    void Awake() => Apply(ChooseCastId());

    /// <summary>이 카트가 누구 것이 되는지. 밖에서 검사할 수 있게 갈라놨다.</summary>
    public string ChooseCastId()
    {
        int slot = aiSlot;

        // <b>씬을 다시 굽지 않아도 AI 는 AI 로 알아본다.</b> aiSlot 이 없던 시절에 구운 씬에는
        // 이 값이 -1 로 남아서, 고쳐 놓고도 똑같이 "내 카트 넷" 이 나왔다(2026-09-17 재발).
        // 운전수가 붙어 있으면 그게 AI 라는 증거고, 순번은 형제 순서로 정한다.
        if (slot < 0 && GetComponent<KartAi>() != null)
            slot = Mathf.Max(0, transform.GetSiblingIndex());

        return slot < 0 ? GameSelection.SelectedCastId : AiCastId(slot);
    }

    /// <summary>
    /// AI 카트가 쓸 캐릭터 — <b>로비에서 고른 캐릭터를 빼고</b> 남은 것 중 순번대로.
    ///
    /// <b>씬을 구울 때 정하면 안 된다.</b> 캐릭터는 씬을 구운 <i>뒤에</i> 로비에서 고르니까,
    /// 구울 때 정해두면 플레이어가 누굴 고르든 그때 박제된 셋이 나온다. 게다가 AI 쪽 KartSkin 도
    /// Awake 에서 GameSelection 을 읽고 있어서, 고른 캐릭터의 카트로 전부 갈아입었다 —
    /// 정이감을 고르면 <b>정이감 카트가 네 대</b> 나왔다(2026-09-17 유저 제보).
    /// </summary>
    string AiCastId(int slot)
    {
        // ★ <b>결승에서는 상대가 형제가 아니라 악당이다.</b> 여덟 판 내내 광고판으로만
        // 보이던 개발업자·시의원이 여기서 처음 같은 코스에 선다(2026-09-18).
        // 카트 FBX 가 없어서 남의 차를 빌려 타지만, `Apply` 가 <b>이름표와 김 색은 제 것</b>으로
        // 달아준다 — 뒤에서 봐도 누군지 안다.
        //
        // 상대가 둘뿐이라 슬롯 2번은 남는다. 셋째 AI 는 끄는 게 맞아 —
        // 관계없는 형제가 한 대 껴 있으면 «둘과 맞붙는다» 가 «셋이 뒤섞인다» 가 된다.
        if (GrandFinal.Available)
            return slot < GrandFinal.Rivals.Length ? GrandFinal.Rivals[slot] : "";

        // <b>로비를 안 거치면 고른 캐릭터가 빈 문자열이다.</b> 그러면 플레이어 카트는
        // fallbackCastId("세진")로 떨어지는데 AI 는 아무도 안 빼서 세진이 또 뽑힌다 —
        // 트랙 씬을 바로 만들면 세진 카트가 두 대였던 게 이거다(2026-09-17 유저 제보).
        // 플레이어가 실제로 타게 될 카트를 기준으로 빼야 한다.
        string player = string.IsNullOrEmpty(GameSelection.SelectedCastId)
                      ? fallbackCastId : GameSelection.SelectedCastId;

        // 쓸 수 있는 카트를 먼저 모은다. <b>모아 놓고 고르는 게 중요하다</b> —
        // 훑으면서 순번을 세다가 모자라면 fallbackCastId("세진")로 떨어졌고, 그러면
        // 플레이어가 세진일 때 <b>세진 카트가 두 대</b> 나왔다(2026-09-17 유저 제보).
        var pool = new System.Collections.Generic.List<string>();
        foreach (var skin in skins)
            if (skin != null && skin.model != null && skin.castId != player)
                pool.Add(skin.castId);

        if (pool.Count == 0) return fallbackCastId;

        if (slot >= pool.Count)
            Debug.LogWarning($"[카트] 쓸 수 있는 카트가 {pool.Count}종뿐이라 AI 카트가 겹친다. " +
                             "씬이 카트 FBX 보다 오래된 거야 — Racing → 트랙 씬 만들기 를 한 번 돌려줘.", this);

        // 모자라도 <b>플레이어 카트로는 절대 안 떨어진다.</b> 겹치더라도 AI 끼리 겹친다.
        return pool[slot % pool.Count];
    }

    /// <summary>그 캐릭터의 카트만 켜고 나머지는 끈다.</summary>
    public void Apply(string castId)
    {
        if (skins == null || skins.Length == 0) return;

        var chosen = Find(castId) ?? Find(fallbackCastId) ?? skins[0];

        foreach (var skin in skins)
            if (skin != null && skin.model != null)
                skin.model.SetActive(skin == chosen);

        CurrentCastId = chosen.castId;

        // ★ <b>«누구 차를 타느냐» 와 «누구냐» 는 다르다.</b> 결승 상대인 개발업자·시의원은
        // 카트 FBX 가 아직 없어서 남의 차를 빌려 타는데, 전에는 그러면 <b>이름표와 색까지
        // 빌린 사람 것</b>이 돼서 악당이 그냥 형제 한 명으로 보였다.
        // 모델은 `chosen` 에서, <b>이름과 색은 원래 castId 에서</b> 가져온다.
        string identity = string.IsNullOrEmpty(castId) ? chosen.castId : castId;

        // 고른 캐릭터의 제원도 같이 얹는다. "이 카트가 누구 것이 된다" 의 일부야 —
        // 모델만 바꾸고 숫자를 안 바꾸면 네 대가 생김새만 다른 같은 차가 된다.
        KartSpec.ApplyTo(GetComponent<KartController>(), CurrentCastId);

        // 순위판에 뜰 이름도 같이. "이 카트가 누구 것이 된다" 에 이름도 들어간다.
        var progress = GetComponent<RaceProgress>();
        if (progress != null) progress.racerName = Cast.NameOf(identity);

        // 뒤로 나오는 김도 이 캐릭터 색으로. 뒤에서 봐도 누군지 알아야 순위가 읽힌다.
        var exhaust = GetComponent<KartExhaust>();
        if (exhaust == null) exhaust = gameObject.AddComponent<KartExhaust>();
        exhaust.SetColor(Cast.ColorOf(identity));

        if (wheels != null)
            wheels.Bind(chosen.steerPivots, chosen.spinWheels, chosen.steeringWheel);

        if (!string.IsNullOrEmpty(castId) && chosen.castId != castId)
            Debug.LogWarning($"[카트] '{castId}' 의 카트가 아직 없어서 '{chosen.castId}' 카트로 달린다. " +
                             "FBX 를 만들면 TestSceneBuilder 의 KartModels 표에 한 줄 더하면 된다.");
    }

    Skin Find(string castId)
    {
        if (string.IsNullOrEmpty(castId) || skins == null) return null;

        foreach (var skin in skins)
            if (skin != null && skin.castId == castId && skin.model != null) return skin;

        return null;
    }
}
