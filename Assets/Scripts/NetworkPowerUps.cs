using Unity.Netcode;
using UnityEngine;

public enum PowerUpType
{
    None,
    Speed,
    RapidFire,
    DoubleDamage
}

public class NetworkPowerUps : NetworkBehaviour
{
    [Header("Power Up Settings")]
    public float PowerUpDuration = 10f;
    public float SpeedMultiplier = 1.6f;
    public float RapidFireMultiplier = 0.35f;
    public float DamageMultiplier = 2f;

    [Header("Pickup Sound")]
    public AudioClip pickupClip;
    public float pickupVolume = 1f;

    // - Networked Variables
    public NetworkVariable<PowerUpType> ActivePowerUp = new NetworkVariable<PowerUpType>(
        PowerUpType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> PowerUpEndTime = new NetworkVariable<double>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkStats stats;

    public float CurrentSpeedMultiplier => ActivePowerUp.Value == PowerUpType.Speed ? SpeedMultiplier : 1f;
    public float CurrentFireCooldownMultiplier => ActivePowerUp.Value == PowerUpType.RapidFire ? RapidFireMultiplier : 1f;
    public float CurrentDamageMultiplier => ActivePowerUp.Value == PowerUpType.DoubleDamage ? DamageMultiplier : 1f;

    public float TimeRemaining
    {
        get
        {
            if (ActivePowerUp.Value == PowerUpType.None)
                return 0f;

            return Mathf.Max(0f, (float)(PowerUpEndTime.Value - NetworkManager.ServerTime.Time));
        }
    }

    public override void OnNetworkSpawn()
    {
        stats = GetComponent<NetworkStats>();

        if (IsServer && stats != null)
            stats.IsAlive.OnValueChanged += OnAliveChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && stats != null)
            stats.IsAlive.OnValueChanged -= OnAliveChanged;

        if (IsOwner && PowerUpHUD.Instance != null)
            PowerUpHUD.Instance.Hide();
    }

    private void OnAliveChanged(bool oldValue, bool newValue)
    {
        if (!newValue)
            Clear();
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (IsServer && ActivePowerUp.Value != PowerUpType.None && NetworkManager.ServerTime.Time >= PowerUpEndTime.Value)
            Clear();

        if (IsOwner && PowerUpHUD.Instance != null)
        {
            if (ActivePowerUp.Value == PowerUpType.None)
                PowerUpHUD.Instance.Hide();
            else
                PowerUpHUD.Instance.Show(GetLabel(ActivePowerUp.Value), GetColor(ActivePowerUp.Value), TimeRemaining);
        }
    }

    public void Apply(PowerUpType type)
    {
        if (!IsServer || type == PowerUpType.None)
            return;

        ActivePowerUp.Value = type;
        PowerUpEndTime.Value = NetworkManager.ServerTime.Time + PowerUpDuration;

        PlayPickupRpc();
    }

    public void Clear()
    {
        if (!IsServer)
            return;

        ActivePowerUp.Value = PowerUpType.None;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayPickupRpc()
    {
        if (pickupClip != null)
            AudioSource.PlayClipAtPoint(pickupClip, transform.position, pickupVolume);
    }

    public static string GetLabel(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Speed: return "SPEED";
            case PowerUpType.RapidFire: return "RAPID FIRE";
            case PowerUpType.DoubleDamage: return "DOUBLE DAMAGE";
            default: return "";
        }
    }

    public static Color GetColor(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Speed: return new Color(0.2f, 0.7f, 1f);
            case PowerUpType.RapidFire: return new Color(1f, 0.85f, 0.1f);
            case PowerUpType.DoubleDamage: return new Color(1f, 0.25f, 0.2f);
            default: return Color.white;
        }
    }
}