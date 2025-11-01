using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPS
{
    /// <summary>
    /// Gestionnaire principal du jeu (pause, game over, restart).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game State")]
        [SerializeField] private bool isPaused = false;
        [SerializeField] private bool isGameOver = false;
        
        [Header("UI Panels")]
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject gameOverMenu;
        
        private PlayerHealth playerHealth;
        
        private void Awake()
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.OnDeath.AddListener(OnPlayerDeath);
            }
            
            // Cacher les menus
            if (pauseMenu != null) pauseMenu.SetActive(false);
            if (gameOverMenu != null) gameOverMenu.SetActive(false);
        }
        
        private void Update()
        {
            // Gérer la pause avec Échap
            if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }
        
        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
            
            if (pauseMenu != null)
                pauseMenu.SetActive(true);
            
            // Déverrouiller le curseur
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            
            if (pauseMenu != null)
                pauseMenu.SetActive(false);
            
            // Verrouiller le curseur
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void OnPlayerDeath()
        {
            isGameOver = true;
            
            if (gameOverMenu != null)
                gameOverMenu.SetActive(true);
            
            // Déverrouiller le curseur
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log("Game Over!");
        }
        
        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        
        public void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        // Propriétés publiques
        public bool IsPaused => isPaused;
        public bool IsGameOver => isGameOver;
    }
}
