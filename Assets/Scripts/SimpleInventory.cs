using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// SimpleInventory
// Basic item storage, add/remove checks, and item count queries. (Этот скрипт отвечает за: basic item storage, add/remove checks, and item count queries.)
// ============================================================
public class SimpleInventory : MonoBehaviour
{
    [Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int amount;
    }

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Debug Start Items")]
    public List<InventoryEntry> startItems = new List<InventoryEntry>();

    private Dictionary<string, int> items = new Dictionary<string, int>();

    // Initialize references and singleton setup. (Initialize references and singleton setup)
    private void Awake()
    {
        items.Clear();

        for (int i = 0; i < startItems.Count; i++)
        {
            InventoryEntry entry = startItems[i];
            AddItem(entry.itemId, entry.amount);
        }
    }

    // Add an item into storage. (Add an предмет into storage)
    public void AddItem(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return;

        if (!items.ContainsKey(itemId))
            items[itemId] = 0;

        items[itemId] += amount;
    }

    // Remove an item from storage. (Remove an предмет from storage)
    public bool RemoveItem(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return false;

        if (!items.ContainsKey(itemId))
            return false;

        if (items[itemId] < amount)
            return false;

        items[itemId] -= amount;

        if (items[itemId] <= 0)
            items.Remove(itemId);

        return true;
    }

    // Get Item Count. (Get Item Count)
    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return 0;

        if (items.TryGetValue(itemId, out int count))
            return count;

        return 0;
    }
}
