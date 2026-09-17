using UnityEngine;

/// <summary>
/// 로비에서 <b>둘러보기(궤도 카메라) ↔ 걷기</b> 를 오간다. TAB.
///
/// 2026-09-17 유저: *"항상 말을 걸면 안 되고 다가가야 말을 걸어야 하는데.
/// 사각형 오브젝트를 임시로 두고 그걸 조종해서 말을 걸어보자."* 맞는 방향이야 —
/// 어제 넣은 "카메라가 보고 있으면 말 걸기" 는 <b>조종하는 몸이 없어서</b> 쓴 임시방편이었고,
/// 몸이 생기면 원래대로 <b>거리</b>로 돌아가는 게 맞다. <see cref="BearNpc"/> 는
/// <c>lookTarget</c> 이 꽂혀 있으면 알아서 거리 방식으로 판단한다.
///
/// 이 몸은 <b>임시</b>다. 캐릭터 모델이 나오면 <c>Player</c> 안의 상자만 갈아 끼우면 되고,
/// 스크립트는 그대로 쓴다 — 그림을 껍데기 안에 넣지 밖에서 바꾸지 않는다(CLAUDE.md 규칙 1).
///
/// 캐릭터 고르기(궤도 카메라)와 걷기를 <b>한 화면에 섞지 않는다.</b> 마우스가 두 가지 일을
/// 하게 되면(시점 돌리기 / 받침대 클릭) 어느 쪽인지 알 수가 없다.
/// </summary>
public class WalkMode : MonoBehaviour
{
    [Header("걷기")]
    public FirstPersonController player;
    public Camera walkCamera;

    [Header("둘러보기")]
    public Camera browseCamera;

    [Tooltip("걷는 동안 멈춰 둘 것들 — 궤도 카메라, 받침대 고르기")]
    public Behaviour[] pauseWhileWalking;

    [Tooltip("걷기로 들어갈 때 설 자리. 비워두면 지금 자리")]
    public Transform entrance;

    public bool Walking { get; private set; }

    void Start() => Apply(false);

    public void Toggle() => Apply(!Walking);

    void Apply(bool walking)
    {
        Walking = walking;

        if (player != null)
        {
            player.gameObject.SetActive(walking);
            player.ControlEnabled = walking;

            // 들어갈 때마다 입구에 세운다. 전에 서 있던 자리가 어디였는지 기억할 이유가 없고,
            // 받침대 위나 곰 안에 남아 있으면 끼인 채로 시작한다.
            if (walking && entrance != null)
                player.transform.SetPositionAndRotation(entrance.position, entrance.rotation);
        }

        if (walkCamera != null) walkCamera.gameObject.SetActive(walking);
        if (browseCamera != null) browseCamera.gameObject.SetActive(!walking);

        foreach (var behaviour in pauseWhileWalking)
            if (behaviour != null) behaviour.enabled = !walking;

        // 걸을 때만 커서를 가둔다. 둘러보기에서는 받침대를 클릭해야 하니까 풀어 둔다.
        if (walking) CursorLock.Lock();
        else CursorLock.Unlock();
    }
}
