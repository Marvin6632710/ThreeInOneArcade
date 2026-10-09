using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Arcade.Games.Arena
{
public class RotateCameraX : MonoBehaviour
{
    private float speed = 200;
    public GameObject player;

    // Update is called once per frame
    void Update()
    {
        float horizontalInput = GetHorizontalInput();
        transform.Rotate(Vector3.up, horizontalInput * speed * Time.deltaTime);

        transform.position = player.transform.position; // Move focal point with player

    }

    // Support Unity 6's Input System as well as the original Input Manager.
    private float GetHorizontalInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return 0;
        }

        float input = 0;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            input += 1;
        }
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            input -= 1;
        }
        return input;
#else
        return Input.GetAxis("Horizontal");
#endif
    }
}

}
