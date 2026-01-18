using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace FPS
{
    /// <summary>
    /// Fix pour HDRP: force un refresh du culling mask de la caméra au démarrage.
    /// Résout le problème où la caméra overlay (arme) ne s'affiche pas correctement.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class HDRPCameraLayerFix : MonoBehaviour
    {
        private Camera cam;
        
        private void Awake()
        {
            cam = GetComponent<Camera>();
        }
        
        private void Start()
        {
            // Forcer un refresh du culling mask en le toggleant
            StartCoroutine(RefreshCullingMask());
        }
        
        private System.Collections.IEnumerator RefreshCullingMask()
        {
            // Attendre une frame pour que HDRP soit initialisé
            yield return null;
            
            // Sauvegarder le culling mask actuel
            int originalMask = cam.cullingMask;
            
            // Toggle off puis on (force HDRP à recalculer)
            cam.cullingMask = 0;
            yield return null;
            cam.cullingMask = originalMask;
            
            // Optionnel: forcer un reset des données HDRP de la caméra
            var hdData = cam.GetComponent<HDAdditionalCameraData>();
            if (hdData != null)
            {
                hdData.enabled = false;
                yield return null;
                hdData.enabled = true;
            }
        }
    }
}
