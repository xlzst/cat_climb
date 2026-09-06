using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Text restartButtonText;

        private void Awake()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        public void Show(int finalScore, int highScore)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = $"FINAL SCORE\n{finalScore}";
            }

            if (highScoreText != null)
            {
                highScoreText.text = $"BEST RECORD: {highScore}";
            }
        }

        public void Hide()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void OnRestartClicked()
        {
            Debug.Log("[GameOverUI] Restart button clicked.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
    }
}
