using UnityEngine;

/// <summary>
/// <b>지금 마우스를 UI 가 쓰고 있나.</b>
///
/// 2026-10-02 유저: *"아이템을 마우스로 잡고 돌리면 아이템뿐만 아니라
/// <b>바깥 배경까지 같이</b> 돌아간다."* 당연한 일이었다 — 궤도 카메라도,
/// 진열장 고르기도 같은 왼쪽 버튼을 읽고 있으니 한 번 끄는 것을 <b>셋이 나눠 받는다.</b>
///
/// IMGUI 에는 «이 사각형이 마우스를 먹었다» 를 알리는 길이 없고(uGUI 의 EventSystem 은
/// 이 프로젝트에 없다), 그렇다고 HUD 가 카메라와 고르기를 직접 끄러 다니면
/// <b>새 HUD 를 만들 때마다 끄는 곳을 하나씩 더 찾아야</b> 한다.
///
/// 그래서 <b>깃발 하나</b>를 둔다. 패널이 «내가 쓰는 중» 이라고 말하면
/// 카메라와 고르기가 스스로 비킨다.
///
/// ★ <b>프레임 번호로 들고 있다가 저절로 풀린다.</b> <c>bool</c> 로 두면 «풀어주는 자리»를
/// 한 군데라도 빠뜨렸을 때 마우스가 영영 잠긴다 — 이 프로젝트에서 일시정지로 겪은 그 함정이야.
/// 매 프레임 <see cref="Capture"/> 를 안 부르면 다음 프레임에 자동으로 풀린다.
/// </summary>
public static class UiFocus
{
    static int capturedFrame = -99;

    /// <summary>
    /// 패널이 매 프레임 부른다. 한 프레임만 쉬어도 바로 풀린다.
    /// </summary>
    public static void Capture() => capturedFrame = Time.frameCount;

    /// <summary>
    /// 지금 UI 가 마우스를 쓰고 있나. <b>한 프레임 여유를 둔다</b> —
    /// HUD 의 Update 가 카메라보다 늦게 돌 수도 있고, 그러면 첫 프레임을 놓친다.
    /// </summary>
    public static bool MouseOverPanel => Time.frameCount - capturedFrame <= 1;
}
