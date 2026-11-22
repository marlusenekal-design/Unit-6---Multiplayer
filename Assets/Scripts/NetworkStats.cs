using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkStats : NetworkBehaviour
{
    public int MaxHealth = 100;
    public int StartingLives = 3;
    public int KillReward = 100;
    public float RespawnDelay = 5f;
    public float ShieldDuration = 3f;
    public int MaxNameLength = 12;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsShielded = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Lives = new NetworkVariable<int>(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes(""), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public ParticleSystem smoke_vfx = null;

    PlayerStatusUI status_ui;
    NetworkTransform networkTransform;
    TankAudio tankAudio;
    Coroutine shieldRoutine;

    public string DisplayName
    {
        get
        {
            if (PlayerName.Value.IsEmpty)
                return GetDefaultName();

            return PlayerName.Value.ToString();
        }
    }

    public string GetDefaultName()
    {
        return $"Player {OwnerClientId + 1}";
    }

    public override void OnNetworkSpawn()
    {
        networkTransform = GetComponent<NetworkTransform>();
        tankAudio = GetComponent<TankAudio>();

        if (IsClient)
        {
            Scoreboard scoreboard = FindFirstObjectByType<Scoreboard>(FindObjectsInactive.Include);

            if (scoreboard != null)
            {
                scoreboard.RegisterPlayer(this);
            }
        }

        if (IsServer)
        {
            CurrentHealth.Value = MaxHealth;
            IsAlive.Value = true;
            Lives.Value = StartingLives;
            Score.Value = 0;
            PlayerName.Value = new FixedString64Bytes(GetDefaultName());

            GiveShield(ShieldDuration);
        }

        if (!IsOwner || !IsClient)
            return;

        SetNameRpc(LocalPlayerData.PlayerName);

        status_ui = FindLocalHUD();
        if (status_ui != null)
        {
            Debug.Log("Successfully Bound UI Status Bar");
            status_ui.gameObject.SetActive(true);
            status_ui.Bind(this);
        }
        else
        {
            Debug.LogWarning("No PlayerStatusUI with Is Local HUD ticked was found");
        }
    }

    [Rpc(SendTo.Server)]
    private void SetNameRpc(string requestedName)
    {
        string cleanName = string.IsNullOrWhiteSpace(requestedName) ? GetDefaultName() : requestedName.Trim();

        if (cleanName.Length > MaxNameLength)
            cleanName = cleanName.Substring(0, MaxNameLength);

        PlayerName.Value = new FixedString64Bytes(cleanName);
    }

    private PlayerStatusUI FindLocalHUD()
    {
        PlayerStatusUI[] allUI = FindObjectsByType<PlayerStatusUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlayerStatusUI ui in allUI)
        {
            if (ui.isLocalHUD)
                return ui;
        }

        return null;
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            Scoreboard scoreboard = FindFirstObjectByType<Scoreboard>(FindObjectsInactive.Include);

            if (scoreboard != null)
            {
                scoreboard.UnregisterPlayer(OwnerClientId);
            }
        }

        if (!IsOwner)
            return;

        if (status_ui != null)
        {
            status_ui.Unbind();
        }
    }

    public void GiveShield(float duration)
    {
        if (!IsServer)
            return;

        if (shieldRoutine != null)
            StopCoroutine(shieldRoutine);

        shieldRoutine = StartCoroutine(ShieldRoutine(duration));
    }

    private IEnumerator ShieldRoutine(float duration)
    {
        IsShielded.Value = true;

        yield return new WaitForSeconds(duration);

        IsShielded.Value = false;
        shieldRoutine = null;
    }

    public void TakeDamage(float amount, ulong shooterId)
    {
        if (!IsServer || !IsAlive.Value || IsShielded.Value)
            return;

        if (MatchManager.Instance != null && !MatchManager.Instance.IsPlaying)
            return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

        if (CurrentHealth.Value == 0)
        {
            ToggleSmokeRpc(true);
            IsAlive.Value = false;

            LoseLife();

            AwardKill(KillReward, shooterId);

            if (Lives.Value > 0)
                StartCoroutine(RespawnRoutine());
        }
    }

    public void ResetForNewMatch()
    {
        if (!IsServer)
            return;

        StopAllCoroutines();
        shieldRoutine = null;

        CurrentHealth.Value = MaxHealth;
        Lives.Value = StartingLives;
        Score.Value = 0;
        IsAlive.Value = true;
        ToggleSmokeRpc(false);

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.GetSpawn(out spawnPosition, out spawnRotation);
        }

        TeleportOwnerRpc(spawnPosition, spawnRotation);

        GiveShield(ShieldDuration);
    }

    private void AwardKill(int amount, ulong shooterId)
    {
        if (!IsServer)
            return;

        if (shooterId == OwnerClientId)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterId, out NetworkClient shooter))
        {
            Debug.Log("Shooter Disconnected");
            return;
        }

        NetworkObject playerObject = shooter.PlayerObject;

        if (playerObject == null) return;

        if (playerObject.TryGetComponent(out NetworkStats shooterStats))
        {
            shooterStats.AddScore(amount);
            shooterStats.PlayKillRewardRpc();
            Debug.Log($"Awarded {amount} points to {shooterStats.DisplayName}");
        }
    }

    [Rpc(SendTo.Owner)]
    public void PlayKillRewardRpc()
    {
        if (tankAudio != null)
            tankAudio.PlayKillReward();
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(RespawnDelay);

        if (!IsSpawned) yield break;

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.GetSpawn(out spawnPosition, out spawnRotation);
        }

        TeleportOwnerRpc(spawnPosition, spawnRotation);

        CurrentHealth.Value = MaxHealth;
        IsAlive.Value = true;
        ToggleSmokeRpc(false);

        GiveShield(ShieldDuration);
    }

    [Rpc(SendTo.Everyone)]
    private void ToggleSmokeRpc(bool state)
    {
        if (tankAudio != null)
        {
            if (state)
                tankAudio.PlayDeath();
            else
                tankAudio.SetAlive();
        }

        if (smoke_vfx == null)
            return;

        if (state)
        {
            smoke_vfx.gameObject.SetActive(true);
            smoke_vfx.Play();
        }

        else
        {
            smoke_vfx.Stop();
            smoke_vfx.gameObject.SetActive(false);
        }
    }

    [Rpc(SendTo.Owner)]
    public void TeleportOwnerRpc(Vector3 newPosition, Quaternion newRotation)
    {
        if (networkTransform != null && networkTransform.CanCommitToTransform)
        {
            networkTransform.Teleport(newPosition, newRotation, transform.localScale);
        }
        else
        {
            transform.SetPositionAndRotation(newPosition, newRotation);
        }
    }

    public void AddScore(int amount)
    {
        if (!IsServer)
            return;

        Score.Value += amount;
    }

    public void LoseLife()
    {
        if (!IsServer)
            return;

        Lives.Value = Mathf.Max(0, Lives.Value - 1);
    }
}