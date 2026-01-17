using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Système de téléportation de debug. Appuyez sur une touche pour recharger la scène
/// et vous téléporter à un point prédéfini.
/// </summary>
public class DebugTeleporter : MonoBehaviour
{
    [System.Serializable]
    public class TeleportPoint
    {
        public string name = "Point";
        public KeyCode key = KeyCode.F1;
        public Transform destination;
    }

    [Header("Teleport Points")]
    [SerializeField] private TeleportPoint[] teleportPoints;

    [Header("Settings")]
    [SerializeField] private bool showDebugUI = true;
    
    // Stockage static pour persister entre les rechargements de scène
    private static Vector3 pendingTeleportPosition;
    private static Quaternion pendingTeleportRotation;
    private static bool hasPendingTeleport = false;

    private Transform player;

    private void Start()
    {
        // Si on a un téléport en attente (après rechargement de scène), l'appliquer
        if (hasPendingTeleport)
        {
            Debug.Log($"[DebugTeleporter] Téléport en attente détecté -> {pendingTeleportPosition}");
            StartCoroutine(ApplyTeleportAfterDelay());
        }
    }

    private System.Collections.IEnumerator ApplyTeleportAfterDelay()
    {
        // Attendre quelques frames pour que le joueur soit bien initialisé
        yield return null;
        yield return null;
        yield return new WaitForFixedUpdate();
        
        // Chercher le joueur maintenant (après que tout soit initialisé)
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[DebugTeleporter] Joueur non trouvé après rechargement !");
            hasPendingTeleport = false;
            yield break;
        }
        
        player = playerObj.transform;
        
        // Désactiver le CharacterController temporairement si présent
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        player.position = pendingTeleportPosition;
        player.rotation = pendingTeleportRotation;

        if (cc != null)
        {
            cc.enabled = true;
        }

        Debug.Log($"[DebugTeleporter] Téléporté à {pendingTeleportPosition}");
        hasPendingTeleport = false;
    }

    private void Update()
    {
        if (teleportPoints == null) return;

        foreach (var point in teleportPoints)
        {
            if (point.destination != null && Input.GetKeyDown(point.key))
            {
                TeleportAndReload(point);
                break;
            }
        }
    }

    private void TeleportAndReload(TeleportPoint point)
    {
        // Stocker la position de destination
        pendingTeleportPosition = point.destination.position;
        pendingTeleportRotation = point.destination.rotation;
        hasPendingTeleport = true;

        Debug.Log($"[DebugTeleporter] Rechargement de la scène -> {point.name}");
        
        // Recharger la scène actuelle
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        if (!showDebugUI || teleportPoints == null) return;

        const int w = 220;
        const int h = 24;
        int x = 10;
        int y = 10;

        GUI.Label(new Rect(x, y, w, h), "<b>Debug Teleporter</b>");
        y += h + 5;

        foreach (var point in teleportPoints)
        {
            if (point.destination == null)
            {
                GUI.Label(new Rect(x, y, w, h), $"[{point.key}] {point.name} (MISSING)");
            }
            else if (GUI.Button(new Rect(x, y, w, h), $"[{point.key}] {point.name}"))
            {
                TeleportAndReload(point);
            }
            y += h + 2;
        }
    }
}
