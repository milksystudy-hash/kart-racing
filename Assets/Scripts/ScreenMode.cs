using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ★★ <b>전체화면 ↔ 창모드.</b> (2026-10-14 제작자 요청)
///
/// 제작자: *"아 그리고 전체화면, 창화면 그것도 만들고 싶은데."*
///
/// ── 왜 따로 한 파일인가 ────────────────────────────────────────────────
/// 바꾸는 자리가 <b>둘</b>이다 — 타이틀의 환경설정 창(마우스로 고르는 사람)과
/// <b>F11</b>(게임 중에 바로 바꾸는 사람). 두 군데가 각자 <c>Screen.fullScreenMode</c> 를
/// 만지면 «지금 어느 쪽인가» 가 두 군데에 저장되고, 이 프로젝트에서 그건 반드시 어긋난다
/// (같은 값을 두 군데 두면 어긋난다 — 장 번호와 수집 기록에서 겪은 것).
/// <b>여기가 유일한 주인</b>이고 나머지는 <see cref="Toggle"/> 만 부른다.
///
/// ── 저장한다 ───────────────────────────────────────────────────────────
/// 매번 들어올 때마다 다시 바꿔야 하면 바꿨다는 의미가 없다
/// (<see cref="ScreenEffects"/> 의 「이펙트」와 같은 판단). PlayerPrefs 에 남긴다.
///
/// ★ <b>에디터에서는 창 크기를 안 건드린다.</b> Game 뷰는 유니티가 관리하는 거라
///   여기서 <c>SetResolution</c> 을 부르면 작업하던 뷰가 멋대로 바뀐다.
///   빌드에서만 실제로 적용되고, 에디터에서는 «눌렸다» 는 표시만 뜬다.
/// </summary>
public static class ScreenMode
{
    const string Key = "화면모드";          // 1 = 전체화면, 0 = 창모드

    /// <summary>창모드일 때 쓰는 크기. 설계 기준(1280×720)과 같은 비율이라 HUD 가 제일 잘 맞는다.</summary>
    const int WinW = 1280, WinH = 720;

    /// <summary>지금 전체화면인가.</summary>
    public static bool Full { get; private set; } = true;

    /// <summary>방금 바뀐 것을 화면에 한 번 알리기 위한 문구. 비어 있으면 알릴 게 없다.</summary>
    public static string Notice { get; private set; } = "";
    public static float NoticeAt { get; private set; } = -99f;

    public static string Label => Full ? "전체화면" : "창모드";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        Full = PlayerPrefs.GetInt(Key, 1) == 1;
        Apply();

        // F11 은 어느 씬에서나 먹어야 한다 — 씬마다 붙이면 한 군데를 반드시 빠뜨린다.
        if (Object.FindFirstObjectByType<Watcher>() == null)
            Object.DontDestroyOnLoad(new GameObject("ScreenMode").AddComponent<Watcher>());
    }

    public static void Set(bool full)
    {
        if (Full == full) return;
        Full = full;
        PlayerPrefs.SetInt(Key, full ? 1 : 0);
        PlayerPrefs.Save();
        Apply();

        Notice = Label;
        NoticeAt = Time.unscaledTime;
    }

    public static void Toggle() => Set(!Full);

    static void Apply()
    {
#if UNITY_EDITOR
        // Game 뷰는 유니티 것이다. 건드리지 않는다.
#else
        if (Full)
        {
            // ★ ExclusiveFullScreen 이 아니라 <b>FullScreenWindow</b>(테두리 없는 전체창)다.
            //   전환이 빠르고, 알트탭이 깨지지 않고, 멀티 모니터에서 사고가 안 난다.
            var d = Screen.currentResolution;
            Screen.SetResolution(d.width, d.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(WinW, WinH, FullScreenMode.Windowed);
        }
#endif
    }

    /// <summary>F11 만 듣는다. 씬을 넘어 살아남는다.</summary>
    class Watcher : MonoBehaviour
    {
        void Update()
        {
            if (Dev.Typing) return;                       // 암호를 치는 중에는 모두가 손을 뗀다
            var k = Keyboard.current;
            if (k != null && k.f11Key.wasPressedThisFrame) Toggle();
        }
    }
}
