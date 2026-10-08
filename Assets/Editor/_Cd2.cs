using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 「두 번째로 레이스에 들어가면 3·2·1 이 안 뜬다」를 <b>실제로 재현해서</b> 잰다.
/// 트랙 → 로비 → 트랙. static 값이 씬을 넘어 살아남는 게 원인인지 보려면 이 순서여야 한다.
/// 배치모드 전용(<c>-quit</c> 빼고), 씬을 저장하지 않는다.
/// </summary>
public static class _Cd2
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Track.unity", OpenSceneMode.Single);
        new GameObject("_CdProbe").AddComponent<_CdProbe>();
        EditorApplication.EnterPlaymode();
    }
}

public class _CdProbe : MonoBehaviour
{
    static string Out => System.Environment.GetEnvironmentVariable("CD_OUT") ?? "C:/temp/cd.txt";
    readonly StringBuilder sb = new();
    float t0;
    float stepAt;
    int step;
    string last = "";
    int shows;

    void Awake() { DontDestroyOnLoad(gameObject); t0 = stepAt = Time.unscaledTime; }
    float Since => Time.unscaledTime - stepAt;
    void Go(int n) { step = n; stepAt = Time.unscaledTime; }

    static string Peek()
    {
        var T = typeof(RaceCountdown);
        string F(string n)
        {
            var f = T.GetField(n, BindingFlags.NonPublic | BindingFlags.Static);
            if (f == null) return "?";
            var v = f.GetValue(null);
            return v is float x ? x.ToString("0.0") : v.ToString();
        }
        return $"running {F("running")} drawn {F("drawn")} startedAt {F("startedAt")} " +
               $"beganAt {F("beganAt")} armedAt {F("armedAt")}";
    }

    void Update()
    {
        float t = Time.unscaledTime - t0;
        string cd = RaceCountdown.Label;

        if (cd != last)
        {
            if (string.IsNullOrEmpty(last) && !string.IsNullOrEmpty(cd)) shows++;
            sb.AppendLine($"{t,6:0.00}s [{SceneManager.GetActiveScene().name,-7}] 숫자 '{last}' → '{cd}'" +
                          (string.IsNullOrEmpty(last) && !string.IsNullOrEmpty(cd) ? "  ★ 보임+소리" : ""));
            last = cd;
        }

        // 1) 첫 레이스 — 카드가 뜨면 닫는다
        if (step == 0 && Since > 2.0f)
        {
            sb.AppendLine($"{t,6:0.00}s 1차 진입 · 카드 {RaceBriefing.Open} · 티키타카 {RaceBriefing.Chatting}");
            sb.AppendLine($"         {Peek()}");
            if (RaceBriefing.Open) Close();
            Go(1); return;
        }
        if (step == 1 && Since > 0.8f && RaceBriefing.Chatting) { SkipChat(); return; }

        // 2) 로비로
        if (step == 1 && Since > 7.0f)
        {
            sb.AppendLine($"{t,6:0.00}s 1차 끝 · {Peek()}  (보인 횟수 {shows})");
            SceneManager.LoadScene("Track"); Go(2); shows = 0; last = ""; return;
        }

        // 3) 다시 트랙 — 여기가 문제의 자리다
        if (step == 2 && Since > 1.6f)
        {
            sb.AppendLine($"{t,6:0.00}s 2차 진입 · 카드 {RaceBriefing.Open} · 티키타카 {RaceBriefing.Chatting}");
            sb.AppendLine($"         {Peek()}");
            if (RaceBriefing.Open) Close();
            Go(4); return;
        }
        if (step == 4 && Since > 0.8f && RaceBriefing.Chatting) { SkipChat(); return; }

        if (step == 4 && Since > 8.0f)
        {
            sb.AppendLine($"{t,6:0.00}s 2차 끝 · {Peek()}");
            sb.AppendLine($"\n★ 2차에서 숫자가 보인 횟수 {shows} — 1 이어야 한다");
            File.WriteAllText(Out, sb.ToString());
            Debug.Log(sb.ToString());
            EditorApplication.Exit(0);
        }
    }

    static void Close() =>
        typeof(RaceBriefing).GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Static)
                            ?.Invoke(null, null);
    static void SkipChat() => RaceBriefing.SkipChatter();
}
