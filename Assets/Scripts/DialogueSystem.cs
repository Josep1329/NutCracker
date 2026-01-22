using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueSystem : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    public KeyCode nextDialogueKey = KeyCode.F;
    public KeyCode closeDialogueKey = KeyCode.Escape;

    [Header("UI References")]
    public Canvas dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    public Button nextButton;
    public Button closeButton;
    public TextMeshProUGUI dialogueCounterText; // e.g. "1/3"

    [Header("References")]
    public Transform playerTransform;

    private EnemyAI currentNearbyEnemy;
    private bool dialogueActive = false;

    void Start()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                playerTransform = playerGO.transform;
        }

        // If no canvas assigned, try to reuse one from AdvancedDialogueSystem
        if (dialogueCanvas == null)
        {
            AdvancedDialogueSystem other = FindAnyObjectByType<AdvancedDialogueSystem>();
            if (other != null && other.dialogueCanvas != null)
                dialogueCanvas = other.dialogueCanvas;
        }

        // Hide dialogue canvas at start
        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);

        // Setup button listeners
        if (nextButton != null)
            nextButton.onClick.AddListener(NextDialogue);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseDialogue);
    }

    void Update()
    {
        if (!dialogueActive)
        {
            // Find nearest detected enemy within interaction distance
            currentNearbyEnemy = FindNearbyDetectedEnemy();

            // Check if player presses E to start dialogue
            if (Input.GetKeyDown(interactionKey) && currentNearbyEnemy != null)
            {
                currentNearbyEnemy.StartInteraction();
                StartDialogue(currentNearbyEnemy);
            }
        }
        else
        {
            // Handle dialogue navigation
            if (Input.GetKeyDown(nextDialogueKey))
            {
                NextDialogue();
            }

            if (Input.GetKeyDown(closeDialogueKey))
            {
                CloseDialogue();
            }
        }
    }

    private EnemyAI FindNearbyDetectedEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        EnemyAI closest = null;
        float closestDistance = interactionDistance;

        foreach (GameObject go in enemies)
        {
            if (go == null) continue;
            EnemyAI enemy = go.GetComponent<EnemyAI>();
            if (enemy == null) continue;

            float distanceToEnemy = Vector3.Distance(playerTransform.position, enemy.transform.position);
            if (distanceToEnemy < closestDistance)
            {
                closestDistance = distanceToEnemy;
                closest = enemy;
            }
        }

        return closest;
    }

    private void StartDialogue(EnemyAI enemy)
    {
        if (enemy.dialogueLines == null || enemy.dialogueLines.Length == 0)
            return;

        currentNearbyEnemy = enemy;
        dialogueActive = true;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(true);

        // Disable player movement and look while dialogue is active
        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.SetInputEnabled(false);
        }

        UpdateDialogueUI();
    }

    private void NextDialogue()
    {
        if (currentNearbyEnemy == null)
            return;

        // Tell the enemy to advance the dialogue
        currentNearbyEnemy.AdvanceDialogue();

        // Check if dialogue has ended (by checking if enemy is no longer interacting)
        if (!currentNearbyEnemy.IsInteracting())
        {
            CloseDialogue();
        }
        else
        {
            UpdateDialogueUI();
        }
    }

    private void CloseDialogue()
    {
        dialogueActive = false;
        
        // Force end dialogue on enemy if still interacting
        if (currentNearbyEnemy != null && currentNearbyEnemy.IsInteracting())
        {
            currentNearbyEnemy.EndDialogueExternal();
        }
        
        currentNearbyEnemy = null;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);

        // Re-enable player movement and lock cursor again
        if (playerTransform != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.SetInputEnabled(true);
        }
    }

    private void UpdateDialogueUI()
    {
        if (currentNearbyEnemy == null || currentNearbyEnemy.dialogueLines == null)
            return;

        // Get current dialogue line from the enemy's internal index
        int currentLineIndex = currentNearbyEnemy.GetCurrentDialogueLineIndex();
        
        // Update dialogue text
        if (dialogueText != null)
            dialogueText.text = currentNearbyEnemy.dialogueLines[currentLineIndex];

        // Update counter
        if (dialogueCounterText != null)
            dialogueCounterText.text = $"{currentLineIndex + 1}/{currentNearbyEnemy.dialogueLines.Length}";

        // Update next button state
        if (nextButton != null)
        {
            bool isLastDialogue = currentLineIndex >= currentNearbyEnemy.dialogueLines.Length - 1;
            nextButton.GetComponentInChildren<TextMeshProUGUI>().text = isLastDialogue ? "Cerrar" : "Siguiente (F)";
        }
    }

    // Optional: Draw interaction range for debugging
    void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, interactionDistance);
        }
    }
}
