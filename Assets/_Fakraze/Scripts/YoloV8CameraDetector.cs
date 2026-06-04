using UnityEngine;


public class YoloV8CameraDetector : MonoBehaviour
{
    [Header("YOLO Model")]
    public Unity.InferenceEngine.ModelAsset modelAsset;

    [Header("Scene Camera")]
    public Camera targetCamera;

    [Header("Detection Setting")]
    [Range(0f, 1f)]
    public float threshold = 0.5f; // 50%

    [Tooltip("每幾秒偵測一次。0.2 = 每秒約 5 次")]
    public float detectInterval = 0.2f;

    [Header("Detection Result")]
    public float bottleConfidence = 0f;
    public bool isBlocking = false;

    private Unity.InferenceEngine.Model runtimeModel;
    private Unity.InferenceEngine.Worker worker;
    private RenderTexture cameraRenderTexture;

    private float timer = 0f;

    private const int InputWidth = 640;
    private const int InputHeight = 640;

    private const int NumBoxes = 8400;

    // COCO class index: bottle = 39
    private const int BottleClassIndex = 39;

    void Start()
    {
        if (modelAsset == null)
        {
            Debug.LogError("Model Asset is missing.");
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogError("Target Camera is missing.");
            return;
        }

        runtimeModel = Unity.InferenceEngine.ModelLoader.Load(modelAsset);
        worker = new Unity.InferenceEngine.Worker(runtimeModel, Unity.InferenceEngine.BackendType.GPUCompute);

        cameraRenderTexture = new RenderTexture(InputWidth, InputHeight, 24);
        cameraRenderTexture.Create();

        Debug.Log("YOLO Camera Detector initialized.");
    }

    void Update()
    {
        if (worker == null || targetCamera == null)
            return;

        timer += Time.deltaTime;

        if (timer >= detectInterval)
        {
            timer = 0f;
            RunDetectionFromCamera();
        }
    }

    private void RunDetectionFromCamera()
    {
        // 1. 暫存原本 Camera 的 targetTexture
        RenderTexture originalTargetTexture = targetCamera.targetTexture;

        // 2. 讓 3D Camera 畫面 render 到 640x640 RenderTexture
        targetCamera.targetTexture = cameraRenderTexture;
        targetCamera.Render();

        // 3. 還原 Camera 原本設定，避免影響正常畫面顯示
        targetCamera.targetTexture = originalTargetTexture;

        // 4. RenderTexture 轉成 YOLO input tensor
        using Unity.InferenceEngine.Tensor<float> inputTensor = Unity.InferenceEngine.TextureConverter.ToTensor(
            cameraRenderTexture,
            width: InputWidth,
            height: InputHeight,
            channels: 3
        );

        // 5. 執行 YOLO
        worker.Schedule(inputTensor);

        Unity.InferenceEngine.Tensor<float> outputTensor = worker.PeekOutput() as Unity.InferenceEngine.Tensor<float>;

        if (outputTensor == null)
        {
            Debug.LogError("YOLO output tensor is null.");
            return;
        }

        // 6. 下載 output
        float[] outputData = outputTensor.DownloadToArray();

        // 7. 分析 bottle confidence
        AnalyzeBottleConfidence(outputData);
    }

    private void AnalyzeBottleConfidence(float[] outputData)
    {
        bottleConfidence = 0f;
        isBlocking = false;

        for (int box = 0; box < NumBoxes; box++)
        {
            float confidence = GetYoloValue(outputData, 4 + BottleClassIndex, box);

            if (confidence > bottleConfidence)
            {
                bottleConfidence = confidence;
            }
        }

        isBlocking = bottleConfidence > threshold;

        if (isBlocking)
        {
            Debug.Log($"Defense ON | Bottle confidence: {bottleConfidence:F3}");
        }
        else
        {
            Debug.Log($"Defense OFF | Bottle confidence: {bottleConfidence:F3}");
        }
    }

    private float GetYoloValue(float[] data, int channel, int box)
    {
        // YOLOv8n ONNX output 通常是 [1, 84, 8400]
        // 84 = 4 bbox values + 80 class scores
        return data[channel * NumBoxes + box];
    }

    void OnDestroy()
    {
        worker?.Dispose();

        if (cameraRenderTexture != null)
        {
            cameraRenderTexture.Release();
            Destroy(cameraRenderTexture);
        }
    }
}