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

    /// <summary>
    /// 첫 화면 배경(<see cref="TitleScreen"/>). 초상화와 <b>거의 같은 설정인데 크기만 다르다</b> —
    /// 화면을 가득 채우는 그림이라 512 로 줄이면 뭉개진다.
    /// </summary>
    const string Backdrops = "Assets/Resources/Backgrounds/";

    /// <summary>
    /// 코스의 골든베어 광고. 초상화·배경과 달리 <b>3D 면에 붙는</b> 그림이라
    /// 밉맵을 켠다 — 안 켜면 멀리서 볼 때 글자가 지글거린다.
    /// </summary>
    const string Ads = "Assets/Resources/Ads/";

    /// <summary>
    /// 이야기 장면 배경(<see cref="DialogueHUD"/>). 첫 화면 배경과 <b>똑같은 설정</b>이다 —
    /// 둘 다 화면을 가득 채우는 그림이라 2048 무압축.
    /// 파일 이름이 곧 장면 id: <c>prologue.png · ch1.png …</c>
    /// </summary>
    const string Story = "Assets/Resources/StoryBackdrops/";

    /// <summary>
    /// ★ <b>이 번호를 올리면 유니티가 이 임포터가 다루는 에셋을 전부 다시 들인다.</b>
    /// 설정을 고쳐 놓고 파일을 안 건드리면 유니티는 «바뀐 게 없다» 며 그냥 넘어간다 —
    /// 파일 날짜만 바꿔서는 안 되고(해시로 보니까), 이 번호가 정석이다.
    ///
    ///   2 — 2026-10-02 배경에 밉맵을 켰다(지지직거림)
    /// </summary>
    public override uint GetVersion() => 2;

    void OnPreprocessTexture()
    {
        bool portrait = assetPath.StartsWith(Folder);
        bool backdrop = assetPath.StartsWith(Backdrops) || assetPath.StartsWith(Story);
        bool ad = assetPath.StartsWith(Ads);
        if (!portrait && !backdrop && !ad) return;

        var t = (TextureImporter)assetImporter;

        // `GUI.DrawTexture` 로 그리니 Sprite 가 아니라 Default 가 맞다.
        t.textureType = TextureImporterType.Default;
        t.alphaIsTransparency = true;
        t.alphaSource = TextureImporterAlphaSource.FromInput;
        // ★★ 2026-10-02 — <b>배경도 밉맵을 켠다.</b> 유저: *"대화할 때 지지직거린다."*
        //   배경 그림이 1672~2560 px 인데 화면은 그보다 작다. 밉맵이 없으면 축소할 때
        //   원본 픽셀을 띄엄띄엄 집어서 <b>가장자리가 들끓는다</b> — 게다가 장소가 넘어갈 때
        //   1.04배에서 당겨 들어오니까 배율이 매 프레임 바뀌어서 더 심하다.
        //   초상화는 그대로 끈다 — 거의 1:1 로 그려서 밉맵은 흐리게만 만든다.
        t.mipmapEnabled = ad || backdrop;
        t.mipMapBias = -0.4f;            // 밉맵을 켜되 조금 선명한 쪽으로 당긴다
        t.filterMode = FilterMode.Trilinear;
        t.wrapMode = TextureWrapMode.Clamp;      // 가장자리가 반대편으로 말리지 않게

        // ★★ <b>이게 없으면 그림이 정사각형으로 찌부러진다.</b>
        // 유니티 기본값(`ToNearest`)은 2의 거듭제곱이 아닌 텍스처를 <b>가장 가까운 정사각 POT</b>
        // 로 리사이즈한다 — 1029 × 1371(3:4)이 <b>512 × 512</b> 로 들어와서 세로가 눌렸다.
        // 측정으로 잡았다: 화면 칸이 218 × 265 인데 그림이 218 × <b>218</b> 로 나왔다.
        // 초상화는 비율이 곧 얼굴이라 여기서 어긋나면 <b>사람이 다르게 생겨 보인다.</b>
        t.npotScale = TextureImporterNPOTScale.None;

        // 화면 칸은 최대 224 × 1.8(스케일 상한) ≈ <b>403px</b> 이라 512 면 충분하다.
        // 원본 1029px 을 그대로 들고 있으면 한 장에 5.6MB 고, 일곱 명 × 세 장이면 118MB 다.
        //
        // 배경은 반대로 <b>화면을 가득 채우니</b> 2048 이 필요하다 — 1080p 가로 1920 을
        // 덮고도 남는 첫 2의 거듭제곱이야. 2560 × 1440 으로 그려 오면 2048 × 1152 로 들어온다.
        t.maxTextureSize = backdrop ? 2048 : ad ? 1024 : 512;

        // 선화 그림이라 DXT5 압축이 <b>가장자리 계단</b>으로 보인다. 512 로 줄였으니
        // 무압축이어도 한 장 1.3MB — 스물한 장에 28MB 면 감당할 수 있다.
        //
        // 배경도 같은 화풍(평면 색 + 굵은 선)이라 무압축으로 둔다. 2048 × 1152 RGBA 가
        // 9.4MB 인데, <b>게임을 켜면 제일 먼저 보는 화면</b>이라 여기서 띠가 지면 안 된다.
        // 용량이 급하면 이 한 줄만 CompressedHQ 로 바꾸면 8분의 1 이 된다.
        // 광고판은 사진 같은 그림이라 압축해도 안 티 난다 — 1024² 무압축이면 4MB 다.
        t.textureCompression = ad ? TextureImporterCompression.CompressedHQ
                                  : TextureImporterCompression.Uncompressed;
    }
}
