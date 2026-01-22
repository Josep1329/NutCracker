using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCombat : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;
    private float lastAttackTime = 0f;
    public float attackRange = 2f;
    public float attackRadius = 1f;

    [Header("UI References")]
    public Image healthBar; // Assign health bar Image component
    public TextMeshProUGUI healthText; // Optional: show "100/100" text

    [Header("Effects")]
    public Animator playerAnimator; // Optional: for kick animation
    public string kickAnimationTrigger = "Kick"; // Animation parameter name

    private bool canAttack = true;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Update()
    {
        // Check if attack is available
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            canAttack = true;
        }

        // Handle left click attack
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            PerformKick();
        }
    }

    private void PerformKick()
    {
        canAttack = false;
        lastAttackTime = Time.time;

        Debug.Log("[Player] Performing kick attack!");

        // Trigger animation if available
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(kickAnimationTrigger);
        }

        // Perform raycast or sphere cast to detect enemies hit
        PerformAttackCheck();
    }

    private void PerformAttackCheck()
    {
        // Get camera forward direction for attack direction
        Transform cameraTransform = transform.Find("Camera") ?? Camera.main?.transform;
        if (cameraTransform == null)
            return;

        Vector3 attackDirection = cameraTransform.forward;
        Vector3 attackOrigin = transform.position + Vector3.up * 1f; // Attack from body height

        // Sphere cast to find enemies in range
        RaycastHit[] hits = Physics.SphereCastAll(attackOrigin, attackRadius, attackDirection, attackRange);

        foreach (RaycastHit hit in hits)
        {
            // Check if hit object has an EnemyAI component
            EnemyAI enemy = hit.collider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                // Apply damage to enemy (you'll need to add a TakeDamage method to EnemyAI)
                Debug.Log($"[Player] Kicked enemy: {enemy.gameObject.name}");
                // enemy.TakeDamage(attackDamage); // Uncomment when EnemyAI has TakeDamage method
            }
        }

        // Debug visualization
        Debug.DrawLine(attackOrigin, attackOrigin + attackDirection * attackRange, Color.red, 0.2f);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[Player] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

        UpdateHealthUI();

        // Check if player is dead
        if (currentHealth <= 0)
        {
            OnPlayerDeath();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);

        Debug.Log($"[Player] Healed {amount}. Health: {currentHealth}/{maxHealth}");

        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth}";
        }
    }

    private void OnPlayerDeath()
    {
        Debug.Log("[Player] Player is dead!");
        // TODO: Add respawn logic, game over screen, etc.
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}
