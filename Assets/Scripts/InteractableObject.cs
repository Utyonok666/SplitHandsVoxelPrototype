using UnityEngine;

// ============================================================
// InteractableObject
// Base interactable object behaviour and interaction metadata. (Этот скрипт отвечает за: base interactable object behaviour and interaction metadata.)
// ============================================================
public class InteractableObject : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Object")]
    public ResourceType objectType;
    public float maxHealth = 100f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Drop Settings")]
    public GameObject dropPrefab;
    public int minDrop = 2;
    public int maxDrop = 5;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitClip;
    public AudioClip breakClip;
    [Range(0f, 1f)] public float hitVolume = 1f;
    [Range(0f, 1f)] public float breakVolume = 1f;
    public bool autoCreateAudioSourceIfMissing = true;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Hit Sound Timing")]
    [Tooltip("Минимальная задержка между звуками удара, чтобы звук не спамился каждый кадр.")]
    public float hitSoundCooldown = 0.15f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("3D Audio Setup")]
    public bool configure3DAudioAutomatically = true;
    public float spatialBlend = 1f;
    public float minDistance = 2.5f;
    public float maxDistance = 18f;

    private float currentHealth;
    private float nextHitSoundTime = -999f;

    void Awake()
    {
        if (maxHealth <= 0f)
            maxHealth = 100f;

        currentHealth = maxHealth;
        EnsureAudioSource();
    }

    void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            if (!autoCreateAudioSourceIfMissing)
                return;

            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;

        if (configure3DAudioAutomatically)
        {
            audioSource.spatialBlend = spatialBlend;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }

    // Can Be Damaged By. (Can Be Damaged By)
    public bool CanBeDamagedBy(ResourceType toolType)
    {
        if (toolType == ResourceType.Universal) return true;
        if (toolType == objectType) return true;
        if (toolType == ResourceType.Flesh) return true;
        return false;
    }

    // Take Hit. (Take Hit)
    public void TakeHit(float damage, ResourceType toolType)
    {
        if (toolType == objectType || toolType == ResourceType.Universal)
            ApplyDamage(damage, "<color=green>Эффективно!</color>");
        else if (toolType == ResourceType.Flesh)
            ApplyDamage(damage * 0.1f, "<color=yellow>Неэффективно (руками долго...).</color>");
        else
            Debug.Log($"<color=red>Нужен другой инструмент!</color> Нужен: {objectType}");
    }

    void ApplyDamage(float amount, string message)
    {
        if (amount <= 0f)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, currentHealth);

        if (currentHealth > 0f)
            TryPlayHitSound();

        Debug.Log($"{message} Урон: {amount}. Осталось HP: {currentHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    void TryPlayHitSound()
    {
        if (audioSource == null || hitClip == null)
            return;

        if (Time.time < nextHitSoundTime)
            return;

        audioSource.PlayOneShot(hitClip, hitVolume);
        nextHitSoundTime = Time.time + Mathf.Max(0.01f, hitSoundCooldown);
    }

    // Get Health Percent. (Get Health Percent)
    public float GetHealthPercent()
    {
        if (maxHealth <= 0f)
            return 0f;

        return Mathf.Clamp01(currentHealth / maxHealth);
    }

    // Get Break Progress Percent. (Get Break Progress Percent)
    public float GetBreakProgressPercent()
    {
        return 100f - (GetHealthPercent() * 100f);
    }

    void Die()
    {
        if (breakClip != null)
            AudioSource.PlayClipAtPoint(breakClip, transform.position, breakVolume);

        if (dropPrefab != null)
        {
            int count = Random.Range(minDrop, maxDrop + 1);

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnOffset = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(0.5f, 1.2f),
                    Random.Range(-0.5f, 0.5f)
                );

                GameObject drop = Instantiate(dropPrefab, transform.position + spawnOffset, Quaternion.identity);
                Rigidbody rb = drop.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    Vector3 force = new Vector3(
                        Random.Range(-2f, 2f),
                        4f,
                        Random.Range(-2f, 2f)
                    );
                    rb.AddForce(force, ForceMode.Impulse);
                }
            }
        }

        SaveableWorldObject saveable = GetComponent<SaveableWorldObject>();
        if (saveable != null)
            saveable.MarkRemoved();

        Destroy(gameObject);
    }
}
