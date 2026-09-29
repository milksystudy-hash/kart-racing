using UnityEngine;

/// <summary>
/// <b>안전 점검 훈련</b> 화면. <see cref="SafetyDrill"/> 이 규칙, 여기는 그리기만.
/// <b>나중에 진짜 UI 가 오면 버릴 스크립트야</b>(기획서 §7).
///
/// 급식(<see cref="CanteenHUD"/>)과 <b>다른 모양으로 그린다.</b> 급식은 화면 가운데 큰 판이
/// 게임판 자체지만, 여기는 <b>게임판이 방 안의 반응벽</b>이다 - 같은 자리에 큰 패널을 깔면
/// 정작 봐야 할 벽을 가린다. 그래서 기획서 §6 대로 구석 셋에만 붙는다:
/// <b>왼쪽 위 남은 시간 · 오른쪽 위 점수와 연속 · 가운데 아래 한 줄.</b>
///
/// 좌표는 전부 <b>가상 1280×720</b>(<see cref="Hud.Begin"/>)이라 어느 해상도에서도 같은
/// 비율로 앉는다.
/// </summary>
public class SafetyDrillHUD : MonoBehaviour
{
    Font font;
    SafetyDrill game;

    /// <summary>시작하고 이만큼은 «장비 착용» 한 줄이 떠 있는다(02 보호장비 보관대, 연출).</summary>
    const float GearSeconds = 2.6f;

    /// <summary>점수가 잠깐 놋쇠색으로 뜨는 시간.</summary>
    const float PopSeconds = 0.25f;

    /// <summary>
    /// 판 색. <b>static 이다</b> — 판을 다시 시작할 때마다 HUD 가 새로 만들어지는데,
    /// 인스턴스 필드로 두면 한 판 할 때마다 1×1 텍스처 다섯 장이 새로 생겨서 조용히 쌓인다
    /// (<see cref="Hud"/> 가 제 텍스처를 static 으로 들고 있는 것과 같은 이유).
    /// </summary>
    static Texture2D greenTex, redTex, darkTex, flashTex, stopTex;

    void Awake()
    {
        game = GetComponent<SafetyDrill>();
        // 캠퍼스 HUD 가 이미 폰트를 들고 있다. 같은 글꼴을 써야 한 게임으로 보인다.
        var campus = FindFirstObjectByType<CampusHUD>();
        if (campus != null) font = campus.uiFont;
    }

    /// <summary>
    /// 판 색 다섯 장. <b>한 번만 만든다</b> - <see cref="Hud.Solid"/> 를 OnGUI 에서 부르면
    /// 프레임마다 텍스처가 새로 생겨서 조용히 샌다.
    /// </summary>
    static void EnsureTextures()
    {
        if (greenTex != null) return;
        greenTex = Hud.Solid(new Color32(0x5C, 0xC4, 0x5E, 0xFF));
        redTex   = Hud.Solid(new Color32(0xD8, 0x45, 0x3C, 0xFF));
        darkTex  = Hud.Solid(new Color32(0x6B, 0x60, 0x53, 0xFF));
        flashTex = Hud.Solid(new Color32(0xFF, 0xEE, 0xBA, 0xFF));
        stopTex  = Hud.Solid(new Color32(0x39, 0x33, 0x2C, 0xFF));
    }

    void OnGUI()
    {
        if (game == null) return;
        EnsureTextures();

        Rect screen = Hud.Begin(font);
        float w = screen.width, h = screen.height;

        // ★ 준비 카드나 ESC 패널이 떠 있으면 계기판을 한 장도 안 그린다 —
        // «큰 패널은 한 번에 한 장» (레이스 ESC 패널 · 캠퍼스 HUD 와 같은 규칙).
        var flow = game.Flow;
        if (flow != null && flow.Blocking) { Hud.End(); return; }

        if (game.Now == SafetyDrill.Phase.진행)
        {
            DrawClock();
            DrawScore(w);
            // 카운트다운 중에는 아래 안내를 접는다. 큰 숫자 하나만 보여야 «시작» 이 읽힌다
            if (flow == null || flow.Running) DrawHint(w, h);
            // 반응벽을 못 찾았을 때만 화면이 판을 대신 그린다(기획서 §7 안전장치)
            if (!game.BoardBound) DrawFallbackBoard(w, h);
        }
        else DrawResult(w, h);

        Hud.End();
    }

    // ── 진행 중 ───────────────────────────────────────────────────────────────

    /// <summary>왼쪽 위 — 남은 시간. <b>오른쪽 끝에 고정</b>이라 자릿수가 변해도 안 흔들린다.</summary>
    void DrawClock()
    {
        var box = new Rect(24f, 20f, 196f, 50f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x + 6f, inner.y + 1f, 90f, inner.height),
                  "남은 시간", Hud.Resize(Hud.Label, 13, TextAnchor.MiddleLeft));

        int secs = Mathf.CeilToInt(game.Left);
        var clock = Hud.Resize(Hud.Value, 24, TextAnchor.MiddleRight);
        // 마지막 10초는 붉은 띠 색으로. 숫자만 줄어들면 끝나가는 걸 눈치 못 챈다(급식과 같다).
        if (secs <= 10) clock.normal.textColor = Hud.Ribbon;
        GUI.Label(new Rect(inner.xMax - 96f, inner.y, 90f, inner.height),
                  $"{secs / 60}:{secs % 60:00}", clock);
    }

    /// <summary>오른쪽 위 — 점수와 연속 정답.</summary>
    void DrawScore(float w)
    {
        const float wide = 238f;
        var box = new Rect(w - 24f - wide, 20f, wide, 50f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x + 6f, inner.y + 1f, 60f, inner.height),
                  "점수", Hud.Resize(Hud.Label, 13, TextAnchor.MiddleLeft));

        var value = Hud.Resize(Hud.Value, 24, TextAnchor.MiddleRight);
        // 방금 한 장 맞췄으면 잠깐 놋쇠색. 숫자가 조용히 올라가면 <b>맞았는지도 모른다</b>
        if (Time.time - LastHit < PopSeconds) value.normal.textColor = Hud.Brass;
        GUI.Label(new Rect(inner.xMax - 130f, inner.y, 124f, inner.height),
                  $"{game.Score}", value);

        // 연속은 <b>붙었을 때만</b> 뜬다. 늘 「연속 0」 이 떠 있으면 그건 정보가 아니라 장식이야.
        if (game.Streak < 2) return;

        var chip = new Rect(box.x, box.yMax + 6f, wide, 28f);
        Hud.Chip(chip);

        int bonus = SafetyDrill.Bonus(game.Streak);
        string tail = bonus > 0 ? $"한 장 {SafetyDrill.HitPoints + bonus}점"
                                : $"{SafetyDrill.StreakStart - game.Streak}번 더";
        var text = Hud.Resize(Hud.Text, 13, TextAnchor.MiddleLeft);
        if (bonus > 0) text.normal.textColor = Hud.Brass;

        GUI.Label(new Rect(chip.x + 10f, chip.y, wide - 20f, chip.height),
                  $"연속 {game.Streak}", text);
        GUI.Label(new Rect(chip.x + 10f, chip.y, wide - 20f, chip.height),
                  tail, Hud.Resize(text, 13, TextAnchor.MiddleRight));
    }

    /// <summary>이번 판에서 제일 최근에 «맞게 누른» 시각.</summary>
    float LastHit
    {
        get
        {
            float at = -99f;
            for (int i = 0; i < SafetyDrill.Pads; i++) at = Mathf.Max(at, game.HitAt(i));
            return at;
        }
    }

    /// <summary>
    /// 가운데 아래 — <b>지금 눌러야 할 것 한 줄.</b> 급식의 안내 줄과 같은 자리·같은 크기다
    /// (기획서 §6 — 한 게임에서 배운 것이 다른 게임에서도 통해야 한다).
    ///
    /// 한 번에 <b>한 줄만</b> 뜬다. 상태가 셋 이상 겹칠 수 있는데 전부 띄우면 아무것도 안 읽힌다.
    /// </summary>
    void DrawHint(float w, float h)
    {
        string line;
        bool warn = false;

        if (game.Frozen)
        {
            // 눌러도 아무 일이 안 되는 <b>이유를 적어 준다</b> — 급식에서 세운 규칙 그대로.
            line = "판 정지  ·  빨강을 밟았다";
            warn = true;
        }
        else if (SafetyDrill.Duration - game.Left < GearSeconds)
        {
            // ★ <b>번호가 어디 적혀 있는지를 말해 준다</b>(2026-09-29 유저: "뭔 번호인지 어떻게 알아").
            // 이제 곰발 판 가운데에 번호패가 박혀 있고, 이 줄은 «거기를 보라» 고 알려 주는 것이다.
            line = "장비 착용  ·  불이 들어온 곰발 가운데의 숫자를 누른다";
        }
        else if (game.Left <= 10f)
        {
            line = "마지막 10초";
            warn = true;
        }
        else if (game.Stage == 0)
        {
            line = "곰발 가운데 숫자를 누른다  ·  7 8 9 / 4 5 6 / 1 2 3";
        }
        else
        {
            line = "초록만 누른다  ·  X 가 그어진 빨강은 건드리지 않는다";
        }

        var text = Hud.Resize(Hud.Text, 16);
        if (warn) text.normal.textColor = Hud.Ribbon;

        float wide = text.CalcSize(new GUIContent(line)).x + 34f;
        var chip = new Rect(w * 0.5f - wide * 0.5f, h - 116f, wide, 30f);
        Hud.Chip(chip);
        GUI.Label(new Rect(chip.x + 16f, chip.y + 5f, chip.width - 24f, 20f), line, text);

        // ESC 는 늘 있는 자리(왼쪽 아래). <b>나가는 길이 화면에 없으면 갇힌 것과 같다.</b>
        var out_ = new Rect(24f, h - 44f, 116f, 26f);
        Hud.Chip(out_);
        GUI.Label(new Rect(out_.x + 10f, out_.y + 4f, 104f, 18f),
                  "ESC  그만두기", Hud.Resize(Hud.Text, 13));
    }

    // ── 반응벽을 못 찾았을 때 ─────────────────────────────────────────────────

    /// <summary>
    /// ★ <b>안전장치</b>(기획서 §7). 방 안의 반응벽에 못 붙었으면 화면이 3×3 판을 대신 그린다.
    ///
    /// 씬이 벽보다 오래됐거나 철곰관 밖에서 시작했을 때 <b>게임이 통째로 못 하는 것</b>이
    /// 되면 안 된다 - 급식 줄이 교착됐을 때 빠져나올 길이 없었던 그 자리야.
    /// 자리 배치는 키패드 그대로 <b>7·8·9 윗줄 / 4·5·6 / 1·2·3 아랫줄</b>.
    /// </summary>
    void DrawFallbackBoard(float w, float h)
    {
        const float cell = 96f, gap = 10f;
        float side = cell * 3f + gap * 2f;
        float x0 = w * 0.5f - side * 0.5f;
        float y0 = h * 0.5f - side * 0.5f + 10f;

        var key = Hud.Resize(Hud.Value, 30, TextAnchor.MiddleCenter);

        for (int i = 0; i < SafetyDrill.Pads; i++)
        {
            int row = 2 - i / 3;             // 0 이 윗줄. 키 1~3 이 아랫줄이라 뒤집는다
            int col = i % 3;
            var box = new Rect(x0 + col * (cell + gap), y0 + row * (cell + gap), cell, cell);

            var state = game.LampOf(i);
            bool flash = Time.time - game.HitAt(i) < 0.18f;

            Texture2D skin = game.Frozen ? stopTex
                           : flash ? flashTex
                           : state == SafetyDrill.Lamp.초록 ? greenTex
                           : state == SafetyDrill.Lamp.빨강 ? redTex
                           : darkTex;

            GUI.DrawTexture(box, skin);
            GUI.DrawTexture(new Rect(box.x, box.y, box.width, 3f), Hud.WoodTex);

            // 빨강에는 <b>X 를 긋는다.</b> 벽과 같은 규칙 — 색만으로 가르지 않는다
            if (!game.Frozen && !flash && state == SafetyDrill.Lamp.빨강) Cross(box);

            key.normal.textColor = state == SafetyDrill.Lamp.꺼짐 && !game.Frozen
                                 ? Hud.Paper : Hud.Ink;
            GUI.Label(box, $"{i + 1}", key);
        }
    }

    /// <summary>판 위의 X. 줄 둘을 대각선으로 긋는다(OnGUI 에는 선이 없어서 얇은 판을 돌린다).</summary>
    void Cross(Rect box)
    {
        float len = Mathf.Sqrt(box.width * box.width + box.height * box.height) * 0.8f;
        var mid = new Vector2(box.center.x, box.center.y);

        for (int s = -1; s <= 1; s += 2)
        {
            Matrix4x4 was = GUI.matrix;
            GUIUtility.RotateAroundPivot(s * 45f, mid * Hud.ScaleFactor);
            GUI.DrawTexture(new Rect(mid.x - len * 0.5f, mid.y - 3f, len, 6f), stopTex);
            GUI.matrix = was;
        }
    }

    // ── 마감 ─────────────────────────────────────────────────────────────────

    void DrawResult(float w, float h)
    {
        const float wide = 440f;
        var box = new Rect(w * 0.5f - wide * 0.5f, h * 0.5f - 150f, wide, 296f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x, inner.y + 10f, inner.width, 30f),
                  "점검 종료", Hud.Resize(Hud.Title, 24, TextAnchor.UpperCenter));
        Hud.Rule(inner.x + 14f, inner.y + 48f, inner.width - 28f);

        // ★ <b>등급과 점수를 나란히.</b> 숫자만 남으면 «잘한 건지» 를 모른다 —
        // 다음 판에 뭘 노릴지가 생기는 자리다.
        GUI.Label(new Rect(inner.x + 40f, inner.y + 60f, inner.width - 80f, 56f),
                  $"{game.Score}", Hud.Resize(Hud.Big, 46, TextAnchor.MiddleCenter));

        var flow = game.Flow;
        if (flow != null)
        {
            string mark = flow.Grade(game.Score);
            var badge = new Rect(inner.xMax - 78f, inner.y + 62f, 54f, 54f);
            GUI.DrawTexture(badge, mark == "S" ? Hud.BrassTex : Hud.WoodTex);
            var big = Hud.Resize(Hud.Big, 30, TextAnchor.MiddleCenter);
            big.normal.textColor = Hud.Paper;
            GUI.Label(badge, mark, big);
        }

        // <b>무엇 때문에 이 점수인지</b>를 한 줄로. 숫자 하나만 남으면 다음 판에 뭘 고칠지 모른다.
        GUI.Label(new Rect(inner.x, inner.y + 118f, inner.width, 20f),
                  $"초록 {game.Hits}  ·  놓침 {game.Missed}  ·  빨강 {game.Slips}",
                  Hud.Resize(Hud.Label, 14, TextAnchor.UpperCenter));

        GUI.Label(new Rect(inner.x, inner.y + 142f, inner.width, 20f),
                  $"최고 연속 {game.BestStreak}",
                  Hud.Resize(Hud.Label, 14, TextAnchor.UpperCenter));

        var best = Hud.Resize(Hud.Text, 15, TextAnchor.UpperCenter);
        if (game.NewBest) best.normal.textColor = Hud.Brass;
        GUI.Label(new Rect(inner.x, inner.y + 172f, inner.width, 22f),
                  game.NewBest ? $"최고 기록 {game.Best}점" : $"최고 {game.Best}점", best);

        // 철거 사유를 하나씩 지운다 — 입간판에 적힌 그 말을 여기서 받는다(기획서 §9).
        GUI.Label(new Rect(inner.x, inner.y + 202f, inner.width, 22f),
                  game.Slips == 0 ? "무사고. 안전 점검표에 기록했다"
                                  : $"{game.Slips}건 접촉. 기록은 남는다",
                  Hud.Resize(Hud.Text, 14, TextAnchor.UpperCenter));

        GUI.Label(new Rect(inner.x, inner.yMax - 30f, inner.width, 22f),
                  "ENTER  다시      ESC  나가기",
                  Hud.Resize(Hud.Text, 14, TextAnchor.UpperCenter));
    }
}
