using UnityEngine;
using System.Collections;

public class AudioTrigger : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Settings")]
    [SerializeField] private bool playOnce = true;
    [SerializeField] private string triggerTag = "Player";
    
    [Header("Fade In")]
    [SerializeField] private bool useFadeIn = true;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] [Range(0f, 1f)] private float targetVolume = 1f;
    
    private bool hasPlayed = false;
    private Coroutine fadeCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(triggerTag)) return;
        
        if (playOnce && hasPlayed) return;
        
        if (audioSource != null && !audioSource.isPlaying)
        {
            hasPlayed = true;
            
            if (useFadeIn)
            {
                audioSource.volume = 0f;
                audioSource.Play();
                fadeCoroutine = StartCoroutine(FadeIn());
            }
            else
            {
                audioSource.volume = targetVolume;
                audioSource.Play();
            }
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeInDuration);
            yield return null;
        }
        
        audioSource.volume = targetVolume;
    }
}
