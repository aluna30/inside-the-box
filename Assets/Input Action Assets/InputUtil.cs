using UnityEngine;
using UnityEngine.InputSystem;

public class InputUtil : MonoBehaviour
{
    public const string INPUT_DIRECTION_UP = "up";
    public const string INPUT_DIRECTION_DOWN = "down";
    public const string INPUT_DIRECTION_LEFT = "left";
    public const string INPUT_DIRECTION_RIGHT = "right";
    public const string INPUT_DIRECTION_IDLE = "idle";

    private static PlayerControls playerControls = new PlayerControls();

    public static string GetInputContextCompositeName(InputAction.CallbackContext context)
    {
        return playerControls.Player.Movement.bindings[playerControls.Player.Movement.GetBindingIndexForControl(context.control)].name;
    }

    public static string GetControlCompositeName(InputControl control)
    {
        return playerControls.Player.Movement.bindings[playerControls.Player.Movement.GetBindingIndexForControl(control)].name;
    }

    public static string GetInputDirection(InputAction movement)
    {
        string direction = INPUT_DIRECTION_IDLE;

        if (movement != null)
        {
            float x = movement.ReadValue<Vector2>().x;
            float y = movement.ReadValue<Vector2>().y;
            
            if (Mathf.Abs(x) == 1)
            {
                direction = (x == 1) ? INPUT_DIRECTION_RIGHT : INPUT_DIRECTION_LEFT;
            }            
            if (Mathf.Abs(y) == 1)
            {
                direction = (y == 1) ? INPUT_DIRECTION_UP : INPUT_DIRECTION_DOWN;
            }
            else if (Mathf.Abs(x) == Mathf.Abs(y))
            {
                direction = GetControlCompositeName(movement.activeControl);
            }
        }

        return direction;
    }
}
