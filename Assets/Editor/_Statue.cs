using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 동상 FBX 점검 — <b>앞면이 어디인지 재서</b> <c>CampusStatue.FacingYaw</c> 에 넣을 값을 뱉는다.
/// 이 프로젝트에서 «블렌더에서 이렇게 돌렸으니 유니티도 이럴 것» 이 두 번 틀렸다.
/// </summary>
public static class _Statue
{
    // 개발용 — 메뉴에는 안 올린다(배치모드로 부른다)
    public static void Run()
    {
        StatueImport.Ensure(true);
        var sb = new StringBuilder();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        foreach (var name in StatueImport.Files)
        {
            string path = $"{StatueImport.Dir}/{name}.fbx";
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (src == null) { sb.AppendLine($"{name}: 에셋 없음"); continue; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            // ★ 회전을 덮어쓰지 않는다 — FBX 축 변환이 루트 회전에 들어 있어서 identity 로
            //   덮으면 동상이 눕는다(실제로 한 번 그렇게 재서 틀렸다).
            go.transform.position = Vector3.zero;

            var rs = go.GetComponentsInChildren<MeshRenderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

            // 텍스처가 물렸나 — 안 물리면 동상이 새하얗게 나온다
            int tex = 0;
            foreach (var r in rs)
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) tex++;

            // ★ 위로 뻗은 주먹이 앞면 표식이다. 위쪽 22% 에서 <b>수평으로 제일 멀리 나간 점</b>을 찾는다.
            //   몸통은 축 둘레로 고른데 주먹만 한쪽으로 튀어나와 있다.
            float cut = b.max.y - b.size.y * 0.22f;
            Vector3 axis = new Vector3(b.center.x, 0f, b.center.z);
            Vector2 far = Vector2.zero;
            float best = 0f;
            int counted = 0;
            foreach (var r in rs)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var verts = mf.sharedMesh.vertices;      // 에디터에서는 Read/Write 없이도 읽힌다
                var l2w = r.transform.localToWorldMatrix;
                foreach (var v in verts)
                {
                    Vector3 w = l2w.MultiplyPoint3x4(v);
                    if (w.y < cut) continue;
                    counted++;
                    var flat = new Vector2(w.x - axis.x, w.z - axis.z);
                    if (flat.magnitude > best) { best = flat.magnitude; far = flat; }
                }
            }

            float ang = Mathf.Atan2(far.x, far.y) * Mathf.Rad2Deg;   // +Z 기준 시계방향
            float yaw = Mathf.Repeat(-ang, 360f);                    // 이만큼 돌리면 그 점이 +Z 로 온다

            // ★ 두 번째 표식 — 기단의 <b>명패</b>. 육각 받침에 판 하나가 앞면에만 튀어나와 있다.
            //   각도별 최대 반경을 찍어 보면 육각 꼭짓점 여섯 개 위로 <b>한 칸만 더 튀어나온다.</b>
            float plinth = b.min.y + b.size.y * 0.10f;
            var bins = new float[24];
            foreach (var r in rs)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var l2w = r.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    Vector3 w = l2w.MultiplyPoint3x4(v);
                    if (w.y > plinth) continue;
                    var flat = new Vector2(w.x - axis.x, w.z - axis.z);
                    int k = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(flat.x, flat.y) * Mathf.Rad2Deg, 360f) / 15f), 0, 23);
                    if (flat.magnitude > bins[k]) bins[k] = flat.magnitude;
                }
            }
            int top = 0;
            for (int k = 1; k < 24; k++) if (bins[k] > bins[top]) top = k;
            var prof = new StringBuilder();
            for (int k = 0; k < 24; k++) prof.Append($"{k * 15,4}:{bins[k]:0.00}{(k == top ? "*" : " ")}");

            sb.AppendLine($"── {name}");
            sb.AppendLine($"   크기 {b.size.x:0.000} x {b.size.y:0.000} x {b.size.z:0.000} · 바닥 y {b.min.y:0.000}");
            sb.AppendLine($"   렌더러 {rs.Length} · _BaseMap 물린 재질 {tex}");
            foreach (var r in rs)
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    sb.AppendLine($"   재질 '{m.name}' 셰이더 {m.shader.name}");
                    foreach (var prop in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap" })
                        if (m.HasProperty(prop))
                        {
                            var t = m.GetTexture(prop);
                            sb.AppendLine($"     {prop,-18} {(t == null ? "없음" : $"{AssetDatabase.GetAssetPath(t)} {t.width}x{t.height}")}");
                        }
                    sb.AppendLine($"     _BaseColor {(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-")} · 키워드 {string.Join(",", m.shaderKeywords)}");
                }
            sb.AppendLine($"   위쪽 22% 정점 {counted} · 제일 먼 점 ({far.x:0.000}, {far.y:0.000}) 거리 {best:0.000}");
            sb.AppendLine($"   그 점이 향하는 각 {ang:0.0}°  →  <b>주먹 기준 FacingYaw = {yaw:0.0}</b>");
            sb.AppendLine("   기단 각도별 최대 반경 (제일 큰 칸 *)");
            sb.AppendLine("     " + prof);
            sb.AppendLine($"   명패 방향 {top * 15}°  →  <b>명패 기준 FacingYaw = {Mathf.Repeat(-top * 15f, 360f):0.0}</b>");
            Object.DestroyImmediate(go);
        }
        Debug.Log("[동상 앞면]\n" + sb);
    }

    /// <summary>동상을 정면에서 한 장 찍는다 — <b>텍스처가 제대로 입혔나</b>를 눈으로 본다.</summary>
    public static void Shot()
    {
        StatueImport.Ensure(false);
        string dir = null;
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-out") dir = a[i + 1];
        dir ??= System.IO.Path.GetTempPath();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.25f;
        sun.transform.rotation = Quaternion.Euler(42f, 150f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);

        foreach (var name in StatueImport.Files)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{StatueImport.Dir}/{name}.fbx");
            if (src == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            go.transform.position = Vector3.zero;
            // 게임에서 서는 각도 그대로 — 회전을 덮어쓰지 않고 <b>앞에 곱한다</b>
            go.transform.rotation = Quaternion.Euler(0f, CampusStatue.FacingYaw, 0f) * go.transform.rotation;

            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

            var cam = new GameObject("_Cam").AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(b.center + new Vector3(0f, 0.05f, 1.5f),
                                                 Quaternion.LookRotation(Vector3.back, Vector3.up));
            cam.fieldOfView = 42f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.16f, 0.18f);
            cam.nearClipPlane = 0.02f;

            DynamicGI.UpdateEnvironment();
            var rt = new RenderTexture(440, 640, 24) { antiAliasing = 2 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(440, 640, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 440, 640), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"shot_{name}.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }
        Debug.Log("[동상 렌더] → " + dir);
    }
}
