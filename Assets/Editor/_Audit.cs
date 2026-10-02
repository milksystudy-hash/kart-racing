using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 게임 자가점검 — 레이스 한 판을 <b>실제로 돌려서</b> 재고, 미니게임 셋은 전수/시뮬레이션으로 본다.
/// 배치모드 전용, 메뉴 없음. 씬을 저장하지 않고 <b>PlayerPrefs 도 건드리지 않는다.</b>
///
/// ★ 레이스는 플레이 모드에서만 잴 수 있다. <c>-quit</c> 없이 돌리고 탐침이 끝나면 스스로 나간다.
/// </summary>
public static class _Audit
{
    static string Out => System.Environment.GetEnvironmentVariable("AUDIT_OUT")
                         ?? "C:/temp/audit.txt";

    /// <summary>씬이 필요 없는 것부터. 플레이 모드 없이 끝난다.</summary>
    public static void Desk()
    {
        var sb = new StringBuilder();
        Canteen(sb);
        Drill(sb);
        File.WriteAllText(Out, sb.ToString());
        Debug.Log(sb.ToString());
    }

    /// <summary>레이스를 실제로 한 판 돌린다. <c>-quit</c> 를 빼고 부를 것.</summary>
    public static void Race()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Track.unity", OpenSceneMode.Single);
        var go = new GameObject("_AuditProbe");
        go.AddComponent<AuditProbe>();
        EditorApplication.EnterPlaymode();
    }

    // ==================================================================
    //  오늘의 급식 — 992가지를 전수로
    // ==================================================================
    static void Canteen(StringBuilder sb)
    {
        sb.AppendLine("══ 오늘의 급식 ══");

        int dishes = 5, cases = 0;
        var byVerdict = new Dictionary<string, (int n, int lo, int hi)>();
        int lazyBest = 0, honestWorst = int.MaxValue, honestBest = 0;

        for (int wanted = 1; wanted < (1 << dishes); wanted++)
            for (int served = 0; served < (1 << dishes); served++)
            {
                cases++;
                foreach (float held in new[] { 1.5f, 6f })
                {
                    int p = CanteenOrder.Points(wanted, served, held, out string v);
                    if (!byVerdict.TryGetValue(v, out var e)) e = (0, int.MaxValue, int.MinValue);
                    byVerdict[v] = (e.n + 1, Mathf.Min(e.lo, p), Mathf.Max(e.hi, p));

                    // «제일 게으른 손» = 주문을 안 읽고 내보낸다(빈 식판이거나 아무거나)
                    if (served == 0 || served != wanted) lazyBest = Mathf.Max(lazyBest, p);
                    if (served == wanted)
                    {
                        honestWorst = Mathf.Min(honestWorst, p);
                        honestBest = Mathf.Max(honestBest, p);
                    }
                }
            }

        foreach (var kv in byVerdict)
            sb.AppendLine($"  {kv.Key,-14} {kv.Value.n,5}가지  {kv.Value.lo}~{kv.Value.hi}점");
        sb.AppendLine($"  주문 × 담기 {cases}가지 전수");
        sb.AppendLine($"  주문을 안 읽은 손의 최고점 {lazyBest}  vs  읽고 맞춘 손 {honestWorst}~{honestBest}");
        sb.AppendLine(lazyBest < honestWorst ? "  → 정답이 이긴다 ✔" : "  → ★ 대충 해도 같거나 이긴다");
        sb.AppendLine();
    }

    // ==================================================================
    //  안전 점검 훈련 — 손 다섯 가지를 60초 × 400판
    // ==================================================================
    static void Drill(StringBuilder sb)
    {
        sb.AppendLine("══ 안전 점검 훈련 ══");
        sb.AppendLine($"  손{"",-12}  평균  최저~최고");

        foreach (var (name, react) in new[]
                 { ("안 누른다", -1f), ("난타(아무 데나)", 0f), ("켜진 것만 난타", 0.02f),
                   ("사람 0.45초", 0.45f), ("사람 0.32초", 0.32f), ("최적 0.18초", 0.18f) })
        {
            var scores = new List<int>();
            for (int seed = 0; seed < 400; seed++) scores.Add(DrillRun(seed, react));
            scores.Sort();
            sb.AppendLine($"  {name,-16} {Avg(scores),5:0}  {scores[0]}~{scores[scores.Count - 1]}");
        }
        sb.AppendLine();
    }

    static float Avg(List<int> v) { float s = 0; foreach (var x in v) s += x; return s / v.Count; }

    /// <summary>
    /// 규칙을 그대로 흉내 낸다 — 점수·연속·정지는 <see cref="SafetyDrill"/> 의 상수를 쓴다.
    /// <c>react</c> 가 음수면 아무것도 안 누르고, 0 이면 아무 칸이나 계속 두드린다.
    /// </summary>
    static int DrillRun(int seed, float react)
    {
        var rng = new System.Random(seed);
        const float step = 0.05f;
        float redRatio = 0.30f, life = 0.85f, gap = 0.75f;

        int score = 0, streak = 0;
        float frozen = 0f, nextLight = 0.6f;
        var lit = new List<(int pad, float until, bool red)>();
        var pressed = new HashSet<int>();

        for (float t = 0f; t < SafetyDrill.Duration; t += step)
        {
            lit.RemoveAll(p => t > p.until);

            if (frozen <= 0f && t >= nextLight)
            {
                bool red = rng.NextDouble() < redRatio;
                lit.Add((rng.Next(SafetyDrill.Pads), t + life, red));
                nextLight = t + gap * (t > 40f ? 0.55f : t > 20f ? 0.75f : 1f);
            }
            if (frozen > 0f) { frozen -= step; continue; }
            if (react < 0f) continue;

            for (int i = lit.Count - 1; i >= 0; i--)
            {
                var p = lit[i];
                if (t < p.until - life + react) continue;      // 아직 반응 못 했다
                if (p.red)
                {
                    if (react <= 0.02f) { frozen = SafetyDrill.FreezeSeconds; streak = 0; lit.RemoveAt(i); }
                    continue;                                   // 사람은 빨강을 피한다
                }
                streak++;
                score += SafetyDrill.HitPoints + SafetyDrill.Bonus(streak);
                lit.RemoveAt(i);
            }

            // 난타는 꺼진 칸도 누른다 → 연속이 끊긴다
            if (react <= 0.001f && lit.Count == 0) streak = 0;
        }
        return score;
    }
}

/// <summary>레이스를 한 판 돌리며 랩타임을 잰다. 끝나면 에디터를 닫는다.</summary>
public class AuditProbe : MonoBehaviour
{
    const float HardTimeout = 260f;

    readonly StringBuilder sb = new();
    readonly List<RaceProgress> cars = new();
    readonly Dictionary<RaceProgress, (int lap, float at, List<float> laps)> seen = new();

    float began;
    bool armed;

    void Start() => DontDestroyOnLoad(gameObject);

    void Update()
    {
        if (!armed)
        {
            // AI 카트는 게이트가 꺼 놨다 — 재려면 켜야 한다. 저장은 안 한다.
            foreach (var ai in FindObjectsByType<KartAi>(FindObjectsInactive.Include,
                                                         FindObjectsSortMode.None))
            {
                ai.gameObject.SetActive(true);
                ai.enabled = true;
            }
            var standings = FindFirstObjectByType<RaceStandings>();
            if (standings != null) standings.Recount();

            cars.AddRange(FindObjectsByType<RaceProgress>(FindObjectsInactive.Exclude,
                                                          FindObjectsSortMode.None));
            if (cars.Count == 0) return;

            RaceBriefing.Skip();
            RaceCountdown.Skip();

            foreach (var c in cars) seen[c] = (c.Lap, Time.time, new List<float>());
            began = Time.time;
            armed = true;

            sb.AppendLine("══ 레이스 (AI 가 세 바퀴) ══");
            foreach (var c in cars)
            {
                var ai = c.GetComponent<KartAi>();
                sb.AppendLine($"  {c.DisplayName,-10} 실력 {(ai != null ? ai.skill : -1f):0.00}");
            }
            return;
        }

        foreach (var c in cars)
        {
            var e = seen[c];
            if (c.Lap != e.lap)
            {
                e.laps.Add(Time.time - e.at);
                seen[c] = (c.Lap, Time.time, e.laps);
            }
            else if (c.Finished && e.laps.Count < c.totalLaps)
            {
                e.laps.Add(Time.time - e.at);
                seen[c] = (c.Lap, Time.time, e.laps);
            }
        }

        bool allDone = true;
        foreach (var c in cars) if (!c.Finished) allDone = false;

        if (!allDone && Time.time - began < HardTimeout) return;
        Report(allDone);
    }

    void Report(bool allDone)
    {
        var mission = FindFirstObjectByType<MissionManager>();
        float best = 999f, bestTotal = 999f;

        foreach (var c in cars)
        {
            var laps = seen[c].laps;
            float total = 0f;
            var line = new StringBuilder($"  {c.DisplayName,-10} ");
            foreach (var l in laps) { total += l; line.Append($"{l,6:0.0}초 "); }
            line.Append($" 합계 {total,6:0.0}초{(c.Finished ? "" : "  (못 끝냄)")}");
            sb.AppendLine(line.ToString());
            foreach (var l in laps) best = Mathf.Min(best, l);
            if (c.Finished) bestTotal = Mathf.Min(bestTotal, total);
        }

        if (!allDone) sb.AppendLine($"  ★ {HardTimeout}초 안에 못 끝낸 차가 있다");

        if (mission != null && bestTotal < 900f)
        {
            sb.AppendLine();
            sb.AppendLine($"  제일 빠른 한 바퀴 {best:0.0}초 · 제일 빠른 완주 {bestTotal:0.0}초");
            sb.AppendLine($"  제한시간 {mission.timeLimit:0}초  → 여유 {(mission.timeLimit / bestTotal - 1f) * 100f:0}%");
            sb.AppendLine($"  완벽    {mission.perfectTimeLimit:0}초  → 여유 {(mission.perfectTimeLimit / bestTotal - 1f) * 100f:0}%");
        }

        string path = System.Environment.GetEnvironmentVariable("AUDIT_OUT") ?? "C:/temp/audit.txt";
        File.AppendAllText(path, sb.ToString());
        Debug.Log(sb.ToString());
        EditorApplication.Exit(0);
    }
}
