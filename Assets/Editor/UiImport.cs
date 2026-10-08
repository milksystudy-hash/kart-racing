using UnityEditor;

/// <summary>
/// ★ <c>Assets/Resources/UI/</c> 그림 설정. 화면에 거의 1:1 로 그리는 UI 라
/// <b>밉맵을 끄고 압축을 안 한다</b> — 초상화(<see cref="PortraitImport"/>)와 같은 판단이다.
///
/// <b><c>npotScale = None</c> 이 제일 중요하다.</b> 기본값(ToNearest)이면 2의 거듭제곱이
/// 아닌 그림을 <b>가장 가까운 정사각형으로 뭉갠다</b> — 초상화가 1086×1448 → 512×512 로
/// 눌렸던 그 함정이다(2026-09-28).
/// </summary>
public class UiImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace((char)92, '/').Contains("/Resources/UI/")) return;

        var im = (TextureImporter)assetImporter;
        im.textureType = TextureImporterType.Default;
        im.npotScale = TextureImporterNPOTScale.None;
        im.alphaIsTransparency = true;          // 끄면 투명 가장자리에서 색이 번져 테가 생긴다
        im.mipmapEnabled = false;
        im.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        im.filterMode = UnityEngine.FilterMode.Bilinear;
        im.maxTextureSize = 1024;
        im.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
