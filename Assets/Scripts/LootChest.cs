using UnityEngine;

// ============================================================
// LootChest
// Chest interaction, timed opening, and loot spawning. (Этот скрипт отвечает за: chest interaction, timed opening, and loot spawning.)
// ============================================================
public class LootChest : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Open")]
    public float openTime = 5f;
    public bool opened = false;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Possible Drops")]
    public GameObject woodDropPrefab;
    public GameObject stoneDropPrefab;
    public GameObject ironBarDropPrefab; // NEW

    public int minWood = 1;
    public int maxWood = 5;

    public int minStone = 0;
    public int maxStone = 4;

    public int minIronBar = 0; // NEW
    public int maxIronBar = 2; // NEW

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Spawn")]
    public float spawnRadius = 0.6f;
    public float upwardForce = 3f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openClip;
    [Range(0f, 1f)] public float openVolume = 1f;
    public bool autoCreateAudioSourceIfMissing = true;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("3D Audio Setup")]
    public bool configure3DAudioAutomatically = true;
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 2.5f;
    public float maxDistance = 20f;

    private float holdProgress = 0f;
    private bool manualOpenRequested = false;
    private bool isOpening = false;

    void Awake()
    {
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

    // Get Open Percent. (Get Open Percent)
    public float GetOpenPercent()
    {
        if (opened)
            return 100f;

        if (openTime <= 0f)
            return 100f;

        return Mathf.Clamp01(holdProgress / openTime) * 100f;
    }

    // Is Opening. (Is Opening)
    public bool IsOpening()
    {
        return isOpening;
    }

    // Hold Interact. (Hold Interact)
    public void HoldInteract(float dt, KeyCode holdKey)
    {
        if (opened)
            return;

        if (!Input.GetKey(holdKey) && !manualOpenRequested)
        {
            isOpening = false;
            holdProgress = 0f;
            return;
        }

        isOpening = true;
        holdProgress += dt;

        if (holdProgress >= openTime)
            OpenChest();
    }

    // Start Manual Open. (Start Manual Open)
    public void StartManualOpen()
    {
        if (opened)
            return;

        manualOpenRequested = true;
        holdProgress = openTime;
        OpenChest();
    }

    void OpenChest()
    {
        if (opened)
            return;

        opened = true;
        isOpening = false;
        manualOpenRequested = false;

        PlayOpenSound();
        SpawnLoot();

        Debug.Log("<color=yellow>Chest opened!</color>");
    }

    void SpawnLoot()
    {
        SpawnItems(woodDropPrefab, minWood, maxWood);
        SpawnItems(stoneDropPrefab, minStone, maxStone);
        SpawnItems(ironBarDropPrefab, minIronBar, maxIronBar); // NEW
    }

    void SpawnItems(GameObject prefab, int min, int max)
    {
        if (prefab == null)
            return;

        int count = Random.Range(min, max + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0.4f,
                Random.Range(-spawnRadius, spawnRadius)
            );

            GameObject drop = Instantiate(
                prefab,
                transform.position + offset,
                Quaternion.identity
            );

            Rigidbody rb = drop.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 force = new Vector3(
                    Random.Range(-2f, 2f),
                    upwardForce,
                    Random.Range(-2f, 2f)
                );

                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }

    void PlayOpenSound()
    {
        if (audioSource == null || openClip == null)
            return;

        audioSource.PlayOneShot(openClip, openVolume);
    }
}
