using UnityEngine;

/// <summary>
/// 로비에서 고른 캐릭터를 트랙 씬까지 들고 간다.
///
/// static 이라 씬이 바뀌어도 값이 살아남는다 — 게임을 껐다 켜면 사라지지만,
/// 지금은 저장 기능이 없으니 그게 맞아. 나중에 필요해지면 그때 붙이면 돼.
/// </summary>
public static class GameSelection
{
    /// <summary>로비에 세워둔 자리 개수. 캐릭터가 3명이어도 자리는 6개다.</summary>
    public const int StandCount = 6;

    // <b>PlayerPrefs 에 남긴다.</b> static 만으로는 같은 실행 안에서만 살아남는데,
    // 유저가 전시실에 갔다 트랙으로 오니 세진 카트가 나왔다(2026-09-17) —
    // 씬을 넘나드는 사이에 값이 날아갈 구멍이 있었다. 수집품과 같은 방식으로 저장한다.
    const string Key = "고른캐릭터";

    static int index = -2;      // -2 = 아직 안 읽음
    static string castId, displayName;

    public static int SelectedIndex { get { Load(); return index; } }
    public static string SelectedName { get { Load(); return displayName; } }

    static void Load()
    {
        if (index != -2) return;
        castId = PlayerPrefs.GetString(Key, "");
        index = string.IsNullOrEmpty(castId) ? -1 : 0;
        displayName = Cast.NameOf(castId);
    }

    /// <summary>
    /// Cast.cs 의 id ("세운" 처럼). 카트 색과 이름표 색이 이걸로 정해진다.
    /// 자리 번호(SelectedIndex)만 들고 다니면 자리 순서를 바꿀 때마다 카트가 뒤바뀐다.
    /// </summary>
    public static string SelectedCastId { get { Load(); return castId ?? ""; } }

    public static bool HasSelection => SelectedIndex >= 0;

    public static void Select(int slot, string name, string id)
    {
        index = slot;
        castId = id ?? "";
        displayName = string.IsNullOrEmpty(name) ? $"#{slot + 1}" : name;

        PlayerPrefs.SetString(Key, castId);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        index = -1;
        castId = "";
        displayName = "";

        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
