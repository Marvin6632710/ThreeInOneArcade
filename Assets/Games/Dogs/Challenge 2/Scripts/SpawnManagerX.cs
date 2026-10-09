using UnityEngine;

namespace Arcade.Games.Dogs
{
public class SpawnManagerX : MonoBehaviour
{
    public GameObject[] ballPrefabs;

    private float spawnLimitXLeft = -22f;
    private float spawnLimitXRight = 7f;
    private float spawnPosY = 30f;

    private float startDelay = 1f;
    private float spawnInterval = 4f;

    void Start()
    {
        InvokeRepeating(
            nameof(SpawnRandomBall),
            startDelay,
            spawnInterval
        );
    }

    void SpawnRandomBall()
    {
        // Randomly choose one ball from the array
        int ballIndex = Random.Range(0, ballPrefabs.Length);

        // Generate a random X position
        float randomX = Random.Range(
            spawnLimitXLeft,
            spawnLimitXRight
        );

        Vector3 spawnPosition = new Vector3(
            randomX,
            spawnPosY,
            0f
        );

        // Generate the selected ball
        Instantiate(
            ballPrefabs[ballIndex],
            spawnPosition,
            ballPrefabs[ballIndex].transform.rotation
        );
    }
}

}
