using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 키보드와 게임패드를 한 군데서 읽는다.
/// 이 프로젝트는 Input System 패키지를 쓰기 때문에 옛날 방식인 Input.GetAxis 를 쓰면 에러가 난다.
///
/// 키보드는 눌렀다/뗐다 두 값뿐이라 그대로 쓰면 조향이 딱딱해서, 여기서 부드럽게 밀어준다.
/// 게임패드 스틱은 원래 아날로그라 거의 그대로 쓴다.
/// </summary>
public static class KartInput
{
    /// <summary>-1(후진) ~ +1(전진)</summary>
    public static float Throttle { get; private set; }
    /// <summary>-1(좌) ~ +1(우)</summary>
    public static float Steer { get; private set; }
    public static bool Drift { get; private set; }

    public static bool RespawnPressed =>
        (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);

    public static bool RestartPressed =>
        (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame ||
                                      Keyboard.current.numpadEnterKey.wasPressedThisFrame)) ||
        (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

    const float KeyboardSteerRate = 5f;     // 낮출수록 조향이 느긋해진다
    const float KeyboardThrottleRate = 8f;
    const float AnalogRate = 20f;

    static int lastTickedFrame = -1;

    /// <summary>프레임당 한 번만 실제로 계산한다. 여러 스크립트가 불러도 안전하다.</summary>
    public static void Tick(float deltaTime)
    {
        if (lastTickedFrame == Time.frameCount) return;
        lastTickedFrame = Time.frameCount;

        float steerTarget = 0f;
        float throttleTarget = 0f;
        bool drift = false;
        bool analog = false;

        var pad = Gamepad.current;
        if (pad != null)
        {
            float stick = pad.leftStick.x.ReadValue();
            float accelerate = pad.rightTrigger.ReadValue();
            float reverse = pad.leftTrigger.ReadValue();

            if (Mathf.Abs(stick) > 0.12f) { steerTarget = stick; analog = true; }
            if (accelerate > 0.05f || reverse > 0.05f) { throttleTarget = accelerate - reverse; analog = true; }

            drift = pad.buttonSouth.isPressed || pad.rightShoulder.isPressed;
        }

        // 키보드를 누르고 있으면 게임패드보다 우선한다
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float horizontal = 0f, vertical = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    vertical += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  vertical -= 1f;

            if (!Mathf.Approximately(horizontal, 0f)) { steerTarget = horizontal; analog = false; }
            if (!Mathf.Approximately(vertical, 0f))   { throttleTarget = vertical; analog = false; }

            drift |= keyboard.spaceKey.isPressed;
        }

        float steerRate = analog ? AnalogRate : KeyboardSteerRate;
        float throttleRate = analog ? AnalogRate : KeyboardThrottleRate;

        Steer = Mathf.MoveTowards(Steer, steerTarget, steerRate * deltaTime);
        Throttle = Mathf.MoveTowards(Throttle, throttleTarget, throttleRate * deltaTime);
        Drift = drift;
    }

    /// <summary>리스폰이나 재시작 직후에 입력이 남아 있지 않게.</summary>
    public static void Clear()
    {
        Steer = 0f;
        Throttle = 0f;
        Drift = false;
    }
}
