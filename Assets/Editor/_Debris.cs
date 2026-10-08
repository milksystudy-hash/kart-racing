using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 자재를 <b>실제로 받아 보고</b> 잰다 — 타고 넘는지, 날아가는지, 속도가 깎이는지.
/// 배치모드 전용(<c>-quit</c> 를 빼고 돌릴 것). 씬을 저장하지 않는다.
/// </summary>
public static class _Debris
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Track.unity", OpenSceneMode.Single);
        var go = new GameObject("_DebrisProbe");
        go.AddComponent<_DebrisProbe>();
        EditorApplication.EnterPlaymode();
    }
}

public class _DebrisProbe : MonoBehaviour
{
    static string Out => System.Environment.GetEnvironmentVariable("DEBRIS_OUT") ?? "C:/temp/debris.txt";
    readonly StringBuilder sb = new();
    int step;
    float t0;
    KartController kart;
    RoadDebris target;
    Vector3 debrisHome;
    float kartYBefore, speedBefore;

    void Awake() { DontDestroyOnLoad(gameObject); t0 = Time.unscaledTime; }

    void FixedUpdate()
    {
        float t = Time.unscaledTime - t0;

        if (step == 0 && t > 1.0f)
        {
            // 카운트다운·브리핑을 걷어낸다
            RaceBriefing.Skip(); RaceCountdown.Skip();

            foreach (var k in FindObjectsByType<KartController>(FindObjectsSortMode.None))
                if (k.GetComponent<PlayerKart>() != null) kart = k;

            var pieces = FindObjectsByType<RoadDebris>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sb.AppendLine($"자재 {pieces.Length}개 · 카트 {(kart ? kart.name : "없음")}");
            if (pieces.Length == 0 || kart == null) { Finish(); return; }

            target = pieces[0];
            var rb = target.GetComponent<Rigidbody>();
            sb.AppendLine($"레이어 {target.gameObject.layer} (2 여야 한다) · 질량 {rb.mass:0.0} · " +
                          $"감쇠 {rb.linearDamping:0.0}/{rb.angularDamping:0.0} · 충돌검출 {rb.collisionDetectionMode}");
            foreach (var c in target.GetComponentsInChildren<Collider>(true))
                sb.AppendLine($"   콜라이더 {c.GetType().Name} on '{c.name}' (trigger {c.isTrigger})");
            step = 1; return;
        }

        if (step == 1 && t > 1.3f)
        {
            // 자재 6m 뒤에, 자재를 향해 세운다
            debrisHome = target.transform.position;

            // ★ 코스 접선을 «찾아서» 쓴다. 자재의 제 forward 는 i*37도 돌려 놔서 못 믿는다.
            var tb = FindFirstObjectByType<TrackBuilder>();
            float best = 0f, bestD = float.MaxValue;
            for (int i = 0; i <= 2000; i++)
            {
                float u = i / 2000f;
                float d2 = (tb.transform.position + tb.PointOnPath(u) - debrisHome).sqrMagnitude;
                if (d2 < bestD) { bestD = d2; best = u; }
            }
            Vector3 dir = tb.TangentOnPath(best).normalized;
            sb.AppendLine($"코스 위치 t {best:0.000} · 자재까지 {Mathf.Sqrt(bestD):0.00} m");

            kart.transform.position = debrisHome - dir * 6f + Vector3.up * 0.5f;
            kart.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            step = 2; return;
        }

        if (step == 2 && t > 1.6f)
        {
            var krb = kart.GetComponent<Rigidbody>();
            krb.linearVelocity = kart.transform.forward * 18f;
            kartYBefore = kart.transform.position.y;
            speedBefore = kart.SpeedKph;
            RoadDebris.ResetHits();
            sb.AppendLine($"\n받으러 간다 — 카트 y {kartYBefore:0.00} · {speedBefore:0.0} ㎞/h");
            step = 3; return;
        }

        if (step == 3 && t > 2.4f)
        {
            float dy = kart.transform.position.y - kartYBefore;
            Vector3 d = target.transform.position - debrisHome;
            sb.AppendLine($"\n결과");
            sb.AppendLine($"  카트 높이 변화 {dy:+0.00;-0.00} m   ← 0.3 넘으면 «타고 넘었다»");
            sb.AppendLine($"  카트 속도    {kart.SpeedKph:0.0} ㎞/h  (받기 전 {speedBefore:0.0})");
            sb.AppendLine($"  자재 이동    옆 {new Vector2(d.x, d.z).magnitude:0.00} m · 위 {d.y:+0.00;-0.00} m");
            sb.AppendLine($"  자재 속도    {target.GetComponent<Rigidbody>().linearVelocity.magnitude:0.0} m/s  ← 7 이하");
            sb.AppendLine($"  센 횟수      {RoadDebris.Hits}  ← 1 이어야 한다");
            Finish();
        }
    }

    void Finish()
    {
        File.WriteAllText(Out, sb.ToString());
        Debug.Log(sb.ToString());
        EditorApplication.Exit(0);
    }
}
