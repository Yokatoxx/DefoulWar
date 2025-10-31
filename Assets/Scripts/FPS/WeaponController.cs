using FPS;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Système d'entrée et de recul pour arme: délègue les tirs à ProjectileWeaponController (pas de hitscan).
    /// </summary>
    [RequireComponent(typeof(ProjectileWeaponController))]
    public class WeaponController : MonoBehaviour
    {
        [Header("Recoil")]
        [SerializeField] private float recoilAmount = 0.5f;
        [SerializeField] private float recoilRecoverySpeed = 5f;
        
        [Header("References")]
        [SerializeField] private Transform weaponModel;
        [SerializeField] private Transform cameraTransform;
        
        [Header("Input Mode")]
        [SerializeField] private bool useNewInputSystem; // valeur par défaut gérée par l'Inspector
        
        private bool isFiring;
        private Vector3 currentRecoil;
        private ProjectileWeaponController projectileCtrl;
        private PlayerStunAutoFire stunController;
        
        private void Awake()
        {
            // Récupérer le contrôleur à projectiles (source de vérité pour tirs/dégâts)
            projectileCtrl = GetComponent<ProjectileWeaponController>();
            
            // Trouver la caméra si non assignée
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
            }
            
            // Chercher le contrôleur de stun
            stunController = GetComponentInParent<PlayerStunAutoFire>();
        }
        
        private void Update()
        {
            if (!useNewInputSystem)
            {
                HandleOldInput();
            }
            
            // Récupération du recul visuel
            if (currentRecoil.magnitude > 0.01f)
            {
                currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);
                if (cameraTransform != null)
                {
                    cameraTransform.localRotation = Quaternion.Euler(-currentRecoil.x, currentRecoil.y, 0);
                }
            }
            
            // Bloquer l'entrée de tir si étourdi
            bool isStunned = stunController != null && stunController.IsStunned;
            if (isStunned)
            {
                isFiring = false;
            }
            
            // Tir continu si appui maintenu
            if (isFiring)
            {
                TryShoot();
            }
        }
        
        private void HandleOldInput()
        {
            // Désactiver la capture de tir si étourdi
            if (stunController != null && stunController.IsStunned)
            {
                isFiring = false;
            }
            else
            {
                isFiring = Input.GetButton("Fire1");
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartReload();
            }
        }
        
        private void TryShoot()
        {
            // Déléguer le tir au contrôleur de projectiles; aucun raycast ici
            if (projectileCtrl == null) return;
            bool shot = projectileCtrl.ExternalTryShoot();
            if (shot)
            {
                // Appliquer un recul visuel simple uniquement si un projectile a été tiré
                float randomX = Random.Range(-recoilAmount, recoilAmount);
                float randomY = Random.Range(-recoilAmount, recoilAmount);
                currentRecoil += new Vector3(recoilAmount, randomY, randomX);
            }
        }
        
        public void StartReload()
        {
            if (projectileCtrl != null)
            {
                projectileCtrl.StartReload();
            }
        }
        
        // Input System callbacks (optionnel)
        public void OnFire(InputAction.CallbackContext context)
        {
            if (useNewInputSystem)
            {
                // Si étourdi, ignorer les événements d'entrée
                if (stunController != null && stunController.IsStunned)
                {
                    isFiring = false;
                    return;
                }
                
                if (context.started)
                {
                    isFiring = true;
                }
                else if (context.canceled)
                {
                    isFiring = false;
                }
            }
        }
        
        public void OnReload(InputAction.CallbackContext context)
        {
            if (useNewInputSystem && context.performed)
            {
                StartReload();
            }
        }
        
        // Propriétés publiques (redirigées vers le contrôleur à projectiles)
        public int CurrentAmmo => projectileCtrl != null ? projectileCtrl.CurrentAmmo : 0;
        public int MaxAmmo => projectileCtrl != null ? projectileCtrl.MaxAmmo : 0;
        public bool IsReloading => projectileCtrl != null && projectileCtrl.IsReloading;
        public Transform WeaponModel => weaponModel;
    }
}