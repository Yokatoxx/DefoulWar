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
        
        private PlayerHealth playerHealth;
        
        private void Awake()
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.OnDeath.AddListener(OnPlayerDeath);
            }
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
            UIManager.Instance?.SetPauseMenu(true);
        }
        
        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            UIManager.Instance?.SetPauseMenu(false);
        }
        
        private void OnPlayerDeath()
        {
            isGameOver = true;
            UIManager.Instance?.ShowGameOverMenu();
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
