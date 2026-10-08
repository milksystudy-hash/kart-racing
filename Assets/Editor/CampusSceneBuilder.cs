using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// <b>캠퍼스 씬</b> — 걸어다니면서 건물을 보고, 곰과 이야기하고, 나중에 미니게임에 들어가는 곳.
///
/// 2026-09-17 유저 판단: *"레이싱하다 내리면 좀 이상하잖아. 똑같은 씬을 만들어서 그 씬을
/// 걷기 모드로 바꾸는 게 낫지 않을까."* 맞다. <b>한 씬이 두 가지 일을 하면 둘 다 어정쩡해진다.</b>
///
/// 트랙 씬과 <b>같은 캠퍼스</b>를 쓴다 — `CampusBuilder` 와 `TrackBuilder` 를 그대로 부른다.
/// 건물을 두 벌로 만들면 한쪽을 고칠 때마다 다른 쪽이 어긋난다(이야기 장면을 로비 안에서
/// 돌리는 것과 같은 이유야).
///
/// <b>코스는 없애지 않는다.</b> 유저가 "트랙을 없애고 건물에 집중해야 하나" 물었는데,
/// 없애면 캠퍼스 한가운데가 포장도 안 된 빈터가 된다. 코스는 그냥 <b>캠퍼스에 난 길</b>이고,
/// 실제로 그 길을 따라 걸으면 건물 열셋을 다 지나가게 돼 있다.
/// 대신 <b>레이스 장비는 아무것도 안 만든다</b> — 카트·AI·발판·체크포인트·순위판.
/// </summary>
public static class CampusSceneBuilder
{
    const string SceneFolder = "Assets/Scenes";
    public const string CampusPath = SceneFolder + "/Campus.unity";

    /// <summary>
    /// 로비에서 들어오는 자리 — <b>한옥 정문 안쪽</b>. 문 열고 나오면 캠퍼스가 남쪽으로 펼쳐진다.
    ///
    /// 2026-09-17 유저: *"레이스 한가운데에 로비홀 들어가는 게 있는 게 좀 이상해."* 맞다.
    /// 전에는 (0, −70) 이었는데 남쪽 직선이 <c>z −78</c> 에 폭 12m 라 <b>도로 위</b>였다
    /// (z −84 ~ −72). 차가 지나다니는 길 한복판에 현관이 있는 꼴이야.
    /// 정문(0, 100) 안쪽으로 옮겼다 — 코스 북쪽 끝이 z ≈ 79 라 10m 넘게 떨어져 있고,
    /// 캠퍼스에 들어오고 나가는 자리가 정문인 게 설명도 필요 없다.
    /// </summary>
    static readonly Vector3 Entrance = new Vector3(0f, 0.2f, 84f);

    [MenuItem("Racing/캠퍼스 씬 만들기", false, 4)]
    public static void BuildCampus()
    {
        // ★ 2026-09-23 <b>유저가 손으로 놓은 것을 먼저 기억한다.</b> 여태는 빌더를 돌리는
        // 순간 통째로 날아가서, 소품 배치를 전부 나한테 시켜야 했다 — 유저 말대로
        // «배치하는 데만 크레딧을 너무 많이 쓰는» 구조였어.
        // 이제 유저는 FBX 를 드래그해서 놓고 Ctrl+S 만 하면 되고, 내가 씬을 몇 번을
        // 다시 지어도 그 자리에 그대로 있는다.
        var mine = MyProps.Collect(CampusPath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MakeLighting();

        // ---- 캠퍼스와 길 : 트랙 씬과 같은 것을 부른다 ----
        var campusGo = new GameObject("Campus");
        var campus = campusGo.AddComponent<CampusBuilder>();

        var trackGo = new GameObject("Track");
        var track = trackGo.AddComponent<TrackBuilder>();
        track.raceFurniture = false;   // 발판·체크포인트를 안 만든다. 길만 남는다

        campus.Build();
        track.Build();

        // <b>구운 걸 그대로 쓴다.</b> `built` 는 저장되지 않는 필드라, 실행할 때 Awake 가
        // "아직 안 지었네" 하고 <b>한 벌 더</b> 짓는다 — 캠퍼스가 통째로 두 벌이 되고
        // 현판 글씨도 두 겹으로 겹쳐 보인다(2026-09-17 유저 제보).
        // 트랙 씬은 굽지 않고 저장해서 Awake 가 짓지만, 여기는 걸어 다니는 씬이라
        // 에디터에서 보이는 게 낫다. 구웠으면 Awake 는 꺼 둔다.
        campus.buildOnAwake = false;
        track.buildOnAwake = false;

        // ---- 걸어다닐 몸 ----
        // 정문에서 캠퍼스 안쪽(남쪽)을 보고 선다
        var player = TestSceneBuilder.MakePlayer(Entrance, 180f);

        // ---- 돌아가는 문 : 정문 안쪽에 세운다 (코스 밖) ----
        MakeReturnDoor(new Vector3(0f, 0f, 90f), 180f);

        foreach (var door in Object.FindObjectsByType<SceneDoor>(FindObjectsSortMode.None))
            door.visitor = player.controller.transform;

        // ---- 리그 ----
        var rig = new GameObject("GameRig");
        rig.AddComponent<SceneNavigator>();
        rig.AddComponent<CampusMood>();      // 현판 딱지와 빛이 이야기를 따라간다
        rig.AddComponent<CampusBoarding>();  // 문에 박힌 폐쇄 판자 — 한 판에 한 동씩 걷힌다
        rig.AddComponent<CampusFestival>();  // 여덟 개를 다 모으면 캠퍼스가 축제가 된다
        rig.AddComponent<CampusHUD>();

        MuseumLook.RefineMaterials();
        MuseumLook.ApplyToOpenScene();

        // 건물이 다시 선 뒤에 되돌린다 — 유저가 건물 <b>안에</b> 놓았으면 그 부모를 찾아야 하니까
        MakeCampusBears(player.controller.transform, track);

        MyProps.Restore(mine);

        EditorSceneManager.SaveScene(scene, CampusPath);
        LobbySceneBuilder.RegisterScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Racing] Campus.unity 생성 완료. F1 로비 · F2 트랙 · F3 전시실 · F4 캠퍼스.");
    }

    /// <summary>
    /// 로비로 돌아가는 문. <b>건물에 붙이지 않고 따로 세운다</b> — 본관(웅지관)에 붙이면
    /// "웅지관에 들어간다" 로 읽히는데 실제로는 중앙홀로 가는 거라 헷갈린다.
    ///
    /// <b>코스 위에 놓지 마라.</b> 한 번 그렇게 놨다가 레이스 직선 한가운데에 현관이 서 있었다.
    /// 코스는 <c>TrackBuilder.Path</c> 의 점들이고 폭이 7~12m 다 — 자리를 잡기 전에 그 표를 봐라.
    /// </summary>

    /// <summary>캠퍼스 곰 — 모델 키가 0.45m 라 사람 눈높이에 맞추려면 이만큼 키운다(≈1.05m).</summary>
    const float HanbokBearScale = 2.3f;

    /// <summary>
    /// ★★ 2026-10-08 — <b>캠퍼스에 곰 여섯.</b> 여태 화장실 곰 하나뿐이라
    /// 걸어 다닐 이유가 약했다(수연: *"캠퍼스가 상호작용 할 게 없다"*).
    ///
    /// ★ <b>자리를 손으로 찍지 않는다.</b> 로비 곰에서 두 번 틀린 그 실수다 —
    /// 처음엔 조각상 안에, 고친 뒤엔 석등 위에 세웠다. 캠퍼스는 200m 짜리라 더 위험하다.
    /// 격자를 깔고 <b>코스에서 멀고 · 물건에서 멀고 · 담장 안</b>인 칸만 남겨서 고른다.
    ///
    /// ★ <b>콜라이더가 아니라 Renderer 바운즈로 잰다.</b> 석등·나무처럼 콜라이더가 없는
    /// 장식은 콜라이더만 보면 «비었다» 로 나오고, 곰이 그 위에 선다.
    /// </summary>
    static void MakeCampusBears(Transform visitor, TrackBuilder track)
    {
        var models = new System.Collections.Generic.List<string>();
        foreach (var g in AssetDatabase.FindAssets("HanbokBear t:Model", new[] { "Assets/NPC_bear" }))
            models.Add(AssetDatabase.GUIDToAssetPath(g));
        models.Sort(System.StringComparer.Ordinal);
        if (models.Count == 0)
        {
            Debug.Log("[캠퍼스] Assets/NPC_bear 에 HanbokBear FBX 가 없어서 곰은 건너뛴다.");
            return;
        }

        // 이미 선 것들의 «눈에 보이는» 크기
        var blocks = new System.Collections.Generic.List<Bounds>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,
                                                             FindObjectsSortMode.None))
        {
            var b = r.bounds;
            if (b.max.y < 0.35f) continue;             // 바닥 무늬는 물건이 아니다
            if (b.min.y > 3.0f) continue;              // 지붕·들보는 머리 위다
            if (b.size.x > 40f || b.size.z > 40f) continue;  // 담장 통짜는 따로 본다
            blocks.Add(b);
        }

        // 코스 중심선 — 여기서 멀어야 한다. 걸어다니는 곰이 길 위에 서면 레이스에 끼어든다
        var lane = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < 400; i++)
            lane.Add(track.transform.position + track.PointOnPath(i / 400f));

        const float Half = 96f;        // 담장 안쪽. 담장에 바싹 붙으면 아무도 안 지나간다
        const float Clear = 3.4f;      // 물건에서 띄울 거리
        const float NearLane = 11f;    // 코스에서 이만큼은 떨어진다 — 길 위에 서면 레이스에 낀다
        const float FarLane = 34f;     // ★ 너무 멀어도 안 된다. 사람이 안 가는 구석에 서 있으면 없는 것과 같다

        // ① 설 수 있는 칸을 전부 모은다
        var ok = new System.Collections.Generic.List<Vector3>();
        for (float x = -Half; x <= Half; x += 4f)
        for (float z = -Half; z <= Half; z += 4f)
        {
            float best = float.MaxValue;
            foreach (var p in lane)
            {
                float d2 = (p.x - x) * (p.x - x) + (p.z - z) * (p.z - z);
                if (d2 < best) best = d2;
            }
            best = Mathf.Sqrt(best);
            if (best < NearLane || best > FarLane) continue;

            bool free = true;
            foreach (var b in blocks)
            {
                var e = b; e.Expand(new Vector3(Clear * 2f, 0f, Clear * 2f));
                if (e.Contains(new Vector3(x, Mathf.Clamp(1f, e.min.y, e.max.y), z))) { free = false; break; }
            }
            if (free) ok.Add(new Vector3(x, 0f, z));
        }

        // ② ★ <b>제일 먼 곳부터 고른다.</b> 격자를 그냥 훑으면 구석부터 차서
        //    여섯이 전부 서쪽 담장에 붙어 섰다(측정: x 전부 −104). 이미 고른 자리에서
        //    <b>가장 멀리 떨어진 칸</b>을 하나씩 집으면 캠퍼스에 고르게 퍼진다.
        var spots = new System.Collections.Generic.List<Vector3>();
        if (ok.Count > 0)
        {
            spots.Add(ok[ok.Count / 2]);   // 가운데쯤에서 시작한다
            while (spots.Count < models.Count && spots.Count < ok.Count)
            {
                Vector3 pick = ok[0]; float far = -1f;
                foreach (var c in ok)
                {
                    float near = float.MaxValue;
                    foreach (var s in spots) near = Mathf.Min(near, (s - c).sqrMagnitude);
                    if (near > far) { far = near; pick = c; }
                }
                spots.Add(pick);
            }
        }
        Debug.Log($"[캠퍼스] 설 수 있는 칸 {ok.Count}개 중 {spots.Count}개를 골랐다");

        if (spots.Count == 0) { Debug.LogWarning("[캠퍼스] 곰을 세울 빈자리를 못 찾았다."); return; }

        var root = new GameObject("BearNpcs").transform;
        for (int i = 0; i < spots.Count; i++)
        {
            var t = CampusBuilder.MyModel(root, models[i % models.Count], $"BearNpc_{i + 1}",
                                          spots[i], 0f);
            if (t == null) continue;
            t.localScale = Vector3.one * HanbokBearScale;
            // 캠퍼스 가운데를 보게 — 담장을 보고 서 있으면 등만 보인다
            Vector3 toward = -spots[i]; toward.y = 0f;
            if (toward.sqrMagnitude > 0.01f)
                t.rotation = Quaternion.LookRotation(toward, Vector3.up) * t.rotation;

            var npc = t.gameObject.AddComponent<BearNpc>();
            // ★ 몸이 있으니 «거리» 로 말을 건다 — 카메라 시선 방식은 걸어다니는 아바타가
            //   없을 때만 쓰는 임시방편이었다(2026-09-17 로비). 역할(대사)은 BearNpc 가
            //   씬 안의 순번으로 알아서 고른다.
            npc.lookTarget = visitor;
            npc.noticeRange = 9f;
            npc.patrolRadius = 9f;
            npc.walkSpeed = 0.55f;

            var box = t.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.22f, 0f);
            box.size = new Vector3(0.34f, 0.45f, 0.26f);   // 로컬이라 스케일을 따라간다
        }
        Debug.Log($"[캠퍼스] 곰 {spots.Count}마리 — " +
                  string.Join(" ", spots.ConvertAll(p => $"({p.x:0},{p.z:0})")));
    }

    static void MakeReturnDoor(Vector3 at, float yaw)
    {
        var root = new GameObject("ReturnDoor").transform;
        root.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));

        HanokDoor.Build(root, Vector3.zero, Quaternion.identity, 4f, 4.6f, FlatMaterial.Get,
                        plaque: true, buildingName: "중앙홀", department: "돌아가기");

        // ★ 2026-09-18 유저: *"바로 홀 뒤에 아무것도 없는데 저 판이 나오는 게 좀..."*
        // 맞다 — 벽 한 장만 세워 두니 <b>들판에 떠 있는 판때기</b>로 보였다.
        // 문에는 <b>들어갈 데가 있어 보여야</b> 한다. 벽을 <b>작은 문간채</b>로 바꾼다:
        // 몸통 + 처마 + 기단 + 양옆 쪽담. 이러면 "이 건물 안으로 들어가서 중앙홀로 간다" 가 된다.
        var cream = FlatMaterial.Get(new Color32(0xEF, 0xE7, 0xD6, 0xFF));
        var wood = FlatMaterial.Get(new Color32(0x6B, 0x4A, 0x33, 0xFF));
        var tile = FlatMaterial.Get(new Color32(0x4E, 0x7A, 0x70, 0xFF));
        var stone = FlatMaterial.Get(new Color32(0xA8, 0xA4, 0x9A, 0xFF));

        Slab(root, "Body", new Vector3(0f, 3.1f, -2.6f), new Vector3(11f, 6.2f, 5.2f), cream);
        Slab(root, "Base", new Vector3(0f, 0.3f, -2.6f), new Vector3(12.4f, 0.6f, 6.6f), stone);
        Slab(root, "Eave", new Vector3(0f, 6.5f, -2.6f), new Vector3(14f, 0.5f, 8.6f), tile);
        Slab(root, "Ridge", new Vector3(0f, 7.1f, -2.6f), new Vector3(11.5f, 0.7f, 6.2f), tile);
        Slab(root, "Beam", new Vector3(0f, 5.9f, -0.05f), new Vector3(11.2f, 0.45f, 0.35f), wood);

        for (int s = -1; s <= 1; s += 2)
        {
            Slab(root, $"Post_{s}", new Vector3(s * 4.9f, 3.1f, -0.05f),
                 new Vector3(0.5f, 6.2f, 0.5f), wood);
            // 양옆 쪽담 — 문간채가 담장에 이어져 보이면 들판에 놓인 상자가 아니게 된다
            Slab(root, $"Wing_{s}", new Vector3(s * 10.5f, 2.1f, -1.4f),
                 new Vector3(10f, 4.2f, 1.1f), cream);
            Slab(root, $"WingCap_{s}", new Vector3(s * 10.5f, 4.35f, -1.4f),
                 new Vector3(10.6f, 0.36f, 1.9f), tile);
        }

        // ★ 지붕 위 상징물 — <b>운전대 + 전시 카트</b>. 이 문은 «중앙홀로» 가는 문이고
        // 중앙홀은 카트를 고르는 방이라, 지붕에 카트가 서 있으면 <b>글자를 안 읽어도</b>
        // 저 문이 어디로 가는지 안다. 다른 열네 동과 같은 규칙(<see cref="CampusBuilder"/>).
        //
        // 문간채만 <c>Hanok</c> 이 아니라 여기서 직접 얹는다 —
        // 능선(`Ridge`)이 y 7.1 에 높이 0.7 이라 <b>윗면이 7.45</b>, z 는 −2.6 이 한가운데다.
        CampusBuilder.AddRoofEmblem(root, "11_Central_Hall_Kart",
                                    new Vector3(0f, 7.45f - 0.06f, -2.6f),
                                    11f * 0.45f, 6.2f * 1.05f);

        var door = root.gameObject.AddComponent<SceneDoor>();
        door.sceneIndex = 0;          // 로비
        door.label = "중앙홀로";
        door.range = 3.6f;
    }

    /// <summary>회전 없는 상자 하나. 문간채처럼 조각이 여럿일 때 줄 수를 줄여준다.</summary>
    static void Slab(Transform parent, string name, Vector3 at, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        go.isStatic = true;
    }

    static void MakeLighting()
    {
        var go = new GameObject("Sun");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f);
        light.intensity = 1.05f;
        light.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(48f, 28f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.60f, 0.58f, 0.53f);
        RenderSettings.ambientEquatorColor = new Color(0.46f, 0.44f, 0.39f);
        RenderSettings.ambientGroundColor  = new Color(0.25f, 0.22f, 0.19f);

        // 트랙과 같은 값. 배경색도 이것과 같아야 먼 벽이 하늘 띠처럼 안 보인다.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.55f, 0.51f, 0.45f);
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 320f;
    }
}
