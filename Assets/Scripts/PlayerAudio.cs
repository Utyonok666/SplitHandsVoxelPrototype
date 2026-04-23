using UnityEngine;

// ============================================================
// PlayerAudio
// Player audio hooks for steps, hits, or action sounds. (Этот скрипт отвечает за: player audio hooks for steps, hits, or action sounds.)
// ============================================================
public class PlayerAudio : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Audio Sources")]
    [SerializeField] private AudioSource movementSource;
    [SerializeField] private AudioSource actionSource;
    [SerializeField] private bool autoCreateSourcesIfMissing = true;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepInterval = 0.45f;
    [SerializeField] private float minMoveSpeedForSteps = 0.15f;
    [SerializeField] private float footstepVolume = 1f;
    [SerializeField] private float footstepPitchMin = 0.96f;
    [SerializeField] private float footstepPitchMax = 1.04f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Jump / Land")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private float jumpVolume = 1f;
    [SerializeField] private float landVolume = 1f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Actions")]
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private AudioClip dropClip;
    [SerializeField] private float attackVolume = 1f;
    [SerializeField] private float pickupVolume = 1f;
    [SerializeField] private float dropVolume = 1f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Health")]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip respawnClip;
    [SerializeField] private float hurtVolume = 1f;
    [SerializeField] private float deathVolume = 1f;
    [SerializeField] private float respawnVolume = 1f;

    private float footstepTimer = 0f;
    private bool wasGrounded = false;

    void Awake()
    {
        EnsureSources();
    }

    void EnsureSources()
    {
        if (!autoCreateSourcesIfMissing)
            return;

        if (movementSource == null)
        {
            Transform existing = transform.Find("MovementAudioSource");
            if (existing != null)
                movementSource = existing.GetComponent<AudioSource>();

            if (movementSource == null)
            {
                GameObject go = new GameObject("MovementAudioSource");
                go.transform.SetParent(transform, false);
                movementSource = go.AddComponent<AudioSource>();
                movementSource.playOnAwake = false;
                movementSource.spatialBlend = 0f;
            }
        }

        if (actionSource == null)
        {
            Transform existing = transform.Find("ActionAudioSource");
            if (existing != null)
                actionSource = existing.GetComponent<AudioSource>();

            if (actionSource == null)
            {
                GameObject go = new GameObject("ActionAudioSource");
                go.transform.SetParent(transform, false);
                actionSource = go.AddComponent<AudioSource>();
                actionSource.playOnAwake = false;
                actionSource.spatialBlend = 0f;
            }
        }
    }

    // Tick. (Tick)
    public void Tick(Vector3 velocity, bool isGrounded, bool noclipEnabled)
    {
        HandleLanding(isGrounded, noclipEnabled);
        HandleFootsteps(velocity, isGrounded, noclipEnabled);
    }

    void HandleLanding(bool isGrounded, bool noclipEnabled)
    {
        if (noclipEnabled)
        {
            wasGrounded = false;
            return;
        }

        if (!wasGrounded && isGrounded)
            PlayOneShot(actionSource, landClip, landVolume);

        wasGrounded = isGrounded;
    }

    void HandleFootsteps(Vector3 velocity, bool isGrounded, bool noclipEnabled)
    {
        if (noclipEnabled)
        {
            footstepTimer = 0f;
            return;
        }

        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        bool isMoving = horizontalVelocity.magnitude > minMoveSpeedForSteps;

        if (!isGrounded || !isMoving)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            PlayRandomFootstep();
            footstepTimer = footstepInterval;
        }
    }

    void PlayRandomFootstep()
    {
        if (movementSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        int index = Random.Range(0, footstepClips.Length);
        float oldPitch = movementSource.pitch;
        movementSource.pitch = Random.Range(footstepPitchMin, footstepPitchMax);
        PlayOneShot(movementSource, footstepClips[index], footstepVolume);
        movementSource.pitch = oldPitch;
    }

    // Play Jump. (Play Jump)
    public void PlayJump()
    {
        PlayOneShot(actionSource, jumpClip, jumpVolume);
    }

    // Play Attack. (Play Attack)
    public void PlayAttack()
    {
        PlayOneShot(actionSource, attackClip, attackVolume);
    }

    // Play Pickup. (Play Pickup)
    public void PlayPickup()
    {
        PlayOneShot(actionSource, pickupClip, pickupVolume);
    }

    // Play Drop. (Play Drop)
    public void PlayDrop()
    {
        PlayOneShot(actionSource, dropClip, dropVolume);
    }

    // Play Hurt. (Play Hurt)
    public void PlayHurt()
    {
        PlayOneShot(actionSource, hurtClip, hurtVolume);
    }

    // Play Death. (Play Death)
    public void PlayDeath()
    {
        PlayOneShot(actionSource, deathClip, deathVolume);
    }

    // Play Respawn. (Play Respawn)
    public void PlayRespawn()
    {
        PlayOneShot(actionSource, respawnClip, respawnVolume);
    }

    void PlayOneShot(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
            return;

        source.PlayOneShot(clip, volume);
    }
}
