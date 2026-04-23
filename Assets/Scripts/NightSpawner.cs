using System.Collections.Generic;
using UnityEngine;

// ============================================================
// NightSpawner
// Night-time enemy spawning control. (Этот скрипт отвечает за: night-time enemy spawning control.)
// ============================================================
public class NightSpawner : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("References")]
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private GameObject skeletonPrefab;
    [SerializeField] private Transform player;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Spawn Settings")]
    [SerializeField] private int maxSkeletons = 5;
    [SerializeField] private float spawnRadiusMin = 18f;
    [SerializeField] private float spawnRadiusMax = 35f;
    [SerializeField] private float spawnCheckInterval = 2f;
    [SerializeField] private float spawnChancePerCheck = 0.6f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Night Range")]
    [SerializeField] private float nightStart = 0.75f;
    [SerializeField] private float nightEnd = 0.25f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask = -1;
    [SerializeField] private float raycastHeight = 50f;
    [SerializeField] private float spawnHeightOffset = 0.1f;

    private readonly List<GameObject> aliveSkeletons = new List<GameObject>();
    private bool wasNightLastFrame = false;
    private float timer = 0f;

    void Update()
    {
        if (dayNightCycle == null || skeletonPrefab == null || player == null)
            return;

        bool isNight = IsNight();

        if (isNight && !wasNightLastFrame)
        {
            wasNightLastFrame = true;
        }
        else if (!isNight && wasNightLastFrame)
        {
            wasNightLastFrame = false;
            RemoveAllSkeletons();
        }

        if (!isNight)
            return;

        CleanupNulls();

        timer += Time.deltaTime;
        if (timer >= spawnCheckInterval)
        {
            timer = 0f;
            TrySpawnSkeleton();
        }
    }

    bool IsNight()
    {
        float t = dayNightCycle.timeOfDay;

        if (nightStart > nightEnd)
            return t >= nightStart || t <= nightEnd;

        return t >= nightStart && t <= nightEnd;
    }

    void TrySpawnSkeleton()
    {
        if (aliveSkeletons.Count >= maxSkeletons)
            return;

        if (Random.value > spawnChancePerCheck)
            return;

        if (!TryGetSpawnPosition(out Vector3 spawnPos))
            return;

        GameObject skeleton = Instantiate(skeletonPrefab, spawnPos, Quaternion.identity);
        aliveSkeletons.Add(skeleton);
    }

    bool TryGetSpawnPosition(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        for (int i = 0; i < 10; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(spawnRadiusMin, spawnRadiusMax);

            Vector3 rayStart = new Vector3(
                player.position.x + circle.x,
                player.position.y + raycastHeight,
                player.position.z + circle.y
            );

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundMask))
            {
                spawnPos = hit.point + Vector3.up * spawnHeightOffset;
                return true;
            }
        }

        return false;
    }

    void RemoveAllSkeletons()
    {
        for (int i = 0; i < aliveSkeletons.Count; i++)
        {
            if (aliveSkeletons[i] != null)
                Destroy(aliveSkeletons[i]);
        }

        aliveSkeletons.Clear();
    }

    void CleanupNulls()
    {
        for (int i = aliveSkeletons.Count - 1; i >= 0; i--)
        {
            if (aliveSkeletons[i] == null)
                aliveSkeletons.RemoveAt(i);
        }
    }
}
