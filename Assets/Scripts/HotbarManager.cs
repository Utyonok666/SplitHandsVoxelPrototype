using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
// ============================================================
// HotbarManager
// Hotbar slot storage, selection, icons, counts, and save/load integration. (Этот скрипт отвечает за: hotbar slot storage, selection, icons, counts, and save/load integration.)
// ============================================================
public class SlotData
{
    public string itemName;
    public int count;
    public Sprite icon;
    public GameObject dropPrefab;

    // Clear. (Clear)
    public void Clear()
    {
        itemName = null;
        count = 0;
        icon = null;
        dropPrefab = null;
    }
}

[System.Serializable]
public class HotbarItemDefinition
{
    public string itemName;
    public Sprite icon;
    public GameObject prefab;
}

public class HotbarManager : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Настройки UI")]
    public RectTransform selectionFrame;
    public RectTransform[] slots;
    public Image[] iconImages;
    public Text[] countTexts;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Fallback UI")]
    public Sprite fallbackIcon;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Item Database")]
    public HotbarItemDefinition[] itemDefinitions;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Параметры инвентаря")]
    public int maxStack = 256;
    public SlotData[] inventorySlots;

    private int currentIndex = 0;
    private Collider playerCollider;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Input Blocking")]
    public ChatConsole chatConsole;

    // Capture Save Data. (Capture Save Data)
    public List<HotbarSlotSaveRecord> CaptureSaveData()
    {
        List<HotbarSlotSaveRecord> result = new List<HotbarSlotSaveRecord>();

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            SlotData slot = inventorySlots[i];

            if (slot != null && !string.IsNullOrEmpty(slot.itemName) && slot.count > 0)
                result.Add(new HotbarSlotSaveRecord { itemName = slot.itemName, count = slot.count });
            else
                result.Add(new HotbarSlotSaveRecord { itemName = null, count = 0 });
        }

        return result;
    }

    // Load From Save. (Load From Save)
    public void LoadFromSave(WorldSaveData data)
    {
        ResetInventory();

        if (data == null || data.hotbarSlots == null)
        {
            UpdateInventoryUI();
            return;
        }

        int length = Mathf.Min(inventorySlots.Length, data.hotbarSlots.Count);

        for (int i = 0; i < length; i++)
        {
            HotbarSlotSaveRecord saved = data.hotbarSlots[i];
            if (saved == null || string.IsNullOrEmpty(saved.itemName) || saved.count <= 0)
                continue;

            HotbarItemDefinition def = FindDefinition(saved.itemName);

            inventorySlots[i].itemName = saved.itemName;
            inventorySlots[i].count = saved.count;
            inventorySlots[i].icon = def != null && def.icon != null ? def.icon : fallbackIcon;
            inventorySlots[i].dropPrefab = def != null ? def.prefab : null;
        }

        UpdateInventoryUI();
    }

    // Find Definition. (Find Definition)
    public HotbarItemDefinition FindDefinition(string itemName)
    {
        if (itemDefinitions == null)
            return null;

        for (int i = 0; i < itemDefinitions.Length; i++)
        {
            HotbarItemDefinition def = itemDefinitions[i];
            if (def != null && def.itemName == itemName)
                return def;
        }

        return null;
    }

    [ContextMenu("Reset Inventory Now")]
    // Reset Inventory. (Reset Inventory)
    public void ResetInventory()
    {
        int slotCount = (slots != null && slots.Length > 0) ? slots.Length : 0;

        inventorySlots = new SlotData[slotCount];
        for (int i = 0; i < inventorySlots.Length; i++)
            inventorySlots[i] = new SlotData();

        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, inventorySlots.Length - 1));

        UpdateInventoryUI();
        UpdateSelectionUI();
    }

    // Initialize references and singleton setup. (Initialize references and singleton setup)
    private void Awake()
    {
        if (inventorySlots == null || inventorySlots.Length == 0)
            ResetInventory();

        playerCollider = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Collider>();

        if (chatConsole == null)
            chatConsole = FindObjectOfType<ChatConsole>();
    }

    // Initialize runtime state. (Initialize runtime state)
    private void Start()
    {
        UpdateInventoryUI();
        UpdateSelectionUI();
    }

    // Run per-frame logic. (Run per-frame logic)
    private void Update()
    {
        if (slots == null || slots.Length == 0)
            return;

        if (chatConsole != null && chatConsole.IsChatOpen)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                currentIndex = i;
                UpdateSelectionUI();
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            currentIndex = (currentIndex > 0) ? currentIndex - 1 : slots.Length - 1;
            UpdateSelectionUI();
        }
        else if (scroll < 0f)
        {
            currentIndex = (currentIndex < slots.Length - 1) ? currentIndex + 1 : 0;
            UpdateSelectionUI();
        }

        if (Input.GetKeyDown(KeyCode.G))
            DropItemFromCurrentSlot();
    }

    // Add an item into storage. (Add an предмет into storage)
    public bool AddItem(string name, Sprite icon, GameObject prefab)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (prefab == null)
        {
            HotbarItemDefinition autoDef = FindDefinition(name);
            if (autoDef != null)
                prefab = autoDef.prefab;
        }

        Sprite finalIcon = icon;

        if (finalIcon == null)
        {
            HotbarItemDefinition autoDef = FindDefinition(name);
            if (autoDef != null && autoDef.icon != null)
                finalIcon = autoDef.icon;
        }

        if (finalIcon == null)
            finalIcon = fallbackIcon;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].itemName == name && inventorySlots[i].count < maxStack)
            {
                inventorySlots[i].count++;
                inventorySlots[i].dropPrefab = prefab;

                if (inventorySlots[i].icon == null)
                    inventorySlots[i].icon = finalIcon;

                UpdateInventoryUI();
                return true;
            }
        }

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (string.IsNullOrEmpty(inventorySlots[i].itemName))
            {
                inventorySlots[i].itemName = name;
                inventorySlots[i].icon = finalIcon;
                inventorySlots[i].dropPrefab = prefab;
                inventorySlots[i].count = 1;
                UpdateInventoryUI();
                return true;
            }
        }

        return false;
    }

    // Get Item Count. (Get Item Count)
    public int GetItemCount(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return 0;

        int total = 0;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null && inventorySlots[i].itemName == itemName)
                total += inventorySlots[i].count;
        }

        return total;
    }

    // Has Item Amount. (Has Item Amount)
    public bool HasItemAmount(string itemName, int amount)
    {
        return GetItemCount(itemName) >= amount;
    }

    // Remove Items. (Remove Items)
    public bool RemoveItems(string itemName, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemName) || amount <= 0)
            return false;

        if (GetItemCount(itemName) < amount)
            return false;

        int remaining = amount;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            SlotData slot = inventorySlots[i];

            if (slot == null || slot.itemName != itemName)
                continue;

            int removeAmount = Mathf.Min(slot.count, remaining);
            slot.count -= removeAmount;
            remaining -= removeAmount;

            if (slot.count <= 0)
                slot.Clear();

            if (remaining <= 0)
                break;
        }

        UpdateInventoryUI();
        return true;
    }

    // Drop Item From Current Slot. (Drop Item From Current Slot)
    private void DropItemFromCurrentSlot()
    {
        if (currentIndex < 0 || currentIndex >= inventorySlots.Length)
            return;

        SlotData slot = inventorySlots[currentIndex];

        if (slot == null || string.IsNullOrEmpty(slot.itemName) || slot.dropPrefab == null)
            return;

        if (Camera.main == null)
            return;

        Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 2.0f;
        GameObject dropped = Instantiate(slot.dropPrefab, spawnPos, Quaternion.identity);

        dropped.transform.localScale = slot.dropPrefab.transform.localScale;

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 throwDir = (Camera.main.transform.forward + Vector3.up * 0.2f).normalized;
            rb.AddForce(throwDir * 15f, ForceMode.Impulse);
        }

        Collider itemCol = dropped.GetComponent<Collider>();
        if (itemCol != null && playerCollider != null)
            StartCoroutine(IgnoreCollisionTemp(itemCol, playerCollider));

        foreach (var script in dropped.GetComponents<MonoBehaviour>())
            script.enabled = true;

        slot.count--;
        if (slot.count <= 0)
            slot.Clear();

        UpdateInventoryUI();
    }

    // Ignore Collision Temp. (Ignore Collision Temp)
    private IEnumerator IgnoreCollisionTemp(Collider item, Collider player)
    {
        Physics.IgnoreCollision(item, player, true);
        yield return new WaitForSeconds(0.5f);

        if (item != null && player != null)
            Physics.IgnoreCollision(item, player, false);
    }

    // Update Selection UI. (Update Selection UI)
    public void UpdateSelectionUI()
    {
        if (slots != null && currentIndex < slots.Length && currentIndex >= 0 && selectionFrame != null)
            selectionFrame.position = slots[currentIndex].position;
    }

    // Update Inventory UI. (Update Inventory UI)
    public void UpdateInventoryUI()
    {
        if (inventorySlots == null)
            return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            bool hasItem = inventorySlots[i] != null && !string.IsNullOrEmpty(inventorySlots[i].itemName);

            if (i < iconImages.Length && iconImages[i] != null)
            {
                Sprite shownIcon = inventorySlots[i] != null && inventorySlots[i].icon != null
                    ? inventorySlots[i].icon
                    : fallbackIcon;

                bool showIcon = hasItem && shownIcon != null;

                iconImages[i].enabled = showIcon;
                iconImages[i].sprite = shownIcon;
                iconImages[i].preserveAspect = true;
                iconImages[i].color = Color.white;
            }

            if (i < countTexts.Length && countTexts[i] != null)
            {
                bool showCount = hasItem && inventorySlots[i].count > 1;
                countTexts[i].enabled = showCount;

                if (showCount)
                    countTexts[i].text = inventorySlots[i].count.ToString();
                else
                    countTexts[i].text = "";
            }
        }
    }
}
