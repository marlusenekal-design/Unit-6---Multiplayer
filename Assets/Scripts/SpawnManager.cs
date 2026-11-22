using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] Transform[] spawnPoints;
    [SerializeField] float occupiedRadius = 3f;
    [SerializeField] float reserveTime = 2f;
    [SerializeField] int maxPlayers = 4;

    private readonly Dictionary<Transform, float> reservedUntil = new Dictionary<Transform, float>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectionApprovalCallback == ApprovalCheck)
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= maxPlayers)
        {
            response.Approved = false;
            response.Reason = "Game is full";
            response.Pending = false;
            return;
        }

        GetSpawn(out Vector3 position, out Quaternion rotation);

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = position;
        response.Rotation = rotation;
        response.Pending = false;
    }

    public void GetSpawn(out Vector3 position, out Quaternion rotation)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return;
        }

        List<Transform> freePoints = new List<Transform>();

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            if (!IsReserved(point) && IsFree(point.position))
                freePoints.Add(point);
        }

        if (freePoints.Count == 0)
        {
            foreach (Transform point in spawnPoints)
            {
                if (point != null && !IsReserved(point))
                    freePoints.Add(point);
            }
        }

        Transform selected = freePoints.Count > 0
            ? freePoints[Random.Range(0, freePoints.Count)]
            : spawnPoints[Random.Range(0, spawnPoints.Length)];

        reservedUntil[selected] = Time.time + reserveTime;

        position = selected.position;
        rotation = selected.rotation;
    }

    private bool IsReserved(Transform point)
    {
        return reservedUntil.TryGetValue(point, out float until) && Time.time < until;
    }

    private bool IsFree(Vector3 point)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return true;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            if (Vector3.Distance(client.PlayerObject.transform.position, point) < occupiedRadius)
                return false;
        }

        return true;
    }
}