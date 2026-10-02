using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 캠퍼스를 다시 굽고 — <b>포스터가 코스 쪽 면에 제대로 서 있는지</b>와
/// <b>동상이 캠퍼스 안쪽을 보는지</b>를 렌더해서 확인한다.
/// 「코드는 도는데 화면은 그대로」를 여섯 번 겪은 뒤의 기본 절차야.
/// </summary>
public static class _AdCheck
{
    const string Out = "AdCheck";   // 프로젝트 밖 스크래치로 -out 인자를 받는다

    public static void Run()
    {
        string dir = Arg("-out") ?? Path.Combine(Path.GetTempPath(), Out);
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();

        StatueImport.Ensure(true);

        // ★ 입간판은 «광고판 임무를 이미 깼으면» 안 선다(설정상 이미 부쉈으니까).
        //   그래서 검사하는 동안만 수집 기록을 비우고 <b>반드시 되돌린다</b> —
        //   진짜 프로젝트에서 도는 검사라 유저 기록을 날리면 안 된다(2026-09-17).
        const string CollectedKey = "Racing.Collected";
        bool hadCollected = PlayerPrefs.HasKey(CollectedKey);
        string savedCollected = PlayerPrefs.GetString(CollectedKey, "");
        CollectionState.ClearAll();

        CampusSceneBuilder.BuildCampus();
        EditorSceneManager.OpenScene("Assets/Scenes/Campus.unity");

        // ── 큰 광고판 ─────────────────────────────────────────────────
        var boards = GameObject.Find("AdBoards");
        if (boards == null) { Debug.LogError("[광고] AdBoards 가 없다"); return; }

        int shot = 0;
        foreach (Transform ad in boards.transform)
        {
            if (!ad.name.StartsWith("Ad_")) continue;
            var poster = ad.Find("Poster");
            var panel = ad.Find("Panel");
            if (poster == null || panel == null) continue;

            // 포스터가 판의 «코스 쪽» 에 있나 — 판 중심에서 포스터 중심으로 가는 방향이
            // 간판 앞면(+z)과 같아야 한다
            Vector3 d = poster.position - panel.position;
            float side = Vector3.Dot(d.normalized, ad.forward);
            sb.AppendLine($"{ad.name}: 포스터가 판 앞면 쪽인가 {side:0.00} (1 이어야 맞다)");

            if (shot++ == 0)
            {
                // 코스 쪽에서 본다 — 간판 앞면 앞 14m
                Snap(Path.Combine(dir, "ad_big.png"),
                     poster.position + ad.forward * 14f - Vector3.up * 1.0f,
                     Quaternion.LookRotation(-ad.forward, Vector3.up), 40f);
            }
        }

        // ── 입간판 ────────────────────────────────────────────────────
        var signs = GameObject.Find("AdSigns");
        if (signs == null) sb.AppendLine("AdSigns 가 없다 !!");
        else
        {
            int k = 0;
            foreach (Transform sg in signs.transform)
            {
                var poster = Find(sg, "Poster");
                var panel = Find(sg, "Panel");
                if (poster == null || panel == null) continue;
                float side = Vector3.Dot((poster.position - panel.position).normalized, poster.forward);
                sb.AppendLine($"{sg.name}: 포스터가 판 앞면 쪽인가 {side:0.00} (1 이어야 맞다)");
                if (k++ == 0)
                    Snap(Path.Combine(dir, "ad_small.png"),
                         poster.position + poster.forward * 3.4f,
                         Quaternion.LookRotation(-poster.forward, Vector3.up), 44f, 560, 560);
            }
        }

        // ── 동상 (런타임에 서니 여기서는 직접 세워 본다) ────────────────
        foreach (var name in StatueImport.Files)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{StatueImport.Dir}/{name}.fbx");
            if (src == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            go.transform.position = new Vector3(0f, 0f, 400f);   // 캠퍼스 밖 빈 자리
            // CampusStatue 와 같은 각도로 세운다
            go.transform.rotation = Quaternion.Euler(0f, 0f, 0f) * go.transform.rotation;

            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

            // 보는 사람은 +Z 쪽에 선다(캠퍼스 안쪽). 앞면이면 얼굴이 보인다.
            Snap(Path.Combine(dir, $"statue_{name}.png"),
                 b.center + new Vector3(0f, 0.1f, 2.4f),
                 Quaternion.LookRotation(Vector3.back, Vector3.up), 34f, 420, 620);
            Object.DestroyImmediate(go);
        }

        if (hadCollected) PlayerPrefs.SetString(CollectedKey, savedCollected);
        else PlayerPrefs.DeleteKey(CollectedKey);
        PlayerPrefs.Save();
        sb.AppendLine("수집 기록 되돌림: " + savedCollected);

        Debug.Log("[광고·동상 점검]\n" + sb + "\n렌더 → " + dir);
    }

    static Transform Find(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static void Snap(string file, Vector3 at, Quaternion rot, float fov, int w = 760, int h = 420)
    {
        var camGo = new GameObject("_Snap");
        var cam = camGo.AddComponent<Camera>();
        camGo.transform.SetPositionAndRotation(at, rot);
        cam.fieldOfView = fov;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.14f, 0.14f, 0.16f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 600f;

        DynamicGI.UpdateEnvironment();
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    static string Arg(string key)
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == key) return a[i + 1];
        return null;
    }
}
