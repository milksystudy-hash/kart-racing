using UnityEngine;

/// <summary>
/// ★ <b>화면이 한 번 번쩍한다.</b> (2026-10-12)
///
/// 제작자: *"점검등 맞았을 때 속도 느려지는 게 체감이 안 된다. 잠시 휘청거리는 건 어떨까 —
/// 눈뽕이라는 의미에서."* 속도는 숫자라 안 보이지만 <b>번쩍임은 안 볼 수가 없다.</b>
///
/// ⚠ <b>조향은 절대 안 건드린다.</b> 「담장 긁지 않기」 판에서 쓰이는 연출이라,
/// 차를 비틀면 내가 플레이어를 벽에 밀어 놓고 실패시키는 꼴이 된다.
/// 화면만 때리고 차는 제 길을 간다.
///
/// 스스로 씬에 들어온다 — 씬을 다시 구울 필요가 없다.
/// 쓰는 쪽은 <see cref="Hit"/> 한 줄이면 끝이고, 아무도 안 부르면 공짜다.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    static ScreenFlash live;
    static float until, strength;
    static Color tint = Color.white;
    static float span = 0.34f;

    /// <summary>한 번 번쩍. <paramref name="amount"/> 는 0~1.</summary>
    public static void Hit(float amount, Color colour, float seconds = 0.34f)
    {
        Ensure();
        strength = Mathf.Clamp01(amount);
        tint = colour;
        span = Mathf.Max(0.05f, seconds);
        until = Time.unscaledTime + span;
    }

    static void Ensure()
    {
        if (live != null) return;
        var go = new GameObject("ScreenFlash");
        DontDestroyOnLoad(go);
        live = go.AddComponent<ScreenFlash>();
    }

    void OnGUI()
    {
        float left = until - Time.unscaledTime;
        if (left <= 0f) return;

        // 확 켜졌다가 천천히 빠진다 — 반대로 하면 «화면이 밝아졌다» 지 «맞았다» 가 아니다.
        float t = left / span;
        float a = strength * t * t;

        GUI.depth = -100;                      // 숫자보다도 앞
        var keep = GUI.color;
        GUI.color = new Color(tint.r, tint.g, tint.b, a);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = keep;
    }
}
