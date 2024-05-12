using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

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
        Debug.Log($"Nadir: Current timestamp: {videoPlayer.time}");

        if (videoPlayer.time >= 1f && videoPlayer.time <= 15f)
        {
            SpawnWoodprites();
        }
        else if (videoPlayer.time > 200f)
        {
            DestroyWoodprites();
        }
    }

    void SpawnWoodprites()
    {
        if (woodprites.Count == 0)
        {
            for (int i = 0; i < 1; i++)
            {
                GameObject woodprite = Instantiate(woodpritePrefab, transform.position, Quaternion.identity);
                woodprites.Add(woodprite);
            }
        }
    }

    void DestroyWoodprites()
    {
        foreach (GameObject woodprite in woodprites)
        {
            Destroy(woodprite);
        }
        woodprites.Clear();
    }
}
