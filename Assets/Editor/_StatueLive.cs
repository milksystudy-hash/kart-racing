using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 동상 <b>플레이 모드</b> 점검. 「에디터에서 Start 를 직접 불러 보는 검사」는 배선만 본다 —
/// 런타임 제약·시간·순서는 <b>플레이 모드에서만</b> 드러난다(2026-09-30 따라 그리기에서 배운 것).
///
/// 배치모드로만 부른다(에디터가 열려 있으면 유니티가 거부한다):
/// <c>Unity.exe -batchmode -projectPath . -executeMethod _StatueLive.Run -out &lt;폴더&gt;</c>
/// </summary>
public static class _StatueLive
{
    public static void Run()
    {
        var a = System.Environment.GetCommandLineArgs();
        string dir = Path.GetTempPath();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-out") dir = a[i + 1];

        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var probe = new GameObject("_Probe").AddComponent<StatueProbe>();
        probe.file = Path.Combine(dir, "statue_live.txt");
        probe.shotDir = dir;
        File.WriteAllText(probe.file, "시작\n");
        EditorApplication.EnterPlaymode();
    }
}

/// <summary>씬에 심는 탐침 — 도메인 리로드를 넘어 살아남는 건 이쪽뿐이다.</summary>
public class StatueProbe : MonoBehaviour
{
    public string file;
    public string shotDir;

    int frame;
    float won;
    readonly StringBuilder sb = new StringBuilder();

    // ★ 이 검사는 <b>진짜 프로젝트</b>에서 돈다. 진행 기록을 만지니 반드시 되돌려 놔야 한다 —
    //   미러 프로젝트가 PlayerPrefs 를 공유해 유저 기록을 날린 적이 있다(2026-09-17).
    static readonly string[] Keys = { "Racing.Collected", "결승클리어", "Racing.Chapter",
                                      "Racing.StorySeen", "Racing.CampusWon" };
    readonly System.Collections.Generic.Dictionary<string, string> backup = new();

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        foreach (var k in Keys)
            backup[k] = PlayerPrefs.HasKey(k)
                      ? PlayerPrefs.GetString(k, PlayerPrefs.GetInt(k, 0).ToString()) : null;
    }

    void Update()
    {
        frame++;
        if (frame == 10) { Look("켠 직후"); Snap("lobby_before.png"); }
        if (frame == 14) { CollectionState.ClearAll(); Look("수집 0 (임무 중)"); }
        if (frame == 40)
        {
            var ids = new System.Collections.Generic.List<string>();
            foreach (var e in ExhibitCatalogue.All) ids.Add(e.id);
            CollectionState.CollectAll(ids);
            GrandFinal.MarkCleared();
            Look("결승까지 깬 직후");
            won = Time.time;
        }
        if (won > 0f && Time.time - won > 6f) { Finish(); }
        if (frame > 4000) { Finish(); }
    }

    void Finish()
    {
        Look("연출 끝");
        Snap("lobby_after.png");
        Restore();
        sb.AppendLine("진행 기록 되돌림");
        File.WriteAllText(file, sb + "끝\n");
        EditorApplication.Exit(0);
    }

    void Look(string when)
    {
        sb.AppendLine($"── {when}");
        var st = Object.FindFirstObjectByType<CampusStatue>(FindObjectsInactive.Include);
        if (st == null) { sb.AppendLine("   DonorStatue 없음 !!"); Write(); return; }

        sb.AppendLine($"   자리 {st.transform.position} · 각 {st.transform.eulerAngles.y:0}°");
        foreach (var n in new[] { "Figure_Muscle", "Figure_Real", "Plate", "Figure" })
        {
            var t = st.transform.Find(n);
            if (t == null) { sb.AppendLine($"   {n,-14} 없음"); continue; }
            var rs = t.GetComponentsInChildren<Renderer>(true);
            string box = "-";
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                box = $"{b.size.x:0.00}x{b.size.y:0.00}x{b.size.z:0.00} 바닥y {b.min.y:0.00} 꼭대기 {b.max.y:0.00}";
            }
            sb.AppendLine($"   {n,-14} 켜짐 {t.gameObject.activeSelf,-5} · {box}");
        }

        // 홀의 다른 물건과 겹치나 — 받침대·방송 화면·문
        var mine = st.GetComponentsInChildren<Renderer>(true);
        if (mine.Length > 0)
        {
            var box = mine[0].bounds;
            for (int i = 1; i < mine.Length; i++) box.Encapsulate(mine[i].bounds);
            int hit = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r.transform.IsChildOf(st.transform)) continue;
                if (r.bounds.max.y < 0.35f || r.bounds.min.y > 6f) continue;
                if (r.bounds.size.x > 25f || r.bounds.size.z > 25f) continue;
                if (box.Intersects(r.bounds)) hit++;
            }
            sb.AppendLine($"   홀 물건과 겹침 {hit}개");
        }
        sb.AppendLine($"   GrandFinal.FreeRun {GrandFinal.FreeRun} · 수집 {CollectionState.Count}");
        Write();
    }

    void Snap(string name)
    {
        if (shotDir == null) return;
        var cam = Camera.main;
        if (cam == null)
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            { cam = c; break; }
        if (cam == null) return;

        var rt = new RenderTexture(760, 428, 24) { antiAliasing = 2 };
        var keep = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(760, 428, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 760, 428), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = keep;
        File.WriteAllBytes(Path.Combine(shotDir, name), tex.EncodeToPNG());
        Object.Destroy(rt);
        Object.Destroy(tex);
    }

    void Restore()
    {
        foreach (var k in Keys)
        {
            if (backup[k] == null) { PlayerPrefs.DeleteKey(k); continue; }
            if (int.TryParse(backup[k], out int n)) PlayerPrefs.SetInt(k, n);
            else PlayerPrefs.SetString(k, backup[k]);
        }
        PlayerPrefs.Save();
    }

    void Write() => File.WriteAllText(file, sb.ToString());
}
