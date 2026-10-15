using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ★★ 크레인 네 대를 <b>실제 플레이 모드에서</b> 잰다. (2026-10-15)
///
/// 코드를 또 고치기 전에 <b>원인부터 확정</b>한다. 에디터에서 Start 를 불러 보는
/// 검사는 배선만 보고, <b>시간·입력·런타임 제약은 플레이 모드에서만 드러난다</b>
/// (2026-09-30 에 <c>mesh.vertices</c> 로 겪은 것).
///
/// 보는 것 여섯:
/// <list type="number">
/// <item>네 대가 씬에 실제로 생겼나 · 임무가 전시품으로 잡혔나</item>
/// <item>Boom / Ball 을 찾았나 (못 찾으면 <see cref="WreckingBall"/> 이 아예 안 붙는다)</item>
/// <item>움직임 코드가 <b>불리고는 있나</b> (Update 가 도는가)</item>
/// <item>붐이 <b>실제로 회전했나</b> — 1초 간격으로 각도를 두 번 잰다</item>
/// <item>철구가 <b>움직였나</b> · 땅 위에 있나 · 보이나(Renderer)</item>
/// <item>네 대에 네 가지 동작이 <b>각각</b> 배정됐나</item>
/// </list>
///
/// ★ <c>-quit</c> 를 빼고 돌린다. 탐침이 끝나면 스스로 나간다.
/// </summary>
public static class _Crane2
{
    public static void Run()
    {
        // 4판(전시품)으로 맞춘다 — 수집품 3개가 그 판이다.
        CollectionState.ClearAll();
        for (int i = 0; i < 3; i++) CollectionState.Collect(ExhibitCatalogue.All[i].id);

        EditorSceneManager.OpenScene("Assets/Scenes/Track.unity", OpenSceneMode.Single);
        new GameObject("_CraneProbe").AddComponent<_CraneProbe>();
        EditorApplication.EnterPlaymode();
    }
}

public class _CraneProbe : MonoBehaviour
{
    static string Out => Path.Combine(Path.GetTempPath(), "crane2.txt");

    readonly StringBuilder sb = new();
    readonly List<Quaternion> armRot0 = new();
    readonly List<Vector3> ballPos0 = new();
    List<WreckingBall> cranes;
    float t0;
    int step;

    void Awake() { DontDestroyOnLoad(gameObject); t0 = Time.unscaledTime; }

    void Update()
    {
        float t = Time.unscaledTime - t0;

        // 1.5초: 씬이 다 지어진 뒤에 찾는다 (RaceProps 는 sceneLoaded 뒤에 짓는다)
        if (step == 0 && t > 1.5f)
        {
            step = 1;
            sb.AppendLine($"임무 = {MissionManager.CurrentGoal}   (전시품이어야 한다)");
            sb.AppendLine($"수집품 = {CollectionState.Count} / {ExhibitCatalogue.Count}");
            sb.AppendLine();

            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int craneObjects = 0;
            foreach (var x in all) if (x.name.Contains("R03_Demolition_Crane")) craneObjects++;
            sb.AppendLine($"씬의 크레인 오브젝트 {craneObjects}개   (4개여야 한다)");

            cranes = new List<WreckingBall>(
                Object.FindObjectsByType<WreckingBall>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            sb.AppendLine($"WreckingBall 컴포넌트 {cranes.Count}개   (4개여야 한다)");
            if (craneObjects > 0 && cranes.Count < craneObjects)
                sb.AppendLine("★ 크레인은 있는데 컴포넌트가 모자란다 — Boom/Ball 을 못 찾아 return 했다");
            sb.AppendLine();

            foreach (var w in cranes)
            {
                armRot0.Add(w.arm != null ? w.arm.rotation : Quaternion.identity);
                ballPos0.Add(w.hitPoint != null ? w.hitPoint.position : Vector3.zero);

                sb.AppendLine($"── {w.style}   period {w.period}");
                sb.AppendLine($"   기둥 {Fmt(w.transform.position)}");
                sb.AppendLine($"   arm      = {(w.arm == null ? "★ null" : w.arm.name)}");
                sb.AppendLine($"   ball     = {(w.ball == null ? "★ null" : w.ball.name)}");
                sb.AppendLine($"   hitPoint = {(w.hitPoint == null ? "★ null" : w.hitPoint.name)}");
                sb.AppendLine($"   enabled {w.enabled} · 오브젝트 {w.gameObject.activeInHierarchy}");

                if (w.hitPoint != null)
                {
                    var r = w.hitPoint.GetComponent<Renderer>();
                    sb.AppendLine($"   철구 자리 {Fmt(w.hitPoint.position)}" +
                                  (r == null ? "   (Renderer 없음)" : $"   보임 {r.enabled && r.isVisible}"));
                    float ground = Physics.Raycast(w.hitPoint.position + Vector3.up * 40f, Vector3.down,
                                                   out var hit, 120f, ~(1 << 2)) ? hit.point.y : float.NaN;
                    sb.AppendLine($"   그 아래 바닥 y {ground:F2}   →  철구가 바닥에서 {w.hitPoint.position.y - ground:+0.00;-0.00} m");
                }
                sb.AppendLine();
            }
            return;
        }

        // 3.5초: 2초 동안 얼마나 움직였나
        if (step == 1 && t > 3.5f)
        {
            step = 2;
            sb.AppendLine("── 2초 동안 움직였나 ──────────────────────────────");
            sb.AppendLine($"RaceCountdown.Blocked = {RaceCountdown.Blocked}   (true 면 Update 가 통째로 멈춘다)");
            sb.AppendLine($"RacePause.On          = {RacePause.On}");
            sb.AppendLine();

            for (int i = 0; i < cranes.Count; i++)
            {
                var w = cranes[i];
                float deg = w.arm != null ? Quaternion.Angle(armRot0[i], w.arm.rotation) : -1f;
                float moved = w.hitPoint != null ? Vector3.Distance(ballPos0[i], w.hitPoint.position) : -1f;
                sb.AppendLine($"{w.style,-6}  붐 회전 {deg,6:0.0}°   철구 이동 {moved,6:0.00} m   " +
                              (deg > 1f || moved > 0.2f ? "움직인다" : "★ 안 움직인다"));
            }

            sb.AppendLine();
            sb.AppendLine("동작 배정: " + string.Join(" · ", cranes.ConvertAll(c => c.style.ToString())));

            File.WriteAllText(Out, sb.ToString());
            Debug.Log(sb.ToString());
            EditorApplication.Exit(0);
        }
    }

    static string Fmt(Vector3 v) => $"({v.x:F1}, {v.y:F1}, {v.z:F1})";
}
