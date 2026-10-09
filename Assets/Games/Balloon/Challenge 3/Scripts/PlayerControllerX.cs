using UnityEngine;
 
namespace Arcade.Games.Balloon
{
public class PlayerControllerX : MonoBehaviour
{
    // Game state
    public bool gameOver;
    public bool isLowEnough;
 
    // Balloon movement
    public float floatForce = 50.0f;
 
    private const float startX = -3.0f;
    private const float startY = 12.0f;
    private const float fixedZ = 0.0f;
    private const float maximumHeight = 13.0f;
 
    private float gravityModifier = 1.5f;
 
    private Rigidbody playerRb;
    private bool spaceHeld;
 
    // Particle effects
    public ParticleSystem explosionParticle;
    public ParticleSystem fireworksParticle;
 
    // Audio
    private AudioSource playerAudio;
 
    public AudioClip moneySound;
    public AudioClip explodeSound;
    public AudioClip bounceSound;
 
    void Start()
    {
        // Get Player components
        playerRb = GetComponent<Rigidbody>();
        playerAudio = GetComponent<AudioSource>();
 
        // Always begin at the correct position
        playerRb.position = new Vector3(
            startX,
            startY,
            fixedZ
        );
 
        transform.rotation = Quaternion.identity;
 
        // Remove movement left over from earlier Play Mode tests
        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
 
        // Allow only vertical movement and Y rotation
        playerRb.constraints =
            RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
 
        // Set gravity to an exact value
        Physics.gravity =
            Vector3.down * 9.81f * gravityModifier;
 
        // Give the balloon a small starting push
        playerRb.AddForce(
            Vector3.up * 5.0f,
            ForceMode.Impulse
        );
    }
 
    void Update()
    {
        // Read the Space key
        spaceHeld = Input.GetKey(KeyCode.Space);
    }
 
    void FixedUpdate()
    {
        // Keep Player locked to the correct X and Z positions
        Vector3 correctedPosition = playerRb.position;
 
        correctedPosition.x = startX;
        correctedPosition.z = fixedZ;
 
        // Hard-limit the balloon's maximum height
        if (correctedPosition.y > maximumHeight)
        {
            correctedPosition.y = maximumHeight;
        }
 
        playerRb.position = correctedPosition;
 
        // Stop upward velocity after reaching the height limit
        if (playerRb.position.y >= maximumHeight &&
            playerRb.linearVelocity.y > 0)
        {
            Vector3 correctedVelocity =
                playerRb.linearVelocity;
 
            correctedVelocity.y = 0;
 
            playerRb.linearVelocity =
                correctedVelocity;
        }
 
        // Update the public boolean shown in the Inspector
        isLowEnough =
            playerRb.position.y < maximumHeight;
 
        // Apply upward force at Unity's physics rate
        if (spaceHeld && isLowEnough && !gameOver)
        {
            playerRb.AddForce(
                Vector3.up * floatForce,
                ForceMode.Force
            );
        }
    }
 
    private void OnCollisionEnter(Collision other)
    {
        // Bomb collision
        if (other.gameObject.CompareTag("Bomb"))
        {
            if (explosionParticle != null)
            {
                explosionParticle.Play();
            }
 
            PlaySound(explodeSound);
 
            gameOver = true;
            Debug.Log("Game Over!");
 
            Destroy(other.gameObject);
        }
        // Money collision
        else if (other.gameObject.CompareTag("Money"))
        {
            if (fireworksParticle != null)
            {
                fireworksParticle.Play();
            }
 
            PlaySound(moneySound);
 
            Destroy(other.gameObject);
        }
        // Ground collision
        else if (other.gameObject.CompareTag("Ground") &&
                 !gameOver)
        {
            playerRb.AddForce(
                Vector3.up * 10.0f,
                ForceMode.Impulse
            );
 
            PlaySound(bounceSound);
        }
    }
 
    private void PlaySound(AudioClip clip)
    {
        // Prevent null AudioClip warnings
        if (playerAudio != null && clip != null)
        {
            playerAudio.PlayOneShot(clip, 1.0f);
        }
    }
}
}
