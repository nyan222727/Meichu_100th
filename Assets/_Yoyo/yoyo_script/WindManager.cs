using System.Collections;
using UnityEngine;

public class WindManager : MonoBehaviour
{
    public static WindManager Instance;

    [Header("Current Wind")]
    public Vector3 currentWindForce;
    public bool isTyphoon;
    public bool isWindActive;

    [Header("Wind Timing")]
    [Min(0f)] public float windActiveDuration = 25f;
    [Min(0f)] public float windRestMinDuration = 30f;
    [Min(0f)] public float windRestMaxDuration = 50f;

    [Header("Wind Force")]
    public float normalWindMin = 2f;
    public float normalWindMax = 8f;
    public float typhoonWindMin = 20f;
    public float typhoonWindMax = 40f;

    [Range(0f, 1f)]
    public float typhoonProbability = 0.1f;

    private Coroutine windRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        windActiveDuration = Mathf.Max(0f, windActiveDuration);
        windRestMinDuration = Mathf.Max(0f, windRestMinDuration);
        windRestMaxDuration = Mathf.Max(windRestMinDuration, windRestMaxDuration);
        normalWindMin = Mathf.Max(0f, normalWindMin);
        normalWindMax = Mathf.Max(normalWindMin, normalWindMax);
        typhoonWindMin = Mathf.Max(0f, typhoonWindMin);
        typhoonWindMax = Mathf.Max(typhoonWindMin, typhoonWindMax);
    }

    private void OnEnable()
    {
        StopWind();
        windRoutine = StartCoroutine(WindRoutine());
    }

    private void OnDisable()
    {
        if (windRoutine != null)
        {
            StopCoroutine(windRoutine);
            windRoutine = null;
        }

        StopWind();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private IEnumerator WindRoutine()
    {
        while (true)
        {
            float restDuration = Random.Range(windRestMinDuration, windRestMaxDuration);
            Debug.Log($"[WindManager] Wind rests for {restDuration:F1} seconds.");
            yield return new WaitForSeconds(restDuration);

            GenerateNewWind();
            Debug.Log($"[WindManager] Wind active for {windActiveDuration:F1} seconds. Force={currentWindForce}");
            yield return new WaitForSeconds(windActiveDuration);

            StopWind();
        }
    }

    private void GenerateNewWind()
    {
        isTyphoon = Random.value < typhoonProbability;

        float angle = Random.Range(0f, 360f);
        Vector3 windDirection = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            0f,
            Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        float forceMagnitude = isTyphoon
            ? Random.Range(typhoonWindMin, typhoonWindMax)
            : Random.Range(normalWindMin, normalWindMax);

        currentWindForce = windDirection * forceMagnitude;
        isWindActive = true;
        GameAudioController.StartWindLoop();

        if (isTyphoon)
        {
            Debug.LogWarning($"[WindManager] Typhoon wind starts. Force={currentWindForce}");
        }
        else
        {
            Debug.Log($"[WindManager] Wind starts. Force={currentWindForce}");
        }
    }

    private void StopWind()
    {
        currentWindForce = Vector3.zero;
        isTyphoon = false;
        isWindActive = false;
        Debug.Log("[WindManager] Wind stopped.");
    }
}

