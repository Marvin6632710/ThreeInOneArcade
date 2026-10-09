using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Arcade.Games.Arena
{
public class PlayerControllerX : MonoBehaviour
{
    private Rigidbody playerRb;
    private float speed = 500;
    private GameObject focalPoint;

    public bool hasPowerup;
    public GameObject powerupIndicator;
    public int powerUpDuration = 5;

    private float normalStrength = 10; // how hard to hit enemy without powerup
    private float powerupStrength = 25; // how hard to hit enemy with powerup
    private float turboBoostStrength = 15;
    private ParticleSystem turboParticle;
    
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        focalPoint = GameObject.Find("Focal Point");

        // The starter scene includes this particle object. Make it follow the
        // focal point so the effect always appears behind the moving player.
        GameObject smokeObject = GameObject.Find("Smoke_Particle");
        if (smokeObject != null)
        {
            smokeObject.transform.SetParent(focalPoint.transform);
            smokeObject.transform.localPosition = new Vector3(0, -0.5f, -1.0f);
            smokeObject.transform.localRotation = Quaternion.Euler(0, 90, 0);
            turboParticle = smokeObject.GetComponent<ParticleSystem>();
        }
    }

    void Update()
    {
        // Add force to player in direction of the focal point (and camera)
        float verticalInput = GetVerticalInput();
        playerRb.AddForce(focalPoint.transform.forward * verticalInput * speed * Time.deltaTime); 

        // Set powerup indicator position to beneath player
        powerupIndicator.transform.position = transform.position + new Vector3(0, -0.6f, 0);

        // Optional challenge: press Space to launch the player forward.
        if (TurboPressedThisFrame())
        {
            playerRb.AddForce(focalPoint.transform.forward * turboBoostStrength, ForceMode.Impulse);

            if (turboParticle != null)
            {
                turboParticle.Play();
            }
        }

    }

    // Support both the newer Input System used by Unity 6 and the legacy
    // Input Manager used by the original course project.
    private float GetVerticalInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return 0;
        }

        float input = 0;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            input += 1;
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            input -= 1;
        }
        return input;
#else
        return Input.GetAxis("Vertical");
#endif
    }

    private bool TurboPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    // If Player collides with powerup, activate powerup
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Powerup"))
        {
            Destroy(other.gameObject);
            hasPowerup = true;
            powerupIndicator.SetActive(true);
            StartCoroutine(PowerupCooldown());
        }
    }

    // Coroutine to count down powerup duration
    IEnumerator PowerupCooldown()
    {
        yield return new WaitForSeconds(powerUpDuration);
        hasPowerup = false;
        powerupIndicator.SetActive(false);
    }

    // If Player collides with enemy
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            Rigidbody enemyRigidbody = other.gameObject.GetComponent<Rigidbody>();
            Vector3 awayFromPlayer = other.gameObject.transform.position - transform.position;
           
            if (hasPowerup) // if have powerup hit enemy with powerup force
            {
                enemyRigidbody.AddForce(awayFromPlayer * powerupStrength, ForceMode.Impulse);
            }
            else // if no powerup, hit enemy with normal strength 
            {
                enemyRigidbody.AddForce(awayFromPlayer * normalStrength, ForceMode.Impulse);
            }


        }
    }



}

}
