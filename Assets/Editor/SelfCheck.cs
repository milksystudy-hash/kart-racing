using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// <b>레이싱 · 곰밥마당 · 철곰관 자가점검.</b> 유니티를 안 열고(배치모드) 씬을 읽어서
/// «게임이 돌아가기 위해 반드시 참이어야 하는 것» 들을 확인한다.
///
/// ★ <b>«눈으로 보면 알 것» 을 안 본다.</b> 여기서 보는 건 화면에 안 드러나는 것들이다 —
/// 체크포인트 번호가 비었는지, 게으른 손이 정답인지, 판이 소품에 제대로 붙는지.
/// 이런 건 <b>플레이해도 한참 뒤에나</b> 드러나서, 그때는 원인을 못 찾는다.
/// </summary>
public static class SelfCheck
{
    [MenuItem("Racing/전체 자가점검", false, 21)]
    public static void All()
    {
        var log = new StringBuilder();
        int bad = 0;
        bad += Racing(log);
        log.Append('\n');
        bad += Canteen(log);
        log.Append('\n');
        bad += Drill(log);

        Debug.Log($"[자가점검]\n{log}\n══ 전체 문제 {bad}건");
    }

    // ══ 레이싱 ═══════════════════════════════════════════════════════════════

    static int Racing(StringBuilder log)
    {
        int bad = 0;
        log.AppendLine("── 레이싱 (Track.unity)");
        EditorSceneManager.OpenScene("Assets/Scenes/Track.unity", OpenSceneMode.Single);

        var lap = Object.FindFirstObjectByType<LapTracker>();
        var gate = Object.FindFirstObjectByType<StartGate>();
        var cps = new List<Checkpoint>(Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None));
        var racers = new List<RaceProgress>(Object.FindObjectsByType<RaceProgress>(FindObjectsSortMode.None));
        var ai = Object.FindObjectsByType<KartAi>(FindObjectsSortMode.None);
        var player = Object.FindFirstObjectByType<PlayerKart>();

        if (lap == null) { log.AppendLine("  ★ LapTracker 가 없다 — 랩이 안 세어진다"); bad++; }
        if (gate == null) { log.AppendLine("  ★ StartGate 가 없다 — 출발을 못 한다"); bad++; }
        if (player == null) { log.AppendLine("  ★ 플레이어 카트가 없다"); bad++; }

        log.AppendLine($"  카트 {racers.Count}대 (플레이어 {(player != null ? 1 : 0)} · AI {ai.Length}) · "
                     + $"체크포인트 {cps.Count}개");

        // ── 체크포인트 번호가 0..N-1 로 빠짐없이 한 번씩 ──
        // ★ 번호가 하나 비면 <b>그 앞에서 모두가 영원히 멈춘다</b> — 순서대로만 통과로 치니까.
        // 화면에는 «랩이 안 올라간다» 로만 보여서 원인을 못 찾는다.
        if (cps.Count > 0)
        {
            var seen = new Dictionary<int, int>();
            foreach (var c in cps)
            {
                seen.TryGetValue(c.index, out int had);
                seen[c.index] = had + 1;
            }

            var missing = new List<int>();
            var dupes = new List<int>();
            for (int i = 0; i < cps.Count; i++)
            {
                if (!seen.TryGetValue(i, out int n)) missing.Add(i);
                else if (n > 1) dupes.Add(i);
            }
            foreach (var kv in seen)
                if (kv.Key < 0 || kv.Key >= cps.Count) missing.Add(-1);

            if (missing.Count > 0 || dupes.Count > 0)
            {
                log.AppendLine($"  ★ 체크포인트 번호가 깨졌다 — 빠진 것 [{string.Join(",", missing)}] "
                             + $"겹친 것 [{string.Join(",", dupes)}]");
                bad++;
            }
            else log.AppendLine($"  번호 0~{cps.Count - 1} 빠짐·겹침 없다");

            // 트리거가 아니면 <b>차가 벽에 부딪힌다</b>
            int solid = 0, tiny = 0;
            float widest = 0f;
            foreach (var c in cps)
            {
                var col = c.GetComponent<Collider>();
                if (col == null || !col.isTrigger) solid++;
                else
                {
                    float wide = Mathf.Max(col.bounds.size.x, col.bounds.size.z);
                    widest = Mathf.Max(widest, wide);
                    if (wide < 6f) tiny++;
                }
            }
            if (solid > 0) { log.AppendLine($"  ★ 트리거가 아닌 체크포인트 {solid}개 — 차가 부딪힌다"); bad++; }
            if (tiny > 0) { log.AppendLine($"  ★ 폭이 6m 안 되는 체크포인트 {tiny}개 — 옆으로 새면 그냥 지나친다"); bad++; }
            if (solid == 0 && tiny == 0)
                log.AppendLine($"  전부 트리거 · 제일 넓은 것 {widest:0.0}m");

            // 이웃한 번호끼리 <b>고르게</b> 떨어져 있나. 한 구간만 유난히 길면 거기서 길을 잃는다
            float near = float.MaxValue, far = 0f;
            for (int i = 0; i < cps.Count; i++)
            {
                var a = Checkpoint.ByIndex(i);
                var b = Checkpoint.ByIndex((i + 1) % cps.Count);
                if (a == null || b == null) continue;
                float dd = Vector3.Distance(a.transform.position, b.transform.position);
                near = Mathf.Min(near, dd); far = Mathf.Max(far, dd);
            }
            if (far > 0f)
            {
                log.AppendLine($"  관문 간격 {near:0.0}~{far:0.0}m (비 {far / Mathf.Max(0.01f, near):0.0}배)");
                if (far / Mathf.Max(0.01f, near) > 4f)
                { log.AppendLine("  ★ 한 구간만 유난히 길다 — 거기서 길을 잃거나 순위가 튄다"); bad++; }
            }
        }

        // ── 규칙이 카트마다 같은 값인가 ──
        // ★ 한 대만 totalLaps 가 다르면 <b>그 카트만 먼저 끝난다.</b> 순위표가 거짓말을 한다.
        if (lap != null && racers.Count > 0)
        {
            var laps = new HashSet<int>();
            var cpn = new HashSet<int>();
            foreach (var r in racers) { laps.Add(r.totalLaps); cpn.Add(r.totalCheckpoints); }

            bool same = laps.Count == 1 && cpn.Count == 1;
            bool agrees = same && laps.Contains(lap.totalLaps) && cpn.Contains(lap.checkpointCount);
            bool real = cpn.Contains(cps.Count);

            log.AppendLine($"  랩 {lap.totalLaps} · 관문 {lap.checkpointCount} "
                         + $"(카트들 랩 [{string.Join(",", laps)}] 관문 [{string.Join(",", cpn)}])");
            if (!same) { log.AppendLine("  ★ 카트마다 규칙이 다르다 — 한 대만 먼저 끝난다"); bad++; }
            if (!agrees) { log.AppendLine("  ★ LapTracker 와 카트의 규칙이 어긋난다"); bad++; }
            if (!real) { log.AppendLine($"  ★ 규칙은 관문 {lap.checkpointCount}개인데 씬에는 {cps.Count}개다"); bad++; }
        }

        // ── 낙사 판정선이 트랙보다 아래인가 ──
        // ★ killPlaneY 가 트랙보다 위면 <b>달리다가 저절로 되돌려진다.</b>
        if (lap != null)
        {
            float low = float.MaxValue;
            foreach (var c in cps) low = Mathf.Min(low, c.transform.position.y);
            if (low < float.MaxValue)
            {
                float room = low - lap.killPlaneY;
                log.AppendLine($"  낙사선 {lap.killPlaneY:0.0} · 제일 낮은 관문 {low:0.0} (여유 {room:0.0}m)");
                if (room < 3f) { log.AppendLine("  ★ 낙사선이 트랙에 너무 가깝다 — 멀쩡히 달리다 되돌려진다"); bad++; }
            }
        }

        // ── AI 가 실제로 겨룰 상대가 되나 ──
        if (ai.Length == 0) { log.AppendLine("  ★ AI 카트가 없다 — 혼자 달린다"); bad++; }
        foreach (var a in ai)
            if (a.GetComponent<RaceProgress>() == null)
            { log.AppendLine($"  ★ {a.name} 에 RaceProgress 가 없다 — 순위에 안 잡힌다"); bad++; break; }

        return bad;
    }

    // ══ 곰밥마당 ═════════════════════════════════════════════════════════════

    static int Canteen(StringBuilder log)
    {
        int bad = 0;
        log.AppendLine("── 곰밥마당 (오늘의 급식)");

        // ── ① <b>게으른 손이 정답인가.</b> 실패가 없는 게임에서 제일 중요한 계산이다 ──
        //
        // 세 가지 손을 90초짜리 한 판으로 돌려 본다. 「빈 식판 연타」가 이기면 게임이 깨진 것이다.
        int spam = 0, random = 0, careful = 0;
        var rng = new System.Random(11);
        const int Rounds = 40;                       // 한 판에 손님 40명쯤

        for (int i = 0; i < Rounds; i++)
        {
            int wanted = 0;
            int size = 2 + rng.Next(3);
            var pool = new List<int> { 0, 1, 2, 3, 4 };
            for (int k = 0; k < size; k++)
            {
                int j = rng.Next(pool.Count);
                wanted |= 1 << pool[j];
                pool.RemoveAt(j);
            }

            // 빈 식판으로 ENTER 연타 — 담는 시간이 0이라 <b>남들보다 세 배 많이</b> 돌린다
            spam += CanteenOrder.Points(wanted, 0, 0.2f, out _) * 3;
            // 아무거나 담기
            random += CanteenOrder.Points(wanted, rng.Next(1, 32), 2.0f, out _);
            // 읽고 맞추기
            careful += CanteenOrder.Points(wanted, wanted, 2.5f, out _);
        }

        log.AppendLine($"  90초 한 판 — 빈 식판 연타 {spam} · 아무거나 {random} · 읽고 맞춤 {careful}");
        if (spam >= careful * 0.5f) { log.AppendLine("  ★ 빈 식판 연타가 통한다 — 주문표를 읽을 이유가 없어진다"); bad++; }
        if (random >= careful * 0.6f) { log.AppendLine("  ★ 아무거나 담아도 비슷하다"); bad++; }
        if (bad == 0) log.AppendLine("  게으른 손이 정답이 아니다");

        // ── ② 점수표가 «틀릴수록 손해» 인가 ──
        int p0 = CanteenOrder.Points(0b10101, 0b10101, 9f, out _);
        int pf = CanteenOrder.Points(0b10101, 0b10101, 1f, out _);
        int p1 = CanteenOrder.Points(0b10101, 0b10001, 1f, out _);
        int p2 = CanteenOrder.Points(0b10101, 0b01010, 1f, out _);
        int pe = CanteenOrder.Points(0b10101, 0, 1f, out _);
        log.AppendLine($"  딱맞음 {p0} · 빠르게딱맞음 {pf} · 하나어긋남 {p1} · 둘이상 {p2} · 빈식판 {pe}");
        if (!(pf > p0 && p0 > p1 && p1 > p2 && p2 <= 0 && pe == 0))
        { log.AppendLine("  ★ 점수 차례가 «정확할수록 이득» 이 아니다"); bad++; }

        // ── ③ 식판에 다섯 칸이 실제로 들어가나 ──
        //
        // ★ <b>const 를 변수에 한 번 받아서 비교한다.</b> 상수끼리 바로 비교하면 컴파일러가
        // 답을 미리 접어서 «닿을 수 없는 코드» 경고를 낸다 — 그러면 <b>«지금은 맞으니까»
        // 라는 이유로 검사 자체가 사라진 것처럼 보인다.</b> 값이 바뀌면 다시 살아나야 한다.
        float food = CanteenStage.TrayFoodWidth, step = CanteenStage.TraySlotStep;
        float tray = CanteenStage.TrayWidth;
        float need = step * (CanteenOrder.Slots - 1) + food;

        log.AppendLine($"  식판 {tray:0.00}m · 음식 5칸이 차지하는 폭 {need:0.00}m · 음식 하나 {food:0.000}m");
        if (need > tray) { log.AppendLine("  ★ 음식이 식판 밖으로 나간다"); bad++; }
        if (step < food) { log.AppendLine("  ★ 칸 간격이 음식보다 좁다 — 서로 파고든다"); bad++; }

        // ── ④ 숫자키가 <b>두 줄 다</b> 잡히나 ──
        // 수연이 두 번 짚은 자리다: *"넘버패드있는쪽은 안되고."*
        string keys = System.IO.File.ReadAllText("Assets/Scripts/Keys.cs");
        bool bothRows = keys.Contains("numpad") && keys.Contains("digit");
        log.AppendLine(bothRows ? "  Keys 가 숫자열과 키패드를 둘 다 읽는다"
                                : "  ★ Keys 에 키패드가 없다");
        if (!bothRows) bad++;

        // ── ⑤ 캠퍼스의 자리 ──
        bad += Spot("오늘의 급식", log);
        return bad;
    }

    // ══ 철곰관 ═══════════════════════════════════════════════════════════════

    static int Drill(StringBuilder log)
    {
        int bad = 0;
        log.AppendLine("── 철곰관 (안전 점검 훈련)");

        // ── ① 점수표 ──
        log.AppendLine($"  한 판 {SafetyDrill.Duration:0}초 · 판 {SafetyDrill.Pads}장 · "
                     + $"한 장 {SafetyDrill.HitPoints}점 · 정지 {SafetyDrill.FreezeSeconds:0.0}초");

        var boni = new List<string>();
        for (int s = 0; s <= 25; s += 5) boni.Add($"{s}→+{SafetyDrill.Bonus(s)}");
        log.AppendLine("  연속 보너스 " + string.Join(" ", boni));

        if (SafetyDrill.Bonus(0) != 0 || SafetyDrill.Bonus(SafetyDrill.StreakStart - 1) != 0)
        { log.AppendLine("  ★ 연속이 짧은데도 보너스가 붙는다"); bad++; }
        if (SafetyDrill.Bonus(999) != SafetyDrill.StreakCap)
        { log.AppendLine($"  ★ 보너스 상한이 {SafetyDrill.StreakCap} 로 안 막힌다 — 한 번 붙으면 끝없이 커진다"); bad++; }

        // ── ② 반응훈련벽이 씬에 있고 <b>3 × 3 격자</b>인가 ──
        //
        // ★ 판이 소품에 붙는 게임이라, <b>소품을 옮기거나 키우면 여기서 깨진다.</b>
        // 오늘 웅지관·곰솥관 배율을 건드렸으니 더더욱 확인해야 한다.
        EditorSceneManager.OpenScene("Assets/Scenes/Campus.unity", OpenSceneMode.Single);

        var wall = GameObject.Find("In_반응훈련벽");
        if (wall == null) { log.AppendLine("  ★ In_반응훈련벽 이 씬에 없다"); return bad + 1; }

        var pads = new List<Transform>();
        for (int no = 1; no <= SafetyDrill.Pads; no++)
            foreach (var t in wall.GetComponentsInChildren<Transform>(true))
                if (t.name == "Target_" + no) { pads.Add(t); break; }

        if (pads.Count != SafetyDrill.Pads)
        { log.AppendLine($"  ★ 곰발 판이 {pads.Count}/{SafetyDrill.Pads} 장만 있다"); return bad + 1; }

        var at = new Vector3[pads.Count];
        for (int i = 0; i < pads.Count; i++)
        {
            var r = pads[i].GetComponentInChildren<Renderer>();
            at[i] = r != null ? r.bounds.center : pads[i].position;
        }

        // 평면에서 법선을 뽑는다 — 트랜스폼 축을 안 믿는 <see cref="SafetyDrillBoard"/> 와 같은 식
        Vector3 a = Vector3.zero, b = Vector3.zero;
        float bestA = 0f, bestB = 0f;
        for (int i = 1; i < at.Length; i++)
        {
            Vector3 v = at[i] - at[0];
            if (v.sqrMagnitude > bestA) { bestA = v.sqrMagnitude; a = v; }
        }
        for (int i = 1; i < at.Length; i++)
        {
            Vector3 off = (at[i] - at[0]) - Vector3.Project(at[i] - at[0], a);
            if (off.sqrMagnitude > bestB) { bestB = off.sqrMagnitude; b = off; }
        }
        Vector3 norm = Vector3.Cross(a, b).normalized;

        float flat = 0f;
        foreach (var p in at) flat = Mathf.Max(flat, Mathf.Abs(Vector3.Dot(p - at[0], norm)));

        float loY = float.MaxValue, hiY = float.MinValue;
        foreach (var p in at) { loY = Mathf.Min(loY, p.y); hiY = Mathf.Max(hiY, p.y); }

        // 격자 크기 — 평면 안에서 가로로 재면 폭이 나온다
        Vector3 up = (Vector3.up - norm * Vector3.Dot(Vector3.up, norm)).normalized;
        Vector3 across = Vector3.Cross(up, norm).normalized;
        float loX = float.MaxValue, hiX = float.MinValue;
        foreach (var p in at)
        {
            float u = Vector3.Dot(p - at[0], across);
            loX = Mathf.Min(loX, u); hiX = Mathf.Max(hiX, u);
        }

        log.AppendLine($"  판 9장 · 격자 {hiX - loX:0.00} x {hiY - loY:0.00}m · "
                     + $"한 평면에서 벗어난 정도 {flat:0.000}m · 높이 {loY:0.00}~{hiY:0.00}m");

        if (flat > 0.08f) { log.AppendLine("  ★ 아홉 장이 한 평면에 없다 — 법선 계산이 흔들린다"); bad++; }
        if (hiX - loX < 0.8f || hiY - loY < 0.8f)
        { log.AppendLine("  ★ 격자가 너무 작다 — 화면에 과녁으로 안 보인다"); bad++; }
        if (loY < 0.3f || hiY > 3.2f)
        { log.AppendLine("  ★ 판이 사람이 볼 높이를 벗어났다"); bad++; }

        // 3 × 3 인가 — 세로로 세 무리, 각 무리에 셋
        var rows = new List<float>();
        foreach (var p in at)
        {
            bool had = false;
            foreach (var r in rows) if (Mathf.Abs(r - p.y) < 0.15f) { had = true; break; }
            if (!had) rows.Add(p.y);
        }
        if (rows.Count != 3) { log.AppendLine($"  ★ 가로 줄이 {rows.Count}줄이다 — 3 × 3 이 아니다"); bad++; }
        else log.AppendLine("  3 × 3 격자 — 키패드 모양 그대로");

        bad += Spot("안전 점검 훈련", log);
        return bad;
    }

    // ══ 공통 ═════════════════════════════════════════════════════════════════

    /// <summary>캠퍼스에 그 자리가 있고 <b>«만든 게임»</b> 으로 읽히나.</summary>
    static int Spot(string title, StringBuilder log)
    {
        if (!EditorSceneManager.GetActiveScene().name.StartsWith("Campus"))
            EditorSceneManager.OpenScene("Assets/Scenes/Campus.unity", OpenSceneMode.Single);

        foreach (var s in Object.FindObjectsByType<MinigameSpot>(FindObjectsSortMode.None))
            if (s.title == title)
            {
                bool open = s.Line == s.title;
                log.AppendLine(open ? $"  자리 「{title}」 — 열림"
                                    : $"  ★ 자리 「{title}」 — {s.Line}");
                return open ? 0 : 1;
            }

        log.AppendLine($"  ★ 자리 「{title}」 가 캠퍼스에 없다");
        return 1;
    }
}
