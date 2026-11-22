using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField] NetworkObject pickupPrefab;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] float firstSpawnDelay = 5f;
    [SerializeField] float spawnInterval = 15f;
    [SerializeField] int maxActive = 2;
    [SerializeField] float heightOffset = 0.75f;

    private readonly List<NetworkObject> activePickups = new List<NetworkObject>();
    private float timer;
    private bool wasServer;

    private void Update()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsServer)
        {
            wasServer = false;
            activePickups.Clear();
            return;
        }

        if (!wasServer)
        {
            wasServer = true;
            timer = firstSpawnDelay;
        }

        activePickups.RemoveAll(pickup => pickup == null || !pickup.IsSpawned);

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        timer = spawnInterval;

        if (activePickups.Count >= maxActive)
            return;

        SpawnPickup();
    }

    private void SpawnPickup()
    {
        Transform point = GetFreePoint();

        if (point == null || pickupPrefab == null)
            return;

        PowerUpType type = (PowerUpType)Random.Range(1, 4);

        NetworkObject pickup = Instantiate(pickupPrefab, point.position + Vector3.up * heightOffset, Quaternion.identity);

        if (pickup.TryGetComponent(out PowerUpPickup pickupScript))
            pickupScript.Type.Value = type;

        pickup.Spawn();
        activePickups.Add(pickup);
    }

    private Transform GetFreePoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        List<Transform> freePoints = new List<Transform>();

        foreach (Transform point in spawnPoints)
        {
            bool occupied = false;

            foreach (NetworkObject pickup in activePickups)
            {
                Vector3 flatPickup = new Vector3(pickup.transform.position.x, 0f, pickup.transform.position.z);
                Vector3 flatPoint = new Vector3(point.position.x, 0f, point.position.z);

                if (Vector3.Distance(flatPickup, flatPoint) < 1f)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
                freePoints.Add(point);
        }

        if (freePoints.Count == 0)
            return null;

        return freePoints[Random.Range(0, freePoints.Count)];
    }
}