using UnityEngine;

/// <summary>
/// 한옥 지붕을 <b>안에서 올려다본 모습</b>으로 짓는다. 로비 · 트랙 · 전시실이 같은 걸 쓴다
/// (2026-09-16, "세 개 다 위에 하늘 안 보이게 지붕 덮어줘").
///
/// 밖에서 본 기와 곡면은 안 만든다 — 플레이어는 평생 안쪽에서만 본다. 대신 <b>서까래</b>를
/// 촘촘히 건다. 천장을 판 한 장으로 덮으면 그게 딱 "유니티로 만든 티" 인데,
/// 서까래가 줄지어 있으면 그 한 가지만으로 한옥으로 읽힌다. 실제 한옥 천장의 인상이 거기서 와.
///
/// 층은 아래에서 위로 이렇게 쌓인다:
///   처마 띠(청기와) → 공포 → 대들보 + 단청 → 서까래 → 반자널
///
/// <b>머티리얼은 밖에서 받는다.</b> 로비·전시실은 씬에 저장돼야 해서 진짜 .mat 에셋이 필요하고,
/// 트랙은 실행할 때 만드는 머티리얼을 쓴다. 그 차이를 이 스크립트가 알 필요는 없어.
/// </summary>
public static class HanokRoof
{
    public static readonly Color Tile      = new Color32(0x4E, 0x7A, 0x70, 0xFF);   // 청기와
    public static readonly Color Beam      = new Color32(0x6B, 0x4A, 0x33, 0xFF);   // 대들보
    public static readonly Color Rafter    = new Color32(0xA8, 0x78, 0x4C, 0xFF);   // 서까래
    public static readonly Color Board     = new Color32(0xC9, 0xB9, 0x9A, 0xFF);   // 반자널
    public static readonly Color Dancheong = new Color32(0xC4, 0x45, 0x3E, 0xFF);   // 단청 띠
    public static readonly Color Skylight  = new Color32(0xFF, 0xF6, 0xDC, 0xFF);   // 천창

    /// <summary>
    /// 지붕 한 채. <paramref name="ceilingY"/> 는 반자널 아랫면 높이 —
    /// 벽 높이를 그대로 넘기면 벽 위에 딱 얹힌다.
    /// </summary>
    /// <param name="material">색 하나를 받아 머티리얼을 돌려주는 함수.</param>
    /// <param name="skylightSize">가운데 천창 한 칸의 한 변(m). 0 이면 안 뚫는다.</param>
    public static GameObject Build(Transform parent, Vector3 centre, float width, float depth,
                                   float ceilingY, System.Func<Color, Material> material,
                                   float skylightSize = 0f)
    {
        var root = new GameObject("HanokRoof").transform;
        root.SetParent(parent, false);
        root.localPosition = centre;

        float halfW = width * 0.5f, halfD = depth * 0.5f;

        // ---- 반자널 — 서까래 위를 덮는 판. 하늘을 가리는 건 결국 이 한 장이다 ----
        Block(root, "Boards", new Vector3(0f, ceilingY + 0.42f, 0f),
              new Vector3(width + 1.6f, 0.3f, depth + 1.6f), Board, material);

        if (skylightSize > 0.5f)
            Block(root, "Skylight", new Vector3(0f, ceilingY + 0.26f, 0f),
                  new Vector3(skylightSize, 0.08f, skylightSize), Skylight, material);

        // ---- 서까래 — 짧은 쪽을 가로질러 촘촘히. 이게 한옥으로 읽히게 하는 유일한 요소야 ----
        // 방이 커질수록 간격을 벌린다. 큰 방에 1.2m 로 걸면 수백 개가 생기고, 어차피 안개에 묻힌다.
        float spacing = Mathf.Max(1.2f, depth / 26f);
        int count = Mathf.Max(3, Mathf.FloorToInt(depth / spacing));
        float thickness = Mathf.Clamp(spacing * 0.22f, 0.16f, 1.1f);

        for (int i = 0; i <= count; i++)
        {
            float z = Mathf.Lerp(-halfD, halfD, i / (float)count);
            Block(root, $"Rafter_{i:00}", new Vector3(0f, ceilingY + 0.14f, z),
                  new Vector3(width + 1.2f, thickness, thickness * 1.15f), Rafter, material);
        }

        // ---- 대들보 셋 — 서까래보다 굵게, 한 단 아래로. 굵기 차이가 있어야 구조로 보인다 ----
        float beamH = Mathf.Clamp(depth * 0.02f, 0.4f, 1.8f);
        for (int i = -1; i <= 1; i++)
        {
            float z = i * halfD * 0.62f;
            Block(root, $"Beam_{i + 1}", new Vector3(0f, ceilingY - beamH * 0.55f, z),
                  new Vector3(width + 1.4f, beamH, beamH * 1.3f), Beam, material);

            // 단청 — 보 양 끝에 두르는 띠. 면적은 손톱만 한데 한국 건물로 읽히게 하는 값이 크다
            for (int s = -1; s <= 1; s += 2)
                Block(root, $"Dancheong_{i + 1}_{(s > 0 ? "E" : "W")}",
                      new Vector3(s * (halfW - beamH * 0.9f), ceilingY - beamH * 0.55f, z),
                      new Vector3(beamH * 0.5f, beamH * 1.04f, beamH * 1.34f), Dancheong, material);
        }

        // ---- 처마 띠 — 벽 위를 두르는 청기와. 벽과 천장이 그냥 만나면 상자 두 개로 보인다 ----
        float bandY = ceilingY - beamH * 1.35f;
        float bandH = Mathf.Clamp(beamH * 0.8f, 0.3f, 1.4f);
        float bandT = bandH * 2.2f;

        Band(root, "Eaves_N", new Vector3(0f, bandY, -halfD), new Vector3(width + 2.4f, bandH, bandT), material);
        Band(root, "Eaves_S", new Vector3(0f, bandY,  halfD), new Vector3(width + 2.4f, bandH, bandT), material);
        Band(root, "Eaves_W", new Vector3(-halfW, bandY, 0f), new Vector3(bandT, bandH, depth + 2.4f), material);
        Band(root, "Eaves_E", new Vector3( halfW, bandY, 0f), new Vector3(bandT, bandH, depth + 2.4f), material);

        // ---- 공포 — 네 귀퉁이에 계단처럼 물린 받침. 지붕이 벽에 '얹혀' 보이게 한다 ----
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                for (int step = 0; step < 3; step++)
                {
                    float grow = 1f + step * 0.55f;
                    Block(root, $"Bracket_{(sx > 0 ? "E" : "W")}{(sz > 0 ? "S" : "N")}_{step}",
                          new Vector3(sx * halfW, bandY - bandH * (1.9f - step * 0.62f), sz * halfD),
                          new Vector3(bandH * grow, bandH * 0.5f, bandH * grow),
                          step == 1 ? Dancheong : Beam, material);
                }

        return root.gameObject;
    }

    static void Band(Transform parent, string name, Vector3 position, Vector3 scale,
                     System.Func<Color, Material> material)
        => Block(parent, name, position, scale, Tile, material);

    /// <summary>
    /// 콜라이더 없는 정적 상자. 지붕에는 아무도 안 닿으니 콜라이더를 달 이유가 없고,
    /// 그림자도 안 드리운다 — 드리우면 방이 통째로 깜깜해진다. 하늘을 가리는 게 목적이지
    /// 빛을 끄는 게 목적이 아니야.
    /// </summary>
    static void Block(Transform parent, string name, Vector3 localPosition, Vector3 scale,
                      Color color, System.Func<Color, Material> material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;

        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }

        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material(color);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        go.isStatic = true;
    }
}
