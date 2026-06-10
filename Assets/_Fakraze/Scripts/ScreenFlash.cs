using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    public Color flashColor = new Color(1f, 0.85f, 0f, 0.7f);
    public float fadeInTime = 0.05f;
    public float fadeOutTime = 0.25f;

    private Image flashImage;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        flashImage = GetComponent<Image>();

        if (flashImage == null)
        {
            Debug.LogError("ScreenFlash needs an Image component.");
            return;
        }

        Color c = flashColor;
        c.a = 0f;
        flashImage.color = c;

        // 避免擋到滑鼠或手機點擊
        flashImage.raycastTarget = false;
    }

    public void Flash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 淡入
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, flashColor.a, t / fadeInTime);

            Color c = flashColor;
            c.a = alpha;
            flashImage.color = c;

            yield return null;
        }

        // 淡出
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(flashColor.a, 0f, t / fadeOutTime);

            Color c = flashColor;
            c.a = alpha;
            flashImage.color = c;

            yield return null;
        }

        Color finalColor = flashColor;
        finalColor.a = 0f;
        flashImage.color = finalColor;

        flashCoroutine = null;
    }
}