using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// PlayerHealth
// Player health, damage, healing, and death state. (Этот скрипт отвечает за: player health, damage, healing, and death state.)
// ============================================================
public class PlayerHealth : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("UI")]
    public Slider healthSlider;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Respawn")]
    public float respawnDelay = 2.5f;
    public Transform respawnPoint;
    public float respawnLift = 0.08f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Optional Visuals")]
    public GameObject playerModel;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Audio")]
    public PlayerAudio playerAudio;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Damage Protection")]
    public float damageCooldown = 0.2f;
    public float respawnInvulnerability = 1.5f;

    private CharacterController characterController;
    private bool isDead = false;
    private bool isInvulnerable = false;
    private float lastDamageTime = -999f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (playerAudio == null)
            playerAudio = GetComponent<PlayerAudio>();

        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    // Apply damage. (Apply damage)
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, "Unknown");
    }

    // Apply damage. (Apply damage)
    public void TakeDamage(float damage, string source)
    {
        if (isDead)
            return;

        if (isInvulnerable)
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        if (damage <= 0f)
            return;

        lastDamageTime = Time.time;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Player took {damage} damage from {source}. HP: {currentHealth}/{maxHealth}");

        if (playerAudio != null)
        {
            if (currentHealth > 0f)
                playerAudio.PlayHurt();
        }

        UpdateHealthUI();

        if (currentHealth <= 0f)
            Die();
    }

    // Restore health. (Restore health)
    public void Heal(float amount)
    {
        if (isDead)
            return;

        if (amount <= 0f)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("Player died");

        if (playerAudio != null)
            playerAudio.PlayDeath();

        if (playerModel != null)
            playerModel.SetActive(false);

        if (characterController != null && characterController.enabled)
            characterController.enabled = false;

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 targetPosition = transform.position;

        if (respawnPoint != null)
            targetPosition = respawnPoint.position + Vector3.up * respawnLift;

        transform.position = targetPosition;

        currentHealth = maxHealth;
        UpdateHealthUI();

        if (playerModel != null)
            playerModel.SetActive(true);

        if (characterController != null)
            characterController.enabled = true;

        if (playerAudio != null)
            playerAudio.PlayRespawn();

        isDead = false;

        yield return StartCoroutine(InvulnerabilityRoutine(respawnInvulnerability));
    }

    IEnumerator InvulnerabilityRoutine(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    // Is Dead. (Is Dead)
    public bool IsDead()
    {
        return isDead;
    }

    // Get Health. (Get Health)
    public float GetHealth()
    {
        return currentHealth;
    }

    // Get Health Percent. (Get Health Percent)
    public float GetHealthPercent()
    {
        if (maxHealth <= 0f)
            return 0f;

        return currentHealth / maxHealth;
    }
}
