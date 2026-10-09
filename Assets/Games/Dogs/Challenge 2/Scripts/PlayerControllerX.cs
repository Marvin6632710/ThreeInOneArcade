using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Arcade.Games.Dogs
{
public class PlayerControllerX : MonoBehaviour
{
    public GameObject dogPrefab;
    public float moveSpeed = 10f;

    private float minX = 8f;
    private float maxX = 17f;

    void Update()
    {
        // Get W/S or Up/Down Arrow input
        float verticalInput = ReadVerticalInput();

        // Move the player forward and backward
        transform.Translate(
            Vector3.forward * verticalInput * moveSpeed * Time.deltaTime
        );

        // Keep the player inside the allowed area
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        transform.position = position;

        // Press Spacebar to send a dog
        if (DogWasPressed())
        {
            Instantiate(
                dogPrefab,
                transform.position,
                dogPrefab.transform.rotation
            );
        }
    }

    private float ReadVerticalInput()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard == null) return 0f;
        float forward = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f;
        float backward = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f;
        return forward - backward;
#else
        return Input.GetAxis("Vertical");
#endif
    }

    private bool DogWasPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}

}
