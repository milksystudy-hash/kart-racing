using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// <b>안전 점검 훈련</b> — 철곰관 미니게임. 곰발 판 아홉 장에 불이 들어오고,
/// <b>초록은 누르고 빨강은 누르지 않는다.</b>
///
/// 한 판 <b>60초, 실패 없음, 점수만</b>. 급식(<see cref="Canteen"/>)과 조작은 같은 숫자키인데
/// <b>요구하는 능력이 다르다</b> — 급식은 「읽고 고르는」 게임이고 여기는 「보고 반응하는」
/// 게임이다(기획서 §1). 둘이 같으면 하나는 다른 하나의 쉬운 버전이 되고, 그러면 둘 다 만들
/// 이유가 없어진다.
///
/// <b>씬에 저장되는 게 없다.</b> <see cref="MinigameSpot.Enter"/> 가 실행 중에 만들고
/// 끝나면 지운다 — 급식과 같은 모양이고, 이 프로젝트에서 «새 컴포넌트로 고치면 씬을 다시
/// 구워야만 고쳐진다» 를 네 번 겪은 뒤의 기본형이다.
///
/// 반응벽은 <see cref="SafetyDrillBoard"/>, 화면은 <see cref="SafetyDrillHUD"/>.
/// 여기는 규칙만 안다 — <b>화면을 버려도 규칙이 남아야 한다.</b>
/// </summary>
public class SafetyDrill : MonoBehaviour
{
    /// <summary>한 판 길이. 급식이 90초인데 여기는 <b>손이 쉬지 않는</b> 게임이라 60초다.</summary>
    public const float Duration = 60f;

    /// <summary>곰발 판 수. 반응훈련벽의 <c>Target_1</c> ~ <c>Target_9</c> 그대로.</summary>
    public const int Pads = 9;

    const string BestKey = "안전점검최고점";

    public enum Phase { 진행, 끝 }

    /// <summary>판 한 장의 상태. <b>색이 아니라 상태다</b> — 색은 보드가 정한다.</summary>
    public enum Lamp { 꺼짐, 초록, 빨강 }

    /// <summary>지금 도는 판. 없으면 null.</summary>
    public static SafetyDrill Active { get; private set; }

    /// <summary>판이 떠 있나. 결과 화면도 포함이다 — 그 동안에도 캠퍼스 HUD 는 접는다.</summary>
    public static bool Open => Active != null;

    public Phase Now { get; private set; } = Phase.진행;
    public float Left { get; private set; } = Duration;
    public int Score { get; private set; }

    /// <summary>초록을 누른 수 · 빨강을 밟은 수 · 혼자 꺼지게 둔 초록 수.</summary>
    public int Hits { get; private set; }
    public int Slips { get; private set; }
    public int Missed { get; private set; }

    /// <summary>지금 연속 정답 · 이번 판 최고 연속.</summary>
    public int Streak { get; private set; }
    public int BestStreak { get; private set; }

    public int Best { get; private set; }
    public bool NewBest { get; private set; }

    // ── 점수 ──────────────────────────────────────────────────────────────────

    /// <summary>초록 한 장.</summary>
    public const int HitPoints = 2;

    // ★ 「빨리 누르면 보너스」 를 넣었다가 <b>되돌렸다</b>(2026-10-02).
    //   제일 빨리 누르는 손은 <b>난타</b>다 — 켜지는 순간 이미 누르고 있으니까.
    //   시뮬레이션에서 난타가 125 → 249 로 뛰고 사람과의 차이가 2.7배 → 1.5배로 줄었다.
    //   <b>속도 보상은 난타를 막는 장치와 정면으로 부딪힌다.</b>
    //   실력 천장은 나중에 «판이 동시에 여러 장 켜지는 것」 으로 올릴 것.

    /// <summary>연속 보너스가 붙기 시작하는 횟수.</summary>
    public const int StreakStart = 5;
    const int StreakStep = 5;
    public const int StreakCap = 4;

    /// <summary>
    /// 연속 보너스. <b>지금 연속 수</b>를 넣으면 «이번 한 장에 얹을 점수» 가 나온다 —
    /// 기획서 §3 의 「연속 5회 정답, 그 다음 정답부터 +1 (최대 +4)」 그대로라
    /// <see cref="StreakStart"/> 를 넘긴 뒤 다섯 장마다 한 칸씩 올라간다.
    /// </summary>
    public static int Bonus(int streak) =>
        streak < StreakStart ? 0 : Mathf.Min(StreakCap, (streak - StreakStart) / StreakStep + 1);

    /// <summary>이번에 초록을 누르면 몇 점인가. 화면이 «다음 한 장의 값» 을 보여준다.</summary>
    public int NextValue => HitPoints + Bonus(Streak);

    // ── 난이도 ────────────────────────────────────────────────────────────────
    //
    // 60초를 <b>20초씩 셋</b>으로 나눈다(기획서 §5). 앞 20초에 빨강이 없는 이유는
    // «뭘 하는 게임인지 배우는 구간» 이기 때문이야 — 레이싱 1판에 아무 조건이 없는 것과 같다.

    /// <summary>한 장이 켜져 있는 시간.</summary>
    static readonly float[] Life = { 1.40f, 1.10f, 0.85f };

    /// <summary>동시에 켜지는 수.</summary>
    static readonly int[] MaxLit = { 1, 2, 3 };

    /// <summary>다음 장이 켜지기까지.</summary>
    static readonly float[] Interval = { 1.50f, 0.75f, 0.45f };

    /// <summary>빨강 비율. <b>앞 20초는 0</b>.</summary>
    static readonly float[] RedRate = { 0f, 0.30f, 0.30f };

    /// <summary>지금 몇 단계인가. 0·1·2.</summary>
    public int Stage => Elapsed < 20f ? 0 : Elapsed < 40f ? 1 : 2;

    /// <summary>
    /// ★★ <b>빨강을 밟으면 판 전체가 이만큼 멈춘다</b> — 그리고 이 숫자 하나가
    /// <b>난타를 막는 유일한 장치</b>다.
    ///
    /// 기획서 §4 가 요구한 대로 만들기 전에 전수로 돌렸다(60초 × 1,200판, 규칙을 그대로 옮긴
    /// 시뮬레이션). 빨강 30% · 정지 0.8초에서:
    ///
    /// | 전략 | 60초 평균 |
    /// |---|---|
    /// | 아무것도 안 누른다 | 0 |
    /// | 전부 난타한다 | 93 |
    /// | 켜진 것만 난타한다 (색을 안 본다) | 118 |
    /// | 사람 (반응 0.45초) | <b>195</b> |
    /// | 사람 (반응 0.32초) | 255 |
    /// | 초록만 골라 누른다 (최적) | 314 |
    ///
    /// <b>난타가 사람의 61% 다.</b> 급식에서 두 번 걸린 함정(«빈 식판 ENTER 연타» ·
    /// «아무거나 담아도 점수»)을 여기서는 <b>만들기 전에</b> 확인했다.
    /// 상수를 건드리면 그 계산을 다시 해라.
    /// </summary>
    public const float FreezeSeconds = 0.8f;

    /// <summary>판이 멈춰 있는 동안. <b>판의 시계도 같이 멈춘다</b>(<see cref="Update"/> 참고).</summary>
    public bool Frozen => Time.time < frozenUntil;
    public float FrozenLeft => Mathf.Max(0f, frozenUntil - Time.time);
    float frozenUntil = -99f;

    /// <summary>방금 빨강을 밟은 시각. 화면 흔들림과 판 깜빡임이 이걸 본다.</summary>
    public float SlipAt { get; private set; } = -99f;

    // ── 판 아홉 장 ────────────────────────────────────────────────────────────

    readonly Lamp[] lamp = new Lamp[Pads];
    readonly float[] until = new float[Pads];
    readonly float[] litAt = new float[Pads];
    readonly float[] hitAt = new float[Pads];
    readonly float[] slipAt = new float[Pads];

    public Lamp LampOf(int i) => (uint)i < Pads ? lamp[i] : Lamp.꺼짐;

    /// <summary>이 판이 켜진 시각. 보드가 «막 켜졌다» 를 알아채는 데 쓴다.</summary>
    public float LitAt(int i) => (uint)i < Pads ? litAt[i] : -99f;

    /// <summary>이 판을 <b>맞게</b> 누른 시각. 켜졌다 꺼지는 번쩍임에 쓴다.</summary>
    public float HitAt(int i) => (uint)i < Pads ? hitAt[i] : -99f;

    /// <summary>이 판에서 <b>빨강을 밟은</b> 시각.</summary>
    public float SlipAtPad(int i) => (uint)i < Pads ? slipAt[i] : -99f;

    /// <summary>남은 시간의 비율(1 → 0). 판이 사그라드는 연출에 쓴다.</summary>
    public float LifeLeft(int i)
    {
        if ((uint)i >= Pads || lamp[i] == Lamp.꺼짐) return 0f;
        float life = Life[Stage];
        return life <= 0f ? 0f : Mathf.Clamp01((until[i] - Time.time) / life);
    }

    public int LitCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Pads; i++) if (lamp[i] != Lamp.꺼짐) n++;
            return n;
        }
    }

    /// <summary>
    /// ★★ <b>꺼진 판을 누른 시각</b>(2026-09-29 유저: *"눌러도 점수에 변경이 없어"*).
    ///
    /// 규칙상 꺼진 판을 누르면 <b>연속만 끊기고</b> 점수는 그대로다 — 그건 맞다. 그런데
    /// <b>화면이 아무 반응도 안 하면 그건 규칙이 아니라 고장으로 읽힌다.</b> 누른 것 자체는
    /// 늘 보여야 한다: 「점수가 안 오른다」와 「입력이 안 먹는다」는 <b>플레이어에게 같은
    /// 그림</b>이고, 둘을 가르는 건 화면밖에 없어. 보드가 이 값을 보고 그 판을 한 번 깜빡인다.
    /// </summary>
    public float StrayAt(int i) => (uint)i < Pads ? strayAt[i] : -99f;

    /// <summary>방금 그 판에서 <b>몇 점</b> 받았나. 판 위에 «+2» 로 띄운다 —
    /// 점수판은 화면 구석이라 <b>눈이 벽을 보고 있는 동안에는 안 본다.</b></summary>
    public int GainAt(int i) => (uint)i < Pads ? gain[i] : 0;

    readonly float[] strayAt = new float[Pads];
    readonly int[] gain = new int[Pads];

    /// <summary>
    /// ★ <b>보드가 반응벽에 붙었나.</b> 못 붙었으면 화면이 3×3 판을 대신 그린다 —
    /// 기획서 §7 의 「막히면 빠져나오는 안전장치」다. 벽을 못 찾았다고 게임이 통째로
    /// 못 하는 것이 되면 안 된다(급식 줄이 교착됐던 것과 같은 자리).
    /// </summary>
    public bool BoardBound { get; set; }

    float nextSpawn;
    float emptySince;

    /// <summary>판이 이만큼 비어 있으면 <b>무슨 일이 있어도</b> 한 장 켠다.</summary>
    const float EmptyGiveUp = 2.5f;

    FirstPersonController player;
    bool playerControlWas = true;
    float startedAt;

    float Elapsed => Time.time - startedAt;

    /// <summary>판을 연다. 이미 열려 있으면 그걸 돌려준다.</summary>
    public static SafetyDrill Begin()
    {
        if (Active != null) return Active;

        var go = new GameObject("SafetyDrill");
        var game = go.AddComponent<SafetyDrill>();   // 규칙
        go.AddComponent<SafetyDrillBoard>();         // 반응벽 — 카메라와 불빛
        go.AddComponent<SafetyDrillHUD>();           // 화면
        go.AddComponent<MinigameFlow>();             // 준비 → 카운트다운 → 일시정지
        return game;
    }

    /// <summary>준비 카드 · 카운트다운 · ESC 패널. 급식과 <b>같은 것</b>을 쓴다.</summary>
    public MinigameFlow Flow => flow != null ? flow : (flow = GetComponent<MinigameFlow>());
    MinigameFlow flow;

    /// <summary>
    /// 틀을 꽂는다. <see cref="Start"/> 에서 하는 이유: <see cref="Begin"/> 이
    /// <c>AddComponent&lt;SafetyDrill&gt;()</c> 를 <b>먼저</b> 하는데 그 자리에서 Awake 가 돌아서,
    /// Awake 에서 찾으면 아직 없다 — 급식 스테이지에서 두 번 걸린 함정이다.
    /// </summary>
    void Start()
    {
        var f = Flow;
        if (f == null) return;

        f.title = "안전 점검 훈련";
        f.subtitle = "철곰관 · 반응훈련벽 · 한 판 60초";
        f.rules = new (string, string)[]
        {
            ("1 ~ 9",  "곰발 판 위에 적힌 번호를 누른다"),
            ("초록",   "누른다  ·  한 장 2점"),
            ("빨강 X", "건드리지 않는다  ·  밟으면 0.8초 정지"),
            ("연속 5", "그 다음부터 한 장에 +1 (최대 +4)"),
            ("ESC",    "잠깐 멈추기"),
        };
        f.goal = "실패는 없다 · 숫자열과 키패드 둘 다 됩니다";
        // 전수 시뮬레이션 기준(1,200판): 최적 314 · 반응 0.32초 255 · 0.45초 195 · 난타 118
        // ★★ 2026-10-02 측정으로 다시 잡았다. 전에는 <b>S 가 260</b> 이었는데
        //   시뮬레이션에서 <b>난타가 125~158, 제대로 한 손이 296~464</b> 가 나왔다 —
        //   즉 <b>아무렇게나 두드려도 B</b> 고 제대로 하면 무조건 S 였다. 등급이 아무 말도 안 한 거야.
        //   난타는 C, 웬만큼 하면 B~A, 거의 안 틀려야 S.
        f.grades = new[] { 400, 320, 220 };
        f.onStart = Restart;
        f.onQuit = Quit;
    }

    void Awake()
    {
        Active = this;

        // 훈련 중에는 걸어다니지 않는다. 급식과 같은 자리를 쓴다 —
        // ControlEnabled 는 시선까지 같이 멈추고 CursorLock 도 안 건드린다.
        player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            playerControlWas = player.ControlEnabled;
            player.ControlEnabled = false;
        }

        Restart();
    }

    /// <summary>
    /// ★ <b>푸는 자리를 한 군데로.</b> 판이 어떻게 끝나든(ESC · 씬 이동 · 오브젝트 삭제)
    /// 여기를 지나간다. 걷기를 못 돌려주면 캠퍼스에 갇힌다 — 급식에서 세운 규칙 그대로.
    /// </summary>
    void OnDestroy()
    {
        if (player != null) player.ControlEnabled = playerControlWas;
        if (Active == this) Active = null;
    }

    public void Restart()
    {
        Now = Phase.진행;
        Score = 0;
        Hits = 0;
        Slips = 0;
        Missed = 0;
        Streak = 0;
        BestStreak = 0;
        NewBest = false;
        frozenUntil = -99f;
        SlipAt = -99f;

        for (int i = 0; i < Pads; i++)
        {
            lamp[i] = Lamp.꺼짐;
            until[i] = 0f;
            litAt[i] = -99f;
            hitAt[i] = -99f;
            slipAt[i] = -99f;
            strayAt[i] = -99f;
            gain[i] = 0;
        }

        startedAt = Time.time;
        Left = Duration;
        emptySince = Time.time;

        // 첫 장은 <b>조금 늦게</b>. 들어오자마자 불이 들어와 있으면 그건 이미 놓친 거야.
        nextSpawn = Time.time + 0.9f;
    }

    public void Quit() => Destroy(gameObject);

    void Update()
    {
        var k = Keyboard.current;
        var f = Flow;

        // ★ 준비 카드 · 카운트다운 · ESC 패널이 떠 있는 동안에는 <b>시계도 판도 멈춰 있다.</b>
        // 이걸 안 하면 규칙을 읽는 동안 60초가 흐른다 — 그게 «시작이 없던» 옛 모양이야.
        if (f != null && !f.Running && f.Now != MinigameFlow.Step.끝)
        {
            // 일시정지 패널에서 «처음부터» 를 골랐다
            if (f.RestartAsked) { f.RestartAsked = false; Restart(); }
            return;
        }

        if (Now == Phase.진행)
        {
            Left = Mathf.Max(0f, Duration - Elapsed);
            if (Left <= 0f) { Finish(); return; }

            Tick();
            if (k != null) ReadPlayKeys(k);
        }
        else if (k != null) ReadEndKeys(k);
    }

    /// <summary>판 돌리기 — 꺼질 것 끄고, 켤 것 켠다.</summary>
    void Tick()
    {
        // ★ <b>멈춰 있는 동안에는 판의 시계도 같이 멈춘다.</b> 안 그러면 0.8초 정지가
        // «켜져 있던 초록이 그 사이에 혼자 꺼지는 것» 으로 바뀌어서, 벌이 두 번 들어온다.
        // 잃는 것은 <b>0.8초 그 자체</b> 하나면 충분하다.
        if (Frozen)
        {
            float d = Time.deltaTime;
            for (int i = 0; i < Pads; i++) if (lamp[i] != Lamp.꺼짐) until[i] += d;
            nextSpawn += d;
            emptySince += d;
            return;
        }

        int stage = Stage;

        for (int i = 0; i < Pads; i++)
        {
            if (lamp[i] == Lamp.꺼짐 || Time.time < until[i]) continue;

            // ★ <b>놓쳐도 연속이 안 끊긴다.</b> 기획서 §3 에 「초록을 놓쳤다 → 0」 이라고만
            // 적혀 있고, 그게 맞다 — <b>끊는 것은 «잘못 누른 것» 하나여야</b> 난타와 사람이
            // 갈린다. 놓침까지 끊으면 3장이 동시에 뜨는 마지막 20초에서 연속이 아예 안 서고,
            // 그러면 <b>난타가 사람을 이긴다</b>(시뮬레이션에서 실제로 그랬다: 211 대 136).
            if (lamp[i] == Lamp.초록) Missed++;
            lamp[i] = Lamp.꺼짐;
        }

        if (LitCount > 0) emptySince = Time.time;

        // <b>안전장치.</b> 무슨 이유로든 판이 오래 비면 그냥 한 장 켠다 —
        // 「못 받는 게 제일 나쁘다」(급식 줄이 교착됐던 것과 같은 판단).
        bool due = Time.time >= nextSpawn || Time.time - emptySince > EmptyGiveUp;
        if (!due) return;

        nextSpawn = Time.time + Interval[stage];
        if (LitCount >= MaxLit[stage]) return;

        int pick = PickDark();
        if (pick < 0) return;

        lamp[pick] = Random.value < RedRate[stage] ? Lamp.빨강 : Lamp.초록;
        until[pick] = Time.time + Life[stage];
        litAt[pick] = Time.time;
        emptySince = Time.time;
    }

    /// <summary>꺼져 있는 판 중에서 하나. 없으면 −1.</summary>
    int PickDark()
    {
        int dark = 0;
        for (int i = 0; i < Pads; i++) if (lamp[i] == Lamp.꺼짐) dark++;
        if (dark == 0) return -1;

        int nth = Random.Range(0, dark);
        for (int i = 0; i < Pads; i++)
        {
            if (lamp[i] != Lamp.꺼짐) continue;
            if (nth-- == 0) return i;
        }
        return -1;
    }

    // ── 입력 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 숫자키 1~9 를 <b>화면 배치 그대로</b> 읽는다 — 7·8·9 윗줄 / 4·5·6 / 1·2·3 아랫줄.
    /// 숫자열과 <b>키패드를 같이</b> 받는다: 키패드는 생김새가 3×3 이라 이 게임에 제일 맞고,
    /// 노트북에는 키패드가 없다. 둘 중 뭘 써도 같은 판이야.
    /// </summary>
    void ReadPlayKeys(Keyboard k)
    {
        // ESC 는 <see cref="MinigameFlow"/> 가 «일시정지» 로 받는다.
        // 예전엔 여기서 바로 Quit 이었는데, <b>손이 미끄러지면 판이 통째로 날아갔다.</b>
        // 숫자열과 키패드를 둘 다 읽는다 — 읽는 자리는 <see cref="Keys"/> 하나뿐이다.
        for (int i = 0; i < Pads; i++)
            if (Keys.Digit(k, i + 1)) Press(i);
    }

    /// <summary>
    /// 결과 화면. ★ ENTER 는 <b>바로 다시 시작하지 않고</b> 카운트다운으로 돌려보낸다 —
    /// 결과를 보던 손가락 그대로 다음 판이 시작되면 첫 장을 반드시 놓친다.
    /// </summary>
    void ReadEndKeys(Keyboard k)
    {
        if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
        {
            if (Flow != null) Flow.Again();   // → 카운트다운이 끝나면 onStart 가 Restart 를 부른다
            else Restart();
        }
        // ESC 는 <see cref="MinigameFlow"/> 가 «나가시겠습니까?» 패널로 받는다.
        // 틀이 없을 때만(있을 수 없지만) 여기서 직접 나간다 — 나가는 길은 늘 있어야 한다.
        if (k.escapeKey.wasPressedThisFrame && Flow == null) Quit();
    }

    /// <summary>
    /// 판 하나를 눌렀다. <b>결과가 셋</b>이고, 셋이 전부 다른 일을 해야 한다 —
    /// 「아무 일도 안 일어남」이 둘이면 플레이어는 왜 점수가 안 오르는지 모른다.
    /// </summary>
    public void Press(int i)
    {
        if (Now != Phase.진행 || (uint)i >= Pads) return;

        // 멈춰 있는 동안에는 아무것도 안 먹는다. 그게 벌의 내용이야.
        if (Frozen) return;

        switch (lamp[i])
        {
            case Lamp.초록:
                gain[i] = HitPoints + Bonus(Streak);
                Score += gain[i];
                Streak++;
                BestStreak = Mathf.Max(BestStreak, Streak);
                Hits++;
                hitAt[i] = Time.time;
                lamp[i] = Lamp.꺼짐;
                // ★ 연달아 눌러도 겹쳐 나는 게 맞다 — 그게 «연타» 로 들린다.
                //   음악은 안 비킨다(duckMusic). 0.4초짜리가 초당 두 번이면 음악이 계속 숙인다.
                Sfx.Play("DrillGood", 0.85f, duckMusic: false);
                break;

            case Lamp.빨강:
                Slips++;
                Streak = 0;
                slipAt[i] = Time.time;
                SlipAt = Time.time;
                lamp[i] = Lamp.꺼짐;
                frozenUntil = Time.time + FreezeSeconds;
                // 0.88초짜리라 <b>정지 시간(0.8초)과 거의 같다</b> — 소리가 끝나면 다시 눌린다.
                Sfx.Play("DrillStop", 0.95f);
                break;

            default:
                // ★ <b>꺼진 판을 누르면 연속만 끊긴다.</b> 점수도 안 깎고 멈추지도 않는다 —
                // 이 게임에 실패는 없다. 하지만 <b>«연속 정답» 은 말 그대로여야</b> 하고,
                // 이 한 줄이 「아홉 개를 다 눌러 놓고 기다리는」 난타를 연속에서 빼낸다.
                //
                // <b>그래도 눌린 건 보여준다</b>(<see cref="StrayAt"/>). 점수가 안 오르는 것과
                // 키가 안 먹는 것은 화면에서 구별이 안 된다 — 2026-09-29 에 실제로 그렇게 읽혔다.
                strayAt[i] = Time.time;
                Streak = 0;
                break;
        }
    }

    void Finish()
    {
        Now = Phase.끝;
        Left = 0f;
        if (Flow != null) Flow.Finish();

        Best = PlayerPrefs.GetInt(BestKey, 0);
        if (Score > Best)
        {
            Best = Score;
            NewBest = true;
            PlayerPrefs.SetInt(BestKey, Best);
            PlayerPrefs.Save();
        }
    }

    /// <summary>로비·전시실 기록처럼 최고점은 남는다. 다시 할 이유가 그거야.</summary>
    public static int BestScore => PlayerPrefs.GetInt(BestKey, 0);
}
