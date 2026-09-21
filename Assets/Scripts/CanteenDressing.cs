using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// <b>곰밥마당을 급식실처럼 꾸민다 - 게임을 안 해도 늘.</b>
///
/// 유저: *"평소에도 수증기가 좀 나오면 좋겠는데, 연출적으로 더 급식실 느낌 나도록."*
///
/// 문제는 <b>어디에 붙이느냐</b>였다. <see cref="CampusBuilder"/> 에 넣으면 Campus 씬을 다시
/// 구워야 하고, 그러면 유저가 손으로 놓은 게 날아간다. 그래서 <b>씬을 안 건드린다</b> -
/// <c>RuntimeInitializeOnLoadMethod</c> 로 실행할 때 스스로 들어와서, 배식대가 있는 씬이면
/// 꾸미고 없으면 아무 일도 안 한다.
///
/// 이 프로젝트가 네 번 겪은 <b>"새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다"</b> 의
/// 마지막 형태야 - 아예 컴포넌트를 씬에 안 둔다.
///
/// <b>남기는 게 0이다.</b> 만든 것은 전부 <c>CanteenDressing</c> 한 덩이 아래 들어가고,
/// 씬에 저장되지 않으며, 재질 에셋도 안 건드린다.
/// </summary>
public class CanteenDressing : MonoBehaviour
{
    const string RootName = "CanteenDressing";

    Transform counter;
    Transform building;
    Transform fan;

    // ---- 들어오기 ----------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Install();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install();

    static void Install()
    {
        // ★ <b>꾸밈이 게임을 망가뜨리면 안 된다.</b> 여기는 씬이 로드되는 길목이라,
        // 여기서 예외가 나면 그 뒤에 돌아야 할 초기화가 통째로 건너뛰어진다.
        // 장식일 뿐이니 실패하면 조용히 포기하고 게임은 계속 굴러가게 둔다.
        try
        {
            // 배식대는 씬에 하나뿐이다(측정: InServeTop 1개). 없으면 곰밥마당이 없는 씬이야.
            var found = GameObject.Find("InServeTop");
            if (found == null) return;

            var host = found.transform.parent;
            if (host == null) return;
            if (host.Find(RootName) != null) return;   // 이미 꾸며 놨다

            var go = new GameObject(RootName);

            // ★★ <b>AddComponent 는 Awake 를 그 자리에서 부른다.</b>
            // 그래서 `AddComponent<T>().field = x` 로 쓰면 <b>대입이 Awake 뒤에</b> 일어나고,
            // Awake 안에서는 그 필드가 null 이다 — 2026-09-21 여기서 NullReferenceException 이 났다.
            // <b>꺼진 채로 만들어</b> Awake 를 미루고, 값을 꽂은 다음 켠다.
            go.SetActive(false);
            go.transform.SetParent(host, false);

            var dressing = go.AddComponent<CanteenDressing>();
            dressing.counter = found.transform;

            go.SetActive(true);   // 이제야 Awake 가 돈다
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[급식실 꾸미기] 실패했지만 게임은 계속된다: {e}");
        }
    }

    // ---- 자리 --------------------------------------------------------------

    Vector3 Along => counter.forward;     // 배식대의 긴 쪽. 식판(+6.4) → 반납대(−6.6)
    Vector3 ToRoom => counter.right;      // 배식대에서 방 쪽
    Vector3 Foot => new Vector3(counter.position.x, 0f, counter.position.z);

    /// <summary>배식대 상판 윗면.</summary>
    const float CounterTop = 1.07f;

    /// <summary>
    /// 차양 높이. <b>이 방이 밋밋했던 진짜 이유</b>가 여기 있다 —
    /// 30 × 19m 에 천장 8.8m 짜리 방은 급식실이 아니라 <b>창고</b>고,
    /// 창고는 조명을 어떻게 달아도 창고다. 요리 게임이 아늑한 건 <b>공간이 좁아서</b>야.
    ///
    /// 건물을 줄일 수는 없으니(씬을 다시 구워야 한다) <b>배식대 위만 덮는다.</b>
    /// 2.45m 짜리 차양 하나가 8.8m 천장을 화면에서 지워 준다.
    /// </summary>
    const float AwningY = 2.45f;

    Vector3 At(float along, float room, float y) =>
        Foot + Along * along + ToRoom * room + Vector3.up * y;

    // ---- 짓기 --------------------------------------------------------------

    void Awake()
    {
        // 누가 이 컴포넌트를 손으로 붙였거나 위 순서가 또 어긋나면 여기서 막는다.
        if (counter == null)
        {
            Debug.LogWarning("[급식실 꾸미기] 배식대를 못 받았다. 꾸미지 않고 넘어간다.");
            enabled = false;
            return;
        }

        building = counter.parent;

        Stall();       // 차양과 포렴 - 조명보다 먼저. 등을 이 아래에 매단다
        Lights();
        Steam();
        SoftenGuard();
        MenuBoard();
        Utensils();
        QueuePosts();
        Poster();
        CeilingFan();
    }

    void Update()
    {
        // 천장 선풍기. <b>방에서 움직이는 게 하나라도 있어야</b> 사진이 아니라 공간으로 보인다.
        if (fan != null) fan.Rotate(0f, 96f * Time.deltaTime, 0f, Space.Self);
    }

    // ---- 가게 --------------------------------------------------------------

    /// <summary>
    /// <b>배식대 위에 가게를 세운다.</b> 차양 · 기둥 · 포렴 · 뒤 선반.
    ///
    /// 유저가 세 번 "연출이 밋밋하다" 고 했다. 두 번은 조명 숫자를 고쳤는데 또 나왔으니
    /// <b>방법이 틀린 것</b>이다(CLAUDE.md: 같은 증상을 두 번 고쳤으면 «이 방법이 맞나» 를 물어라).
    ///
    /// 조명이 아니라 <b>공간</b>이 문제였다. 요리 게임 화면에는 셋이 있다 —
    /// <b>낮은 지붕 · 따뜻한 배경 · 색</b>. 이 방엔 셋 다 없었다:
    /// 천장은 8.8m 고, 배경은 어두운 벽이고, 전부 크림·나무·돌 한 가지 색조였다.
    /// </summary>
    void Stall()
    {
        Color canvas = new Color32(0x39, 0x4E, 0x5C, 0xFF);   // 짙은 남색 천 — 한국 포장마차 색
        Color band = new Color32(0xC4, 0x45, 0x3E, 0xFF);   // 붉은 띠
        Color wood = new Color32(0x6B, 0x4A, 0x33, 0xFF);
        Color woodLight = new Color32(0x9A, 0x77, 0x4E, 0xFF);
        Color brass = new Color32(0xB9, 0x9A, 0x5E, 0xFF);

        // ---- 차양 ----
        // Along 을 로컬 +Z 로 삼는다. 그러면 길이는 로컬 Z, 폭은 로컬 X 가 된다.
        var awning = new GameObject("Awning").transform;
        awning.SetParent(transform, false);
        awning.position = At(2.3f, 0.45f, AwningY);
        awning.rotation = Quaternion.LookRotation(Along, Vector3.up);

        Slab(awning, "Canvas", Vector3.zero, new Vector3(2.7f, 0.09f, 10.6f), canvas);
        // 앞단 붉은 띠 — 천 한 장만 있으면 널빤지로 보인다. 테두리가 있어야 «천» 이다.
        Slab(awning, "Trim", new Vector3(1.35f, -0.10f, 0f), new Vector3(0.10f, 0.26f, 10.6f), band);
        Slab(awning, "Beam", new Vector3(1.28f, 0.14f, 0f), new Vector3(0.16f, 0.16f, 10.8f), wood);

        // ---- 기둥 ----
        // ★ 자리를 <b>줄 밖</b>으로 잡는다. 줄은 Along 0.6~7.1 (다섯 자리 + 들어오는 자리)이고
        // 손님 반폭이 0.26 이라, 기둥을 그 사이에 세우면 손님을 뚫고 선다.
        // −2.8 과 7.4 면 제일 가까운 손님과 0.42m 떨어진다.
        foreach (float along in new[] { -2.8f, 7.4f })
        {
            Vector3 at = At(along, 1.85f, 0f);
            var post = Rod(transform, $"StallPost_{along:0.0}", Vector3.zero, 0.13f, AwningY, wood);
            post.position = new Vector3(at.x, AwningY * 0.5f, at.z);
        }

        // ---- 포렴 ----
        // ★ <b>높이 잡기가 전부다.</b> 아래로 길게 늘어뜨리면 손님과 그릇을 가린다.
        // 바닥 2.0m 로 끊으면 손님(키 1.09)과 상판(1.07) 위를 지나가서,
        // 화면 <b>맨 윗줄에 색 띠</b>로만 보인다 — 그게 "가게" 신호로는 충분하다.
        for (int i = 0; i < 13; i++)
        {
            float along = -2.9f + i * 0.82f;
            var strip = Slab(transform, $"Noren_{i}", Vector3.zero,
                             new Vector3(0.05f, 0.42f, 0.66f), i % 2 == 0 ? canvas : band);
            strip.position = At(along, 1.72f, 2.21f);
            strip.rotation = Quaternion.LookRotation(Along, Vector3.up);
        }

        // ---- 뒤 선반 ----
        // <b>배경이 어두운 벽이면 화면에 구멍이 뚫린 것처럼 보인다.</b> 상판(1.07) 위라
        // 배식대와 안 부딪히고, 카메라에서 보면 손님 뒤를 채워 준다.
        var shelf = Slab(transform, "BackShelf", Vector3.zero, new Vector3(0.26f, 0.05f, 6.4f), woodLight);
        shelf.position = At(1.2f, -0.98f, 1.78f);
        shelf.rotation = Quaternion.LookRotation(Along, Vector3.up);

        // 그릇 더미 셋 — 쌓인 물건이 있어야 «쓰는 곳» 으로 보인다
        for (int i = 0; i < 3; i++)
        {
            float along = -0.9f + i * 2.1f;
            for (int k = 0; k < 4; k++)
            {
                var bowl = Ball(transform, $"StackBowl_{i}_{k}", Vector3.zero,
                                new Vector3(0.30f, 0.08f, 0.30f), brass, Finish.금속);
                bowl.position = At(along, -0.98f, 1.84f + k * 0.07f);
            }
        }

        // 소쿠리 둘 — 벽에 걸린 것
        for (int i = 0; i < 2; i++)
        {
            var basket = Ball(transform, $"Basket_{i}", Vector3.zero,
                              new Vector3(0.44f, 0.26f, 0.12f), woodLight);
            basket.position = At(-2.6f + i * 6.0f, -1.02f, 2.15f);
        }
    }

    // ---- 조명 --------------------------------------------------------------

    /// <summary>
    /// 이 방에는 <b>실제 조명이 하나도 없었다.</b> 천장등(<c>InLamp</c>)은 발광 재질일 뿐이라
    /// 빛나는 사각형만 떠 있고 빛이 퍼지지 않는다 - 그게 "인공적" 의 정체다.
    ///
    /// <b>약한 등 셋 + 반대쪽 찬빛.</b> 센 등 하나는 바로 밑만 태우고 두 걸음 밖은 새까맣게
    /// 만든다. 셋을 걸면 빛이 겹쳐 고르게 밝고, 걸어 다니는 동안 밝기가 안 바뀐다.
    /// </summary>
    void Lights()
    {
        Color warm = new Color(1.00f, 0.85f, 0.62f);

        // ★ <b>등을 차양 아래로 내렸다.</b> 전에는 3.25m 에 걸고 천장(8.8m)까지 4.65m 짜리
        // 줄을 늘어뜨렸는데, 그러면 등이 «높은 방에 매달린 것» 이라 아늑함이 안 생긴다.
        // 포장마차 등처럼 <b>머리 바로 위</b>에 와야 한다.
        float[] spots = { 4.3f, 1.3f, -1.7f };
        for (int i = 0; i < spots.Length; i++)
        {
            Vector3 at = At(spots[i], 0.7f, 2.12f);
            var lamp = Lamp($"Pendant_{i}", at, warm, 1.45f, 7.5f);

            Ball(lamp, "Shade", Vector3.up * 0.09f, new Vector3(0.42f, 0.24f, 0.42f),
                 new Color32(0x3A, 0x2E, 0x26, 0xFF), Finish.금속);
            Ball(lamp, "Bulb", Vector3.down * 0.05f, new Vector3(0.19f, 0.21f, 0.19f),
                 warm, Finish.발광);

            // 줄은 차양까지만. 짧을수록 «낮은 지붕 아래» 로 읽힌다.
            float toAwning = AwningY - at.y;
            if (toAwning > 0.1f)
                Rod(lamp, "Cord", Vector3.up * (0.09f + toAwning * 0.5f), 0.03f, toAwning,
                    new Color32(0x2E, 0x26, 0x20, 0xFF));
        }

        // ★ <b>뒤 벽을 씻어 준다.</b> 배경이 안 밝으면 손님 뒤가 검은 구멍이라 화면이 얕아 보인다 -
        // 앞 · 가운데 · 뒤가 다 보여야 공간으로 읽힌다. 메뉴판도 이 빛으로 읽힌다.
        Lamp("WallWash", At(1.2f, -0.55f, 2.25f), new Color(1.00f, 0.90f, 0.76f), 1.1f, 7f);

        // 반대쪽에서 <b>아주 약한 찬빛</b>. 방이 한 가지 색이면 색칠한 것처럼 보인다 -
        // 창에서 드는 낮빛을 흉내내면 따뜻한 등과 대비가 생기고, 그 대비가 "실내" 로 읽힌다.
        Lamp("WindowFill", At(1.0f, 9f, 3.4f), new Color(0.72f, 0.80f, 0.95f), 0.5f, 16f);

        // 반납대 쪽이 어두우면 방이 거기서 끊겨 보인다.
        Lamp("BackFill", At(-6.0f, 3.0f, 2.9f), new Color(0.95f, 0.92f, 0.86f), 0.55f, 9f);
    }

    /// <summary>
    /// 천장 밑면. <c>GameObject.Find("InCeiling")</c> 은 쓰면 안 된다 -
    /// 씬에 <b>15개</b>가 있다(건물마다 실내가 있으니까). 배식대의 형제에서 찾는다.
    /// </summary>
    float CeilingY()
    {
        var ceil = building != null ? building.Find("InCeiling") : null;
        if (ceil == null) return 0f;
        return ceil.position.y - Mathf.Abs(ceil.lossyScale.y) * 0.5f;
    }

    Transform Lamp(string name, Vector3 at, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = at;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;

        // 기획서 7.6 은 실시간 그림자를 주요 조명 하나로 묶어 뒀다. 여기는 안 켠다.
        light.shadows = LightShadows.None;
        return go.transform;
    }

    // ---- 김 ----------------------------------------------------------------

    /// <summary>
    /// 국통 넷에서 김이 오른다. <b>평소에도</b> - 움직이는 게 없는 방은 사진처럼 보인다.
    ///
    /// 국통(<c>InPot_0..3</c>)을 직접 찾아 그 위에 얹는다. 미니게임의 그릇은 판이 돌 때만
    /// 있으니 그걸 기준으로 하면 평소에는 김이 안 난다.
    /// </summary>
    void Steam()
    {
        for (int i = 0; i < 4; i++)
        {
            var pot = building != null ? building.Find($"InPot_{i}") : null;
            if (pot == null) continue;

            float top = pot.position.y + Mathf.Abs(pot.lossyScale.y) * 0.5f;
            Puff($"Steam_{i}", new Vector3(pot.position.x, top + 0.05f, pot.position.z),
                 Mathf.Abs(pot.lossyScale.x) * 0.3f);
        }
    }

    void Puff(string name, Vector3 at, float spread)
    {
        var material = SteamMaterial();
        if (material == null) return;

        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = at;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // 파티클은 로컬 +Z 로 뿜는다

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        // <b>수명과 크기를 흩는다.</b> 전부 같으면 알갱이가 줄 맞춰 올라가서 기계처럼 보인다.
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
        // 회전을 안 흩으면 같은 그림이 겹쳐 찍혀서 격자무늬가 보인다.
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(1f, 1f, 1f, 0.11f);   // 얇게 여러 겹이 진한 하나보다 낫다
        main.gravityModifier = -0.015f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = 7f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 9f;
        shape.radius = Mathf.Max(0.05f, spread);

        // 위로 갈수록 퍼진다. 김은 올라가면서 식어서 넓어지니까.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.45f, 1f, 2.6f));

        // 천천히 돌아야 덩어리가 살아 움직인다.
        var spin = ps.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.22f), new GradientAlphaKey(0f, 1f) });
        fade.color = new ParticleSystem.MinMaxGradient(grad);

        // 곧게 오르면 연기가 아니라 분수로 보인다.
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.13f;
        noise.frequency = 0.28f;
        noise.scrollSpeed = 0.2f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = material;

        ps.Play();
    }

    /// <summary>
    /// 알갱이 재질은 <see cref="SmokePuff"/> 한 군데에 있다 — 카트 배기 김과 같은 것을 쓴다.
    ///
    /// ★ 여기 있던 판이 «흰 네모» 로 보였던 건 텍스처가 없어서가 <b>아니라</b>
    /// <b>투명이 안 켜져 있어서</b>였다. <c>SetFloat("_Surface", 1)</c> 은 속성만 바꾸고
    /// URP 는 <b>셰이더 키워드</b>를 본다. 자세한 건 SmokePuff 주석에.
    /// </summary>
    static Material SteamMaterial() => SmokePuff.Material();

    // ---- 위생 가림막 -------------------------------------------------------

    /// <summary>
    /// <c>InGuard</c> 는 <b>11.4m 짜리가 18도 기울어 선 유리판</b>(매끄러움 0.95)이라
    /// 정반사가 통째로 <b>길고 흰 대각선 띠</b>로 잡힌다. 유저가 "조명이 이상하다" 고 한
    /// 큰 몫이 이거였다 - 조명이 아니라 재질 문제야.
    ///
    /// 재질 <b>에셋은 안 건드린다.</b> 렌더러가 가리키는 재질만 바꾼다 -
    /// .mat 을 실행 중에 고치면 디스크 파일이 바뀌어서 다른 씬까지 따라간다.
    /// </summary>
    void SoftenGuard()
    {
        var found = building != null ? building.Find("InGuard") : null;
        if (found == null) return;

        var skin = found.GetComponent<Renderer>();
        if (skin == null || skin.sharedMaterial == null) return;

        Color tint = skin.sharedMaterial.HasProperty("_BaseColor")
                   ? skin.sharedMaterial.GetColor("_BaseColor")
                   : new Color(0.85f, 0.90f, 0.92f, 0.4f);

        skin.sharedMaterial = FlatMaterial.Get(tint, Finish.무광);
    }

    // ---- 급식실 소품 -------------------------------------------------------

    static readonly string[] Menu = { "밥", "국", "김치", "반찬", "후식" };

    /// <summary>
    /// 메뉴판에 <b>오늘의 메뉴</b>를 적는다. 벽에 붙은 <c>InMenuSlip_0..4</c> 다섯 장이
    /// 원래 비어 있었다 - 글씨가 들어가면 그 순간 급식실이 된다.
    ///
    /// 그리고 이게 <b>미니게임의 다섯 칸과 같은 이름</b>이라, 배식대에 서기 전에 이미
    /// 무엇을 담게 될지 배운다.
    /// </summary>
    void MenuBoard()
    {
        var font = HudFont.Resolve(null);
        if (font == null) return;

        for (int i = 0; i < Menu.Length; i++)
        {
            var slip = building != null ? building.Find($"InMenuSlip_{i}") : null;
            if (slip == null) continue;

            var go = new GameObject($"MenuText_{i}");
            go.transform.SetParent(transform, false);
            go.transform.position = slip.position + ToRoom * 0.10f;

            // ★ 글자는 <b>보는 사람 반대쪽</b>을 보게 단다. 현판이 전부 거울 글씨로 나왔던
            // 그 규칙이야(2026-09-17) - 여기서는 방이 +ToRoom 쪽이니 −ToRoom 을 본다.
            go.transform.rotation = Quaternion.LookRotation(-ToRoom, Vector3.up);

            var text = go.AddComponent<TextMesh>();
            text.font = font;
            text.text = Menu[i];
            text.fontSize = 120;                       // 크게 구워서 줄여 쓴다 - 작게 구우면 계단이 보인다
            text.characterSize = 0.42f * 10f / 120f;   // 한 줄 높이 0.42m
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color32(0x3A, 0x2E, 0x22, 0xFF);
            go.GetComponent<MeshRenderer>().sharedMaterial = BuildingSign.TextMaterial(font);
        }
    }

    /// <summary>수저통과 국자. 한국 급식실에서 <b>없으면 바로 어색한</b> 두 가지.</summary>
    void Utensils()
    {
        Color steel = new Color32(0xB4, 0xB8, 0xBC, 0xFF);
        Color wood = new Color32(0x8A, 0x6A, 0x46, 0xFF);

        // 수저통 - 식판 쌓인 곳 옆. 줄의 시작이라 여기 있어야 동선이 맞는다.
        Box(transform, "SpoonBin", At(5.5f, 0.15f, CounterTop + 0.11f),
            new Vector3(0.34f, 0.22f, 0.26f), steel);

        // ★ 수저를 <b>수저통의 자식으로 달면 안 된다.</b> 통이 (0.34, 0.22, 0.26) 으로 눌린
        // 상자라 자식도 그 배율로 눌려서, 굵기 6cm 짜리 수저가 2cm × 12cm 로 찌그러진다.
        // 스케일 1 인 뿌리에 달고 <b>월드 좌표로</b> 세운다.
        for (int i = 0; i < 7; i++)
        {
            float off = -0.11f + i * 0.037f;
            var spoon = Rod(transform, $"Spoon_{i}", Vector3.zero, 0.022f, 0.30f, steel);
            spoon.position = At(5.5f + off, 0.15f, CounterTop + 0.24f);
        }

        // 국자 둘 - 국통에 꽂혀 있다. 자루가 비스듬해야 "쓰던 것" 으로 보인다.
        for (int i = 1; i <= 2; i++)
        {
            var pot = building != null ? building.Find($"InPot_{i}") : null;
            if (pot == null) continue;

            var ladle = new GameObject($"Ladle_{i}").transform;
            ladle.SetParent(transform, false);
            ladle.position = pot.position + Vector3.up * 0.2f;
            ladle.rotation = Quaternion.LookRotation(ToRoom, Vector3.up) * Quaternion.Euler(0f, 0f, 24f);

            Rod(ladle, "Handle", Vector3.up * 0.26f, 0.028f, 0.52f, wood);
            Ball(ladle, "Cup", Vector3.zero, new Vector3(0.14f, 0.09f, 0.14f), steel, Finish.금속);
        }
    }

    /// <summary>
    /// 줄 안내봉. <b>곰들이 왜 거기 줄을 서는지</b>가 눈에 보여야 한다 -
    /// 없으면 다섯 마리가 우연히 일렬로 선 것처럼 보인다.
    /// </summary>
    void QueuePosts()
    {
        Color post = new Color32(0x9A, 0x9E, 0xA2, 0xFF);
        Color belt = new Color32(0xC4, 0x45, 0x3E, 0xFF);

        float[] spots = { 0.2f, 1.9f, 3.6f, 5.3f };
        for (int i = 0; i < spots.Length; i++)
        {
            Vector3 at = At(spots[i], 2.65f, 0f);
            var stand = new GameObject($"QueuePost_{i}").transform;
            stand.SetParent(transform, false);
            stand.position = at;
            stand.rotation = Quaternion.LookRotation(Along, Vector3.up);

            Ball(stand, "Base", Vector3.up * 0.03f, new Vector3(0.28f, 0.06f, 0.28f), post, Finish.금속);
            Rod(stand, "Pole", Vector3.up * 0.48f, 0.05f, 0.96f, post);
            Ball(stand, "Cap", Vector3.up * 0.98f, new Vector3(0.09f, 0.09f, 0.09f), post, Finish.금속);

            // 띠는 <b>기둥 사이</b>에 걸린다. 마지막 기둥 뒤에는 걸 데가 없다.
            if (i < spots.Length - 1)
                Box(stand, "Belt", at + Along * 0.85f + Vector3.up * 0.86f,
                    new Vector3(0.02f, 0.05f, 1.7f), belt);
        }
    }

    /// <summary>
    /// 반납대 위 안내문. 한국 급식실 벽에 반드시 붙어 있는 것이고,
    /// <b>이 게임 말투에도 맞는다</b> - 잔반 얘기는 원래 좀 웃기니까.
    /// </summary>
    void Poster()
    {
        var font = HudFont.Resolve(null);

        Vector3 at = At(-6.2f, -0.55f, 1.95f);

        // ★ 테두리도 <b>뿌리에</b> 단다. 판의 자식으로 달면 판의 배율(0.05, 0.72, 1.15)이
        // 곱해져서 테두리가 종잇장처럼 납작해진다.
        // 층은 뒤에서 앞으로: 테두리가 뒤, 판이 앞, 글자가 제일 앞(2026-09-18 규칙).
        Box(transform, "PosterEdge", at - ToRoom * 0.02f, new Vector3(0.04f, 0.82f, 1.28f),
            new Color32(0x6B, 0x4A, 0x33, 0xFF));
        Box(transform, "Poster", at, new Vector3(0.05f, 0.72f, 1.15f),
            new Color32(0xF3, 0xEC, 0xDA, 0xFF));

        if (font == null) return;

        var go = new GameObject("PosterText");
        go.transform.SetParent(transform, false);
        go.transform.position = at + ToRoom * 0.06f;
        go.transform.rotation = Quaternion.LookRotation(-ToRoom, Vector3.up);

        var text = go.AddComponent<TextMesh>();
        text.font = font;
        text.text = "남기지 맙시다\n곰도 굶습니다";
        text.fontSize = 120;
        text.characterSize = 0.20f * 10f / 120f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color32(0x3A, 0x2E, 0x22, 0xFF);
        go.GetComponent<MeshRenderer>().sharedMaterial = BuildingSign.TextMaterial(font);
    }

    /// <summary>천장 선풍기. 도는 것 하나가 방 전체를 살린다.</summary>
    void CeilingFan()
    {
        float ceiling = CeilingY();
        if (ceiling <= 1f) return;

        Color metal = new Color32(0x8E, 0x8A, 0x84, 0xFF);

        var root = new GameObject("CeilingFan").transform;
        root.SetParent(transform, false);
        root.position = At(0.5f, 5.5f, ceiling - 0.9f);

        Rod(root, "Drop", Vector3.up * 0.45f, 0.05f, 0.9f, metal);
        Ball(root, "Hub", Vector3.zero, new Vector3(0.26f, 0.16f, 0.26f), metal, Finish.금속);

        fan = new GameObject("Blades").transform;
        fan.SetParent(root, false);

        for (int i = 0; i < 4; i++)
        {
            var blade = new GameObject($"Blade_{i}").transform;
            blade.SetParent(fan, false);
            blade.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
            Box(blade, "Vane", root.position + blade.rotation * new Vector3(0f, 0f, 0.62f),
                new Vector3(0.22f, 0.03f, 1.05f), new Color32(0x6B, 0x4A, 0x33, 0xFF));
        }
    }

    // ---- 프리미티브 도우미 -------------------------------------------------
    //
    // 전부 콜라이더를 뗀다. 안 떼면 소품이 사람과 곰을 막는다 -
    // 그리고 눌린 캡슐 콜라이더는 커다란 구로 부푼다(CLAUDE.md).

    Transform Box(Transform parent, string name, Vector3 worldAt, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        return Dress(go, parent, name, worldAt, size, color, Finish.무광, world: true);
    }

    /// <summary>
    /// 상자 하나를 <b>부모 기준</b>으로. <see cref="Box"/> 는 월드 좌표를 받는데,
    /// 차양처럼 <b>돌아가 있는 부모</b> 밑에 조각을 얹을 때는 로컬이어야 계산이 안 꼬인다.
    /// </summary>
    Transform Slab(Transform parent, string name, Vector3 localAt, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        return Dress(go, parent, name, localAt, size, color, Finish.무광, world: false);
    }

    Transform Ball(Transform parent, string name, Vector3 localAt, Vector3 size,
                   Color color, Finish finish = Finish.무광)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        return Dress(go, parent, name, localAt, size, color, finish, world: false);
    }

    /// <summary>가는 막대. <c>size = (굵기, 길이)</c> 를 캡슐 메시(높이 2)에 맞춰 바꿔 준다.</summary>
    Transform Rod(Transform parent, string name, Vector3 localAt, float thick, float length, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        return Dress(go, parent, name, localAt, new Vector3(thick, length * 0.5f, thick),
                     color, Finish.무광, world: false);
    }

    Transform Dress(GameObject go, Transform parent, string name, Vector3 at, Vector3 size,
                    Color color, Finish finish, bool world)
    {
        go.name = name;

        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        go.transform.SetParent(parent, false);
        if (world) go.transform.position = at;
        else go.transform.localPosition = at;

        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color, finish);
        return go.transform;
    }
}
