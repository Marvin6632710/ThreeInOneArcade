using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arcade.Games.Balloon
{
public class SpinObjectsX : MonoBehaviour
{
    public float spinSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
    }
}

}
