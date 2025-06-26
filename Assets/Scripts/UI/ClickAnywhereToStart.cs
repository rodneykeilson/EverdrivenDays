using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using EverdrivenDays; // Added namespace for SmallEnemyRhythmController

public class ClickAnywhereToStart : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Name of the scene to load when starting the game.")]
    public string sceneToLoad = "WorldMap";

    public GameObject loadingPanel; // Assign in Inspector
    public AdvancedLoadingBar loadingBar; // Assign in Inspector (optional, for async loading)
    public TMPro.TextMeshProUGUI difficultyText; // Assign in Inspector

    private bool hasStarted = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        StartGame();
    }

    void Update()
    {
        // Only start game if a key other than Y is pressed, or mouse click
        if (!hasStarted)
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                CycleDifficulty();
            }
            else if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                // Prevent Y from starting the game
                if (!Input.GetKeyDown(KeyCode.Y))
                {
                    StartGame();
                }
            }
        }
        // Update difficulty display
        if (difficultyText != null)
        {
            difficultyText.text = $"Difficulty: {SmallEnemyRhythmController.CurrentDifficulty}";
        }
    }

    private void StartGame()
    {
        if (hasStarted) return;
        hasStarted = true;
        if (loadingBar != null)
        {
            loadingBar.sceneToLoad = sceneToLoad;
            loadingBar.StartLoading();
        }
        else if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            StartCoroutine(LoadSceneWithDelay());
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private System.Collections.IEnumerator LoadSceneWithDelay()
    {
        yield return null;
        SceneManager.LoadScene(sceneToLoad);
    }

    private void CycleDifficulty()
    {
        var values = System.Enum.GetValues(typeof(SmallEnemyRhythmController.DifficultyMode));
        int current = (int)SmallEnemyRhythmController.CurrentDifficulty;
        int next = (current + 1) % values.Length;
        SmallEnemyRhythmController.CurrentDifficulty = (SmallEnemyRhythmController.DifficultyMode)values.GetValue(next);
    }
}
