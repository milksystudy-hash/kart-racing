using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// <b>미니게임 키 읽기를 한 군데로.</b>
///
/// 2026-09-29 유저: *"곰밥마당에서 키보드로 하는데 넘버패드있는쪽은 안되고, 한글위에
/// 달려있는 키보드만 되던데... 모든 미니게임 키보드가 그 숫자 자판이랑 오른쪽 넘버패드랑
/// 둘다 쓸수있게 해야해."*
///
/// 맞는 지적이고 <b>실제로 급식만 반쪽이었다</b> — 안전 점검 훈련은 만들 때부터 둘 다
/// 읽었는데 급식은 <c>digit1Key</c>~<c>digit5Key</c> 다섯 줄이 전부였다.
/// 게임마다 키를 따로 적으면 <b>새 게임을 만들 때마다 같은 것을 빠뜨린다</b> —
/// 그래서 읽는 자리를 하나로 모은다. 여기만 고치면 세 게임이 같이 고쳐진다.
///
/// ★ <b>키패드는 «있으면 좋은 것» 이 아니라 이 게임들의 제 자리다.</b>
/// 안전 점검 훈련의 곰발 판은 3 × 3 이고, 그 모양을 그대로 가진 자판이 키패드다.
/// 급식의 다섯 칸도 왼손이 숫자열, 오른손이 키패드 — 사람마다 편한 손이 다르다.
/// </summary>
public static class Keys
{
    /// <summary>
    /// 숫자 <paramref name="n"/>(1~9) 을 <b>이번 프레임에 눌렀나</b>.
    /// 숫자열과 키패드를 <b>둘 다</b> 본다.
    /// </summary>
    public static bool Digit(Keyboard k, int n)
    {
        if (k == null || n < 1 || n > 9) return false;

        KeyControl row = n switch
        {
            1 => k.digit1Key, 2 => k.digit2Key, 3 => k.digit3Key,
            4 => k.digit4Key, 5 => k.digit5Key, 6 => k.digit6Key,
            7 => k.digit7Key, 8 => k.digit8Key, _ => k.digit9Key,
        };
        KeyControl pad = n switch
        {
            1 => k.numpad1Key, 2 => k.numpad2Key, 3 => k.numpad3Key,
            4 => k.numpad4Key, 5 => k.numpad5Key, 6 => k.numpad6Key,
            7 => k.numpad7Key, 8 => k.numpad8Key, _ => k.numpad9Key,
        };

        return row.wasPressedThisFrame || pad.wasPressedThisFrame;
    }

    /// <summary>
    /// 「확인·내보내기」. ENTER · 키패드 ENTER · <b>SPACE</b> 를 다 받는다.
    ///
    /// SPACE 를 넣은 이유: 급식에서 왼손은 1~5 에 올라가 있는데 ENTER 는 <b>오른쪽 끝</b>이라
    /// 손이 계속 왕복한다 — 그게 «뻑뻑한 조작감» 의 정체다. 엄지로 닿는 키를 하나 열어 둔다.
    /// </summary>
    public static bool Confirm(Keyboard k) =>
        k != null && (k.enterKey.wasPressedThisFrame
                   || k.numpadEnterKey.wasPressedThisFrame
                   || k.spaceKey.wasPressedThisFrame);

    /// <summary>「나가기·멈춤」. ESC 하나뿐이다 — 이것까지 여러 개면 실수로 나가진다.</summary>
    public static bool Escape(Keyboard k) => k != null && k.escapeKey.wasPressedThisFrame;

    /// <summary>위로 한 칸. 화살표와 W, 키패드 8.</summary>
    public static bool Up(Keyboard k) =>
        k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame
                   || k.numpad8Key.wasPressedThisFrame);

    /// <summary>아래로 한 칸. 화살표와 S, 키패드 2.</summary>
    public static bool Down(Keyboard k) =>
        k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame
                   || k.numpad2Key.wasPressedThisFrame);
}
