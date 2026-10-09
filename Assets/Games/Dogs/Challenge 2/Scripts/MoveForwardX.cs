using UnityEngine;

namespace Arcade.Games.Dogs
{
public class MoveForwardX : MonoBehaviour
{
    public float speed;

    void Update()
    {
        transform.Translate(
            Vector3.forward * speed * Time.deltaTime
        );
    }
}

}
