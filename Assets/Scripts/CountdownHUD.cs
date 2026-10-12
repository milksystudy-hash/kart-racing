using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ★★★ <b>3 · 2 · 1 · 출발!</b> — 숫자와 소리를 같이 맡는다. (2026-10-12)
///
/// 전에는 이게 <see cref="TestHUD"/> 안에 있었고, 수연이 <b>세 번</b> 같은 신고를 했다:
/// *"이야기 보고 레이싱 들어가면 321 표시도 음성도 안 뜬다."* 두 번 고쳤는데 또 나왔다.
/// 같은 증상을 두 번 고쳐서 안 되면 <b>고치는 대신 방법이 맞는지</b> 물어야 한다.
///
/// 틀린 건 자리였다. 카운트가 <c>TestHUD</c> 안에 있으면 <b>그 컴포넌트가 돌아야만</b>
/// 보인다 — <c>InKart</c>(모드 스위처), 컴포넌트 활성, 씬이 구워진 시점,
/// ESC 패널, 큰 패널 우선순위… <b>안 뜰 수 있는 길이 다섯 개</b>다.
/// 그래서 <b>스스로 씬에 들어오는 제 컴포넌트</b>로 뺐다 — 이 프로젝트에서
/// 「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」를 다섯 번 겪은 뒤의 기본형이고,
/// 이제 <c>LapTracker</c> 가 있는 씬이면 <b>무조건</b> 그린다.
///
/// ★ <b>시계는 «그려진 프레임» 에서만 간다.</b> 전에는 네 프레임 안에 아무도 안 그리면
///   저절로 시작한 것으로 쳤는데, 그러면 <b>화면에 한 번도 안 뜬 채로 3초가 타버린다</b> —
///   「코드는 도는데 화면은 그대로」의 전형이다. 지금은 안 그려지면 시계가 아예 안 간다.
///
/// ★ 끝날 때 <b>몇 프레임이나 그려졌는지</b>를 콘솔에 남긴다. 다음에 또 안 보이면
///   그 숫자 하나로 «안 그려졌다» 와 «그렸는데 못 봤다» 가 갈린다.
/// </summary>
public class CountdownHUD : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Install();
    }

    static void OnLoaded(Scene s, LoadSceneMode m) => Install();

    static void Install()
    {
        // 레이스가 있는 씬에만. 로비·전시실·캠퍼스에는 셀 것이 없다.
        if (Object.FindFirstObjectByType<LapTracker>() == null) return;
        if (Object.FindFirstObjectByType<CountdownHUD>() != null) return;
        new GameObject("CountdownHUD").AddComponent<CountdownHUD>();
    }

    string lastLabel = "";
    int drawnFrames;
    bool told;

    void Update()
    {
        // ★ 소리는 <b>Update 에서</b> 낸다 — OnGUI 는 한 프레임에 두 번 돌아 소리가 겹친다.
        string cd = RaceCountdown.Label;
        if (cd == lastLabel) return;

        string was = lastLabel;
        lastLabel = cd;

        // 수연이 구한 음원은 «삐 · 삐 · 삐 · 빵» 이 <b>한 파일에</b> 들어 있다.
        // 숫자마다 틀면 넷이 겹쳐서 뭉갠다 — 셀 때 한 번이다.
        if (string.IsNullOrEmpty(was) && !string.IsNullOrEmpty(cd))
            Sfx.Play("Countdown", 0.9f, duckMusic: false);
        else if (!string.IsNullOrEmpty(cd) && cd != "출발!")
            Sfx.Play("CountdownTick", 0.9f, duckMusic: false);

        if (cd == "출발!") Sfx.Play("CountdownGo", 1f, duckMusic: false);

        if (string.IsNullOrEmpty(cd) && !string.IsNullOrEmpty(was) && !told)
        {
            told = true;
            Debug.Log($"[카운트] 끝 · 화면에 그려진 프레임 {drawnFrames}" +
                      (drawnFrames < 30 ? "  ← 이 숫자가 작으면 화면에 안 뜬 것이다" : ""));
        }
    }

    void OnGUI()
    {
        // 제일 앞에 그린다. 숫자가 다른 판 뒤로 가면 없는 것과 같다.
        GUI.depth = -50;

        // 브리핑 카드나 티키타카가 떠 있는 동안은 <b>시계를 시작하지 않는다.</b>
        // 그때 시작하면 카드 뒤에서 3초가 타버린다.
        if (RaceBriefing.Open || RaceBriefing.Chatting) return;

        RaceCountdown.Shown();
        string label = RaceCountdown.Label;
        if (string.IsNullOrEmpty(label)) return;

        if (Event.current.type == EventType.Repaint) drawnFrames++;
        told = false;

        var screen = Hud.Begin(Resources.Load<Font>("HudFont"));
        float w = screen.width, h = screen.height;

        bool go = label == "출발!";

        // ★ 판을 안 깐다. 카운트는 3초짜리고 그동안 카트가 아예 안 움직이니
        //   화면 한가운데를 통째로 써도 손해가 없다. 대신 종이 판이 없으면 밝은 노면에서
        //   묻히니까 <b>글자 뒤에 그림자를 넉 장</b> 깔아 어느 배경에서도 테두리가 생기게 한다.
        var style = Hud.Resize(Hud.Title, Mathf.RoundToInt((go ? 64 : 120) * RaceCountdown.Pop),
                               TextAnchor.MiddleCenter);
        var area = new Rect(0f, h * 0.5f - 80f, w, 160f);

        var shade = Hud.Resize(Hud.Title, style.fontSize, TextAnchor.MiddleCenter);
        shade.normal.textColor = new Color(0.12f, 0.09f, 0.07f, 0.75f);
        for (int dx = -1; dx <= 1; dx += 2)
            for (int dy = -1; dy <= 1; dy += 2)
                GUI.Label(new Rect(area.x + dx * 3f, area.y + dy * 3f, area.width, area.height), label, shade);

        style.normal.textColor = go ? Hud.Brass : new Color(0.98f, 0.95f, 0.88f);
        GUI.Label(area, label, style);

        Hud.End();
    }
}
