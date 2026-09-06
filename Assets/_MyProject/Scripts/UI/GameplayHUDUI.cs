using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI
{
    public class GameplayHUDUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Button pauseButton;

        private void Awake()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }
        }

        private void OnPauseClicked()
        {
            Debug.Log("[GameplayHUDUI] Pause button clicked.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }
        }

        public void Show(int currentScore, int highScore)
        {
            if (hudPanel != null)
            {
                hudPanel.SetActive(true);
            }

            UpdateScore(currentScore);
            UpdateHighScore(highScore);
        }

        public void Hide()
        {
            if (hudPanel != null)
            {
                hudPanel.SetActive(false);
            }
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score}";
            }
        }

        public void UpdateHighScore(int highScore)
        {
            if (highScoreText != null)
            {
                highScoreText.text = $"BEST: {highScore}";
            }
        }
    }
}
