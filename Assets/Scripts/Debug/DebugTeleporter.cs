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

    [System.Serializable]
    public class SceneEntry
    {
        public string sceneName;
        public KeyCode key = KeyCode.Alpha1;
    }

    [System.Serializable]
    public class GameObjectToggle
    {
        public string name = "Object";
        public KeyCode key = KeyCode.G;
        public GameObject target;
    }

    [Header("Teleport Points")]
    [SerializeField] private TeleportPoint[] teleportPoints;

    [Header("Scene Switching")]
    [SerializeField] private SceneEntry[] scenes;
    [SerializeField] private KeyCode sceneMenuKey = KeyCode.F10;
    private bool showSceneMenu = false;

    [Header("GameObject Toggles")]
    [SerializeField] private GameObjectToggle[] gameObjectToggles;
    [SerializeField] private KeyCode toggleMenuKey = KeyCode.F11;
    private bool showToggleMenu = false;

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

        // Réinitialiser les effets caméra pour éviter le décalage du headbob
        var cameraEffects = player.GetComponentInChildren<FPS.FPSCameraEffects>();
        if (cameraEffects != null)
        {
            cameraEffects.ResetDefaultPosition();
        }

        Debug.Log($"[DebugTeleporter] Téléporté à {pendingTeleportPosition}");
        hasPendingTeleport = false;
    }

    private void Update()
    {
        // Toggle du menu de scènes
        if (Input.GetKeyDown(sceneMenuKey))
        {
            showSceneMenu = !showSceneMenu;
        }

        // Toggle du menu de GameObjects
        if (Input.GetKeyDown(toggleMenuKey))
        {
            showToggleMenu = !showToggleMenu;
        }

        // Téléportation
        if (teleportPoints != null)
        {
            foreach (var point in teleportPoints)
            {
                if (point.destination != null && Input.GetKeyDown(point.key))
                {
                    TeleportAndReload(point);
                    return;
                }
            }
        }

        // Changement de scène par touche
        if (scenes != null)
        {
            foreach (var scene in scenes)
            {
                if (!string.IsNullOrEmpty(scene.sceneName) && Input.GetKeyDown(scene.key))
                {
                    LoadScene(scene.sceneName);
                    return;
                }
            }
        }

        // Toggle des GameObjects par touche
        if (gameObjectToggles != null)
        {
            foreach (var toggle in gameObjectToggles)
            {
                if (toggle.target != null && Input.GetKeyDown(toggle.key))
                {
                    ToggleGameObject(toggle);
                }
            }
        }
    }

    private void LoadScene(string sceneName)
    {
        Debug.Log($"[DebugTeleporter] Chargement de la scène: {sceneName}");
        hasPendingTeleport = false;
        SceneManager.LoadScene(sceneName);
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

    private void ToggleGameObject(GameObjectToggle toggle)
    {
        bool newState = !toggle.target.activeSelf;
        toggle.target.SetActive(newState);
        Debug.Log($"[DebugTeleporter] {toggle.name} -> {(newState ? "ON" : "OFF")}");
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;

        const int w = 220;
        const int h = 24;
        int x = 10;
        int y = 10;

        // Titre principal
        GUI.Label(new Rect(x, y, w, h), "<b>Debug Teleporter</b>");
        y += h + 5;

        // Points de téléportation
        if (teleportPoints != null)
        {
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

        // Séparateur et menu de scènes
        y += 10;
        if (GUI.Button(new Rect(x, y, w, h), $"[{sceneMenuKey}] Scènes {(showSceneMenu ? "▼" : "▶")}"))
        {
            showSceneMenu = !showSceneMenu;
        }
        y += h + 2;

        if (showSceneMenu && scenes != null)
        {
            foreach (var scene in scenes)
            {
                if (string.IsNullOrEmpty(scene.sceneName)) continue;
                
                bool isCurrentScene = SceneManager.GetActiveScene().name == scene.sceneName;
                string label = isCurrentScene ? $"[{scene.key}] ► {scene.sceneName}" : $"[{scene.key}] {scene.sceneName}";
                
                if (GUI.Button(new Rect(x + 10, y, w - 10, h), label))
                {
                    LoadScene(scene.sceneName);
                }
                y += h + 2;
            }
        }

        // Menu de toggle GameObject
        y += 10;
        if (GUI.Button(new Rect(x, y, w, h), $"[{toggleMenuKey}] Toggles {(showToggleMenu ? "▼" : "▶")}"))
        {
            showToggleMenu = !showToggleMenu;
        }
        y += h + 2;

        if (showToggleMenu && gameObjectToggles != null)
        {
            foreach (var toggle in gameObjectToggles)
            {
                if (toggle.target == null)
                {
                    GUI.Label(new Rect(x + 10, y, w - 10, h), $"[{toggle.key}] {toggle.name} (MISSING)");
                }
                else
                {
                    string state = toggle.target.activeSelf ? "ON" : "OFF";
                    if (GUI.Button(new Rect(x + 10, y, w - 10, h), $"[{toggle.key}] {toggle.name} [{state}]"))
                    {
                        ToggleGameObject(toggle);
                    }
                }
                y += h + 2;
            }
        }
    }
}
