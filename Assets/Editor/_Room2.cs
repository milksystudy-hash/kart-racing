using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 방 안을 찍는다 — 소품이 제대로 섰는지 <b>눈으로</b> 확인하는 용도.
/// 좌표를 손으로 적지 않고 <b>건물의 제 좌표계</b>로 준다(건물이 yaw 로 돌아가 있다).
/// 배치모드 전용(<c>-nographics</c> 를 빼고 돌릴 것), 씬을 저장하지 않는다.
/// </summary>
public static class _Room2
{
    static readonly (string hall, Vector3 at, Vector3 look, float fov)[] Takes =
    {
        ("참잘했어요관", new Vector3( 0.5f, 2.0f,  4.3f), new Vector3(-1.0f, 1.4f, -3.0f), 66f),
        ("참잘했어요관", new Vector3(-5.5f, 1.8f,  4.6f), new Vector3( 4.0f, 1.3f, -1.0f), 66f),
    };

    public static void Run()
    {
        string dir = Arg("-out") ?? Path.Combine(Path.GetTempPath(), "Room2");
        Directory.CreateDirectory(dir);
        EditorSceneManager.OpenScene("Assets/Scenes/Campus.unity", OpenSceneMode.Single);

        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError("[방] 카메라가 없다"); return; }
        cam.enabled = true;

        DynamicGI.UpdateEnvironment();
        foreach (var pr in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
            pr.RenderProbe();

        int n = 0;
        foreach (var t in Takes)
        {
            var hall = Find(t.hall);
            if (hall == null) { Debug.LogError($"[방] '{t.hall}' 을 못 찾았다"); continue; }

            cam.transform.position = hall.TransformPoint(t.at);
            cam.transform.LookAt(hall.TransformPoint(t.look));
            cam.fieldOfView = t.fov;

            var rt = new RenderTexture(900, 560, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var png = new Texture2D(900, 560, TextureFormat.RGB24, false);
            png.ReadPixels(new Rect(0, 0, 900, 560), 0, 0);
            png.Apply();
            RenderTexture.active = null; cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(dir, $"{t.hall}_{++n}.png"), png.EncodeToPNG());
            Object.DestroyImmediate(rt); Object.DestroyImmediate(png);
        }
        Debug.Log($"[방] {n}장 찍었다 → {dir}");
    }

    /// <summary>현판(<c>Plaque</c>)의 글자에서 건물을 찾는다 — 이름표가 곧 방 이름이다.</summary>
    static Transform Find(string hall)
    {
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                               FindObjectsSortMode.None))
            if (tr.name == hall) return tr;
        return null;
    }

    static string Arg(string key)
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == key) return a[i + 1];
        return null;
    }
}
