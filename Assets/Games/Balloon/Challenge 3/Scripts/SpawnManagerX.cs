using UnityEngine;
 
namespace Arcade.Games.Balloon
{
public class SpawnManagerX : MonoBehaviour
{
    // Money and Bomb prefabs
    public GameObject[] objectPrefabs;
 
    private float spawnDelay = 2.0f;
    private float spawnInterval = 1.5f;
 
    private PlayerControllerX playerControllerScript;
 
    void Start()
    {
        // Find Player before spawning begins
        playerControllerScript =
            GameObject.Find("Player")
                .GetComponent<PlayerControllerX>();
 
        // Start spawning after 2 seconds
        InvokeRepeating(
            "SpawnObjects",
            spawnDelay,
            spawnInterval
        );
    }
 
    void SpawnObjects()
    {
        // Do not spawn anything after game over
        if (!playerControllerScript.gameOver)
        {
            // Spawn at X 30 with a random height
            Vector3 spawnLocation = new Vector3(
                30,
                Random.Range(5, 15),
                0
            );
 
            // Randomly select Money or Bomb
            int index = Random.Range(
                0,
                objectPrefabs.Length
            );
 
            Instantiate(
                objectPrefabs[index],
                spawnLocation,
                objectPrefabs[index].transform.rotation
            );
        }
    }
}
}
