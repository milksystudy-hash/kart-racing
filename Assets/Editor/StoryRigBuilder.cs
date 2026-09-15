using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 로비에 대화 시스템을 붙인다.
///
/// 이야기 장면 전용 씬을 따로 만들지 않는 이유: 중앙홀이 두 벌이 되기 때문이다.
/// 한쪽에만 FBX 를 채워 넣으면 같은 방인데 로비와 이야기 장면의 생김새가 달라진다.
/// 방은 <b>Lobby.unity 하나</b>뿐이고, 이야기가 흐를 때 카메라만 갈아탄다(StoryStage).
///
/// <b>이 메뉴는 로비를 다시 짓지 않는다.</b> 이미 있는 씬에 없는 것만 더하고 저장한다.
/// 그래서 네가 로비를 꾸민 뒤에 눌러도 안전하고, 여러 번 눌러도 똑같다.
/// StoryCamera 를 옮겨 놓았다면 그 위치도 그대로 둔다.
/// </summary>
public static class StoryRigBuilder
{
    const string LobbyPath = "Assets/Scenes/Lobby.unity";

    const string RigName    = "StoryRig";
    const string CameraName = "StoryCamera";

    // 기본 카메라 자리 — 중앙홀의 큰 곰 조형물(0, 0, -11.5)을 오른쪽에 두고 비스듬히 본다.
    // 화면 아래 1/4 은 대화창이 덮으니 곰이 위쪽에 앉게 잡았다.
    static readonly Vector3 DefaultCamPos  = new Vector3(2.2f, 2.9f, -1.2f);
    static readonly Vector3 DefaultCamLook = new Vector3(-2.0f, 3.0f, -11.5f);

    static void AttachToLobby()
    {
        if (!System.IO.File.Exists(LobbyPath))
        {
            EditorUtility.DisplayDialog("로비가 없어",
                "Assets/Scenes/Lobby.unity 가 없어서 붙일 데가 없어.\n" +
                "먼저 Racing → 로비 씬 만들기 를 눌러줘.", "알겠어");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(LobbyPath, OpenSceneMode.Single);
        var stage = EnsureRig();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // 빌드 설정도 다시 맞춘다 — 예전에 만들었던 이야기 전용 씬이 목록에 남아 있으면 지운다
        LobbySceneBuilder.RegisterScenes();
        AssetDatabase.SaveAssets();

        Debug.Log($"[Racing] 로비에 대화 시스템을 붙였어. 재생하면 이야기가 흐른다. " +
                  $"(T 다시 보기 · 1~5 장면 고르기 · F9/F10 수집 상태 뒤집기)\n" +
                  $"이야기 장면 각도는 {RigName} > {CameraName} 를 옮겨서 정하면 돼. " +
                  $"지금 잠깐 꺼두는 것들: {stage.pauseWhileTalking.Length}개");
    }

    /// <summary>
    /// 지금 열려 있는 씬에 대화 시스템을 보장한다. 있으면 연결만 다시 맞추고,
    /// 없으면 만든다. 로비를 새로 지을 때도 이 함수를 쓴다 — 코드가 한 벌이어야 안 어긋난다.
    /// </summary>
    public static StoryStage EnsureRig()
    {
        var stage = Object.FindFirstObjectByType<StoryStage>(FindObjectsInactive.Include);

        GameObject rigGo;
        if (stage != null)
        {
            rigGo = stage.gameObject;
        }
        else
        {
            rigGo = new GameObject(RigName);
            stage = rigGo.AddComponent<StoryStage>();
        }

        // ---- 대화 ----
        var runner = Ensure<DialogueRunner>(rigGo);
        runner.playOnStart = false;      // 켜는 건 StoryStage 가 정한다
        runner.followChapter = false;
        runner.enabled = false;

        var hud = Ensure<DialogueHUD>(rigGo);
        hud.runner = runner;
        hud.enabled = false;

        // ---- 이야기 카메라 (이미 있으면 위치를 건드리지 않는다) ----
        var camT = rigGo.transform.Find(CameraName);
        Camera storyCam;
        if (camT != null)
        {
            storyCam = Ensure<Camera>(camT.gameObject);
        }
        else
        {
            var camGo = new GameObject(CameraName);
            camGo.transform.SetParent(rigGo.transform, false);
            camGo.transform.position = DefaultCamPos;
            camGo.transform.LookAt(DefaultCamLook);

            storyCam = camGo.AddComponent<Camera>();
            storyCam.fieldOfView = 55f;
            storyCam.nearClipPlane = 0.1f;
            storyCam.farClipPlane = 120f;
            // AudioListener 는 일부러 안 붙인다. 로비 카메라에 이미 있어서 둘이면 경고가 뜬다.
        }
        storyCam.enabled = false;

        // ---- 로비 쪽 연결 ----
        var orbit    = Object.FindFirstObjectByType<LobbyOrbitCamera>(FindObjectsInactive.Include);
        var selector = Object.FindFirstObjectByType<LobbySelector>(FindObjectsInactive.Include);
        var lobbyHud = Object.FindFirstObjectByType<LobbyHUD>(FindObjectsInactive.Include);

        Camera lobbyCam = orbit != null ? orbit.GetComponent<Camera>() : Camera.main;

        var pause = new System.Collections.Generic.List<Behaviour>();
        if (orbit != null)    pause.Add(orbit);
        if (selector != null) pause.Add(selector);
        if (lobbyHud != null) pause.Add(lobbyHud);

        stage.runner = runner;
        stage.hud = hud;
        stage.storyCamera = storyCam;
        stage.lobbyCamera = lobbyCam;
        stage.pauseWhileTalking = pause.ToArray();

        if (lobbyCam == null)
            Debug.LogWarning("[Racing] 로비 카메라를 못 찾았어. StoryStage 의 lobbyCamera 를 손으로 꽂아줘.");

        return stage;
    }

    /// <summary>
    /// 없으면 붙이고 있으면 그대로. `??` 를 쓰지 않는 이유는 유니티 오브젝트의
    /// "파괴됐지만 null 은 아닌" 상태를 `??` 가 못 걸러내기 때문이야.
    /// </summary>
    static T Ensure<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }
}
