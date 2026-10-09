using UnityEngine;
 
namespace Arcade.Games.Balloon
{
public class MoveLeftX : MonoBehaviour
{
    public float speed;
 
    private PlayerControllerX playerControllerScript;
    private float leftBound = -10;
 
    void Start()
    {
        // Find Player and access its gameOver variable
        playerControllerScript =
            GameObject.Find("Player")
                .GetComponent<PlayerControllerX>();
    }
 
    void Update()
    {
        // Move only while the game is active
        if (!playerControllerScript.gameOver)
        {
            transform.Translate(
                Vector3.left * speed * Time.deltaTime,
                Space.World
            );
        }
 
        // Destroy off-screen Money and Bomb objects.
        // Do not destroy the repeating Background.
        if (transform.position.x < leftBound &&
            !gameObject.CompareTag("Background"))
        {
            Destroy(gameObject);
        }
    }
}
}
