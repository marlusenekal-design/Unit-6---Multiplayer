using UnityEngine;
using UnityEngine.InputSystem;

using Unity.Netcode;

public class NetworkedPlayerController : NetworkBehaviour
{
    [Header("Input References")]
    public InputActionReference moveAction;
    public InputActionReference shootAction;
    public InputActionReference turretRotationAction;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 100f;
    public float mouseSensitivity = 0.2f;
    public Transform turretPivot;

    [Header("Shooting")]
    public Transform muzzlePoint;
    public NetworkObject projectilePrefab;
    public float fireCooldown = 0.8f;
    private float nextShotTime;

    [Header("Components")]
    public GameObject cam;
    private NetworkStats stats;
    private NetworkPowerUps powerUps;
    private TankAudio tankAudio;
    private Rigidbody rb;
    public Material mat_localPlayer;
    public Material mat_enemyPlayer;

    // - Networked Variables
    private readonly NetworkVariable<float> turretAngle = new NetworkVariable<float>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        stats = GetComponent<NetworkStats>();
        powerUps = GetComponent<NetworkPowerUps>();
        tankAudio = GetComponent<TankAudio>();
        rb = GetComponent<Rigidbody>();

        if (stats == null)
            Debug.LogError("NetworkStats is missing from " + gameObject.name);

        if (rb != null)
            rb.isKinematic = !(IsSpawned && IsOwner);

        if (!IsSpawned || !IsOwner)
        {
            cam.SetActive(false);

            if (IsSpawned && mat_enemyPlayer != null)
                GetComponentInChildren<MeshRenderer>().material = mat_enemyPlayer;

            return;
        }

        GetComponentInChildren<MeshRenderer>().material = mat_localPlayer;

        if (SceneCamera.Instance != null)
            SceneCamera.Instance.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && SceneCamera.Instance != null)
            SceneCamera.Instance.SetActive(true);
    }

    private bool CanPlay()
    {
        return MatchManager.Instance == null || MatchManager.Instance.IsPlaying;
    }

    private void Update()
    {
        if (!IsSpawned || stats == null)
            return;

        if (IsOwner && stats.IsAlive.Value && CanPlay())
        {
            HandleTurret();
            HandleShooting();
        }

        if (!IsOwner && turretPivot != null)
        {
            turretPivot.localRotation = Quaternion.Euler(0f, turretAngle.Value, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner || stats == null || rb == null)
            return;

        if (stats.IsAlive.Value && CanPlay())
        {
            HandleMovement();
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        float move = moveInput.y;
        float rot = moveInput.x;

        float speed = moveSpeed * (powerUps != null ? powerUps.CurrentSpeedMultiplier : 1f);

        Vector3 forwardVelocity = transform.forward * move * speed;
        rb.linearVelocity = new Vector3(forwardVelocity.x, rb.linearVelocity.y, forwardVelocity.z);

        Quaternion turn = Quaternion.Euler(0f, rot * rotationSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);

        rb.angularVelocity = Vector3.zero;
    }

    private void HandleTurret()
    {
        if (turretPivot == null)
            return;

        float turn = turretRotationAction.action.ReadValue<float>();

        if (Mathf.Approximately(turn, 0f))
            return;

        turretPivot.transform.Rotate(transform.up * turn * mouseSensitivity);

        UpdateTurretRpc(turretPivot.localEulerAngles.y);
    }

    [Rpc(SendTo.Server)]
    private void UpdateTurretRpc(float angle)
    {
        angle = Mathf.Repeat(angle, 360f);
        turretAngle.Value = angle;
        turretPivot.localRotation = Quaternion.Euler(0, angle, 0);
    }

    private void HandleShooting()
    {
        if (shootAction.action.WasPressedThisFrame() && Time.time >= nextShotTime)
        {
            float cooldown = fireCooldown * (powerUps != null ? powerUps.CurrentFireCooldownMultiplier : 1f);
            nextShotTime = Time.time + cooldown;

            if (tankAudio != null)
                tankAudio.PlayShoot();

            ShootRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void ShootRpc()
    {
        if (stats == null || !stats.IsAlive.Value || !CanPlay())
            return;

        if (muzzlePoint == null || projectilePrefab == null)
            return;

        NetworkObject projectile = Instantiate(projectilePrefab, muzzlePoint.position, muzzlePoint.rotation);

        if (projectile.TryGetComponent(out NetworkProjectile projectileScript))
        {
            projectileScript.ShooterId = OwnerClientId;

            if (powerUps != null)
                projectileScript.damage = Mathf.RoundToInt(projectileScript.damage * powerUps.CurrentDamageMultiplier);
        }

        projectile.Spawn();

        ShootSoundRpc();
    }

    [Rpc(SendTo.NotOwner)]
    private void ShootSoundRpc()
    {
        if (tankAudio != null)
            tankAudio.PlayShoot();
    }
}