using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "FakrazeNyanMerge";
    [SerializeField] private Button startButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button tutorialBackButton;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject tutorialPanel;

    private void Awake()
    {
        ResolveSceneReferences();

        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(ShowTutorial);
        }

        if (tutorialBackButton != null)
        {
            tutorialBackButton.onClick.AddListener(HideTutorial);
        }

        ShowMainMenu();
        GameAudioController.PlayMenuMusic();
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveListener(ShowTutorial);
        }

        if (tutorialBackButton != null)
        {
            tutorialBackButton.onClick.RemoveListener(HideTutorial);
        }
    }

    private void ResolveSceneReferences()
    {
        if (startButton == null)
        {
            startButton = FindButton("StartButton");
        }

        if (tutorialButton == null)
        {
            tutorialButton = FindButton("TutorialButton");
        }

        if (tutorialBackButton == null)
        {
            tutorialBackButton = FindButton("TutorialBackButton");
        }

        if (mainPanel == null)
        {
            mainPanel = FindChild("MainPanel");
        }

        if (tutorialPanel == null)
        {
            tutorialPanel = FindChild("TutorialPanel");
        }
    }

    private Button FindButton(string objectName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.name == objectName)
            {
                return button;
            }
        }

        return null;
    }

    private GameObject FindChild(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == objectName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    private void ShowTutorial()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }

    private void HideTutorial()
    {
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }
}
