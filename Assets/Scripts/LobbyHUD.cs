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
        if (Dev.Typing) return;   // 암호 치는 중 — 키를 건드리지 않는다
        var k = Keyboard.current;
        if (k == null) return;

        if (k.hKey.wasPressedThisFrame)
        {
            showControls = !showControls;
            if (showControls) SetLore(false);   // 둘이 같이 열리면 쪽지가 숨은 채로 몸만 묶인다
        }

        // ★ 2026-10-13 — 발바닥 버튼에 「L」이라고 <b>적어 놓고 듣는 데가 없었다.</b>
        //   「키가 있어도 화면에 없으면 없는 것」의 <b>반대 경우</b>다 —
        //   화면에는 있는데 키가 없었다. 눌러 본 사람에게는 고장으로 보인다.
        if (!StoryAlbum.Open && GrandFinal.FreeRun && !Lore.Empty
            && k.lKey.wasPressedThisFrame && !showControls) SetLore(!showLore);

        if (showLore)
        {
            // 쪽 넘기기는 <b>앨범과 같은 손짓</b>이다 — 한 번 배우면 두 군데에서 통한다.
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)
                lorePage = (lorePage - 1 + Lore.Pages.Length) % Lore.Pages.Length;
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)
                lorePage = (lorePage + 1) % Lore.Pages.Length;
            if (k.escapeKey.wasPressedThisFrame) SetLore(false);
            return;      // 읽는 동안에는 E 가 안 먹는다 — 큰 패널은 한 번에 한 장
        }

        // ── ESC — 게임 종료 (2026-10-14 제작자) ────────────────────────────
        // 제작자: *"ESC 누르니까 게임 종료가 안 되는데(빌드 파일 기준)."*
        // <b>나가는 길이 타이틀의 「나가기」 하나뿐이었다</b> — 한 번 시작하면 창을
        // 닫는 것 말고는 끄는 방법이 없었다. 전체화면이면 그마저 어렵다.
        //
        // ★ <b>로비에서만</b> 연다. 트랙 ESC 는 이미 「로비로/다시/계속」 메뉴고,
        //   걷는 중 ESC 는 「둘러보기로」다 — 같은 키에 주인이 셋이면 안 된다.
        //   그래서 걷는 중이면(<see cref="WalkMode.Afoot"/>) 비켜선다.
        // ★ 바로 안 끈다. <b>잘못 눌렀을 때 되돌릴 수 있어야 한다</b> —
        //   트랙 ESC 를 두 번 누르게 만든 것과 같은 판단(2026-09-16).
        if (askQuit)
        {
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) quitYes = true;
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) quitYes = false;
            if (k.escapeKey.wasPressedThisFrame) askQuit = false;
            if (Keys.Confirm(k)) { if (quitYes) QuitGame(); else askQuit = false; }
            return;      // 큰 패널은 한 번에 한 장
        }

        if (k.escapeKey.wasPressedThisFrame && !WalkMode.Afoot && !showControls)
        {
            askQuit = true;
            quitYes = false;      // 기본은 「아니요」 — 잘못 눌러 ENTER 를 또 치면 안 꺼진다
            return;
        }

        if (k.escapeKey.wasPressedThisFrame && showControls) { showControls = false; return; }

        // ── 개발용: 몇 판째인지 한 칸씩 옮긴다 (2026-10-15) ──────────────
        // ★ <b>고친 기믹을 보려고 레이스를 세 번 깰 수는 없다.</b> 포크레인은 4판,
        //   헬기는 3판, 비행선은 2판이라 지금은 거기까지 가는 데만 5분이 든다.
        //   <c>[</c> 와 <c>]</c> 로 증거 수를 한 개씩 늘리고 줄인다 — 그게 곧 판 번호다
        //   (<see cref="MissionManager"/> 가 수집 기록만 보고 판을 고른다).
        // ★ <see cref="Dev.Enabled"/> 뒤에 있으니 빌드에서는 컴파일 단계에서 사라진다.
        if (Dev.Enabled)
        {
            if (k.leftBracketKey.wasPressedThisFrame) StepCollection(-1);
            if (k.rightBracketKey.wasPressedThisFrame) StepCollection(1);
        }

        // TAB 은 WalkMode 가 직접 듣는다 — 이야기가 도는 동안 이 스크립트가 꺼져도 먹게.

        // 동물의 숲처럼 <b>버튼을 눌러야</b> 말한다. 가까이 갔다고 저절로 떠들면
        // 지나갈 때마다 말이 튀어나와서 금방 시끄러워진다(2026-09-16 유저).
        if (k.eKey.wasPressedThisFrame)
        {
            // 문이 먼저다. 문 앞에 곰이 서 있을 때 말만 걸리고 못 들어가면 답답하다.
            // ★ 시계가 잡혀 있으면 곰은 건너뛴다 — `DeskClock` 이 제 E 를 따로 듣기 때문에,
            // 여기서 곰까지 말하게 두면 <b>한 번 누르고 둘이 동시에 반응한다.</b>
            // ★★ 2026-10-14 제작자: *"문을 열면 안은 보이는데 들어가려면 E 를 한 번 더
            //   눌러야 된다. 가까이 안 간 상태로 누르면 열리기만 하고."*
            //   <b>같은 문인데 E 가 둘로 갈렸던 것</b>이다 — 조금 멀면 <see cref="SceneDoor"/>
            //   가 안 잡히고 <see cref="HingedDoor"/> 만 잡혀서 «열기» 로 끝났다.
            //   잡힌 문짝에 씬 문이 같이 붙어 있으면 <b>그쪽으로 넘긴다</b> —
            //   <see cref="SceneDoor.Enter"/> 가 열고 0.55초 뒤에 들어간다.
            if (SceneDoor.Nearest != null) SceneDoor.Nearest.Enter();
            else if (HingedDoor.Nearest != null)
            {
                var exit = HingedDoor.Nearest.GetComponent<SceneDoor>();
                if (exit != null) exit.Enter();
                else HingedDoor.Nearest.Toggle();
            }
            else if (DeskClock.Nearest != null) { /* 시계가 직접 연다 */ }
            else if (NearStand() is CharacterStand pick) Choose(pick);
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
        if (DeskClock.PanelOpen || StoryAlbum.Open) return;

        // ★ 인스펙터가 비어 있어도 찾는다 — 씬이 빌더보다 오래됐으면 안 꽂혀 있다.
        if (walk == null) walk = FindFirstObjectByType<WalkMode>();
        bool walking = walk != null && walk.Walking;

        // ★ 2026-10-02 — 이야기 다시 보기(T)를 여기 적는다. 「키가 있어도 화면에 없으면 없는 것」
        //   이 프로젝트에서 세 번째다(G 유령 · TAB 걷기 · 이번).
        // 처처 2026-10-12 — 막대 칩을 <b>눌리는 발바닥 버튼</b>으로 바꿨다(제작자 요청).
        //   키로도 되고 <b>눌러도 된다</b> — 키만 있으면 안 눌러본 사람에겐 없는 기능이다.
        //
        // 시계 앞에서는 «E 말 걸기» 가 거짓말이 된다 — 그 자리의 E 는 시계가 먹는다.
        // 받침대 앞에서도 그렇다 — 그 자리의 E 는 캐릭터 고르기가 먹는다.
        var stand = NearStand();

        // ★★ 2026-10-13 — <b>「이야기 다시 보기」 버튼을 걷어냈다.</b>
        //   <see cref="StoryAlbum"/> 이 이미 같은 일을 한다(이야기 쪽에서 장을 골라 본다).
        //   버튼이 둘이면 «뭐가 다르지» 가 생기고, 그게 «중구난방» 의 한 조각이다.
        //   <b>T 키는 그대로 산다</b> — 앨범 안내줄이 「T 지금 장 바로 보기」로 적어 준다.
        //   발바닥 버튼은 넷 → 셋(조작법 · 앨범 · 설정).
        const float Tall = 44f;
        var story = new Rect(w - 16f - 200f, h - 108f, 200f, Tall);   // 안내 자리를 잡는 기준으로만 쓴다

        // ★ 이름을 <b>«걸어다니기»</b> 로 못박는다(2026-10-12 제작자 요청) —
        //   키가 아니라 <b>하는 일</b>이 먼저 읽혀야 한다. TAB 도 그대로 먹는다.
        string chipText = !walking ? "걸어다니기"
                        : (DeskClock.Nearest != null || stand != null) ? "둘러보기"
                        : "둘러보기  ·  E 말 걸기";

        int size = walking ? 13 : 14;
        float cw = Mathf.Max(200f, Hud.PawWidth(chipText, size, Tall));
        var chip = new Rect(w - 16f - cw, h - 56f, cw, Tall);
        if (Hud.PawButton(chip, chipText, size) && walk != null) walk.Toggle();

        // ★ 접수대 시계 앞에 서면 그 자리에서 알려준다.
        // <b>키가 있어도 화면에 없으면 없는 것이다</b> — 이 프로젝트에서 세 번째야(G, TAB, 이번).
        if (walking && stand != null)
        {
            // ★★ 2026-10-13 제작자: *"이야기 다시보기 나무패널에 캐릭터 선택 글씨가 겹친다."*
            //   <b>겹친 게 맞다.</b> 재 보면 자리가 그대로 포개져 있었다:
            //       안내  h−86 ~ h−62        (chip.y − 30, 높이 24)
            //       나무판 h−108 ~ h−64      (story 버튼)
            //   → <b>22px 겹침.</b> 게다가 바탕이 없어서 나무 무늬 위에 글자가 바로 얹혔다.
            //
            //   오른쪽 줄은 아래에서부터 <b>걸어다니기(h−56) · 이야기(h−108)</b> 로 쌓인다.
            //   안내는 그 <b>위</b>로 올린다(h−138). 그리고 <see cref="Hud.Chip"/> 바탕을 깐다 —
            //   앨범 안내가 이미 그렇게 하고 있어서 둘이 같은 물건으로 보인다.
            var hint = new Rect(story.x - 40f, story.y - 32f, story.width + 40f, 26f);
            Hud.Chip(hint);
            var big = Hud.Resize(Hud.Text, 16, TextAnchor.MiddleCenter);
            big.normal.textColor = Hud.Brass;
            GUI.Label(hint, $"E   {stand.Label} 고르기", big);
        }
        else if (walking && AlbumStand.Near)
        {
            var hint = new Rect(w * 0.5f - 110f, h - 92f, 220f, 26f);
            Hud.Chip(hint);
            var big = Hud.Resize(Hud.Label, 13, TextAnchor.MiddleCenter);
            big.normal.textColor = Hud.Brass;
            GUI.Label(hint, "B   곰 기억 앨범", big);
        }
        else if (walking && DeskClock.Nearest != null)
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
        // ★ 종료 물음은 <b>가장 위</b>다. 뒤에 안내가 비치면 고르는 화면으로 안 읽힌다.
        if (askQuit)
        {
            DrawQuit(w, h);
            Hud.End();
            return;
        }

        if (!DeskClock.PanelOpen && !StoryAlbum.Open && !UnlockNotice.Open)
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
            else if (showLore) DrawLore(w, h);
        }

        Hud.End();
    }

    // ---- 좌상단: 오늘 할 일 ----
    /// <summary>
    /// ★★★ <b>이 게임이 무슨 게임인지 말하는 유일한 줄.</b> (2026-10-13)
    ///
    /// 담당자님: *"게임이 되게 중구난방하다."* <b>맞는 지적이었다.</b> 로비에 들어서면
    /// 입구가 일곱 개인데(앨범 · 조작법 · 설정 · 이야기 · 걸어다니기 · 캐릭터 · 출발문),
    /// 화면 어디에도 <b>«여덟 판을 돌아 증거 여덟 개를 모아 철거를 막는다»</b> 가 없었다.
    /// 전에는 여기에 「로비」와 「드라이버 정이감」만 적혀 있었다 —
    /// <b>「로비」는 보면 아는 것</b>이고 드라이버는 방금 고른 것이라, 둘 다 새 정보가 아니다.
    ///
    /// 그래서 제일 큰 글씨를 <b>증거 n / 8</b> 에 준다. 그 아래 한 줄이 <b>다음에 뭘 하는지</b>다.
    /// 1초면 «아, 여덟 번 달려서 모으는 게임이구나» 가 된다.
    ///
    /// ⚠ 값은 전부 <b>이미 있던 것</b>을 읽기만 한다 — <see cref="CollectionState.Count"/> ·
    ///   <see cref="MissionManager.CurrentGoal"/> · <see cref="GrandFinal"/>.
    ///   새로 저장하는 게 없으니 어긋날 자리도 없다.
    /// </summary>
    void DrawDriverPanel()
    {
        // 제원표(y 116)와 폭을 맞춘다 — 둘이 한 줄로 서야 «한 덩어리» 로 읽힌다.
        var p = new Rect(16f, 16f, 248f, 92f);
        Hud.Panel(p);

        float x = p.x + 14f;
        int got = CollectionState.Count;
        int all = ExhibitCatalogue.All.Length;

        // ── 머리줄 ───────────────────────────────────────────────────
        if (GrandFinal.FreeRun)
            GUI.Label(new Rect(x, p.y + 10f, p.width - 28f, 28f),
                      "철거를 막았다", Hud.Resize(Hud.Value, 20));
        else
        {
            GUI.Label(new Rect(x, p.y + 10f, 54f, 28f), "증거", Hud.Resize(Hud.Text, 16));
            GUI.Label(new Rect(x + 56f, p.y + 8f, p.width - 70f, 28f),
                      $"{got} / {all}", Hud.Resize(Hud.Value, 21));
        }

        Hud.Rule(x, p.y + 42f, p.width - 28f);

        // ── 다음에 할 일 ─────────────────────────────────────────────
        string next = GrandFinal.FreeRun ? "자유 주행"
                    : GrandFinal.Available ? "결승 — 개발업자 · 시의원"
                    : RaceVoice.Plain(MissionManager.CurrentGoal);

        GUI.Label(new Rect(x, p.y + 50f, 40f, 20f), "다음", Hud.Label);
        var nextStyle = Hud.Resize(Hud.Text, 14, TextAnchor.MiddleLeft);
        nextStyle.wordWrap = false;
        GUI.Label(new Rect(x + 42f, p.y + 50f, p.width - 56f, 20f), next, nextStyle);

        // ── 드라이버는 작게 아래로 ───────────────────────────────────
        //   고른 캐릭터는 받침대와 제원표가 이미 크게 보여준다. 여기서는 확인용 한 줄.
        GUI.Label(new Rect(x, p.y + 70f, 50f, 18f), "드라이버", Hud.Resize(Hud.Label, 12));
        GUI.Label(new Rect(x + 52f, p.y + 70f, p.width - 66f, 18f),
                  GameSelection.HasSelection ? GameSelection.SelectedName : "아직 안 골랐다",
                  Hud.Resize(Hud.Text, 13));
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

        // ★★ 2026-10-06 강사님: *"튜토리얼 중에 세진이가 누구고 세운이가 누군지 모르겠다."*
        //   <b>맞는 지적이고, 고칠 자리는 여기다.</b> 받침대를 고르는 화면은 플레이어가
        //   <b>레이스마다 한 번씩 반드시 지나가는</b> 자리라, 여기 한 줄이면 아홉 번 읽게 된다.
        //   이야기에 설명을 넣는 것보다 싸고, 프롤로그의 «몰랐다» 도 안 건드린다.
        // 처 2026-10-12 제작자: *"캐릭터 소개 패널도 키워줘."* 196x162 → <b>248x196</b>.
        //   안의 자리(<see cref="Row"/> · 가로줄 · 한 줄 소개)도 같이 옮겨야 한다 —
        //   판만 키우면 글자가 왼쪽 위에 몰려서 더 허전해 보인다.
        var p = new Rect(16f, 116f, 248f, 196f);
        Hud.Panel(p);

        float x = p.x + 14f;
        // "제원" 이라고만 써두면 캐릭터가 달리는 줄 안다. 무인 모형 카트라는 걸 제목이 말해준다.
        GUI.Label(new Rect(x, p.y + 12f, p.width - 28f, 22f), "전용 장난감 카트", Hud.Resize(Hud.Text, 15));
        Hud.Rule(x, p.y + 36f, p.width - 28f);

        // 항목 이름은 전문 용어를 안 쓴다. 중량/최고속도/가속도/접지력 은 뜻이 안 와닿는다는
        // 유저 지적(2026-09-16). 무슨 일이 일어나는지를 그대로 적었다.
        Row(p, 0, "무게",   $"{spec.mass:0.0} kg", KartSpec.MassBar(spec));
        Row(p, 1, "빠르기", $"{spec.topSpeed:0.0}",  KartSpec.SpeedBar(spec));
        Row(p, 2, "출발",   $"{spec.acceleration:0.0}", KartSpec.AccelBar(spec));
        Row(p, 3, "코너",   $"{spec.grip:0.0}",     KartSpec.GripBar(spec));

        // 누구인지 한 줄. 이름만 있으면 «세진» 이 사람 이름인지 카트 이름인지도 모른다
        if (string.IsNullOrEmpty(spec.tagline)) return;

        Hud.Rule(x, p.y + 142f, p.width - 28f);
        var who = Hud.Resize(Hud.Label, 12, TextAnchor.UpperLeft);
        who.wordWrap = true;
        GUI.Label(new Rect(x, p.y + 152f, p.width - 28f, 38f), spec.tagline, who);

        DrawProfile(p, castId, spec);
    }

    /// <summary>
    /// 제원표 <b>아래</b>에 붙는 흰 편지지 — 그 캐릭터가 누구인가 세 줄.
    ///
    /// ★ 제원표와 <b>다른 종이</b>여야 한다. 한 판에 숫자와 성격을 같이 넣으면
    /// 둘 다 «스펙» 으로 읽힌다 — 현판과 안내판을 나눈 것과 같은 판단이야(2026-09-17).
    /// 왼쪽 세로줄만 그 사람 색이라 <b>어느 종이가 누구 것인지</b>가 색으로 먼저 보인다.
    /// </summary>
    void DrawProfile(Rect spec, string castId, KartSpec.Spec data)
    {
        if (data.profile == null || data.profile.Length == 0) return;

        var style = Hud.Resize(Hud.Label, 12, TextAnchor.UpperLeft);
        style.wordWrap = true;

        // ★★ 2026-10-14 제작자: *"캐릭터 이름이 나무패널 밖으로 나올려고 한다."*
        //   줄 높이를 17 로 박아 놨는데 <b>글이 길어지면 두 줄로 접힌다</b> —
        //   그러면 아랫줄이 잘리고 종이도 그만큼 모자란다.
        //   이제 <b>재서</b> 잡는다(<c>CalcHeight</c>) — 글을 더 길게 써도 안 넘친다.
        float w = spec.width;
        float textW = w - 24f;

        float need = 0f;
        foreach (var ln in data.profile)
            if (!string.IsNullOrEmpty(ln))
                need += Mathf.Max(17f, style.CalcHeight(new GUIContent(ln), textW));

        float h = 20f + need;
        var p = new Rect(spec.x, spec.yMax + 8f, w, h);

        // 종이 한 장 — 나무 판 없이 종이만. 제원표와 질감이 달라야 다른 물건으로 읽힌다
        GUI.DrawTexture(p, Hud.PaperTex);

        // 왼쪽 세로줄만 그 사람 색. 종이 전체를 물들이면 글자 대비가 무너진다
        GUI.color = Cast.ColorOf(castId);
        GUI.DrawTexture(new Rect(p.x, p.y, 4f, p.height), Hud.PaperTex);
        GUI.color = Color.white;

        float y = p.y + 9f;
        foreach (var line in data.profile)
        {
            if (string.IsNullOrEmpty(line)) continue;
            float lh = Mathf.Max(17f, style.CalcHeight(new GUIContent(line), textW));
            GUI.Label(new Rect(p.x + 14f, y, textW, lh), line, style);
            y += lh;
        }
    }

    void Row(Rect p, int index, string label, string value, float fill)
    {
        float y = p.y + 46f + index * 24f;
        GUI.Label(new Rect(p.x + 14f, y, 42f, 20f), label, Hud.Resize(Hud.Label, 14));

        // 막대 — 숫자만 있으면 네 대를 머릿속에서 비교해야 한다. 막대가 있으면 눈으로 비교돼.
        var bar = new Rect(p.x + 62f, y + 6f, 96f, 9f);
        GUI.DrawTexture(bar, Hud.WoodDarkTex);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(fill), bar.height), Hud.BrassTex);

        GUI.Label(new Rect(p.x + 166f, y, 68f, 20f), value,
                  Hud.Resize(Hud.Text, 14, TextAnchor.MiddleRight));
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

    // ---- 걸어가서 캐릭터 고르기 ----

    /// <summary>받침대까지 걸어갈 수 있는 거리. 받침대 지름 1.9m 라 한 걸음 앞.</summary>
    const float StandReach = 2.9f;

    /// <summary>
    /// 걷기 모드에서 <b>지금 고를 수 있는 받침대</b>. 2026-10-01 유저:
    /// *"캡슐 선택하면 레이스 들어갈 권리도 생기고."*
    ///
    /// 둘러보기에서는 마우스로 받침대를 누르는데, 걷는 동안에는
    /// <see cref="LobbySelector"/> 가 꺼져 있어서 <b>고를 방법이 아예 없었다.</b>
    /// 캠퍼스가 쓰는 <see cref="Reach"/> 와 같은 자(거리 × 각도 벌점)로 고른다 —
    /// 받침대 여섯이 반원으로 서 있어서 <b>거리만 보면 옆 자리가 이긴다.</b>
    /// </summary>
    CharacterStand NearStand()
    {
        if (walk == null || !walk.Walking) return null;
        if (selector == null || selector.stands == null) return null;

        var who = walk.player != null ? walk.player.transform : null;
        if (who == null || !who.gameObject.activeInHierarchy) return null;

        CharacterStand best = null;
        float bestScore = float.MaxValue;
        foreach (var stand in selector.stands)
        {
            if (stand == null || !stand.Selectable) continue;
            if (!Reach.Score(who, stand.transform.position, StandReach, out float d)) continue;
            if (d < bestScore) { bestScore = d; best = stand; }
        }
        return best;
    }

    void Choose(CharacterStand stand)
    {
        selector.ChooseByWalk(stand);
        Toast.Show($"{stand.Label}   —   이 카트로 달린다");
    }

    // ---- 말 걸기 버튼 ★임시 ----
    /// <summary>제일 가까운 곰 한 마리에게만 뜬다. 셋이 몰려 있을 때 누구한테 거는지 헷갈리면 안 된다.</summary>
    void DrawTalkPrompt(float w, float h)
    {
        if (BearNpc.Nearest == null) return;

        // ★★ <b>시계가 잡혔으면 곰은 양보한다</b>(2026-09-22 유저: *"시계 패널 뜰 때 곰인형이
        // 지나가는데 동시에 E 를 누르면 시계가 열리고 곰 대사는 안 뜬다"*).
        //
        // E 를 <b>두 군데서 따로 듣고 있었다</b> — <see cref="DeskClock"/> 가 제 E 를 직접 받고
        // 여기서는 문·곰을 받는다. 그래서 <b>안내는 둘 다 뜨는데 실제로는 시계만 먹었다.</b>
        // 안내가 둘이면 어느 쪽이 먹는지 알 수가 없고, 그건 «눌렀는데 아무 일이 없다» 로 읽힌다.
        //
        // 곰이 지나가는 건 <b>내가 못 정하는 일</b>이라(순찰한다) 시계 쪽을 이기게 두면
        // 같은 자리에서 결과가 매번 달라진다. <b>고정된 물건이 이긴다.</b>
        if (DeskClock.Nearest != null) return;
        if (NearStand() != null) return;   // 받침대가 잡혔으면 그 자리의 E 는 캐릭터 고르기다

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
        // 처 곰 발바닥 버튼. H 로도 열리고 <b>마우스로도 눌린다</b> —
        //   키만 있으면 안 눌러본 사람에겐 없는 기능이다(G·TAB 에서 두 번 겪은 실수).
        var chip = new Rect(16f, h - 56f, 158f, 44f);
        if (Hud.PawButton(chip, "조작법   H")) showControls = !showControls;

        // ★★ 2026-10-12 — 앨범은 <b>물건이 아니라 버튼</b>이다(제작자 판단).
        //   다시 보기를 걸어가서 찾게 만들면 「어딨지」가 된다.
        var album = new Rect(16f, h - 106f, 192f, 44f);
        if (Hud.PawButton(album, StoryAlbum.Prompt) && StoryAlbum.Live != null)
            StoryAlbum.Live.OpenBook();

        // 처처 <b>설정은 다 깨기 전에는 안 보인다</b>(2026-10-12 제작자 결정).
        //   세계관 설정이 들어갈 자리고, 임무를 도는 동안 띄우면
        //   «아직 못 본 게 있나» 로 읽혀서 진행을 방해한다.
        if (!GrandFinal.FreeRun || Lore.Empty) return;
        var lore = new Rect(16f, h - 156f, 158f, 44f);
        if (Hud.PawButton(lore, "설정   L")) SetLore(!showLore);
    }

    /// <summary>
    /// ★ <b>세계관 설정</b> — 다 깬 뒤에만 열린다(2026-10-12). 내용은 <see cref="Lore"/> 에 있고
    /// <b>제작자가 쓴다.</b>
    ///
    /// ★★ <b>네 쪽을 ◀▶ 로 넘긴다</b>(2026-10-13). 한 장에 다 넣으려다 재 보니
    /// 본문이 <b>열한 줄</b>밖에 안 들어가는데 글은 <b>스물세 줄</b>이었다.
    /// 글을 줄이는 대신 쪽을 나눴다 — 한 토막씩 읽는 쪽이 더 잘 읽히고,
    /// <b>◀▶ 는 앨범에서 이미 배운 조작</b>이라 새로 가르칠 것이 없다.
    ///
    /// ★ 칸 높이를 <b>재서</b> 잡는다(<c>CalcHeight</c>). 줄 높이를 22 같은 값으로 박으면
    /// 한글 서른 자가 두 줄로 접히는 순간 <b>아랫줄이 잘린다</b> — 눈으로는 «좀 붙었네» 로
    /// 넘어가는 종류의 오류다. 네 쪽 중 <b>제일 긴 쪽</b>에 맞춰서 쪽을 넘겨도 칸이 안 움직인다.
    /// </summary>
    bool showLore;
    int lorePage;
    FirstPersonController loreHeld;

    /// <summary>
    /// 개발용 — 증거 수를 <paramref name="step"/> 만큼 옮긴다. <b>그게 곧 몇 판째인지</b>다.
    ///
    /// <c>ExhibitCatalogue.All</c> 의 <b>앞에서부터</b> 채운다 — 진행 순서가 그 순서라
    /// (<see cref="MissionManager.NextReward"/>), 그래야 임무 번호와 딱 맞는다.
    /// </summary>
    static void StepCollection(int step)
    {
        int want = Mathf.Clamp(CollectionState.Count + step, 0, ExhibitCatalogue.Count);

        CollectionState.ClearAll();
        for (int i = 0; i < want; i++) CollectionState.Collect(ExhibitCatalogue.All[i].id);

        string goal = want >= ExhibitCatalogue.Count
            ? (GrandFinal.Cleared ? "자유 주행" : "결승")
            : $"{want + 1}판 · {RaceVoice.Plain(MissionManager.GoalForReward(ExhibitCatalogue.All[want].id))}";

        Toast.Show($"[개발] 증거 {want} / {ExhibitCatalogue.Count}   →   {goal}");
        SceneNavigator.Reload();
    }

    // ── 게임 종료 물음 ───────────────────────────────────────────────────
    bool askQuit;
    bool quitYes;

    static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 「게임을 종료할까요?」 — <b>예 / 아니요</b> 두 칸.
    ///
    /// ★ 제작자가 «게임을 종료할까곰?» 을 생각해 봤는데, <b>여기는 담백한 쪽이 맞다.</b>
    ///   2026-09-16 에 HUD 문구를 시비 거는 투로 썼다가 되돌린 것과 같은 이유 —
    ///   <b>고르는 화면에서는 농담이 한 박자를 잡아먹는다.</b> 곰 말장난은 이미
    ///   건물 이름(곰손관·곰밥마당·참잘했어요관)이 잔뜩 하고 있어서 부족하지도 않다.
    ///   바꾸고 싶으면 아래 문자열 한 줄이다.
    ///
    /// ★ 기본 커서는 <b>「아니요」</b>. ESC 를 잘못 눌렀을 때 ENTER 를 또 치면
    ///   그냥 닫히지, 게임이 꺼지면 안 된다.
    /// </summary>
    void DrawQuit(float w, float h)
    {
        Hud.Fill(new Rect(0f, 0f, w, h), new Color(0.03f, 0.03f, 0.05f, 0.62f));

        var box = new Rect(w * 0.5f - 230f, h * 0.5f - 96f, 460f, 192f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x, inner.y + 6f, inner.width, 32f), "게임을 종료할까요?",
                  Hud.Resize(Hud.Title, 23, TextAnchor.MiddleCenter));
        Hud.Rule(inner.x + inner.width * 0.28f, inner.y + 46f, inner.width * 0.44f);

        // 두 칸을 나란히. 고른 칸은 <b>나무 판</b>, 안 고른 칸은 <b>옅은 쪽지</b> —
        // 색만 바꾸면 어느 게 잡혔는지 안 갈린다(타이틀 메뉴와 같은 규칙).
        const float bw = 168f, bh = 46f, gap = 20f;
        float y = inner.y + 64f;
        var yes = new Rect(inner.center.x - bw - gap * 0.5f, y, bw, bh);
        var no = new Rect(inner.center.x + gap * 0.5f, y, bw, bh);

        Pick(yes, "예", quitYes);
        Pick(no, "아니요", !quitYes);

        if (Hud.MouseOver(yes)) quitYes = true;
        if (Hud.MouseOver(no)) quitYes = false;
        if (ClickedOn(yes)) QuitGame();
        if (ClickedOn(no)) askQuit = false;

        GUI.Label(new Rect(inner.x, inner.yMax - 32f, inner.width, 24f),
                  "←  →   고르기      ENTER   확인      ESC   돌아가기",
                  Hud.Resize(Hud.Label, 13, TextAnchor.MiddleCenter));
    }

    /// <summary>
    /// 이 칸을 눌렀나. ★ <b>그리는 패스에서만 한 번</b> 돌려준다 —
    /// OnGUI 는 한 프레임에 두 번 돌고(Layout·Repaint) <c>wasPressedThisFrame</c> 은
    /// 그 내내 참이라, 안 막으면 한 번 누른 게 두 번으로 센다
    /// (<see cref="Hud.PawButton"/> 이 같은 함정에 2026-10-12 에 걸렸다).
    /// </summary>
    static bool ClickedOn(Rect r)
    {
        var m = UnityEngine.InputSystem.Mouse.current;
        return Hud.MouseOver(r) && m != null && m.leftButton.wasPressedThisFrame
               && Event.current != null && Event.current.type == EventType.Repaint;
    }

    static void Pick(Rect r, string text, bool on)
    {
        if (on) Hud.Panel(r); else Hud.Chip(r);
        var s = Hud.Resize(on ? Hud.Title : Hud.Text, on ? 20 : 18, TextAnchor.MiddleCenter);
        GUI.Label(r, text, s);
    }

    /// <summary>
    /// ★ 여닫는 자리는 <b>여기 하나</b>다 — L · 발바닥 버튼 · ESC · H · 꺼짐이 전부 지나간다.
    /// 읽는 동안 걷는 몸을 묶는데(앨범과 같은 이유 — 안 묶으면 글을 읽는 사이에 뒤에서
    /// 몸이 걸어간다), <b>못 돌려주면 로비에 갇힌다.</b>
    /// </summary>
    void SetLore(bool on)
    {
        showLore = on;
        if (on) lorePage = 0;
        if (on && loreHeld == null) loreHeld = Object.FindFirstObjectByType<FirstPersonController>();
        if (loreHeld != null) loreHeld.ControlEnabled = !on;
        if (!on) loreHeld = null;
    }

    void OnDisable() => SetLore(false);

    void DrawLore(float w, float h)
    {
        if (Lore.Empty) return;
        lorePage = Mathf.Clamp(lorePage, 0, Lore.Pages.Length - 1);
        var page = Lore.Pages[lorePage];

        var body = Hud.Resize(Hud.Text, 14, TextAnchor.UpperLeft);
        body.wordWrap = true;

        const float CardW = 520f, HeadH = 80f, FootH = 66f, Gap = 4f, BlankH = 10f;
        float bw = CardW * 0.89f - 36f;

        // 제일 긴 쪽에 맞춘다 — 쪽을 넘길 때마다 칸이 커졌다 작아지면 눈이 따라간다.
        float tall = 0f;
        foreach (var pg in Lore.Pages)
        {
            float t = 0f;
            foreach (string ln in pg.lines)
                t += ln.Length == 0 ? BlankH : body.CalcHeight(new GUIContent(ln), bw) + Gap;
            tall = Mathf.Max(tall, t);
        }

        float need = (HeadH + tall + FootH) / 0.89f;
        var box = new Rect(w * 0.5f - CardW * 0.5f, h * 0.5f - need * 0.5f, CardW, need);
        Hud.NoteCard(box);
        var inner = Hud.NoteInner(box);

        GUI.Label(new Rect(inner.x, inner.y + 4f, inner.width, 28f), Lore.Title, Hud.Title);

        // 쪽 이름을 <b>화살표 사이에</b> 둔다 — 넘길 수 있다는 걸 글자가 스스로 말한다.
        GUI.Label(new Rect(inner.x, inner.y + 38f, inner.width, 24f),
                  "◀   " + page.head + "   ▶",
                  Hud.Resize(Hud.Text, 15, TextAnchor.MiddleCenter));
        Hud.Rule(inner.x + 14f, inner.y + 66f, inner.width - 28f);

        float y = inner.y + HeadH;
        foreach (string ln in page.lines)
        {
            if (ln.Length == 0) { y += BlankH; continue; }
            float lh = body.CalcHeight(new GUIContent(ln), bw);
            GUI.Label(new Rect(inner.x + 18f, y, bw, lh), ln, body);
            y += lh + Gap;
        }

        // 몇 쪽인지 — 남은 쪽이 보여야 끝까지 넘긴다.
        string dots = "";
        for (int i = 0; i < Lore.Pages.Length; i++) dots += i == lorePage ? "●" : "○";
        GUI.Label(new Rect(inner.x, inner.yMax - 48f, inner.width, 20f), dots,
                  Hud.Resize(Hud.Label, 13, TextAnchor.MiddleCenter));

        GUI.Label(new Rect(inner.x, inner.yMax - 26f, inner.width, 20f),
                  "◀▶ 쪽 넘기기 · L · ESC 닫기",
                  Hud.Resize(Hud.Label, 12, TextAnchor.MiddleCenter));
    }

    void DrawControls(float w, float h)
    {
        // ★ 2026-10-14 제작자: *"H 가 카트 항목 패널 줄 조금 빠져나왔다."* 재 보니 둘이었다 —
        //   조작법 일곱째 줄(182~202)이 가로줄(194)을 <b>12px 가로지르고</b>,
        //   카트 항목 마지막 줄 끝(312)이 종이 아래 테두리(317)에서 <b>5px</b>였다.
        //   카드를 336 → 372 로 키우고 아래 두 덩어리를 18px 내렸다.
        //   측정: 조작법 끝 204 < 가로줄 212 · 카트 끝 330 < 종이 끝 351.
        // ★★ 2026-10-16 F11 줄을 넣으면서 <b>줄 수를 잘못 셌다.</b> 둘러보기 쪽은
        //   원래 일곱 줄이라 여덟 줄이 됐고, 여덟째(H)가 y+204~224 인데 가로줄이
        //   y+212 라 <b>글자를 가로질렀다.</b> 아래 덩어리를 통째로 22px 내리고
        //   카드도 그만큼 키운다 — 「줄을 더하면 높이도 같이 늘려라」.
        //
        //   측정: 둘러보기 8줄 끝 224 < 가로줄 234 · 걷기 7줄 끝 202 < 234 ·
        //        카트 항목 끝 352 < 카드 아래 394(여백 42, 전과 같다).
        var box = new Rect(w * 0.5f - 200f, h * 0.5f - 197f, 400f, 394f);
        Hud.NoteCard(box);

        GUI.Label(new Rect(box.x, box.y + 14f, box.width, 24f), "조작법", Hud.Title);
        Hud.Rule(box.x + 20f, box.y + 40f, box.width - 40f);

        // 걷는 중이면 걷는 조작만 보여준다. 둘을 같이 늘어놓으면 지금 뭐가 먹는지 알 수가 없다.
        bool walking = walk != null && walk.Walking;

        string[,] controls = walking
            ? new[,]
            {
                { "WASD", "걷기 (SHIFT 뛰기)" },
                { "마우스", "둘러보기" },
                { "E", "문 · 곰에게 말 걸기 · 받침대에서 카트 고르기" },
                { "TAB", "돌아가서 둘러보기" },
                { "−  =", "음악 줄이기 · 키우기" },
                { "F11", "전체화면 ↔ 창모드" },
                { "H", "이 창 닫기" },
            }
            : new[,]
            {
                { "마우스 움직이기", "둘러보기" },
                { "마우스 끌기", "빙 돌려 보기" },
                { "휠", "가까이 · 멀리" },
                { "클릭", "카트 고르기 / 출발문 열기" },
                { "TAB", "걸어다니기" },
                { "−  =", "음악 줄이기 · 키우기" },
                { "F11", "전체화면 ↔ 창모드" },
                { "H", "이 창 닫기" },
            };
        Rows(box, controls, box.y + 50f);

        Hud.Rule(box.x + 20f, box.y + 234f, box.width - 40f);
        GUI.Label(new Rect(box.x, box.y + 240f, box.width, 22f), "카트 항목", Hud.Resize(Hud.Title, 15));

        // 무슨 일이 일어나는지를 적는다. 어느 카트가 좋다는 말은 여기에도 안 쓴다.
        string[,] spec =
        {
            { "무게",   "부딪힐 때 덜 밀린다" },
            { "빠르기", "직선에서 더 나간다" },
            { "출발",   "멈췄다 붙을 때 빠르다" },
            { "코너",   "높으면 안 미끄러지고 낮으면 잘 돈다" },
        };
        Rows(box, spec, box.y + 266f);
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
