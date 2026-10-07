using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ★★ 2026-10-06 <b>판마다 트랙 위·바깥의 풍경이 바뀐다.</b>
///
/// 강사님들의 «트랙이 똑같다» 는 <b>코스 모양</b> 얘기가 아니라 <b>화면이 안 변한다</b>는 뜻이다.
/// 길을 고치는 건 비싸고 위험하지만(코스를 바꾸면 발판·체크포인트·랩타임·건물 간격이 전부 따라온다),
/// <b>하늘과 갓길은 공짜로 바꿀 수 있다.</b> 임무 내용은 한 줄도 안 건드린다.
///
/// 구조는 이 프로젝트가 이미 네 번 쓴 그 꼴이다 —
/// <b>수집 기록만 보고 스스로 켜고 끈다.</b> 컴포넌트도 씬도 필요 없고,
/// 「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」 를 또 겪지 않는다.
///
/// 모델은 <b>유저가 만든 FBX 열 종</b>(<c>Assets/Resources/RaceProps/</c>, 6,663쿼드 ·
/// 100% 쿼드 · 전부 사양서대로 들어왔다). 한 판에 한두 종만 켜지니 §7.6 예산 안이다.
/// </summary>
public static class RaceProps
{
    const string Folder = "RaceProps/";

    /// <summary>프롭 전부가 들어가는 통. 판이 바뀌면 통째로 갈아엎는다.</summary>
    static Transform root;

    /// <summary>판이 바뀌면 통째로 지워지는 통. 떨어진 자재가 여기 쌓인다.</summary>
    public static Transform Root => root;
    static int builtFor = -999;

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
        root = null;
        builtFor = -999;

        // 트랙 씬에서만. 코스가 없으면 둘 자리를 못 잡는다
        if (Object.FindFirstObjectByType<TrackBuilder>() == null) return;

        var go = new GameObject("RaceProps");
        go.AddComponent<RacePropHost>();
    }

    /// <summary>지금 판에 맞는 풍경을 세운다. 이미 그 판이면 아무 것도 안 한다.</summary>
    public static void Refresh()
    {
        var track = Object.FindFirstObjectByType<TrackBuilder>();
        if (track == null) return;

        // ★ 자유 주행은 «1판» 이 아니다. 전부 모으면 NextReward() 가 비어서
        //   CurrentGoal 이 Goal.완주 로 떨어지는데, 그 판의 풍경은 «아무 것도 없음» 이다 —
        //   아홉 판을 다 깬 보상이 <b>텅 빈 트랙</b>이면 이긴 티가 안 난다.
        bool free = GrandFinal.FreeRun;
        int goal = free ? 9999 : (int)MissionManager.CurrentGoal;
        if (goal == builtFor && root != null) return;
        builtFor = goal;

        if (root != null) Object.Destroy(root.gameObject);
        RaceShock.Clear();
        SkyDrop.Clear();
        root = new GameObject("RacePropSet").transform;

        if (free)
        {
            // 다 끝난 뒤 — 관중이 가득 차 있고 하늘은 비어 있다.
            // <b>비행선도 헬기도 없다</b>: 감시하던 것들이 사라진 게 이 판의 연출이야.
            Crowd(track, 30);
            return;
        }

        switch (MissionManager.CurrentGoal)
        {
            // 1판 — <b>아무 것도 없다.</b> 기준점이 있어야 나머지가 «달라졌다» 가 된다
            case MissionManager.Goal.완주: break;

            // 2판 — 관장 얼굴 황금 비행선이 <b>경기장 위를 순회한다.</b> 제 갈 길을 일정하게
            // 도는 것이 «감시» 고, 뒤를 졸졸 따라오는 건 반려동물이다(2026-10-06 유저 지적)
            case MissionManager.Goal.발판전부:
                // ★ 높이 17.5 로 뒀다가 <b>천장을 뚫었다</b> — 플레이 모드에서 재니 꼭대기가
                //   y 26.3 인데 캠퍼스 반자널이 26 이다. 비행선 꼭대기 = 높이 + 8.8 이라
                //   15.5 면 24.3 으로 1.7m 가 남는다. <b>지붕 위에 뭘 올릴 때는 건물이 아니라
                //   캠퍼스 천장을 봐라</b>(지붕 상징물에서 이미 한 번 겪었다).
                // ★★ 2026-10-06 유저: *"비행선 움직임이 너무 느리고 어디 가는지 모르겠다."*
                //   52초는 <b>카트보다 느려서</b> 늘 뒤에 처져 있었다 — 그러면 «날아가는 것» 이
                //   아니라 «떠 있는 것» 으로 보인다. <b>24초</b>면 카트(29초)보다 빨라서
                //   레이스 중 서너 번 <b>앞질러 지나간다</b> — 움직이는 게 눈에 들어온다.
                //   높이도 15.5 → 11.5 로 낮췄다. 멀면 작아 보이고 작으면 안 움직여 보인다.
                Patrol(track, "R01_Director_Blimp", 11.5f, 24f, lateral: 0f, bob: 0.3f, shadow: 11f, noseBack: true);
                break;

            // 3판 — 안전점검 헬기가 낮게 돈다 + 담장 위에 곰 관중이 처음 몇 마리
            case MissionManager.Goal.무충돌:
                Patrol(track, "R02_Safety_Helicopter", 9.5f, 19f, lateral: -8f, bob: 0.6f, shadow: 6f);
                Crowd(track, 8);
                break;

            // 4판 — 거대 철거 크레인 둘. 코스 <b>바깥</b>에서 쇠구슬이 느리게 흔들린다
            case MissionManager.Goal.전시품:
                Side(track, "R03_Demolition_Crane", 0.17f, 26f, swing: true);
                Side(track, "R03_Demolition_Crane", 0.63f, -28f, swing: true);
                break;

            // 5판 — 서류 폭풍. 관장 도장이 <b>몇 초마다 쿵 찍고</b> 땅울림이 코스를 쓸고 간다.
            //   <see cref="StampSlam"/> · <see cref="RaceShock"/> 참고 — SPACE 로 넘는다.
            case MissionManager.Goal.제한시간:
                // ★ 다섯 개로는 «어쩌다 한 번» 이라 사건이 안 된다(유저 지적).
                //   <b>구간마다 하나씩</b> — 코스가 여섯 구간이니 여덟이면 한 바퀴 내내 만난다.
                //   좌우를 번갈아 둬서 <b>늘 같은 쪽으로 피하면 되는 게 아니게</b> 한다.
                for (int i = 0; i < 8; i++)
                {
                    var go = Side(track, "R04_Paper_Storm", 0.05f + i * 0.12f,
                                  (i % 2 == 0 ? 14f : -15f), drift: true);
                    if (go == null) continue;

                    var slam = go.AddComponent<StampSlam>();
                    // 주기를 조금씩 다르게 준다. 똑같으면 여덟이 <b>한 박자로</b> 찍혀서
                    // «쿵 쿵 쿵» 이 아니라 «쿵» 한 번이 된다.
                    slam.period = 5.6f + i * 0.41f;
                    slam.phase  = i * 1.37f;
                }
                break;

            // 6판 — 골든베어 리조트 모형. 황금 곰 동상이 회전 받침 위에서 돈다
            case MissionManager.Goal.광고판:
                Side(track, "R05_Resort_Model", 0.30f, 22f);
                Side(track, "R05_Resort_Model", 0.78f, -23f);
                break;

            // 7판 — 중계 차량이 바깥을 따라 달리고 드론 셋이 플레이어를 찍는다
            case MissionManager.Goal.장애물:
                Chase(track, "R06_Broadcast_Van", 19f, 0.0f);
                Fly("R07_Camera_Drone", 6.5f, 13f, 1.4f, shadow: 0f, side: -6f, sway: 4f, follow: 3.4f);
                Fly("R07_Camera_Drone", 7.8f, 16f, 1.1f, shadow: 0f, side: 7f, sway: 4f, follow: 3.1f);
                Fly("R07_Camera_Drone", 5.4f, 19f, 1.7f, shadow: 0f, side: 0f, sway: 6f, follow: 2.8f);
                break;

            // 8판 — ★ <b>사람이 처음 나타난다.</b> 철거 심사단이 서서 보고 있다.
            //   곰은 사람 앞에서 움직일 수 없다 — 그래서 카트만 나간다. 그 규칙이 눈앞에서 증명된다
            case MissionManager.Goal.완벽:
                Side(track, "R08_Inspection_Car", 0.02f, 14f);
                Side(track, "R08_Inspection_Car", 0.05f, 16.5f);
                Side(track, "R08_Inspection_Car", 0.52f, -15f);
                People(track, 0.03f, 12.5f, 3);
                People(track, 0.50f, -13f, 3);
                break;

            // 9판 결승 — 담장이 곰인형 관중으로 가득 차고 비행선 둘이 뜬다
            case MissionManager.Goal.결승:
                Crowd(track, 26);
                Patrol(track, "R01_Director_Blimp", 12.5f, 22f, lateral: 7f, bob: 0.25f, shadow: 10f, noseBack: true);
                Patrol(track, "R02_Safety_Helicopter", 10.5f, 17f, lateral: -12f, bob: 0.7f, shadow: 6f);
                break;
        }
    }

    // ──────────────────────────────────────────────────────────── 만들기
    static GameObject Load(string file)
    {
        var prefab = Resources.Load<GameObject>(Folder + file);
        if (prefab == null) { Debug.LogWarning($"[풍경] {file} 을 못 찾았다 — Resources/RaceProps/"); return null; }

        var go = Object.Instantiate(prefab, root);
        go.name = file;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        Spin(go);
        return go;
    }

    /// <summary>
    /// 돌아가야 하는 부품을 이름으로 찾아 돌린다. <b>애니메이션 클립을 안 쓴다</b> —
    /// 카트 바퀴 · 곰 팔 · 스피커 귀와 같은 방식이고, 사양서가 이름을 약속해 준 덕이다.
    /// </summary>
    static void Spin(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n.StartsWith("Rotor_Main")) Add(t, Vector3.up, 900f);
            else if (n.StartsWith("Rotor_Tail")) Add(t, Vector3.right, 1400f);
            else if (n.StartsWith("Rotor_")) Add(t, Vector3.up, 1600f);
            else if (n.StartsWith("Prop_")) Add(t, Vector3.up, 220f);
            else if (n == "Dish") Add(t, Vector3.up, 26f);
            else if (n == "Turntable" || n == "Bear_Statue") Add(t, Vector3.up, 14f);
            else if (n.StartsWith("Wheel_")) Add(t, Vector3.right, 420f);
        }
    }

    static void Add(Transform t, Vector3 axis, float speed)
    {
        var s = t.gameObject.AddComponent<SpinPart>();
        s.axis = axis; s.degreesPerSecond = speed;
    }

    /// <summary>하늘에서 <b>플레이어를 따라오는</b> 것. 바닥에 그림자를 깐다.</summary>
    /// <summary><paramref name="distance"/> 는 카트 <b>앞쪽</b> 거리다(백뷰라 뒤는 안 보인다).</summary>
    static void Fly(string file, float height, float distance, float bob, float shadow,
                    float side = 0f, float sway = 0f, float follow = 2.6f)
    {
        var go = Load(file);
        if (go == null) return;

        var f = go.AddComponent<FlyingProp>();
        f.height = height; f.distance = distance; f.bob = bob; f.sideways = side;
        f.shadowRadius = shadow; f.sway = sway; f.follow = follow;
    }

    /// <summary>
    /// 코스 위를 <b>일정하게 도는</b> 비행체 — 감시 비행. 플레이어를 안 쫓는다.
    /// <paramref name="lapSeconds"/> 가 카트 한 바퀴(약 29초)보다 길면 주기적으로 추월당하고,
    /// 짧으면 뒤에서 따라잡힌다. <b>어느 쪽이든 가끔 마주치는 게 요점</b>이다.
    /// </summary>
    static void Patrol(TrackBuilder track, string file, float height, float lapSeconds,
                       float lateral, float bob, float shadow, bool noseBack = false)
    {
        var go = Load(file);
        if (go == null) return;

        var f = go.AddComponent<FlyingProp>();
        f.patrol = track; f.height = height; f.lapSeconds = lapSeconds;
        f.lateral = lateral; f.bob = bob; f.shadowRadius = shadow; f.noseBack = noseBack;
    }

    /// <summary>코스 <b>바깥</b>에 고정. 자리를 손으로 안 찍는다 — 코스 t 와 옆거리로 적는다.</summary>
    static GameObject Side(TrackBuilder track, string file, float t, float offset,
                           bool swing = false, bool drift = false)
    {
        var go = Load(file);
        if (go == null) return null;

        Vector3 fwd = track.TangentOnPath(t);
        Vector3 at = track.transform.position + track.PointOnPath(t)
                   + Vector3.Cross(Vector3.up, fwd) * offset;

        go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(-Vector3.Cross(Vector3.up, fwd)));

        if (swing)
        {
            var ball = go.transform.Find("Ball") ?? go.transform.Find("Boom");
            if (ball != null)
            {
                var sw = ball.gameObject.AddComponent<SwingPart>();
                sw.degrees = 11f; sw.period = 4.5f;
            }
        }
        if (drift) go.AddComponent<PaperDrift>();
        return go;
    }

    /// <summary>코스 바깥을 <b>같이 달리는</b> 차.</summary>
    static void Chase(TrackBuilder track, string file, float offset, float lead)
    {
        var go = Load(file);
        if (go == null) return;
        var c = go.AddComponent<ChaseVehicle>();
        c.track = track; c.offset = offset; c.lead = lead;
    }

    /// <summary>담장 위 곰인형 관중. 판이 올라갈수록 늘어난다.</summary>
    static void Crowd(TrackBuilder track, int count)
    {
        var prefab = Resources.Load<GameObject>(Folder + "R09_Spectator_Bear");
        if (prefab == null) return;

        string[] pose = { "Bear_Sit", "Bear_Watch", "Bear_Wave" };

        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            float side = (i % 3 == 0) ? -1f : 1f;

            Vector3 fwd = track.TangentOnPath(t);
            Vector3 at = track.transform.position + track.PointOnPath(t)
                       + Vector3.Cross(Vector3.up, fwd) * (side * (track.WidthOnPath(t) * 0.5f + 1.1f))
                       + Vector3.up * 4.25f;                 // 담장 꼭대기

            var go = Object.Instantiate(prefab, root);
            go.name = $"Bear_{i}";
            go.transform.SetPositionAndRotation(
                at, Quaternion.LookRotation(-Vector3.Cross(Vector3.up, fwd) * side));
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);

            // 포즈 셋 중 하나만 켠다
            string keep = pose[i % pose.Length];
            foreach (var p in pose)
            {
                var child = go.transform.Find(p);
                if (child != null) child.gameObject.SetActive(p == keep);
            }
        }
    }

    /// <summary>철거 심사단. 서서 보고 있기만 한다.</summary>
    static void People(TrackBuilder track, float t, float offset, int count)
    {
        var prefab = Resources.Load<GameObject>(Folder + "R10_Inspector_Figure");
        if (prefab == null) return;

        string[] pose = { "Person_Stand", "Person_Clipboard", "Person_Point" };
        Vector3 fwd = track.TangentOnPath(t);
        Vector3 side = Vector3.Cross(Vector3.up, fwd);
        Vector3 at = track.transform.position + track.PointOnPath(t) + side * offset;

        for (int i = 0; i < count; i++)
        {
            var go = Object.Instantiate(prefab, root);
            go.name = $"Inspector_{i}";
            // 코스를 보게 세운다 — 보고 있다는 게 이 판의 전부야
            go.transform.SetPositionAndRotation(
                at + fwd * (i * 1.6f - count * 0.5f) + side * (i % 2 == 0 ? 0f : 1.3f),
                Quaternion.LookRotation(-side * Mathf.Sign(offset)));
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);

            string keep = pose[i % pose.Length];
            foreach (var p in pose)
            {
                var child = go.transform.Find(p);
                if (child != null) child.gameObject.SetActive(p == keep);
            }
        }
    }
}

/// <summary>프롭을 세우고, 판이 바뀌면 다시 세운다.</summary>
public class RacePropHost : MonoBehaviour
{
    void Start() => RaceProps.Refresh();

    // 판은 ENTER 재시작으로도 바뀐다. 매 프레임 세는 게 아니라 <b>달라졌을 때만</b> 다시 짓는다
    void Update()
    {
        RaceProps.Refresh();
        RaceShock.Tick();
        if (track == null) track = Object.FindFirstObjectByType<TrackBuilder>();
        if (track != null) SkyDrop.Tick(track);
    }

    TrackBuilder track;
}

/// <summary>부품 하나를 제자리에서 돌린다. 클립도 리그도 없다.</summary>
public class SpinPart : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 360f;

    void Update() { if (!RacePause.On) transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self); }
}

/// <summary>크레인 쇠구슬처럼 <b>천천히 흔들린다.</b></summary>
public class SwingPart : MonoBehaviour
{
    public float degrees = 10f;
    public float period = 4f;

    Quaternion home;
    void Awake() => home = transform.localRotation;

    void Update()
    {
        if (RacePause.On) return;
        float a = Mathf.Sin(Time.time * Mathf.PI * 2f / period) * degrees;
        transform.localRotation = Quaternion.Euler(a, 0f, 0f) * home;
    }
}

/// <summary>서류가 바람에 천천히 뒤척인다.</summary>
public class PaperDrift : MonoBehaviour
{
    readonly List<Transform> sheets = new();
    readonly List<Vector3> home = new();

    float gustAt = -99f;

    /// <summary>
    /// 도장이 찍힐 때 서류가 <b>확 솟았다 제자리로</b> 돌아온다.
    /// 유저 요청 중 «종이를 날린 뒤 원상복귀» 가 이것 — <c>home</c> 을 들고 있어서 공짜다.
    /// </summary>
    public void Gust() => gustAt = Time.time;

    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>())
            if (t.name.StartsWith("Paper_")) { sheets.Add(t); home.Add(t.localPosition); }
    }

    void Update()
    {
        if (RacePause.On) return;

        // 0.15초 만에 솟고 1.7초에 걸쳐 가라앉는다. 올라가는 게 빨라야 «맞아서 튄 것» 으로 보인다
        float age = Time.time - gustAt;
        float blow = age < 0.15f ? age / 0.15f
                   : age < 1.85f ? 1f - (age - 0.15f) / 1.7f
                   : 0f;
        blow *= blow;

        for (int i = 0; i < sheets.Count; i++)
        {
            float p = Time.time * 0.6f + i * 1.7f;
            float a = i * 2.3f;
            // ★ 유저: «날아오는 게 잘 안 보인다» → 멀리·높이 던지고 <b>크기도 키운다</b>.
            //   A4 한 장은 20m 밖에서 안 보인다 — 서류 뭉치로 읽히려면 커야 한다.
            Vector3 gust = blow * new Vector3(Mathf.Cos(a) * 6.5f,
                                              4.2f + (i % 3) * 1.6f,
                                              Mathf.Sin(a) * 6.5f);
            sheets[i].localScale = Vector3.one * (1f + blow * 1.6f);

            sheets[i].localPosition = home[i] + gust + new Vector3(Mathf.Sin(p) * 0.5f,
                                                                   Mathf.Sin(p * 1.3f) * 0.35f,
                                                                   Mathf.Cos(p * 0.8f) * 0.4f);
            sheets[i].Rotate(Vector3.up, (24f + blow * 260f) * Time.deltaTime, Space.Self);
        }
    }
}

/// <summary>
/// ★ <b>플레이어를 따라오는 비행체 + 바닥 그림자.</b>
///
/// 고정 궤도로 돌면 배경이고 <b>따라오면 감시</b>다 — 그게 공짜로 찝찝해진다.
/// 그림자는 <b>바닥에 까는 판</b>이지 실시간 그림자가 아니다(§7.6: 실시간 그림자는 태양 하나).
/// 낮아질수록 그림자가 <b>작고 진해진다</b> = 가까워진다.
/// </summary>
public class FlyingProp : MonoBehaviour
{
    public float height = 17f;
    public float distance = 26f;
    public float bob = 0.2f;
    public float sideways = 0f;
    public float shadowRadius = 8f;

    /// <summary>좌우로 천천히 건너간다 — <b>보일 듯 말 듯</b> 하라는 유저 요청이 이것이다.</summary>
    public float sway = 0f;
    public float swayRate = 0.17f;

    // ──────────────────────────────── 순회 비행(감시)
    /// <summary>
    /// 꽂혀 있으면 <b>플레이어를 안 따라가고 코스 위를 돈다.</b>
    ///
    /// ★★ 2026-10-06 유저: *"왜 비행기가 저따구로 날아. 일직선으로 날아서 경기장 안을
    /// 빙글빙글 돌아야지 감시하듯이. 왜 꼬리 뒤로 나냐고."* 둘 다 맞는 지적이다.
    ///
    /// ① <b>추종은 «감시» 로 안 읽힌다.</b> 내 뒤를 졸졸 따라오면 그건 반려동물이고,
    ///    <b>제 갈 길을 일정하게 도는 것</b>이 감시다. 코스를 따라 날면 긴 직선에서는
    ///    곧게 날고 코너에서만 선회해서 «정찰 비행» 모양이 저절로 나온다.
    /// ② <b>꼬리가 앞으로 가고 있었다.</b> 기수는 로컬 <c>+Z</c> 인데(블렌더에서 재니
    ///    프로펠러가 −Y = 유니티 −Z 쪽이다) 옛 코드가 <b>플레이어를 바라보게</b> 돌렸다.
    ///    앞쪽에 띄워 놓고 뒤를 보게 했으니 <b>거꾸로 날 수밖에 없었다.</b>
    ///    이제 <b>진행 방향(접선)</b>을 본다.
    /// </summary>
    public TrackBuilder patrol;
    public float lapSeconds = 40f;
    public float lateral = 0f;

    /// <summary>
    /// ★★ 2026-10-06 유저: *"비행선이 앞으로 가긴 하는데 꼬리 쪽을 기준으로 이동해서
    /// 뒤로 가는 느낌이다. 뭉툭한 부분이 앞에 있어야 하는데."* <b>맞다 — 비행선만 거꾸로였다.</b>
    ///
    /// 전에는 프로펠러 자리로 기수를 짐작했는데 그게 틀렸다. <b>몸통 반폭을 길이 방향으로
    /// 재니</b> 결정적이다 — 비행선은 −Y 끝이 <b>반폭 3.74(뭉툭)</b>, +Y 끝이 <b>2.06(뾰족)</b> 이라
    /// <b>기수가 −Z</b> 다. 헬기는 반대로 −Y 가 0.13(테일붐), +Y 가 1.52(기수)라 <b>이쪽은 맞았다.</b>
    ///
    /// > <b>«어느 쪽이 앞인가» 는 부품 자리가 아니라 «모양» 으로 재라.</b>
    /// > 프로펠러·조명은 앞뒤 어디에나 붙지만, <b>뭉툭한 쪽이 앞</b>인 건 날아가는 물건의 성질이다.
    /// </summary>
    public bool noseBack;

    float lap;

    /// <summary>따라붙는 빠르기. 시간상수가 <c>1/follow</c> 초다 — 작으면 뒤처져 딴 데로 간다.</summary>
    public float follow = 2.6f;

    Transform target, shade;
    Renderer shadeRenderer;
    float seed;

    void Start()
    {
        seed = Random.value * 10f;

        // 순회 비행은 <b>코스 어딘가에서 시작</b>한다. 0 에서 시작하면 출발선 바로 위에
        // 떠 있다가 플레이어와 같은 속도로 붙어 가서 «따라온다» 로 보인다.
        if (patrol != null) { lap = Random.value; Patrol(seed); }

        foreach (var k in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
            if (k.GetComponent<PlayerKart>() != null) { target = k.transform; break; }
        if (target == null && Camera.main != null) target = Camera.main.transform;

        if (shadowRadius <= 0f) return;

        // 그림자 — 납작한 원기둥. Quad 는 한쪽 면만 있어서 «어느 쪽이 앞인가» 가
        // 또 하나의 짐작거리가 된다(이 프로젝트에서 방향으로 두 번 틀렸다)
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(disc.GetComponent<Collider>());
        disc.name = "Shadow";
        shade = disc.transform;
        shadeRenderer = disc.GetComponent<Renderer>();

        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetFloat("_Surface", 1f);                               // Transparent
        m.SetFloat("_Blend", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.renderQueue = 3000;
        m.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.08f, 0.34f));
        shadeRenderer.sharedMaterial = m;
    }

    void LateUpdate()
    {
        if (RacePause.On) return;
        if (target == null && patrol == null) return;

        float t = Time.time + seed;

        if (patrol != null) { Patrol(t); Shadow(); return; }

        // ★★ <b>앞쪽 비스듬히</b> 떠 있는다. 카메라가 <b>3인칭 백뷰</b>라 카트 뒤에 두면
        // 그건 곧 «카메라 뒤» 라서 <b>한 번도 화면에 안 들어온다</b> — 유저가 «비행선이 안 쫓아온다»
        // 고 한 게 이거였다. 앞에 두면 그림자가 길을 쓸고 지나가는 것까지 같이 보인다.
        float lane = sideways + Mathf.Sin(t * swayRate) * sway;
        Vector3 want = target.position
                     + target.forward * distance
                     + target.right * lane
                     + Vector3.up * (height + Mathf.Sin(t * 0.8f) * bob * 3f);

        // 시간상수 1/follow 초. 전에는 0.9초라 22m/s 짜리 카트에 20m 씩 뒤처져 딴 데로 샜다
        transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-follow * Time.deltaTime));

        Vector3 look = target.position + Vector3.up * 2f - transform.position;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                  Quaternion.LookRotation(look.normalized, Vector3.up),
                                                  1f - Mathf.Exp(-1.6f * Time.deltaTime));

        Shadow();
    }

    /// <summary>코스를 따라 일정한 속도로 돈다. 기수는 <b>진행 방향</b>.</summary>
    void Patrol(float t)
    {
        lap += Time.deltaTime / Mathf.Max(4f, lapSeconds);
        lap -= Mathf.Floor(lap);

        Vector3 fwd = patrol.TangentOnPath(lap);
        Vector3 at = patrol.transform.position + patrol.PointOnPath(lap)
                   + Vector3.Cross(Vector3.up, fwd) * lateral
                   + Vector3.up * (height + Mathf.Sin(t * 0.5f) * bob * 3f);

        transform.position = at;

        // 선회할 때 <b>안쪽으로 기운다.</b> 수평으로 미끄러지면 종이비행기로 보인다 —
        // 각속도에서 기울기를 뽑으니 코스 모양을 바꿔도 저절로 맞는다.
        float turn = Vector3.SignedAngle(lastFwd == Vector3.zero ? fwd : lastFwd, fwd, Vector3.up);
        lastFwd = fwd;
        bank = Mathf.Lerp(bank, Mathf.Clamp(turn / Mathf.Max(0.0001f, Time.deltaTime) * 0.45f, -22f, 22f),
                          1f - Mathf.Exp(-2.5f * Time.deltaTime));

        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              Quaternion.LookRotation(fwd, Vector3.up)
                                                  * Quaternion.Euler(0f, noseBack ? 180f : 0f, -bank),
                                              1f - Mathf.Exp(-4f * Time.deltaTime));
    }

    Vector3 lastFwd;
    float bank;

    void Shadow()
    {
        if (shade == null) return;

        // 바닥을 찾아 그 위 5cm 에 깐다. 높을수록 크고 옅다
        Vector3 ground = transform.position;
        float up = transform.position.y;
        if (Physics.Raycast(transform.position, Vector3.down, out var hit, 60f, ~(1 << 2)))
        { ground = hit.point; up = transform.position.y - hit.point.y; }
        else ground.y = 0f;

        float k = Mathf.Clamp01(up / 24f);
        float r = shadowRadius * (0.55f + k * 0.9f);
        shade.position = ground + Vector3.up * 0.05f;
        shade.localScale = new Vector3(r, 0.01f, r);
        shade.rotation = Quaternion.identity;

        if (shadeRenderer != null)
        {
            var c = shadeRenderer.sharedMaterial.GetColor("_BaseColor");
            c.a = Mathf.Lerp(0.42f, 0.14f, k);
            shadeRenderer.sharedMaterial.SetColor("_BaseColor", c);
        }
    }
}

/// <summary>코스 <b>바깥</b> 길을 따라 같이 달리는 차. 코스 모양을 바꿔도 안 고친다.</summary>
public class ChaseVehicle : MonoBehaviour
{
    public TrackBuilder track;
    public float offset = 18f;
    public float lead = 0f;

    Transform player;
    float t;

    void Start()
    {
        foreach (var k in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
            if (k.GetComponent<PlayerKart>() != null) { player = k.transform; break; }
    }

    /// <summary>
    /// 플레이어가 코스 어디쯤인지. <b>64칸만 훑는다</b> — <see cref="KartAi"/> 와 같은 생각이고,
    /// 코스 모양을 바꿔도 이 코드는 안 고친다.
    /// </summary>
    float NearestT(Vector3 from)
    {
        float best = 0f, bestD = float.MaxValue;
        for (int i = 0; i < 64; i++)
        {
            float u = i / 64f;
            float d = (track.transform.position + track.PointOnPath(u) - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = u; }
        }
        return best;
    }

    void Update()
    {
        if (track == null || RacePause.On) return;

        float want = player != null ? Mathf.Repeat(NearestT(player.position) + lead, 1f)
                                    : Mathf.Repeat(Time.time * 0.04f, 1f);

        t = Mathf.Repeat(t + Mathf.DeltaAngle(t * 360f, want * 360f) / 360f * 3f * Time.deltaTime, 1f);

        Vector3 fwd = track.TangentOnPath(t);
        Vector3 side = Vector3.Cross(Vector3.up, fwd);
        Vector3 at = track.transform.position + track.PointOnPath(t) + side * offset;

        transform.position = Vector3.Lerp(transform.position, at, 1f - Mathf.Exp(-4f * Time.deltaTime));
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}

// ======================================================================
//  5판 — 관장 도장이 쿵 찍는다
// ======================================================================
/// <summary>
/// ★★ 2026-10-06 유저 제안: *"미션 5에 스탬프 말인데 몇 초마다 한 번씩 세게 쿵쿵
/// 찍어내리면 어때. 그러다가 레이싱할 때 진동 울리는 거지. 그때 점프 기능 누르면 어때.
/// 점프 기능 쓸 일 없잖아 지금. 아님 종이를 조금 가져와서 플레이 화면에 날린 뒤
/// 원상복귀 시키거나."*
///
/// 둘 다 넣었다. <b>SPACE(호핑)에 처음으로 쓸 일이 생긴다</b> — 2026-09-16 에
/// 태엽(SHIFT)과 호핑(SPACE)을 가른 뒤로 호핑은 할 일이 없었다.
///
/// ★ <b>임무 내용은 안 바꾼다.</b> 5판은 그대로 «94초 안에 완주» 고, 땅울림은
/// 속도에만 영향을 준다 — 넘으면 조금 빨라지고 맞으면 조금 느려진다.
/// 판정에 끼어들면 그건 새 임무지 연출이 아니다.
/// </summary>
public class StampSlam : MonoBehaviour
{
    [Tooltip("몇 초마다 한 번 찍나")]
    public float period = 5.2f;
    [Tooltip("도장끼리 어긋나게 — 다 같이 찍으면 쿵쿵이 아니라 한 번이다")]
    public float phase = 0f;
    [Tooltip("들어 올리는 높이")]
    public float lift = 2.2f;

    Transform stamp;
    Vector3 home;
    PaperDrift papers;
    float lastU = 1f;

    /// <summary>도장 밑으로 쏘는 빨간 기둥. <b>이게 켜져 있으면 곧 찍힌다.</b></summary>
    Transform beam;

    /// <summary>내리꽂기가 끝나는 지점(주기 안의 비율). 여기서 «쿵» 이 난다.</summary>
    const float Hit = 0.70f;

    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == "Stamp") { stamp = t; break; }

        if (stamp == null)
        {
            Debug.LogWarning("[풍경] 도장(Stamp)을 못 찾았다 — R04_Paper_Storm 의 부품 이름을 봐라.");
            enabled = false; return;
        }
        home = stamp.localPosition;
        papers = GetComponent<PaperDrift>();

        // 레이저 기둥 — 도장과 바닥을 잇는다. 콜라이더 없음, 그림자 없음
        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(cyl.GetComponent<Collider>());
        cyl.name = "SlamBeam";
        cyl.GetComponent<Renderer>().sharedMaterial =
            FlatMaterial.Get(new Color32(0xC4, 0x45, 0x3E, 0xFF));
        cyl.GetComponent<Renderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        cyl.transform.SetParent(transform, false);
        beam = cyl.transform;
        beam.gameObject.SetActive(false);
    }

    void Update()
    {
        if (RacePause.On || RaceCountdown.Blocked) return;

        float u = Mathf.Repeat(Time.time + phase, period) / period;

        // 천천히 올라갔다가 <b>짧게 내리꽂는다.</b> 올라가는 데 오래 걸려야
        // «지금 찍는다» 가 예고되고, 그래야 뛰어넘을 준비를 할 수 있다.
        float h;
        if (u < 0.62f) h = Mathf.Sin(u / 0.62f * Mathf.PI * 0.5f);
        else if (u < Hit) { float k = (u - 0.62f) / (Hit - 0.62f); h = 1f - k * k; }
        else h = 0f;

        stamp.localPosition = home + Vector3.up * (lift * h);

        // ★ <b>찍히기 1.2초 전부터</b> 빨간 기둥이 켜진다. 경고 없이 때리면 회피가 운이 되고,
        //   회피가 운이면 위력을 올릴 수가 없다 — 그래서 경고가 먼저다.
        //   깜빡이는 간격이 <b>점점 빨라진다</b>: 리듬만 봐도 언제 떨어질지 안다.
        float toHit = (Hit - u) * period;
        bool arming = toHit > 0f && toHit < 1.2f;
        if (beam != null)
        {
            bool on = arming && Mathf.Repeat(toHit * (6f + (1.2f - toHit) * 10f), 1f) > 0.42f;
            if (beam.gameObject.activeSelf != on) beam.gameObject.SetActive(on);
            if (on)
            {
                // 도장 밑면에서 바닥까지. 실린더는 2단위 높이라 스케일이 절반이다
                float top = home.y + lift * h;
                float len = Mathf.Max(0.5f, top);
                beam.localPosition = new Vector3(home.x, top - len * 0.5f, home.z);
                beam.localScale = new Vector3(0.9f, len * 0.5f, 0.9f);
            }
        }

        // 주기를 넘어가며 Hit 를 지나는 순간에만 한 번
        if (lastU < Hit && u >= Hit)
        {
            RaceShock.Boom(stamp.position);
            Sfx.Play("StampSlam", 0.8f, duckMusic: false);
            if (papers != null) papers.Gust();
            if (beam != null) beam.gameObject.SetActive(false);
        }
        lastU = u;
    }
}

/// <summary>
/// 도장이 찍힐 때 퍼지는 <b>땅울림</b>. 보이는 고리가 바닥을 쓸고 지나가고,
/// 지나갈 때 <b>땅에 붙어 있으면</b> 속도를 깎인다 — <b>SPACE 로 떠 있으면 넘어간다.</b>
///
/// ★ 고리를 <b>눈에 보이게</b> 만드는 게 핵심이다. 안 보이면 «랜덤으로 느려진다» 가 되고,
/// 이 프로젝트는 그 민원을 이미 두 번 받았다(벽 부딪힘 · 자재).
/// </summary>
public static class RaceShock
{
    /// <summary>퍼지는 속도. 도장이 갓길 15m 에 있으니 코스까지 0.6초쯤 걸린다.</summary>
    const float Speed = 24f;
    // ★ 44m 로 뒀다가 다시 계산했다. 도장 다섯이 평균 1.1초마다 찍는데 고리가 44m 면
    //   <b>코스 전체가 늘 땅울림 안</b>이라 랩마다 열 번씩 맞는다 — 그러면 «94초 안에 완주» 가
    //   사실상 다른 임무가 된다. <b>28m 면 도장 근처 20m 쯤만 쓸고 지나간다.</b>
    const float Reach = 28f;
    const int Puffs = 26;

    /// <summary>
    /// ★★ 2026-10-06 유저: *"어느 타이밍에 찍히는지도 몰라서 점프 회피가 불가능하며,
    /// 도장 위력이 약하다. 빨간색 길이나 레이저 같은 걸로 표시해 주면 안 되나."*
    /// <b>경고가 없으면 회피는 운이고, 회피가 운이면 위력을 올릴 수가 없다.</b>
    /// 그래서 셋을 같이 바꿨다 — 빨간 고리 · 도장 위 레이저 · 깎이는 양.
    /// </summary>
    static readonly Color Warn = new Color32(0xC4, 0x45, 0x3E, 0xFF);

    class Ring
    {
        public Vector3 at;
        public float born, last;
        public bool used;
        public Transform[] puff;
    }

    static readonly List<Ring> rings = new();
    static KartController player;
    static KartCamera cam;
    static bool toldOnce;
    static int dodged;

    public static void Clear()
    {
        foreach (var r in rings)
            if (r.puff != null && r.puff.Length > 0 && r.puff[0] != null)
                Object.Destroy(r.puff[0].parent.gameObject);
        rings.Clear();
        dodged = 0;
    }

    public static void Boom(Vector3 at)
    {
        // 바닥을 찾아 거기에 깐다. 못 찾으면 y 0
        float y = 0f;
        if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var hit, 40f, ~(1 << 2))) y = hit.point.y;

        var holder = new GameObject("Shock").transform;
        holder.position = new Vector3(at.x, y + 0.06f, at.z);

        var mat = FlatMaterial.Get(Warn);
        var puff = new Transform[Puffs];
        for (int i = 0; i < Puffs; i++)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(d.GetComponent<Collider>());
            d.transform.SetParent(holder, false);
            d.GetComponent<Renderer>().sharedMaterial = mat;
            d.transform.localScale = new Vector3(2.2f, 0.02f, 2.2f);
            puff[i] = d.transform;
        }
        rings.Add(new Ring { at = holder.position, born = Time.time, puff = puff });
    }

    public static void Tick()
    {
        if (rings.Count == 0) return;

        if (player == null)
            foreach (var k in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
                if (k.GetComponent<PlayerKart>() != null) { player = k; cam = Object.FindFirstObjectByType<KartCamera>(); break; }

        for (int i = rings.Count - 1; i >= 0; i--)
        {
            var r = rings[i];
            float age = Time.time - r.born;
            float rad = age * Speed;

            if (rad > Reach)
            {
                if (r.puff != null && r.puff.Length > 0 && r.puff[0] != null)
                    Object.Destroy(r.puff[0].parent.gameObject);
                rings.RemoveAt(i);
                continue;
            }

            // 고리를 그린다 — 점점 커지고 옅어진다
            float fade = 1f - rad / Reach;
            fade *= fade;
            for (int p = 0; p < r.puff.Length; p++)
            {
                if (r.puff[p] == null) continue;
                float a = p / (float)r.puff.Length * Mathf.PI * 2f;
                r.puff[p].localPosition = new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad);
                float s = (2.0f + rad * 0.16f) * fade;
                r.puff[p].localScale = new Vector3(s, 0.02f, s);
            }

            // 플레이어를 지나가는 그 프레임에 한 번만
            if (!r.used && player != null)
            {
                Vector3 d = player.transform.position - r.at; d.y = 0f;
                float dist = d.magnitude;
                if (r.last < dist && dist <= rad)
                {
                    r.used = true;
                    Resolve(player);
                }
            }
            r.last = rad;
        }
    }

    static void Resolve(KartController kart)
    {
        if (kart.IsHopping || !kart.IsGrounded)
        {
            // 넘었다 — 작은 보상. 벌만 있고 상이 없으면 아무도 안 뛴다
            kart.ApplyBoost(2.5f, 0.7f);
            if (cam != null) cam.Jolt(0.06f);
            if (dodged++ < 2) Toast.Show("넘었다");
            return;
        }

        // 맞았다 — <b>가볍게</b>. 94초 제한에 쫓기는 판이라 여기서 크게 깎으면 임무가 바뀐다
        var rb = kart.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity *= 0.86f;
        if (cam != null) cam.Jolt(0.45f);

        if (!toldOnce) { toldOnce = true; Toast.Show("땅울림 — SPACE 로 뛰어넘는다"); }
    }
}

// ======================================================================
//  7판(드론 판) — 위에서 철거 자재가 떨어진다
// ======================================================================
/// <summary>
/// ★★ 2026-10-06 유저: *"드론 있는 임무 말인데 그냥 날아다니기만 하는 거야?
/// 그 임무 한정으로 자재를 랜덤으로 위에서 떨어뜨린다든가 그런 기믹 추가해도 돼?
/// 기획에는 맞나. 치이카와 같은 불쾌한 은유는 있나."*
///
/// <b>기획에 맞는다.</b> 이 판(<c>Goal.장애물</c>)의 이름이 이미 «철거 자재» 고,
/// 바닥에 굴러다니는 <see cref="RoadDebris"/> 가 그 자재다. <b>위에서 떨어지는 쪽이
/// 오히려 원래 그림</b>이야 — 철거는 위에서부터 한다.
///
/// 은유도 공짜로 생긴다. <b>중계 드론이 찍고 있는 바로 그 위에서 자재가 떨어진다.</b>
/// 치는 쪽과 찍는 쪽이 같은 하늘에 있고, 아무도 «그만» 이라고 안 한다 —
/// 방송은 사고를 막는 게 아니라 <b>사고를 기다린다.</b> 설명하는 대사가 한 줄도 없어서
/// 더 불쾌하다. 그게 치이카와식이다(§4.4 의 광고판과 같은 수법).
///
/// ★ <b>임무 판정에는 안 들어간다.</b> 이 판의 «기회 3» 은 <see cref="RoadDebris"/> 를
/// 센 것이고, 낙하 자재는 속도만 깎는다. 판정에 끼어들면 그건 새 임무지 연출이 아니다.
/// </summary>
public static class SkyDrop
{
    /// <summary>몇 초마다 하나 떨어지나.</summary>
    const float Every = 3.4f;
    /// <summary>빨간 표적이 떠 있는 시간 — 이만큼 보고 피한다.</summary>
    const float Telegraph = 1.45f;
    /// <summary>표적 반지름. 이 안에 있으면 맞는다.</summary>
    const float Radius = 3.2f;

    class Drop
    {
        public Vector3 at;
        public float born;
        public bool landed;
        public Transform mark, outline, block;
    }

    static readonly List<Drop> live = new();
    static float nextAt;
    static KartController player;
    static KartCamera cam;
    static bool told;

    public static void Clear()
    {
        foreach (var d in live) if (d.mark != null) Object.Destroy(d.mark.parent.gameObject);
        live.Clear();
        nextAt = 0f;
    }

    public static void Tick(TrackBuilder track)
    {
        bool want = MissionManager.CurrentGoal == MissionManager.Goal.장애물;
        if (!want) { if (live.Count > 0) Clear(); return; }

        if (player == null)
        {
            foreach (var k in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
                if (k.GetComponent<PlayerKart>() != null) { player = k; break; }
            cam = Object.FindFirstObjectByType<KartCamera>();
        }

        if (!RaceCountdown.Blocked && !RacePause.On && player != null && Time.time >= nextAt)
        {
            nextAt = Time.time + Every * Random.Range(0.75f, 1.3f);
            Spawn(track);
        }

        for (int i = live.Count - 1; i >= 0; i--) Step(live[i], i);
    }

    static void Spawn(TrackBuilder track)
    {
        // ★ <b>플레이어 앞</b>에 떨군다. 뒤나 옆에 떨어지면 못 보고, 못 보면 연출이 아니라 잡음이다.
        //   62~96m 앞이면 22m/s 로 3~4초 — 표적을 보고 피할 시간이 난다.
        float t = NearestT(track, player.transform.position);
        t = Mathf.Repeat(t + Random.Range(62f, 96f) / Mathf.Max(1f, track.LapLength), 1f);

        Vector3 fwd = track.TangentOnPath(t);
        Vector3 side = Vector3.Cross(Vector3.up, fwd);
        Vector3 at = track.transform.position + track.PointOnPath(t) + side * Random.Range(-3.4f, 3.4f);

        // ★★ 2026-10-06 유저: *"공중에 자재가 떨어뜨려지는 경우도 있는데 그럼 안 돼.
        //   땅에 박히게 해야지."* <b>바닥을 못 찾으면 아예 안 떨군다.</b>
        //   전에는 못 찾으면 코스 높이를 그대로 썼는데, 그 값은 길 표면이 아니라
        //   <b>중심선의 높이</b>라 둔덕·다리 위에서는 허공이 된다.
        if (!Physics.Raycast(at + Vector3.up * 8f, Vector3.down, out var hit, 60f, ~(1 << 2))) return;
        at.y = hit.point.y;

        var holder = new GameObject("SkyDrop").transform;
        holder.position = at;

        var red = FlatMaterial.Get(new Color32(0xC4, 0x45, 0x3E, 0xFF));
        var cream = FlatMaterial.Get(new Color32(0xFD, 0xF8, 0xEC, 0xFF));
        var stone = FlatMaterial.Get(new Color32(0xA8, 0xA4, 0x9A, 0xFF));

        // 바깥 테 — 크기가 고정이라 «여기가 범위» 를 알려준다
        var ring = Disc(holder, red, Radius * 2f, 0.03f);
        // 안쪽 원 — 줄어든다. 다 줄면 떨어진다
        var mark = Disc(holder, cream, Radius * 2f, 0.05f);

        // 떨어지는 자재 — 콜라이더 없음. 물리로 치면 카트가 날아가 레이스가 망가진다
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(cube.GetComponent<Collider>());
        cube.name = "Slab";
        cube.GetComponent<Renderer>().sharedMaterial = stone;
        cube.transform.SetParent(holder, false);
        cube.transform.localScale = new Vector3(2.3f, 1.1f, 1.7f);
        cube.transform.localPosition = new Vector3(0f, 34f, 0f);
        cube.transform.localRotation = Random.rotation;

        live.Add(new Drop { at = at, born = Time.time, mark = mark, outline = ring, block = cube.transform });
    }

    static Transform Disc(Transform parent, Material mat, float dia, float y)
    {
        var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(d.GetComponent<Collider>());
        d.GetComponent<Renderer>().sharedMaterial = mat;
        d.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        d.transform.SetParent(parent, false);
        d.transform.localPosition = new Vector3(0f, y, 0f);
        d.transform.localScale = new Vector3(dia, 0.01f, dia);
        return d.transform;
    }

    static void Step(Drop d, int index)
    {
        if (RacePause.On) return;

        float age = Time.time - d.born;

        if (!d.landed)
        {
            float k = Mathf.Clamp01(age / Telegraph);

            // 안쪽 원이 줄어든다 — 다 줄어드는 순간이 착탄이다
            float dia = Mathf.Lerp(Radius * 2f, 0.4f, k);
            if (d.mark != null) d.mark.localScale = new Vector3(dia, 0.01f, dia);

            // 바깥 테는 깜빡인다. 점점 빨라져서 <b>리듬만 봐도</b> 남은 시간을 안다
            if (d.outline != null)
                d.outline.gameObject.SetActive(Mathf.Repeat(age * (4f + k * 12f), 1f) > 0.35f);

            if (d.block != null)
            {
                // 마지막 0.42초에 떨어진다. 처음부터 내려오면 어디 떨어질지가 안 보인다
                float fall = Mathf.Clamp01((age - (Telegraph - 0.42f)) / 0.42f);
                // ★ 끝 높이 0.25 — 슬래브 두께의 절반쯤이라 <b>바닥에 반쯤 박힌다.</b>
                //   0.6 으로 두면 떠 있고, 0 으로 두면 바닥과 같은 평면이라 지지직거린다.
                d.block.localPosition = new Vector3(0f, Mathf.Lerp(34f, 0.25f, fall * fall), 0f);
                d.block.Rotate(60f * Time.deltaTime, 95f * Time.deltaTime, 0f, Space.Self);
            }

            if (age >= Telegraph) { d.landed = true; Land(d); }
            return;
        }

        // ★★ 2026-10-06 유저: *"자재 떨어지면 사라지게 하지 말고 냅둬. 그러나 레이스에는
        //   지장 없게."* <b>맞다 — 사라지면 «내가 본 게 맞나» 가 되고, 길에 쌓이면
        //   «여기 철거가 진행 중» 이 눈에 남는다.</b> 콜라이더가 없으니 지장은 0 이다.
        //   목록에서만 빼고 <b>슬래브는 그 자리에 둔다</b>(판이 바뀌면 Clear 가 치운다).
        if (age > Telegraph + 0.9f)
        {
            if (d.block != null) d.block.SetParent(RaceProps.Root, true);
            if (d.mark != null) Object.Destroy(d.mark.parent.gameObject);
            live.RemoveAt(index);
        }
    }

    static void Land(Drop d)
    {
        if (d.outline != null) d.outline.gameObject.SetActive(false);
        if (d.mark != null) d.mark.gameObject.SetActive(false);

        if (player == null) return;

        Vector3 v = player.transform.position - d.at; v.y = 0f;
        if (v.magnitude > Radius + 1.1f) return;

        // 맞았다 — 떨어지는 콘크리트라 땅울림보다 세다. 대신 <b>표적이 1.45초 동안 보였다.</b>
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity *= 0.72f;
        player.CancelBoost();
        if (cam != null) cam.Jolt(0.6f);

        if (!told) { told = true; Toast.Show("위를 봐라 — 빨간 표적은 비켜라"); }
    }

    /// <summary>코스에서 제일 가까운 지점의 t. <see cref="ChaseVehicle"/> 와 같은 방식이다.</summary>
    static float NearestT(TrackBuilder track, Vector3 world)
    {
        Vector3 local = world - track.transform.position;
        float best = 0f, bestD = float.MaxValue;
        for (int i = 0; i < 64; i++)
        {
            float t = i / 64f;
            float dd = (track.PointOnPath(t) - local).sqrMagnitude;
            if (dd < bestD) { bestD = dd; best = t; }
        }
        return best;
    }
}
