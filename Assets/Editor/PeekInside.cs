using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// <b>건물 안을 씬 뷰에서 들여다본다.</b>
///
/// 2026-09-23 유저: *"에셋 배치하려고 해당 관에 들어가면 일정 이상 확대가 안 되고
/// 건물 밖에만 계속 보게 된다."*
///
/// 원인 둘이다:
/// 1. <b>씬 뷰 줌은 «피벗까지의 거리» 에 비례한다.</b> 캠퍼스가 200m 규모라 피벗이 멀면
///    한 번 굴릴 때 수 미터씩 움직여서 방 안으로 못 들어간다.
/// 2. 들어가도 <b>지붕과 벽에 가려서</b> 안이 안 보인다.
///
/// 이 메뉴는 고른 건물의 <b>지붕·천장·앞벽을 잠깐 숨기고</b> 씬 뷰 카메라를 그 방 한가운데에
/// 놓는다. 다시 누르면 전부 되돌린다. <b>씬을 바꾸지 않는다</b> — 숨기는 것은
/// `SceneVisibilityManager` 라 저장돼도 게임에는 아무 영향이 없다.
/// </summary>
public static class PeekInside
{
    static readonly List<GameObject> hidden = new List<GameObject>();
    static bool peeking;

    [MenuItem("Racing/건물 안 들여다보기 %#i", false, 20)]
    public static void Toggle()
    {
        if (peeking) { Restore(); return; }

        var target = Selection.activeGameObject;
        if (target == null)
        {
            EditorUtility.DisplayDialog("건물 안 들여다보기",
                "Hierarchy 에서 건물을 하나 고르고 다시 누르세요.\n\n" +
                "예) Campus > 곰밥마당\n\n" +
                "고른 건물의 지붕과 앞벽을 잠깐 숨기고 방 한가운데로 들어갑니다.\n" +
                "다시 누르면 되돌아옵니다. (단축키 Ctrl+Shift+I)", "확인");
            return;
        }

        // 건물 루트를 찾는다 — 안쪽 조각을 골랐어도 위로 올라가서 동을 잡는다
        var root = target.transform;
        while (root.parent != null && root.parent.name != "Campus" && root.parent.parent != null)
            root = root.parent;

        // ---- 가리는 것을 숨긴다 ----
        // 지붕·처마·기와·천장·앞벽. <b>뒷벽과 옆벽은 남긴다</b> — 다 지우면 방이 아니라
        // 허공이 되어서 오히려 자리를 못 잡는다.
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            bool blocks = n.StartsWith("Roof") || n.StartsWith("Eave") || n.StartsWith("Tile")
                       || n.StartsWith("Ridge") || n.StartsWith("Ceil") || n.StartsWith("Rafter")
                       || n.StartsWith("WallFront") || n.StartsWith("InRafter")
                       || n.StartsWith("Gable") || n.StartsWith("Bracket");
            if (!blocks) continue;
            if (t.GetComponent<Renderer>() == null && t.childCount == 0) continue;

            SceneVisibilityManager.instance.Hide(t.gameObject, true);
            hidden.Add(t.gameObject);
        }

        // ---- 방 한가운데로 ----
        // 바운즈를 그대로 쓰면 지붕까지 쳐서 카메라가 너무 높이 뜬다. <b>바닥에서 눈높이</b>로.
        var rs = root.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) { Restore(); return; }
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        var view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            var pivot = new Vector3(b.center.x, b.min.y + 1.6f, b.center.z);
            // ★ <b>`size` 가 곧 줌 한계다.</b> 이걸 안 줄이면 캠퍼스 크기에 맞춰 잡혀서
            // 아무리 굴려도 방 안으로 못 들어간다 — 유저가 겪은 게 정확히 이것이다.
            view.LookAt(pivot, Quaternion.Euler(12f, 0f, 0f), Mathf.Max(b.size.x, b.size.z) * 0.45f);
            view.Repaint();
        }

        peeking = true;
        Debug.Log($"[들여다보기] '{root.name}' 안으로. 지붕 {hidden.Count}조각을 잠깐 숨겼다 — " +
                  "Ctrl+Shift+I 로 되돌린다. (게임에는 영향 없음)");
    }

    static void Restore()
    {
        foreach (var go in hidden)
            if (go != null) SceneVisibilityManager.instance.Show(go, true);
        hidden.Clear();
        peeking = false;
        Debug.Log("[들여다보기] 지붕을 되돌렸다.");
    }
}
