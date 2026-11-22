using Unity.Netcode;
using UnityEngine;

public class PowerUpPickup : NetworkBehaviour
{
    public NetworkVariable<PowerUpType> Type = new NetworkVariable<PowerUpType>(
        PowerUpType.Speed, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Visuals")]
    [SerializeField] Renderer crateRenderer;
    [SerializeField] float spinSpeed = 90f;
    [SerializeField] float bobHeight = 0.25f;
    [SerializeField] float bobSpeed = 2f;

    private Vector3 startPosition;
    private bool collected;

    public override void OnNetworkSpawn()
    {
        startPosition = transform.position;

        if (crateRenderer == null)
            crateRenderer = GetComponentInChildren<Renderer>();

        ApplyColor(Type.Value);
        Type.OnValueChanged += OnTypeChanged;
    }

    public override void OnNetworkDespawn()
    {
        Type.OnValueChanged -= OnTypeChanged;
    }

    private void OnTypeChanged(PowerUpType oldValue, PowerUpType newValue)
    {
        ApplyColor(newValue);
    }

    private void ApplyColor(PowerUpType type)
    {
        if (crateRenderer != null)
            crateRenderer.material.color = NetworkPowerUps.GetColor(type);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPosition + Vector3.up * bob;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !IsSpawned || collected)
            return;

        NetworkPowerUps powerUps = other.GetComponentInParent<NetworkPowerUps>();

        if (powerUps == null)
            return;

        NetworkStats stats = other.GetComponentInParent<NetworkStats>();
        if (stats != null && !stats.IsAlive.Value)
            return;

        collected = true;
        powerUps.Apply(Type.Value);
        NetworkObject.Despawn();
    }
}