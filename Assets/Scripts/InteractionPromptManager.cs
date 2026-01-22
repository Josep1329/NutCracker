using UnityEngine;
using TMPro;

/// <summary>
/// Manages the shared interaction prompt for all enemies.
/// Only the nearest enemy within interaction distance shows the prompt.
/// </summary>
public class InteractionPromptManager : MonoBehaviour
{
    public static InteractionPromptManager Instance { get; private set; }

    [SerializeField] private GameObject promptGameObject;
    [SerializeField] private TextMeshProUGUI promptText;

    private EnemyAI currentNearestEnemy = null;
    private float lastUpdateTime = 0f;
    private float updateInterval = 0.2f; // Check every 0.2 seconds to avoid constant searching

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        if (promptGameObject != null)
            promptGameObject.SetActive(false);
    }

    void Update()
    {
        lastUpdateTime += Time.deltaTime;
        if (lastUpdateTime >= updateInterval)
        {
            UpdateNearestEnemy();
            lastUpdateTime = 0f;
        }
    }

    private void UpdateNearestEnemy()
    {
        EnemyAI nearestEnemy = FindNearestEnemyInRange();

        if (nearestEnemy != currentNearestEnemy)
        {
            // Enemy changed, update the prompt
            currentNearestEnemy = nearestEnemy;
            
            if (currentNearestEnemy != null)
            {
                ShowPrompt(currentNearestEnemy);
            }
            else
            {
                HidePrompt();
            }
        }
    }

    private EnemyAI FindNearestEnemyInRange()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        EnemyAI nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject go in enemies)
        {
            if (go == null) continue;
            EnemyAI enemy = go.GetComponent<EnemyAI>();
            if (enemy == null || enemy.playerTransform == null) continue;

            float distance = Vector3.Distance(enemy.transform.position, enemy.playerTransform.position);

            // Only consider enemies within interaction range and not in dialogue
            if (distance <= enemy.stoppingDistance && !enemy.IsInteracting())
            {
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }

    public void ShowPrompt(EnemyAI enemy)
    {
        if (promptGameObject == null)
            return;

        if (promptText != null && !enemy.IsInteracting())
            promptText.text = enemy.interactMessage;

        promptGameObject.SetActive(true);
    }

    public void ShowDialogueLine(string dialogueLine)
    {
        if (promptGameObject == null)
            return;

        if (promptText != null)
            promptText.text = dialogueLine;

        promptGameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptGameObject != null)
            promptGameObject.SetActive(false);
    }

    public bool IsShowingPrompt()
    {
        return promptGameObject != null && promptGameObject.activeSelf;
    }

    public void ForceUpdateNearestEnemy()
    {
        // Force an immediate update of the nearest enemy
        lastUpdateTime = updateInterval;
    }
}
