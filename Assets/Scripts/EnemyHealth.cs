using UnityEngine;

// ============================================================
// EnemyHealth
// Enemy health storage, damage processing, and death handling. (Этот скрипт отвечает за: enemy health storage, damage processing, and death handling.)
// ============================================================
public class EnemyHealth : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private float hitVolume = 1f;
    [SerializeField] private float deathVolume = 1f;
    [SerializeField] private bool autoCreateAudioSourceIfMissing = true;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    void Start()
    {
        currentHealth = maxHealth;
        EnsureAudioSource();
    }

    void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        if (!autoCreateAudioSourceIfMissing)
            return;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    // Apply damage. (Apply damage)
    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHealth <= 0f)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (currentHealth > 0f)
            PlayOneShot(hitClip, hitVolume);

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        if (deathClip != null)
            AudioSource.PlayClipAtPoint(deathClip, transform.position, deathVolume);

        Destroy(gameObject);
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }
}
