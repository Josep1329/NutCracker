using UnityEngine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class BossAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Vector3 patrolCenter = Vector3.zero;
    public float patrolRadius = 10f;
    public float patrolSpeed = 3f;
    public float changeWaypointDistance = 1f;
    public float waypointChangeInterval = 3f;

    [Header("Detection Settings")]
    public float detectionRadius = 15f;
    public Transform playerTransform;
    public float stoppingDistance = 2f;

    [Header("Dialogue")]
    public DialogueLine[] dialogueLines;

    [Header("Combat")]
    public float attackDamage = 15f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;
    public float attackRange = 2f;
    public float attackRadius = 1f;

    [Header("References")]
    public Transform enemyCamera;
    public string interactMessage = "Presiona E para interactuar";

    // Combat state
    private bool inCombat = false;
    private bool isInteracting = false;
    private int currentDialogueLineIndex = 0;

    // Movement
    private CharacterController controller;
    private Vector3 currentWaypoint;
    private float timeSinceLastWaypoint = 0f;
    private bool playerDetected = false;
    private Vector3 velocity;
    private float gravity = -9.81f;

    // Animator
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                playerTransform = playerGO.transform;
        }

        GenerateRandomWaypoint();
    }

    void Update()
    {
        playerDetected = DetectPlayer();

        if (inCombat)
        {
            HandleCombat();
        }
        else if (playerDetected)
        {
            HandlePlayerDetected();
        }
        else
        {
            HandlePatrol();
        }

        // Apply gravity
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;

        Vector3 movement = GetMovementVector();
        controller.Move(movement * Time.deltaTime);
    }

    private bool DetectPlayer()
    {
        if (playerTransform == null || inCombat)
            return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        return distanceToPlayer <= detectionRadius && !isInteracting;
    }

    private void HandlePlayerDetected()
    {
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
    }

    private void HandlePatrol()
    {
        timeSinceLastWaypoint += Time.deltaTime;

        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
        if (distanceToWaypoint < changeWaypointDistance || timeSinceLastWaypoint > waypointChangeInterval)
        {
            GenerateRandomWaypoint();
            timeSinceLastWaypoint = 0f;
        }

        Vector3 directionToWaypoint = (currentWaypoint - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
    }

    private void HandleCombat()
    {
        if (playerTransform == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Move towards player
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(directionToPlayer), Time.deltaTime * 5f);

        // Attack if in range
        if (distanceToPlayer <= attackRange)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
            }
        }
    }

    private void PerformAttack()
    {
        lastAttackTime = Time.time;

        Debug.Log($"[BossAI] {gameObject.name} attacking player!");

        // Trigger animation if available
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Raycast to damage player
        PlayerCombat playerCombat = playerTransform.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.TakeDamage(attackDamage);
        }
    }

    private Vector3 GetMovementVector()
    {
        Vector3 movement = Vector3.zero;

        if (inCombat && playerDetected)
        {
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            movement = directionToPlayer * patrolSpeed + new Vector3(0f, velocity.y, 0f);
        }
        else if (!playerDetected)
        {
            Vector3 directionToWaypoint = (currentWaypoint - transform.position).normalized;
            movement = directionToWaypoint * patrolSpeed + new Vector3(0f, velocity.y, 0f);
        }

        return movement;
    }

    private void GenerateRandomWaypoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        currentWaypoint = patrolCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    public void StartInteraction()
    {
        isInteracting = true;
    }

    public void EndDialogueExternal()
    {
        isInteracting = false;
        currentDialogueLineIndex = 0;
    }

    public void SetCombatMode(bool combatEnabled)
    {
        inCombat = combatEnabled;
        isInteracting = combatEnabled;

        if (inCombat)
        {
            Debug.Log($"[BossAI] {gameObject.name} entered combat mode!");
        }
    }

    public bool IsInteracting()
    {
        return isInteracting;
    }

    public int GetCurrentDialogueLineIndex()
    {
        return currentDialogueLineIndex;
    }

    public void AdvanceDialogue()
    {
        if (!isInteracting)
            return;

        currentDialogueLineIndex++;

        if (currentDialogueLineIndex >= dialogueLines.Length)
        {
            EndDialogueExternal();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(patrolCenter, patrolRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        if (Application.isPlaying)
        {
            Gizmos.DrawSphere(currentWaypoint, 0.5f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }
    }
}
