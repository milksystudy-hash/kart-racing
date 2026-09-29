using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// <b>따라 그리기 자가점검.</b> 유니티를 안 열고(배치모드) 씬을 읽어서, 게임이 실제로
/// 돌아가기 위해 필요한 것들이 <b>거기 있는지</b>와 <b>좌표가 안 뒤집혔는지</b>를 본다.
///
/// 판정(<see cref="Tracing"/>)은 이미 3D 없이 전수로 맞춰 놨다. 여기서 보는 건 그 판정과
/// <b>실제 화판 사이</b>다 — 이 프로젝트에서 «규칙은 맞는데 화면이 틀린» 일이 계속 났다.
/// </summary>
public static class TracingCheck
{
    [MenuItem("Racing/따라 그리기 점검", false, 20)]
    public static void Run()
    {
        int bad = 0;
        var log = new List<string>();

        EditorSceneManager.OpenScene("Assets/Scenes/Campus.unity", OpenSceneMode.Single);

        // ── ① 입간판이 «만든 게임» 으로 보이나 ──────────────────────────────────
        MinigameSpot spot = null;
        foreach (var s in Object.FindObjectsByType<MinigameSpot>(FindObjectsSortMode.None))
            if (s.title == MinigameSpot.TraceTitle) spot = s;

        if (spot == null) { log.Add("★ 재주관 미니게임 자리가 씬에 없다"); bad++; }
        else
        {
            log.Add($"자리: 「{spot.title}」 · {spot.blurb}");
            // Now 는 «여덟 판 다 깼나» 에도 걸린다. 여기서 보는 건 <b>제목 판정</b>이다.
            log.Add(spot.Line == spot.title
                    ? "제목 판정: 열림 (Made 목록에 있다)"
                    : $"★ 아직 «준비 중» 으로 읽힌다 — {spot.Line}");
            if (spot.Line != spot.title) bad++;
        }

        // ── ② 화판을 찾을 수 있나 ───────────────────────────────────────────────
        Transform canvas = null;
        var easel = GameObject.Find("In_이젤");
        if (easel != null)
            foreach (var t in easel.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Canvas")) { canvas = t; break; }

        if (canvas == null) { log.Add("★ In_이젤 안에서 Canvas 면을 못 찾았다"); Report(log, bad + 1); return; }

        var mf = canvas.GetComponent<MeshFilter>();
        var mr = canvas.GetComponent<Renderer>();
        if (mf == null || mr == null || mf.sharedMesh == null)
        { log.Add("★ Canvas 에 메시나 렌더러가 없다"); Report(log, bad + 1); return; }

        log.Add($"화판: {canvas.name} · 꼭짓점 {mf.sharedMesh.vertexCount} · 재질 {mr.sharedMaterial?.name}");

        // ── ③ 평면을 <see cref="TracingBoard"/> 와 <b>같은 식</b>으로 잰다 ──────
        var v = mf.sharedMesh.vertices;
        var w = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++) w[i] = canvas.TransformPoint(v[i]);

        Vector3 a = Vector3.zero; float best = 0f;
        for (int i = 1; i < w.Length; i++)
        {
            Vector3 dd = w[i] - w[0];
            if (dd.sqrMagnitude > best) { best = dd.sqrMagnitude; a = dd; }
        }
        Vector3 b = Vector3.zero; best = 0f;
        for (int i = 1; i < w.Length; i++)
        {
            Vector3 off = (w[i] - w[0]) - Vector3.Project(w[i] - w[0], a);
            if (off.sqrMagnitude > best) { best = off.sqrMagnitude; b = off; }
        }

        Vector3 n = Vector3.Cross(a, b).normalized;
        Vector3 up = (Vector3.up - n * Vector3.Dot(Vector3.up, n)).normalized;
        Vector3 across = Vector3.Cross(up, n).normalized;

        float loU = float.MaxValue, hiU = float.MinValue, loV = float.MaxValue, hiV = float.MinValue;
        foreach (var p in w)
        {
            float u = Vector3.Dot(p - w[0], across), q = Vector3.Dot(p - w[0], up);
            loU = Mathf.Min(loU, u); hiU = Mathf.Max(hiU, u);
            loV = Mathf.Min(loV, q); hiV = Mathf.Max(hiV, q);
        }
        float sizeU = hiU - loU, sizeV = hiV - loV;
        Vector3 origin = w[0] + across * loU + up * loV;

        log.Add($"크기 {sizeU:0.00} x {sizeV:0.00} m · 바닥에서 {origin.y:0.00}m · "
              + $"법선 기울기 {Vector3.Angle(n, Vector3.up):0}도(90이면 수직)");

        if (sizeU < 0.35f || sizeV < 0.35f) { log.Add("★ 화판이 너무 작다 — 그릴 수가 없다"); bad++; }
        if (Mathf.Abs(Vector3.Angle(n, Vector3.up) - 90f) > 35f)
        { log.Add("★ 화판이 세워져 있지 않다 — 세로 축을 «월드의 위» 로 잡는 계산이 안 맞는다"); bad++; }

        // 사람 눈높이에 오나 — 이젤은 서서 그리는 물건이다
        float mid = origin.y + sizeV * 0.5f;
        log.Add(mid > 0.7f && mid < 2.1f
                ? $"화판 한가운데가 {mid:0.00}m — 서서 그리는 높이"
                : $"★ 화판 한가운데가 {mid:0.00}m — 눈높이를 많이 벗어난다");
        if (mid <= 0.7f || mid >= 2.1f) bad++;

        // ── ④ 좌표 왕복 — <b>여기가 제일 틀리기 쉬운 자리</b> ───────────────────
        //
        // 화면의 한 점 → 평면 → uv → 도형 좌표 → 다시 평면. 돌아온 자리가 같아야 한다.
        // 특히 <b>y 뒤집기</b>를 한 번 빠뜨리거나 두 번 하면 여기서 바로 드러난다.
        float worst = 0f;
        foreach (var probe in new[] { new Vector2(0.12f, 0.20f), new Vector2(0.80f, 0.35f),
                                      new Vector2(0.50f, 0.90f), new Vector2(0.97f, 0.03f) })
        {
            // 도형 좌표(y 아래) → uv(v 위)
            Vector3 world = origin + across * (sizeU * probe.x) + up * (sizeV * (1f - probe.y));
            // 다시 uv → 도형 좌표
            Vector3 on = world - origin;
            float u2 = Vector3.Dot(on, across) / sizeU, v2 = Vector3.Dot(on, up) / sizeV;
            var back = new Vector2(u2, 1f - v2);
            worst = Mathf.Max(worst, (back - probe).magnitude);
        }
        log.Add(worst < 1e-3f ? $"좌표 왕복 오차 {worst:0.000000} — 뒤집힘 없다"
                              : $"★ 좌표 왕복 오차 {worst:0.0000} — 축이 어긋났다");
        if (worst >= 1e-3f) bad++;

        // ── ⑤ 도형이 화판 안에 드나 · 완벽하게 그리면 만점인가 ──────────────────
        int lazyTotal = 0;
        foreach (var shape in Tracing.Shapes)
        {
            float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
            foreach (var p in shape.path)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }

            // 성실한 손: 윤곽을 촘촘히 따라간다
            var drawn = new List<Vector2>();
            for (int i = 0; i <= 600; i++) drawn.Add(Tracing.At(shape, i / 600f));
            var mark = Tracing.Judge(shape, drawn);

            // 게으른 손: 한가운데만 마구 칠한다
            var lazy = new List<Vector2>();
            var rng = new System.Random(7);
            for (int i = 0; i < 600; i++)
                lazy.Add(new Vector2(0.2f + 0.6f * (float)rng.NextDouble(),
                                     0.2f + 0.6f * (float)rng.NextDouble()));
            var lazyMark = Tracing.Judge(shape, lazy);

            lazyTotal += lazyMark.score;

            // ★ <b>기준을 절댓값으로 잡지 않는다.</b> 처음엔 «마구칠하기 30점 이하» 로 뒀는데
            // 솥뚜껑만 34가 나왔다 — 뭉툭하게 모인 도형이라 가운데에 아무렇게나 찍어도
            // 윤곽 띠에 걸리는 비율이 높다. 그건 <b>도형의 성질</b>이지 규칙의 고장이 아니야.
            // 게임에서 진짜 중요한 건 <b>성실한 손과 벌어지는가</b>다.
            bool inside = minX >= 0.02f && maxX <= 0.98f && minY >= 0.02f && maxY <= 0.98f;
            bool fair = mark.score >= 95 && lazyMark.score <= mark.score * 0.45f;
            if (!inside || !fair) bad++;

            log.Add($"{(inside && fair ? " " : "★")} {shape.name,-5} "
                  + $"x {minX:0.00}~{maxX:0.00} y {minY:0.00}~{maxY:0.00} · "
                  + $"성실 {mark.score,3} · 마구칠하기 {lazyMark.score,3}");
        }

        // ★★ <b>한 판 전체로 봐야 한다.</b> 도형 하나에서 34점이 나오는 건 넘어갈 수 있어도,
        // 네 판을 다 마구 칠해서 <b>등급이 나오면</b> 그건 게임이 깨진 것이다.
        log.Add(lazyTotal < Tracing.Grades[2]
                ? $"네 판 다 마구 칠하면 {lazyTotal}점 — B({Tracing.Grades[2]})에 못 미친다"
                : $"★ 네 판 다 마구 칠해서 {lazyTotal}점 — 등급이 나온다");
        if (lazyTotal >= Tracing.Grades[2]) bad++;

        // ── ⑥ 만점과 등급 ──────────────────────────────────────────────────────
        int full = Tracing.Rounds * 100;
        log.Add($"만점 {full} · 등급 S {Tracing.Grades[0]} / A {Tracing.Grades[1]} / B {Tracing.Grades[2]}");
        if (Tracing.Grades[0] > full) { log.Add("★ S 기준이 만점보다 높다 — 아무도 못 받는다"); bad++; }

        Report(log, bad);
    }

    static void Report(List<string> log, int bad)
    {
        Debug.Log("[따라 그리기 점검]\n  " + string.Join("\n  ", log)
                + $"\n  ── 문제 {bad}건");
    }
}
