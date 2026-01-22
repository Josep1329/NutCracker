using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public int nextDialogueIndex = -1; // -1 means end dialogue
    public DialogueOptionAction actionOnSelect = DialogueOptionAction.None;
}

[System.Serializable]
public enum DialogueOptionAction
{
    None,
    StartCombat,
    EndDialogue
}

[System.Serializable]
public class DialogueLine
{
    public string text;
    public DialogueOption[] options; // If empty, use F to advance
}

/// <summary>
/// Advanced dialogue system that supports dialogue options/choices
/// </summary>
public class AdvancedDialogueSystem : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    public KeyCode nextDialogueKey = KeyCode.F;
    public KeyCode closeDialogueKey = KeyCode.Escape;

    [Header("UI References")]
    public Canvas dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    public UnityEngine.UI.Button nextButton;
    public Transform optionsContainer; // Parent for option buttons
    public UnityEngine.UI.Button optionButtonPrefab;
    public TextMeshProUGUI dialogueCounterText;
    public UnityEngine.UI.Button closeButton;

    [Header("References")]
    public Transform playerTransform;

    private BossAI currentNearbyEnemy;
    private bool dialogueActive = false;
    private int currentDialogueIndex = 0;

    void Start()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                playerTransform = playerGO.transform;
        }

        // Try to reuse DialogueSystem's canvas if available
        if (dialogueCanvas == null)
        {
            DialogueSystem other = FindAnyObjectByType<DialogueSystem>();
            if (other != null && other.dialogueCanvas != null)
                dialogueCanvas = other.dialogueCanvas;
        }

        // Fallback to any Canvas
        if (dialogueCanvas == null)
        {
            Canvas any = FindAnyObjectByType<Canvas>();
            if (any != null)
                dialogueCanvas = any;
        }

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);

        // Auto-assign dialogue text if not set
        if (dialogueText == null && dialogueCanvas != null)
        {
            TextMeshProUGUI tmp = dialogueCanvas.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
                dialogueText = tmp;
        }

        // Setup buttons
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseDialogue);
        }

        // Ensure next button calls NextDialogue
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextDialogue);
            nextButton.interactable = true;
        }

        // Warn if there's no EventSystem (UI won't receive clicks)
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[AdvancedDialogueSystem] No EventSystem found in the scene. UI buttons will not receive clicks.");
        }

        if (dialogueText == null)
            Debug.LogWarning("[AdvancedDialogueSystem] `dialogueText` is not assigned. Dialogue UI will not show text.");

        // Ensure we have an options container; if missing, create one under the canvas to avoid accidental deletion
        if (optionsContainer == null && dialogueCanvas != null)
        {
            Transform existing = dialogueCanvas.transform.Find("OptionsContainer");
            if (existing != null)
                optionsContainer = existing;
            else
            {
                GameObject oc = new GameObject("OptionsContainer", typeof(RectTransform));
                oc.transform.SetParent(dialogueCanvas.transform, false);
                optionsContainer = oc.transform;
                // Add a vertical layout so buttons stack nicely
                var layout = oc.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                oc.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        if (optionsContainer == null || optionButtonPrefab == null)
            Debug.LogWarning("[AdvancedDialogueSystem] Options container or button prefab not assigned. Dialogue options will not display.");
        else
        {
            Debug.Log("[AdvancedDialogueSystem] Initialized with Canvas='" + (dialogueCanvas!=null?dialogueCanvas.name:"<null>") + "', dialogueText='" + (dialogueText!=null?dialogueText.name:"<null>") + "', optionsContainer='" + optionsContainer.name + "', optionPrefab='" + optionButtonPrefab.name + "', nextButton='" + (nextButton!=null?nextButton.name:"<null>") + "', closeButton='" + (closeButton!=null?closeButton.name:"<null>") + "'");
        }
    }

    void Update()
    {
        if (!dialogueActive)
        {
            currentNearbyEnemy = FindNearbyBoss();

            if (Input.GetKeyDown(interactionKey) && currentNearbyEnemy != null)
            {
                currentNearbyEnemy.StartInteraction();
                StartDialogue(currentNearbyEnemy);
            }
        }
        else
        {
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

    private BossAI FindNearbyBoss()
    {
        // Find by tag to support scene prefabs and tagging workflow
        GameObject[] bossObjects = GameObject.FindGameObjectsWithTag("Boss_Enemy");
        BossAI closest = null;
        float closestDistance = interactionDistance;

        foreach (GameObject go in bossObjects)
        {
            if (go == null) continue;
            BossAI boss = go.GetComponent<BossAI>();
            if (boss == null) continue;
            float distanceToEnemy = Vector3.Distance(playerTransform.position, boss.transform.position);
            if (distanceToEnemy < closestDistance)
            {
                closestDistance = distanceToEnemy;
                closest = boss;
            }
        }

        return closest;
    }

    private void StartDialogue(BossAI boss)
    {
        if (boss.dialogueLines == null || boss.dialogueLines.Length == 0)
            return;

        currentNearbyEnemy = boss;
        currentDialogueIndex = 0;
        dialogueActive = true;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(true);

        Debug.Log("[AdvancedDialogueSystem] StartDialogue for boss='" + (boss != null ? boss.name : "<null>") + "' using canvas='" + (dialogueCanvas!=null?dialogueCanvas.name:"<null>") + "' and dialogueText='" + (dialogueText!=null?dialogueText.name:"<null>") + "'");

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

        // Check if current dialogue has options
        DialogueLine currentLine = currentNearbyEnemy.dialogueLines[currentDialogueIndex];
        if (currentLine.options != null && currentLine.options.Length > 0)
        {
            // Don't advance if there are options to display
            return;
        }

        currentDialogueIndex++;

        if (currentDialogueIndex >= currentNearbyEnemy.dialogueLines.Length)
        {
            CloseDialogue();
        }
        else
        {
            UpdateDialogueUI();
        }
    }

    private void SelectOption(DialogueOption option)
    {
        Debug.Log("[AdvancedDialogueSystem] SelectOption chosen='" + (option!=null?option.optionText:"<null>") + "' action='" + (option!=null?option.actionOnSelect.ToString():"<null>") + "' nextIndex='" + (option!=null?option.nextDialogueIndex.ToString():"<null>") + "'");
        // Handle option selection
        if (option.actionOnSelect == DialogueOptionAction.StartCombat)
        {
            Debug.Log("[AdvancedDialogue] Player selected COMBAT!");
            StartCombat();
        }
        else if (option.actionOnSelect == DialogueOptionAction.EndDialogue)
        {
            Debug.Log("[AdvancedDialogue] Player selected PEACEFUL!");
            CloseDialogue();
        }

        if (option.nextDialogueIndex >= 0)
        {
            currentDialogueIndex = option.nextDialogueIndex;
            UpdateDialogueUI();
        }
        else
        {
            CloseDialogue();
        }
    }

    private void StartCombat()
    {
        // All enemies start attacking the player (find by tag)
        GameObject[] enemyGOs = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject go in enemyGOs)
        {
            if (go == null) continue;
            EnemyAI enemy = go.GetComponent<EnemyAI>();
            if (enemy != null)
                enemy.SetCombatMode(true);
        }

        GameObject[] bossGOs = GameObject.FindGameObjectsWithTag("Boss_Enemy");
        foreach (GameObject go in bossGOs)
        {
            if (go == null) continue;
            BossAI boss = go.GetComponent<BossAI>();
            if (boss != null)
                boss.SetCombatMode(true);
        }

        Debug.Log("[AdvancedDialogue] Combat started!");
        CloseDialogue();
    }

    private void CloseDialogue()
    {
        dialogueActive = false;
        currentDialogueIndex = 0;

        if (currentNearbyEnemy != null && currentNearbyEnemy.IsInteracting())
        {
            currentNearbyEnemy.EndDialogueExternal();
        }

        currentNearbyEnemy = null;

        if (dialogueCanvas != null)
            dialogueCanvas.gameObject.SetActive(false);

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

        DialogueLine currentLine = currentNearbyEnemy.dialogueLines[currentDialogueIndex];

        // Update dialogue text
        if (dialogueText != null)
            dialogueText.text = currentLine.text;

        // Update counter
        if (dialogueCounterText != null)
            dialogueCounterText.text = $"{currentDialogueIndex + 1}/{currentNearbyEnemy.dialogueLines.Length}";

        // Clear and recreate options
        ClearOptions();

        if (currentLine.options != null && currentLine.options.Length > 0)
        {
            // If we have options, create buttons and disable the next button (if present)
            DisplayOptions(currentLine.options);
            if (nextButton != null)
            {
                nextButton.interactable = false;
                var txt = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "Opciones";
            }
        }
        else
        {
            // No options: ensure next button is available
            if (nextButton != null)
            {
                nextButton.interactable = true;
                bool isLastDialogue = currentDialogueIndex >= currentNearbyEnemy.dialogueLines.Length - 1;
                nextButton.GetComponentInChildren<TextMeshProUGUI>().text = isLastDialogue ? "Cerrar" : "Siguiente (F)";
            }
        }
    }

    private void DisplayOptions(DialogueOption[] options)
    {
        if (optionsContainer == null || optionButtonPrefab == null)
            return;

        foreach (DialogueOption option in options)
        {
            UnityEngine.UI.Button newButton = Instantiate(optionButtonPrefab, optionsContainer);
            // Mark option buttons so ClearOptions only removes these and not other canvas children
            newButton.name = "Option - " + (string.IsNullOrEmpty(option.optionText) ? "unnamed" : option.optionText);
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = option.optionText;

            // Remove default listeners to avoid unexpected behavior
            newButton.onClick.RemoveAllListeners();

            // Use a local copy to avoid closure issues
            DialogueOption optionCopy = option;
            newButton.onClick.AddListener(() => SelectOption(optionCopy));

            Debug.Log("[AdvancedDialogueSystem] Created option button='" + newButton.name + "' with text='" + option.optionText + "'");
        }
    }

    private void ClearOptions()
    {
        if (optionsContainer == null)
            return;

        // Only destroy children that were created as option buttons by this system.
        // We detect them by the name prefix used in DisplayOptions ("Option - ").
        // This prevents accidentally deleting unrelated UI elements if the optionsContainer
        // reference is misassigned to a parent canvas or panel.
        var children = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in optionsContainer)
        {
            children.Add(child);
        }

        foreach (Transform child in children)
        {
            if (child == null) continue;
            if (child.name != null && child.name.StartsWith("Option - "))
                Destroy(child.gameObject);
        }
    }
}
