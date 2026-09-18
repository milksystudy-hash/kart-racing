using UnityEngine;

/// <summary>
/// <b>아홉 번째 판 — 결승.</b> 2026-09-18 유저 결정:
/// *"수집품 8개를 다 모으면 정치인에게 가서 8개 증거를 들이밀 거야. 그리고 결승에서는
/// 정치인이랑 카트 대결 뜨겠지. 정치인이 마구 던지는 장애물을 피하면서 달릴 수도 있고.
/// 그 9개가 끝나야 레이싱은 자유 플레이가 가능하려나."* — 맞는 순서다.
///
/// 전에는 여덟 개를 다 모으면 <b>이름 없는 «자유 주행»</b> 으로 빠졌다. 여덟 판을 도는 내내
/// 쌓아온 게 아무 장면도 없이 끝나는 거라, 이야기의 절정에 해당하는 레이스가 게임에 없었다.
/// <b>새로 만든 게 아니라 그 자리에 이름과 상대를 준 것</b>이야 — AI 카트(<see cref="KartAi"/>),
/// 켜는 장치(<see cref="AiRaceGate"/>), 던질 장애물(<see cref="RoadDebris"/>) 은 전부 있었다.
///
/// <b>수집 기록만 보는 static 이다.</b> 컴포넌트도 순서도 필요 없어 —
/// 이 프로젝트에서 «새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다» 를 네 번 겪었다.
/// </summary>
public static class GrandFinal
{
    const string PrefsKey = "결승클리어";

    /// <summary>
    /// 결승에서 만나는 상대. <b>개발업자와 시의원</b> — 여덟 판 내내 광고판으로만 보이던
    /// 둘이 여기서 처음 같은 코스에 선다. 둘 다 <c>Cast.RacerIds</c> 에 이미 등록돼 있고
    /// 이름표 색(금색·자홍)도 있다.
    ///
    /// <b>카트 모델은 아직 없어서 남의 차를 빌려 탄다</b>(<see cref="KartSkin.Apply"/> 가
    /// 경고를 띄운다). 그래도 이름표와 배기 김 색이 제 것이라 뒤에서 봐도 누군지 안다 —
    /// 카트 넷이 뒤에서 보면 똑같이 생겨서 색으로 구분하게 만들어 둔 게 여기서 값을 한다.
    /// </summary>
    public static readonly string[] Rivals = { "개발업자", "시의원" };

    static bool loaded;
    static bool cleared;

    /// <summary>결승을 이겨 봤나. 수집품과 같은 방식으로 PlayerPrefs 에 남는다.</summary>
    public static bool Cleared
    {
        get
        {
            if (!loaded)
            {
                cleared = PlayerPrefs.GetInt(PrefsKey, 0) == 1;
                loaded = true;
            }
            return cleared;
        }
    }

    /// <summary>여덟 개를 다 모았나. 결승과 자유 주행이 공유하는 조건.</summary>
    public static bool AllCollected =>
        ExhibitCatalogue.Count > 0 && CollectionState.Count >= ExhibitCatalogue.Count;

    /// <summary>
    /// <b>지금이 결승 판인가.</b> 여덟 개를 다 모았고 아직 못 이겼을 때.
    /// 장애물도 AI 도 이걸 보고 스스로 켜진다.
    /// </summary>
    public static bool Available => AllCollected && !Cleared;

    /// <summary>
    /// <b>결승까지 끝낸 뒤.</b> 이제 판정이 없는 자유 주행이다 —
    /// 이야기가 끝나고 나서 노는 게 맞는 순서라는 유저 판단.
    /// </summary>
    public static bool FreeRun => AllCollected && Cleared;

    public static void MarkCleared()
    {
        cleared = true;
        loaded = true;
        PlayerPrefs.SetInt(PrefsKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>디버그용. 로비의 F10(수집품 비우기)과 같이 불린다.</summary>
    public static void Reset()
    {
        cleared = false;
        loaded = true;
        PlayerPrefs.SetInt(PrefsKey, 0);
        PlayerPrefs.Save();
    }
}
