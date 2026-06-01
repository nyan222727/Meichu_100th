using System.Collections;
using UnityEngine;

public class PandaSoundController : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Sound Effects")]
    public AudioClip clawSound;
    public AudioClip throwSound;
    public AudioClip castSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    [Header("Sound Delay")]
    [Tooltip("Claw 觸發後幾秒播放聲音")]
    public float clawSoundDelay = 0.1f;

    [Tooltip("Throw / Plum 觸發後幾秒播放聲音")]
    public float throwSoundDelay = 0.2f;

    [Tooltip("Cast / Meteor 觸發後幾秒播放聲音")]
    public float castSoundDelay = 0.1f;

    [Tooltip("Hit 觸發後幾秒播放聲音")]
    public float hitSoundDelay = 0f;

    [Tooltip("Death 觸發後幾秒播放聲音")]
    public float deathSoundDelay = 0.2f;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayClawSound()
    {
        PlaySoundWithDelay(clawSound, clawSoundDelay);
    }

    public void PlayThrowSound()
    {
        PlaySoundWithDelay(throwSound, throwSoundDelay);
    }

    public void PlayCastSound()
    {
        PlaySoundWithDelay(castSound, castSoundDelay);
    }

    public void PlayHitSound()
    {
        PlaySoundWithDelay(hitSound, hitSoundDelay);
    }

    public void PlayDeathSound()
    {
        PlaySoundWithDelay(deathSound, deathSoundDelay);
    }

    private void PlaySoundWithDelay(AudioClip clip, float delay)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("PandaSoundController: Audio Source is missing.");
            return;
        }

        if (clip == null)
        {
            return;
        }

        if (delay <= 0f)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            StartCoroutine(PlaySoundAfterDelay(clip, delay));
        }
    }

    private IEnumerator PlaySoundAfterDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
