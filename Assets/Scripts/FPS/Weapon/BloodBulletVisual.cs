using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Active/désactive un GameObject contenant les visuels du mode Blood Bullet sur l'arme.
    /// </summary>
    public class BloodBulletVisual : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("L'objet à activer quand le mode Blood Bullet est actif")]
        [SerializeField] private GameObject bloodBulletVisualRoot;
        
        [Tooltip("Référence au WeaponSystem (auto-trouvé si null)")]
        [SerializeField] private WeaponSystem weaponSystem;

        private void Awake()
        {
            if (weaponSystem == null)
                weaponSystem = GetComponentInParent<WeaponSystem>();
            
            // S'assurer que le visuel est désactivé au départ
            if (bloodBulletVisualRoot != null)
                bloodBulletVisualRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (weaponSystem != null)
            {
                weaponSystem.OnBloodBulletModeChanged.AddListener(OnBloodBulletModeChanged);
                
                // Synchroniser l'état initial
                OnBloodBulletModeChanged(weaponSystem.IsUsingBloodBullets);
            }
        }

        private void OnDisable()
        {
            if (weaponSystem != null)
            {
                weaponSystem.OnBloodBulletModeChanged.RemoveListener(OnBloodBulletModeChanged);
            }
        }

        private void OnBloodBulletModeChanged(bool isActive)
        {
            if (bloodBulletVisualRoot != null)
            {
                bloodBulletVisualRoot.SetActive(isActive);
            }
        }
    }
}
