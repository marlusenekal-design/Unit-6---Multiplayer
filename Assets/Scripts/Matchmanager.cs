using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum MatchState
{
    Playing,
    Ended
}

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Match Settings")]
    public float MatchDuration = 180f;
    public float ResultsDuration = 8f;

    // - Networked Variables
    public NetworkVariable<MatchState> State = new NetworkVariable<MatchState>(
        MatchState.Playing, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> MatchEndTime = new NetworkVariable<double>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> NextRoundTime = new NetworkVariable<double>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> ResultText = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes(""), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsPlaying => !IsSpawned || State.Value == MatchState.Playing;

    public float TimeRemaining => Mathf.Max(0f, (float)(MatchEndTime.Value - NetworkManager.ServerTime.Time));

    public float TimeUntilNextRound => Mathf.Max(0f, (float)(NextRoundTime.Value - NetworkManager.ServerTime.Time));

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartMatch();
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned)
            return;

        double now = NetworkManager.ServerTime.Time;

        if (State.Value == MatchState.Playing)
        {
            if (now >= MatchEndTime.Value || OnlyOneTankLeft())
                EndMatch();
        }
        else
        {
            if (now >= NextRoundTime.Value)
                StartNewRound();
        }
    }

    private void StartMatch()
    {
        State.Value = MatchState.Playing;
        MatchEndTime.Value = NetworkManager.ServerTime.Time + MatchDuration;
        ResultText.Value = new FixedString64Bytes("");
    }

    private void EndMatch()
    {
        State.Value = MatchState.Ended;
        ResultText.Value = new FixedString64Bytes(GetResult());
        NextRoundTime.Value = NetworkManager.ServerTime.Time + ResultsDuration;

        Debug.Log("Match Over: " + ResultText.Value);
    }

    private void StartNewRound()
    {
        // - Reset every player
        foreach (NetworkStats player in GetAllPlayers())
        {
            player.ResetForNewMatch();

            if (player.TryGetComponent(out NetworkPowerUps powerUps))
                powerUps.Clear();
        }

        PowerUpPickup[] pickups = FindObjectsByType<PowerUpPickup>(FindObjectsSortMode.None);

        foreach (PowerUpPickup pickup in pickups)
        {
            if (pickup.IsSpawned)
                pickup.NetworkObject.Despawn();
        }

        StartMatch();
    }

    private List<NetworkStats> GetAllPlayers()
    {
        List<NetworkStats> players = new List<NetworkStats>();

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out NetworkStats stats))
                players.Add(stats);
        }

        return players;
    }

    private bool OnlyOneTankLeft()
    {
        List<NetworkStats> players = GetAllPlayers();

        if (players.Count < 2)
            return false;

        int stillIn = players.Count(p => p.IsAlive.Value || p.Lives.Value > 0);

        return stillIn <= 1;
    }

    private string GetResult()
    {
        List<NetworkStats> ranking = GetAllPlayers()
            .OrderByDescending(p => p.Score.Value)
            .ThenByDescending(p => p.Lives.Value)
            .ToList();

        if (ranking.Count == 0)
            return "MATCH OVER";

        if (ranking.Count > 1 &&
            ranking[0].Score.Value == ranking[1].Score.Value &&
            ranking[0].Lives.Value == ranking[1].Lives.Value)
        {
            return "DRAW!";
        }

        return $"{ranking[0].DisplayName} WINS!";
    }
}