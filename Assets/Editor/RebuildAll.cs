using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 씬 셋을 한 번에 다시 굽는다.
///
/// 이게 있어야 하는 이유: 코드를 고쳐도 <b>씬은 저절로 안 바뀐다</b>. 씬은 만들 때의 상태가
/// 파일로 굳어 있어서, 새 부품(RaceProgress · KartSkin 같은)을 붙였으면 다시 구워야 들어간다.
/// 메뉴가 씬마다 따로 있으니 하나를 빼먹기 쉬웠고, 실제로 그래서
/// "랩이 안 세어진다", "세운이를 골라도 세진 카트가 나온다" 가 반복됐다.
///
/// 그래서 버튼 하나로 묶었다. 헷갈릴 일이 없다.
/// </summary>
public static class RebuildAll
{
    [MenuItem("Racing/씬 세 개 전부 다시 만들기", false, 0)]
    public static void Everything()
    {
        if (!EditorUtility.DisplayDialog(
                "씬 세 개 다시 만들기",
                "로비 · 트랙 · 전시실을 코드에서 다시 만든다.\n\n" +
                "★ 씬에 손으로 넣어둔 물건은 사라진다.\n" +
                "   (FBX 를 로비에 끌어다 놓은 게 있으면 지금 누르지 마)\n\n" +
                "대사·색·설정은 코드에 있어서 안전하다.",
                "다시 만들기", "취소"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        LobbySceneBuilder.BuildLobby();
        TestSceneBuilder.BuildAll();
        GallerySceneBuilder.BuildGallery();

        LobbySceneBuilder.RegisterScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity", OpenSceneMode.Single);

        Debug.Log("[Racing] 로비 · 트랙 · 전시실 셋 다 다시 만들었어. " +
                  "F1 로비 / F2 트랙 / F3 전시실. 로비를 열어뒀으니 그대로 재생하면 된다.");
    }
}
