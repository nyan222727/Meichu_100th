using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    private const string LibraryResourceName = "GameAudioLibrary";

    private static GameAudioController instance;
    private static bool warnedMissingLibrary;

    [SerializeField] private GameAudioLibrary library;

    private AudioSource sfxSource;
    private AudioSource strengthSource;
    private AudioSource windSource;
    private AudioSource musicSource;

    private GameAudioLibrary Library
    {
        get
        {
            if (library == null)
            {
                library = Resources.Load<GameAudioLibrary>(LibraryResourceName);
                if (library == null && !warnedMissingLibrary)
                {
                    warnedMissingLibrary = true;
                    Debug.LogWarning($"[GameAudioController] Missing Resources/{LibraryResourceName}.asset.");
                }
            }

            return library;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
    }

    public static void PlayPlayerHurt()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayOneShot(controller.Library?.playerHurtClip);
    }

    public static void PlayQuestion()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayOneShot(controller.Library?.questionClip);
    }

    public static void PlayWrongQuestion()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayOneShot(controller.Library?.wrongQuestionClip);
    }

    public static void PlayShoot()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayOneShot(controller.Library?.shootClip);
    }

    public static void PlayKnife()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayOneShot(controller.Library?.knifeClip);
    }

    public static void StartStrengthLoop()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayLoop(controller.strengthSource, controller.Library?.strengthClip, controller.Library?.strengthVolume ?? 1f);
    }

    public static void StopStrengthLoop()
    {
        if (instance != null)
        {
            instance.StopLoop(instance.strengthSource);
        }
    }

    public static void StartWindLoop()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayLoop(controller.windSource, controller.Library?.windClip, controller.Library?.windVolume ?? 1f);
    }

    public static void PlayMenuMusic()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayMusic(controller.Library?.menuMusicClip);
    }

    public static void PlayGameMusic()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayMusic(controller.Library?.gameMusicClip);
    }

    public static void PlayVictoryMusic()
    {
        GameAudioController controller = EnsureInstance();
        controller.PlayMusic(controller.Library?.victoryMusicClip);
    }

    private static GameAudioController EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<GameAudioController>(FindObjectsInactive.Include);
        if (instance != null)
        {
            instance.EnsureSources();
            return instance;
        }

        GameObject audioObject = new GameObject("GameAudioController");
        instance = audioObject.AddComponent<GameAudioController>();
        return instance;
    }

    private void EnsureSources()
    {
        sfxSource ??= CreateSource("SFX", false);
        strengthSource ??= CreateSource("StrengthLoop", true);
        windSource ??= CreateSource("WindLoop", true);
        musicSource ??= CreateSource("Music", true);
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureSources();
        sfxSource.PlayOneShot(clip, Library != null ? Library.sfxVolume : 1f);
    }

    private void PlayLoop(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
        {
            return;
        }

        if (source.clip == clip && source.isPlaying)
        {
            return;
        }

        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.loop = true;
        source.Play();
    }

    private void StopLoop(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureSources();
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.volume = Library != null ? Library.musicVolume : 1f;
        musicSource.loop = true;
        musicSource.Play();
    }
}
