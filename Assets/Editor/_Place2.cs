using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 배치 자가점검 — 캠퍼스를 다시 굽고, <b>관마다 소품이 제 방 안에 제대로 섰는지</b> 잰다.
/// 배치모드 전용, 메뉴 없음.
///
/// ★ 재는 것은 넷이다: 방 밖으로 나갔나 · 서로 겹치나 · 문 앞을 막았나 · 콜라이더가 붙었나.
///   <b>전부 부모(건물) 좌표로 잰다</b> — 건물이 yaw 로 돌아가 있어서 월드 AABB 는 거짓말한다.
/// </summary>
public static class _Place2
{
    static string Out => System.Environment.GetEnvironmentVariable("PLACE_OUT") ?? "C:/temp/place.txt";

    /// <summary>
    /// 소품 이름 규칙. <b>관마다 다르다</b> — 이야기 소품은 <c>In_</c>, 곰밥마당은
    /// <c>InPot_</c>(배식 팬)과 <c>Ex</c>(전시·설비)다. 하나만 보면 한 관이 통째로 빠진다.
    /// </summary>
    static bool IsProp(string n) =>
        n.StartsWith("In_") || n.StartsWith("InPot_") || n.StartsWith("Ex");

    public static void Run()
    {
        var sb = new StringBuilder();
        if (System.Environment.GetEnvironmentVariable("PLACE_BUILD") == "1")
            CampusSceneBuilder.BuildCampus();
        else
            EditorSceneManager.OpenScene("Assets/Scenes/Campus.unity");

        // ★ 소품에서 거꾸로 올라가 방을 찾는다. 현판이 어디에 매달려 있든 안 흔들린다.
        var halls = new List<Transform>();
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
            if (IsProp(tr.name) && tr.parent != null && !halls.Contains(tr.parent))
                halls.Add(tr.parent);

        int totalProps = 0, bad = 0;
        foreach (var t in halls)
        {
            var props = new List<Transform>();
            foreach (Transform c in t) if (IsProp(c.name)) props.Add(c);
            if (props.Count == 0) continue;
            totalProps += props.Count;

            // 방 크기 — 벽(Wall_*)에서 읽는다
            // ★ <b>바닥에서만 잰다.</b> 벽(Wall_*)을 같이 재면 <b>처마</b>가 섞여서 방이 실제보다
            //   크게 잡히고, 「방 밖」 검사가 통째로 거짓이 된다 — 한 번 그렇게 재서 멀쩡한
            //   집무책상이 ★방밖 으로 찍혔다.
            float halfW = 0f, halfD = 0f;
            foreach (Transform c in t)
            {
                if (c.name != "InFloor") continue;
                var mf = c.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var sz = Vector3.Scale(mf.sharedMesh.bounds.size, c.localScale);
                halfW = Mathf.Max(halfW, Mathf.Abs(sz.x) * 0.5f);
                halfD = Mathf.Max(halfD, Mathf.Abs(sz.z) * 0.5f);
            }
            if (halfW < 1f) { sb.AppendLine($"## {t.name} — 바닥을 못 찾아 건너뜀"); continue; }
            // InFloor 는 이미 «안쪽» 크기다 — 벽 두께를 또 빼면 안 된다(한 번 빼서 71건이 거짓으로 찍혔다).

            sb.AppendLine($"## {t.name}  방 {halfW * 2:0.0} x {halfD * 2:0.0}  소품 {props.Count}");

            var boxes = new List<(string n, Vector2 lo, Vector2 hi, float y0, float y1)>();
            foreach (var p in props)
            {
                Vector3 lo = Vector3.one * 1e9f, hi = -Vector3.one * 1e9f;
                int cols = 0;
                foreach (var r in p.GetComponentsInChildren<Renderer>(true))
                {
                    // 부모 좌표로 재야 회전한 건물에서 거짓말을 안 한다
                    var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                    var b = mf.sharedMesh.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var v = new Vector3(((i & 1) == 0 ? b.min : b.max).x,
                                            ((i & 2) == 0 ? b.min : b.max).y,
                                            ((i & 4) == 0 ? b.min : b.max).z);
                        var w = t.InverseTransformPoint(r.transform.TransformPoint(v));
                        lo = Vector3.Min(lo, w); hi = Vector3.Max(hi, w);
                    }
                }
                cols = p.GetComponentsInChildren<Collider>(true).Length;
                if (lo.x > 1e8f) continue;

                string flag = "";
                if (lo.x < -halfW - 0.15f || hi.x > halfW + 0.15f ||
                    lo.z < -halfD - 0.15f || hi.z > halfD + 0.15f) { flag += " ★방밖"; bad++; }
                // 문 앞 — 정면(+Z) 가운데 폭 ±2.2, 안쪽 2.6m
                if (hi.x > -2.2f && lo.x < 2.2f && hi.z > halfD - 2.6f) { flag += " ★문앞"; bad++; }
                if (cols > 0) { flag += $" ★콜라이더{cols}"; bad++; }

                boxes.Add((p.name, new Vector2(lo.x, lo.z), new Vector2(hi.x, hi.z), lo.y, hi.y));
                sb.AppendLine($"   {p.name,-22} x {lo.x,6:0.00}..{hi.x,6:0.00}  z {lo.z,6:0.00}..{hi.z,6:0.00}" +
                              $"  y {lo.y,5:0.00}..{hi.y,5:0.00}{flag}");
            }

            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    var a = boxes[i]; var b = boxes[j];
                    float ox = Mathf.Min(a.hi.x, b.hi.x) - Mathf.Max(a.lo.x, b.lo.x);
                    float oz = Mathf.Min(a.hi.y, b.hi.y) - Mathf.Max(a.lo.y, b.lo.y);
                    // ★ <b>위아래로 떨어져 있으면 겹침이 아니라 «얹은 것»</b> 이다.
                    //   웅성관의 증거 셋은 책상 위에 놓여서, xz 만 보면 셋 다 겹친 걸로 나온다.
                    // <b>바닥에서 시작하는 높이가 다르면</b> 하나가 다른 하나 «위에» 있는 것이다.
                    // 바운즈 꼭대기로 재면 안 된다 — 책상 꼭대기는 모니터(1.65)라 그 위에 얹은
                    // 테이프(0.95)보다 높아서, 얹은 것이 영영 «겹침» 으로 찍힌다.
                    bool stacked = Mathf.Abs(a.y0 - b.y0) > 0.3f;
                    if (!stacked && ox > 0.12f && oz > 0.12f)
                    { sb.AppendLine($"   ★겹침 {a.n} ↔ {b.n}  {ox:0.00} x {oz:0.00}m"); bad++; }
                }
        }

        sb.AppendLine($"\n관 {halls.Count} · 소품 {totalProps} · 문제 {bad}");
        File.WriteAllText(Out, sb.ToString());
        Debug.Log(sb.ToString());
    }
}
