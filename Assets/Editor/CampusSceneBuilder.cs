using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// <b>캠퍼스 씬</b> — 걸어다니면서 건물을 보고, 곰과 이야기하고, 나중에 미니게임에 들어가는 곳.
///
/// 2026-09-17 유저 판단: *"레이싱하다 내리면 좀 이상하잖아. 똑같은 씬을 만들어서 그 씬을
/// 걷기 모드로 바꾸는 게 낫지 않을까."* 맞다. <b>한 씬이 두 가지 일을 하면 둘 다 어정쩡해진다.</b>
///
/// 트랙 씬과 <b>같은 캠퍼스</b>를 쓴다 — `CampusBuilder` 와 `TrackBuilder` 를 그대로 부른다.
/// 건물을 두 벌로 만들면 한쪽을 고칠 때마다 다른 쪽이 어긋난다(이야기 장면을 로비 안에서
/// 돌리는 것과 같은 이유야).
///
/// <b>코스는 없애지 않는다.</b> 유저가 "트랙을 없애고 건물에 집중해야 하나" 물었는데,
/// 없애면 캠퍼스 한가운데가 포장도 안 된 빈터가 된다. 코스는 그냥 <b>캠퍼스에 난 길</b>이고,
/// 실제로 그 길을 따라 걸으면 건물 열셋을 다 지나가게 돼 있다.
/// 대신 <b>레이스 장비는 아무것도 안 만든다</b> — 카트·AI·발판·체크포인트·순위판.
/// </summary>
public static class CampusSceneBuilder
{
    const string SceneFolder = "Assets/Scenes";
    public const string CampusPath = SceneFolder + "/Campus.unity";

    /// <summary>로비에서 들어오는 자리 — 웅지관(본관) 앞 광장. 문 열고 나오면 캠퍼스가 펼쳐진다.</summary>
    static readonly Vector3 Entrance = new Vector3(0f, 0.2f, -70f);

    [MenuItem("Racing/캠퍼스 씬 만들기", false, 4)]
    public static void BuildCampus()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeLighting();

        // ---- 캠퍼스와 길 : 트랙 씬과 같은 것을 부른다 ----
        var campusGo = new GameObject("Campus");
        var campus = campusGo.AddComponent<CampusBuilder>();

        var trackGo = new GameObject("Track");
        var track = trackGo.AddComponent<TrackBuilder>();
        track.raceFurniture = false;   // 발판·체크포인트를 안 만든다. 길만 남는다

        campus.Build();
        track.Build();

        // <b>구운 걸 그대로 쓴다.</b> `built` 는 저장되지 않는 필드라, 실행할 때 Awake 가
        // "아직 안 지었네" 하고 <b>한 벌 더</b> 짓는다 — 캠퍼스가 통째로 두 벌이 되고
        // 현판 글씨도 두 겹으로 겹쳐 보인다(2026-09-17 유저 제보).
        // 트랙 씬은 굽지 않고 저장해서 Awake 가 짓지만, 여기는 걸어 다니는 씬이라
        // 에디터에서 보이는 게 낫다. 구웠으면 Awake 는 꺼 둔다.
        campus.buildOnAwake = false;
        track.buildOnAwake = false;

        // ---- 걸어다닐 몸 ----
        var player = TestSceneBuilder.MakePlayer(Entrance, 0f);

        // ---- 돌아가는 문 : 본관 앞에 세운다 ----
        MakeReturnDoor(new Vector3(0f, 0f, -74f), 0f);

        foreach (var door in Object.FindObjectsByType<SceneDoor>(FindObjectsSortMode.None))
            door.visitor = player.controller.transform;

        // ---- 리그 ----
        var rig = new GameObject("GameRig");
        rig.AddComponent<SceneNavigator>();
        rig.AddComponent<CampusMood>();      // 현판 딱지와 빛이 이야기를 따라간다
        rig.AddComponent<CampusHUD>();

        MuseumLook.RefineMaterials();
        MuseumLook.ApplyToOpenScene();

        EditorSceneManager.SaveScene(scene, CampusPath);
        LobbySceneBuilder.RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Campus.unity 생성 완료. F1 로비 · F2 트랙 · F3 전시실 · F4 캠퍼스.");
    }

    /// <summary>
    /// 로비로 돌아가는 문. <b>건물에 붙이지 않고 따로 세운다</b> — 본관(웅지관)에 붙이면
    /// "웅지관에 들어간다" 로 읽히는데 실제로는 중앙홀로 가는 거라 헷갈린다.
    /// </summary>
    static void MakeReturnDoor(Vector3 at, float yaw)
    {
        var root = new GameObject("ReturnDoor").transform;
        root.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));

        HanokDoor.Build(root, Vector3.zero, Quaternion.identity, 4f, 4.6f, FlatMaterial.Get,
                        plaque: true, buildingName: "중앙홀", department: "돌아가기");

        // 문 뒤를 막는 벽 한 장. 문만 들판에 서 있으면 통과해서 뒤로 걸어가게 된다.
        var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "ReturnWall";
        back.transform.SetParent(root, false);
        back.transform.localPosition = new Vector3(0f, 3f, -0.4f);
        back.transform.localScale = new Vector3(8f, 6f, 0.6f);
        back.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(new Color32(0xEF, 0xE7, 0xD6, 0xFF));
        back.isStatic = true;

        var door = root.gameObject.AddComponent<SceneDoor>();
        door.sceneIndex = 0;          // 로비
        door.label = "중앙홀로";
        door.range = 3.6f;
    }

    static void MakeLighting()
    {
        var go = new GameObject("Sun");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f);
        light.intensity = 1.05f;
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(48f, 28f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.60f, 0.58f, 0.53f);
        RenderSettings.ambientEquatorColor = new Color(0.46f, 0.44f, 0.39f);
        RenderSettings.ambientGroundColor  = new Color(0.25f, 0.22f, 0.19f);

        // 트랙과 같은 값. 배경색도 이것과 같아야 먼 벽이 하늘 띠처럼 안 보인다.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.55f, 0.51f, 0.45f);
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 320f;
    }
}
