using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Gère tous les aspects de l'interface utilisateur (menus, curseur, etc.).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // --------------------
        // Singleton
        // --------------------
        private static UIManager instance;
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<UIManager>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("Multiple UIManager instances found. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        [Header("UI Panels")]
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject gameOverMenu;

        [Header("HUD")]
        [SerializeField] private TMPro.TextMeshProUGUI speedDisplay;

        private void Start()
        {
            // Initialiser l'état de l'UI
            if (pauseMenu != null) pauseMenu.SetActive(false);
            if (gameOverMenu != null) gameOverMenu.SetActive(false);

            // Verrouiller le curseur au démarrage
            SetCursorLocked(true);
        }

        /// <summary>
        /// Affiche ou masque le menu de pause.
        /// </summary>
        public void SetPauseMenu(bool isVisible)
        {
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(isVisible);
            }
            SetCursorLocked(!isVisible);
        }

        /// <summary>
        /// Affiche le menu de game over.
        /// </summary>
        public void ShowGameOverMenu()
        {
            if (gameOverMenu != null)
            {
                gameOverMenu.SetActive(true);
            }
            SetCursorLocked(false);
        }

        /// <summary>
        /// Gère l'état du curseur (verrouillé ou non).
        /// </summary>
        /// <param name="isLocked">True pour verrouiller, false pour libérer.</param>
        public void SetCursorLocked(bool isLocked)
        {
            if (isLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        /// <summary>
        /// Met à jour l'affichage de la vitesse du joueur.
        /// </summary>
        public void UpdateSpeedDisplay(float speed)
        {
            if (speedDisplay != null)
            {
                speedDisplay.text = "Speed: " + Mathf.RoundToInt(speed).ToString();
            }
        }
    }
}
