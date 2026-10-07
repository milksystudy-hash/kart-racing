using UnityEngine;

/// <summary>
/// <b>증거 하나를 찾았을 때의 연출.</b> 결승선을 넘고 상품이 들어오는 순간 한 번 뜬다.
///
/// ★★ 2026-10-02 에 <b>다시 만들었다.</b> 첫 판은 «결승선 앞에서 빛이 번쩍» 이었는데
/// 유저가 원한 것은 그게 아니었다:
/// *"임무가 끝나고 바로 빛나는 게 아니라, 화면이 0.2초 정도 암전되었다가
/// <b>박물관 씬을 보여주며</b> 그 아이템에 <b>젤다의 전설처럼 빛을 쬐어</b> 달라는 뜻이었어.
/// 지금 번쩍 빛나는 씬은 삭제하는 게 좋아."*
///
/// <b>차이가 크다.</b> 번쩍임은 «지금 여기서 뭔가 일어났다» 지만, 암전 뒤 다른 장소를
/// 보여주는 건 <b>«그 물건이 저기 들어갔다»</b> 이다. 수집품은 트랙에 있는 게 아니라
/// <b>전시실에 쌓이는 것</b>이라, 보여줘야 하는 자리도 전시실이다.
///
/// <code>
///   0.00  달리던 화면이 0.2초에 걸쳐 까맣게       ← 장소가 바뀐다는 신호
///   0.20  전시실 그림이 떠오른다
///   0.45  위에서 빛줄기가 내려와 진열장에 꽂힌다
///   0.95  빛 속에 이름이 선다
///   ...   아무 키 (안 눌러도 저절로 넘어간다)
/// </code>
///
/// ★ <see cref="RaceCountdown"/> · <see cref="RaceBriefing"/> 과 같은 <b>static</b> 이다.
/// 씬에 올릴 것도 저장할 것도 없고, 옛 씬에서도 그대로 돈다.
/// </summary>
public static class ItemReveal
{
    /// <summary>까맣게 덮는 시간. <b>짧아야 «장면이 바뀐다» 지 «렉» 이 안 된다.</b></summary>
    public const float Blackout = 0.2f;

    /// <summary>전시실 그림이 떠오르는 시간.</summary>
    const float Appear = 0.25f;

    /// <summary>빛줄기가 내려오는 시간.</summary>
    const float Beam = 0.5f;

    /// <summary>글자가 머무는 시간. 이 뒤로는 아무 키나 누르면 넘어간다.</summary>
    const float Hold = 2.2f;

    /// <summary>다 읽지 않아도 저절로 넘어가는 한계. <b>안 넘어가는 게 제일 나쁘다.</b></summary>
    const float Timeout = 9f;

    /// <summary>
    /// 빛이 꽂히는 자리 — 화면 가로·세로 비율. 배경 그림의 <b>진열장 위치</b>에 맞춘 값이다.
    /// 그림을 갈아끼우면 이 둘을 다시 재야 한다.
    /// </summary>
    public const float BeamX = 0.5f;
    public const float BeamY = 0.55f;

    /// <summary>
    /// ★ 진단 로그. 2026-10-02 유저: *"전시실 화면이 0.2초 뜨고 사라진다."*
    /// 코드를 읽어서는 원인이 안 보였고, 배치모드 플레이 테스트는 멈춰 버렸다.
    /// <b>이 프로젝트에서 «코드는 도는데 화면은 그대로» 를 로그 없이 잡은 적이 없다</b>
    /// (문 여섯 번). 그래서 <b>누가 언제 닫았는지</b>를 콘솔이 말하게 한다.
    /// 제출 전에 끌 것.
    /// </summary>
    public static bool debugLog = true;

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

    /// <summary>까맣게 덮인 정도 0~1.</summary>
    public static float Dark => Mathf.Clamp01(Elapsed / Blackout);

    /// <summary>전시실 그림이 떠오른 정도 0~1.</summary>
    public static float Stage => Mathf.Clamp01((Elapsed - Blackout) / Appear);

    /// <summary>빛줄기가 내려온 정도 0~1.</summary>
    public static float Shaft => Mathf.Clamp01((Elapsed - Blackout - Appear) / Beam);

    /// <summary>글자가 선 정도 0~1.</summary>
    public static float Settle => Mathf.Clamp01((Elapsed - Blackout - Appear - Beam) / 0.3f);

    /// <summary>글자가 다 섰고 <see cref="Hold"/> 도 지났나 — 그때부터 «아무 키» 안내가 뜬다.</summary>
    public static bool CanSkip => Elapsed >= Hold;

    /// <summary>
    /// 전시실 그림. <b>진열장 번호마다 다른 사진</b>이다 —
    /// 2026-10-02 유저: *"두 번째 임무를 깼는데 여전히 1번 코인에만 빛이 뜬다."*
    /// 사진이 한 장뿐이면 이름만 바뀌고 <b>여덟 판 내내 같은 칸이 빛난다.</b>
    ///
    /// <c>StoryBackdrops/gallery_3.png</c> 처럼 번호를 붙이고,
    /// 없으면 <c>gallery.png</c>, 그것도 없으면 짙은 남색 방으로 떨어진다 —
    /// 그림이 안 들어왔다고 연출이 통째로 사라지면 «고장» 으로 보인다.
    /// </summary>
    public static Texture2D Backdrop =>
        Resources.Load<Texture2D>($"StoryBackdrops/gallery_{CaseNumber}")
        ?? Resources.Load<Texture2D>("StoryBackdrops/gallery");

    /// <summary>
    /// 상품이 들어오는 자리에서 부른다(<see cref="MissionManager"/>).
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

        if (Dev.Enabled && debugLog)
            Debug.Log($"[획득] '{ItemName}' (진열장 {CaseNumber}번) 연출 시작 · " +
                      $"배경 {(Backdrop == null ? "없음 → 남색 방" : Backdrop.name)} · " +
                      $"{Hold:0.0}초 뒤부터 아무 키로 닫힘 · {Timeout:0}초면 저절로");

        Sfx.Play("ItemFound");
    }

    /// <summary>아무 키·마우스로 닫는다. <see cref="CanSkip"/> 전에는 안 닫힌다.</summary>
    /// <summary>
    /// ★ 연출을 닫은 <b>직후</b>인가. 닫는 조건이 «아무 키» 라서 연타하면
    /// 남은 키가 뒤의 ESC 처리로 새는데, 그게 <b>두 번이면 로비로 나가 버린다</b>
    /// (유저: *"레이싱 끝나니까 갑자기 메인 화면으로 이동한다"*).
    /// </summary>
    public static bool JustClosed => Time.unscaledTime - closedAt < 0.45f;

    static float closedAt = -99f;

    public static void Dismiss()
    {
        if (!CanSkip) return;
        if (Dev.Enabled && debugLog && up) Debug.Log($"[획득] {Elapsed:0.00}초에 <b>눌러서</b> 닫음");
        up = false;
        closedAt = Time.unscaledTime;
    }

    /// <summary>판을 다시 시작하거나 씬을 떠날 때. 떠 있는 채로 남으면 화면이 막힌다.</summary>
    public static void Clear()
    {
        if (Dev.Enabled && debugLog && up) Debug.Log($"[획득] {Elapsed:0.00}초에 <b>Clear() 로</b> 닫음 — " +
                                      "판을 다시 시작했거나 씬을 떠났다");
        up = false;
    }
}
