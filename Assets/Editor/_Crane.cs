using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 크레인 네 대가 <b>땅이나 건물에 박혀 있는지</b> 잰다. 고치지 않는다 — 숫자만 뱉는다.
/// 2026-10-15 수연: *"포크레인이 다 땅이나 벽에 박혀 있다."*
///
/// 구운 Campus 씬에서 잰다 — 건물·담장·길이 전부 들어 있는 유일한 씬이라
/// <b>실제로 그 자리에 뭐가 있는지</b>를 볼 수 있다(Track 은 Awake 에 지어져서 비어 있다).
/// </summary>
public static class _Crane
{
    // RaceProps 의 호출과 같은 값이어야 한다.
    static readonly (float t, float offset, string name)[] Spots =
    {
        (0.17f,  29.0f, "회전(먼 쪽)"),
        (0.63f, -13.0f, "붕괴(먼 쪽)"),
        (0.40f,  13f,   "길막(길 옆)"),
        (0.84f, -13.5f, "거칠게(길 옆)"),
    };

    public static void Run()
    {
        var sb = new StringBuilder();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Campus.unity", OpenSceneMode.Single);
        sb.AppendLine($"씬: {scene.name} · 루트 {scene.rootCount}");

        var track = Object.FindFirstObjectByType<TrackBuilder>();
        if (track == null) { Dump(sb.AppendLine("TrackBuilder 를 못 찾았다")); return; }

        // 크레인 크기를 잰다
        var prefab = Resources.Load<GameObject>("RaceProps/R03_Demolition_Crane");
        if (prefab == null) { Dump(sb.AppendLine("크레인 프리팹을 못 찾았다")); return; }

        var probe = Object.Instantiate(prefab);
        probe.transform.position = Vector3.zero;
        probe.transform.rotation = Quaternion.identity;
        var rs = probe.GetComponentsInChildren<Renderer>();
        Bounds local = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) local.Encapsulate(rs[i].bounds);
        Vector3 size = local.size;
        float bottom = -local.min.y;                       // 원점에서 맨 밑까지
        foreach (var r in rs)
            sb.AppendLine($"   부품 {r.name,-16} {r.bounds.size.x:F2} × {r.bounds.size.z:F2} × {r.bounds.size.y:F2}  밑 {r.bounds.min.y:F2}");
        Object.DestroyImmediate(probe);

        sb.AppendLine($"크레인 전체 {size.x:F2} × {size.z:F2} × {size.y:F2} m · 원점→바닥 {bottom:F2}");
        sb.AppendLine();

        // 씬의 모든 렌더러 바운즈 (바닥·얇은 것 빼고)
        var solids = new List<(Bounds b, string name)>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            var b = r.bounds;
            if (b.size.y < 1.5f) continue;                 // 바닥 · 띠 · 줄눈
            if (b.size.x > 150f || b.size.z > 150f) continue;  // 담장 통짜 · 지붕
            solids.Add((b, Path(r.transform)));
        }
        sb.AppendLine($"검사 대상 렌더러 {solids.Count}개");
        sb.AppendLine();

        foreach (var (t, offset, name) in Spots)
        {
            Vector3 fwd = track.TangentOnPath(t);
            Vector3 side = Vector3.Cross(Vector3.up, fwd);
            Vector3 at = track.transform.position + track.PointOnPath(t) + side * offset;

            sb.AppendLine($"── {name}   t={t}  offset={offset}");
            sb.AppendLine($"   코스 위 좌표 ({at.x:F1}, {at.y:F1}, {at.z:F1})");

            // 바닥
            if (Physics.Raycast(at + Vector3.up * 40f, Vector3.down, out var hit, 120f, ~(1 << 2)))
                sb.AppendLine($"   바닥 y {hit.point.y:F2}  ({Path(hit.transform)})   ← 코스 y 와 차이 {hit.point.y - at.y:+0.00;-0.00}");
            else
                sb.AppendLine("   ★ 바닥을 못 찾았다 — 허공이다");

            // 겹침
            const float half = 4.81f;     // 본체 6.00 x 7.52 의 반대각선 — 돌려세워도 안 틀린다
            var box = new Bounds(new Vector3(at.x, at.y + size.y * 0.5f, at.z),
                                 new Vector3(half * 2f, size.y, half * 2f));
            int n = 0;
            foreach (var (b, who) in solids)
            {
                if (!b.Intersects(box)) continue;
                sb.AppendLine($"   ★ 겹침: {who}  (크기 {b.size.x:F1}×{b.size.z:F1}×{b.size.y:F1})");
                if (++n >= 6) { sb.AppendLine("   ..."); break; }
            }
            if (n == 0) sb.AppendLine($"   겹침 없음 (반경 {half:F1} m 기준)");

            // 얼마나 밀면 비는지
            var tried = new List<float>();
            for (int step = 1; step <= 36 && n > 0; step++)
            {
                // 바깥으로 · 안쪽으로 번갈아 — 가까운 자리를 먼저 찾는다
                float d = (step + 1) / 2 * 1.5f;
                float trial = offset + Mathf.Sign(offset) * (step % 2 == 1 ? d : -d);
                if (Mathf.Abs(trial) < 10f || Mathf.Abs(trial) > 52f) continue;
                tried.Add(trial);
                Vector3 p = track.transform.position + track.PointOnPath(t) + side * trial;
                var tryBox = new Bounds(new Vector3(p.x, p.y + size.y * 0.5f, p.z), box.size);
                bool clear = true;
                foreach (var (b, _) in solids) if (b.Intersects(tryBox)) { clear = false; break; }
                if (clear) { sb.AppendLine($"   → ★ offset {trial:F1} 로 옮기면 빈다 (지금 {offset})"); break; }
                if (step == 36) sb.AppendLine($"   → 10~52m 어디에도 빈 자리가 없다 ({tried.Count}군데 봤다)");
            }
            sb.AppendLine();
        }

        Dump(sb);
    }

    static string Path(Transform t)
    {
        var s = t.name;
        for (var p = t.parent; p != null; p = p.parent) s = p.name + "/" + s;
        return s;
    }

    static void Dump(StringBuilder sb)
    {
        System.IO.File.WriteAllText(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "crane.txt"), sb.ToString());
        Debug.Log(sb.ToString());
        EditorApplication.Exit(0);
    }
}
