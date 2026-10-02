using UnityEngine;

/// <summary>
/// 걸어다니는 <b>몸</b>. 지금까지 걷기 모드는 <b>그림이 하나도 없는 1인칭 카메라</b>였다 —
/// 2026-09-17 에 «사각형 오브젝트를 임시로 두고 그걸 조종하자» 고 해놓고 실제로는
/// 상자를 한 번도 안 만들었고, 그래서 내내 «몸 없는 유령» 이었다.
///
/// 2026-10-01 유저: *"제출할 때 TAB 눌러서 곰인형한테 말 걸거나 캠퍼스를 돌아다닐 수는
/// 없으니까, 임시로 로비에서 자신이 선택한 캡슐이 그 역할을 해 주게 해 줘."*
///
/// 그래서 <b>로비 받침대에 선 그 캡슐</b>을 그대로 몸으로 쓴다 — 같은 치수(키 1.15m ·
/// 지름 0.45m), 같은 색(<see cref="Cast"/> 의 인물 색). 고른 캐릭터가 걸어다니는 걸로
/// 읽히고, 받침대에서 본 것과 <b>같은 물건</b>이라 설명이 필요 없다.
///
/// ★ <b>스스로 붙는다.</b> <see cref="FirstPersonController"/> 가 Awake 에서 불러 주니
/// 씬을 다시 구울 필요가 없다 — «새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다» 를
/// 다섯 번 겪은 뒤의 기본형.
///
/// ★ <b>콜라이더는 안 단다.</b> 몸은 <c>CharacterController</c> 가 이미 가지고 있고,
/// 그림이 물리를 바꾸면 안 된다(CLAUDE.md 규칙 2). 3인칭 카메라가 뒤로 빠질 때
/// 제 몸에 걸려 멈추는 것도 이 덕분에 안 생긴다.
///
/// 캐릭터 모델이 나오면 <b>이 캡슐만 갈아 끼운다</b> — <c>Holder</c> 가 그대로 남아서
/// 스크립트는 하나도 안 바뀐다(규칙 1).
/// </summary>
[DisallowMultipleComponent]
public class VisitorBody : MonoBehaviour
{
    public const string HolderName = "Body";

    /// <summary>로비 받침대의 자리표시와 같은 값 — 치비 서 있는 키 1.15m.</summary>
    const float Height = 1.15f;
    const float Width = 0.45f;

    Transform holder;
    Renderer skin;
    string wearing;

    /// <summary>없으면 붙이고, 있으면 그대로 돌려준다.</summary>
    public static VisitorBody Ensure(GameObject host)
    {
        var body = host.GetComponent<VisitorBody>();
        if (body == null) body = host.AddComponent<VisitorBody>();
        return body;
    }

    void Awake() => Build();

    void Build()
    {
        if (holder != null) return;

        // ★ 2026-10-01 유저: *"캡슐 사각형 인간몸 말고 캡슐 자체가 움직이게 해 줘."*
        //   예전에 <see cref="LobbySceneBuilder"/> 가 <b>나무색 상자 몸통 + 돌색 머리</b>를
        //   넣어 뒀는데, 그 안에 캡슐을 세우니 «상자에 캡슐이 박힌 것» 이 됐다.
        //   빌더에서도 뺐지만, <b>이미 구운 씬에는 남아 있다</b> — 여기서 같이 걷어낸다.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var kid = transform.GetChild(i);
            if (kid.name != "TempBody" && kid.name != "TempHead") continue;
            if (Application.isPlaying) Destroy(kid.gameObject);
            else DestroyImmediate(kid.gameObject);
        }

        var found = transform.Find(HolderName);
        holder = found != null ? found : new GameObject(HolderName).transform;
        holder.SetParent(transform, false);
        holder.localPosition = Vector3.zero;
        holder.localRotation = Quaternion.identity;
        if (found != null && holder.childCount > 0) { Paint(); return; }   // 진짜 모델이 들어와 있다

        // 몸통 — 유니티 캡슐 메시는 높이 2 라 반으로 나눠 준다
        skin = Piece(PrimitiveType.Capsule, "Skin",
                     new Vector3(0f, Height * 0.5f, 0f),
                     new Vector3(Width, Height * 0.5f, Width));

        // ★ 앞을 가리키는 표시는 <b>안 붙인다.</b> 한 번 넣었다가 뺐다 —
        //   유저: *"캡슐 사각형 인간몸 말고 캡슐 자체가 움직이게 해 줘."*
        //   3인칭이라 <b>카메라가 곧 몸의 방향</b>이고, 어느 쪽을 보는지는 화면이 이미 말해 준다.
        //   받침대에 선 캡슐과 <b>똑같이 생겨야</b> «저게 내가 고른 애» 로 읽힌다.

        Paint();
    }

    Renderer Piece(PrimitiveType shape, string name, Vector3 at, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
        go.transform.SetParent(holder, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        return go.GetComponent<Renderer>();
    }

    void Update()
    {
        // 전시실에 갔다 오거나 로비에서 다시 고르면 그 자리에서 갈아입는다
        if (skin != null && wearing != GameSelection.SelectedCastId) Paint();
    }

    void Paint()
    {
        wearing = GameSelection.SelectedCastId;
        if (skin == null) return;

        Color c = string.IsNullOrEmpty(wearing)
                ? new Color32(0xC9, 0xAC, 0x8A, 0xFF)      // 아직 안 골랐으면 나무색
                : Cast.ColorOf(wearing);

        skin.sharedMaterial = FlatMaterial.Get(c);
    }
}
