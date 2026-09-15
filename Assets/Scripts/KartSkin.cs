using UnityEngine;

/// <summary>
/// 로비에서 고른 캐릭터의 카트로 갈아입힌다.
///
/// 카트 모델을 씬 만들 때 하나로 박아두면, 누굴 골라도 그 카트로만 달리게 된다.
/// (실제로 세운이를 골라도 세진 카트가 나왔다.)
///
/// 그래서 있는 카트를 <b>전부</b> 껍데기 안에 넣어두고, 시작할 때 고른 것만 켠다.
/// 카트 한 대가 3천 삼각형 남짓이라 네 대를 다 들고 있어도 부담이 없고,
/// 이렇게 하면 레이스 도중에 바꾸는 것도 공짜다.
///
/// 어떤 카트가 있는지는 씬을 만들 때 정해져서 여기 목록으로 꽂힌다 —
/// 새 카트 FBX 를 만들면 TestSceneBuilder 의 표에 한 줄만 더하면 된다.
/// </summary>
public class KartSkin : MonoBehaviour
{
    [System.Serializable]
    public class Skin
    {
        [Tooltip("Cast.cs 의 id — 이감 / 시우 / 세운 / 세진")]
        public string castId = "";
        public GameObject model;

        [Tooltip("앞바퀴를 감싼 껍데기")]
        public Transform[] steerPivots;
        public Transform[] spinWheels;
        public Transform steeringWheel;
    }

    [Header("연결")]
    public KartWheels wheels;
    public Skin[] skins;

    [Tooltip("고른 캐릭터의 카트가 아직 없을 때 쓸 카트")]
    public string fallbackCastId = "세진";

    public string CurrentCastId { get; private set; } = "";

    void Awake()
    {
        Apply(GameSelection.SelectedCastId);
    }

    /// <summary>그 캐릭터의 카트만 켜고 나머지는 끈다.</summary>
    public void Apply(string castId)
    {
        if (skins == null || skins.Length == 0) return;

        var chosen = Find(castId) ?? Find(fallbackCastId) ?? skins[0];

        foreach (var skin in skins)
            if (skin != null && skin.model != null)
                skin.model.SetActive(skin == chosen);

        CurrentCastId = chosen.castId;

        if (wheels != null)
            wheels.Bind(chosen.steerPivots, chosen.spinWheels, chosen.steeringWheel);

        if (!string.IsNullOrEmpty(castId) && chosen.castId != castId)
            Debug.LogWarning($"[카트] '{castId}' 의 카트가 아직 없어서 '{chosen.castId}' 카트로 달린다. " +
                             "FBX 를 만들면 TestSceneBuilder 의 KartModels 표에 한 줄 더하면 된다.");
    }

    Skin Find(string castId)
    {
        if (string.IsNullOrEmpty(castId) || skins == null) return null;

        foreach (var skin in skins)
            if (skin != null && skin.castId == castId && skin.model != null) return skin;

        return null;
    }
}
