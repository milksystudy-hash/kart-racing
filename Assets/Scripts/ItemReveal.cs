using UnityEngine;

/// <summary>
/// <b>증거 하나를 찾았을 때의 연출.</b> 결승선을 넘고 상품이 들어오는 순간 한 번 뜬다.
///
/// 2026-10-02 유저: *"임무 한 개 끝날 때 찾은 아이템이 새로 추가되었다고, 빛이 쏘아지면서
/// 불 켜지는 소리와 찾은 아이템을 잠시 보여주는 이벤트씬 같은 거… 너무 복잡해지나.
/// 어차피 모델링도 조잡한데."*
///
/// <b>복잡하지 않고, 모델링이 하나도 안 필요하다.</b> 이야기 장면이 3D 캐릭터를 안 쓰고
/// 2D 초상화로 가는 것과 같은 이유야(§3.6) — 보여줘야 하는 건 «물건의 생김새» 가 아니라
/// <b>«하나 더 찾았다»</b> 라서, 빛과 글자만으로 충분하다. 오히려 조잡한 모델을 띄우면
/// 그 순간에 제일 못 만든 걸 화면 한가운데에 크게 보여주는 꼴이 된다.
///
/// ★ <see cref="RaceCountdown"/> · <see cref="RaceBriefing"/> 과 같은 <b>static</b> 이다.
/// 씬에 올릴 것도 저장할 것도 없고, 옛 씬에서도 그대로 돈다 —
/// 「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」 를 다섯 번 겪은 뒤의 기본형.
/// </summary>
public static class ItemReveal
{
    /// <summary>빛이 번쩍 — 어둠이 덮이고 광선이 벌어지는 구간.</summary>
    const float Flash = 0.45f;

    /// <summary>글자가 머무는 시간. 이 뒤로는 아무 키나 누르면 넘어간다.</summary>
    const float Hold = 2.6f;

    /// <summary>다 읽지 않아도 저절로 넘어가는 한계. <b>안 넘어가는 게 제일 나쁘다.</b></summary>
    const float Timeout = 7f;

    static float shownAt = -99f;
    static bool up;

    public static string ItemName { get; private set; } = "";
    public static string ItemText { get; private set; } = "";
    public static int CaseNumber { get; private set; }
    public static int Have { get; private set; }
    public static int Total { get; private set; }

    /// <summary>떠 있나. <b>떠 있는 동안 완주 패널은 안 그린다</b> — 큰 패널은 한 번에 한 장.</summary>
    public static bool Open => up && Elapsed < Timeout;

    public static float Elapsed => Time.unscaledTime - shownAt;

    /// <summary>번쩍이 끝나고 글자가 다 선 비율 0~1.</summary>
    public static float Settle => Mathf.Clamp01((Elapsed - Flash) / 0.35f);

    /// <summary>광선이 벌어진 정도 0~1. 번쩍 구간에서만 움직인다.</summary>
    public static float Burst => Mathf.Clamp01(Elapsed / Flash);

    /// <summary>글자가 다 섰고 <see cref="Hold"/> 도 지났나 — 그때부터 «아무 키» 안내가 뜬다.</summary>
    public static bool CanSkip => Elapsed >= Hold;

    /// <summary>
    /// 상품이 들어오는 자리에서 부른다(<see cref="MissionManager.GiveReward"/>).
    /// 이름이 비면 아무 일도 안 한다.
    /// </summary>
    public static void Show(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        ItemName = ExhibitCatalogue.NameOf(id);
        CaseNumber = ExhibitCatalogue.CaseNumberOf(id);
        ItemText = "";
        foreach (var e in ExhibitCatalogue.All)
            if (e.id == id) { ItemText = e.description; break; }

        Have = CollectionState.Count;
        Total = ExhibitCatalogue.Count;

        shownAt = Time.unscaledTime;
        up = true;

        Sfx.Play("ItemFound");
    }

    /// <summary>아무 키로 닫는다. <see cref="CanSkip"/> 전에는 안 닫힌다.</summary>
    public static void Dismiss()
    {
        if (!CanSkip) return;
        up = false;
    }

    /// <summary>판을 다시 시작하거나 씬을 떠날 때. 떠 있는 채로 남으면 화면이 막힌다.</summary>
    public static void Clear() => up = false;
}
