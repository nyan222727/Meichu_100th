using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 控制 Image 需要引入此命名空間

public class UltimateSkillManager : MonoBehaviour
{
    [Header("測試設定")]
    public bool enableKeyboardDebugTrigger = false;
    public KeyCode triggerKey = KeyCode.B; // Debug 用，正式流程由玩家大招觸發
    public float fadeDuration = 1f;        // 淡出時間（1秒）

    [Header("UI 綁定")]
    public Image ultimateImage; // 綁定大招圖片元件

    private bool isPlaying = false; // 防止大招播放期間重複觸發

    void Start()
    {
        // 遊戲一開始，先確保大招圖片是關閉的
        if (ultimateImage != null)
        {
            ultimateImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (enableKeyboardDebugTrigger && Input.GetKeyDown(triggerKey))
        {
            TriggerUltimate();
        }
    }

    // 公開函式：玩家真正觸發狐狸大招時呼叫
    public void TriggerUltimate()
    {
        if (ultimateImage == null || isPlaying) return;
        StartCoroutine(UltimateRoutine());
    }

    // 大招核心協程
    private IEnumerator UltimateRoutine()
    {
        isPlaying = true;

        // 1. 瞬間展現：打開圖片，並將透明度（Alpha）設為 1 (完全不透明)
        ultimateImage.gameObject.SetActive(true);
        Color originalColor = ultimateImage.color;
        ultimateImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

        // 💡 提示：如果未來想加特效音（如尖叫聲或閃電聲），可以在這裡播放！

        // 2. 漸漸淡出：在一秒內將透明度降到 0
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            // 計算當前的透明度百分比
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            ultimateImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            yield return null; // 等待下一幀
        }

        // 3. 播放完畢：關閉圖片，釋放鎖定
        ultimateImage.gameObject.SetActive(false);
        isPlaying = false;
    }
}
