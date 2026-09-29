using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// <b>오늘의 급식</b> — 곰밥마당 미니게임. 배식 줄에 선 곰에게 식판을 채워 내보낸다.
///
/// 한 판 <b>90초, 실패 없음, 점수만</b>. 레이싱과 정반대 리듬이어야 넣는 의미가 있다 —
/// 또 빠르고 또 실수하면 안 되는 거면 레이스를 한 판 더 하는 것과 같다(기획서 §1).
///
/// <b>씬에 저장되는 게 없다.</b> <see cref="MinigameSpot.Enter"/> 가 실행 중에 만들고
/// 끝나면 지운다. 이 프로젝트에서 «새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다» 를
/// 네 번 겪었다(카트 중복 · 벽 부딪힘 · AI · 장애물) — <b>물건이 스스로 서게</b> 만든다.
///
/// 화면은 <see cref="CanteenHUD"/> 가 그린다. 여기는 규칙만 안다.
/// </summary>
public class Canteen : MonoBehaviour
{
    /// <summary>한 판 길이. 레이스 한 판이 ~100초라 비슷한 호흡으로 맞췄다.</summary>
    public const float Duration = 90f;

    const string BestKey = "급식최고점";

    public enum Phase { 진행, 끝 }

    /// <summary>지금 도는 판. 없으면 null.</summary>
    public static Canteen Active { get; private set; }

    /// <summary>판이 떠 있나. 결과 화면도 포함이다 — 그 동안에도 캠퍼스 HUD 는 접는다.</summary>
    public static bool Open => Active != null;

    public Phase Now { get; private set; } = Phase.진행;
    public float Left { get; private set; } = Duration;
    public int Score { get; private set; }
    public int Served { get; private set; }

    /// <summary>지금 식판에 담긴 것. 비트마스크.</summary>
    public int Tray { get; private set; }

    /// <summary>방금 내보낸 식판. <see cref="CanteenStage"/> 가 손님 손에 그대로 올린다.</summary>
    public int LastServedTray { get; private set; }

    /// <summary>
    /// ★ <b>맨 앞 손님이 자리에 섰나.</b> <see cref="CanteenStage"/> 가 매 프레임 알려준다.
    ///
    /// 2026-09-22 유저: *"곰돌이들이 떠나고 새 곰돌이들 오는 동안에도 ENTER 를 누르면
    /// 점수가 1점씩 올라간다."* 맞다 — 규칙 쪽은 <see cref="Serve"/> 가 끝나는 <b>그 프레임에</b>
    /// 줄을 당겨 버려서, 화면에서 곰이 걸어오는 동안 이미 «다음 손님» 이 서 있는 셈이었다.
    /// 그래서 ENTER 연타로 줄을 통째로 흘려보낼 수 있었다.
    ///
    /// <b>화면에 보이는 것이 규칙이어야 한다.</b> 곰이 도착해야 받는다.
    /// </summary>
    public bool FrontArrived { get; private set; } = true;

    /// <summary>자리에 서고 이만큼은 지나야 받는다. 도착하자마자 연타하는 것도 막는다.</summary>
    const float SettleSeconds = 0.25f;

    CanteenStage stage;

    /// <summary>
    /// ★ <b>늦게 찾는다.</b> <see cref="Begin"/> 이 `AddComponent&lt;Canteen&gt;()` 를 먼저 하는데
    /// <b>`AddComponent` 는 그 자리에서 `Awake` 를 돌린다</b> — 그 시점엔 스테이지가 아직 없어서
    /// `Awake` 에서 찾으면 영영 null 이고, 그러면 도착 판정이 <b>통째로 꺼진다</b>
    /// (`stage == null` 이 «스테이지 없음」으로 읽혀서 항상 통과). 같은 함정에 이미
    /// `CanteenDressing` 에서 한 번 걸렸다(2026-09-21).
    /// </summary>
    CanteenStage Stage => stage != null ? stage : (stage = GetComponent<CanteenStage>());

    /// <summary>지금 내보낼 수 있나. 화면(<see cref="CanteenHUD"/>)도 이 값으로 안내를 바꾼다.</summary>
    public bool CanServe => Now == Phase.진행 && queue.Count > 0
                         && (Stage == null || FrontReady)
                         && Time.time - queue[0].ArrivedAt >= SettleSeconds;

    /// <summary>
    /// 스테이지가 알려준다. <b>«안 섬 → 섬» 으로 넘어가는 순간에 빠른 보너스 시계를 다시 박는다</b> —
    /// 걸어온 시간까지 치면 3초 보너스가 영영 안 나온다.
    /// </summary>
    public void SetFrontArrived(bool arrived)
    {
        if (arrived && !FrontArrived) StampFront();
        if (!arrived && FrontArrived) blockedAt = Time.time;
        FrontArrived = arrived;
    }

    /// <summary>
    /// ⚠ <b>안전장치.</b> 무슨 이유로든 도착 신호가 안 오면 이만큼 뒤에 풀어준다.
    /// <b>못 받는 게 제일 나쁘다</b> — 문에 미끄러질 거리 바닥값을 준 것,
    /// 브리핑 카드에 25초를 둔 것과 같은 판단이다.
    /// </summary>
    const float BlockedGiveUp = 4f;
    float blockedAt = -99f;

    bool FrontReady => FrontArrived || Time.time - blockedAt > BlockedGiveUp;

    public int Best { get; private set; }
    public bool NewBest { get; private set; }

    public string Verdict { get; private set; } = "";
    public float VerdictAt { get; private set; } = -99f;

    /// <summary>줄. [0] 이 지금 받는 손님이고 나머지는 뒤에 서 있다.</summary>
    public IReadOnlyList<CanteenOrder> Queue => queue;
    readonly List<CanteenOrder> queue = new List<CanteenOrder>();

    FirstPersonController player;
    bool playerControlWas = true;
    float startedAt;

    float Elapsed => Time.time - startedAt;

    // ── 난이도 ────────────────────────────────────────────────────────────────
    // <b>속도를 올리지 않는다.</b> 손이 빨라지는 게임이 아니라 기억하고 고르는 게임이라,
    // 어려워지는 건 «주문이 길어지는 것» 하나뿐이다(기획서 §2).
    static int OrderSize(float t) => t < 30f ? 2 : t < 60f ? 3 : Random.Range(4, 6);
    static int QueueLength(float t) => t < 30f ? 2 : t < 60f ? 3 : 4;

    /// <summary>판을 연다. 이미 열려 있으면 그걸 돌려준다.</summary>
    public static Canteen Begin()
    {
        if (Active != null) return Active;

        var go = new GameObject("Canteen");
        var game = go.AddComponent<Canteen>();   // 규칙
        go.AddComponent<CanteenStage>();         // 방 — 카메라와 곰 손님
        go.AddComponent<CanteenHUD>();           // 화면
        go.AddComponent<MinigameFlow>();         // 준비 → 카운트다운 → 일시정지
        return game;
    }

    void Awake()
    {
        Active = this;

        // 배식 중에는 걸어다니지 않는다. ControlEnabled 는 시선까지 같이 멈추고,
        // FirstPersonController 가 그 아래에서 CursorLock 도 건드리지 않는다(93행 조기 반환).
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
    /// 여기를 지나간다. 걷기를 못 돌려주면 캠퍼스에 갇힌다 —
    /// 일시정지를 timeScale 로 만들었다가 같은 실수를 한 적이 있다(2026-09-18).
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
        Served = 0;
        Tray = 0;
        LastServedTray = 0;
        FrontArrived = true;
        NewBest = false;
        Verdict = "";
        VerdictAt = -99f;
        startedAt = Time.time;
        Left = Duration;

        queue.Clear();
        Refill();
        StampFront();
    }

    public void Quit() => Destroy(gameObject);

    /// <summary>준비 카드 · 카운트다운 · ESC 패널. 안전 점검 훈련과 <b>같은 것</b>을 쓴다.</summary>
    public MinigameFlow Flow => flow != null ? flow : (flow = GetComponent<MinigameFlow>());
    MinigameFlow flow;

    /// <summary>
    /// 틀을 꽂는다. <b>Awake 가 아니라 Start</b> 인 이유는 <see cref="Stage"/> 와 같다 —
    /// <see cref="Begin"/> 이 이 컴포넌트를 제일 먼저 붙이고 그 자리에서 Awake 가 돌기 때문에,
    /// Awake 에서 형제를 찾으면 영영 null 이다.
    /// </summary>
    void Start()
    {
        var f = Flow;
        if (f == null) return;

        f.title = "오늘의 급식";
        f.subtitle = "곰밥마당 · 배식 · 한 판 90초";
        f.rules = new (string, string)[]
        {
            ("1 ~ 5",  "밥 · 국 · 김치 · 반찬 · 후식 담기 (다시 누르면 뺀다)"),
            ("ENTER / SPACE", "식판 내보내기"),
            ("딱 맞음", "10점  ·  3초 안에 내보내면 +3"),
            ("하나 어긋", "4점  ·  둘 이상 어긋나면 0점"),
            ("ESC",    "잠깐 멈추기"),
        };
        f.goal = "주문표를 읽고 그대로 담는다 · 숫자열과 키패드 둘 다 됩니다";
        f.grades = new[] { 220, 170, 110 };
        f.onStart = Restart;
        f.onQuit = Quit;
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        var f = Flow;
        // ★ 준비 카드 · 카운트다운 · ESC 패널이 떠 있는 동안에는 <b>시계도 줄도 멈춰 있다.</b>
        if (f != null && !f.Running && f.Now != MinigameFlow.Step.끝)
        {
            if (f.RestartAsked) { f.RestartAsked = false; Restart(); }
            return;
        }

        if (Now == Phase.진행)
        {
            Left = Mathf.Max(0f, Duration - Elapsed);
            if (Left <= 0f) { Finish(); return; }
            ReadPlayKeys(k);
        }
        else ReadEndKeys(k);
    }

    void ReadPlayKeys(Keyboard k)
    {
        // <b>토글이다.</b> 잘못 누른 걸 같은 키로 뺄 수 있어야 한다 —
        // 못 빼면 실수 하나가 그대로 점수가 되고, 그건 감점제와 다를 게 없다.
        //
        // ★ <see cref="Keys.Digit"/> 가 <b>숫자열과 키패드를 둘 다</b> 읽는다.
        // 예전엔 여기에 `digit1Key`~`digit5Key` 를 손으로 적어 놔서 키패드가 안 먹었다
        // (2026-09-29 유저 제보). 게임마다 키를 따로 적으면 반드시 한쪽을 빠뜨린다.
        for (int i = 0; i < CanteenOrder.Slots; i++)
            if (Keys.Digit(k, i + 1)) Toggle((Dish)i);

        if (Keys.Confirm(k)) Serve();
        // ESC 는 <see cref="MinigameFlow"/> 가 «일시정지» 로 받는다 — 전엔 손이 미끄러지면
        // 판이 통째로 날아갔다(2026-09-29).
    }

    /// <summary>
    /// 결과 화면. ★ ENTER 는 <b>바로 다시 시작하지 않고</b> 카운트다운으로 돌려보낸다 —
    /// 결과를 보던 손가락 그대로 다음 판이 시작되면 첫 손님을 반드시 놓친다.
    /// </summary>
    void ReadEndKeys(Keyboard k)
    {
        if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
        {
            if (Flow != null) Flow.Again(); else Restart();
        }
        // ESC 는 <see cref="MinigameFlow"/> 가 «나가시겠습니까?» 패널로 받는다.
        if (k.escapeKey.wasPressedThisFrame && Flow == null) Quit();
    }

    void Toggle(Dish d) => Tray ^= 1 << (int)d;

    void Serve()
    {
        // ★ 걸어오는 중에는 못 받는다. 이게 ENTER 연타 구멍을 막는 자리야.
        if (!CanServe) return;

        var order = queue[0];
        float held = Time.time - order.ArrivedAt;

        Score += CanteenOrder.Points(order.Wanted, Tray, held, out string verdict);
        Verdict = verdict;
        VerdictAt = Time.time;
        Served++;

        LastServedTray = Tray;
        Tray = 0;
        queue.RemoveAt(0);

        // 다음 손님은 <b>걸어와야</b> 받을 수 있다. 스테이지가 도착을 알려줄 때까지 잠근다.
        FrontArrived = false;
        blockedAt = Time.time;

        Refill();
        StampFront();
    }

    void Refill()
    {
        int want = QueueLength(Elapsed);
        while (queue.Count < want)
            queue.Add(CanteenOrder.Roll(OrderSize(Elapsed), Time.time));
    }

    /// <summary>
    /// ★ 맨 앞 손님의 «선 시각」을 <b>지금 이 순간</b>으로 다시 박는다.
    ///
    /// 빠른 보너스는 «줄 맨 앞에 서고 나서 3초 안」이어야 한다. 만들어진 시각을 그대로 쓰면
    /// 뒤에 서 있던 시간까지 쳐서 보너스가 영영 안 나온다 —
    /// <b>값은 사건이 일어난 자리에서 박아라</b>(결승 판정을 FinishOrder 로 옮긴 것과 같다).
    /// </summary>
    void StampFront()
    {
        if (queue.Count == 0) return;
        queue[0] = new CanteenOrder(queue[0].Wanted, Time.time);
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
