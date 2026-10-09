using UnityEngine;

namespace Arcade.Games.Dogs
{
public class DestroyOutOfBoundsX : MonoBehaviour
{
    private float leftLimit = -30f;
    private float bottomLimit = -5f;

    void Update()
    {
        // Remove dogs after they leave the left side
        if (transform.position.x < leftLimit)
        {
            Destroy(gameObject);
        }

        // Remove missed balls and display Game Over
        else if (transform.position.y < bottomLimit)
        {
            Debug.Log("Game Over!");
            Destroy(gameObject);
        }
    }
}
}
