using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Unity.PolySpatial;

public class AvatarClipVideoPlayerController : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    public GameObject woodpritePrefab;
    // Start is called before the first frame update
    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();   
    }

    private List<GameObject> woodprites = new List<GameObject>();

    void Update()
    {
        Unity.PolySpatial.PolySpatialObjectUtils.MarkDirty(videoPlayer.targetTexture);
        // Debug.Log($"Nadir: Current timestamp: {videoPlayer.time}");

        // if (videoPlayer.time >= 1f && videoPlayer.time <= 15f)
        // {
        //     SpawnWoodprites();
        // }
        // else if (videoPlayer.time > 200f)
        // {
        //     DestroyWoodprites();
        // }
    }

    void SpawnWoodprites()
    {
        if (woodprites.Count == 0)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 woodspritePosition = GenerateRandomVector() + transform.position;
                GameObject woodprite = Instantiate(woodpritePrefab, woodspritePosition, Quaternion.identity);
                woodprites.Add(woodprite);
            }
        }
    }

    // TODO figure out a way to randomly spawn woodprites in the right position/cluster

    void DestroyWoodprites()
    {
        foreach (GameObject woodprite in woodprites)
        {
            Destroy(woodprite);
        }
        woodprites.Clear();
    }

    private Vector3 minSpawnPosition = new Vector3(-50f, -50f, 0f);
    private Vector3 maxSpawnPosition = new Vector3(50f, 50f, 50f);

    Vector3 GenerateRandomVector()
    {
        float randomX = Random.Range(minSpawnPosition.x, maxSpawnPosition.x);
        float randomY = Random.Range(minSpawnPosition.y, maxSpawnPosition.y);
        float randomZ = Random.Range(minSpawnPosition.z, maxSpawnPosition.z);
        return new Vector3(randomX, randomY, randomZ);
    }
}
