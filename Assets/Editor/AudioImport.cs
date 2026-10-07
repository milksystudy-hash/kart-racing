using UnityEngine;
using UnityEditor;

/// <summary>
/// ★★ 음악·효과음 임포트 설정. <b>유저에게 인스펙터를 시키지 않는다</b>(기획서 §9.3) —
/// 폴더에 파일을 드래그하는 것으로 끝나야 한다.
///
/// 2026-10-07 에 넣은 이유는 <b>용량</b>이다. <c>Resources</c> 폴더는 <b>쓰든 안 쓰든
/// 통째로 빌드에 들어간다.</b> 음악 14곡이 전부 무압축 WAV 라 <b>429MB</b> 였고,
/// 그대로 빌드하면 게임이 450MB 를 넘는다(예전 빌드는 음악이 없어서 119MB 였다).
///
/// <list type="bullet">
/// <item><b>Vorbis 압축</b> — 배경음악의 표준이다. 품질 0.7 이면 112kbps 쯤이라
///       <b>무압축의 1/10</b> 이 되고, 깔리는 소리라 차이를 못 듣는다.</item>
/// <item><b>Streaming</b>(음악만) — 3분짜리 곡을 통째로 메모리에 올리지 않고
///       디스크에서 흘려 읽는다. 약한 노트북이 목표라(§7.6) 이쪽이 맞다.</item>
/// <item>효과음은 <b>DecompressOnLoad</b> — 짧고, 눌렀을 때 바로 나야 한다.
///       스트리밍으로 두면 첫 재생이 늦는다.</item>
/// </list>
///
/// ★ 음질이 아쉬우면 <see cref="MusicQuality"/> 만 올리면 된다. 1.0 은 거의 무손실이고
///   용량은 세 배쯤 된다. 파일은 한 개도 안 건드린다 — 임포트 설정일 뿐이야.
/// </summary>
public class AudioImport : AssetPostprocessor
{
    const string MusicFolder = "Assets/Resources/Music/";
    const string SfxFolder = "Assets/Resources/Sfx/";

    /// <summary>0.7 ≈ 112kbps. 배경음악에 충분하다.</summary>
    const float MusicQuality = 0.7f;

    /// <summary>효과음은 짧아서 품질을 조금 더 줘도 용량이 안 는다.</summary>
    const float SfxQuality = 0.8f;

    public override uint GetVersion() => 2;

    void OnPreprocessAudio()
    {
        bool music = assetPath.StartsWith(MusicFolder);
        bool sfx = assetPath.StartsWith(SfxFolder);
        if (!music && !sfx) return;

        var a = (AudioImporter)assetImporter;

        var s = a.defaultSampleSettings;
        // ★ 효과음은 <b>압축하지 않는다.</b> 1초도 안 되는 소리라 PCM 이어도 다 합쳐 1MB 미만이고,
        //   디코딩이 없어서 <b>누른 순간 바로 난다.</b> 압축은 음악에만 의미가 있다.
        s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        s.quality = music ? MusicQuality : SfxQuality;
        s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        // 음악은 흘려 읽으니 미리 올릴 필요가 없다. 효과음은 미리 올려야 바로 난다
        s.preloadAudioData = !music;
        a.defaultSampleSettings = s;

        // 음악은 2D 로만 쓰고(카메라가 바뀌어도 소리는 그대로), 효과음도 이 프로젝트는 전부 2D 다.
        // 모노로 접으면 용량이 반이지만 <b>음악의 공간감이 죽는다</b> — 효과음만 접는다.
        a.forceToMono = sfx;
        a.loadInBackground = music;
    }
}
