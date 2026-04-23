using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
// ============================================================
// ItemPhysics
// Physics setup for dropped/equipped items. (Этот скрипт отвечает за: physics setup for dropped/equipped items.)
// ============================================================
public class ItemPhysics : MonoBehaviour
{
    private Rigidbody rb;
    private bool initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // В префабе rb.isKinematic должно быть TRUE!
    }

    // Этот метод вызовет скрипт подбора игрока, чтобы физика не включилась в руке
    // Prepare For Pickup. (Prepare For Pickup)
    public void PrepareForPickup()
    {
        initialized = true; 
        StopAllCoroutines();
    }

    void Start()
    {
        if (transform.parent == null) // Если предмет в мире, а не в руке
        {
            StartCoroutine(ActivatePhysicsAfterLoad());
        }
    }

    IEnumerator ActivatePhysicsAfterLoad()
    {
        // Ждем прогрузки чанка
        yield return new WaitForSeconds(1.0f); 

        if (rb != null && !initialized)
        {
            rb.isKinematic = false; // "Размораживаем" предмет
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
}
