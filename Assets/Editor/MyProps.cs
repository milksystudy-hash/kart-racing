using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// <b>유저가 손으로 놓은 것을 빌더가 안 지운다.</b>
///
/// 2026-09-23 유저: *"내가 에셋 배치하고 Ctrl+S 누르면 저장하고, 그제서야 너한테 이 에셋
/// 배치했으니까 플레이에 문제없고 네가 재배치 하는 그런 방법은 없을까. 아스트라로 에셋을
/// 10개 이상 제작하는데 배치하는데만 크레딧을 너무 많이 쓰는 것 같아."*
///
/// 맞는 요구다. 여태는 유저가 놓으면 <b>내가 빌더를 다시 돌리는 순간 통째로 날아갔고</b>,
/// 그래서 배치를 전부 나한테 시켜야 했다. 그건 크레딧을 자리 좌표 찍는 데 쓰는 거야.
///
/// <b>규칙은 하나: 빌더가 만들지 않은 루트 오브젝트는 건드리지 않는다.</b>
/// 유저는 FBX 를 씬에 드래그하고 원하는 자리에 놓고 `Ctrl+S` 만 하면 된다 —
/// 빈 통을 만들 필요도, 이름을 맞출 필요도 없다.
///
/// 되살릴 때 쓰는 것은 <b>프리팹 경로 + 트랜스폼</b>이다. 유저가 드래그한 FBX 는
/// 프리팹 인스턴스라 원본 경로가 남아 있어서, 씬을 새로 지어도 같은 자리에 다시 놓을 수 있다.
/// </summary>
public static class MyProps
{
    /// <summary>빌더가 만드는 루트 이름. <b>이 목록에 없으면 유저 것</b>이다.</summary>
    static readonly HashSet<string> Mine = new HashSet<string>
    {
        "Campus", "Track", "Player", "GameRig", "ReturnDoor", "Lobby", "Gallery",
        "Sun", "Directional Light", "Volume", "MuseumLook", "ReflectionProbe",
        "StoryRig", "LobbyRig", "GalleryRig", "StartGate", "Bears", "Stands",
    };

    public class Kept
    {
        public string prefabPath;     // 원본 FBX/프리팹 경로
        public string name;
        public string parentPath;     // 건물 안에 놓았으면 그 경로. 없으면 루트
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;
    }

    /// <summary>
    /// 씬을 새로 짓기 <b>전</b>에 부른다. 지금 열린 씬(또는 <paramref name="scenePath"/>)에서
    /// 유저가 놓은 것을 찾아 기록한다.
    /// </summary>
    public static List<Kept> Collect(string scenePath)
    {
        var kept = new List<Kept>();
        if (!System.IO.File.Exists(scenePath)) return kept;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid()) return kept;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (Mine.Contains(root.name)) continue;
            Gather(root.transform, kept, null);
        }

        // 빌더가 만든 루트 <b>안에</b> 놓았을 수도 있다 — 건물 안에 소품을 넣는 게
        // 오히려 자연스럽다. 프리팹 인스턴스이면서 빌더가 안 만든 것을 찾는다.
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!Mine.Contains(root.name)) continue;
            foreach (var kid in root.GetComponentsInChildren<Transform>(true))
            {
                if (kid == root.transform) continue;
                if (PrefabUtility.GetPrefabAssetType(kid.gameObject) == PrefabAssetType.NotAPrefab) continue;
                if (PrefabUtility.GetNearestPrefabInstanceRoot(kid.gameObject) != kid.gameObject) continue;
                Gather(kid, kept, Path(kid.parent));
            }
        }

        if (kept.Count > 0)
            Debug.Log($"[내 배치] 손으로 놓은 것 {kept.Count}개를 기억했다 — 씬을 다시 지어도 살아남는다.");
        return kept;
    }

    static void Gather(Transform t, List<Kept> into, string parentPath)
    {
        var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
        if (src == null) return;                       // 프리팹이 아니면 되살릴 방법이 없다
        string path = AssetDatabase.GetAssetPath(src);
        if (string.IsNullOrEmpty(path)) return;

        into.Add(new Kept
        {
            prefabPath = path,
            name = t.name,
            parentPath = parentPath,
            pos = t.position,
            rot = t.rotation,
            scale = t.localScale,
        });
    }

    static string Path(Transform t)
    {
        if (t == null) return null;
        string p = t.name;
        for (var up = t.parent; up != null; up = up.parent) p = up.name + "/" + p;
        return p;
    }

    /// <summary>씬을 다 지은 <b>뒤</b>에 부른다. 기록해 둔 것을 같은 자리에 다시 놓는다.</summary>
    public static void Restore(List<Kept> kept)
    {
        if (kept == null || kept.Count == 0) return;

        var holder = new GameObject("내가 놓은 것").transform;
        int ok = 0, lost = 0;

        foreach (var k in kept)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(k.prefabPath);
            if (asset == null) { lost++; continue; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            // ★ 언팩한다 — 이름·위치만 줘도 그건 «오버라이드» 라서, 나중에 다른 모델이
            // 리임포트를 돌리면 통째로 되돌아간다(2026-09-22 스피커에서 겪은 것).
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely,
                                               InteractionMode.AutomatedAction);
            go.name = k.name;

            Transform parent = holder;
            if (!string.IsNullOrEmpty(k.parentPath))
            {
                var found = GameObject.Find(k.parentPath);
                if (found != null) parent = found.transform;   // 건물이 다시 지어졌으면 그 안으로
            }
            go.transform.SetParent(parent, false);
            go.transform.position = k.pos;
            go.transform.rotation = k.rot;
            go.transform.localScale = k.scale;
            ok++;
        }

        if (holder.childCount == 0) Object.DestroyImmediate(holder.gameObject);

        Debug.Log(lost == 0
            ? $"[내 배치] {ok}개를 제자리에 되돌렸다."
            : $"[내 배치] {ok}개 되돌림 · {lost}개는 원본 파일을 못 찾아 빠졌다.");
    }
}
