using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace FPS
{
    /// <summary>
    /// Configure automatiquement le rendu de l'arme pour éviter le clipping avec l'environnement.
    /// Utilise les Custom Passes HDRP au lieu de deux caméras.
    /// </summary>
    [ExecuteAlways]
    public class WeaponRenderSetup : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Le layer utilisé pour l'arme (doit exister dans les Tags and Layers)")]
        [SerializeField] private string weaponLayerName = "Weapon";
        
        [Tooltip("Appliquer automatiquement le layer à tous les enfants")]
        [SerializeField] private bool applyLayerToChildren = true;

        [Header("Custom Pass Settings")]
        [Tooltip("Point d'injection dans le pipeline de rendu")]
        [SerializeField] private CustomPassInjectionPoint injectionPoint = CustomPassInjectionPoint.BeforePostProcess;

        [Header("References (auto-assignées si vides)")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CustomPassVolume customPassVolume;

        private int weaponLayer = -1;

        private void OnEnable()
        {
            Setup();
        }

        [ContextMenu("Setup Weapon Rendering")]
        public void Setup()
        {
            if (!ValidateLayer())
                return;

            ApplyLayerToWeapon();
            SetupCamera();
            SetupCustomPassVolume();
            
            Debug.Log($"[WeaponRenderSetup] Configuration terminée. Layer: {weaponLayerName}");
        }

        private bool ValidateLayer()
        {
            weaponLayer = LayerMask.NameToLayer(weaponLayerName);
            
            if (weaponLayer == -1)
            {
                Debug.LogError($"[WeaponRenderSetup] Layer '{weaponLayerName}' introuvable ! " +
                    "Créez-le dans Edit > Project Settings > Tags and Layers");
                return false;
            }
            
            return true;
        }

        private void ApplyLayerToWeapon()
        {
            gameObject.layer = weaponLayer;
            
            if (applyLayerToChildren)
            {
                SetLayerRecursively(transform, weaponLayer);
            }
        }

        private void SetLayerRecursively(Transform parent, int layer)
        {
            foreach (Transform child in parent)
            {
                child.gameObject.layer = layer;
                SetLayerRecursively(child, layer);
            }
        }

        private void SetupCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("[WeaponRenderSetup] Caméra principale introuvable.");
                return;
            }

            // Exclure le layer Weapon du culling mask de la caméra
            int weaponLayerMask = 1 << weaponLayer;
            mainCamera.cullingMask &= ~weaponLayerMask;
            
            Debug.Log($"[WeaponRenderSetup] Caméra '{mainCamera.name}' configurée pour exclure le layer {weaponLayerName}");
        }

        private void SetupCustomPassVolume()
        {
            // Chercher ou créer le CustomPassVolume
            if (customPassVolume == null)
            {
                customPassVolume = FindExistingWeaponPassVolume();
            }

            if (customPassVolume == null)
            {
                customPassVolume = CreateCustomPassVolume();
            }

            ConfigureCustomPasses();
        }

        private CustomPassVolume FindExistingWeaponPassVolume()
        {
            // Chercher un volume existant nommé "WeaponRenderVolume"
            var volumes = FindObjectsByType<CustomPassVolume>(FindObjectsSortMode.None);
            foreach (var vol in volumes)
            {
                if (vol.gameObject.name == "WeaponRenderVolume")
                {
                    return vol;
                }
            }
            return null;
        }

        private CustomPassVolume CreateCustomPassVolume()
        {
            GameObject volumeGO = new GameObject("WeaponRenderVolume");
            volumeGO.transform.SetParent(null);
            
            var volume = volumeGO.AddComponent<CustomPassVolume>();
            volume.isGlobal = true;
            volume.injectionPoint = injectionPoint;
            
            Debug.Log("[WeaponRenderSetup] CustomPassVolume créé: WeaponRenderVolume");
            
            return volume;
        }

        private void ConfigureCustomPasses()
        {
            if (customPassVolume == null) return;

            customPassVolume.injectionPoint = injectionPoint;
            
            // Nettoyer les passes existantes
            customPassVolume.customPasses.Clear();

            int weaponLayerMask = 1 << weaponLayer;

            // Pass 1: Clear Depth pour l'arme (rend l'arme "toujours visible")
            var clearDepthPass = new DrawRenderersCustomPass
            {
                name = "WeaponClearDepth",
                targetColorBuffer = CustomPass.TargetBuffer.Camera,
                targetDepthBuffer = CustomPass.TargetBuffer.Camera,
                clearFlags = ClearFlag.Depth,
                layerMask = weaponLayerMask,
                overrideDepthState = true,
                depthCompareFunction = CompareFunction.Always,
                depthWrite = true,
                sortingCriteria = UnityEngine.Rendering.SortingCriteria.CommonOpaque,
                renderQueueType = CustomPass.RenderQueueType.AllOpaque
            };

            // Pass 2: Render normal pour le self-depth correct de l'arme
            var renderPass = new DrawRenderersCustomPass
            {
                name = "WeaponRender",
                targetColorBuffer = CustomPass.TargetBuffer.Camera,
                targetDepthBuffer = CustomPass.TargetBuffer.Camera,
                clearFlags = ClearFlag.None,
                layerMask = weaponLayerMask,
                overrideDepthState = true,
                depthCompareFunction = CompareFunction.LessEqual,
                depthWrite = true,
                sortingCriteria = UnityEngine.Rendering.SortingCriteria.CommonOpaque,
                renderQueueType = CustomPass.RenderQueueType.AllOpaque
            };

            customPassVolume.customPasses.Add(clearDepthPass);
            customPassVolume.customPasses.Add(renderPass);

            Debug.Log("[WeaponRenderSetup] Custom Passes configurées avec succès");
        }

        private void OnValidate()
        {
            // Réappliquer le setup quand les valeurs changent dans l'Inspector
            if (Application.isPlaying || !gameObject.activeInHierarchy)
                return;
                
            Setup();
        }

#if UNITY_EDITOR
        [ContextMenu("Reset Camera Culling Mask")]
        private void ResetCameraCullingMask()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            if (mainCamera != null)
            {
                mainCamera.cullingMask = -1; // Everything
                Debug.Log("[WeaponRenderSetup] Culling mask réinitialisé à Everything");
            }
        }

        [ContextMenu("Delete Custom Pass Volume")]
        private void DeleteCustomPassVolume()
        {
            var volume = FindExistingWeaponPassVolume();
            if (volume != null)
            {
                DestroyImmediate(volume.gameObject);
                customPassVolume = null;
                Debug.Log("[WeaponRenderSetup] WeaponRenderVolume supprimé");
            }
        }
#endif
    }
}
