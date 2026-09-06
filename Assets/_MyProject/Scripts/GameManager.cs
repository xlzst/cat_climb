using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;

namespace MyProject
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const string HIGH_SCORE_KEY = "CatClimb_HighScore";

        [Header("Settings")]
        [Tooltip("The buffer below the screen bottom where Game Over is triggered.")]
        [SerializeField] private float offScreenBuffer = 0.8f;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        private PlayerController player;
        private Camera mainCamera;
        private bool isGameOver = false;

        private float initialPlayerY = 0f;
        private float highestHeightReached = 0f;
        private int currentScore = 0;
        private int highScore = 0;

        public bool IsGameOver => isGameOver;
        public int CurrentScore => currentScore;
        public int HighScore => highScore;

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnScoreChanged;
        public event Action<int> OnHighScoreChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Load persistent high score
            highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        }

        private void Start()
        {
            // Find references automatically to ensure scene setup works cleanly
            player = FindFirstObjectByType<PlayerController>();
            mainCamera = Camera.main;

            if (player == null)
            {
                Debug.LogWarning("[GameManager] PlayerController not found in the scene.");
            }
            else
            {
                initialPlayerY = player.transform.position.y;
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("[GameManager] Main Camera not found in the scene.");
            }

            // Initial state is MainMenu
            SetState(GameState.MainMenu);
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;

            switch (CurrentState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    isGameOver = false;
                    currentScore = 0;
                    highestHeightReached = 0f;
                    if (player != null)
                    {
                        player.enabled = true;
                    }
                    break;

                case GameState.Playing:
                    Time.timeScale = 1f;
                    isGameOver = false;
                    if (player != null)
                    {
                        player.enabled = true;
                        initialPlayerY = player.transform.position.y;
                    }
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                case GameState.GameOver:
                    Time.timeScale = 1f;
                    isGameOver = true;
                    if (player != null)
                    {
                        player.enabled = false;
                        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            rb.linearVelocity = Vector2.zero;
                            rb.bodyType = RigidbodyType2D.Static;
                        }
                    }
                    break;
            }

            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void StartGame()
        {
            int totalRuns = PlayerPrefs.GetInt("CatClimb_TotalRuns", 0);
            PlayerPrefs.SetInt("CatClimb_TotalRuns", totalRuns + 1);
            PlayerPrefs.Save();

            SetState(GameState.Playing);
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                Debug.Log("[GameManager] Game Paused.");
                SetState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                Debug.Log("[GameManager] Game Resumed.");
                SetState(GameState.Playing);
            }
        }

        public void ReturnToMainMenu()
        {
            Debug.Log("[GameManager] Returning to Main Menu...");
            Time.timeScale = 1f;
            RestartGame();
        }

        private void Update()
        {
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
            {
                if (CurrentState == GameState.Playing)
                {
                    PauseGame();
                    return;
                }
                else if (CurrentState == GameState.Paused)
                {
                    ResumeGame();
                    return;
                }
            }

            if (CurrentState == GameState.GameOver)
            {
                HandleRestartInput();
                return;
            }

            if (CurrentState != GameState.Playing)
            {
                return;
            }

            // Update score based on highest Y position reached
            if (player != null)
            {
                float heightDiff = player.transform.position.y - initialPlayerY;
                if (heightDiff > highestHeightReached)
                {
                    highestHeightReached = heightDiff;
                    int newScore = Mathf.FloorToInt(highestHeightReached * 10f); // 10 points per unit of height
                    if (newScore != currentScore)
                    {
                        currentScore = newScore;
                        OnScoreChanged?.Invoke(currentScore);
                    }
                }
            }

            // Check if the player fell below the visible camera screen
            if (player != null && mainCamera != null)
            {
                float cameraBottomY = mainCamera.transform.position.y - mainCamera.orthographicSize;
                if (player.transform.position.y < cameraBottomY - offScreenBuffer)
                {
                    TriggerGameOver();
                }
            }
        }

        public void TriggerGameOver()
        {
            if (CurrentState == GameState.GameOver) return;

            // Check and update High Score
            if (currentScore > highScore)
            {
                highScore = currentScore;
                PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
                PlayerPrefs.Save();
                OnHighScoreChanged?.Invoke(highScore);
            }

            Debug.Log($"[GameManager] Game Over! Final Score: {currentScore}, High Score: {highScore}");
            SetState(GameState.GameOver);
        }

        private void HandleRestartInput()
        {
            bool restartPressed = false;

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                restartPressed = true;
            }

            if (restartPressed)
            {
                RestartGame();
            }
        }

        public void RestartGame()
        {
            Debug.Log("[GameManager] Restarting game...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}

