using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(VideoPlayer))]
public sealed class ReverseVideoQuadPlayer : MonoBehaviour
{
    [SerializeField] private VideoClip videoClip;
    [SerializeField, Min(1f)] private double fallbackFrameRate = 30d;
    [SerializeField] private bool hideUntilPlay = true;
    [SerializeField] private bool faceMainCameraOnPlay = true;

    private MeshRenderer meshRenderer;
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private Material runtimeMaterial;
    private Coroutine prepareRoutine;
    private double frameAccumulator;
    private long currentFrame;
    private bool playingReverse;

    public void SetClip(VideoClip clip)
    {
        videoClip = clip;
        if (videoPlayer != null)
        {
            videoPlayer.clip = videoClip;
        }
    }

    public void PlayReverse()
    {
        EnsureComponents();

        if (videoClip == null)
        {
            Debug.LogWarning("[ReverseVideoQuadPlayer] Missing video clip.", this);
            return;
        }

        if (prepareRoutine != null)
        {
            StopCoroutine(prepareRoutine);
        }

        prepareRoutine = StartCoroutine(PrepareAndPlayReverse());
    }

    public void StopAndHide()
    {
        playingReverse = false;
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    private void Awake()
    {
        EnsureComponents();
        if (hideUntilPlay)
        {
            meshRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (!playingReverse || videoPlayer == null)
        {
            return;
        }

        if (faceMainCameraOnPlay)
        {
            FaceMainCamera();
        }

        double frameRate = GetFrameRate();
        frameAccumulator += Time.unscaledDeltaTime * frameRate;
        long framesToStep = (long)frameAccumulator;
        if (framesToStep <= 0)
        {
            return;
        }

        frameAccumulator -= framesToStep;
        currentFrame = System.Math.Max(0L, currentFrame - framesToStep);
        videoPlayer.frame = currentFrame;

        if (currentFrame <= 0)
        {
            playingReverse = false;
            videoPlayer.Pause();
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    private IEnumerator PrepareAndPlayReverse()
    {
        ConfigureVideoPlayer();
        EnsureRenderTexture();

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        meshRenderer.enabled = true;
        if (faceMainCameraOnPlay)
        {
            FaceMainCamera();
        }

        currentFrame = GetLastFrame();
        frameAccumulator = 0d;
        videoPlayer.frame = currentFrame;
        videoPlayer.Play();
        videoPlayer.Pause();
        playingReverse = true;
        prepareRoutine = null;
    }

    private void EnsureComponents()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
        }
    }

    private void ConfigureVideoPlayer()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.skipOnDrop = false;
    }

    private void EnsureRenderTexture()
    {
        int width = Mathf.Max(16, videoClip != null ? (int)videoClip.width : 1280);
        int height = Mathf.Max(16, videoClip != null ? (int)videoClip.height : 720);

        if (renderTexture != null && renderTexture.width == width && renderTexture.height == height)
        {
            return;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        videoPlayer.targetTexture = renderTexture;

        if (runtimeMaterial == null)
        {
            runtimeMaterial = CreateVideoMaterial();
            meshRenderer.material = runtimeMaterial;
        }

        runtimeMaterial.mainTexture = renderTexture;
    }

    private long GetLastFrame()
    {
        ulong frameCount = videoPlayer.frameCount > 0
            ? videoPlayer.frameCount
            : videoClip != null ? videoClip.frameCount : 0;

        if (frameCount == 0)
        {
            return 0;
        }

        return frameCount > long.MaxValue ? long.MaxValue : (long)frameCount - 1;
    }

    private double GetFrameRate()
    {
        double rate = videoPlayer.frameRate;
        if (rate <= 0.01d && videoClip != null)
        {
            rate = videoClip.frameRate;
        }

        return rate > 0.01d ? rate : fallbackFrameRate;
    }

    private void FaceMainCamera()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        Vector3 toCamera = targetCamera.transform.position - transform.position;
        if (toCamera.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }

    private static Material CreateVideoMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        }

        return material;
    }
}
