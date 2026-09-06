using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MyProject.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private GameplayHUDUI gameplayHUDUI;
        [SerializeField] private GameOverUI gameOverUI;
        [SerializeField] private PauseUI pauseUI;
        [SerializeField] private ShopUI shopUI;
        [SerializeField] private MissionUI missionUI;
        [SerializeField] private RankingUI rankingUI;

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

            EnsureInputModule();
        }

        private void EnsureInputModule()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = Object.FindFirstObjectByType<EventSystem>();
            }

            if (eventSystem != null)
            {
                var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    DestroyImmediate(standaloneModule);
                }

                var inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (inputSystemModule == null)
                {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }

            if (gameplayHUDUI != null)
            {
                var hudImg = gameplayHUDUI.GetComponent<UnityEngine.UI.Image>();
                if (hudImg != null && hudImg.color.a <= 0.01f)
                {
                    hudImg.raycastTarget = false;
                }
            }
        }

        private void Start()
        {
            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnScoreChanged += HandleScoreChanged;
                GameManager.Instance.OnHighScoreChanged += HandleHighScoreChanged;

                // Sync UI with initial state
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }
            else
            {
                Debug.LogWarning("[UIManager] GameManager instance not found in scene.");
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnScoreChanged -= HandleScoreChanged;
                GameManager.Instance.OnHighScoreChanged -= HandleHighScoreChanged;
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            int currentScore = GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0;
            int highScore = GameManager.Instance != null ? GameManager.Instance.HighScore : 0;

            switch (newState)
            {
                case GameState.MainMenu:
                    if (mainMenuUI != null) mainMenuUI.Show(highScore);
                    if (gameplayHUDUI != null) gameplayHUDUI.Hide();
                    if (gameOverUI != null) gameOverUI.Hide();
                    if (pauseUI != null) pauseUI.Hide();
                    if (shopUI != null) shopUI.Hide();
                    if (missionUI != null) missionUI.Hide();
                    if (rankingUI != null) rankingUI.Hide();
                    break;

                case GameState.Playing:
                    if (mainMenuUI != null) mainMenuUI.Hide();
                    if (gameplayHUDUI != null) gameplayHUDUI.Show(currentScore, highScore);
                    if (gameOverUI != null) gameOverUI.Hide();
                    if (pauseUI != null) pauseUI.Hide();
                    if (shopUI != null) shopUI.Hide();
                    if (missionUI != null) missionUI.Hide();
                    if (rankingUI != null) rankingUI.Hide();
                    break;

                case GameState.GameOver:
                    if (mainMenuUI != null) mainMenuUI.Hide();
                    if (gameplayHUDUI != null) gameplayHUDUI.Hide();
                    if (gameOverUI != null) gameOverUI.Show(currentScore, highScore);
                    if (pauseUI != null) pauseUI.Hide();
                    if (shopUI != null) shopUI.Hide();
                    if (missionUI != null) missionUI.Hide();
                    if (rankingUI != null) rankingUI.Hide();
                    break;

                case GameState.Paused:
                    if (pauseUI != null) pauseUI.Show();
                    break;
            }
        }

        public void ShowShop()
        {
            if (mainMenuUI != null) mainMenuUI.Hide();
            if (missionUI != null) missionUI.Hide();
            if (rankingUI != null) rankingUI.Hide();
            if (shopUI != null) shopUI.Show();
        }

        public void HideShop()
        {
            if (shopUI != null) shopUI.Hide();
            if (mainMenuUI != null) mainMenuUI.Show(GameManager.Instance != null ? GameManager.Instance.HighScore : 0);
        }

        public void ShowMissions()
        {
            if (mainMenuUI != null) mainMenuUI.Hide();
            if (shopUI != null) shopUI.Hide();
            if (rankingUI != null) rankingUI.Hide();
            if (missionUI != null) missionUI.Show();
        }

        public void HideMissions()
        {
            if (missionUI != null) missionUI.Hide();
            if (mainMenuUI != null) mainMenuUI.Show(GameManager.Instance != null ? GameManager.Instance.HighScore : 0);
        }

        public void ShowRanking()
        {
            if (mainMenuUI != null) mainMenuUI.Hide();
            if (shopUI != null) shopUI.Hide();
            if (missionUI != null) missionUI.Hide();
            if (rankingUI != null) rankingUI.Show();
        }

        public void HideRanking()
        {
            if (rankingUI != null) rankingUI.Hide();
            if (mainMenuUI != null) mainMenuUI.Show(GameManager.Instance != null ? GameManager.Instance.HighScore : 0);
        }

        private void HandleScoreChanged(int newScore)
        {
            if (gameplayHUDUI != null)
            {
                gameplayHUDUI.UpdateScore(newScore);
            }
        }

        private void HandleHighScoreChanged(int newHighScore)
        {
            if (mainMenuUI != null)
            {
                mainMenuUI.UpdateHighScore(newHighScore);
            }
            if (gameplayHUDUI != null)
            {
                gameplayHUDUI.UpdateHighScore(newHighScore);
            }
            if (shopUI != null)
            {
                shopUI.RefreshUI();
            }
            if (missionUI != null)
            {
                missionUI.RefreshUI();
            }
            if (rankingUI != null)
            {
                rankingUI.RefreshUI();
            }
        }

        public void SetReferences(MainMenuUI mainMenu, GameplayHUDUI hud, GameOverUI gameOver, ShopUI shop = null, MissionUI mission = null, RankingUI ranking = null, PauseUI pause = null)
        {
            mainMenuUI = mainMenu;
            gameplayHUDUI = hud;
            gameOverUI = gameOver;
            shopUI = shop;
            missionUI = mission;
            rankingUI = ranking;
            pauseUI = pause;
        }
    }
}
