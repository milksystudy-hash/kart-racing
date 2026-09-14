using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 네가 직접 만든 맵을 넣을 빈 씬을 만든다.
///
/// 회색 상자 트랙(Track 씬)은 내 테스트용으로 그대로 두고, 여기서 네 맵을 작업하면
/// 서로 안 부딪힌다. 카트와 카메라와 HUD 는 이미 붙어 있어서 FBX 만 넣으면 바로 굴려볼 수 있다.
///
/// 넣는 자리는 **MapRoot** — 거기 자식으로 FBX 를 끌어다 놓으면 된다.
/// 맵이 들어오면 `StarterGround` 는 지워도 된다. 그전까지 카트가 허공에 떨어지지 말라고 깔아둔 판이야.
///
/// 체크포인트는 아직 없다. 네 맵이 자리를 잡으면 그때 코스를 따라 놓을게 —
/// 랩 카운트와 임무 판정은 체크포인트 **번호**만 보기 때문에, 맵이 뭐가 되든 그대로 얹힌다.
/// </summary>
public static class MyTrackSceneBuilder
{
    const string SceneFolder = "Assets/Scenes";
    const string MyTrackPath = SceneFolder + "/MyTrack.unity";

    static readonly Color ColStarter = new Color32(0x6E, 0x6C, 0x66, 0xFF);
    static readonly Color ColGuide   = new Color32(0xE8, 0xEA, 0xE2, 0xFF);
    static readonly Color ColGuideKm = new Color32(0xC9, 0x8A, 0x78, 0xFF);
    static readonly Color ColRefSage = new Color32(0x9C, 0xC4, 0x89, 0xFF);

    [MenuItem("Racing/내 맵 씬 만들기 (MyTrack)")]
    public static void BuildMyTrack()
    {
        if (File.Exists(MyTrackPath) &&
            !EditorUtility.DisplayDialog("MyTrack 씬 다시 만들기",
                "MyTrack.unity 가 이미 있어. 다시 만들면 그 안에 넣어둔 맵이 사라져.\n정말 다시 만들까?",
                "다시 만들기", "취소"))
            return;

        Directory.CreateDirectory(SceneFolder);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        TestSceneBuilder.MakeSun();

        // ★ 네 맵이 들어갈 자리
        var mapRoot = new GameObject("MapRoot");
        mapRoot.transform.position = Vector3.zero;

        MakeStarterGround();
        MakeScaleGuides();

        var kart = TestSceneBuilder.MakeKart(new Vector3(0f, 0.38f, 0f), Quaternion.identity);
        var kartCam = TestSceneBuilder.MakeKartCamera(kart);

        var rig = new GameObject("GameRig");
        rig.AddComponent<SceneNavigator>();

        var tracker = rig.AddComponent<LapTracker>();
        tracker.kart = kart;
        tracker.checkpointCount = 0;   // 맵이 정해지면 그때 체크포인트를 놓는다
        tracker.totalLaps = 3;

        var switcher = rig.AddComponent<PlayerModeSwitcher>();
        switcher.kart = kart;
        switcher.kartCamera = kartCam;

        var hud = rig.AddComponent<TestHUD>();
        hud.modeSwitcher = switcher;
        hud.kart = kart;
        hud.tracker = tracker;

        EditorSceneManager.SaveScene(scene, MyTrackPath);
        LobbySceneBuilder.RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] MyTrack.unity 생성 완료. MapRoot 에 네 FBX 를 넣으면 돼. (F6 으로 이동)");
    }

    /// <summary>맵이 들어오기 전까지 카트가 설 자리. 맵 넣으면 지워도 된다.</summary>
    static void MakeStarterGround()
    {
        var go = TestSceneBuilder.Cube(null, "StarterGround (맵 넣으면 지워도 됨)",
                                       new Vector3(0f, -0.5f, 0f),
                                       new Vector3(80f, 1f, 80f), ColStarter);
        go.isStatic = true;
    }

    /// <summary>
    /// 맵 크기를 눈으로 가늠하는 기준물. 블렌더에서 "100m 가 얼마나 큰가" 를 감으로 잡기 어려운데,
    /// 여기 세워두면 네 맵을 넣었을 때 바로 비교가 된다. 다 쓰면 지워도 된다.
    /// </summary>
    static void MakeScaleGuides()
    {
        var root = new GameObject("ScaleGuides (참고용 · 지워도 됨)").transform;

        // 규격 기준물 — Testbed 와 같은 것들
        TestSceneBuilder.Cube(root, "Ref_1m_Cube", new Vector3(-6f, 0.5f, 4f), Vector3.one, ColGuide);
        TestSceneBuilder.Capsule(root, "Ref_Character_1.25m", new Vector3(-3.5f, 0.625f, 4f),
                                 new Vector3(0.48f, 0.625f, 0.48f), ColRefSage);
        TestSceneBuilder.Cube(root, "Ref_Kart_1.5x1.1x0.55", new Vector3(-1f, 0.275f, 4f),
                              new Vector3(1.1f, 0.55f, 1.5f), ColGuideKm);

        // 거리 기둥 — 원점에서 동서남북으로 25 / 50 / 100 m
        var posts = new GameObject("DistancePosts").transform;
        posts.SetParent(root, false);

        int[] metres = { 25, 50, 100 };
        Vector3[] directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        string[] names = { "E", "W", "N", "S" };

        foreach (int m in metres)
            for (int d = 0; d < directions.Length; d++)
            {
                // 멀수록 높게 세워서 한눈에 구분되게
                float height = 3f + m * 0.06f;
                var post = TestSceneBuilder.Cube(posts, $"{m}m_{names[d]}",
                                                 directions[d] * m + Vector3.up * (height * 0.5f),
                                                 new Vector3(0.5f, height, 0.5f),
                                                 m == 100 ? ColGuideKm : ColGuide, keepCollider: false);
                post.isStatic = true;
            }
    }
}
