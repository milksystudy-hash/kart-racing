using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 로비(중앙홀)에서 <b>동상을 세울 자리</b>를 찾는다 — 좌표를 손으로 찍지 않는다.
/// 로비 곰에서 손으로 찍었다가 두 번 틀렸다(2026-09-16).
/// </summary>
public static class _LobbySpot
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var sb = new StringBuilder();

        // 바닥·천장·벽은 빼고, 홀 안에 실제로 서 있는 물건만 모은다
        var blobs = new List<(string name, Bounds b)>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var b = r.bounds;
            if (b.max.y < 0.35f) continue;               // 바닥 장식
            if (b.min.y > 2.6f) continue;                // 천장·들보
            if (b.size.magnitude < 0.3f) continue;       // 실오라기
            if (b.size.x > 25f || b.size.z > 25f) continue;   // 벽 통짜
            var root = r.transform;
            while (root.parent != null) root = root.parent;
            blobs.Add((root.name, b));
        }
        sb.AppendLine($"홀 안 물건 {blobs.Count}개");

        // 2m 격자로 훑어 «제일 널널한 칸» 을 찾는다
        var cands = new List<(Vector3 at, float clear, float camDot)>();
        var pivot = new Vector3(0f, 1.4f, 0f);
        for (float x = -14f; x <= 14.001f; x += 1f)
            for (float z = -13f; z <= 13.001f; z += 1f)
            {
                var at = new Vector3(x, 0f, z);
                float clear = 99f;
                foreach (var (_, b) in blobs)
                {
                    var p = b.ClosestPoint(new Vector3(x, 1.2f, z));
                    clear = Mathf.Min(clear, Vector2.Distance(new Vector2(x, z), new Vector2(p.x, p.z)));
                }
                if (clear < 2.6f) continue;
                cands.Add((at, clear, Vector3.Distance(at, pivot)));
            }
        cands.Sort((a, b) => b.clear.CompareTo(a.clear));
        sb.AppendLine($"후보 {cands.Count}개 — 널널한 순 상위 12");
        for (int i = 0; i < Mathf.Min(12, cands.Count); i++)
            sb.AppendLine($"   ({cands[i].at.x,5:0.0}, {cands[i].at.z,5:0.0})  여유 {cands[i].clear:0.00}m · 홀 가운데서 {cands[i].camDot:0.0}m");

        // ★ 널널한 것만으로는 부족하다 — <b>궤도 카메라가 받침대를 보는 길</b>을 막으면 안 된다.
        //   캐릭터를 고르는 게 로비의 본업이라, 동상이 그 앞을 가리면 자리를 잘못 잡은 것이다.
        var stands = new List<Vector3>();
        foreach (var cs in Object.FindObjectsByType<CharacterStand>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            stands.Add(cs.transform.position);
        sb.AppendLine($"받침대 {stands.Count}개");
        foreach (var p in stands) sb.AppendLine($"   ({p.x,6:0.0},{p.z,6:0.0})");

        sb.AppendLine("자리 점수 — 여유 3.4m 이상 · 가운데서 5~13m · 받침대 가리는 각도가 적은 순");
        var scored = new List<(Vector3 at, float clear, float d, int blocked)>();
        foreach (var (at, clear, d) in cands)
        {
            if (clear < 3.4f || d < 5f || d > 13f) continue;
            int blocked = 0;
            for (int a = 0; a < 36; a++)
            {
                float rad = a * 10f * Mathf.Deg2Rad;
                var cam = pivot + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * 11f;
                foreach (var st in stands)
                {
                    // 카메라→받침대 선이 동상 기둥(반경 1.3m)을 지나가나
                    Vector2 c = new Vector2(cam.x, cam.z), t = new Vector2(st.x, st.z), q = new Vector2(at.x, at.z);
                    Vector2 dir = t - c;
                    float len = dir.magnitude;
                    if (len < 0.01f) continue;
                    float u = Mathf.Clamp01(Vector2.Dot(q - c, dir) / (len * len));
                    if (Vector2.Distance(c + dir * u, q) < 1.3f) { blocked++; break; }
                }
            }
            scored.Add((at, clear, d, blocked));
        }
        scored.Sort((x, y) => x.blocked != y.blocked ? x.blocked.CompareTo(y.blocked) : y.clear.CompareTo(x.clear));
        for (int i = 0; i < Mathf.Min(10, scored.Count); i++)
            sb.AppendLine($"   ({scored[i].at.x,5:0.0}, {scored[i].at.z,5:0.0})  여유 {scored[i].clear:0.00}m · 가운데서 {scored[i].d:0.0}m · 가리는 각도 {scored[i].blocked}/36");

        // 문·받침대·출발문이 어디 있는지도 같이 — 자리를 고를 때 읽으려고
        sb.AppendLine("\n주요 물건 자리");
        var seen = new HashSet<string>();
        foreach (var (n, b) in blobs)
        {
            if (!seen.Add(n)) continue;
            sb.AppendLine($"   {n,-22} 중심 ({b.center.x,6:0.0},{b.center.z,6:0.0}) 크기 {b.size.x:0.0}x{b.size.y:0.0}x{b.size.z:0.0}");
        }
        Debug.Log("[로비 자리]\n" + sb);
    }
}
