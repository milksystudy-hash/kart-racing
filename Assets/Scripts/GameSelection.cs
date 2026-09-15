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

    public static int SelectedIndex { get; private set; } = -1;
    public static string SelectedName { get; private set; } = "";

    /// <summary>
    /// Cast.cs 의 id ("세운" 처럼). 카트 색과 이름표 색이 이걸로 정해진다.
    /// 자리 번호(SelectedIndex)만 들고 다니면 자리 순서를 바꿀 때마다 카트가 뒤바뀐다.
    /// </summary>
    public static string SelectedCastId { get; private set; } = "";

    public static bool HasSelection => SelectedIndex >= 0;

    public static void Select(int index, string name, string castId)
    {
        SelectedIndex = index;
        SelectedName = string.IsNullOrEmpty(name) ? $"#{index + 1}" : name;
        SelectedCastId = castId ?? "";
    }

    public static void Clear()
    {
        SelectedIndex = -1;
        SelectedName = "";
        SelectedCastId = "";
    }
}
