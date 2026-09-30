using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Every gameplay control in one place, read from the Input System package. The keys, and the smoothing of
// Horizontal, match the old Input Manager setup, so the game plays the same. Add gamepad bindings here.
// UI clicks and navigation don't go through this: the EventSystem in UI.prefab uses InputSystemUIInputModule.
public static class GameInput
{
    // The Input Manager's "Horizontal" axis moved toward the held direction at Sensitivity units per second,
    // fell back to 0 at Gravity units per second, and snapped to 0 when the direction reversed
    private const float AxisSensitivity = 3f;
    private const float AxisGravity = 3f;
    private const float AxisDeadZone = 0.001f;

    private static float horizontal;
    private static int horizontalFrame = -1;
    private static float horizontalTime;

    private static Keyboard Keys => Keyboard.current;
    private static Mouse Pointer => Mouse.current;

    // -1 to 1, smoothed like Input.GetAxis("Horizontal")
    public static float Horizontal
    {
        get
        {
            if (horizontalFrame != Time.frameCount)
            {
                horizontalFrame = Time.frameCount;
                float deltaTime = Time.unscaledTime - horizontalTime;
                horizontalTime = Time.unscaledTime;
                horizontal = SmoothAxis(horizontal, HorizontalRaw, deltaTime);
            }
            return Mathf.Abs(horizontal) < AxisDeadZone ? 0f : horizontal;
        }
    }

    // -1, 0 or 1 from A/D and the arrow keys
    public static float HorizontalRaw
    {
        get
        {
            if (Keys == null)
                return 0f;
            float value = 0f;
            if (Keys.dKey.isPressed || Keys.rightArrowKey.isPressed)
                value += 1f;
            if (Keys.aKey.isPressed || Keys.leftArrowKey.isPressed)
                value -= 1f;
            return value;
        }
    }

    public static bool JumpPressed => WasPressed(Keys?.spaceKey);
    public static bool JumpHeld => Keys != null && Keys.spaceKey.isPressed; // Releasing early cuts a jump short
    public static bool DownHeld => Keys != null && (Keys.sKey.isPressed || Keys.downArrowKey.isPressed); // Drop from a ledge
    public static bool RollPressed => WasPressed(Keys?.leftShiftKey);
    public static bool AttackPressed => WasPressed(Pointer?.leftButton);
    public static bool BlockPressed => WasPressed(Pointer?.rightButton);
    public static bool BlockHeld => Pointer != null && Pointer.rightButton.isPressed;
    public static bool InteractPressed => WasPressed(Keys?.eKey);
    public static bool AdvanceDialogPressed => InteractPressed || AttackPressed;
    public static bool InventoryPressed => WasPressed(Keys?.iKey);
    public static bool CancelPressed => WasPressed(Keys?.escapeKey); // Pause, or close an open window

    private static bool WasPressed(ButtonControl button) => button != null && button.wasPressedThisFrame;

    private static float SmoothAxis(float value, float target, float deltaTime)
    {
        if (target != 0f && value != 0f && Mathf.Sign(target) != Mathf.Sign(value))
            value = 0f; // Snap when reversing
        float speed = target != 0f ? AxisSensitivity : AxisGravity;
        return Mathf.MoveTowards(value, target, speed * deltaTime);
    }
}
