using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class PoolEntry
{
    public string name;
    public GameObject prefab;
    public int initialSize   = 5;
    public float spawnInterval = 2f;
    [Range(1, 100)]
    public int spawnWeight   = 10;
    public bool isHuddle;

    [HideInInspector] public Queue<GameObject> pool = new Queue<GameObject>();
    [HideInInspector] public float nextSpawnTime;
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager instancePool;

    [Header("Objects to Pool")]
    public List<PoolEntry> entries;

    [Header("Spawn Position")]
    public float spawnX = 10f;
    public float spawnYMin = -4f;
    public float spawnYMax = 4f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public bool  spawnOnRight = true;

    private void Awake()
    {
        instancePool = this;
    }

    private void Start()
    {
        foreach (var entry in entries)
        {
            for (int i = 0; i < entry.initialSize; i++)
            {
                var obj = CreateNewObject(entry);
                obj.SetActive(false);
                entry.pool.Enqueue(obj);
            }
            entry.nextSpawnTime = Time.time + entry.spawnInterval;
        }
    }

    private readonly List<PoolEntry> _readyEntries = new List<PoolEntry>();

    private void Update()
    {
        _readyEntries.Clear();
        foreach (var entry in entries)
        {
            if (Time.time >= entry.nextSpawnTime)
                _readyEntries.Add(entry);
        }

        if (_readyEntries.Count == 0) return;

        var chosen = GetWeightedRandom(_readyEntries);
        SpawnObject(chosen);
        chosen.nextSpawnTime = Time.time + chosen.spawnInterval;
    }

    public GameObject SpawnObject(PoolEntry entry)
    {
        var obj = GetFromPool(entry);

        float x = spawnOnRight ? spawnX : -spawnX;
        float y = entry.isHuddle
            ? PlaySceneManager.Instance.huddleSpawnPosition.position.y
            : Random.Range(spawnYMin, spawnYMax);

        obj.transform.position = new Vector3(x, y, 0f);
        obj.SetActive(true);

        var mover = obj.GetComponent<PooledObjectMover>();
        if (mover != null)
            mover.Initialize(moveSpeed, spawnOnRight, entry.name);
        else
            Debug.LogWarning($"[ObjectPoolManager] '{entry.name}' is missing a PooledObjectMover.");

        return obj;
    }

    public void ReturnToPool(string entryName, GameObject obj)
    {
        obj.SetActive(false);

        var entry = entries.Find(e => e.name == entryName);
        if (entry != null)
            entry.pool.Enqueue(obj);
        else
        {
            Debug.LogWarning($"[ObjectPoolManager] No pool entry '{entryName}'. Destroying object.");
            Destroy(obj);
        }
    }

    public void SetMoveSpeed(float fraction)
    {
        moveSpeed *= (1f + fraction);
        UpdateActiveMovers();
    }

    public void RestoreSpeed(float exactSpeed)
    {
        moveSpeed = exactSpeed;
        UpdateActiveMovers();
    }

    private void UpdateActiveMovers()
    {
        foreach (var mover in GetComponentsInChildren<PooledObjectMover>())
            mover.UpdateSpeed(moveSpeed);
    }

    private GameObject GetFromPool(PoolEntry entry)
    {
        while (entry.pool.Count > 0)
        {
            var candidate = entry.pool.Dequeue();
            if (candidate != null && !candidate.activeInHierarchy)
                return candidate;
        }
        return CreateNewObject(entry);
    }

    private GameObject CreateNewObject(PoolEntry entry)
    {
        var obj = Instantiate(entry.prefab, transform);
        obj.name = entry.name;
        return obj;
    }

    private PoolEntry GetWeightedRandom(List<PoolEntry> pool)
    {
        int totalWeight = 0;
        foreach (var e in pool) totalWeight += e.spawnWeight;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var e in pool)
        {
            cumulative += e.spawnWeight;
            if (roll < cumulative) return e;
        }
        return pool[pool.Count - 1];
    }
}
