using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] prefabToSpawn;
    public float repeatInterval;
    public void Start()
    {
        if(repeatInterval > 0)
        {
            InvokeRepeating("SpawnObject", 0, repeatInterval);
        }
    }
    
    public GameObject SpawnObject()
    {
        if(prefabToSpawn != null && prefabToSpawn.Length > 0)
        {
            int index = Random.Range(0, prefabToSpawn.Length);
            return Instantiate(prefabToSpawn[index], transform.position, Quaternion.identity);
        }
        return null;
    }
}