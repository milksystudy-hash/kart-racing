using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 로비 화면 표시. 고른 드라이버, 눈앞의 안내문, 출발문 카운트다운.
///
/// 트랙 HUD 와 <b>같은 나무 판 · 같은 종이 · 같은 잉크</b>를 쓴다(<see cref="Hud"/>).
/// 로비만 어두운 반투명 판이면 방을 나갈 때마다 화면이 다른 게임처럼 보인다.
///
/// 화면 아래 조작법 띠는 없앴다(2026-09-16). 플레이어는 마우스를 움직여 보고 알아채면 되고,
/// 카메라가 마우스를 따라 살짝 움직여서 "이거 돌아가는구나" 를 먼저 알려준다
/// (<see cref="LobbyOrbitCamera"/>). 자세한 건 <b>H</b>.
///
/// 진짜 UI 를 만들 때 통째로 버릴 스크립트야.
/// </summary>
public class LobbyHUD : MonoBehaviour
{
    public LobbySelector selector;
    public StartGate gate;

    [Header("폰트 (비워 두면 OS 한글 폰트를 쓴다)")]
    public Font uiFont;

    public WalkMode walk;

    bool showControls;

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        if (k.hKey.wasPressedThisFrame) showControls = !showControls;

        // TAB 은 WalkMode 가 직접 듣는다 — 이야기가 도는 동안 이 스크립트가 꺼져도 먹게.

        // 동물의 숲처럼 <b>버튼을 눌러야</b> 말한다. 가까이 갔다고 저절로 떠들면
        // 지나갈 때마다 말이 튀어나와서 금방 시끄러워진다(2026-09-16 유저).
        if (k.eKey.wasPressedThisFrame)
        {
            // 문이 먼저다. 문 앞에 곰이 서 있을 때 말만 걸리고 못 들어가면 답답하다.
            if (SceneDoor.Nearest != null) SceneDoor.Nearest.Enter();
            else if (HingedDoor.Nearest != null) HingedDoor.Nearest.Toggle();
            else if (BearNpc.Nearest != null) BearNpc.Nearest.Talk();
        }
    }

    /// <summary>
    /// <b>걸어다닐 수 있다는 걸 화면에 적는다.</b> 2026-09-18 유저:
    /// *"플레이어가 조종할 수 있는 게 없다 보니 언제 곰인형들이 가까워지는지도 모르고..
    /// 로비도 자유롭게 돌아다닐 수 있어야 하는 거 아닐까."*
    ///
    /// 걷기 모드(TAB)는 <b>2026-09-17 에 이미 들어가 있었다.</b> 그런데 조작법 카드(H)를
    /// 열어야만 보여서, 안 열어본 사람에게는 <b>없는 기능</b>이었다 — 유령 모드(G) 때와
    /// 똑같은 실수야. **키가 있어도 화면에 없으면 없는 것이다.**
    /// </summary>
    void DrawWalkHint(float w, float h)
    {
        // 큰 패널이 떠 있으면 <b>한 장도 안 그린다.</b> 2026-09-18 유저: *"UI 가 겹쳐서
        // 좀 드러워 보인다."* 레이스 HUD 에서 이미 배운 규칙이야 — `if` 를 나열하면
        // 둘 다 그려지고, 반투명이 아니라서 겹친 걸 바로 못 알아챈다.
        if (DeskClock.PanelOpen) return;

        bool walking = walk != null && walk.Walking;

        var chip = new Rect(w - 214f, h - 40f, 198f, 24f);
        Hud.Chip(chip);

        var style = Hud.Resize(Hud.Label, 12, TextAnchor.MiddleCenter);
        if (walking) style.normal.textColor = Hud.Brass;

        GUI.Label(chip, walking ? "TAB 둘러보기로   ·   E 말 걸기" : "TAB 걸어다니기", style);

        // ★ 접수대 시계 앞에 서면 그 자리에서 알려준다.
        // <b>키가 있어도 화면에 없으면 없는 것이다</b> — 이 프로젝트에서 세 번째야(G, TAB, 이번).
        if (walking && DeskClock.Nearest != null)
        {
            var hint = new Rect(w * 0.5f - 110f, h - 92f, 220f, 26f);
            Hud.Chip(hint);
            var big = Hud.Resize(Hud.Label, 13, TextAnchor.MiddleCenter);
            big.normal.textColor = Hud.Brass;
            GUI.Label(hint, $"E   {DeskClock.Nearest.Action}", big);
        }
    }

    void OnGUI()
    {
        Rect screen = Hud.Begin(uiFont);
        float w = screen.width, h = screen.height;

        // ★ 시계 돋보기가 떠 있으면 <b>그 창 하나만</b> 남기고 전부 접는다.
        // 유저: *"UI 가 겹쳐서 좀 드러워 보인다."* 안내가 대여섯 장이라 큰 창 위아래로
        // 다 삐져나온다 — 큰 패널은 한 번에 한 장만(레이스 HUD 와 같은 규칙).
        if (!DeskClock.PanelOpen)
        {
            DrawDriverPanel();
            DrawSpecSheet();
            DrawToast(w, h);
            DrawTalkPrompt(w, h);
            DrawPrompt(w, h);
            DrawGate(w, h);
            DrawCorner(h);
            DrawWalkHint(w, h);
            if (showControls) DrawControls(w, h);
        }

        Hud.End();
    }

    // ---- 좌상단: 고른 드라이버 ----
    void DrawDriverPanel()
    {
        var p = new Rect(16f, 16f, 196f, 80f);
        Hud.Panel(p);

        float x = p.x + 14f;
        GUI.Label(new Rect(x, p.y + 12f, 160f, 26f), "로비", Hud.Resize(Hud.Value, 20));
        Hud.Rule(x, p.y + 40f, p.width - 28f);

        // 라벨과 이름을 위아래로 겹쳐 쓰다가 글자가 부딪혔다. 한 줄에 두 칸으로 나눈다.
        GUI.Label(new Rect(x, p.y + 48f, 58f, 22f), "드라이버", Hud.Label);
        GUI.Label(new Rect(p.x + 76f, p.y + 46f, p.width - 90f, 24f),
                  GameSelection.HasSelection ? GameSelection.SelectedName : "선택 안 됨",
                  Hud.Resize(Hud.Value, 19));
    }

    // ---- 제원표 ----
    /// <summary>
    /// 커서를 올린 카트(없으면 고른 카트)의 제원. <b>어느 쪽이 좋다고 안 적는다</b> —
    /// 네 대 다 무언가를 내주고 얻으니까, 숫자만 보여주고 판단은 플레이어한테 맡긴다.
    /// </summary>
    void DrawSpecSheet()
    {
        string castId = selector != null && selector.Hovered != null
            ? selector.Hovered.CastId : GameSelection.SelectedCastId;
        if (!KartSpec.TryGet(castId, out var spec)) return;

        var p = new Rect(16f, 104f, 196f, 124f);
        Hud.Panel(p);

        float x = p.x + 14f;
        // "제원" 이라고만 써두면 캐릭터가 달리는 줄 안다. 무인 모형 카트라는 걸 제목이 말해준다.
        GUI.Label(new Rect(x, p.y + 10f, 160f, 18f), "전용 장난감 카트", Hud.Resize(Hud.Text, 13));
        Hud.Rule(x, p.y + 30f, p.width - 28f);

        // 항목 이름은 전문 용어를 안 쓴다. 중량/최고속도/가속도/접지력 은 뜻이 안 와닿는다는
        // 유저 지적(2026-09-16). 무슨 일이 일어나는지를 그대로 적었다.
        Row(p, 0, "무게",   $"{spec.mass:0.0} kg", KartSpec.MassBar(spec));
        Row(p, 1, "빠르기", $"{spec.topSpeed:0.0}",  KartSpec.SpeedBar(spec));
        Row(p, 2, "출발",   $"{spec.acceleration:0.0}", KartSpec.AccelBar(spec));
        Row(p, 3, "코너",   $"{spec.grip:0.0}",     KartSpec.GripBar(spec));
    }

    void Row(Rect p, int index, string label, string value, float fill)
    {
        float y = p.y + 38f + index * 19f;
        GUI.Label(new Rect(p.x + 14f, y, 34f, 17f), label, Hud.Resize(Hud.Label, 13));

        // 막대 — 숫자만 있으면 네 대를 머릿속에서 비교해야 한다. 막대가 있으면 눈으로 비교돼.
        var bar = new Rect(p.x + 50f, y + 5f, 72f, 7f);
        GUI.DrawTexture(bar, Hud.WoodDarkTex);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(fill), bar.height), Hud.BrassTex);

        GUI.Label(new Rect(p.x + 128f, y, 58f, 17f), value,
                  Hud.Resize(Hud.Text, 13, TextAnchor.MiddleRight));
    }

    // ---- 곰인형이 하는 혼잣말 ★임시 ----
    /// <summary>
    /// 트랙 HUD 와 같은 알림 줄을 로비에도 띄운다. 안 그리면 곰이 말을 해도 화면에 안 나온다.
    /// 정비 곰을 걷어낼 때 이 함수도 같이 지우면 돼(2026-09-16).
    /// </summary>
    void DrawToast(float w, float h)
    {
        if (!Toast.Visible) return;

        // ★ 2026-09-21 유저: *"배관공 곰들 대사 너무 긴 거 있으면 글자가 빠져나온다."*
        // 칸이 420 × 52 에 <b>줄바꿈이 꺼져 있어서</b> 한 줄 25자가 한계였는데 대사가 40자까지 간다.
        //
        // 고치는 방향 둘 — 대사를 자르거나, 칸이 대사에 맞추거나. <b>후자가 맞다:</b>
        // 대사는 유저가 쓰는 거라 길이를 내가 정할 수 없고, 나중에 더 긴 게 들어와도 안 터져야 한다.
        const float wide = 440f;
        var text = Hud.Resize(Hud.Title, 16, TextAnchor.MiddleCenter);
        text.wordWrap = true;

        // 실제로 몇 줄이 되는지 재서 칸 높이를 잡는다. 눈으로 어림하면 또 넘친다.
        float inner = wide - 14f;                       // Hud.Inner 가 좌우로 7 씩 먹는다
        float need = text.CalcHeight(new GUIContent(Toast.Message), inner - 12f);
        float tall = Mathf.Clamp(need + 26f, 52f, 108f);   // 세 줄까지. 그 이상은 대사가 잘못된 것

        var box = new Rect(w * 0.5f - wide * 0.5f, h * 0.14f, wide, tall);
        Hud.Panel(box);
        GUI.DrawTexture(new Rect(box.x + 7f, box.y + 7f, 6f, box.height - 16f), Hud.RibbonTex);

        // ★ <b>Hud.Inner 밖으로 안 그린다.</b> 나무 테두리를 넘어가면 글자가 잘려 보인다.
        Rect area = Hud.Inner(box);
        GUI.Label(new Rect(area.x + 12f, area.y, area.width - 16f, area.height), Toast.Message, text);
    }

    // ---- 말 걸기 버튼 ★임시 ----
    /// <summary>제일 가까운 곰 한 마리에게만 뜬다. 셋이 몰려 있을 때 누구한테 거는지 헷갈리면 안 된다.</summary>
    void DrawTalkPrompt(float w, float h)
    {
        if (BearNpc.Nearest == null) return;

        var chip = new Rect(w * 0.5f - 62f, h * 0.72f, 124f, 28f);
        Hud.Chip(chip);
        GUI.Label(chip, "E   말 걸기", Hud.Resize(Hud.Text, 15, TextAnchor.MiddleCenter));
    }

    // ---- 화면 가운데 아래: 눈앞의 안내문 ----
    void DrawPrompt(float w, float h)
    {
        if (selector == null || string.IsNullOrEmpty(selector.Prompt)) return;

        var box = new Rect(w * 0.5f - 170f, h * 0.64f, 340f, 40f);
        Hud.Panel(box);
        GUI.Label(box, selector.Prompt, Hud.Resize(Hud.Title, 16));
    }

    // ---- 출발문 ----
    void DrawGate(float w, float h)
    {
        if (gate == null || !(gate.Hovered || gate.CountingDown)) return;

        var box = new Rect(w * 0.5f - 150f, h * 0.30f, 300f, 82f);
        Hud.Panel(box);

        var head = Hud.Resize(Hud.Title, 18);
        var sub  = Hud.Resize(Hud.Label, 14, TextAnchor.MiddleCenter);

        if (gate.Blocked)
        {
            GUI.Label(new Rect(box.x, box.y + 14f, box.width, 26f), "먼저 드라이버를 고르세요", head);
            GUI.Label(new Rect(box.x, box.y + 44f, box.width, 22f), "받침대 위 카트를 클릭", sub);
        }
        else if (gate.CountingDown)
        {
            GUI.Label(new Rect(box.x, box.y + 14f, box.width, 26f), "레이스 시작", head);

            float fill = 1f - Mathf.Clamp01(gate.Remaining / Mathf.Max(0.01f, gate.countdownSeconds));
            var bar = new Rect(box.x + 40f, box.y + 50f, box.width - 80f, 12f);
            GUI.DrawTexture(bar, Hud.WoodDarkTex);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fill, bar.height), Hud.BrassTex);
        }
        else
        {
            GUI.Label(new Rect(box.x, box.y + 14f, box.width, 26f), "출발문", head);
            GUI.Label(new Rect(box.x, box.y + 44f, box.width, 22f), "클릭하면 레이스가 시작됩니다", sub);
        }
    }

    // ---- 왼쪽 아래 구석 ----
    void DrawCorner(float h)
    {
        if (showControls) return;
        var chip = new Rect(16f, h - 28f, 88f, 22f);
        Hud.Chip(chip);
        GUI.Label(new Rect(chip.x + 9f, chip.y + 3f, 78f, 18f), "H  조작법", Hud.Resize(Hud.Text, 13));
    }

    void DrawControls(float w, float h)
    {
        var box = new Rect(w * 0.5f - 200f, h * 0.5f - 146f, 400f, 292f);
        Hud.Panel(box);

        GUI.Label(new Rect(box.x, box.y + 14f, box.width, 24f), "조작법", Hud.Title);
        Hud.Rule(box.x + 20f, box.y + 40f, box.width - 40f);

        // 걷는 중이면 걷는 조작만 보여준다. 둘을 같이 늘어놓으면 지금 뭐가 먹는지 알 수가 없다.
        bool walking = walk != null && walk.Walking;

        string[,] controls = walking
            ? new[,]
            {
                { "WASD", "걷기 (SHIFT 뛰기)" },
                { "마우스", "둘러보기" },
                { "E", "문으로 들어가기 · 곰에게 말 걸기" },
                { "TAB", "돌아가서 카트 고르기" },
                { "H", "이 창 닫기" },
                { "", "" },
            }
            : new[,]
            {
                { "마우스 움직이기", "둘러보기" },
                { "마우스 끌기", "빙 돌려 보기" },
                { "휠", "가까이 · 멀리" },
                { "클릭", "카트 고르기 / 출발문 열기" },
                { "TAB", "걸어다니기" },
                { "H", "이 창 닫기" },
            };
        Rows(box, controls, box.y + 50f);

        Hud.Rule(box.x + 20f, box.y + 168f, box.width - 40f);
        GUI.Label(new Rect(box.x, box.y + 174f, box.width, 22f), "카트 항목", Hud.Resize(Hud.Title, 15));

        // 무슨 일이 일어나는지를 적는다. 어느 카트가 좋다는 말은 여기에도 안 쓴다.
        string[,] spec =
        {
            { "무게",   "부딪힐 때 덜 밀린다" },
            { "빠르기", "직선에서 더 나간다" },
            { "출발",   "멈췄다 붙을 때 빠르다" },
            { "코너",   "높으면 안 미끄러지고 낮으면 잘 돈다" },
        };
        Rows(box, spec, box.y + 200f);
    }

    void Rows(Rect box, string[,] rows, float top)
    {
        var key = Hud.Resize(Hud.Text, 13);
        key.fontStyle = FontStyle.Bold;
        var desc = Hud.Resize(Hud.Label, 13);

        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = top + i * 22f;
            GUI.Label(new Rect(box.x + 24f, y, 120f, 20f), rows[i, 0], key);
            GUI.Label(new Rect(box.x + 150f, y, box.width - 172f, 20f), rows[i, 1], desc);
        }
    }
}
