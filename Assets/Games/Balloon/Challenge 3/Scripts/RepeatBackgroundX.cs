using UnityEngine;
 
namespace Arcade.Games.Balloon
{
public class RepeatBackgroundX : MonoBehaviour
{
    private Vector3 startPos;
    private float repeatWidth;
 
    void Start()
    {
        // Remember the background's original position
        startPos = transform.position;
 
        // Use half the background's X width
        repeatWidth =
            GetComponent<BoxCollider>().size.x / 2;
    }
 
    void Update()
    {
        // Return the background to its starting position
        // after it moves left by one repeat width
        if (transform.position.x <
            startPos.x - repeatWidth)
        {
            transform.position = startPos;
        }
    }
}
}
