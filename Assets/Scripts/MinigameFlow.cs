using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// <b>미니게임 한 판의 «틀».</b> 준비 카드 → 카운트다운 → 진행 → (ESC)일시정지 → 결과.
/// 급식(<see cref="Canteen"/>)과 안전 점검 훈련(<see cref="SafetyDrill"/>)이 <b>같은 것</b>을 쓴다.
///
/// ━━ 왜 만들었나 (2026-09-29 유저: *"지금 어린애들 장난같아서 그래"*) ━━━━━━━━━━━━━━━
///
/// 규칙은 멀쩡했는데 <b>게임처럼 안 보였다.</b> 빠진 것을 세어 보니 전부 «틀» 이었다:
///
/// <list type="number">
/// <item><b>시작이 없었다.</b> E 를 누른 그 프레임에 이미 시계가 돌고 손님이 서 있었다 —
///       무슨 게임인지 읽기도 전에 점수를 잃는다. 게임은 <b>«준비됐나» 를 묻고</b> 시작한다.</item>
/// <item><b>규칙을 아무도 안 알려줬다.</b> 조작법이 HUD 한 줄에 흘러갔다. 처음 한 번은
///       <b>판을 멈추고</b> 보여줘야 한다 — 그게 «게임 방식을 보기 편하게» 의 알맹이다.</item>
/// <item><b>ESC 가 곧 종료였다.</b> 손이 미끄러지면 판이 날아간다. 정식 게임에서 ESC 는
///       <b>일시정지</b>고, 그만두는 건 거기서 한 번 더 고르는 것이다.</item>
/// <item><b>끝나도 «잘한 건지» 를 안 알려줬다.</b> 숫자 하나로는 모른다. <b>등급</b>이 있으면
///       다음 판에 무엇을 노릴지가 생긴다.</item>
/// </list>
///
/// <b>이 넷은 게임 내용을 하나도 안 바꾼다.</b> 그래서 규칙(전수 계산으로 맞춰 둔 것)을
/// 다시 안 건드려도 된다 — 틀만 씌운다.
/// </summary>
public class MinigameFlow : MonoBehaviour
{
    public enum Step { 준비, 시작전, 진행, 멈춤, 끝 }

    public Step Now { get; private set; } = Step.준비;

    /// <summary>게임이 시계를 돌려도 되나. <b>진행</b> 일 때만 true.</summary>
    public bool Running => Now == Step.진행;

    /// <summary>큰 패널이 떠 있나. 게임 HUD 는 이때 계기판을 접는다 —
    /// «큰 패널은 한 번에 한 장» (레이스 ESC 패널에서 세운 규칙).</summary>
    public bool Blocking => Now == Step.준비 || Now == Step.멈춤;

    // ── 이 판이 무엇인지 ──────────────────────────────────────────────────────
    public string title = "";
    public string subtitle = "";

    /// <summary>준비 카드에 줄 단위로 뜬다. <b>키 · 설명</b> 두 칸으로 적는다.</summary>
    public (string key, string what)[] rules = new (string, string)[0];

    /// <summary>한 줄 요약 — 카드 맨 아래. 목표를 한 문장으로.</summary>
    public string goal = "";

    /// <summary>등급 자르는 점수. 큰 것부터 S · A · B, 그 아래는 C.</summary>
    public int[] grades = { 999999, 999999, 999999 };

    // ── 게임이 꽂는 것 ────────────────────────────────────────────────────────
    /// <summary>카운트다운이 <b>끝나는 순간</b>. 게임은 여기서 시계를 0으로 돌린다.</summary>
    public System.Action onStart;
    /// <summary>ESC 패널에서 «나가기».</summary>
    public System.Action onQuit;

    Font font;
    float stepAt;
    int pauseChoice;

    /// <summary>ESC 패널을 연 자리. «계속하기» 는 <b>진행</b>일 수도 <b>결과</b>일 수도 있다.</summary>
    Step back = Step.진행;

    /// <summary>카운트다운 한 칸.</summary>
    const float Beat = 0.62f;

    float Since => Time.time - stepAt;

    void Awake()
    {
        var campus = FindFirstObjectByType<CampusHUD>();
        if (campus != null) font = campus.uiFont;
        stepAt = Time.time;
    }

    /// <summary>
    /// ★★ <b>멈춤은 <see cref="Time.timeScale"/> 로 멈춘다.</b>
    ///
    /// 두 게임의 시계가 전부 <c>Time.time</c> 을 읽는데(남은 시간 · 곰발 판 수명 · 0.8초 정지 ·
    /// 손님 도착), <b><c>Time.time</c> 은 timeScale 의 영향을 받는다.</b> 그래서 0 으로 두면
    /// 시계 하나를 안 고치고도 전부 정확히 멈춘다. 각 게임에서 «멈춘 만큼 되밀기» 를 손으로
    /// 짜면 <b>되밀 자리를 반드시 하나 빠뜨린다</b>(안전훈련만 해도 시계가 여섯 개다).
    ///
    /// 이 프로젝트가 예전에 timeScale 일시정지로 한 번 갇힌 적이 있다(2026-09-18).
    /// 그때 빠진 것은 <b>되돌리는 자리</b>였지 방법이 아니었다 —
    /// 그래서 <see cref="OnDestroy"/> 에서도 되돌린다. <b>푸는 자리를 두 군데 다 둔다.</b>
    /// </summary>
    void Go(Step next)
    {
        Now = next;
        stepAt = Time.time;
        Time.timeScale = next == Step.멈춤 ? 0f : 1f;
    }

    /// <summary>판이 어떻게 끝나든(나가기 · 씬 이동 · 오브젝트 삭제) 여기를 지나간다.
    /// 시간이 멈춘 채로 남으면 <b>게임 전체가 얼어붙는다</b> — 갇히는 것보다 나쁘다.</summary>
    void OnDestroy() => Time.timeScale = 1f;

    /// <summary>게임이 «판이 끝났다» 를 알려준다. 이제 결과는 게임이 그린다.</summary>
    public void Finish() => Go(Step.끝);

    /// <summary>게임이 «다시 한다» 를 알려준다. 카운트다운부터 다시.</summary>
    public void Again() => Go(Step.시작전);

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        // ENTER · 키패드 ENTER · SPACE 를 다 받는다(<see cref="Keys"/>).
        bool enter = Keys.Confirm(k);

        switch (Now)
        {
            case Step.준비:
                // ★ <b>자동으로 안 넘어간다.</b> 읽는 속도는 사람마다 다르고, 규칙 카드가
                // 저절로 사라지면 «못 읽은 사람» 은 영영 못 읽는다.
                if (enter) Go(Step.시작전);
                else if (k.escapeKey.wasPressedThisFrame) onQuit?.Invoke();
                break;

            case Step.시작전:
                if (k.escapeKey.wasPressedThisFrame) { onQuit?.Invoke(); break; }
                if (Since >= Beat * 4f)
                {
                    Go(Step.진행);
                    onStart?.Invoke();   // ← 게임의 시계는 <b>여기서</b> 0이 된다
                }
                break;

            case Step.진행:
                if (k.escapeKey.wasPressedThisFrame) { pauseChoice = 0; back = Step.진행; Go(Step.멈춤); }
                break;

            // ★ 결과 화면에서도 ESC 는 <b>같은 패널</b>을 연다(2026-09-29 유저 요청).
            // 전에는 여기서만 ESC 가 곧바로 나가서, «어디서는 묻고 어디서는 안 묻는» 게임이었다.
            case Step.끝:
                if (k.escapeKey.wasPressedThisFrame) { pauseChoice = 0; back = Step.끝; Go(Step.멈춤); }
                break;

            case Step.멈춤:
                if (k.escapeKey.wasPressedThisFrame) { Go(back); break; }
                // 화살표 · WS · 키패드 8/2 — 손이 어디 있든 고를 수 있게
                if (Keys.Down(k)) pauseChoice = Mathf.Min(2, pauseChoice + 1);
                if (Keys.Up(k)) pauseChoice = Mathf.Max(0, pauseChoice - 1);
                if (enter)
                {
                    if (pauseChoice == 0) Go(back);          // 어디서 왔든 <b>그리로</b> 돌아간다
                    else if (pauseChoice == 1) { RestartAsked = true; Again(); }
                    else onQuit?.Invoke();
                }
                break;
        }
    }

    /// <summary>
    /// 일시정지에서 «처음부터» 를 골랐다. 게임이 한 번 읽어 가면 내려간다 —
    /// 이벤트를 하나 더 만드는 것보다 <b>읽고 지우는 깃발</b>이 순서 문제가 없다.
    /// </summary>
    public bool RestartAsked { get; set; }

    /// <summary>점수에 붙는 등급. <b>«잘한 건가» 에 답한다</b> — 숫자만으로는 모른다.</summary>
    public string Grade(int score) =>
        score >= grades[0] ? "S" : score >= grades[1] ? "A" : score >= grades[2] ? "B" : "C";

    // ── 그리기 ────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        if (Now == Step.진행 || Now == Step.끝) return;

        Rect screen = Hud.Begin(font);
        float w = screen.width, h = screen.height;

        if (Now == Step.준비) DrawCard(w, h);
        else if (Now == Step.시작전) DrawCountdown(w, h);
        else DrawPause(w, h);

        Hud.End();
    }

    /// <summary>
    /// 준비 카드. <b>조작법이 표로 있어야</b> 읽힌다 — 문장으로 흘려 쓰면 아무도 안 읽는다
    /// (캠퍼스 H 조작법 카드에서 이미 정한 모양 그대로).
    /// </summary>
    void DrawCard(float w, float h)
    {
        float tall = 168f + rules.Length * 30f;
        var box = new Rect(w * 0.5f - 260f, h * 0.5f - tall * 0.5f, 520f, tall);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x, inner.y + 12f, inner.width, 30f),
                  title, Hud.Resize(Hud.Title, 26, TextAnchor.UpperCenter));
        GUI.Label(new Rect(inner.x, inner.y + 46f, inner.width, 22f),
                  subtitle, Hud.Resize(Hud.Label, 14, TextAnchor.UpperCenter));
        Hud.Rule(inner.x + 18f, inner.y + 76f, inner.width - 36f);

        var key = Hud.Resize(Hud.Text, 15, TextAnchor.MiddleLeft);
        key.fontStyle = FontStyle.Bold;
        var what = Hud.Resize(Hud.Label, 15, TextAnchor.MiddleLeft);

        for (int i = 0; i < rules.Length; i++)
        {
            float y = inner.y + 88f + i * 30f;
            GUI.Label(new Rect(inner.x + 30f, y, 128f, 26f), rules[i].key, key);
            GUI.Label(new Rect(inner.x + 168f, y, inner.width - 190f, 26f), rules[i].what, what);
        }

        float foot = inner.y + 96f + rules.Length * 30f;
        if (!string.IsNullOrEmpty(goal))
        {
            var chip = new Rect(inner.x + 24f, foot, inner.width - 48f, 30f);
            Hud.Chip(chip);
            GUI.Label(chip, goal, Hud.Resize(Hud.Title, 15, TextAnchor.MiddleCenter));
        }

        GUI.Label(new Rect(inner.x, inner.yMax - 28f, inner.width, 22f),
                  "ENTER  시작      ESC  나가기",
                  Hud.Resize(Hud.Text, 15, TextAnchor.UpperCenter));
    }

    /// <summary>
    /// 3 · 2 · 1 · 시작. <b>레이스 카운트다운과 같은 박자</b>로 맞춘다 —
    /// 한 게임 안에서 «시작» 의 리듬이 두 가지면 그게 제일 어설프다.
    /// </summary>
    void DrawCountdown(float w, float h)
    {
        int beat = Mathf.Clamp(Mathf.FloorToInt(Since / Beat), 0, 3);
        string[] say = { "3", "2", "1", "시작" };
        float into = (Since - beat * Beat) / Beat;

        var big = Hud.Resize(Hud.Big, beat == 3 ? 58 : 84, TextAnchor.MiddleCenter);
        // 한 칸이 끝나갈수록 옅어진다. 숫자가 «툭 바뀌는» 것과 «사라졌다 나타나는» 것은 다르다
        var c = beat == 3 ? Hud.Brass : Hud.Ink;
        c.a = Mathf.Clamp01(1.3f - into);
        big.normal.textColor = c;

        GUI.Label(new Rect(0f, h * 0.5f - 70f, w, 140f), say[beat], big);

        GUI.Label(new Rect(0f, h * 0.5f + 74f, w, 24f), title,
                  Hud.Resize(Hud.Label, 15, TextAnchor.UpperCenter));
    }

    /// <summary>
    /// ESC 패널. <b>고르는 게 셋</b>이라 위아래로 고른다 — 키를 세 개 외우게 하지 않는다.
    ///
    /// ★ <b>제목이 «나가시겠습니까?» 다</b>(2026-09-29 유저 요청). ESC 를 누르는 사람이
    /// 열에 아홉은 <b>나가려고</b> 누른다 — 패널이 그 질문에 먼저 답해야 한다.
    /// 그래도 <b>처음 고른 칸은 «계속하기»</b> 로 둔다: ESC 를 누르고 반사적으로 ENTER 를
    /// 치는 손이 제일 흔한데, 그게 곧 «나가기» 면 <b>확인 패널이 있으나 마나</b>가 된다.
    /// </summary>
    void DrawPause(float w, float h)
    {
        var box = new Rect(w * 0.5f - 180f, h * 0.5f - 122f, 360f, 244f);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x, inner.y + 12f, inner.width, 28f),
                  "나가시겠습니까?", Hud.Resize(Hud.Title, 22, TextAnchor.UpperCenter));
        GUI.Label(new Rect(inner.x, inner.y + 44f, inner.width, 20f),
                  string.IsNullOrEmpty(title) ? "미니게임" : title,
                  Hud.Resize(Hud.Label, 13, TextAnchor.UpperCenter));
        Hud.Rule(inner.x + 18f, inner.y + 68f, inner.width - 36f);

        string[] menu = { "계속하기", "처음부터", "나가기" };
        for (int i = 0; i < menu.Length; i++)
        {
            var row = new Rect(inner.x + 40f, inner.y + 82f + i * 40f, inner.width - 80f, 32f);
            bool on = i == pauseChoice;
            if (on) GUI.DrawTexture(row, i == 2 ? Hud.RibbonTex : Hud.WoodTex);

            var style = Hud.Resize(Hud.Title, 17, TextAnchor.MiddleCenter);
            if (on) style.normal.textColor = Hud.Paper;
            GUI.Label(row, menu[i], style);
        }

        GUI.Label(new Rect(inner.x, inner.yMax - 26f, inner.width, 22f),
                  "↑↓ 고르기   ENTER 확인   ESC 계속하기",
                  Hud.Resize(Hud.Text, 13, TextAnchor.UpperCenter));
    }
}
