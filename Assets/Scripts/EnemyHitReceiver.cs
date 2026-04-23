using UnityEngine;

// ============================================================
// EnemyHitReceiver
// Receives hits from player attacks and forwards damage to enemy health. (Этот скрипт отвечает за: receives hits from player attacks and forwards damage to enemy health.)
// ============================================================
public class EnemyHitReceiver : MonoBehaviour
{
    private EnemyHealth health;

    void Awake()
    {
        health = GetComponentInParent<EnemyHealth>();
    }

    // Apply Damage. (Apply Damage)
    public void ApplyDamage(float damage)
    {
        if (health != null)
            health.TakeDamage(damage);
    }

    // Get Health. (Get Health)
    public EnemyHealth GetHealth()
    {
        return health;
    }
}
