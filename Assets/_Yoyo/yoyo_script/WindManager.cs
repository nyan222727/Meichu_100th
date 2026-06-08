using System.Collections;
using UnityEngine;

public class WindManager : MonoBehaviour
{
    // 單例模式，方便其他腳本讀取
    public static WindManager Instance;

    [Header("當前風場資訊")]
    public Vector3 currentWindForce;
    public bool isTyphoon;

    [Header("風力設定")]
    public float normalWindMin = 2f;
    public float normalWindMax = 8f;
    public float typhoonWindMin = 20f;
    public float typhoonWindMax = 40f;

    [Range(0f, 1f)]
    public float typhoonProbability = 0.1f; // 10% 機率是颱風

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // 遊戲開始時啟動風向切換的協程
        StartCoroutine(WindRoutine());
    }

    IEnumerator WindRoutine()
    {
        while (true)
        {
            GenerateNewWind();

            // 隨機等待 40 到 60 秒
            float waitTime = Random.Range(40f, 60f);
            Debug.Log($"[WindManager] 下一次風向改變在 {waitTime:F1} 秒後");

            yield return new WaitForSeconds(waitTime);
        }
    }

    void GenerateNewWind()
    {
        // 決定是否為颱風
        isTyphoon = Random.value < typhoonProbability;

        // 隨機生成 XZ 平面上的風向 (假設 Y 軸是上下，風通常是水平吹)
        float angle = Random.Range(0f, 360f);
        Vector3 windDirection = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        // 根據是否為颱風決定風力大小
        float forceMagnitude = isTyphoon ? Random.Range(typhoonWindMin, typhoonWindMax) : Random.Range(normalWindMin, normalWindMax);

        currentWindForce = windDirection * forceMagnitude;

        if (isTyphoon)
        {
            Debug.LogWarning($"[WindManager] 颱風來了！風力：{currentWindForce}");
        }
        else
        {
            Debug.Log($"[WindManager] 一般風向改變。風力：{currentWindForce}");
        }
    }
}