using Unity.Netcode;
using UnityEngine;

public class NetworkProjectile : NetworkBehaviour
{
    [Header("Projectile Variables")]
    public float speed = 18f;
    public float lifetime = 4f;
    public float radius = 0.125f;
    public int damage = 25;
    private float timeRemaining;

    [Header("Impact Sounds")]
    public AudioClip tankHitClip;
    public AudioClip wallHitClip;
    public float impactVolume = 1f;

    [HideInInspector] public ulong ShooterId;

    [SerializeField] GameObject explosion_vfx;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timeRemaining = lifetime;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned)
            return;

        timeRemaining -= Time.fixedDeltaTime;

        if (timeRemaining <= 0)
        {
            NetworkObject.Despawn();
            return;
        }

        float distance = speed * Time.fixedDeltaTime;

        if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit, distance))
        {
            NetworkObject hitNetworkObject = hit.collider.GetComponentInParent<NetworkObject>();

            if (hitNetworkObject != null && hitNetworkObject.IsPlayerObject && hitNetworkObject.OwnerClientId == ShooterId)
            {
                transform.position += transform.forward * distance;
                return;
            }

            NetworkStats hitStats = hit.collider.GetComponentInParent<NetworkStats>();
            bool hitTank = hitStats != null;

            if (hitTank)
            {
                hitStats.TakeDamage(damage, ShooterId);
            }

            TriggerExplosion(hit.point, hitTank);
            NetworkObject.Despawn();
            return;
        }

        transform.position += transform.forward * distance;
    }

    private void TriggerExplosion(Vector3 position, bool hitTank)
    {
        if (!IsServer)
            return;

        PlayExplosionRpc(position, hitTank);
    }

    [Rpc(SendTo.Everyone)]
    private void PlayExplosionRpc(Vector3 position, bool hitTank)
    {
        if (explosion_vfx != null)
        {
            GameObject explosion = Instantiate(explosion_vfx, position, Quaternion.identity);
            Destroy(explosion, 5f);
        }

        AudioClip clip = hitTank ? tankHitClip : wallHitClip;

        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, position, impactVolume);
    }
}