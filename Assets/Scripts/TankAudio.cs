using System.Collections;
using UnityEngine;

public class TankAudio : MonoBehaviour
{
    [Header("Engine (plays while moving)")]
    public AudioClip engineLoop;
    public float engineMaxVolume = 0.5f;
    public float engineIdlePitch = 0.8f;
    public float engineMaxPitch = 1.3f;
    public float fullSpeed = 4f;
    public float fullTurnSpeed = 100f;

    [Header("Turret (plays while rotating)")]
    public AudioClip turretLoop;
    public float turretVolume = 0.4f;
    public Transform turretPivot;

    [Header("One Shots")]
    public AudioClip shootClip;
    public AudioClip deathClip;
    public AudioClip killRewardClip;
    public float shootVolume = 0.8f;
    public float deathVolume = 1f;
    public float killRewardVolume = 2f;

    [Header("Death Sound")]
    public float deathFadeOutTime = 0.25f;

    [Header("Kill Reward Ducking")]
    public float killRewardDelay = 0.15f;
    public float duckVolume = 0.35f;
    public float duckDuration = 0.8f;
    public float duckFadeSpeed = 8f;

    [Header("3D Sound Range")]
    public float minDistance = 5f;
    public float maxDistance = 60f;

    private static float duckUntilTime;

    private AudioSource engineSource;
    private AudioSource turretSource;
    private AudioSource sfxSource;
    private AudioSource deathSource;
    private AudioSource uiSource;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float lastTurretAngle;
    private float turretSoundTimer;
    private bool isDead;
    private float currentDuck = 1f;
    private Coroutine deathFadeRoutine;

    private const float TeleportSpeed = 20f;

    private const float TurretHoldTime = 0.15f;

    private void Awake()
    {
        engineSource = CreateSource(true, true);
        turretSource = CreateSource(true, true);
        sfxSource = CreateSource(false, true);
        deathSource = CreateSource(false, true);
        uiSource = CreateSource(false, false);

        if (engineLoop != null)
        {
            engineSource.clip = engineLoop;
            engineSource.volume = 0f;
            engineSource.Play();
        }

        if (turretLoop != null)
        {
            turretSource.clip = turretLoop;
            turretSource.volume = 0f;
            turretSource.Play();
        }

        lastPosition = transform.position;
        lastRotation = transform.rotation;

        if (turretPivot != null)
            lastTurretAngle = turretPivot.localEulerAngles.y;
    }

    private AudioSource CreateSource(bool loop, bool is3D)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;

        source.spatialBlend = is3D ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;

        return source;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        HandleDucking(dt);
        HandleEngine(dt);
        HandleTurret(dt);
    }

    private void HandleDucking(float dt)
    {
        float target = Time.time < duckUntilTime ? duckVolume : 1f;
        currentDuck = Mathf.Lerp(currentDuck, target, duckFadeSpeed * dt);

        sfxSource.volume = currentDuck;

        if (deathFadeRoutine == null && deathSource.isPlaying)
            deathSource.volume = deathVolume * currentDuck;
    }

    private void HandleEngine(float dt)
    {
        float moveSpeed = (transform.position - lastPosition).magnitude / dt;
        float turnSpeed = Quaternion.Angle(lastRotation, transform.rotation) / dt;

        lastPosition = transform.position;
        lastRotation = transform.rotation;

        if (moveSpeed > TeleportSpeed)
            moveSpeed = 0f;

        float effort = Mathf.Max(moveSpeed / fullSpeed, turnSpeed / fullTurnSpeed);
        effort = isDead ? 0f : Mathf.Clamp01(effort);

        float targetVolume = effort * engineMaxVolume * currentDuck;
        engineSource.volume = Mathf.Lerp(engineSource.volume, targetVolume, 10f * dt);
        engineSource.pitch = Mathf.Lerp(engineIdlePitch, engineMaxPitch, effort);
    }

    private void HandleTurret(float dt)
    {
        if (turretPivot == null)
            return;

        float currentAngle = turretPivot.localEulerAngles.y;
        float turretSpeed = Mathf.Abs(Mathf.DeltaAngle(lastTurretAngle, currentAngle)) / dt;
        lastTurretAngle = currentAngle;

        if (turretSpeed > 5f && !isDead)
            turretSoundTimer = TurretHoldTime;
        else
            turretSoundTimer -= dt;

        float targetVolume = turretSoundTimer > 0f ? turretVolume * currentDuck : 0f;
        turretSource.volume = Mathf.Lerp(turretSource.volume, targetVolume, 15f * dt);
    }

    public void PlayShoot()
    {
        if (shootClip != null)
            sfxSource.PlayOneShot(shootClip, shootVolume);
    }

    public void PlayDeath()
    {
        isDead = true;

        if (deathClip == null)
            return;

        if (deathFadeRoutine != null)
        {
            StopCoroutine(deathFadeRoutine);
            deathFadeRoutine = null;
        }

        deathSource.clip = deathClip;
        deathSource.volume = deathVolume * currentDuck;
        deathSource.Play();
    }

    public void SetAlive()
    {
        isDead = false;

        if (deathSource.isPlaying)
        {
            if (deathFadeRoutine != null)
                StopCoroutine(deathFadeRoutine);

            deathFadeRoutine = StartCoroutine(FadeOutDeathSound());
        }
    }

    private IEnumerator FadeOutDeathSound()
    {
        float startVolume = deathSource.volume;
        float timer = 0f;

        while (timer < deathFadeOutTime)
        {
            timer += Time.deltaTime;
            deathSource.volume = Mathf.Lerp(startVolume, 0f, timer / deathFadeOutTime);
            yield return null;
        }

        deathSource.Stop();
        deathSource.volume = deathVolume;
        deathFadeRoutine = null;
    }

    public void PlayKillReward()
    {
        if (killRewardClip == null)
            return;

        duckUntilTime = Time.time + duckDuration;

        StartCoroutine(PlayKillRewardDelayed());
    }

    private IEnumerator PlayKillRewardDelayed()
    {
        yield return new WaitForSeconds(killRewardDelay);

        uiSource.PlayOneShot(killRewardClip, killRewardVolume);
    }
}