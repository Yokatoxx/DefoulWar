using UnityEngine;

/// <summary>
/// Joue un son spatialisé (3D) lorsque le joueur entre dans la zone trigger.
/// Permet de donner vie aux PNJ avec des sons ambiants/barks.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class Bark : MonoBehaviour
{
    [Header("Sons")]
    [Tooltip("Liste de sons à jouer aléatoirement")]
    [SerializeField] private AudioClip[] audioClips;
    
    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float minVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;
    
    [Header("Pitch (variation)")]
    [SerializeField, Range(0.5f, 1.5f)] private float minPitch = 0.9f;
    [SerializeField, Range(0.5f, 1.5f)] private float maxPitch = 1.1f;
    
    [Header("Comportement")]
    [Tooltip("Si activé, le son ne sera joué qu'une seule fois")]
    [SerializeField] private bool playOnce = false;
    
    [Tooltip("Temps minimum entre deux déclenchements (en secondes)")]
    [SerializeField] private float cooldown = 2f;
    
    [Tooltip("Délai avant de jouer le son (en secondes)")]
    [SerializeField] private float delay = 0f;
    
    [Header("Spatialisation 3D")]
    [Tooltip("Distance minimale pour le son 3D")]
    [SerializeField] private float minDistance = 1f;
    
    [Tooltip("Distance maximale pour le son 3D")]
    [SerializeField] private float maxDistance = 20f;
    
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    
    // État interne
    private AudioSource _audioSource;
    private bool _hasPlayed = false;
    private float _lastPlayTime = -Mathf.Infinity;
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Configuration de l'AudioSource pour le son 3D
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f; // Full 3D
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.minDistance = minDistance;
        _audioSource.maxDistance = maxDistance;
    }
    
    /// <summary>
    /// Détection d'entrée dans le trigger (3D)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            TryPlaySound();
        }
    }
    
    /// <summary>
    /// Détection d'entrée dans le trigger (2D)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            TryPlaySound();
        }
    }
    
    /// <summary>
    /// Tente de jouer un son si les conditions sont remplies
    /// </summary>
    private void TryPlaySound()
    {
        // Vérification si déjà joué (mode playOnce)
        if (playOnce && _hasPlayed)
            return;
        
        // Vérification du cooldown
        if (Time.time - _lastPlayTime < cooldown)
            return;
        
        // Vérification qu'il y a des clips
        if (audioClips == null || audioClips.Length == 0)
        {
            Debug.LogWarning($"Bark: Aucun AudioClip assigné sur {gameObject.name}");
            return;
        }
        
        _lastPlayTime = Time.time;
        _hasPlayed = true;
        
        if (delay > 0f)
        {
            Invoke(nameof(PlayRandomSound), delay);
        }
        else
        {
            PlayRandomSound();
        }
    }
    
    /// <summary>
    /// Joue un son aléatoire de la liste avec pitch et volume aléatoires
    /// </summary>
    private void PlayRandomSound()
    {
        // Sélection aléatoire d'un clip
        AudioClip clip = audioClips[Random.Range(0, audioClips.Length)];
        
        if (clip == null)
        {
            Debug.LogWarning($"Bark: Un AudioClip est null dans la liste de {gameObject.name}");
            return;
        }
        
        // Application du pitch aléatoire
        _audioSource.pitch = Random.Range(minPitch, maxPitch);
        
        // Volume aléatoire
        float volume = Random.Range(minVolume, maxVolume);
        
        // Jouer le son
        _audioSource.PlayOneShot(clip, volume);
    }
    
    /// <summary>
    /// Réinitialise l'état du Bark (permet de rejouer même en mode playOnce)
    /// </summary>
    public void ResetBark()
    {
        _hasPlayed = false;
        _lastPlayTime = -Mathf.Infinity;
    }
    
    /// <summary>
    /// Force la lecture d'un son (ignore les conditions)
    /// </summary>
    public void ForcePlaySound()
    {
        PlayRandomSound();
    }
}
