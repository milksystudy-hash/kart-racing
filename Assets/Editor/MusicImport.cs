using UnityEditor;
using UnityEngine;

/// <summary>
/// <c>Assets/Resources/Music/</c> 에 떨어진 음악 파일의 임포트 설정 —
/// <b>유저에게 인스펙터를 시키지 않는다</b>(기획서 §9.3).
///
/// WAV 를 그대로 두면 <b>32MB 가 통째로 빌드에 들어가고 메모리에도 전부 올라간다.</b>
/// 3분짜리 루프라 <c>Streaming</c> + <c>Vorbis</c> 로 바꾸면 빌드가 3MB 쯤 되고
/// 로딩 때 멈칫하지도 않는다.
///
/// <see cref="PortraitImport"/> 과 같은 방식이라 <b>앞으로 곡을 몇 개 더 넣어도
/// 폴더에 드래그만 하면 된다.</b>
/// </summary>
public class MusicImport : AssetPostprocessor
{
    const string Dir = "Assets/Resources/Music/";
    const string SfxDir = "Assets/Resources/Sfx/";

    void OnPreprocessAudio()
    {
        bool sfx = assetPath.StartsWith(SfxDir);
        if (!sfx && !assetPath.StartsWith(Dir)) return;
        var im = (AudioImporter)assetImporter;

        var s = im.defaultSampleSettings;
        // ★ 효과음과 음악은 <b>반대로</b> 넣는다. 0.8초짜리를 스트리밍으로 두면
        //   누를 때마다 디스크를 읽어 <b>소리가 늦게 난다</b> — 문이 이미 열린 뒤에 «스르륵» 이 난다.
        //   짧은 건 통째로 메모리에 올리고(비압축) 길은 건 흘려보낸다.
        s.loadType = sfx ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.Streaming;
        s.compressionFormat = sfx ? AudioCompressionFormat.PCM : AudioCompressionFormat.Vorbis;
        s.quality = 0.7f;              // 음악은 0.7 이면 귀로 구분이 안 된다
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        s.preloadAudioData = sfx;      // 효과음은 미리 올려 둔다 — 늦게 나면 소리가 아니라 메아리다
        im.defaultSampleSettings = s;

        im.forceToMono = false;        // 스테레오를 살린다 — 2D 로 틀어도 폭이 있다
        im.loadInBackground = !sfx;    // 씬 로딩을 안 붙잡는다
        im.ambisonic = false;
    }
}
