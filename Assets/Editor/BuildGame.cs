using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 배치모드 윈도우 빌드. <b>두 가지를 뽑는다.</b>
///
/// <code>
/// Unity.exe -quit -batchmode -projectPath . -executeMethod BuildGame.Build      ← 제출용
/// Unity.exe -quit -batchmode -projectPath . -executeMethod BuildGame.BuildDev   ← 개발용
/// </code>
///
/// <list type="table">
/// <item><b>제출용</b> — <c>Build\제출용\</c> · 개발 키가 <b>컴파일 단계에서 사라진다</b></item>
/// <item><b>개발용</b> — <c>Build\개발용\</c> · <c>DEV_BUILD</c> 를 켜서 F9 · F10 · F12 가 산다</item>
/// </list>
///
/// ★★ <b>영상 녹화는 개발용으로 한다.</b> 전·후 연출을 보여주려면 수집 상태를
///   즉석에서 만들어야 하는데(F9 · F10), 제출용에는 그 키가 아예 없다 —
///   <see cref="Dev.Enabled"/> 가 상수 <c>false</c> 라 <b>if 문째로 컴파일에서 빠진다.</b>
///
/// ★ <b>exe 이름이 서로 다르다.</b> 폴더만 다르면 작업 표시줄에서 둘이 똑같이 보여서
///   어느 쪽을 녹화하고 있는지 모른다. 개발용은 창 제목에도 「개발용」이 붙는다.
///
/// 결과는 <c>Build\</c> 아래라 <c>.gitignore</c> 에 걸린다.
/// </summary>
public static class BuildGame
{
    // 2026-09-30 제작자가 제목을 정했다 — <see cref="TitleScreen.GameTitle"/> 과 같은 이름이어야
    // 받아 보는 사람이 exe 와 첫 화면을 같은 게임으로 읽는다.
    const string ShipDir  = "Build/제출용";
    const string ShipExe  = "철거까지_여덟_바퀴.exe";

    const string DevDir   = "Build/개발용";
    const string DevExe   = "철거까지_여덟_바퀴_개발용.exe";

    /// <summary>제출용. 개발 키 없음.</summary>
    [MenuItem("Racing/윈도우 빌드 뽑기 (제출용)")]
    public static void Build() => Run(ShipDir, ShipExe, null);

    /// <summary>
    /// 개발용. <c>DEV_BUILD</c> 가 켜져서 <see cref="DevUnlock"/> 과 디버그 키가 들어간다.
    ///
    /// ★ <b>이건 남에게 주는 빌드가 아니다.</b> F9 로 게임을 통째로 깬 상태로 만들 수 있다.
    /// </summary>
    [MenuItem("Racing/윈도우 빌드 뽑기 (개발용 · 녹화용)")]
    public static void BuildDev() => Run(DevDir, DevExe, new[] { "DEV_BUILD" });

    static void Run(string outDir, string exeName, string[] defines)
    {
        bool dev = defines != null && defines.Length > 0;
        string tag = dev ? "개발용" : "제출용";

        // ★ 플레이 중에는 빌드도 안 된다 — 씬을 읽어야 하니 같은 벽에 부딪힌다.
        if (Application.isBatchMode == false && MyProps.Playing($"윈도우 빌드 뽑기({tag})")) return;

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

        Debug.Log($"[빌드·{tag}] 씬 {scenes.Length}개: {string.Join(", ", scenes)}");

        Directory.CreateDirectory(outDir);

        // ★★ 창 제목으로 둘을 가른다. 녹화할 때 어느 빌드를 켰는지 <b>화면에서</b> 보여야 한다 —
        //   「키가 있어도 화면에 없으면 없는 것」의 사촌이야. 뽑고 나면 반드시 되돌린다.
        string keepName = PlayerSettings.productName;
        if (dev) PlayerSettings.productName = keepName + " (개발용)";

        var options = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = Path.Combine(outDir, exeName),
            target           = BuildTarget.StandaloneWindows64,
            options          = BuildOptions.None,
        };

        // ★ <c>extraScriptingDefines</c> 는 <b>이 빌드에만</b> 적용된다 —
        //   ProjectSettings 를 건드리지 않으니 에디터와 다음 제출용 빌드가 안 오염된다.
        if (dev) options.extraScriptingDefines = defines;

        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            PlayerSettings.productName = keepName;
        }

        BuildSummary s = report.summary;

        Debug.Log($"[빌드·{tag}] 결과 {s.result} · {s.totalSize / 1048576f:F1} MB · " +
                  $"{s.totalTime.TotalSeconds:F0}초 · 에러 {s.totalErrors} · 경고 {s.totalWarnings}");

        if (s.result != BuildResult.Succeeded)
        {
            // 어디서 터졌는지 남긴다 — 로그 끝만 봐서는 단계를 알 수 없다.
            foreach (var step in report.steps)
                foreach (var msg in step.messages)
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        Debug.LogError($"[빌드·{tag}] {step.name}: {msg.content}");

            EditorApplication.Exit(1);
            return;
        }

        // ★★ 2026-10-16 — <b>유니티가 남기는 「DoNotShip」 폴더를 지운다.</b>
        //   Burst 컴파일러가 네이티브로 바꾼 목록을 텍스트로 남기는데(192KB, 파일 한 장),
        //   게임은 그걸 안 읽는다. 유니티가 <b>폴더 이름에 「배포하지 마라」를 박아</b> 뒀고,
        //   제출본에 그런 이름이 섞여 있으면 받는 쪽이 한 번 멈칫한다.
        //   매 빌드마다 다시 생기니 손으로 지우면 다음 빌드에서 또 있다.
        foreach (string dir in Directory.GetDirectories(outDir))
            if (dir.EndsWith("_DoNotShip"))
            {
                Directory.Delete(dir, true);
                Debug.Log($"[빌드·{tag}] 디버그 찌꺼기 지움 → {Path.GetFileName(dir)}");
            }

        Debug.Log($"[빌드·{tag}] 성공 → {Path.GetFullPath(options.locationPathName)}");
        EditorApplication.Exit(0);
    }
}
