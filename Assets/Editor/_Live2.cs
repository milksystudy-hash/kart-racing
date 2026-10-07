using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 출발 카운트를 <b>실제 플레이 모드에서</b> 센다 — 「소리만 나고 또 한 번」이 고쳐졌는지 보는 탐침.
///
/// ★ 에디터 콜백으로는 못 잰다. <c>EnterPlaymode</c> 가 도메인을 다시 읽으면서
///   <c>EditorApplication.update</c> 구독이 날아가기 때문에, <b>씬에 MonoBehaviour 를 심어</b> 몬다.
/// ★ <c>-quit</c> 를 빼고 돌릴 것. 탐침이 끝나면 스스로 나간다.
/// </summary>
public static class _Live2
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Track.unity", OpenSceneMode.Single);
        var go = new GameObject("_Probe");
        go.AddComponent<_CountProbe>();
        EditorApplication.EnterPlaymode();
    }
}

public class _CountProbe : MonoBehaviour
{
    static string Out => System.Environment.GetEnvironmentVariable("LIVE_OUT") ?? "C:/temp/live.txt";

    readonly StringBuilder sb = new();
    string last = "";
    int starts;           // "" → "3" 이 몇 번인가. <b>이게 곧 소리가 몇 번 나는가</b>다
    float t0;
    bool closed;

    void Awake() { DontDestroyOnLoad(gameObject); t0 = Time.unscaledTime; }

    void Update()
    {
        float t = Time.unscaledTime - t0;
        string cd = RaceCountdown.Label;

        if (cd != last)
        {
            if (string.IsNullOrEmpty(last) && !string.IsNullOrEmpty(cd)) starts++;
            sb.AppendLine($"{t,6:0.00}s  카드 {(RaceBriefing.Open ? "O" : "·")}  " +
                          $"티키타카 {(RaceBriefing.Chatting ? "O" : "·")}  " +
                          $"숫자 '{last}' → '{cd}'" +
                          (string.IsNullOrEmpty(last) && !string.IsNullOrEmpty(cd) ? "   ★ 소리" : ""));
            last = cd;
        }

        // 1.2초 뒤에 카드를 닫는다 — 사람이 키를 누른 것과 같은 길을 탄다(25초 안전장치가 아니라)
        if (!closed && t > 1.2f && RaceBriefing.Open)
        {
            typeof(RaceBriefing).GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Static)
                                ?.Invoke(null, null);
            closed = true;
            sb.AppendLine($"{t,6:0.00}s  (카드를 닫았다)");
        }

        if (t > 18f)
        {
            sb.AppendLine($"\n★ 카운트가 시작된 횟수 {starts} — 1 이어야 한다");
            File.WriteAllText(Out, sb.ToString());
            Debug.Log(sb.ToString());
            EditorApplication.Exit(0);
        }
    }
}
