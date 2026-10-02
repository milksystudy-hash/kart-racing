using UnityEngine;

/// <summary>
/// 따라 그리기 화면. 급식·안전훈련과 <b>같은 자리에 같은 모양</b>으로 붙인다 —
/// 한 게임 안에서 계기판이 판마다 다른 데 있으면 그게 제일 어설프다.
///
/// <b>큰 패널이 떠 있을 때는 접는다</b>(<see cref="MinigameFlow.Blocking"/>) —
/// 「큰 패널은 한 번에 한 장」 은 레이스 ESC 패널에서 세운 규칙이다.
/// </summary>
public class TracingHUD : MonoBehaviour
{
    TracingGame game;
    MinigameFlow flow;
    Font font;

    void Start()
    {
        game = GetComponent<TracingGame>();
        flow = GetComponent<MinigameFlow>();
        var campus = FindFirstObjectByType<CampusHUD>();
        if (campus != null) font = campus.uiFont;
    }

    void OnGUI()
    {
        if (game == null || flow == null) return;
        if (flow.Blocking) return;

        Rect screen = Hud.Begin(font);
        float w = screen.width, h = screen.height;

        if (flow.Now == MinigameFlow.Step.끝) Result(w, h);
        else if (flow.Running) Playing(w, h);

        Hud.End();
    }

    void Playing(float w, float h)
    {
        // ── 왼쪽 위 — 무엇을 그리나 ──
        // 진행 막대가 한 줄 늘어서 86 → 124. <b>칸을 안 늘리면 종이 밖으로 나간다</b>
        // (Hud.Inner 는 높이에서 16 을 뺀다 — 이 프로젝트에서 두 번 겪은 자리야).
        var head = new Rect(24f, 20f, 268f, 124f);
        Hud.Panel(head);
        Rect hi = Hud.Inner(head);

        GUI.Label(new Rect(hi.x + 14f, hi.y + 6f, hi.width - 28f, 26f),
                  game.Shape.name, Hud.Resize(Hud.Title, 22, TextAnchor.MiddleLeft));
        GUI.Label(new Rect(hi.x + 14f, hi.y + 34f, hi.width - 28f, 20f),
                  $"도형 {game.Round + 1} / {Tracing.Rounds}",
                  Hud.Resize(Hud.Label, 14, TextAnchor.MiddleLeft));

        // 남은 시간 막대 — 숫자보다 <b>줄어드는 것</b>이 빨리 읽힌다
        float frac = Mathf.Clamp01(game.Left / Tracing.PerShape);
        var bar = new Rect(hi.x + 14f, hi.y + 58f, hi.width - 28f, 10f);
        GUI.DrawTexture(bar, Hud.WoodDarkTex);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * frac, bar.height),
                        frac < 0.25f ? Hud.RibbonTex : Hud.BrassTex);

        // ── 진행 — ★ <b>덮은 만큼 찬다</b> ──
        // 이게 없으면 18초 동안 «잘하고 있나» 를 알 방법이 없어서, 그리기가 아니라
        // <b>그냥 문지르기</b>가 된다. 남은 시간(금색)과 <b>다른 색</b>으로 칠해야
        // 두 막대가 서로 다른 말을 하는 게 보인다.
        float cov = game.LiveCoverage;
        GUI.Label(new Rect(hi.x + 14f, hi.y + 72f, 100f, 18f), "진행",
                  Hud.Resize(Hud.Label, 13, TextAnchor.MiddleLeft));
        GUI.Label(new Rect(hi.x + hi.width - 70f, hi.y + 72f, 56f, 18f), $"{cov * 100f:0}%",
                  Hud.Resize(Hud.Label, 13, TextAnchor.MiddleRight));

        var cbar = new Rect(hi.x + 14f, hi.y + 90f, hi.width - 28f, 10f);
        GUI.DrawTexture(cbar, Hud.WoodDarkTex);
        GUI.DrawTexture(new Rect(cbar.x, cbar.y, cbar.width * cov, cbar.height), Hud.PaperTex);

        // ── 오른쪽 위 — 점수 ──
        var sc = new Rect(w - 24f - 176f, 20f, 176f, 66f);
        Hud.Panel(sc);
        Rect si = Hud.Inner(sc);
        GUI.Label(new Rect(si.x, si.y + 4f, si.width, 18f), "점수",
                  Hud.Resize(Hud.Label, 13, TextAnchor.UpperCenter));
        GUI.Label(new Rect(si.x, si.y + 20f, si.width, 34f), game.Score.ToString(),
                  Hud.Resize(Hud.Value, 28, TextAnchor.UpperCenter));

        // ── 가운데 아래 — 한 줄 안내 ──
        if (!game.BoardBound)
        {
            var warn = new Rect(w * 0.5f - 250f, h - 96f, 500f, 34f);
            Hud.Panel(warn);
            GUI.Label(warn, "이젤 화판을 못 찾았다 — 재주관에 In_이젤 이 있는지 확인해라",
                      Hud.Resize(Hud.Text, 14, TextAnchor.MiddleCenter));
        }
        else if (!game.Showing)
        {
            GUI.Label(new Rect(0f, h - 58f, w, 24f),
                      game.Drawn.Count < 2
                        ? "마우스 왼쪽 버튼을 누른 채로 흐린 윤곽 위를 덧그린다"
                        : "다 그렸으면 ENTER  ·  ESC 멈추기",
                      Hud.Resize(Hud.Text, 15, TextAnchor.UpperCenter));
        }

        // ── 회차 결과 ── 한 도형이 끝나면 <b>왜 그 점수인지</b>를 잠깐 띄운다
        if (game.Showing) Mark(w, h);
    }

    /// <summary>
    /// 한 도형의 채점. <b>숫자 하나만 주면 다음 판에 무엇을 고칠지 모른다</b> —
    /// 「다 그렸나」와 「딴 데 안 칠했나」를 따로 보여야 «어디가 문제인지» 가 보인다.
    /// </summary>
    void Mark(float w, float h)
    {
        var box = new Rect(w * 0.5f - 190f, h * 0.5f - 86f, 380f, 172f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x, inner.y + 10f, inner.width, 32f),
                  $"{game.Last.score}점", Hud.Resize(Hud.Big, 34, TextAnchor.UpperCenter));
        GUI.Label(new Rect(inner.x, inner.y + 48f, inner.width, 22f),
                  game.Last.Verdict, Hud.Resize(Hud.Title, 16, TextAnchor.UpperCenter));
        Hud.Rule(inner.x + 24f, inner.y + 76f, inner.width - 48f);

        Row(inner, 0, "다 그렸나", game.Last.coverage);
        Row(inner, 1, "딴 데 안 칠했나", game.Last.accuracy);
    }

    void Row(Rect inner, int i, string what, float v)
    {
        float y = inner.y + 88f + i * 30f;
        GUI.Label(new Rect(inner.x + 26f, y, 150f, 24f), what,
                  Hud.Resize(Hud.Label, 14, TextAnchor.MiddleLeft));

        var bar = new Rect(inner.x + 182f, y + 7f, 120f, 10f);
        GUI.DrawTexture(bar, Hud.WoodDarkTex);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(v), bar.height),
                        Hud.BrassTex);
        GUI.Label(new Rect(inner.x + 310f, y, 48f, 24f), $"{v * 100f:0}%",
                  Hud.Resize(Hud.Text, 14, TextAnchor.MiddleRight));
    }

    void Result(float w, float h)
    {
        var box = new Rect(w * 0.5f - 210f, h * 0.5f - 130f, 420f, 260f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x, inner.y + 12f, inner.width, 28f),
                  "따라 그리기 — 끝", Hud.Resize(Hud.Title, 22, TextAnchor.UpperCenter));
        Hud.Rule(inner.x + 18f, inner.y + 48f, inner.width - 36f);

        GUI.Label(new Rect(inner.x, inner.y + 62f, inner.width, 52f),
                  game.Score.ToString(), Hud.Resize(Hud.Big, 46, TextAnchor.UpperCenter));
        GUI.Label(new Rect(inner.x, inner.y + 114f, inner.width, 20f),
                  $"도형 {Tracing.Rounds}개 합계 · 만점 {Tracing.Rounds * 100}",
                  Hud.Resize(Hud.Label, 13, TextAnchor.UpperCenter));

        // 등급 — <b>«잘한 건가» 에 답한다.</b> 숫자만으로는 모른다
        var badge = new Rect(inner.x + inner.width * 0.5f - 42f, inner.y + 140f, 84f, 42f);
        Hud.Chip(badge);
        GUI.Label(badge, flow.Grade(game.Score), Hud.Resize(Hud.Big, 30, TextAnchor.MiddleCenter));

        GUI.Label(new Rect(inner.x, inner.yMax - 30f, inner.width, 22f),
                  "ESC  나가기 / 처음부터", Hud.Resize(Hud.Text, 14, TextAnchor.UpperCenter));
    }
}
