using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 바퀴별 노면 장치 점검 — <b>플레이 모드</b>에서 돈다. 트랙 기하는 Awake 에 지어지니
/// 에디터에서 씬만 열어서는 아무 것도 안 보인다.
/// <c>Unity.exe -batchmode -projectPath . -executeMethod _Hazard.Run -out &lt;폴더&gt;</c>
/// </summary>
public static class _Hazard
{
    public static void Run()
    {
        var a = System.Environment.GetCommandLineArgs();
        string dir = Path.GetTempPath();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-out") dir = a[i + 1];

        // ── 먼저 로비 문부터 ──────────────────────────────────────
        var lobby = new StringBuilder();
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        foreach (var d in Object.FindObjectsByType<SceneDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var h = d.GetComponent<HingedDoor>();
            string leaves = h == null ? "HingedDoor 없음 !!"
                : h.leaves == null ? "leaves 비었다 !!"
                : $"문짝 {h.leaves.Length}짝 · null {System.Array.FindAll(h.leaves, x => x == null).Length}개";
            int batched = 0, total = 0;
            if (h != null && h.leaves != null)
                foreach (var l in h.leaves)
                {
                    if (l == null) continue;
                    foreach (var r in l.GetComponentsInChildren<Renderer>(true))
                    { total++; if (r.gameObject.isStatic) batched++; }
                }
            lobby.AppendLine($"  {d.label,-8} → 씬 {d.sceneIndex} · {leaves} · 조각 {total} 중 static {batched} (0 이어야 열린다)");
        }
        File.WriteAllText(Path.Combine(dir, "lobbydoors.txt"), lobby.ToString());

        EditorSceneManager.OpenScene("Assets/Scenes/Track.unity");
        var probe = new GameObject("_Probe").AddComponent<HazardProbe>();
        probe.file = Path.Combine(dir, "hazard.txt");
        probe.shotDir = dir;
        File.WriteAllText(probe.file, "시작\n");
        EditorApplication.EnterPlaymode();
    }
}

public class HazardProbe : MonoBehaviour
{
    public string file;
    public string shotDir;
    int frame;
    readonly StringBuilder sb = new StringBuilder();

    void Awake() => DontDestroyOnLoad(gameObject);

    void Update()
    {
        frame++;
        if (frame == 12) Look();
        if (frame == 20) { Shoot("flood", true); }
        if (frame == 28) { Shoot("bumps", false); Done(); }
    }

    void Look()
    {
        var gate = FindFirstObjectByType<LapHazards>(FindObjectsInactive.Include);
        if (gate == null) { sb.AppendLine("LapHazards 없음 !!"); Write(); return; }

        var floods = gate.floods;
        var bumps = gate.bumps;
        var fz = floods.GetComponentsInChildren<FloodZone>(true);
        var rb = bumps.GetComponentsInChildren<RoadBump>(true);
        sb.AppendLine($"잠긴 구간 {fz.Length}개 · 둔덕 자리 {rb.Length}개");
        sb.AppendLine($"1바퀴 상태 — 물 켜짐 {floods.activeSelf} · 둔덕 켜짐 {bumps.activeSelf}  (둘 다 false 여야 맞다)");

        // ★ 둔덕 꼭대기가 카트 바닥(0.095m)보다 낮아야 몸통이 안 긁힌다
        float worst = 0f;
        int humps = 0;
        foreach (var r in bumps.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!r.name.StartsWith("Hump")) continue;
            humps++;
            // 노면 높이를 바로 밑에서 재서 «노면 위로 얼마나 나왔나» 를 본다
            var b = r.bounds;
            float road = b.center.y;
            if (Physics.Raycast(b.center + Vector3.up * 5f, Vector3.down, out var hit, 20f,
                                ~0, QueryTriggerInteraction.Ignore))
                road = hit.point.y;
            worst = Mathf.Max(worst, b.max.y - (road - (b.max.y - road)));
        }
        sb.AppendLine($"둔덕 {humps}개");

        // 더 확실한 방법 — 둔덕 한가운데에서 위로 레이를 쏴 노면과 둔덕 꼭대기를 각각 잡는다
        float maxRise = 0f;
        foreach (var r in bumps.GetComponentsInChildren<MeshCollider>(true))
        {
            Vector3 top = r.bounds.max;
            Vector3 c = new Vector3(r.bounds.center.x, top.y + 3f, r.bounds.center.z);
            if (!Physics.Raycast(c, Vector3.down, out var onBump, 10f, ~0, QueryTriggerInteraction.Ignore)) continue;
            // 둔덕을 잠깐 끄고 다시 쏘면 노면이 잡힌다
            bool was = r.enabled; r.enabled = false;
            float road = Physics.Raycast(c, Vector3.down, out var onRoad, 10f, ~0, QueryTriggerInteraction.Ignore)
                       ? onRoad.point.y : onBump.point.y;
            r.enabled = was;
            maxRise = Mathf.Max(maxRise, onBump.point.y - road);
        }
        sb.AppendLine($"둔덕이 노면 위로 솟은 높이 최대 {maxRise:0.000} m  (카트 바닥 0.095 보다 낮아야 한다)");

        // 벽 부딪힘으로 안 세는지 — 둔덕에 RoadBump 가 붙어 있나
        int tagged = 0;
        foreach (var mc in bumps.GetComponentsInChildren<MeshCollider>(true))
            if (mc.GetComponentInParent<RoadBump>() != null) tagged++;
        sb.AppendLine($"RoadBump 표식이 붙은 둔덕 콜라이더 {tagged} / {bumps.GetComponentsInChildren<MeshCollider>(true).Length}");

        // 발판·입간판과 겹치나 (월드 거리)
        float nearPad = 999f;
        var pads = FindObjectsByType<BoostPad>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var z in fz)
        {
            var zb = z.GetComponent<Collider>().bounds;
            foreach (var p in pads)
                if (zb.Contains(new Vector3(p.transform.position.x, zb.center.y, p.transform.position.z)))
                    sb.AppendLine($"   !! {z.name} 안에 {p.name} 가 들어 있다");
            foreach (var p in pads)
                nearPad = Mathf.Min(nearPad, Vector3.Distance(z.transform.position, p.transform.position));
        }
        sb.AppendLine($"잠긴 구간 가운데 ↔ 가속 발판 제일 가까운 거리 {nearPad:0.0} m");

        sb.AppendLine($"씬 전체 MeshCollider {FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}개 (도로·담장 + 둔덕)");
        Write();
    }

    void Shoot(string name, bool wet)
    {
        var gate = FindFirstObjectByType<LapHazards>(FindObjectsInactive.Include);
        if (gate == null || shotDir == null) return;
        gate.floods.SetActive(wet);
        gate.bumps.SetActive(!wet);

        var target = (wet ? gate.floods : gate.bumps).transform.GetChild(wet ? 1 : 2);
        var cam = new GameObject("_Cam").AddComponent<Camera>();
        Vector3 back = -target.forward * 13f + Vector3.up * 3.6f;
        cam.transform.SetPositionAndRotation(target.position + back,
                                             Quaternion.LookRotation(target.position + Vector3.up * 1.2f - (target.position + back)));
        cam.fieldOfView = 58f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.51f, 0.45f);
        cam.farClipPlane = 400f;

        var rt = new RenderTexture(820, 460, 24) { antiAliasing = 2 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(820, 460, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 820, 460), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(shotDir, $"hazard_{name}.png"), tex.EncodeToPNG());
        Destroy(cam.gameObject); Destroy(rt); Destroy(tex);
        sb.AppendLine($"{name} 렌더 찍음");
        Write();
    }

    void Done()
    {
        File.WriteAllText(file, sb + "끝\n");
        EditorApplication.Exit(0);
    }

    void Write() => File.WriteAllText(file, sb.ToString());
}
