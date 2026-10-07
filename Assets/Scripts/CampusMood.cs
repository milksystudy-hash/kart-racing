using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캠퍼스가 <b>이야기에 반응한다.</b> 수집품을 모을수록 폐과 딱지가 하나씩 떨어지고,
/// 빛이 차가운 쪽에서 따뜻한 쪽으로 옮겨간다.
///
/// 유저 요청(2026-09-17): *"레이스+스토리 다 깨기 전과 다 깨고 미니게임 들어갈 때 연출이
/// 달라져야 할 것 같다."* 맞는 말이야. 여덟 판을 도는 동안 <b>화면이 하나도 안 변하면</b>
/// 이긴다는 게 뭔지 알 수가 없다. 마지막에 한 번 확 바뀌는 것보다, 한 판마다 조금씩
/// 되살아나는 게 낫다 — 그래야 다음 판을 도는 이유가 화면에 있다.
///
/// <b>기하는 하나도 안 만들고 안 부순다.</b> 이미 서 있는 현판의 딱지와 조명 색만 건드린다.
/// 그래서 공짜고, 씬을 다시 구울 필요도 없다.
///
/// 딱지는 <b>웅지관(행정)에는 절대 안 붙는다.</b> 행정동은 없어지지 않는다는 게 이 이야기의
/// 농담이고, 플레이어가 그걸 알아채면 더 좋다.
/// </summary>
[DefaultExecutionOrder(40)]
public class CampusMood : MonoBehaviour
{
    [Tooltip("이 이름이 든 건물에는 폐과 딱지를 안 붙인다")]
    public string neverClosed = "웅지관";

    /// <summary>
    /// ★★ 2026-10-06 <b>여덟 개를 다 모아도 이 한 동은 끝내 안 열린다.</b>
    ///
    /// 전에는 8/8 이면 열두 장이 전부 떨어졌다 — 깔끔한 승리고, 그래서 <b>아무것도 안 남는다.</b>
    /// 한 장을 남기면 「이겼지만 전부는 아니다」가 <b>기하를 하나도 안 만들고</b> 생긴다.
    ///
    /// 하필 <b>기념관</b>인 게 요점이다. 캠퍼스는 살렸는데 <b>그걸 기억하는 자리</b>가 안 돌아왔다 —
    /// 안에 들어가면 「환웅의 발자취」 수첩이 그대로 있는데 바깥 현판에는 딱지가 붙어 있다.
    /// 설명은 한 줄도 안 한다. 끝까지 안 떨어지는 딱지 한 장이 그 말을 대신한다.
    ///
    /// 비워 두면 전처럼 전부 떨어진다.
    /// </summary>
    [Tooltip("여덟 개를 다 모아도 이 건물의 폐과 딱지는 끝내 안 떨어진다 — 비우면 전부 떨어진다")]
    public string neverReopens = "대충기념관";

    [Header("철거 위기 — 수집품 0개")]
    [Tooltip("탁하고 서늘한 쪽. 어둡게는 하지 않는다 — 어두우면 안 보일 뿐 슬프지 않다")]
    public Color coldFog = new Color(0.52f, 0.52f, 0.52f);
    public Color coldAmbient = new Color(0.42f, 0.43f, 0.46f);
    public float coldSun = 0.85f;

    [Header("지켜냄 — 수집품 전부")]
    public Color warmFog = new Color(0.60f, 0.55f, 0.46f);
    public Color warmAmbient = new Color(0.56f, 0.52f, 0.44f);
    public float warmSun = 1.15f;

    [Tooltip("몇 초에 걸쳐 옮겨가는지. 한 프레임에 바뀌면 설정을 만진 것처럼 보인다")]
    public float fadeSeconds = 2.5f;

    readonly List<BuildingSign> signs = new List<BuildingSign>();
    Light sun;
    float shown = -1f;
    int lastCount = -1;

    void Start()
    {
        foreach (var sign in FindObjectsByType<BuildingSign>(FindObjectsSortMode.None))
            if (!string.IsNullOrEmpty(sign.buildingName) && sign.buildingName != neverClosed)
                signs.Add(sign);

        // 이름 순으로 정렬해야 딱지가 매번 같은 순서로 떨어진다.
        // 안 그러면 다시 켤 때마다 다른 건물이 살아나서 "내가 저길 살렸다" 가 안 남는다.
        signs.Sort((a, b) => string.CompareOrdinal(a.buildingName, b.buildingName));

        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional) { sun = light; break; }

        shown = Saved;
        ApplyLight(shown);
        ApplyStickers(shown, animate: false);
    }

    void Update()
    {
        float goal = Saved;

        // 세이브 값이 바뀌면 딱지를 다시 계산한다. 로비의 F9/F10 로 바꿔도 바로 보이게.
        if (lastCount != CollectionState.Count)
        {
            lastCount = CollectionState.Count;
            ApplyStickers(goal, animate: true);
        }

        if (Mathf.Approximately(shown, goal)) return;

        shown = Mathf.MoveTowards(shown, goal, Time.deltaTime / Mathf.Max(0.1f, fadeSeconds));
        ApplyLight(shown);
    }

    /// <summary>0(철거 위기) ~ 1(지켜냄).</summary>
    static float Saved => ExhibitCatalogue.Count <= 0 ? 0f
        : Mathf.Clamp01(CollectionState.Count / (float)ExhibitCatalogue.Count);

    void ApplyLight(float t)
    {
        RenderSettings.fogColor = Color.Lerp(coldFog, warmFog, t);
        RenderSettings.ambientLight = Color.Lerp(coldAmbient, warmAmbient, t);
        if (sun != null) sun.intensity = Mathf.Lerp(coldSun, warmSun, t);

        // 하늘(카메라 배경)은 안개와 <b>같은 값</b>이어야 한다. 다르면 먼 벽이 하늘 띠처럼 보인다.
        foreach (var cam in Camera.allCameras)
            if (cam.clearFlags == CameraClearFlags.SolidColor)
                cam.backgroundColor = RenderSettings.fogColor;
    }

    /// <summary>
    /// 살아남은 건물 수 = 전체 × 진행도. <b>한 판에 한 동씩 딱지가 떨어진다.</b>
    /// 수집품 8개에 건물 12동이라 한 판에 한 동 넘게 살아나는데, 그게 맞아 —
    /// 여덟 번 달려서 열두 동을 되살리는 게 여덟 동만 되살리는 것보다 이긴 느낌이 크다.
    /// </summary>
    void ApplyStickers(float t, bool animate)
    {
        int alive = Mathf.RoundToInt(signs.Count * t);
        for (int i = 0; i < signs.Count; i++)
        {
            // <b>경계에서부터 퍼져 나가게</b> 순서를 준다 — 방금 살아난 건물이 먼저 떨어지고
            // 멀리 있는 것이 나중이라, 눈이 «어디서 시작됐는지» 를 따라갈 수 있다.
            float delay = animate ? Mathf.Abs(i - alive) * Reveal.Step : -1f;

            // ★ 한 동은 끝내 안 열린다 — <see cref="neverReopens"/> 참고
            bool closed = i >= alive
                       || (!string.IsNullOrEmpty(neverReopens)
                           && signs[i].buildingName == neverReopens);
            signs[i].SetClosed(closed, delay);
        }
    }
}
