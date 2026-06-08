using UnityEngine;

// 確保掛載此腳本的物件一定有 Rigidbody
[RequireComponent(typeof(Rigidbody))]
public class WindReceiver : MonoBehaviour
{
    private Rigidbody rb;

    [Tooltip("可以針對特定物件微調風力影響程度，預設為 1")]
    public float windEffectMultiplier = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // 確保 WindManager 存在
        if (WindManager.Instance != null)
        {
            Vector3 appliedForce = WindManager.Instance.currentWindForce * windEffectMultiplier;

            // 使用 ForceMode.Acceleration 直接給予加速度，忽略物件質量
            rb.AddForce(appliedForce, ForceMode.Acceleration);
        }
    }
}