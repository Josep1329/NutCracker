using UnityEngine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
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
    public string[] dialogueLines = new string[] { "Hola", "¿Cómo estás?", "Adiós" };

    [Header("References")]
    public Transform enemyCamera; // Optional: for aiming/looking at player
    [Tooltip("Message to show when player is within `stoppingDistance`.")]
    public string interactMessage = "Presiona E para interactuar";

    // Runtime references
    private CharacterController controller;
    private Vector3 currentWaypoint;
    private float timeSinceLastWaypoint = 0f;
    private bool playerDetected = false;
    private bool isInteracting = false;
    private int currentDialogueLineIndex = 0;
    private bool inCombat = false;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;
    public float attackRange = 2f;
    public float attackRadius = 1f;
    private Vector3 velocity;
    private float gravity = -9.81f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                playerTransform = playerGO.transform;
        }

        // Initialize first waypoint
        GenerateRandomWaypoint();
    }

    void Update()
    {
        // Check if player is detected
        playerDetected = DetectPlayer();

        // Update interaction prompt visibility through the manager
        UpdateInteractionPromptVisibility();

        // Interaction is triggered externally via StartInteraction() method

        if (inCombat)
        {
            HandleCombat();
        }
        else if (playerDetected)
        {
            // Stop movement (face player)
            HandlePlayerDetected();
        }
        else
        {
            // Continue patrolling
            HandlePatrol();
        }

        // Apply gravity
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;

        // Apply movement
        Vector3 movement = GetMovementVector();
        controller.Move(movement * Time.deltaTime);
    }

    private bool DetectPlayer()
    {
        if (playerTransform == null)
            return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        // If in combat, detection is not relevant — combat state controls engagement
        if (inCombat)
            return true;
        return distanceToPlayer <= detectionRadius;
    }

    private void HandleCombat()
    {
        if (playerTransform == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Rotate towards player
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

        // Move towards player
        if (distanceToPlayer > attackRange)
        {
            controller.Move((directionToPlayer * patrolSpeed + new Vector3(0f, velocity.y, 0f)) * Time.deltaTime);
        }
        else
        {
            // Attack if cooldown elapsed
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                // Damage player
                PlayerCombat pc = playerTransform.GetComponent<PlayerCombat>();
                if (pc != null)
                {
                    pc.TakeDamage(attackDamage);
                }
            }
        }
    }

    private void UpdateInteractionPromptVisibility()
    {
        if (playerTransform == null || InteractionPromptManager.Instance == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool withinInteractRange = distanceToPlayer <= stoppingDistance && !isInteracting;

        // The manager will show/hide the prompt based on the nearest enemy
        // This method is just for informational purposes
    }

    public void StartInteraction()
    {
        isInteracting = true;

        // Hide the prompt during dialogue (through the manager)
        if (InteractionPromptManager.Instance != null)
            InteractionPromptManager.Instance.HidePrompt();

        // Start dialogue coroutine
        StartCoroutine(RunDialogue());
    }

    public void AdvanceDialogue()
    {
        // Called externally to advance to the next dialogue line
        if (!isInteracting)
            return;

        currentDialogueLineIndex++;

        if (currentDialogueLineIndex >= dialogueLines.Length)
        {
            // Dialogue finished
            EndDialogue();
            // Update prompt through manager
            if (InteractionPromptManager.Instance != null)
            {
                InteractionPromptManager.Instance.HidePrompt();
                InteractionPromptManager.Instance.ForceUpdateNearestEnemy();
            }
        }
        else
        {
            // Show next line
            DisplayCurrentDialogueLine();
        }
    }

    private void DisplayCurrentDialogueLine()
    {
        if (currentDialogueLineIndex < dialogueLines.Length)
        {
            string line = dialogueLines[currentDialogueLineIndex];
            Debug.Log($"[Enemy Dialogue] {line}");

            // Note: The DialogueSystem will handle displaying the dialogue on the main canvas
            // We don't need to show it here in the interaction prompt
        }
    }

    private void EndDialogue()
    {
        isInteracting = false;
        currentDialogueLineIndex = 0;
    }

    public bool IsInteracting()
    {
        return isInteracting;
    }

    public void EndDialogueExternal()
    {
        EndDialogue();
        // Notify manager to update prompt visibility
        if (InteractionPromptManager.Instance != null)
        {
            InteractionPromptManager.Instance.ForceUpdateNearestEnemy();
        }
    }

    public int GetCurrentDialogueLineIndex()
    {
        return currentDialogueLineIndex;
    }

    public void SetCombatMode(bool combatEnabled)
    {
        inCombat = combatEnabled;
        isInteracting = !combatEnabled ? isInteracting : false;
        if (inCombat)
            Debug.Log($"[EnemyAI] {gameObject.name} entered combat mode!");
    }

    private System.Collections.IEnumerator RunDialogue()
    {
        // Display first dialogue line and wait for external AdvanceDialogue() calls
        currentDialogueLineIndex = 0;
        DisplayCurrentDialogueLine();
        yield return null;
    }

    private void HandlePlayerDetected()
    {
        // Stop movement (only apply gravity)
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        
        // Optionally rotate towards player
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
    }

    private void HandlePatrol()
    {
        // Update waypoint timer
        timeSinceLastWaypoint += Time.deltaTime;

        // Check if reached current waypoint or time to change
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
        if (distanceToWaypoint < changeWaypointDistance || timeSinceLastWaypoint > waypointChangeInterval)
        {
            GenerateRandomWaypoint();
            timeSinceLastWaypoint = 0f;
        }

        // Move towards waypoint
        Vector3 directionToWaypoint = (currentWaypoint - transform.position).normalized;
        
        // Rotate towards waypoint
        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
    }

    private Vector3 GetMovementVector()
    {
        Vector3 movement = Vector3.zero;

        if (playerDetected)
        {
            // No horizontal movement when player is detected
            movement = new Vector3(0f, velocity.y, 0f);
        }
        else
        {
            // Move towards waypoint
            Vector3 direction = (currentWaypoint - transform.position).normalized;
            movement = transform.forward * patrolSpeed + new Vector3(0f, velocity.y, 0f);
        }

        return movement;
    }

    private void GenerateRandomWaypoint()
    {
        // Generate random point within patrol radius
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        currentWaypoint = patrolCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw patrol area
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(patrolCenter, patrolRadius);

        // Draw detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw current waypoint
        Gizmos.color = Color.green;
        if (Application.isPlaying)
        {
            Gizmos.DrawSphere(currentWaypoint, 0.5f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }
    }
}
