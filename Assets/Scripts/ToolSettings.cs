using UnityEngine;

// ============================================================
// ToolSettings
// Tool metadata, equip/drop behaviour, and effectiveness rules. (Этот скрипт отвечает за: tool metadata, equip/drop behaviour, and effectiveness rules.)
// ============================================================
public class ToolSettings : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Tool")]
    public ResourceType toolType = ResourceType.Universal;
    public float damage = 25f;
    public bool isEquipped = false;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Placeable")]
    public bool isPlaceable = false;
    public GameObject placedPrefab;
    public bool snapToGrid = true;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Hand Offsets")]
    public Vector3 handRotationOffset = new Vector3(0f, 90f, 0f);
    public Vector3 handPositionOffset = new Vector3(0f, 0f, 0.15f);

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Scale In Hand")]
    public Vector3 handScaleMultiplier = Vector3.one;

    [HideInInspector] public Vector3 originalScale;

    private Rigidbody cachedRb;
    private Collider[] cachedColliders;

    void Awake()
    {
        originalScale = transform.localScale;
        cachedRb = GetComponent<Rigidbody>();
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    // On Equipped. (On Equipped)
    public void OnEquipped(Transform parent)
    {
        isEquipped = true;

        transform.SetParent(parent, false);
        transform.localPosition = handPositionOffset;
        transform.localRotation = Quaternion.Euler(handRotationOffset);
        transform.localScale = Vector3.Scale(originalScale, handScaleMultiplier);

        if (cachedRb != null)
        {
            cachedRb.isKinematic = true;
            cachedRb.detectCollisions = false;
            cachedRb.velocity = Vector3.zero;
            cachedRb.angularVelocity = Vector3.zero;
        }

        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
                cachedColliders[i].enabled = false;
        }
    }

    // On Dropped. (On Dropped)
    public void OnDropped()
    {
        isEquipped = false;
        transform.SetParent(null, true);
        transform.localScale = originalScale;

        if (cachedRb != null)
        {
            cachedRb.isKinematic = false;
            cachedRb.detectCollisions = true;
        }

        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
                cachedColliders[i].enabled = true;
        }
    }

    // Check whether this tool matches a target resource type. (Check whether this инструмент matches a target resource type)
    public bool IsEffectiveAgainst(ResourceType targetType)
    {
        if (toolType == ResourceType.Universal)
            return true;

        return toolType == targetType;
    }
}
