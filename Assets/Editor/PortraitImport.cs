using UnityEditor;
using UnityEngine;

/// <summary>
/// <b>초상화는 폴더에 넣기만 하면 설정이 맞춰진다.</b>
///
/// 2026-09-28 유저가 첫 초상화 세 장(세진 기본·기쁨·당황)을 그려 왔다.
/// 유니티 기본 임포트 설정으로 들어오면 두 가지가 어긋난다:
///
/// <list type="number">
/// <item><b>`alphaIsTransparency` 가 꺼져 있다.</b> 그러면 투명한 가장자리에서 색이
///       번져서 <b>인물 둘레에 검은 테</b>가 생긴다 — 그림 잘못으로 보이지만 임포트 설정이다.</item>
/// <item><b>밉맵이 켜져 있다.</b> 화면에 거의 1:1 로 그리는 UI 그림이라 밉맵은
///       메모리만 33% 더 먹고 <b>축소될 때 흐려지기만</b> 한다.</item>
/// </list>
///
/// 유저에게 인스펙터를 만지라고 시키지 않는다(기획서 §9.3) — 그래서 <see cref="AssetPostprocessor"/>
/// 다. 앞으로 그림을 스무 장 더 넣어도 <b>드래그만 하면 끝</b>이야.
/// </summary>
public class PortraitImport : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Portraits/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;

        var t = (TextureImporter)assetImporter;

        // `GUI.DrawTexture` 로 그리니 Sprite 가 아니라 Default 가 맞다.
        t.textureType = TextureImporterType.Default;
        t.alphaIsTransparency = true;
        t.alphaSource = TextureImporterAlphaSource.FromInput;
        t.mipmapEnabled = false;
        t.wrapMode = TextureWrapMode.Clamp;      // 가장자리가 반대편으로 말리지 않게
        t.filterMode = FilterMode.Bilinear;

        // ★★ <b>이게 없으면 그림이 정사각형으로 찌부러진다.</b>
        // 유니티 기본값(`ToNearest`)은 2의 거듭제곱이 아닌 텍스처를 <b>가장 가까운 정사각 POT</b>
        // 로 리사이즈한다 — 1029 × 1371(3:4)이 <b>512 × 512</b> 로 들어와서 세로가 눌렸다.
        // 측정으로 잡았다: 화면 칸이 218 × 265 인데 그림이 218 × <b>218</b> 로 나왔다.
        // 초상화는 비율이 곧 얼굴이라 여기서 어긋나면 <b>사람이 다르게 생겨 보인다.</b>
        t.npotScale = TextureImporterNPOTScale.None;

        // 화면 칸은 최대 224 × 1.8(스케일 상한) ≈ <b>403px</b> 이라 512 면 충분하다.
        // 원본 1029px 을 그대로 들고 있으면 한 장에 5.6MB 고, 일곱 명 × 세 장이면 118MB 다.
        t.maxTextureSize = 512;

        // 선화 그림이라 DXT5 압축이 <b>가장자리 계단</b>으로 보인다. 512 로 줄였으니
        // 무압축이어도 한 장 1.3MB — 스물한 장에 28MB 면 감당할 수 있다.
        t.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
