using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ★★ <b>중앙홀 서쪽 방송 화면에 글자를 띄운다.</b> (2026-10-15 수연 요청)
///
/// 수연: *"큰 칠판에 아무것도 안 써져 있어? 뭐라도 좀 쓸까. 그냥 냅두기에는 좀 그런데."*
///
/// <b>맞는 지적이다.</b> 7 × 3.6m 짜리 검은 판이 홀 한쪽 벽을 통째로 차지하고 있는데
/// 아무것도 안 적혀 있으면, 그건 «방송 화면» 이 아니라 <b>안 만들다 만 벽</b>으로 읽힌다.
///
/// ── 무엇을 적나 ───────────────────────────────────────────────────────
/// <b>철거 안내 방송</b>이다. 플레이어가 홀에 처음 들어왔을 때 이 게임이 무슨 상황인지를
/// <b>대사 한 줄 없이</b> 알려주는 자리고, 다 깨고 나면 <b>그 자리가 바뀌는 것</b>으로
/// 이겼다는 게 한 번 더 보인다(폐과 딱지 · 먼지 덮개와 같은 문법).
///
/// ── 설계 ───────────────────────────────────────────────────────────────
/// · <b>스스로 씬에 들어온다</b> — 로비를 다시 구울 필요가 없다.
///   「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」를 이 프로젝트에서 다섯 번 겪었다.
/// · 글자는 <see cref="BuildingSign.TextMaterial"/> 를 쓴다 — 현판·시계와 같은 셰이더라
///   <b>벽 뒤에서 비치지 않는다</b>(기본 폰트 재질은 <c>ZTest Always</c> 라 뚫고 보인다).
/// · 판이 <b>+X(홀 안쪽)</b> 를 보고 서 있으니 글자도 그쪽으로 돌린다 —
///   방향은 짐작하지 않고 <b>홀 한가운데를 향하게</b> 계산한다.
/// </summary>
public class BroadcastBoard : MonoBehaviour
{
    static readonly string[] Before =
    {
        "철거 예정 안내",
        "안전진단 결과에 따라",
        "금월 내 철거 예정",
        "양해를 바랍니다",
    };

    static readonly string[] After =
    {
        "재개관 안내",
        "철거 계획이 백지화되었습니다",
        "전시실을 다시 엽니다",
        "찾아주셔서 고맙습니다",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Install();
    }

    static void OnLoaded(Scene s, LoadSceneMode m) => Install();

    static void Install()
    {
        if (Object.FindFirstObjectByType<BroadcastBoard>() != null) return;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                              FindObjectsSortMode.None))
            if (t.name == "BroadcastScreen") { t.gameObject.AddComponent<BroadcastBoard>(); return; }
    }

    bool wonWhenBuilt;

    void Start() => Build();

    void Update()
    {
        // 다 깨면 그 자리에서 문구가 바뀐다 — 로비를 나갔다 올 필요가 없다.
        if (GrandFinal.FreeRun != wonWhenBuilt) Build();
    }

    void Build()
    {
        var old = transform.Find("Lines");
        if (old != null) Destroy(old.gameObject);

        var font = Resources.Load<Font>("HudFont");
        if (font == null) return;

        wonWhenBuilt = GrandFinal.FreeRun;
        string[] lines = wonWhenBuilt ? After : Before;

        var holder = new GameObject("Lines").transform;
        holder.SetParent(transform, false);
        holder.localPosition = new Vector3(0.36f, 0f, 0f);     // 판 앞면(0.28)에서 8cm

        // ★★ <b>TextMesh 는 제 forward 의 «반대쪽» 에서 읽힌다.</b> 보는 사람을
        //   <b>등지게</b> 돌려야 글자가 바로 보인다 — 홀 쪽으로 돌리면 뒤에서 본 꼴이라
        //   좌우가 뒤집힌다. <see cref="BuildingSign.Label"/> 이 180도를 더하는 것과 같은 이유.
        //
        // ★★★ 2026-10-15 — <b>홀 한가운데가 아니라 「판 자신의 축」에 맞춘다.</b>
        //   수연: *"글자 잘렸어."* 한가운데를 보게 돌렸더니 화면이 z −2 에 있어서
        //   정면이 아니라 <b>6.5도 비뚤어졌다.</b> 5.6m 짜리 줄이면 끝이 0.32m 밀리는데
        //   글자는 판에서 0.02m 밖에 안 떠 있었으니 <b>오른쪽 끝이 판 속으로 파고들었다.</b>
        //   판은 +X 를 보고 납작하게 서 있으니 글자는 <b>정확히 −X</b> 를 보면 된다.
        holder.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

        // 제목 한 줄 + 본문 세 줄. 제목만 크고 밝다.
        const float Title = 0.62f, Body = 0.40f, Gap = 0.58f;
        float top = (lines.Length - 1) * 0.5f * Gap + 0.30f;

        for (int i = 0; i < lines.Length; i++)
        {
            float size = i == 0 ? Title : Body;
            var go = new GameObject($"Line_{i}");
            go.transform.SetParent(holder, false);
            go.transform.localPosition = new Vector3(0f, top - i * Gap - (i == 0 ? 0f : 0.18f), 0f);

            var text = go.AddComponent<TextMesh>();
            text.font = font;
            text.text = lines[i];
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 120;                      // 크게 구워서 줄인다 — 작게 구우면 계단이 보인다
            text.characterSize = size * 10f / 120f;   // 한 줄 월드 높이 = fontSize × characterSize / 10
            text.color = i == 0
                ? (wonWhenBuilt ? new Color32(0xB8, 0xE6, 0xC8, 0xFF)   // 재개관 — 푸른 기
                                : new Color32(0xE8, 0x9A, 0x8A, 0xFF))  // 철거 — 붉은 기
                : new Color32(0xD6, 0xCD, 0xBA, 0xFF);

            go.GetComponent<MeshRenderer>().sharedMaterial = BuildingSign.TextMaterial(font);
        }
    }
}
