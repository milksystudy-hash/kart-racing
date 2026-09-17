using UnityEngine;

/// <summary>
/// 화면 효과(왜곡·색수차·김)를 한 번에 <b>끄고 켜는 스위치</b>.
///
/// 2026-09-17 유저: *"이펙트가 아직도 좀 신경 쓰여서, 없애고 켜는 단축키 하나 만들어줘."*
/// 세기를 줄이는 것과 <b>끌 수 있는 것</b>은 다른 문제야 — 사람마다 눈이 다르고,
/// 멀미를 타는 사람한테는 아무리 연해도 거슬린다. 접근성 설정이라고 보는 게 맞다.
///
/// 선택은 <b>PlayerPrefs 에 남는다.</b> 매번 들어올 때마다 다시 끄게 하면 껐다는 의미가 없다.
/// 나중에 유저가 UI 를 그리면 그 화면에서 이 값을 그대로 쓰면 된다 —
/// 소리 조절도 같은 자리에 붙을 거라 키를 `화면효과` 처럼 사람이 읽을 수 있게 뒀다.
/// </summary>
public static class ScreenEffects
{
    const string Key = "화면효과";

    /// <summary>켜져 있나. 기본은 켜짐.</summary>
    public static bool On
    {
        get => PlayerPrefs.GetInt(Key, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(Key, value ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }
    }

    public static void Toggle() => On = !On;

    /// <summary>
    /// 씬에 있는 효과들에 지금 설정을 먹인다. 씬을 새로 불러올 때도 불러야 해서 public.
    ///
    /// 컴포넌트를 끄는 것이지 지우는 게 아니다 — 다시 켜면 그대로 돌아온다.
    /// <see cref="SpeedRush"/> 는 꺼지면 볼륨 무게가 0 으로 남아 <b>비용도 0</b> 이 된다.
    /// </summary>
    public static void Apply()
    {
        bool on = On;

        foreach (var rush in Object.FindObjectsByType<SpeedRush>(FindObjectsSortMode.None))
        {
            if (!on) rush.Silence();
            rush.enabled = on;
        }

        foreach (var puff in Object.FindObjectsByType<KartExhaust>(FindObjectsSortMode.None))
            puff.SetOn(on);
    }
}
