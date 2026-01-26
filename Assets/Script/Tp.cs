using UnityEngine;

public class Tp : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Transform player;
    [SerializeField] private ScreenFader screenFader;
    
    [Header("Position de téléportation")]
    [SerializeField] private Transform targetPosition; // Empty à assigner dans l'inspecteur

    [Header("Objets à activer/désactiver")]
    [SerializeField] private GameObject[] objectsToDisable;
    [SerializeField] private GameObject[] objectsToEnable;

    [Header("Paramètres de fade")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private bool useFade = true;

    [Header("Trigger")]
    [SerializeField] private bool teleportOnTrigger = true;
    [SerializeField] private string playerTag = "Player";

    private bool isTeleporting = false;

    private void Start()
    {
        // Cherche automatiquement le joueur si non assigné
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Cherche automatiquement le ScreenFader si non assigné
        if (screenFader == null)
        {
            screenFader = ScreenFader.Instance;
        }
    }

    /// <summary>
    /// Détection d'entrée dans le trigger (3D)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!teleportOnTrigger || isTeleporting) return;

        if (other.CompareTag(playerTag))
        {
            isTeleporting = true;
            Teleport();
            
            // Reset après le fade
            if (useFade)
            {
                Invoke(nameof(ResetTeleporting), fadeDuration * 2f + 0.2f);
            }
            else
            {
                isTeleporting = false;
            }
        }
    }

    /// <summary>
    /// Détection d'entrée dans le trigger (2D)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!teleportOnTrigger || isTeleporting) return;

        if (other.CompareTag(playerTag))
        {
            isTeleporting = true;
            Teleport();
            
            if (useFade)
            {
                Invoke(nameof(ResetTeleporting), fadeDuration * 2f + 0.2f);
            }
            else
            {
                isTeleporting = false;
            }
        }
    }

    private void ResetTeleporting()
    {
        isTeleporting = false;
    }

    /// <summary>
    /// Téléporte le joueur à la position cible (utilise les paramètres de l'inspecteur)
    /// </summary>
    public void Teleport()
    {
        if (useFade)
        {
            TeleportWithFade(targetPosition, objectsToDisable, objectsToEnable);
        }
        else
        {
            TeleportInstant();
        }
    }

    /// <summary>
    /// Téléporte instantanément sans fade
    /// </summary>
    public void TeleportInstant()
    {
        if (player != null && targetPosition != null)
        {
            player.position = targetPosition.position;
            player.rotation = targetPosition.rotation;
        }
        SwitchObjects(objectsToDisable, objectsToEnable);
    }

    /// <summary>
    /// Téléporte le joueur avec un fade noir, désactive des objets et en active d'autres
    /// </summary>
    /// <param name="target">Position de destination</param>
    /// <param name="toDisable">GameObjects à désactiver</param>
    /// <param name="toEnable">GameObjects à activer</param>
    public void TeleportWithFade(Transform target, GameObject[] toDisable, GameObject[] toEnable)
    {
        if (screenFader == null)
            screenFader = ScreenFader.Instance;

        if (screenFader != null)
        {
            screenFader.DoFadeSequence(() =>
            {
                // Téléportation
                if (player != null && target != null)
                {
                    player.position = target.position;
                    player.rotation = target.rotation;
                }

                // Désactivation/Activation des objets
                SwitchObjects(toDisable, toEnable);

            }, fadeDuration);
        }
        else
        {
            // Sans fade, exécution directe
            if (player != null && target != null)
            {
                player.position = target.position;
                player.rotation = target.rotation;
            }
            SwitchObjects(toDisable, toEnable);
        }
    }

    /// <summary>
    /// Téléporte le joueur avec un fade noir (version avec Vector3)
    /// </summary>
    public void TeleportWithFade(Vector3 target, GameObject[] toDisable, GameObject[] toEnable)
    {
        if (screenFader == null)
            screenFader = ScreenFader.Instance;

        if (screenFader != null)
        {
            screenFader.DoFadeSequence(() =>
            {
                if (player != null)
                    player.position = target;

                SwitchObjects(toDisable, toEnable);

            }, fadeDuration);
        }
        else
        {
            if (player != null)
                player.position = target;
            SwitchObjects(toDisable, toEnable);
        }
    }

    /// <summary>
    /// Désactive des GameObjects et en active d'autres (sans fade)
    /// </summary>
    /// <param name="toDisable">GameObjects à désactiver</param>
    /// <param name="toEnable">GameObjects à activer</param>
    public void SwitchObjects(GameObject[] toDisable, GameObject[] toEnable)
    {
        if (toDisable != null)
        {
            foreach (GameObject obj in toDisable)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if (toEnable != null)
        {
            foreach (GameObject obj in toEnable)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Désactive un GameObject et en active un autre (sans fade) - version simple
    /// </summary>
    public void SwitchObjects(GameObject toDisable, GameObject toEnable)
    {
        if (toDisable != null)
            toDisable.SetActive(false);

        if (toEnable != null)
            toEnable.SetActive(true);
    }

    /// <summary>
    /// Change uniquement l'état des objets avec un fade noir
    /// </summary>
    public void SwitchObjectsWithFade(GameObject[] toDisable, GameObject[] toEnable)
    {
        if (screenFader == null)
            screenFader = ScreenFader.Instance;

        if (screenFader != null)
        {
            screenFader.DoFadeSequence(() =>
            {
                SwitchObjects(toDisable, toEnable);
            }, fadeDuration);
        }
        else
        {
            SwitchObjects(toDisable, toEnable);
        }
    }

    /// <summary>
    /// Change uniquement l'état des objets avec un fade noir - version simple
    /// </summary>
    public void SwitchObjectsWithFade(GameObject toDisable, GameObject toEnable)
    {
        if (screenFader == null)
            screenFader = ScreenFader.Instance;

        if (screenFader != null)
        {
            screenFader.DoFadeSequence(() =>
            {
                SwitchObjects(toDisable, toEnable);
            }, fadeDuration);
        }
        else
        {
            SwitchObjects(toDisable, toEnable);
        }
    }
}
