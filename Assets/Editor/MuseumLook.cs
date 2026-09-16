using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 화면에 박물관 느낌을 입힌다. 물건을 더 만드는 게 아니라 <b>찍는 방식</b>을 바꾸는 쪽이야.
///
/// "유니티로 만든 티" 의 정체는 대개 이 셋이다:
///   1. <b>후처리가 꺼져 있다</b> — 색이 물감처럼 평평하고, 조명이 눈부시지 않고, 화면 가장자리가 안 가라앉는다
///   2. <b>계단 현상</b> — 모서리가 톱니 모양이다
///   3. <b>접촉 그림자가 없다</b> — 물건이 바닥에 놓인 게 아니라 떠 있는 것처럼 보인다
///
/// 셋 다 모델을 한 개도 안 고치고 해결된다. 그래서 FBX 가 들어오기 전인 지금 하는 게 맞아 —
/// 회색 상자도 이걸 입히면 회색 상자처럼 안 보인다.
///
/// <b>이 메뉴는 씬을 다시 짓지 않는다.</b> 없는 것만 더하고 저장한다. 여러 번 눌러도 같다.
/// </summary>
public static class MuseumLook
{
    const string SettingsFolder = "Assets/Settings";
    const string ProfilePath    = SettingsFolder + "/MuseumLook.asset";
    const string VolumeName     = "PostFX (박물관 룩)";

    static void ApplyToOpenSceneMenu()
    {
        int cameras = ApplyToOpenScene();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        Debug.Log($"[Racing] '{scene.name}' 에 박물관 룩을 입혔어. 카메라 {cameras}대에 후처리와 SMAA 를 켰다.\n" +
                  $"세기를 바꾸고 싶으면 {ProfilePath} 를 인스펙터에서 만지면 돼 — 코드 수정 없이 실시간으로 보인다.");
    }

    /// <summary>지금 열린 씬에 전역 볼륨과 카메라 설정을 보장한다. 씬 빌더들도 이걸 부른다.</summary>
    public static int ApplyToOpenScene()
    {
        var profile = EnsureProfile();
        TuneAmbientOcclusion();   // 씬 빌더로 들어와도 접촉 그림자가 같이 걸리게

        var volume = Object.FindFirstObjectByType<Volume>(FindObjectsInactive.Include);
        if (volume == null)
        {
            var go = new GameObject(VolumeName);
            volume = go.AddComponent<Volume>();
        }
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.weight = 1f;
        volume.sharedProfile = profile;

        int count = 0;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;

            // SMAA 는 MSAA 보다 싸고 URP 후처리 안에서 돈다. 약한 노트북 기준(1280×720 30fps)에 이게 맞아.
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.Medium;
            count++;
        }
        return count;
    }

    // ==================================================================
    //  볼륨 프로파일
    // ==================================================================
    public static VolumeProfile EnsureProfile()
    {
        var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder(SettingsFolder))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);

        // ---- 톤 매핑 : 밝은 곳이 하얗게 타버리지 않게 눌러준다 ----
        // ACES 는 대비가 너무 세서 저폴리 단색이 칙칙해진다. Neutral 이 이 그림체에 맞아.
        var tone = Add<Tonemapping>(profile);
        Set(tone.mode, TonemappingMode.Neutral);

        // ---- 색 보정 : 평평한 단색에 살짝 힘을 준다 ----
        // 대비를 세게 주면 어두운 구석이 통째로 뭉개진다. 실제로 재보니 대비 14 · 비네트 0.26 에서
        // 바닥 밝기가 0.231 → 0.047 로 다섯 배 떨어졌다. 전시실은 원래 어둑한 방이라 여유가 없어.
        var color = Add<ColorAdjustments>(profile);
        Set(color.postExposure, 0.22f);
        Set(color.contrast, 6f);
        Set(color.saturation, 8f);

        // ---- 화이트 밸런스 : 나무와 크림색이 따뜻하게 읽히도록 ----
        var white = Add<WhiteBalance>(profile);
        Set(white.temperature, 10f);
        Set(white.tint, -2f);

        // ---- 그림자·중간톤·하이라이트 : 그림자는 차게, 빛은 따뜻하게 ----
        // 실내 사진이 "찍은 사진처럼" 보이는 이유의 큰 부분이 이 색 갈림이야.
        // 넷째 값은 밝기 오프셋이다. 살짝 들어올려서 검정이 완전히 막히지 않게 —
        // 영화 화면이 게임 화면과 다르게 보이는 이유 중 하나가 이 "들린 검정" 이야.
        var smh = Add<ShadowsMidtonesHighlights>(profile);
        Set(smh.shadows,    new Vector4(0.94f, 0.97f, 1.08f, 0.02f));
        Set(smh.midtones,   new Vector4(1f, 1f, 1f, 0f));
        Set(smh.highlights, new Vector4(1.06f, 1.01f, 0.93f, 0f));

        // ---- 블룸 : 전시 조명과 등불이 실제로 눈부시게 ----
        var bloom = Add<Bloom>(profile);
        Set(bloom.threshold, 0.95f);
        Set(bloom.intensity, 0.5f);
        Set(bloom.scatter, 0.62f);
        Set(bloom.tint, new Color(1f, 0.94f, 0.84f));
        Set(bloom.highQualityFiltering, false);   // 약한 노트북 기준

        // ---- 비네트 : 가장자리를 가라앉혀 시선을 가운데로 ----
        // 약하게. 세게 걸면 화면 아래쪽(대개 바닥)이 통째로 까맣게 죽는다.
        var vignette = Add<Vignette>(profile);
        Set(vignette.intensity, 0.13f);
        Set(vignette.smoothness, 0.55f);
        Set(vignette.rounded, false);

        profile.isDirty = true;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Racing] {ProfilePath} 를 만들었어. 값은 인스펙터에서 바꿔도 된다.");
        return profile;
    }

    static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var found)) return found;

        var comp = ScriptableObject.CreateInstance<T>();
        comp.name = typeof(T).Name;
        comp.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

        profile.components.Add(comp);
        AssetDatabase.AddObjectToAsset(comp, profile);
        return comp;
    }

    /// <summary>볼륨 값은 "덮어쓰기" 를 켜야 실제로 먹는다. 매번 두 줄 쓰기 싫어서 묶어뒀다.</summary>
    static void Set<T>(VolumeParameter<T> parameter, T value)
    {
        parameter.overrideState = true;
        parameter.value = value;
    }

    // ==================================================================
    //  접촉 그림자 (SSAO)
    // ==================================================================
    /// <summary>
    /// 이미 켜져 있지만 세기가 약해서 거의 안 보인다. 방 크기에 맞게 올린다.
    /// 물건이 바닥에 <b>붙어 보이는</b> 건 대부분 이것 덕분이야 — 그림자를 켜는 것보다 훨씬 싸다.
    /// </summary>
    public static void TuneAmbientOcclusion()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:ScriptableRendererData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (data == null) continue;

            foreach (var feature in data.rendererFeatures)
            {
                if (feature == null || feature.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;

                var so = new SerializedObject(feature);
                TrySet(so, "m_Settings.Intensity", 0.85f);
                TrySet(so, "m_Settings.Radius", 0.45f);
                TrySet(so, "m_Settings.DirectLightingStrength", 0.35f);
                TrySetInt(so, "m_Settings.Downsample", 1);   // 반해상도 — 약한 노트북에서 공짜에 가깝게
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(data);
                Debug.Log($"[Racing] {path} 의 접촉 그림자(SSAO)를 진하게 올렸어.");
            }
        }
    }

    static void TrySet(SerializedObject so, string path, float value)
    {
        var p = so.FindProperty(path);
        if (p != null) p.floatValue = value;
    }

    static void TrySetInt(SerializedObject so, string path, int value)
    {
        var p = so.FindProperty(path);
        if (p == null) return;
        if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value != 0;
        else p.intValue = value;
    }

    // ==================================================================
    //  재질 마감 — 이미 만들어 둔 씬에 되돌릴 수 있게 입힌다
    // ==================================================================
    /// <summary>
    /// 지금 열린 씬의 <c>Flat_*</c> 머티리얼을 색에 맞는 마감판으로 바꿔 끼운다.
    /// 나무는 나무처럼, 돌은 돌처럼, 석등은 빛나게.
    ///
    /// <b>씬을 다시 만들지 않는다.</b> 네가 손으로 놓아 둔 물건은 그대로 있고,
    /// 머티리얼 참조만 갈아 끼우니까 Ctrl+Z 로 되돌릴 수도 있다. 로비처럼 이미 꾸며 둔
    /// 씬을 다듬을 때 쓰라고 만든 거야 — 캠퍼스/트랙은 실행할 때 알아서 이렇게 붙는다.
    ///
    /// 이름이 <c>Flat_</c> 로 시작하는 것만 건드린다. 네가 넣은 FBX 의 재질은
    /// 색이 우연히 겹쳐도 절대 안 바뀐다.
    /// </summary>
    // 메뉴에는 안 건다 — 유저가 메뉴는 씬 만들기 셋만 두라고 했다(2026-09-16).
    // 세 빌더가 마지막에 스스로 부르니까 구워 나오는 씬은 항상 다듬어진 상태야.
    public static void RefineMaterials()
    {
        int changed = 0, already = 0, matte = 0;

        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,
                                                            FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials;
            bool touched = false;

            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || !m.name.StartsWith("Flat_")) continue;
                if (!m.HasProperty("_BaseColor")) continue;

                Color c = m.GetColor("_BaseColor");
                Finish finish = FlatMaterial.FinishFor(c);
                if (finish == Finish.무광) { matte++; continue; }

                var refined = TestSceneBuilder.MaterialAsset(c, finish);
                if (refined == null || refined == m) { already++; continue; }

                mats[i] = refined;
                touched = true;
                changed++;
            }

            if (!touched) continue;
            Undo.RecordObject(r, "재질 다듬기");
            r.sharedMaterials = mats;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[재질] {changed}개 바꿈 · 이미 맞음 {already} · 무광 그대로 {matte}. " +
                  "Ctrl+S 로 저장, 마음에 안 들면 Ctrl+Z");
    }
}
