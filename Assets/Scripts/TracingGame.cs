using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// <b>따라 그리기</b> — 재주관 미니게임의 <b>한 판</b>. 규칙과 판정은 <see cref="Tracing"/> 에
/// 따로 있고(3D 없이 전수로 맞춰 둔 것이라 손대지 않는다), 여기는 <b>판을 굴리는 쪽</b>이다.
///
/// ━━ ★★ 왜 여태 «플레이가 안 됐나» (2026-09-29 수연) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
///
/// 버그가 아니라 <b>안 만든 것</b>이었다. 있던 건 <see cref="Tracing"/> 하나 — 점수 계산기다.
/// 없던 게 넷이다:
///
/// <list type="number">
/// <item><b>판</b> — 이젤의 화판에 그림을 올리고 마우스를 받는 것(<see cref="TracingBoard"/>)</item>
/// <item><b>화면</b> — 남은 시간·회차·점수(<see cref="TracingHUD"/>)</item>
/// <item><b>한 판의 진행</b> — 네 도형을 차례로 내주고 합계를 내는 것(이 파일)</item>
/// <item><b>입구</b> — <see cref="MinigameSpot"/> 이 제목을 봐도 «만든 게임» 목록에 없었다</item>
/// </list>
///
/// 그래서 재주관에서 E 를 누르면 <b>「곧 들어갑니다」 토스트</b>만 떴다. 급식·안전훈련은
/// <c>Made(title)</c> 에 제목이 있어서 열리는데 따라 그리기만 빠져 있었던 것 —
/// <b>«만들다 만 것» 은 «고장» 과 화면에서 구별이 안 된다.</b>
///
/// ━━ 셋 중 유일하게 마우스를 쓴다 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
///
/// 급식은 «읽고 고르는», 안전훈련은 «보고 반응하는» 게임이라 여기는 <b>«손으로 맞추는»</b>
/// 게임이다. 그래서 <b>커서를 푼다</b> — 걷기 모드는 커서를 가둬 두니까, 안 풀면 마우스가
/// 화판이 아니라 시선을 돌린다.
/// </summary>
public class TracingGame : MonoBehaviour
{
    public static TracingGame Active { get; private set; }

    /// <summary>판을 연다. 이미 열려 있으면 그걸 돌려준다. 급식·안전훈련과 같은 모양.</summary>
    public static TracingGame Begin()
    {
        if (Active != null) return Active;

        var go = new GameObject("TracingGame");
        var game = go.AddComponent<TracingGame>();   // 진행
        go.AddComponent<TracingBoard>();             // 화판 — 카메라와 그림
        go.AddComponent<TracingHUD>();               // 화면
        go.AddComponent<MinigameFlow>();             // 준비 → 카운트다운 → 일시정지
        return game;
    }

    public MinigameFlow Flow => flow != null ? flow : (flow = GetComponent<MinigameFlow>());
    MinigameFlow flow;

    // ── 판의 상태 ─────────────────────────────────────────────────────────────

    /// <summary>지금 몇 번째 도형인가. 0부터.</summary>
    public int Round { get; private set; }

    /// <summary>여태 합계.</summary>
    public int Score { get; private set; }

    /// <summary>이번 도형에 남은 시간(초).</summary>
    public float Left { get; private set; }

    /// <summary>지금 그릴 도형.</summary>
    public Tracing.Shape Shape => Tracing.Shapes[order[Mathf.Clamp(Round, 0, order.Length - 1)]];

    /// <summary>이번 판에 그린 점들. <b>화판 0~1 좌표</b>(y 는 아래로).</summary>
    public readonly List<Vector2> Drawn = new List<Vector2>();

    /// <summary>
    /// 윤곽 표본 하나하나가 덮였나. <b><see cref="Tracing.Judge"/> 와 같은 표본·같은 잣대</b>다 —
    /// 다른 식으로 세면 막대가 90% 인데 점수는 60 이 나오고, 그 순간 막대가 거짓말이 된다.
    /// </summary>
    public readonly bool[] Covered = new bool[Tracing.Marks];

    /// <summary>덮은 표본 수.</summary>
    public int CoveredCount { get; private set; }

    /// <summary>
    /// 지금까지 덮은 비율. ★ <b>이게 없어서 18초 동안 «잘하고 있나» 를 알 수가 없었다</b> —
    /// 그리기 게임에서 제일 중요한 건 <b>남은 곳이 줄어드는 게 보이는 것</b>이야.
    /// </summary>
    public float LiveCoverage => CoveredCount / (float)Tracing.Marks;

    /// <summary>방금 끝낸 도형의 채점. 회차 사이에 잠깐 띄운다.</summary>
    public Tracing.Mark Last { get; private set; }

    /// <summary>회차가 끝나고 결과를 보여주는 동안. 이때는 못 그린다.</summary>
    public bool Showing => showUntil > Time.time;
    float showUntil;

    /// <summary>회차 결과를 보여주는 시간.</summary>
    const float ShowFor = 1.6f;

    /// <summary>
    /// 화판을 찾았나. <b>못 찾아도 판은 굴러간다</b> — 안전훈련에서 세운 규칙 그대로다.
    /// 소품 이름이 바뀌었다고 게임이 통째로 «안 되는 것» 이 되면 안 된다.
    /// </summary>
    public bool BoardBound { get; set; }

    int[] order = { 0, 1, 2, 3 };
    FirstPersonController player;
    bool playerControlWas = true;
    bool cursorWasLocked;

    void Start()
    {
        var f = Flow;
        if (f == null) return;

        f.title = "따라 그리기";
        f.subtitle = $"재주관 · 이젤 화판 · 도형 {Tracing.Rounds}개";
        f.rules = new (string, string)[]
        {
            ("마우스 왼쪽", "누른 채로 윤곽 위를 덧그린다"),
            ("점수",       "다 그렸나 × 딴 데 안 칠했나"),
            ("ENTER",      "다 그렸으면 다음 도형으로"),
            ("도형 하나",  $"{Tracing.PerShape:0}초  ·  최고 100점"),
            ("ESC",        "잠깐 멈추기"),
        };
        f.goal = "마구 칠하면 점수가 안 난다 · 실패는 없습니다";
        f.grades = Tracing.Grades;
        f.onStart = Restart;
        f.onQuit = Quit;
    }

    void Awake()
    {
        Active = this;

        // 그리는 동안은 걸어다니지 않는다. 급식·안전훈련과 같은 자리.
        player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            playerControlWas = player.ControlEnabled;
            player.ControlEnabled = false;
        }

        // ★ 여기만 커서를 푼다. 다른 두 게임은 키보드라 안 건드렸는데,
        // 이 게임은 <b>마우스가 곧 붓</b>이라 커서가 갇혀 있으면 아무것도 못 한다.
        cursorWasLocked = CursorLock.IsLocked;
        CursorLock.Unlock();

        Restart();
    }

    /// <summary>
    /// ★ <b>푸는 자리를 한 군데로.</b> 판이 어떻게 끝나든(ESC · 씬 이동 · 오브젝트 삭제)
    /// 여기를 지나간다. 걷기나 커서를 못 돌려주면 캠퍼스에 갇힌다.
    /// </summary>
    void OnDestroy()
    {
        if (player != null) player.ControlEnabled = playerControlWas;
        if (cursorWasLocked) CursorLock.Lock();
        if (Active == this) Active = null;
    }

    public void Restart()
    {
        // ★★ <b>예약된 «다음 도형» 을 반드시 취소한다.</b> 회차 결과를 띄우는 1.6초 동안
        // ESC → 처음부터 를 고르면, 새 판이 시작된 뒤에 <b>지난 판의 예약이 뒤늦게 터져서</b>
        // 첫 도형을 건너뛴다. 「판이 어떻게 끝나든 푸는 자리를 한 군데로」와 같은 종류의 실수야.
        CancelInvoke();

        Score = 0;
        Round = 0;
        Last = default;
        showUntil = 0f;
        Shuffle();
        OpenRound();
    }

    /// <summary>
    /// 도형 순서를 섞는다. <b>네 판 다 쓴다</b> — 뽑기로 하면 같은 도형이 두 번 나와서
    /// «외운 사람» 이 유리해지고, 그건 그리기 게임이 겨룰 것이 아니야.
    /// </summary>
    void Shuffle()
    {
        order = new int[Tracing.Shapes.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
    }

    void OpenRound()
    {
        Drawn.Clear();
        System.Array.Clear(Covered, 0, Covered.Length);
        CoveredCount = 0;
        Left = Tracing.PerShape;
    }

    /// <summary>
    /// 화판이 점을 하나 찍었다. <see cref="TracingBoard"/> 가 부른다.
    ///
    /// ★ <b>간격을 두고 받는다</b>(<see cref="Tracing.StepMin"/>). 마우스는 한 프레임에
    /// 0.001 씩도 움직이는데 그걸 다 담으면 <b>천천히 그린 사람일수록 점이 많아져서</b>
    /// 같은 그림인데 점수가 달라진다.
    /// </summary>
    public bool Paint(Vector2 at)
    {
        if (!Flow.Running || Showing) return false;

        if (Drawn.Count > 0 && (Drawn[Drawn.Count - 1] - at).sqrMagnitude
            < Tracing.StepMin * Tracing.StepMin) return false;

        Drawn.Add(at);
        if (Drawn.Count > Tracing.MaxPoints) Drawn.RemoveAt(0);

        // 새로 덮인 윤곽 표본을 센다. 점 하나에 220번 도는데, 점은 <see cref="Tracing.StepMin"/>
        // 간격으로만 들어오니 한 회차에 많아야 900번이다 — 공짜나 다름없다.
        var shape = Shape;
        for (int i = 0; i < Tracing.Marks; i++)
        {
            if (Covered[i]) continue;
            Vector2 m = Tracing.At(shape, (float)i / (Tracing.Marks - 1));
            if ((m - at).sqrMagnitude > Tracing.Tolerance * Tracing.Tolerance) continue;
            Covered[i] = true;
            CoveredCount++;
        }

        Brush();
        return true;
    }

    float nextBrushAt;
    int lastBrush = -1;

    /// <summary>
    /// ★ 2026-10-08 — <b>붓질 소리.</b> 그리는 18초 내내 나야 하는데, <see cref="Sfx.Play"/> 는
    /// <b>한 번 나고 끝나는</b> 소리다. 그래서 <c>KartEngine</c> 처럼 전용 AudioSource 를 두거나,
    /// <b>짧은 붓질을 이어 붙이거나</b> 둘 중 하나다 — 후자를 골랐다. 새 컴포넌트가 0개고,
    /// 받아 온 음원이 <b>길이가 다른 붓질 여섯 개</b>라 이어 붙이면 저절로 변화가 생긴다.
    ///
    /// <list type="bullet">
    /// <item><b>같은 것을 두 번 연달아 안 튼다.</b> 세 개뿐이라 반복이 금방 들린다</item>
    /// <item><b>0.30초 간격.</b> 제일 짧은 토막이 0.33초라 조금 겹치는데, 그 겹침이
    ///       <b>끊긴 소리를 이어 준다</b> — 간격을 길이보다 길게 잡으면 «사각 … 사각» 이 된다</item>
    /// <item>음악은 안 비킨다. 1초에 서너 번 나는 소리에 음악이 숙이면 계속 눌려 있다</item>
    /// </list>
    /// </summary>
    void Brush()
    {
        if (Time.unscaledTime < nextBrushAt) return;
        nextBrushAt = Time.unscaledTime + 0.30f;

        int pick = Random.Range(0, 3);
        if (pick == lastBrush) pick = (pick + 1) % 3;
        lastBrush = pick;

        Sfx.Play($"Brush_{pick + 1}", 0.75f, duckMusic: false);
    }

    void Update()
    {
        if (Flow == null) return;

        // ★ 깃발만 내린다. <b>여기서 Restart 를 부르면 안 된다</b> —
        // 「처음부터」는 <see cref="MinigameFlow.Again"/> 로 카운트다운을 다시 돌리고,
        // 그 끝에서 <c>onStart</c> 가 이미 <see cref="Restart"/> 를 부른다.
        // 여기서 또 부르면 <b>카운트다운 중에 한 번, 끝나고 또 한 번</b> 섞인다.
        if (Flow.RestartAsked) Flow.RestartAsked = false;
        if (!Flow.Running) return;

        if (Showing) return;

        Left -= Time.deltaTime;

        var k = Keyboard.current;
        bool submit = k != null && Keys.Confirm(k) && Drawn.Count >= 2;

        if (Left <= 0f || submit) Judge();
    }

    void Judge()
    {
        Last = Tracing.Judge(Shape, Drawn);
        Score += Last.score;
        showUntil = Time.time + ShowFor;
        Invoke(nameof(NextRound), ShowFor);
    }

    void NextRound()
    {
        Round++;
        if (Round >= Tracing.Rounds) { Done(); return; }
        OpenRound();
    }

    /// <summary>
    /// ★ 2026-10-08 — <b>네 판을 다 그렸다.</b> 끝나는 소리를 여기서 고른다.
    ///
    /// 수연은 «50점 이상이면 win» 이라고 했는데, 재 보니 <b>50 은 마구 칠해도 넘는 점수</b>다 —
    /// 네 판 합이 400점 만점이고 <b>아무렇게나 칠한 손이 97점</b>이 나온다(자가점검 측정값).
    /// 50 으로 두면 <b>이기는 소리가 항상</b> 난다.
    ///
    /// 그래서 <b>이미 화면에 뜨는 등급선</b>에 건다 — C 면 지는 소리, B 이상이면 이기는 소리.
    /// B 가 170점이라 «절반쯤 그렸다» 와 거의 같은 자리고, 무엇보다 <b>플레이어가 보는 글자와
    /// 들리는 소리가 같아진다.</b> 숫자를 따로 두면 「B 인데 왜 지는 소리지」가 생긴다.
    /// </summary>
    void Done()
    {
        bool won = Flow.Grade(Score) != "C";
        Sfx.Play(won ? "DrawWin" : "DrawLose", won ? 0.85f : 0.8f);
        Flow.Finish();
    }

    void Quit()
    {
        if (Flow != null) Flow.Finish();
        Destroy(gameObject);
    }
}
