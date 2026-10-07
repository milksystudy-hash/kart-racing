using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 중앙홀 받침대 위에 <b>2D 초상화를 세운다</b> — 기념품점의 종이 스탠디처럼.
///
/// ★★ 2026-09-16 에 «3D 캐릭터는 0개, 받침대에는 같은 2D 초상화를 스탠디로» 로 정해 놓고
/// 그림이 없어서 미뤄 뒀던 자리다. 일곱 명 × 세 표정 <b>21장이 다 들어왔으니</b> 이제 세운다.
///
/// ★★ 2026-10-06 강사님: *"튜토리얼 중에 세진이가 누구고 세운이가 누군지 모르겠다."*
/// <b>이게 제일 직접적인 답이다.</b> 글로 «막내 · 체육학과» 라고 적는 것보다,
/// 고르는 자리에 <b>그 얼굴이 서 있는 것</b>이 빠르다 — 이야기에서 그 얼굴이 다시 나오니까
/// 두 번째부터는 이름을 안 읽어도 누군지 안다.
///
/// <list type="bullet">
/// <item><b>모델링이 필요 없다.</b> 이야기 장면이 쓰는 그 PNG 를 그대로 쓴다.</item>
/// <item><b>런타임에 세운다.</b> 로비를 다시 굽지 않아도 들어오고 씬 파일이 안 커진다.</item>
/// <item>잠긴 자리(개발업자 · 시의원)는 <b>안 세운다</b> — 이름이 «???» 인데 얼굴이 있으면
///       숨긴 의미가 없다.</item>
/// </list>
/// </summary>
public static class StandStandee
{
    /// <summary>서 있는 키. 기획서의 3등신 1.25m 를 그대로 쓴다.</summary>
    const float Height = 1.42f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Build();
    }

    static void OnLoaded(Scene s, LoadSceneMode m) => Build();

    static void Build()
    {
        foreach (var stand in Object.FindObjectsByType<CharacterStand>(FindObjectsSortMode.None))
        {
            if (stand.locked) continue;
            if (stand.transform.Find("Standee") != null) continue;

            var art = Cast.Portrait(stand.CastId, Cast.Mood.기본);
            if (art == null) continue;

            // 받침대 꼭대기. FBX 를 꽂으라고 만들어 둔 자리라 발이 정확히 여기 온다
            var anchor = stand.transform.Find("ModelAnchor") ?? stand.transform;

            // ★★ 2026-10-06 유저: *"캐릭터 캡슐과 스탠딩이 아예 겹쳐졌다."*
            //   받침대에는 <b>임시 자리표시 캡슐</b>이 꽂혀 있다 — 3D 캐릭터가 올 자리였다.
            //   초상화가 그 자리를 대신하기로 한 게 2026-09-16 결정이니 <b>캡슐은 물러난다.</b>
            //   지우지 않고 <b>끄기만</b> 한다: 되돌리고 싶으면 켜면 되고, 씬 파일은 그대로다.
            var ph = anchor.Find("Placeholder");
            if (ph != null) ph.gameObject.SetActive(false);

            var root = new GameObject("Standee").transform;
            root.SetParent(anchor, false);
            root.localPosition = Vector3.zero;

            // 홀 한가운데를 본다. 받침대가 어느 쪽에 서 있든 같은 숫자가 통한다
            Vector3 toHall = -new Vector3(anchor.position.x, 0f, anchor.position.z);
            if (toHall.sqrMagnitude > 0.01f)
                root.rotation = Quaternion.LookRotation(toHall.normalized, Vector3.up);

            float w = Height * art.width / Mathf.Max(1, art.height);

            // ★ <b>앞뒤로 한 장씩</b> 둔다. 유니티 Quad 는 한쪽 면만 보여서, 한 장만 세우면
            //   반대쪽에서 <b>통째로 사라진다</b> — 이 프로젝트에서 방향을 다섯 번 틀렸으니
            //   «어느 쪽이 앞인가» 를 아예 안 묻는 쪽으로 짓는다.
            Face(root, "Front", art, w, 0f);
            Face(root, "Back", art, w, 180f);

            // 발밑 받침 — 종이 스탠디는 받침이 있어야 선다. 없으면 바닥에 뜬 그림이다
            var foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(foot.GetComponent<Collider>());
            foot.name = "Foot";
            foot.transform.SetParent(root, false);
            foot.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            foot.transform.localScale = new Vector3(w * 0.72f, 0.05f, 0.26f);
            foot.GetComponent<MeshRenderer>().sharedMaterial =
                FlatMaterial.Get(new Color32(0x6B, 0x4A, 0x33, 0xFF));
        }
    }

    static void Face(Transform parent, string name, Texture2D art, float w, float yaw)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(q.GetComponent<Collider>());
        q.name = name;
        q.transform.SetParent(parent, false);
        q.transform.localPosition = new Vector3(0f, Height * 0.5f + 0.05f, 0f);
        q.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        q.transform.localScale = new Vector3(w, Height, 1f);

        // 언릿 — 홀 조명이 어두워도 얼굴이 또렷해야 «누군지 알아보는» 일을 한다.
        // 투명 설정을 해야 인물 둘레의 빈칸이 흰 상자로 안 보인다.
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetFloat("_Surface", 1f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 1);
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.35f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = 2450;
        m.SetTexture("_BaseMap", art);
        m.SetColor("_BaseColor", Color.white);

        q.GetComponent<MeshRenderer>().sharedMaterial = m;
    }
}
