using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class GameplayPauseMenuController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private bool pauseTimeScale = true;

    [Header("Result Screen")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PandaBossAI pandaBoss;
    [SerializeField] private CanvasGroup resultPanel;
    [SerializeField] private Text resultTitleText;
    [SerializeField] private Text resultMessageText;
    [SerializeField] private Button resultRestartButton;
    [SerializeField] private Button resultMenuButton;
    [SerializeField] private string victoryTitle = "VICTORY";
    [SerializeField] private string defeatTitle = "FAILED";
    [SerializeField] private string victoryMessage = "Panda defeated";
    [SerializeField] private string defeatMessage = "Player defeated";
    [SerializeField] private bool pauseTimeScaleOnResult = true;

    [Header("Result Delay")]
    [SerializeField] private GameObject resultSequenceObject;
    [SerializeField] private Animator resultSequenceAnimator;
    [SerializeField] private VideoPlayer resultSequenceVideo;
    [SerializeField] private string resultSequenceStateName;
    [SerializeField] private bool activateResultSequenceOnVictory;
    [SerializeField, Min(0f)] private float resultFallbackDelay = 1.2f;
    [SerializeField, Min(0.1f)] private float resultAnimationWaitTimeout = 5f;

    private bool isPaused;
    private bool resultStarted;
    private bool resultShown;

    private void Awake()
    {
        ResolveReferences();
        EnsurePausePanel();
        EnsureResultPanel();
        RegisterCallbacks();
        SetPaused(false);
        SetResultVisible(false, false);
    }

    private void Update()
    {
        if (resultStarted)
        {
            return;
        }

        ResolveGameplayReferences();

        if (playerHealth != null && playerHealth.IsDead)
        {
            BeginResult(false);
            return;
        }

        if (pandaBoss != null && pandaBoss.IsDefeated)
        {
            BeginResult(true);
        }
    }

    private void OnDestroy()
    {
        UnregisterCallbacks();
        if ((isPaused && pauseTimeScale) || (resultShown && pauseTimeScaleOnResult))
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
            Transform panel = transform.Find("PausePanel");
            if (panel == null)
            {
                panel = transform.Find("Content/PausePanel");
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

        if (resultRestartButton != null)
        {
            resultRestartButton.onClick.AddListener(RestartGame);
        }

        if (resultMenuButton != null)
        {
            resultMenuButton.onClick.AddListener(BackToMenu);
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

        if (resultRestartButton != null)
        {
            resultRestartButton.onClick.RemoveListener(RestartGame);
        }

        if (resultMenuButton != null)
        {
            resultMenuButton.onClick.RemoveListener(BackToMenu);
        }
    }

    private void BeginResult(bool victory)
    {
        resultStarted = true;
        isPaused = false;
        StopGameplayForResult();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }

        if (!victory)
        {
            ShowResult(false);
            return;
        }

        StartCoroutine(ShowResultAfterSequence(victory));
    }

    private IEnumerator ShowResultAfterSequence(bool victory)
    {
        yield return WaitForResultSequence(victory);
        ShowResult(victory);
    }

    private IEnumerator WaitForResultSequence(bool victory)
    {
        ResolveResultSequence();

        if (resultSequenceObject != null && activateResultSequenceOnVictory)
        {
            resultSequenceObject.SetActive(true);
        }

        if (resultSequenceAnimator == null && resultSequenceObject != null)
        {
            resultSequenceAnimator = resultSequenceObject.GetComponentInChildren<Animator>(true);
        }

        if (resultSequenceVideo == null && resultSequenceObject != null)
        {
            resultSequenceVideo = resultSequenceObject.GetComponentInChildren<VideoPlayer>(true);
        }

        if (resultSequenceObject == null || (resultSequenceAnimator == null && resultSequenceVideo == null))
        {
            yield return new WaitForSecondsRealtime(resultFallbackDelay);
            yield break;
        }

        if (resultSequenceVideo != null)
        {
            yield return WaitForVideoSequence();
            yield break;
        }

        float remaining = resultAnimationWaitTimeout;
        bool enteredTargetState = string.IsNullOrWhiteSpace(resultSequenceStateName);
        int targetStateHash = string.IsNullOrWhiteSpace(resultSequenceStateName)
            ? 0
            : Animator.StringToHash(resultSequenceStateName);

        while (remaining > 0f)
        {
            if (resultSequenceObject.activeInHierarchy)
            {
                AnimatorStateInfo stateInfo = resultSequenceAnimator.GetCurrentAnimatorStateInfo(0);
                bool isTargetState = enteredTargetState
                    || stateInfo.shortNameHash == targetStateHash
                    || stateInfo.IsName(resultSequenceStateName);

                if (isTargetState)
                {
                    enteredTargetState = true;
                    if (stateInfo.normalizedTime >= 1f && !resultSequenceAnimator.IsInTransition(0))
                    {
                        yield break;
                    }
                }
            }

            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForVideoSequence()
    {
        float remaining = resultAnimationWaitTimeout;
        float elapsedAfterStart = 0f;
        float targetDuration = resultFallbackDelay;
        bool started = false;

        while (remaining > 0f)
        {
            if (resultSequenceObject != null
                && resultSequenceObject.activeInHierarchy
                && resultSequenceVideo != null
                && resultSequenceVideo.isActiveAndEnabled)
            {
                if (!started)
                {
                    started = true;

                    if (resultSequenceVideo.length > 0.1)
                    {
                        targetDuration = Mathf.Min(resultAnimationWaitTimeout, (float)resultSequenceVideo.length);
                    }

                    if (!resultSequenceVideo.isPlaying)
                    {
                        resultSequenceVideo.Play();
                    }
                }

                elapsedAfterStart += Time.unscaledDeltaTime;
                if (elapsedAfterStart >= targetDuration)
                {
                    yield break;
                }
            }

            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void ShowResult(bool victory)
    {
        resultShown = true;

        if (victory)
        {
            GameAudioController.PlayVictoryMusic();
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = victory ? victoryTitle : defeatTitle;
            resultTitleText.color = victory
                ? new Color(0.35f, 0.84f, 0.77f, 0.95f)
                : new Color(1f, 0.71f, 0.37f, 0.95f);
        }

        if (resultMessageText != null)
        {
            resultMessageText.text = victory ? victoryMessage : defeatMessage;
        }

        SetResultVisible(true, true);

        if (pauseTimeScaleOnResult)
        {
            Time.timeScale = 0f;
        }
    }

    private void StopGameplayForResult()
    {
        PlayerCombatController combatController = FindFirstObjectByType<PlayerCombatController>(FindObjectsInactive.Include);
        if (combatController != null)
        {
            combatController.enabled = false;
        }
    }

    private void SetResultVisible(bool visible, bool interactive)
    {
        if (resultPanel == null)
        {
            return;
        }

        resultPanel.gameObject.SetActive(visible);
        resultPanel.alpha = visible ? 1f : 0f;
        resultPanel.interactable = visible && interactive;
        resultPanel.blocksRaycasts = visible && interactive;
    }

    private void ResolveGameplayReferences()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        }

        if (pandaBoss == null)
        {
            pandaBoss = FindFirstObjectByType<PandaBossAI>(FindObjectsInactive.Include);
        }
    }

    private void ResolveResultSequence()
    {
        ResolveGameplayReferences();

        if (resultSequenceObject == null && pandaBoss != null)
        {
            resultSequenceObject = pandaBoss.goDownElevatorObject;
            if (resultSequenceObject == null)
            {
                Transform elevator = pandaBoss.transform.Find("GoDownElevator");
                if (elevator == null)
                {
                    elevator = pandaBoss.transform.Find("GoDownEvlelator");
                }

                resultSequenceObject = elevator != null ? elevator.gameObject : null;
            }
        }

        if (resultSequenceAnimator == null && resultSequenceObject != null)
        {
            resultSequenceAnimator = resultSequenceObject.GetComponentInChildren<Animator>(true);
        }

        if (resultSequenceVideo == null && resultSequenceObject != null)
        {
            resultSequenceVideo = resultSequenceObject.GetComponentInChildren<VideoPlayer>(true);
        }
    }

    private void EnsurePausePanel()
    {
        Transform existingRootPanel = transform.Find("PausePanel");
        if (existingRootPanel != null && existingRootPanel.Find("Card") != null)
        {
            pausePanel = existingRootPanel.gameObject;
            restartButton = existingRootPanel.Find("Card/RestartButton")?.GetComponent<Button>() ?? restartButton;
            backToMenuButton = existingRootPanel.Find("Card/BackToMenuButton")?.GetComponent<Button>() ?? backToMenuButton;
            return;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        RectTransform panel = CreateRect("PausePanel", transform);
        Stretch(panel);

        Image dim = panel.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.58f);
        dim.raycastTarget = true;

        RectTransform card = CreateRect("Card", panel);
        Center(card, new Vector2(0f, 0f), new Vector2(300f, 230f));
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = new Color(0.03f, 0.03f, 0.03f, 0.82f);
        cardImage.raycastTarget = false;

        Text title = CreateText("Title", card, "PAUSED", 36, FontStyle.Bold, new Color(1f, 1f, 1f, 0.9f));
        Center(title.rectTransform, new Vector2(0f, 65f), new Vector2(260f, 48f));

        Text message = CreateText("Message", card, "Game paused", 18, FontStyle.Bold, new Color(1f, 1f, 1f, 0.72f));
        Center(message.rectTransform, new Vector2(0f, 24f), new Vector2(250f, 34f));

        restartButton = CreateButton("RestartButton", card, "Restart");
        Center(restartButton.GetComponent<RectTransform>(), new Vector2(0f, -34f), new Vector2(190f, 44f));

        backToMenuButton = CreateButton("BackToMenuButton", card, "Menu");
        Center(backToMenuButton.GetComponent<RectTransform>(), new Vector2(0f, -88f), new Vector2(190f, 44f));

        pausePanel = panel.gameObject;
    }

    private void EnsureResultPanel()
    {
        if (resultPanel != null)
        {
            return;
        }

        Transform existingPanel = transform.Find("ResultPanel");
        if (existingPanel != null)
        {
            resultPanel = existingPanel.GetComponent<CanvasGroup>();
            if (resultPanel == null)
            {
                resultPanel = existingPanel.gameObject.AddComponent<CanvasGroup>();
            }

            resultTitleText ??= existingPanel.Find("Card/Title")?.GetComponent<Text>();
            resultMessageText ??= existingPanel.Find("Card/Message")?.GetComponent<Text>();
            resultRestartButton ??= existingPanel.Find("Card/ResultRestartButton")?.GetComponent<Button>();
            resultRestartButton ??= existingPanel.Find("Card/RestartButton")?.GetComponent<Button>();
            resultMenuButton ??= existingPanel.Find("Card/ResultMenuButton")?.GetComponent<Button>();
            resultMenuButton ??= existingPanel.Find("Card/MenuButton")?.GetComponent<Button>();
            return;
        }

        RectTransform panel = CreateRect("ResultPanel", transform);
        Stretch(panel);

        Image dim = panel.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.58f);
        dim.raycastTarget = true;

        resultPanel = panel.gameObject.AddComponent<CanvasGroup>();

        RectTransform card = CreateRect("Card", panel);
        Center(card, new Vector2(0f, 0f), new Vector2(300f, 230f));
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = new Color(0.03f, 0.03f, 0.03f, 0.82f);
        cardImage.raycastTarget = false;

        resultTitleText = CreateText("Title", card, defeatTitle, 36, FontStyle.Bold, new Color(1f, 0.71f, 0.37f, 0.95f));
        Center(resultTitleText.rectTransform, new Vector2(0f, 65f), new Vector2(260f, 48f));

        resultMessageText = CreateText("Message", card, defeatMessage, 18, FontStyle.Bold, new Color(1f, 1f, 1f, 0.72f));
        Center(resultMessageText.rectTransform, new Vector2(0f, 24f), new Vector2(250f, 34f));

        resultRestartButton = CreateButton("ResultRestartButton", card, "Restart");
        Center(resultRestartButton.GetComponent<RectTransform>(), new Vector2(0f, -34f), new Vector2(190f, 44f));

        resultMenuButton = CreateButton("ResultMenuButton", card, "Menu");
        Center(resultMenuButton.GetComponent<RectTransform>(), new Vector2(0f, -88f), new Vector2(190f, 44f));
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = GetBuiltinFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.18f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateText("Label", buttonObject.transform, label, 20, FontStyle.Bold, new Color(1f, 1f, 1f, 0.9f));
        Stretch(text.rectTransform);
        return button;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void Center(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }
}
