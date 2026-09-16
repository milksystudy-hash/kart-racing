using UnityEngine;

/// <summary>
/// 화면 가운데 위에 잠깐 떴다 사라지는 알림 한 줄. "기념 코인을 얻었습니다" 같은 것.
///
/// 예전엔 이 글이 ExhibitPickup 안에 들어 있었다. 그런데 수집품을 <b>주워서</b> 얻는 게 아니라
/// <b>임무를 깨서</b> 얻는 방식으로 바꾸면서(2026-09-16), 알림을 띄우는 쪽이 둘이 됐다.
/// 그래서 알림만 따로 떼어냈다 — HUD 는 "누가 띄웠는지" 를 알 필요가 없어.
/// </summary>
public static class Toast
{
    public static string Message { get; private set; } = "";
    public static float ShownAt { get; private set; } = -99f;

    /// <summary>몇 초 동안 떠 있을지.</summary>
    public const float Seconds = 3.5f;

    public static bool Visible => !string.IsNullOrEmpty(Message) && Time.time - ShownAt <= Seconds;

    public static void Show(string message)
    {
        Message = message;
        ShownAt = Time.time;
    }

    public static void Clear()
    {
        Message = "";
        ShownAt = -99f;
    }
}
