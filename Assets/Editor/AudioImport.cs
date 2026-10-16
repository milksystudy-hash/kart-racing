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
///
/// ★★★ 2026-10-16 — <b>오디오 임포터가 둘이었다.</b> 이 파일과 <c>MusicImport.cs</c> 가
///   같은 폴더를 보면서 <b>음악의 로드 방식을 정반대로</b> 정하고 있었고
///   (여기 CompressedInMemory ↔ 저기 Streaming), 나중에 도는 쪽이 이겼다.
///   그래서 이 파일을 고쳐도 <b>게임에는 아무 변화가 없었다</b> — 두 번 고치고서야 알았다.
///   <c>MusicImport.cs</c> 는 지웠다. <b>임포터는 폴더당 하나만 둬라.</b>
/// </summary>
public class AudioImport : AssetPostprocessor
{
    const string MusicFolder = "Assets/Resources/Music/";
    const string SfxFolder = "Assets/Resources/Sfx/";

    /// <summary>0.7 ≈ 112kbps. 배경음악에 충분하다.</summary>
    const float MusicQuality = 0.85f;

    /// <summary>효과음은 짧아서 품질을 조금 더 줘도 용량이 안 는다.</summary>
    const float SfxQuality = 0.8f;

    /// <summary>
    /// ★★ <b>이 숫자를 올려야 이미 들어온 파일이 다시 임포트된다.</b>
    ///   2026-10-16 에 로드 방식을 바꿔 놓고 이걸 안 올려서, 코드만 바뀌고
    ///   <b>.meta 는 옛 설정 그대로</b>였다 — 고친 게 게임에 하나도 안 들어갔다.
    ///   <b>이 파일의 설정을 건드리면 반드시 같이 올려라.</b>
    /// </summary>
    public override uint GetVersion() => 5;

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
                // ★★★ 2026-10-16 (두 번째) — <b>실시간 디코딩을 아예 없앤다.</b>
        //
        //   제작자: *"지지직이 컷씬 볼 때 계속 나오고, 전체화면이든 창이든 똑같다."*
        //   첫 수정(Streaming → CompressedInMemory)으로 조금 줄었을 뿐 안 없어졌다.
        //   둘 다 <b>재생하면서 Vorbis 를 푼다</b>는 점은 같아서다 — 디스크를 안 탈 뿐
        //   오디오 스레드가 계속 디코딩을 한다. 곡이 바뀔 때는 크로스페이드로
        //   <b>두 곡을 동시에</b> 푸니 약한 노트북에서 그 몫이 모자란다. 그게 지지직이다.
        //
        //   <b>DecompressOnLoad</b> 는 올릴 때 한 번 통째로 풀어 PCM 으로 들고 있는다.
        //   재생 중에는 메모리를 읽기만 하니 <b>끊길 데가 구조적으로 없다</b> —
        //   효과음이 한 번도 지지직거린 적 없는 이유가 이것이다.
        //
        //   값: 3분 스테레오 44.1kHz = 약 31MB. 한 번에 올라오는 곡이 한둘이라 60MB 안쪽이고,
        //   <b>디스크 용량은 그대로다</b>(파일은 Vorbis 로 압축된 채 들어간다).
        //   씬을 열 때 0.5초쯤 더 걸리는 게 전부야.
        s.loadType = AudioClipLoadType.DecompressOnLoad;
        // ★ 음악도 <b>미리 올린다.</b> CompressedInMemory 로 바꾸면서 같이 바뀐 조건이야 —
        //   미리 안 올리면 Play 를 부른 뒤에야 읽기 시작해서 <b>첫 몇 초가 비거나 끊긴다.</b>
        //   압축된 채로 들고 있으니 한 곡에 3MB 안팎이고, 한 번에 한두 곡만 올라온다.
        s.preloadAudioData = true;


        // 음악은 2D 로만 쓰고(카메라가 바뀌어도 소리는 그대로), 효과음도 이 프로젝트는 전부 2D 다.
        // 모노로 접으면 용량이 반이지만 <b>음악의 공간감이 죽는다</b> — 효과음만 접는다.
        // ★ 스테레오를 살린다 — 2D 로 틀어도 폭이 있다. (지운 MusicImport 가 이렇게 두고 있었고,
        //   제출 직전에 소리가 달라지는 변경을 끼워 넣지 않는다.)
        a.forceToMono = false;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        a.ambisonic = false;
        a.defaultSampleSettings = s;
        // ★ 백그라운드 로딩도 껐다. 미리 올리는 쪽으로 바꿨으니 둘을 같이 쓰면
        //   «아직 안 올라왔는데 Play» 가 되어 소리가 안 난다.
        a.loadInBackground = false;
    }
}
