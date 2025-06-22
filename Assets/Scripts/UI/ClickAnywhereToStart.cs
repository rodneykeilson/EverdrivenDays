using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class ClickAnywhereToStart : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Name of the scene to load when starting the game.")]
    public string sceneToLoad = "WorldMap";

    public GameObject loadingPanel; // Assign in Inspector
    public AdvancedLoadingBar loadingBar; // Assign in Inspector (optional, for async loading)

    private bool hasStarted = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        StartGame();
    }

    void Update()
    {
        if (!hasStarted && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            StartGame();
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
}
