using UnityEngine;

/// <summary>
/// <b>오늘의 급식</b> 화면. <see cref="Canteen"/> 이 규칙, 여기는 그리기만.
///
/// 좌표는 전부 <b>가상 1280×720</b>(<see cref="Hud.Begin"/>)이고, 세로 가운데를 기준으로
/// 쌓는다 — 창이 납작해도 위아래가 안 잘린다. 이 프로젝트에서 HUD 고정 좌표로 세 번 겹쳤다
/// (지도 · 개발 정보 칩 · 카운트다운).
/// </summary>
public class CanteenHUD : MonoBehaviour
{
    Font font;
    Canteen game;

    /// <summary>판정 한 줄이 떠 있는 시간.</summary>
    const float VerdictSeconds = 1.4f;

    void Awake()
    {
        game = GetComponent<Canteen>();
        // 캠퍼스 HUD 가 이미 폰트를 들고 있다. 같은 글꼴을 써야 한 게임으로 보인다.
        var campus = FindFirstObjectByType<CampusHUD>();
        if (campus != null) font = campus.uiFont;
    }

    void OnGUI()
    {
        if (game == null) return;

        Rect screen = Hud.Begin(font);
        float w = screen.width, h = screen.height;

        const float panelW = 560f;
        float x = w * 0.5f - panelW * 0.5f;
        float top = h * 0.5f - 210f;

        if (game.Now == Canteen.Phase.진행) DrawPlaying(x, top, panelW);
        else DrawResult(x, top, panelW);

        Hud.End();
    }

    // ── 진행 중 ───────────────────────────────────────────────────────────────

    void DrawPlaying(float x, float top, float panelW)
    {
        DrawHeader(x, top, panelW);
        DrawQueue(x, top + 52f, panelW);
        DrawOrder(x, top + 90f, panelW);
        DrawTray(x, top + 206f, panelW);
        DrawFooter(x, top + 302f, panelW);
    }

    /// <summary>제목과 남은 시간. 시간은 <b>오른쪽 끝에 고정</b>이라 자릿수가 변해도 안 흔들린다.</summary>
    void DrawHeader(float x, float y, float panelW)
    {
        var box = new Rect(x, y, panelW, 44f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x + 8f, inner.y, 260f, inner.height),
                  "오늘의 급식", Hud.Resize(Hud.Title, 20, TextAnchor.MiddleLeft));

        int secs = Mathf.CeilToInt(game.Left);
        var clock = Hud.Resize(Hud.Value, 22, TextAnchor.MiddleRight);
        // 마지막 10초는 붉은 띠 색으로. 숫자만 줄어들면 끝나가는 걸 눈치 못 챈다.
        if (secs <= 10) clock.normal.textColor = Hud.Ribbon;
        GUI.Label(new Rect(inner.xMax - 160f, inner.y, 152f, inner.height),
                  $"{secs / 60}:{secs % 60:00}", clock);
    }

    /// <summary>뒤에 서 있는 손님. <b>다음에 뭐가 올지</b> 보여야 마음의 준비가 된다.</summary>
    void DrawQueue(float x, float y, float panelW)
    {
        var waiting = game.Queue;
        if (waiting.Count <= 1) return;

        var chip = new Rect(x, y, panelW, 30f);
        Hud.Chip(chip);

        var label = Hud.Resize(Hud.Label, 13, TextAnchor.MiddleLeft);
        GUI.Label(new Rect(chip.x + 12f, chip.y, 74f, chip.height), "다음", label);

        var text = Hud.Resize(Hud.Text, 13, TextAnchor.MiddleLeft);
        var sb = new System.Text.StringBuilder();
        for (int i = 1; i < waiting.Count; i++)
        {
            if (sb.Length > 0) sb.Append("      ");
            sb.Append(waiting[i].Label);
        }
        GUI.Label(new Rect(chip.x + 62f, chip.y, chip.width - 74f, chip.height), sb.ToString(), text);
    }

    /// <summary>지금 손님의 주문표. <b>화면에 답이 적혀 있다</b> — 이게 이 게임의 조작 설명 전부다.</summary>
    void DrawOrder(float x, float y, float panelW)
    {
        var box = new Rect(x, y, panelW, 104f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x, inner.y + 2f, inner.width, 18f),
                  "손님 주문", Hud.Resize(Hud.Label, 13, TextAnchor.UpperCenter));

        string order = game.Queue.Count > 0 ? game.Queue[0].Label : "";
        GUI.Label(new Rect(inner.x, inner.y + 26f, inner.width, 52f),
                  order, Hud.Resize(Hud.Title, 30, TextAnchor.MiddleCenter));
    }

    /// <summary>
    /// 식판 다섯 칸. <b>주문에 든 칸을 짚어주지 않는다</b> — 짚어주면 읽을 필요가 없어져서
    /// «읽고 고르는 게임」이 «색칠된 데를 누르는 게임」이 된다.
    /// </summary>
    void DrawTray(float x, float y, float panelW)
    {
        const int n = CanteenOrder.Slots;
        const float gap = 10f;
        float cellW = (panelW - gap * (n - 1)) / n;

        var key = Hud.Resize(Hud.Value, 22, TextAnchor.UpperCenter);
        var name = Hud.Resize(Hud.Text, 15, TextAnchor.LowerCenter);

        for (int i = 0; i < n; i++)
        {
            var cell = new Rect(x + i * (cellW + gap), y, cellW, 84f);
            bool filled = (game.Tray & (1 << i)) != 0;

            // 담긴 칸은 종이, 빈 칸은 어두운 나무. 색만 다른 게 아니라 <b>재질이 다르게</b>
            // 보여야 «담겼다» 가 한눈에 온다.
            GUI.DrawTexture(cell, filled ? Hud.PaperTex : Hud.WoodDarkTex);
            GUI.DrawTexture(new Rect(cell.x, cell.y, cell.width, 3f), Hud.WoodTex);

            key.normal.textColor = filled ? Hud.Ink : Hud.Paper;
            name.normal.textColor = filled ? Hud.Ink : Hud.Paper;

            GUI.Label(new Rect(cell.x, cell.y + 10f, cell.width, 30f), $"{i + 1}", key);
            GUI.Label(new Rect(cell.x, cell.yMax - 34f, cell.width, 24f), CanteenOrder.Names[i], name);
        }
    }

    /// <summary>안내 · 점수 · 방금 판정.</summary>
    void DrawFooter(float x, float y, float panelW)
    {
        var box = new Rect(x, y, panelW, 40f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x + 8f, inner.y, 300f, inner.height),
                  "ENTER  내보내기      ESC  그만두기",
                  Hud.Resize(Hud.Text, 14, TextAnchor.MiddleLeft));

        GUI.Label(new Rect(inner.xMax - 230f, inner.y, 222f, inner.height),
                  $"{game.Served}명   {game.Score}점",
                  Hud.Resize(Hud.Value, 18, TextAnchor.MiddleRight));

        // 방금 낸 판정. 패널 <b>밖 아래</b>에 둔다 — 안에 넣으면 점수 줄과 부딪힌다.
        if (string.IsNullOrEmpty(game.Verdict)) return;
        if (Time.time - game.VerdictAt > VerdictSeconds) return;

        var chip = new Rect(x + panelW * 0.5f - 130f, y + 48f, 260f, 28f);
        Hud.Chip(chip);
        GUI.Label(chip, game.Verdict, Hud.Resize(Hud.Title, 15, TextAnchor.MiddleCenter));
    }

    // ── 마감 ─────────────────────────────────────────────────────────────────

    void DrawResult(float x, float top, float panelW)
    {
        var box = new Rect(x + 60f, top + 60f, panelW - 120f, 228f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x, inner.y + 10f, inner.width, 30f),
                  "배식 마감", Hud.Resize(Hud.Title, 24, TextAnchor.UpperCenter));
        Hud.Rule(inner.x + 14f, inner.y + 48f, inner.width - 28f);

        GUI.Label(new Rect(inner.x, inner.y + 62f, inner.width, 56f),
                  $"{game.Score}", Hud.Resize(Hud.Big, 46, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(inner.x, inner.y + 118f, inner.width, 20f),
                  $"손님 {game.Served}명", Hud.Resize(Hud.Label, 14, TextAnchor.UpperCenter));

        var best = Hud.Resize(Hud.Text, 15, TextAnchor.UpperCenter);
        if (game.NewBest) best.normal.textColor = Hud.Brass;
        GUI.Label(new Rect(inner.x, inner.y + 146f, inner.width, 22f),
                  game.NewBest ? $"최고 기록 {game.Best}점" : $"최고 {game.Best}점", best);

        GUI.Label(new Rect(inner.x, inner.yMax - 30f, inner.width, 22f),
                  "ENTER  다시      ESC  나가기",
                  Hud.Resize(Hud.Text, 14, TextAnchor.UpperCenter));
    }
}
