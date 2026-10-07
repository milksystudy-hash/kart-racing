using UnityEditor;

/// <summary>
/// 레이스 연출 프롭 열 종의 임포트 설정. <b>유저에게 인스펙터를 시키지 않는다</b>(기획서 §9.3) —
/// 폴더에 FBX 를 드래그하는 것으로 끝나야 한다.
///
/// ★ <c>materialLocation</c> 이 <c>External</c> 이면 짝이 맞는 .mat 이 없을 때
/// <b>모델이 통째로 새하얗게</b> 나온다. 카트 · 곰 · 로비 소품에서 이미 세 번 겪은 함정이야.
/// </summary>
public class RacePropImport : AssetPostprocessor
{
    const string Folder = "Assets/Resources/RaceProps/";

    public override uint GetVersion() => 1;

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Folder)) return;

        var m = (ModelImporter)assetImporter;
        m.materialLocation = ModelImporterMaterialLocation.InPrefab;
        m.importCameras = false;
        m.importLights = false;
        m.importAnimation = false;        // 클립은 안 쓴다 — 코드가 트랜스폼을 돌린다
        m.importConstraints = false;
        m.addCollider = false;            // 전부 장식. 주행에 영향이 0 이어야 한다
        m.globalScale = 1f;               // 크기는 블렌더에서 맞춰 왔다(열 종 전부 사양서대로)
        m.importBlendShapes = false;
        m.importVisibility = false;
    }
}
