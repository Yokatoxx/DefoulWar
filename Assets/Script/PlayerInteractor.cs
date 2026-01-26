using UnityEngine;

/// <summary>
/// PlayerInteractor: écoute l'événement OnInteract du PlayerInputHandler
/// et effectue un raycast depuis la caméra principale pour invoquer IInteractable.Interact.
/// Attacher ce composant au joueur ou à un GameObject de la scène.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("Distance max du raycast pour détecter un interactable")]
    public float MaxDistance = 3f;

    [Tooltip("Camera à utiliser pour le raycast (si null, Camera.main sera utilisée)")]
    public Camera RaycastCamera;

    [Tooltip("LayerMask pour limiter le raycast (par défaut Everything)")]
    public LayerMask HitLayers = ~0;

    PlayerInputHandler _inputHandler;

    void Awake()
    {
        if (RaycastCamera == null)
            RaycastCamera = Camera.main;

        // Try find input handler in scene
        _inputHandler = FindFirstObjectByType<PlayerInputHandler>();
        if (_inputHandler != null)
        {
            _inputHandler.OnInteract.AddListener(OnInteract);
            Debug.Log("PlayerInteractor: abonné à OnInteract");
        }
        else
        {
            Debug.LogWarning("PlayerInteractor: PlayerInputHandler non trouvé dans la scène!");
        }
    }

    void OnDestroy()
    {
        if (_inputHandler != null)
            _inputHandler.OnInteract.RemoveListener(OnInteract);
    }

    void OnInteract()
    {
        Debug.Log("PlayerInteractor: OnInteract invoqué");
        if (RaycastCamera == null)
        {
            RaycastCamera = Camera.main;
            if (RaycastCamera == null)
            {
                Debug.LogWarning("PlayerInteractor: aucune caméra assignée et Camera.main est null.");
                return;
            }
        }

        // Raycast depuis le centre de l'écran
        Ray ray = RaycastCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        
        Debug.DrawRay(ray.origin, ray.direction * MaxDistance, Color.green, 1f);

        if (Physics.Raycast(ray, out var hit, MaxDistance, HitLayers))
        {
            Debug.Log($"PlayerInteractor: Raycast hit {hit.collider.gameObject.name}");
            
            // Chercher IInteractable sur l'objet touché
            var interactable = hit.collider.GetComponent<Script.IInteractable>();
            if (interactable != null)
            {
                Debug.Log("PlayerInteractor: Interactable trouvé, appel de Interact()");
                interactable.Interact(gameObject);
                return;
            }

            // Chercher dans les parents
            var parent = hit.collider.transform.parent;
            while (parent != null)
            {
                interactable = parent.GetComponent<Script.IInteractable>();
                if (interactable != null)
                {
                    Debug.Log("PlayerInteractor: Interactable trouvé sur parent, appel de Interact()");
                    interactable.Interact(gameObject);
                    return;
                }
                parent = parent.parent;
            }
            
            Debug.Log("PlayerInteractor: objet touché mais pas d'IInteractable trouvé");
        }
        else
        {
            // Fallback: spherecast
            float sphereRadius = 0.5f;
            if (Physics.SphereCast(ray, sphereRadius, out hit, MaxDistance, HitLayers))
            {
                Debug.Log($"PlayerInteractor: SphereCast hit {hit.collider.gameObject.name}");
                
                var interactable = hit.collider.GetComponent<Script.IInteractable>();
                if (interactable != null)
                {
                    Debug.Log("PlayerInteractor: Interactable trouvé via spherecast");
                    interactable.Interact(gameObject);
                    return;
                }

                var parent = hit.collider.transform.parent;
                while (parent != null)
                {
                    interactable = parent.GetComponent<Script.IInteractable>();
                    if (interactable != null)
                    {
                        Debug.Log("PlayerInteractor: Interactable trouvé sur parent via spherecast");
                        interactable.Interact(gameObject);
                        return;
                    }
                    parent = parent.parent;
                }
            }
            else
            {
                Debug.Log("PlayerInteractor: aucun objet détecté");
            }
        }
    }
}
