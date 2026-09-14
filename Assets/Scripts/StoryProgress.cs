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
}
