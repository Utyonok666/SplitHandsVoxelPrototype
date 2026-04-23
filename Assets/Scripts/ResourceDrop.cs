using UnityEngine;

// Мы наследуемся от ItemDrop, чтобы работала твоя система подбора
// ============================================================
// ResourceDrop
// World resource pickup data and helper behaviour. (Этот скрипт отвечает за: world resource pickup data and helper behaviour.)
// ============================================================
public class ResourceDrop : ItemDrop 
{
    // itemName, itemIcon и myPrefab уже есть в родителе (ItemDrop)

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Твой код с импульсом (красивый вылет)
            Vector3 randomDir = new Vector3(Random.Range(-2f, 2f), 3f, Random.Range(-2f, 2f));
            rb.AddForce(randomDir, ForceMode.Impulse);
            
            // Добавим случайное вращение для реализма
            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }
    }
}
