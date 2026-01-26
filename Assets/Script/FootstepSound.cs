using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FootstepSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip footstepLoop;
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.6f;
    [SerializeField, Range(0.1f, 3f)] private float walkPitch = 1.0f;
    [SerializeField, Range(0f, 1f)] private float runVolume = 0.8f;
    [SerializeField, Range(0.1f, 3f)] private float runPitch = 1.1f;

    [Header("Détection de mouvement")]
    [Tooltip("Vitesse horizontale minimale pour considérer que le joueur marche")]
    [SerializeField] private float moveThreshold = 0.1f;
    [Tooltip("Lissage du volume/pitch pour éviter les sauts brusques")]
    [SerializeField] private float smoothTime = 0.1f;

    private CharacterController cc;
    private AudioSource audioSrc;

    // Smoothing
    private float targetVolume;
    private float currentVolume;
    private float volumeVel;
    private float targetPitch;
    private float currentPitch;
    private float pitchVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        audioSrc = GetComponent<AudioSource>();

        audioSrc.playOnAwake = false;
        audioSrc.loop = true;
        audioSrc.spatialBlend = 0f; // 2D par défaut; mets à 1f si tu veux du 3D
        audioSrc.clip = footstepLoop;

        currentVolume = 0f;
        targetVolume = 0f;
        currentPitch = walkPitch;
        targetPitch = walkPitch;
        ApplyAudioImmediate();
    }

    void Update()
    {
        // Vitesse horizontale du CharacterController
        Vector3 horizontalVel = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
        float speed = horizontalVel.magnitude;

        bool isGrounded = cc.isGrounded;
        bool isMoving = isGrounded && speed > moveThreshold;
        bool isRunning = isMoving && Input.GetKey(KeyCode.LeftShift);

        // Choix des cibles volume/pitch
        if (isMoving)
        {
            targetVolume = isRunning ? runVolume : walkVolume;
            targetPitch = isRunning ? runPitch : walkPitch;

            if (!audioSrc.isPlaying)
            {
                // Assure le clip
                if (audioSrc.clip != footstepLoop && footstepLoop != null)
                    audioSrc.clip = footstepLoop;

                if (audioSrc.clip != null)
                    audioSrc.Play();
            }
        }
        else
        {
            targetVolume = 0f;
            // Pitch facultatif quand on arrête; on peut garder le dernier
        }

        // Lissage
        currentVolume = Mathf.SmoothDamp(currentVolume, targetVolume, ref volumeVel, smoothTime);
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVel, smoothTime);

        audioSrc.volume = currentVolume;
        audioSrc.pitch = currentPitch;

        // Quand volume devient quasi 0, on stop le son pour éviter le souffle
        if (audioSrc.isPlaying && currentVolume <= 0.01f && targetVolume <= 0.01f)
        {
            audioSrc.Stop();
        }
    }

    private void ApplyAudioImmediate()
    {
        audioSrc.volume = currentVolume;
        audioSrc.pitch = currentPitch;
    }
}