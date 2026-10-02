using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 캠퍼스 <b>바깥</b>에서 환웅박물관을 찍는다 — 이야기 장면 배경용(2026-10-01).
/// 배치모드 전용, 메뉴 없음. <b>씬을 저장하지 않는다.</b>
///
/// ★ <c>-nographics</c> 를 빼고 돌려야 그림이 나온다. 그리고
/// <see cref="DynamicGI.UpdateEnvironment"/> 와 반사 프로브를 먼저 돌리지 않으면 까맣게 나온다.
///
/// ★★ <b>캠퍼스는 지붕과 윗벽으로 완전히 덮여 있다</b>(z ±125, y 4~26) — 하늘을 가리려고
/// 만든 것이라, 바깥에서 보면 <b>회색 벽</b>밖에 안 보인다(첫 시도가 그랬다).
/// 사진을 찍는 동안만 지붕·윗벽·받침 기둥을 숨기고 안개와 배경색을 <b>하늘색</b>으로 바꾼다.
/// 지붕은 건축이 아니라 <b>게임 장치</b>라서 정지 그림 한 장에는 없는 게 맞다.
/// </summary>
public static class _Shot
{
    const string Scene = "Assets/Scenes/Campus.unity";

    /// <summary>맑고 서늘한 가을 아침. 안개도 같은 값으로 둬야 먼 것이 하늘에 녹는다.</summary>
    static readonly Color Sky = new Color32(0xBF, 0xC9, 0xCE, 0xFF);

    /// <summary>사진 찍는 동안 숨길 것 — 하늘을 가리는 장치들.</summary>
    static readonly string[] Hide = { "UpperWalls", "RoofPosts", "Roof", "HanokRoof", "CampusRoof" };

    static string Dir => System.Environment.GetEnvironmentVariable("SHOT_DIR") ?? "C:/temp/shots";

    // (이름, 카메라 자리, 바라보는 곳, 시야각)
    static readonly (string name, Vector3 at, Vector3 look, float fov)[] Takes =
    {
        ("F1", new Vector3(-27f, 12.5f, 132f), new Vector3( 3f, 5.5f,  95f), 42f),
        ("F2", new Vector3(-24f, 11.0f, 129f), new Vector3( 2f, 5.5f,  96f), 44f),
        ("F3", new Vector3(-33f, 15.5f, 139f), new Vector3( 6f, 4.0f,  90f), 40f),
    };

    public static void Candidates() => Run(1280, 720, Takes, notice: false);

    /// <summary>
    /// 진열장 여덟 칸을 <b>하나씩</b> 찍는다 — 획득 연출의 배경.
    ///
    /// ★★ 2026-10-02 유저: *"두 번째 임무를 깼는데 여전히 1번 코인에만 빛이 뜬다."*
    /// 맞다 — 배경 사진이 <b>1번 진열장 한 장뿐</b>이었다. 이름만 바뀌고 그림은 늘 같은 칸이니,
    /// 여덟 판을 깨도 같은 코인이 여덟 번 빛난다.
    ///
    /// <c>gallery_1.png</c> … <c>gallery_8.png</c>. 없으면 <c>gallery.png</c> 로 떨어진다.
    /// 카메라는 <b>칸마다 똑같은 거리·높이·시야각</b>이라 그림이 서로 튀지 않는다.
    /// </summary>
    public static void Cases()
    {
        Directory.CreateDirectory(Dir);
        EditorSceneManager.OpenScene("Assets/Scenes/Gallery.unity", OpenSceneMode.Single);

        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (cams.Length == 0) { Debug.LogError("[사진] 전시실에 카메라가 없다"); EditorApplication.Exit(1); return; }

        var cam = cams[0];
        cam.gameObject.SetActive(true);
        cam.enabled = true;
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 600f);
        cam.fieldOfView = 46f;

        // 덮개와 분위기 소품을 치운다 — «물건이 들어갈 자리» 가 보여야 한다
        foreach (var c in Object.FindObjectsByType<GalleryCase>(FindObjectsInactive.Include,
                                                                FindObjectsSortMode.None))
        {
            if (c.dustCover != null) c.dustCover.SetActive(false);

            // ★ 진짜 모델이 있으면 그걸 세우고 임시 도형은 치운다 —
            //   획득 연출의 배경은 «모은 뒤» 의 모습이어야 한다.
            if (c.realModel != null)
            {
                c.realModel.SetActive(true);
                if (c.placeholder != null) c.placeholder.SetActive(false);
            }
        }

        foreach (var m in Object.FindObjectsByType<GalleryMood>(FindObjectsInactive.Include,
                                                                FindObjectsSortMode.None))
        {
            foreach (var g in m.beforeThings) if (g != null) g.SetActive(false);
            foreach (var g in m.afterThings)  if (g != null) g.SetActive(false);
        }

        DynamicGI.UpdateEnvironment();
        foreach (var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude,
                                                                    FindObjectsSortMode.None))
            p.RenderProbe();

        var cases = Object.FindObjectsByType<GalleryCase>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);
        const int W = 1920, H = 1080;

        for (int i = 0; i < ExhibitCatalogue.Count; i++)
        {
            string id = ExhibitCatalogue.All[i].id;
            Transform target = null;
            foreach (var c in cases) if (c.itemId == id) { target = c.transform; break; }
            if (target == null) { Debug.LogWarning($"[사진] {id} 진열장을 못 찾았다"); continue; }

            // 칸은 반지름 8.2 원 위에 있고 방 가운데를 본다. 4.6m 앞에서 같은 각도로.
            Vector3 p = target.position;
            Vector3 inward = new Vector3(p.x, 0f, p.z).normalized;
            cam.transform.SetPositionAndRotation(
                new Vector3(p.x, 1.70f, p.z) - inward * 4.6f,
                Quaternion.LookRotation(new Vector3(p.x, 1.42f, p.z)
                                      - (new Vector3(p.x, 1.70f, p.z) - inward * 4.6f), Vector3.up));

            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            string path = Path.Combine(Dir, $"gallery_{i + 1}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[사진] {i + 1}번 {ExhibitCatalogue.All[i].name,-12} 밝기 {Mean(tex):0.000} → {path}");

            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
        // ★ 저장하지 않는다.
    }

    /// <summary>
    /// 방 안 참고 사진 — 이야기 배경으로 쓸 그림을 GPT 에 넘기기 전의 <b>3D 참고</b>다.
    /// 바깥 사진과 달리 <b>아무 것도 숨기지 않는다</b>(지붕이 있어야 방이다).
    ///
    /// 전시실 컷은 그대로 <c>StoryBackdrops/gallery.png</c> 로 쓸 수 있게 잡았다 —
    /// <see cref="ItemReveal.BeamX"/> 0.50 · <see cref="ItemReveal.BeamY"/> 0.62 자리에
    /// <b>진열장 하나가 오도록</b> 한 구도다.
    /// </summary>
    public static void Rooms()
    {
        // (씬, 이름, 카메라 자리, 보는 곳, 시야각)
        var takes = new (string scene, string name, Vector3 at, Vector3 look, float fov)[]
        {
            // 전시실 — 1번 진열장이 (0, 0, 8.2) 에 있고 방 가운데를 본다.
            // 받침 윗면 1.05 · 물건 자리 1.15 · 유리 1.00~2.24
            ("Gallery", "gallery",       new Vector3(0f, 1.70f, 3.60f), new Vector3(0f, 1.42f, 8.2f), 46f),
            ("Gallery", "gallery_near",  new Vector3(0f, 1.55f, 4.90f), new Vector3(0f, 1.35f, 8.2f), 40f),
            ("Gallery", "gallery_wide",  new Vector3(0f, 3.10f, -2.6f), new Vector3(0f, 1.40f, 7.0f), 58f),

            // 중앙홀 — 출발문 쪽을 등지고 접수대·곰 받침대가 보이는 각도
            ("Lobby",   "hall",          new Vector3(-2.5f, 2.3f, -9.0f), new Vector3(2.0f, 2.0f, 6.0f), 56f),
            ("Lobby",   "hall_gate",     new Vector3( 0.0f, 2.2f, -2.0f), new Vector3(0.0f, 2.6f, 11.0f), 52f),

            // 캠퍼스 — 웅지관 앞 광장, 동상이 보이는 자리
            ("Campus",  "campus",        new Vector3(-6f, 2.4f, -72f), new Vector3(10f, 2.6f, -90f), 54f),
            ("Campus",  "campus_plaza",  new Vector3( 0f, 3.2f, -50f), new Vector3( 4f, 2.0f, -86f), 58f),

            // 경기장 — 출발선 아치 아래
            ("Track",   "track",         new Vector3(0f, 3.0f, -62f), new Vector3(0f, 3.0f, -84f), 56f),
        };

        Directory.CreateDirectory(Dir);
        string current = "";
        Camera cam = null;

        foreach (var t in takes)
        {
            if (current != t.scene)
            {
                EditorSceneManager.OpenScene($"Assets/Scenes/{t.scene}.unity", OpenSceneMode.Single);
                current = t.scene;

                var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,
                                                            FindObjectsSortMode.None);
                if (cams.Length == 0) { Debug.LogError($"[사진] {t.scene} 에 카메라가 없다"); continue; }
                cam = cams[0];
                cam.gameObject.SetActive(true);
                cam.enabled = true;
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, 600f);

                // ★ 전시실은 <b>덮개를 걷고</b> 찍는다. 획득 연출의 배경으로 쓸 그림이라
                //   «아직 못 모은 진열장» 이 아니라 <b>물건이 들어갈 자리</b>가 보여야 한다.
                //   붉은 카펫·현수막(다 모은 뒤 물건)과 출입 금지 띠(모으기 전 물건)도 치운다 —
                //   둘 다 화면을 가로질러서 빛줄기가 설 자리를 먹는다.
                if (t.scene == "Gallery")
                {
                    foreach (var c in Object.FindObjectsByType<GalleryCase>(FindObjectsInactive.Include,
                                                                            FindObjectsSortMode.None))
                        if (c.dustCover != null) c.dustCover.SetActive(false);

                    foreach (var m in Object.FindObjectsByType<GalleryMood>(FindObjectsInactive.Include,
                                                                            FindObjectsSortMode.None))
                    {
                        foreach (var g in m.beforeThings) if (g != null) g.SetActive(false);
                        foreach (var g in m.afterThings)  if (g != null) g.SetActive(false);
                    }
                }

                DynamicGI.UpdateEnvironment();
                foreach (var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude,
                                                                            FindObjectsSortMode.None))
                    p.RenderProbe();
            }
            if (cam == null) continue;

            cam.transform.SetPositionAndRotation(t.at, Quaternion.LookRotation(t.look - t.at, Vector3.up));
            cam.fieldOfView = t.fov;

            const int W = 1920, H = 1080;
            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            string path = Path.Combine(Dir, t.name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[사진] {t.name,-14} 밝기 {Mean(tex):0.000}  →  {path}");

            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
        // ★ 씬은 저장하지 않는다.
    }


    /// <summary>고른 한 컷을 2560×1440 으로. 통지서 붙인 것과 안 붙인 것 둘 다.</summary>
    public static void Final()
    {
        string pick = System.Environment.GetEnvironmentVariable("SHOT_TAKE") ?? "A_길건너";
        var one = System.Array.Find(Takes, t => t.name == pick);
        if (one.name == null) { Debug.LogError($"[사진] '{pick}' 컷이 없어"); EditorApplication.Exit(1); return; }

        Run(2560, 1440, new[] { ("prologue_민짜", one.at, one.look, one.fov) }, notice: false);
        Run(2560, 1440, new[] { ("prologue_통지서", one.at, one.look, one.fov) }, notice: true);
    }

    static void Run(int w, int h, (string name, Vector3 at, Vector3 look, float fov)[] takes, bool notice)
    {
        Directory.CreateDirectory(Dir);
        if (EditorSceneManager.GetActiveScene().path != Scene)
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

        // 씬에 이미 있는 카메라를 빌려 쓴다 — 새로 만들면 URP 후처리(톤매핑·블룸·비네트)가 없어서
        // 다른 게임처럼 나온다.
        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (cams.Length == 0) { Debug.LogError("[사진] 캠퍼스 씬에 카메라가 없다"); EditorApplication.Exit(1); return; }

        var cam = cams[0];
        var go = cam.gameObject;
        bool wasActive = go.activeSelf, wasOn = cam.enabled;
        var keepPos = cam.transform.position;
        var keepRot = cam.transform.rotation;
        float keepFov = cam.fieldOfView, keepFar = cam.farClipPlane;
        var keepClear = cam.clearFlags;
        var keepBg = cam.backgroundColor;

        var keepFog = RenderSettings.fogColor;
        float keepDensity = RenderSettings.fogDensity;

        go.SetActive(true);
        cam.enabled = true;
        cam.farClipPlane = Mathf.Max(keepFar, 800f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Sky;

        // 안개를 하늘색으로. 값이 다르면 먼 담장이 <b>하늘에 그은 띠</b>처럼 보인다
        // (2026-09-16 에 트랙에서 겪은 것과 같은 함정).
        RenderSettings.fogColor = Sky;
        RenderSettings.fogDensity = keepDensity * 0.18f;

        var hidden = HideEnclosure();
        GameObject prop = notice ? Notice() : null;

        DynamicGI.UpdateEnvironment();
        foreach (var p in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude,
                                                                   FindObjectsSortMode.None))
            p.RenderProbe();

        foreach (var t in takes)
        {
            cam.transform.SetPositionAndRotation(t.at, Quaternion.LookRotation(t.look - t.at, Vector3.up));
            cam.fieldOfView = t.fov;

            var rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            string path = Path.Combine(Dir, t.name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[사진] {t.name}  밝기 {Mean(tex):0.000}  →  {path}");

            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }

        if (prop != null) Object.DestroyImmediate(prop);
        foreach (var r in hidden) if (r != null) r.enabled = true;

        RenderSettings.fogColor = keepFog;
        RenderSettings.fogDensity = keepDensity;
        cam.transform.SetPositionAndRotation(keepPos, keepRot);
        cam.fieldOfView = keepFov;
        cam.farClipPlane = keepFar;
        cam.clearFlags = keepClear;
        cam.backgroundColor = keepBg;
        cam.enabled = wasOn;
        go.SetActive(wasActive);
        // ★ 저장하지 않는다.
    }

    /// <summary>지붕·윗벽·받침 기둥을 잠깐 끈다. 렌더러만 끄니 씬 데이터는 그대로다.</summary>
    static List<Renderer> HideEnclosure()
    {
        var off = new List<Renderer>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude,
                                                             FindObjectsSortMode.None))
        {
            if (System.Array.IndexOf(Hide, t.name) < 0) continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>(false))
                if (r.enabled) { r.enabled = false; off.Add(r); }
        }
        Debug.Log($"[사진] 하늘 가리개 {off.Count}조각 숨김");
        return off;
    }

    static float Mean(Texture2D t)
    {
        var px = t.GetPixels();
        float s = 0f;
        foreach (var c in px) s += c.grayscale;
        return s / px.Length;
    }

    /// <summary>
    /// 정문 기둥에 붙은 <b>붉은 철거 통지서</b>. 프롤로그의 첫 대사가
    /// *"현관에 뭐가 붙어 있는데"* 라서, 배경에 이게 있으면 대사가 그림을 가리킨다.
    /// 씬에 저장되지 않는 임시 물건이다.
    /// </summary>
    static GameObject Notice()
    {
        var root = new GameObject("_Notice");

        // ── 정문에 걸린 철거 예고 현수막 ──────────────────────────────
        // 기둥에 붙인 A4 쪽지는 2560 폭에서 <b>58×101px</b> 밖에 안 됐다(측정) —
        // 화면에서는 29px 이라 «뭔가 붙어 있네» 도 안 된다. 한국에서 철거 예고는
        // <b>현수막</b>으로 걸리고, 그래야 멀리서도 읽힌다.
        var band = new GameObject("Banner").transform;
        band.SetParent(root.transform, false);
        band.SetPositionAndRotation(new Vector3(0f, 6.5f, 103.0f), Quaternion.Euler(0f, 180f, 0f));

        Panel(band, "Cloth", Vector3.zero, new Vector3(15.6f, 2.0f, 0.06f),
              new Color32(0xC4, 0x45, 0x3E, 0xFF));
        Panel(band, "Hem_T", new Vector3(0f,  0.92f, -0.03f), new Vector3(15.6f, 0.16f, 0.05f),
              new Color32(0x9A, 0x33, 0x2E, 0xFF));
        Panel(band, "Hem_B", new Vector3(0f, -0.92f, -0.03f), new Vector3(15.6f, 0.16f, 0.05f),
              new Color32(0x9A, 0x33, 0x2E, 0xFF));

        // 글자 자리 — 흰 막대 다섯. 실제 글씨는 없지만 <b>글이 적힌 천</b>으로 읽힌다
        float[] wide = { 2.1f, 2.1f, 1.2f, 2.1f, 2.1f };
        float x = -5.2f;
        for (int i = 0; i < wide.Length; i++)
        {
            Panel(band, $"Word_{i}", new Vector3(x + wide[i] * 0.5f, 0.14f, -0.04f),
                  new Vector3(wide[i], 0.62f, 0.04f), new Color32(0xF3, 0xEC, 0xDC, 0xFF));
            x += wide[i] + 0.45f;
        }
        Panel(band, "Sub", new Vector3(0f, -0.52f, -0.04f), new Vector3(7.4f, 0.17f, 0.04f),
              new Color32(0xF3, 0xEC, 0xDC, 0xFF));

        // 매단 끈 — 현수막이 <b>걸려 있다</b>는 걸 끈이 말한다
        for (int i = -1; i <= 1; i += 2)
            Panel(band, $"Tie_{i}", new Vector3(i * 7.6f, 1.6f, 0f),
                  new Vector3(0.07f, 1.4f, 0.07f), new Color32(0x6B, 0x4A, 0x33, 0xFF));

        // ── 기둥에 붙은 작은 고지문 — 가까이서 보는 디테일 ──────────────
        var slip = new GameObject("Slip").transform;
        slip.SetParent(root.transform, false);
        slip.SetPositionAndRotation(new Vector3(-7f, 4.6f, 103.02f), Quaternion.Euler(0f, 182f, -3f));

        Panel(slip, "Back", Vector3.zero, new Vector3(1.25f, 1.75f, 0.05f),
              new Color32(0xEC, 0xE6, 0xD6, 0xFF));
        Panel(slip, "Band", new Vector3(0f, 0.63f, -0.035f), new Vector3(1.25f, 0.35f, 0.04f),
              new Color32(0xC4, 0x45, 0x3E, 0xFF));
        for (int i = 0; i < 5; i++)
            Panel(slip, $"Line_{i}", new Vector3(0f, 0.24f - i * 0.22f, -0.035f),
                  new Vector3(0.94f, 0.07f, 0.03f), new Color32(0x7B, 0x67, 0x52, 0xFF));
        Panel(slip, "Seal", new Vector3(0.36f, -0.67f, -0.04f), new Vector3(0.28f, 0.28f, 0.03f),
              new Color32(0xC4, 0x45, 0x3E, 0xFF));

        return root;
    }

    static void Panel(Transform parent, string name, Vector3 local, Vector3 size, Color c)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = local;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(c);
    }
}
