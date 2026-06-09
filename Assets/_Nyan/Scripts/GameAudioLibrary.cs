using UnityEngine;

[CreateAssetMenu(fileName = "GameAudioLibrary", menuName = "Meichu/Audio Library")]
public class GameAudioLibrary : ScriptableObject
{
    [Header("SFX")]
    public AudioClip playerHurtClip;
    public AudioClip questionClip;
    public AudioClip wrongQuestionClip;
    public AudioClip shootClip;
    public AudioClip knifeClip;

    [Header("Loops")]
    public AudioClip strengthClip;
    public AudioClip windClip;

    [Header("Music")]
    public AudioClip menuMusicClip;
    public AudioClip gameMusicClip;
    public AudioClip victoryMusicClip;

    [Header("Volumes")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float strengthVolume = 0.65f;
    [Range(0f, 1f)] public float windVolume = 0.45f;
    [Range(0f, 1f)] public float musicVolume = 0.55f;
}
