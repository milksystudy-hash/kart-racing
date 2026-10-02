using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지금 몇 장을 진행 중인지 기억한다. 트랙에 어떤 수집품이 나올지를 이 값이 정한다.
///
/// 기획서 §9.2 의 StoryUnlockManager 가 자라날 뿌리다. 지금은 "현재 챕터" 하나뿐이고,
/// 나중에 임무 판정이 붙으면 **필수 임무를 깨면 이 숫자를 하나 올리는** 식으로 이어진다.
/// 그때 ExhibitPickup 쪽은 고칠 게 없다 — 이미 이 값을 보고 있으니까.
/// </summary>
public static class StoryProgress
{
    const string PrefsKey = "Racing.Chapter";

    public const int Prologue = 0;
    public const int FinalChapter = 4;

    static int? cached;

    /// <summary>
    /// 수집품 <paramref name="collected"/> 개를 모았으면 몇 장인가.
    ///
    /// ★★ 2026-09-28 <b>장 번호는 수집 기록을 넘어설 수 없다.</b> 유저 제보:
    /// *"T 를 눌러도 이야기 장면이 재생이 안 돼."* 재 보니 <b>장 4 · 수집품 0 · 결승 안 깸</b>
    /// 이라는 있을 수 없는 조합이었다 — F10 으로 수집품을 비울 때 <see cref="CollectionState.ClearAll"/>
    /// 이 `GrandFinal` 만 되돌리고 <b>장 번호는 4에 남겨 뒀기</b> 때문이다.
    /// 그러면 `SceneForChapter(4)` 가 "epilogue" 고, `CurrentScene()` 은 «결승도 안 깼는데
    /// 결말이 먼저 나오면 안 된다» 며 <b>빈 문자열</b>을 돌려준다 → T 가 아무 일도 안 한다.
    ///
    /// <b>둘을 따로 저장하면 반드시 어긋난다.</b> 게이트를 수집 기록으로 옮겼던 것과 같은
    /// 판단이야(2026-09-17) — 여기서도 <b>수집 기록이 진실</b>이고 장 번호는 그 아래로 묶인다.
    /// 1~2판 → 1장 · 3~4판 → 2장 · 5~6판 → 3장 · 7~8판 → 4장.
    /// </summary>
    public static int ChapterFor(int collected) =>
        Mathf.Clamp((collected + 1) / 2, Prologue, FinalChapter);

    /// <summary>0 프롤로그 · 1~3 메인 · 4 마지막 장.</summary>
    public static int CurrentChapter
    {
        get
        {
            // ★ 기본값이 <b>1</b> 이었다. 새로 깐 사람은 장 1 로 시작해서
            // `SceneForChapter(1)` = "ch1" 이 나오고 <b>프롤로그를 영영 못 본다.</b>
            // 0 이어야 «철거 통지서» 부터 시작한다.
            // ★★ 2026-10-01 — <b>수집 기록만 본다.</b> 유저: *"미션을 깨고도 게임을 껐다
            // 다시 들어가야 이야기를 볼 수 있다."*
            //
            // 전에는 <c>Min(저장값, ChapterFor(수집))</c> 이었다. 저장값은
            // <see cref="AdvanceChapter"/> 로만 올라가는데, 그걸 부르는 조건이
            // «그 장의 전시품이 전부 들어왔을 때» 였다. 그런데 <see cref="ExhibitCatalogue"/> 의
            // <c>chapterIndex</c> 는 <b>모으는 순서와 다르다</b> — 프롤로그 몫인 조감도가
            // 여덟 번째라, <b>장 0 은 8판을 깰 때까지 안 끝난다.</b>
            // 그래서 저장값이 0 에 묶이고 <c>Min</c> 이 그걸 그대로 돌려줘서
            // 한 판을 깨도 «프롤로그»(이미 본 것)만 나왔다 — 아무 것도 안 뜬 이유야.
            //
            // <b>같은 것을 두 군데 저장하면 반드시 어긋난다</b>(2026-09-28 에 적어 둔 것).
            // 수집 기록이 진실이고, 장 번호는 거기서 <b>나오기만</b> 하면 된다.
            cached ??= PlayerPrefs.GetInt(PrefsKey, Prologue);
            return ChapterFor(CollectionState.Count);
        }
        set
        {
            int clamped = Mathf.Clamp(value, Prologue, FinalChapter);
            if (cached == clamped) return;
            cached = clamped;
            PlayerPrefs.SetInt(PrefsKey, clamped);
            PlayerPrefs.Save();
        }
    }

    /// <summary>필수 임무를 깼을 때. 마지막 장을 넘어가지는 않는다.</summary>
    public static void AdvanceChapter() => CurrentChapter = CurrentChapter + 1;

    public static string NameOf(int chapter) => chapter switch
    {
        0 => "프롤로그 · 철거 통지서",
        1 => "제1장 · 사라진 관람객",
        2 => "제2장 · 조작된 안전진단",
        3 => "제3장 · 관장의 서명",
        _ => "마지막 장 · 철거 전야",
    };

    public static string CurrentName => NameOf(CurrentChapter);

    // ==================================================================
    //  이미 본 이야기 장면
    //  같은 장면이 로비에 들를 때마다 다시 뜨면 성가시니까 한 번 본 건 기억한다.
    //  기획서 §3.7 의 "챕터 선택 화면에서 이미 본 이야기를 다시 볼 수 있다" 도 이 목록을 쓴다.
    // ==================================================================

    const string SeenKey = "Racing.StorySeen";

    static HashSet<string> seen;

    static HashSet<string> Seen
    {
        get
        {
            if (seen != null) return seen;

            seen = new HashSet<string>();
            foreach (var id in PlayerPrefs.GetString(SeenKey, "").Split(','))
                if (!string.IsNullOrWhiteSpace(id)) seen.Add(id.Trim());

            return seen;
        }
    }

    public static bool HasSeen(string sceneId) =>
        !string.IsNullOrEmpty(sceneId) && Seen.Contains(sceneId);

    public static void MarkSeen(string sceneId)
    {
        if (string.IsNullOrEmpty(sceneId) || !Seen.Add(sceneId)) return;
        PlayerPrefs.SetString(SeenKey, string.Join(",", Seen));
        PlayerPrefs.Save();
    }

    /// <summary>테스트용. 처음부터 다시 보고 싶을 때.</summary>
    public static void ClearSeen()
    {
        Seen.Clear();
        PlayerPrefs.SetString(SeenKey, "");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 이야기를 통째로 처음으로. <see cref="CollectionState.ClearAll"/>(F10)이 부른다 —
    /// <b>수집품을 비우는 건 «처음부터» 라는 뜻</b>이니 장 번호와 본 장면도 같이 간다.
    /// </summary>
    public static void ResetStory()
    {
        cached = Prologue;
        PlayerPrefs.SetInt(PrefsKey, Prologue);
        PlayerPrefs.Save();
        ClearSeen();
    }
}
