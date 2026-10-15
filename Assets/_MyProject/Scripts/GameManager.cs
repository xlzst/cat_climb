using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;
using MyProject.UI;

namespace MyProject
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Revive,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const string HIGH_SCORE_KEY = "CatClimb_HighScore";
        public const string COINS_KEY = "CatClimb_Coins";

        [Header("Settings")]
        [Tooltip("The buffer below the screen bottom where Game Over is triggered.")]
        [SerializeField] private float offScreenBuffer = 0.8f;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        private PlayerController player;
        private Camera mainCamera;
        private bool isGameOver = false;

        private float initialPlayerY = 0f;
        private float highestHeightReached = 0f;
        private int lastAltitudeMilestone = 0;
        private int currentScore = 0;
        private int highScore = 0;
        private int totalCoins = 0;
        private int sessionCoins = 0;
        private bool hasUsedReviveThisRun = false;

        public bool IsGameOver => isGameOver;
        public int CurrentScore => currentScore;
        public int HighScore => highScore;
        public int TotalCoins => totalCoins;
        public int SessionCoins => sessionCoins;
        public bool HasUsedReviveThisRun => hasUsedReviveThisRun;
        public float CurrentAltitude => highestHeightReached;

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnScoreChanged;
        public event Action<int> OnHighScoreChanged;
        public event Action<int> OnCoinsChanged;

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

            // Load persistent high score and total coins
            highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
            totalCoins = PlayerPrefs.GetInt(COINS_KEY, 0);

            // Self-heal core system managers if missing from scene
            if (AudioManager.Instance == null)
            {
                GameObject audioGo = new GameObject("AudioManager");
                audioGo.AddComponent<AudioManager>();
            }
            if (BiomeManager.Instance == null)
            {
                GameObject biomeGo = new GameObject("BiomeManager");
                biomeGo.AddComponent<BiomeManager>();
            }
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
                    lastAltitudeMilestone = 0;
                    sessionCoins = 0;
                    hasUsedReviveThisRun = false;
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
                case GameState.Revive:
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

            hasUsedReviveThisRun = false;
            sessionCoins = 0;
            lastAltitudeMilestone = 0;
            SetState(GameState.Playing);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            sessionCoins += amount;
            totalCoins += amount;
            PlayerPrefs.SetInt(COINS_KEY, totalCoins);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(totalCoins);
            Debug.Log($"[GameManager] Collected {amount} Fish Coins! Session: {sessionCoins}, Total: {totalCoins}");
        }

        public bool TrySpendCoins(int amount)
        {
            if (totalCoins >= amount)
            {
                totalCoins -= amount;
                PlayerPrefs.SetInt(COINS_KEY, totalCoins);
                PlayerPrefs.Save();
                OnCoinsChanged?.Invoke(totalCoins);
                Debug.Log($"[GameManager] Spent {amount} Fish Coins. Remaining: {totalCoins}");
                return true;
            }
            return false;
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

                        // Altitude Milestone feedback every 25 meters (250 score)
                        int currentMilestone = Mathf.FloorToInt(highestHeightReached / 25f);
                        if (currentMilestone > lastAltitudeMilestone && currentMilestone > 0)
                        {
                            lastAltitudeMilestone = currentMilestone;
                            int meters = currentMilestone * 25;
                            string milestoneText = meters switch
                            {
                                25 => "25m REACHED!",
                                50 => "50m SKY HIGH!",
                                75 => "75m CLOUD SURFER!",
                                100 => "100m STRATOSPHERE!",
                                150 => "150m MESOSPHERE!",
                                200 => "200m SPACE CAT!",
                                _ => $"{meters}m CLIMBED!"
                            };
                            UIManager.Instance?.ShowToast(milestoneText, new Color(1f, 0.88f, 0.25f, 1f));
                            CameraFollowTarget.Instance?.AddTrauma(0.18f);
                            AudioManager.Instance?.PlayStreak();
                        }
                    }
                }
            }

            // Check if the player fell below the visible camera screen
            if (player != null && mainCamera != null)
            {
                float cameraBottomY = mainCamera.transform.position.y - mainCamera.orthographicSize;
                if (player.transform.position.y < cameraBottomY - offScreenBuffer)
                {
                    HandlePlayerDefeat();
                }
            }
        }

        public void HandlePlayerDefeat()
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.Revive) return;

            Debug.Log($"[GameManager] HandlePlayerDefeat called. hasUsedReviveThisRun: {hasUsedReviveThisRun}");

            // Offer 1 Revive / Second Chance per run
            if (!hasUsedReviveThisRun)
            {
                hasUsedReviveThisRun = true;
                SetState(GameState.Revive);
            }
            else
            {
                ConfirmGameOver();
            }
        }

        public void RevivePlayer()
        {
            Debug.Log("[GameManager] Reviving player into safe zone!");
            Time.timeScale = 1f;

            if (player == null)
            {
                player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (player != null)
            {
                float camY = (mainCamera != null) ? mainCamera.transform.position.y : player.transform.position.y + 2f;
                Vector3 respawnPos = new Vector3(0f, camY - 0.5f, 0f);
                player.RespawnAt(respawnPos);
            }

            SetState(GameState.Playing);
        }

        public void ConfirmGameOver()
        {
            Time.timeScale = 1f;
            TriggerGameOver();
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

