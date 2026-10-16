using UnityEngine;

/// <summary>
/// ★★★ <b>프레임을 묶는다.</b> (2026-10-16)
///
/// 제작자: *"게임 킬 때랑 컷씬 볼 때 지지직 소리가 난다. 음악을 0 으로 내려도 그대로다."*
///
/// 음악도 효과음도 아니었다. 재 보니 이 게임은 <b>프레임 제한이 아예 없었다</b> —
/// <c>vSyncCount: 0</c> 에 <c>targetFrameRate</c> 를 정하는 코드가 한 줄도 없다.
/// 그러면 타이틀 화면이나 로비처럼 가벼운 장면에서 GPU 가 <b>수백에서 천 프레임</b>을
/// 쉬지 않고 그린다. 그래픽카드가 그렇게 돌면
///
/// <list type="bullet">
/// <item><b>아날로그 이어폰 잭에 전기 잡음이 올라탄다</b> — 그게 지지직이다.
///       소리 신호가 아니라 <b>간섭</b>이라 음량을 0 으로 내려도 그대로 난다</item>
/// <item>코일 노이즈와 팬 소리가 같이 커진다</item>
/// <item>전체화면과 창 모드의 부하가 달라 <b>소리도 다르게</b> 난다 — 제작자가 처음 한 말이다</item>
/// </list>
///
/// 묶으면 그 셋이 한꺼번에 줄어든다. 그리고 이건 어차피 해야 하는 일이야 —
/// 기획서 §7.6 의 목표가 <b>1280×720 @ 30fps</b> 인데, 메뉴 하나에 천 프레임을
/// 쓰는 건 그 반대쪽이다. <b>영상 녹화에도 유리하다</b>: 프레임이 고르면
/// 60fps 로 잡는 화면이 매끄럽고, 들쭉날쭉하면 끊긴 것처럼 찍힌다.
///
/// ★ 게임 플레이는 아무 것도 안 바뀐다 — 물리는 <c>FixedUpdate</c> 고
///   카메라·연출은 전부 <c>deltaTime</c> 으로 돈다.
/// </summary>
public static class FrameRate
{
    /// <summary>수직 동기가 꺼져 있거나 주사율이 높을 때의 상한.</summary>
    public const int Cap = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        // 화면 주사율에 맞춘다 — 찢어짐이 없고, 60Hz 화면에서는 그대로 60 이 된다.
        QualitySettings.vSyncCount = 1;

        // ★ 수직 동기를 쓰면 유니티가 이 값을 무시한다. 그래도 적어 둔다 —
        //   드라이버 설정으로 동기가 꺼져 있는 컴퓨터에서는 이쪽이 상한이 된다.
        Application.targetFrameRate = Cap;
    }
}
