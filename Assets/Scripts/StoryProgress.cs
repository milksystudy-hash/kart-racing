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

    /// <summary>0 프롤로그 · 1~3 메인 · 4 마지막 장.</summary>
    public static int CurrentChapter
    {
        get
        {
            cached ??= PlayerPrefs.GetInt(PrefsKey, 1);
            return Mathf.Clamp(cached.Value, Prologue, FinalChapter);
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
}
