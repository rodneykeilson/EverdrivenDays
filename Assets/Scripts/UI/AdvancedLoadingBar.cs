using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class AdvancedLoadingBar : MonoBehaviour
{
    public string sceneToLoad = "WorldMap";
    public GameObject loadingPanel; // Assign in Inspector
    public Slider loadingBarSlider; // Assign in Inspector (Slider component)
    public TextMeshProUGUI loadingText; // Assign in Inspector for percentage text

    public void StartLoading()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        if (loadingBarSlider != null)
            loadingBarSlider.value = 0f;
        StartCoroutine(LoadAsyncScene());
    }

    private IEnumerator LoadAsyncScene()
    {
        yield return null; // Ensure UI updates
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncOp.allowSceneActivation = false;

        while (!asyncOp.isDone)
        {
            float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
            if (loadingBarSlider != null)
                loadingBarSlider.value = progress;
            if (loadingText != null)
                loadingText.text = Mathf.RoundToInt(progress * 100f) + "%";

            if (asyncOp.progress >= 0.9f)
            {
                if (loadingBarSlider != null)
                    loadingBarSlider.value = 1f;
                if (loadingText != null)
                    loadingText.text = "100%";
                yield return new WaitForSeconds(0.5f);
                asyncOp.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
