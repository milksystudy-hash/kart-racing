using UnityEngine;

/// <summary>
/// 대사 한 줄과, 그 줄이 나올 조건.
///
/// 조건이 여기 붙어 있는 게 핵심이야. "이야기 조각을 다 모았을 때만 나오는 대사" 같은 걸
/// if 문을 새로 짜지 않고 <b>줄 옆에 한마디 붙이는 것</b>으로 끝낸다:
///
/// <code>
///   Talk.Say("세진", "아직 증거가 모자라.").OnlyIf(When.NotAllCollected),
///   Talk.Say("세진", Mood.기쁨, "다 모았어! 이제 방송 타자.").OnlyIf(When.AllCollected),
/// </code>
///
/// 두 줄을 나란히 써두면 게임이 알아서 하나만 고른다. 조건이 없는 줄은 항상 나온다.
/// 기획서 §3.4 의 "숨겨진 수집품 → 캐릭터별 비밀 대사" 가 이 구조로 들어간다.
/// </summary>
public struct DialogueLine
{
    /// <summary>누가 말하는지. Cast 의 id. 비워 두면 나레이션.</summary>
    public string speakerId;
    public string text;
    public Cast.Mood mood;
    public DialogueCondition condition;

    /// <summary>이 줄이 나올 조건을 건다. 조건을 안 걸면 항상 나온다.</summary>
    public DialogueLine OnlyIf(DialogueCondition c)
    {
        condition = c;
        return this;
    }

    public bool IsNarration => string.IsNullOrEmpty(speakerId);
    public bool Passes => condition.Passes();
}

/// <summary>
/// 대사 한 줄이 나올 조건. 직접 만들지 말고 <see cref="When"/> 의 이름들을 쓰면 된다.
/// </summary>
public struct DialogueCondition
{
    public enum Kind { 언제나, 다모았을때, 아직다못모았을때, 가지고있을때, 아직없을때, 몇장이상일때, 몇장미만일때 }

    public Kind kind;
    /// <summary>수집품 id (ExhibitCatalogue 의 id) 또는 장 번호.</summary>
    public string id;
    public int number;

    public bool Passes()
    {
        switch (kind)
        {
            case Kind.다모았을때:      return CollectionState.Count >= ExhibitCatalogue.Count;
            case Kind.아직다못모았을때: return CollectionState.Count < ExhibitCatalogue.Count;
            case Kind.가지고있을때:    return CollectionState.Has(id);
            case Kind.아직없을때:      return !CollectionState.Has(id);
            case Kind.몇장이상일때:    return StoryProgress.CurrentChapter >= number;
            case Kind.몇장미만일때:    return StoryProgress.CurrentChapter < number;
            default:                  return true;
        }
    }
}

/// <summary>
/// 대사에 거는 조건들. 이야기를 쓰면서 `.OnlyIf(...)` 안에 넣는 이름이야.
/// 새 조건이 필요해지면 DialogueCondition.Kind 에 한 줄, 여기에 한 줄만 늘리면 된다.
/// </summary>
public static class When
{
    /// <summary>전시품 여덟 개를 전부 모았을 때.</summary>
    public static DialogueCondition AllCollected =>
        new DialogueCondition { kind = DialogueCondition.Kind.다모았을때 };

    /// <summary>아직 하나라도 못 모았을 때.</summary>
    public static DialogueCondition NotAllCollected =>
        new DialogueCondition { kind = DialogueCondition.Kind.아직다못모았을때 };

    /// <summary>그 전시품을 가지고 있을 때. id 는 ExhibitCatalogue 에 적힌 것 — "contract" 처럼.</summary>
    public static DialogueCondition Has(string exhibitId) =>
        new DialogueCondition { kind = DialogueCondition.Kind.가지고있을때, id = exhibitId };

    /// <summary>그 전시품이 아직 없을 때.</summary>
    public static DialogueCondition Missing(string exhibitId) =>
        new DialogueCondition { kind = DialogueCondition.Kind.아직없을때, id = exhibitId };

    /// <summary>지금 몇 장 이상 진행했을 때. 0 프롤로그 · 1~3 메인 · 4 마지막 장.</summary>
    public static DialogueCondition ChapterAtLeast(int chapter) =>
        new DialogueCondition { kind = DialogueCondition.Kind.몇장이상일때, number = chapter };

    /// <summary>아직 그 장에 못 갔을 때.</summary>
    public static DialogueCondition ChapterBelow(int chapter) =>
        new DialogueCondition { kind = DialogueCondition.Kind.몇장미만일때, number = chapter };
}

/// <summary>대사 한 줄을 만드는 짧은 이름들. StoryScript 에서 이것만 쓰면 된다.</summary>
public static class Talk
{
    public static DialogueLine Say(string speakerId, string text) =>
        new DialogueLine { speakerId = speakerId, text = text, mood = Cast.Mood.기본 };

    public static DialogueLine Say(string speakerId, Cast.Mood mood, string text) =>
        new DialogueLine { speakerId = speakerId, text = text, mood = mood };

    /// <summary>이름표 없이 가운데로 나오는 글. 장면 전환이나 상황 설명에.</summary>
    public static DialogueLine Narrate(string text) =>
        new DialogueLine { speakerId = Cast.Narrator, text = text, mood = Cast.Mood.기본 };
}
