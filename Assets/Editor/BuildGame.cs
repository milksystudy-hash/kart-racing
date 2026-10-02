using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 배치모드 윈도우 빌드. 제출물(.exe)을 뽑는 자리다.
///
///   Unity.exe -quit -batchmode -projectPath . -executeMethod BuildGame.Build
///
/// 결과는 Build\환웅박물관_최후의_그랑프리\ 에 떨어진다 (.gitignore 에 걸려 있다).
/// </summary>
public static class BuildGame
{
    // 2026-09-30 유저가 제목을 정했다 — <see cref="TitleScreen.GameTitle"/> 과 같은 이름이어야
    // 받아 보는 사람이 exe 와 첫 화면을 같은 게임으로 읽는다.
    const string OutDir  = "Build/철거까지_여덟_바퀴";
    const string ExeName = "철거까지_여덟_바퀴.exe";

    [MenuItem("Racing/윈도우 빌드 뽑기")]
    public static void Build()
    {
        // 빌드 설정에 켜져 있는 씬만. 순서는 F1 로비 · F2 트랙 · F3 전시실 · F4 캠퍼스.
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[빌드] 등록된 씬이 없다. File > Build Settings 확인.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[빌드] 씬 {scenes.Length}개: {string.Join(", ", scenes)}");

        Directory.CreateDirectory(OutDir);

        var options = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = Path.Combine(OutDir, ExeName),
            target           = BuildTarget.StandaloneWindows64,
            options          = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary s = report.summary;

        Debug.Log($"[빌드] 결과 {s.result} · {s.totalSize / 1048576f:F1} MB · {s.totalTime.TotalSeconds:F0}초 · 에러 {s.totalErrors} · 경고 {s.totalWarnings}");

        if (s.result != BuildResult.Succeeded)
        {
            // 어디서 터졌는지 남긴다 — 로그 끝만 봐서는 단계를 알 수 없다.
            foreach (var step in report.steps)
                foreach (var msg in step.messages)
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        Debug.LogError($"[빌드] {step.name}: {msg.content}");

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[빌드] 성공 → {Path.GetFullPath(options.locationPathName)}");
        EditorApplication.Exit(0);
    }
}
