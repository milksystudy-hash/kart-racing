using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 명예 후원자 동상 FBX 두 개의 임포트 설정 — <b>유저에게 인스펙터를 시키지 않는다</b>(기획서 §9.3).
///
/// ★★ <b>받은 원본을 그대로 쓰면 UV 가 뒤섞여 «얼룩무늬 위장복» 으로 나온다.</b>
/// 블렌더로 열어 보면 같은 파일이 멀쩡히 청동으로 보이니(평면광 렌더로 확인) 기하도 UV 도
/// 문제가 없고, <b>유니티의 FBX 리더가 이 파일의 UV 를 못 읽는 것</b>이다.
/// 그래서 <b>블렌더를 통역으로 한 번 거쳐</b> 다시 내보낸 파일을 쓴다 —
/// 이 프로젝트가 카트·곰·소품에서 늘 쓰던 그 경로야(스크래치패드 <c>statue_reexport.py</c>).
/// 덤으로 파일이 27MB → <b>0.2MB</b> 가 됐다(텍스처가 옆 폴더의 PNG 로 빠졌다).
///
/// 텍스처를 <b>파일로</b> 두는 이유: 박힌 채로는 크기·타입을 따로 못 준다.
/// base_color 는 조각이 수백 개인 아틀라스라 1024 로 줄면 섬끼리 번져서 또 얼룩이 된다.
/// </summary>
public static class StatueImport
{
    public const string Dir = "Assets/Resources/Statue";
    public static readonly string[] Files = { "Statue_Muscle", "Statue_Real" };

    // 개발용 — 메뉴에는 안 올린다(배치모드로 부른다)
    public static void Menu() => Ensure(true);

    /// <summary>이미 맞춰져 있으면 아무 것도 안 한다. 여러 번 불러도 안전하다.</summary>
    public static void Ensure(bool log = false)
    {
        foreach (var name in Files)
        {
            // ── 텍스처부터 (모델보다 먼저여야 재질이 제대로 물린다) ──────────
            string tex = $"{Dir}/Textures_{name.Replace("Statue_", string.Empty)}";
            if (Directory.Exists(tex))
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { tex }))
                {
                    string tp = AssetDatabase.GUIDToAssetPath(guid);
                    var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
                    if (ti == null) continue;
                    bool td = false;

                    // normal 을 그냥 두면 <b>동상이 파랗게 칠해진다</b>
                    bool isNormal = Path.GetFileNameWithoutExtension(tp).ToLowerInvariant().Contains("normal");
                    var want = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    if (ti.textureType != want) { ti.textureType = want; td = true; }

                    // 크기는 <b>아틀라스가 얼마나 잘게 쪼개졌나</b>로 정한다.
                    // 법선은 면 단위로 완만해서 1024 로 줄여도 안 티 난다.
                    int size = isNormal ? 1024 : 2048;
                    if (ti.maxTextureSize != size) { ti.maxTextureSize = size; td = true; }
                    if (ti.textureCompression != TextureImporterCompression.CompressedHQ)
                    { ti.textureCompression = TextureImporterCompression.CompressedHQ; td = true; }

                    if (td) { ti.SaveAndReimport(); if (log) Debug.Log($"[동상] {tp} 맞춤"); }
                }

            // ── 모델 ────────────────────────────────────────────────────
            string path = $"{Dir}/{name}.fbx";
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null)
            {
                if (log) Debug.LogWarning($"[동상] {path} 이 없다.");
                continue;
            }

            bool dirty = false;
            // External 이면 맞는 .mat 이 없을 때 <b>새하얗게</b> 나온다(카트·곰에서 두 번 겪었다)
            if (mi.materialLocation != ModelImporterMaterialLocation.InPrefab)
            { mi.materialLocation = ModelImporterMaterialLocation.InPrefab; dirty = true; }
            if (mi.importCameras || mi.importLights) { mi.importCameras = mi.importLights = false; dirty = true; }
            if (mi.addCollider) { mi.addCollider = false; dirty = true; }   // 콜라이더는 기단에만
            if (mi.importAnimation) { mi.importAnimation = false; dirty = true; }
            if (mi.animationType != ModelImporterAnimationType.None)
            { mi.animationType = ModelImporterAnimationType.None; dirty = true; }

            if (dirty) { mi.SaveAndReimport(); if (log) Debug.Log($"[동상] {name} 임포트 설정 맞춤"); }
        }
        AssetDatabase.Refresh();
    }
}
