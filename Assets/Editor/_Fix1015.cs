using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 2026-10-15 — ① 런타임에 <c>Shader.Find</c> 로 찾는 셰이더를 <b>빌드에 넣는다</b>
/// ② 크레인 프리팹 계층을 찍어서 <c>Boom</c>/<c>Ball</c> 을 왜 못 움직이는지 본다.
/// </summary>
public static class _Fix1015
{
    /// <summary>
    /// ★★★ <b>빌드에서만 핑크가 되던 이유.</b>
    ///
    /// 제작자: *"유니티에서는 캐릭터가 잘 나오는데 빌드판은 핑크 네모에 알약이다."*
    ///
    /// <c>Shader.Find</c> 는 <b>빌드에 들어간 셰이더만</b> 찾는다. 셰이더는 씬이나
    /// Resources 의 머티리얼이 쓸 때만 따라 들어가는데, <see cref="StandStandee"/> 는
    /// 머티리얼을 <b>코드로</b> 만든다 — 그래서 아무도 «쓴다» 고 표시해 주지 않는다.
    /// 에디터에서는 프로젝트 안의 모든 셰이더가 다 있으니 멀쩡하고,
    /// <b>빌드에서만</b> null 이 되어 <c>new Material(null)</c> → <b>핑크</b>.
    ///
    /// Graphics Settings 의 <b>Always Included Shaders</b> 에 올리면 무조건 들어간다.
    /// </summary>
    static readonly string[] Needed =
    {
        "Universal Render Pipeline/Unlit",             // StandStandee · TracingBoard
        "Universal Render Pipeline/Lit",               // RaceProps · Surfaces
        "Universal Render Pipeline/Particles/Unlit",   // SmokePuff
        "Racing/PlaqueText",                           // BuildingSign (현판 글씨)
        "Sprites/Default",                             // SmokePuff 대체
    };

    public static void Run()
    {
        var sb = new StringBuilder();

        // ── ① 셰이더 ────────────────────────────────────────────────────
        var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
        var so = new SerializedObject(gs);
        var list = so.FindProperty("m_AlwaysIncludedShaders");

        sb.AppendLine($"지금 등록된 셰이더 {list.arraySize}개");
        for (int i = 0; i < list.arraySize; i++)
        {
            var s = list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            sb.AppendLine($"   {(s == null ? "(빈 칸)" : s.name)}");
        }

        int added = 0;
        foreach (string name in Needed)
        {
            var shader = Shader.Find(name);
            if (shader == null) { sb.AppendLine($"★ 프로젝트에 «{name}» 가 없다"); continue; }

            bool already = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) { already = true; break; }
            if (already) { sb.AppendLine($"   이미 있음: {name}"); continue; }

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            sb.AppendLine($"   ＋ 넣음: {name}");
            added++;
        }

        if (added > 0)
        {
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
        sb.AppendLine($"→ {added}개 추가, 이제 {list.arraySize}개");
        sb.AppendLine();

        // ── ② 크레인 계층 ───────────────────────────────────────────────
        var prefab = Resources.Load<GameObject>("RaceProps/R03_Demolition_Crane");
        if (prefab == null) sb.AppendLine("크레인 프리팹 없음");
        else
        {
            var go = Object.Instantiate(prefab);
            sb.AppendLine("크레인 계층:");
            Walk(go.transform, "", sb);

            var boom = Deep(go.transform, "Boom");
            var ball = Deep(go.transform, "Ball");
            sb.AppendLine();
            sb.AppendLine($"Deep(\"Boom\") = {(boom == null ? "★ 못 찾음" : Path(boom, go.transform))}");
            sb.AppendLine($"Deep(\"Ball\") = {(ball == null ? "★ 못 찾음" : Path(ball, go.transform))}");
            if (ball != null)
            {
                sb.AppendLine($"   Ball 의 부모 = {ball.parent.name}   (루트의 직계인가: {ball.parent == go.transform})");
                sb.AppendLine($"   Ball localPosition {ball.localPosition}");
                sb.AppendLine($"   Ball world       {ball.position}");
                sb.AppendLine($"   루트 TransformPoint(localPosition) = {go.transform.TransformPoint(ball.localPosition)}");
                sb.AppendLine("   ↑ 이 둘이 다르면 WreckingBall 이 공을 엉뚱한 데로 보낸다");
            }
            Object.DestroyImmediate(go);
        }

        System.IO.File.WriteAllText(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fix1015.txt"), sb.ToString());
        Debug.Log(sb.ToString());
        EditorApplication.Exit(0);
    }

    static void Walk(Transform t, string pad, StringBuilder sb)
    {
        sb.AppendLine($"   {pad}{t.name}   (자식 {t.childCount})");
        if (pad.Length > 10) return;
        foreach (Transform c in t) Walk(c, pad + "  ", sb);
    }

    static Transform Deep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root) { var r = Deep(c, name); if (r != null) return r; }
        return null;
    }

    static string Path(Transform t, Transform root)
    {
        string s = t.name;
        for (var p = t.parent; p != null && p != root; p = p.parent) s = p.name + "/" + s;
        return s;
    }
}
