using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioClip successUIClick;
    [SerializeField] private AudioClip unsuccessfulUIClick;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlaySuccessUIClick()
    {
        audioSource.PlayOneShot(successUIClick);
    }

    public void PlayUnsuccessfulUIClick()
    {
        audioSource.PlayOneShot(unsuccessfulUIClick);
    }
}