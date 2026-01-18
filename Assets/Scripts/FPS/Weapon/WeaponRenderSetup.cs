using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

#if HDRP_OUTLINE
using EPOOutline;
#endif

namespace FPS
{
    /// <summary>
    /// Configure automatiquement le rendu de l'arme pour éviter le clipping avec l'environnement.
    /// Configure aussi Easy Performant Outline pour les outlines d'ennemis.
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
        [Tooltip("Point d'injection - AfterOpaqueAndSky évite les conflits avec Easy Performant Outline")]
        [SerializeField] private CustomPassInjectionPoint injectionPoint = CustomPassInjectionPoint.AfterOpaqueAndSky;

        [Header("Easy Performant Outline")]
        [Tooltip("Configurer automatiquement Easy Performant Outline")]
        [SerializeField] private bool setupOutline = true;

        [Header("References (auto-assignées si vides)")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CustomPassVolume weaponPassVolume;

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
            SetupWeaponCustomPassVolume();
            
            if (setupOutline)
            {
                SetupEasyPerformantOutline();
            }
            
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

        private void SetupWeaponCustomPassVolume()
        {
            if (weaponPassVolume == null)
            {
                weaponPassVolume = FindVolumeByName("WeaponRenderVolume");
            }

            if (weaponPassVolume == null)
            {
                weaponPassVolume = CreateCustomPassVolume("WeaponRenderVolume", injectionPoint);
            }

            ConfigureWeaponPasses();
        }

        private CustomPassVolume FindVolumeByName(string volumeName)
        {
            var volumes = FindObjectsByType<CustomPassVolume>(FindObjectsSortMode.None);
            foreach (var vol in volumes)
            {
                if (vol.gameObject.name == volumeName)
                {
                    return vol;
                }
            }
            return null;
        }

        private CustomPassVolume CreateCustomPassVolume(string volumeName, CustomPassInjectionPoint injection)
        {
            GameObject volumeGO = new GameObject(volumeName);
            volumeGO.transform.SetParent(null);
            
            var volume = volumeGO.AddComponent<CustomPassVolume>();
            volume.isGlobal = true;
            volume.injectionPoint = injection;
            
            Debug.Log($"[WeaponRenderSetup] CustomPassVolume créé: {volumeName}");
            
            return volume;
        }

        private void ConfigureWeaponPasses()
        {
            if (weaponPassVolume == null) return;

            weaponPassVolume.injectionPoint = injectionPoint;
            weaponPassVolume.customPasses.Clear();

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

            weaponPassVolume.customPasses.Add(clearDepthPass);
            weaponPassVolume.customPasses.Add(renderPass);

            Debug.Log("[WeaponRenderSetup] Custom Passes pour l'arme configurées");
        }

        /// <summary>
        /// Configure Easy Performant Outline: CustomPassVolume + Outliner sur la caméra
        /// </summary>
        private void SetupEasyPerformantOutline()
        {
#if HDRP_OUTLINE
            // 1. Chercher ou créer le CustomPassVolume pour l'outline
            var outlineVolume = FindOutlineCustomPassVolume();
            
            if (outlineVolume == null)
            {
                outlineVolume = CreateCustomPassVolume("OutlineCustomPassVolume", CustomPassInjectionPoint.BeforePostProcess);
            }

            // 2. Vérifier si le volume a déjà un OutlineCustomPass
            bool hasOutlinePass = false;
            foreach (var pass in outlineVolume.customPasses)
            {
                if (pass is OutlineCustomPass)
                {
                    hasOutlinePass = true;
                    break;
                }
            }

            if (!hasOutlinePass)
            {
                outlineVolume.AddPassOfType(typeof(OutlineCustomPass));
                Debug.Log("[WeaponRenderSetup] OutlineCustomPass ajouté au volume");
            }

            // 3. Ajouter Outliner à la caméra si nécessaire
            if (mainCamera != null)
            {
                var outliner = mainCamera.GetComponent<Outliner>();
                if (outliner == null)
                {
                    outliner = mainCamera.gameObject.AddComponent<Outliner>();
                    Debug.Log("[WeaponRenderSetup] Outliner ajouté à la caméra");
                }
            }

            Debug.Log("[WeaponRenderSetup] Easy Performant Outline configuré");
#else
            Debug.LogWarning("[WeaponRenderSetup] HDRP_OUTLINE n'est pas défini. " +
                "Lancez Tools > Easy performant outline > Setup pour configurer EPO.");
#endif
        }

#if HDRP_OUTLINE
        private CustomPassVolume FindOutlineCustomPassVolume()
        {
            var volumes = FindObjectsByType<CustomPassVolume>(FindObjectsSortMode.None);
            foreach (var vol in volumes)
            {
                // Chercher un volume qui contient déjà un OutlineCustomPass
                foreach (var pass in vol.customPasses)
                {
                    if (pass is OutlineCustomPass)
                    {
                        return vol;
                    }
                }
                
                // Ou un volume nommé pour l'outline
                if (vol.gameObject.name == "OutlineCustomPassVolume" || 
                    vol.gameObject.name == "Custom volume")
                {
                    return vol;
                }
            }
            return null;
        }
#endif

        private void OnValidate()
        {
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

        [ContextMenu("Delete Custom Pass Volumes")]
        private void DeleteCustomPassVolumes()
        {
            var weaponVol = FindVolumeByName("WeaponRenderVolume");
            if (weaponVol != null)
            {
                DestroyImmediate(weaponVol.gameObject);
                weaponPassVolume = null;
                Debug.Log("[WeaponRenderSetup] WeaponRenderVolume supprimé");
            }
        }

        [ContextMenu("Force Setup Easy Performant Outline")]
        private void ForceSetupOutline()
        {
            setupOutline = true;
            SetupEasyPerformantOutline();
        }
#endif
    }
}

