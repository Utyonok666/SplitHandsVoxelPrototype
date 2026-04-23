using UnityEngine;

// ============================================================
// Furnace
// Auto-smelts input resources into output drops over time. (Этот скрипт отвечает за: auto-smelts input resources into output drops over time.)
// ============================================================
public class Furnace : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Input / Output")]
    public string inputItemName = "IronChunk";
    public GameObject outputPrefab;
    public Transform outputPoint;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Auto Pickup")]
    public float pickupRadius = 2.2f;
    public LayerMask pickupMask = ~0;
    public int maxQueue = 8;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Smelting")]
    public float smeltTimePerItem = 3.5f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Debug")]
    public bool showDebugLogs = false;

    private int queuedInput = 0;
    private float smeltTimer = 0f;

    public int QueuedInput => queuedInput;
    public bool IsSmelting => queuedInput > 0;

    // Run per-frame logic. (Run per-frame logic)
    private void Update()
    {
        TryAbsorbNearbyIronChunk();
        HandleSmelting();
    }

    // Try to Absorb Nearby Iron Chunk. (Try to Absorb Nearby Iron Chunk)
    private void TryAbsorbNearbyIronChunk()
    {
        if (queuedInput >= maxQueue)
            return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            pickupRadius,
            pickupMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (queuedInput >= maxQueue)
                return;

            if (hits[i] == null)
                continue;

            ItemDrop drop = hits[i].GetComponentInParent<ItemDrop>();
            if (drop == null)
                continue;

            if (drop.itemName != inputItemName)
                continue;

            if (showDebugLogs)
                Debug.Log("[Furnace] Absorbed: " + drop.itemName);

            queuedInput++;
            Destroy(drop.gameObject);
            break;
        }
    }

    // Handle Smelting. (Handle Smelting)
    private void HandleSmelting()
    {
        if (queuedInput <= 0)
        {
            smeltTimer = 0f;
            return;
        }

        smeltTimer += Time.deltaTime;

        if (smeltTimer >= smeltTimePerItem)
        {
            smeltTimer = 0f;
            queuedInput--;
            SpawnOutput();
        }
    }

    // Spawn Output. (Spawn Output)
    private void SpawnOutput()
    {
        if (outputPrefab == null)
        {
            Debug.LogWarning("[Furnace] Output prefab is missing.");
            return;
        }

        Vector3 spawnPos =
            outputPoint != null
            ? outputPoint.position
            : transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;

        GameObject obj = Instantiate(outputPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 force =
                transform.forward * Random.Range(0.6f, 1.2f) +
                Vector3.up * Random.Range(1.2f, 2.0f);

            rb.AddForce(force, ForceMode.Impulse);
        }

        if (showDebugLogs)
            Debug.Log("[Furnace] Spawned output: " + outputPrefab.name);
    }

    // Get Smelt Progress01. (Get Smelt Progress01)
    public float GetSmeltProgress01()
    {
        if (queuedInput <= 0 || smeltTimePerItem <= 0f)
            return 0f;

        return Mathf.Clamp01(smeltTimer / smeltTimePerItem);
    }

    // On Draw Gizmos Selected. (On Draw Gizmos Selected)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, pickupRadius);
    }
}
