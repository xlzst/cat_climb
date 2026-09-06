using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Button playButton;
        [SerializeField] private Text playButtonText;
        [SerializeField] private Button skinsButton;
        [SerializeField] private Button missionButton;
        [SerializeField] private Button rankingButton;

        [Header("Animation Settings")]
        [SerializeField] private bool pulsePlayButton = true;
        [SerializeField] private float pulseSpeed = 3.0f;
        [SerializeField] private float pulseAmount = 0.08f;

        private Vector3 originalButtonScale = Vector3.one;

        private void Awake()
        {
            if (playButton != null)
            {
                originalButtonScale = playButton.transform.localScale;
                playButton.onClick.AddListener(OnPlayClicked);
            }
            if (skinsButton != null)
            {
                skinsButton.onClick.AddListener(OnSkinsClicked);
            }
            if (missionButton != null)
            {
                missionButton.onClick.AddListener(OnMissionClicked);
            }
            if (rankingButton != null)
            {
                rankingButton.onClick.AddListener(OnRankingClicked);
            }
        }

        private void Update()
        {
            // Subtle pulsing animation on play button for high visual feedback
            if (pulsePlayButton && playButton != null && menuPanel != null && menuPanel.activeSelf)
            {
                float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                playButton.transform.localScale = originalButtonScale * (1f + scaleOffset);
            }
        }

        public void Show(int highScore)
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
            }

            UpdateHighScore(highScore);
        }

        public void Hide()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
        }

        public void UpdateHighScore(int highScore)
        {
            if (highScoreText != null)
            {
                highScoreText.text = $"BEST RECORD: {highScore} FT";
            }
        }

        private void OnPlayClicked()
        {
            Debug.Log("[MainMenuUI] Play button pressed. Starting gameplay...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        private void OnSkinsClicked()
        {
            Debug.Log("[MainMenuUI] Skins button pressed. Opening Shop...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowShop();
            }
        }

        private void OnMissionClicked()
        {
            Debug.Log("[MainMenuUI] Mission button pressed. Opening Missions...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMissions();
            }
        }

        private void OnRankingClicked()
        {
            Debug.Log("[MainMenuUI] Ranking button pressed. Opening Leaderboard...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowRanking();
            }
        }
    }
}
