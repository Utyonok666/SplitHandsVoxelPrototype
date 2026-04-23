using UnityEngine;

// ============================================================
// ItemDrop
// World pickup item behaviour and item metadata. (Этот скрипт отвечает за: world pickup item behaviour and item metadata.)
// ============================================================
public class ItemDrop : MonoBehaviour
{
    public string itemName;
    public Sprite itemIcon;
    public GameObject myPrefab; 

    // Pick Up. (Pick Up)
    public void PickUp() { Pickup(); }

    // Handle pickup logic. (Handle pickup logic)
    public void Pickup()
    {
        // Если забыл назначить - ищем в папке Resources
        if (myPrefab == null || myPrefab.scene.name != null)
        {
            myPrefab = Resources.Load<GameObject>(itemName);
        }

        if (myPrefab == null)
        {
            Debug.LogError($"[ItemDrop] Не найден префаб для {itemName}! Проверь папку Resources.");
            return;
        }

        HotbarManager hotbar = FindObjectOfType<HotbarManager>();
        if (hotbar != null && hotbar.AddItem(itemName, itemIcon, myPrefab))
        {
            Destroy(gameObject);
        }
    }


    // Capture Save Data. (Capture Save Data)
    public WorldDropSaveRecord CaptureSaveData()
    {
        return new WorldDropSaveRecord
        {
            itemName = itemName,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z
        };
    }
}
