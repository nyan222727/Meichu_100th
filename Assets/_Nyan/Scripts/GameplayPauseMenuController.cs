using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameplayPauseMenuController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private bool pauseTimeScale = true;

    private bool isPaused;

    private void Awake()
    {
        ResolveReferences();
        RegisterCallbacks();
        SetPaused(false);
    }

    private void OnDestroy()
    {
        UnregisterCallbacks();
        if (isPaused && pauseTimeScale)
        {
            Time.timeScale = 1f;
        }
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }

        if (pauseTimeScale)
        {
            Time.timeScale = paused ? 0f : 1f;
        }
    }

    private void ResolveReferences()
    {
        if (pauseButton == null)
        {
            pauseButton = FindButton("PauseButton");
        }

        if (restartButton == null)
        {
            restartButton = FindButton("RestartButton");
        }

        if (backToMenuButton == null)
        {
            backToMenuButton = FindButton("BackToMenuButton");
        }

        if (pausePanel == null)
        {
            Transform panel = transform.Find("Content/PausePanel");
            if (panel == null)
            {
                panel = transform.Find("PausePanel");
            }

            pausePanel = panel != null ? panel.gameObject : null;
        }
    }

    private Button FindButton(string objectName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == objectName)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void RegisterCallbacks()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(BackToMenu);
        }
    }

    private void UnregisterCallbacks()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveListener(BackToMenu);
        }
    }
}
