using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    [Header("AudioSource")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Crée automatiquement un AudioSource si non assigné.")]
    [SerializeField] private bool createSourceIfMissing = true;
    [Tooltip("0 = 2D, 1 = 3D")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 0f;

    [Header("Librairie (optionnelle)")]
    [SerializeField] private List<NamedClip> clips = new List<NamedClip>();

    [Serializable]
    public struct NamedClip
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float defaultVolume;
    }

    private void Awake()
    {
        if (audioSource == null && createSourceIfMissing)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        if (audioSource != null)
        {
            audioSource.spatialBlend = spatialBlend;
        }
    }

    // Lecture immédiate d’un clip (one-shot)
    public void PlayOneShot(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || audioSource == null) return;
        float oldPitch = audioSource.pitch;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        audioSource.pitch = oldPitch; // restore
    }

    // Lecture par "clé" depuis la librairie
    public void PlayOneShot(string key, float? overrideVolume = null, float pitch = 1f)
    {
        if (!TryGetClip(key, out var clip, out var defVol)) return;
        PlayOneShot(clip, overrideVolume.HasValue ? overrideVolume.Value : defVol, pitch);
    }

    // Lecture en boucle (par clip)
    public void PlayLoop(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.clip = clip;
        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.pitch = pitch;
        audioSource.loop = true;
        audioSource.Play();
    }

    // Lecture en boucle (par clé)
    public void PlayLoop(string key, float? overrideVolume = null, float pitch = 1f)
    {
        if (!TryGetClip(key, out var clip, out var defVol)) return;
        PlayLoop(clip, overrideVolume.HasValue ? overrideVolume.Value : defVol, pitch);
    }

    public void StopLoop()
    {
        if (audioSource == null) return;
        audioSource.Stop();
        audioSource.loop = false;
        audioSource.clip = null;
    }

    // Lecture ponctuelle dans l’espace (utilise un AudioSource éphémère)
    public static void PlayAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
    }

    private bool TryGetClip(string key, out AudioClip clip, out float defaultVolume)
    {
        clip = null;
        defaultVolume = 1f;
        if (string.IsNullOrEmpty(key)) return false;
        foreach (var nc in clips)
        {
            if (!string.IsNullOrEmpty(nc.key) && string.Equals(nc.key, key, StringComparison.Ordinal))
            {
                clip = nc.clip;
                defaultVolume = nc.defaultVolume <= 0f ? 1f : nc.defaultVolume;
                return clip != null;
            }
        }
        return false;
    }
}