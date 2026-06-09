using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuizData
{
    public string QuestionText;
    public List<int> Options;
    public int CorrectOptionIndex;
}

public class QuizManager : MonoBehaviour
{
    [Header("測試設定")]
    public bool autoStartQuestions = false;
    public float questionInterval = 8f;
    public float answerTimeLimit = 5f;

    [Header("玩家懲罰")]
    public PlayerHealth playerHealth;
    public int wrongAnswerDamage = 10;
    public int timeoutDamage = 15;

    [Header("UI 綁定區塊")]
    public GameObject quizPanel;
    public GameObject questionBoardUI; // 🌟 新增：用來「整包隱藏」的卷軸面板
    public TextMeshProUGUI questionTextUI;
    public TextMeshProUGUI[] optionTextsUI;
    public Button[] optionButtons;
    public TextMeshProUGUI timerTextUI;

    [Header("新增：回饋提示 UI")]
    public TextMeshProUGUI feedbackTextUI;

    private QuizData currentQuiz;
    private Coroutine timeoutCoroutine;
    private bool isAnswering = false;
    private PlayerHealth activeDamageTarget;
    private int activeWrongAnswerDamage;
    private int activeTimeoutDamage;

    public bool IsAnswering => isAnswering;

    private void Start()
    {
        ResolvePlayerHealth();

        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }

        if (autoStartQuestions)
        {
            StartCoroutine(TestQuizRoutine());
        }
    }

    private IEnumerator TestQuizRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(questionInterval);
            if (!isAnswering) TriggerNewQuiz();
        }
    }

    public void TriggerNewQuiz()
    {
        TryTriggerNewQuiz();
    }

    public bool TryTriggerNewQuiz(
        PlayerHealth damageTarget = null,
        int wrongAnswerDamageOverride = -1,
        int timeoutDamageOverride = -1)
    {
        if (isAnswering)
        {
            return false;
        }

        if (!ValidateReferences())
        {
            return false;
        }

        isAnswering = true;
        activeDamageTarget = damageTarget != null ? damageTarget : ResolvePlayerHealth();
        activeWrongAnswerDamage = wrongAnswerDamageOverride >= 0 ? wrongAnswerDamageOverride : wrongAnswerDamage;
        activeTimeoutDamage = timeoutDamageOverride >= 0 ? timeoutDamageOverride : timeoutDamage;
        currentQuiz = GenerateMathQuiz();
        GameAudioController.PlayQuestion();

        // 🌟 恢復顯示：打開黑幕背景與卷軸面板
        if (quizPanel.GetComponent<Image>() != null)
            quizPanel.GetComponent<Image>().enabled = true;
        questionBoardUI.SetActive(true);

        feedbackTextUI.gameObject.SetActive(false);
        SetOptionButtonsInteractable(true);

        questionTextUI.text = currentQuiz.QuestionText;
        for (int i = 0; i < 3; i++)
        {
            optionTextsUI[i].text = currentQuiz.Options[i].ToString();

            int buttonIndex = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(buttonIndex));
        }

        quizPanel.SetActive(true);

        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(TimeoutRoutine());
        return true;
    }

    private void OnOptionSelected(int selectedIndex)
    {
        if (!isAnswering) return;
        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);

        if (selectedIndex == currentQuiz.CorrectOptionIndex)
        {
            Debug.Log("<color=green>答對了！</color>");
            SetOptionButtonsInteractable(false);
            StartCoroutine(ShowFeedbackAndClose("答對了", Color.green));
        }
        else
        {
            Debug.Log("<color=red>答錯了！</color>");
            SetOptionButtonsInteractable(false);
            GameAudioController.PlayWrongQuestion();
            ApplyPenalty(activeWrongAnswerDamage);
            StartCoroutine(ShowFeedbackAndClose("答錯了", Color.red));
        }
    }

    private IEnumerator TimeoutRoutine()
    {
        float currentTime = answerTimeLimit;
        while (currentTime > 0)
        {
            timerTextUI.text = Mathf.CeilToInt(currentTime).ToString();
            currentTime -= Time.deltaTime;
            yield return null;
        }

        timerTextUI.text = "0";
        Debug.Log("<color=orange>超時未作答！</color>");
        SetOptionButtonsInteractable(false);
        GameAudioController.PlayWrongQuestion();
        ApplyPenalty(activeTimeoutDamage);
        StartCoroutine(ShowFeedbackAndClose("爛透了", new Color(1f, 0.5f, 0f)));
    }

    private IEnumerator ShowFeedbackAndClose(string message, Color textColor)
    {
        // 🌟 瞬間把畫面還給玩家：關閉黑幕與卷軸 (連同裡面的按鈕與題目)
        if (quizPanel.GetComponent<Image>() != null)
            quizPanel.GetComponent<Image>().enabled = false;
        questionBoardUI.SetActive(false);

        // 顯示浮空的回饋文字
        feedbackTextUI.gameObject.SetActive(true);
        feedbackTextUI.text = message;

        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            feedbackTextUI.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            yield return null;
        }

        CloseQuiz();
    }

    private void CloseQuiz()
    {
        isAnswering = false;
        activeDamageTarget = null;

        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }
    }

    private PlayerHealth ResolvePlayerHealth()
    {
        if (playerHealth != null)
        {
            return playerHealth;
        }

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        return playerHealth;
    }

    private void ApplyPenalty(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        PlayerHealth target = activeDamageTarget != null ? activeDamageTarget : ResolvePlayerHealth();
        if (target == null)
        {
            Debug.LogWarning("[QuizManager] PlayerHealth is missing. Quiz penalty was skipped.");
            return;
        }

        target.TakeDamage(damage);
    }

    private void SetOptionButtonsInteractable(bool interactable)
    {
        if (optionButtons == null)
        {
            return;
        }

        foreach (Button button in optionButtons)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    private bool ValidateReferences()
    {
        bool valid = quizPanel != null
            && questionBoardUI != null
            && questionTextUI != null
            && optionTextsUI != null
            && optionTextsUI.Length >= 3
            && optionButtons != null
            && optionButtons.Length >= 3
            && timerTextUI != null
            && feedbackTextUI != null;

        if (valid)
        {
            for (int i = 0; i < 3; i++)
            {
                valid = optionTextsUI[i] != null && optionButtons[i] != null;
                if (!valid)
                {
                    break;
                }
            }
        }

        if (!valid)
        {
            Debug.LogWarning("[QuizManager] Quiz UI references are incomplete.");
        }

        return valid;
    }

    private QuizData GenerateMathQuiz()
    {
        QuizData quiz = new QuizData();
        quiz.Options = new List<int>();
        int totalNumbers = Random.Range(2, 5);
        int remainingNumbers = totalNumbers;
        List<(int value, string expr)> terms = new List<(int value, string expr)>();

        while (remainingNumbers > 0)
        {
            int termSize = (remainingNumbers >= 2) ? Random.Range(1, 3) : 1;
            if (termSize == 1)
            {
                int a = Random.Range(1, 10);
                terms.Add((a, a.ToString()));
                remainingNumbers -= 1;
            }
            else if (termSize == 2)
            {
                bool isMultiply = Random.value > 0.5f;
                int a = Random.Range(2, 10);
                int b = Random.Range(2, 10);
                if (isMultiply) terms.Add((a * b, $"{a} * {b}"));
                else { int dividend = a * b; terms.Add((b, $"{dividend} / {a}")); }
                remainingNumbers -= 2;
            }
        }

        int finalResult = terms[0].value;
        string finalExpr = terms[0].expr;

        for (int i = 1; i < terms.Count; i++)
        {
            bool isAdd = Random.value > 0.5f;
            if (!isAdd && finalResult - terms[i].value < 0) isAdd = true;

            if (isAdd) { finalResult += terms[i].value; finalExpr += $" + {terms[i].expr}"; }
            else { finalResult -= terms[i].value; finalExpr += $" - {terms[i].expr}"; }
        }

        quiz.QuestionText = $"{finalExpr} = ?";
        quiz.Options.Add(finalResult);

        int fake1 = finalResult + Random.Range(1, 4);
        int fake2 = finalResult - Random.Range(1, 4);
        if (fake2 < 0) fake2 = finalResult + Random.Range(4, 7);
        if (fake1 == fake2) fake1++;
        quiz.Options.Add(fake1);
        quiz.Options.Add(fake2);

        for (int i = 0; i < quiz.Options.Count; i++)
        {
            int temp = quiz.Options[i];
            int randomIndex = Random.Range(i, quiz.Options.Count);
            quiz.Options[i] = quiz.Options[randomIndex];
            quiz.Options[randomIndex] = temp;
        }

        quiz.CorrectOptionIndex = quiz.Options.IndexOf(finalResult);
        return quiz;
    }
}
