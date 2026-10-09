using UnityEngine;

namespace Arcade.Games.Dogs
{
public class DetectCollisionsX : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Dog"))
        {
            Destroy(gameObject);
        }
    }
}
}
