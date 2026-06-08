using UnityEngine;

public class AndroidImmersiveMode : MonoBehaviour
{
    [SerializeField] private bool enableOnStart = true;
    [SerializeField] private bool consumeBackButton = true;

    private void Awake()
    {
        if (enableOnStart)
        {
            ApplyImmersiveMode();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && enableOnStart)
        {
            ApplyImmersiveMode();
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (!isPaused && enableOnStart)
        {
            ApplyImmersiveMode();
        }
    }

    private void Update()
    {
        if (consumeBackButton && Input.GetKeyDown(KeyCode.Escape))
        {
            ApplyImmersiveMode();
        }
    }

    public void ApplyImmersiveMode()
    {
        Screen.fullScreen = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            using AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
            using AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView");

            const int systemUiFlagLowProfile = 0x00000001;
            const int systemUiFlagHideNavigation = 0x00000002;
            const int systemUiFlagFullscreen = 0x00000004;
            const int systemUiFlagLayoutStable = 0x00000100;
            const int systemUiFlagLayoutHideNavigation = 0x00000200;
            const int systemUiFlagLayoutFullscreen = 0x00000400;
            const int systemUiFlagImmersiveSticky = 0x00001000;

            int flags =
                systemUiFlagLowProfile |
                systemUiFlagHideNavigation |
                systemUiFlagFullscreen |
                systemUiFlagLayoutStable |
                systemUiFlagLayoutHideNavigation |
                systemUiFlagLayoutFullscreen |
                systemUiFlagImmersiveSticky;

            decorView.Call("setSystemUiVisibility", flags);
        }));
#endif
    }
}
