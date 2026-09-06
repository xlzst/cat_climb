using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI
{
    public class PauseUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button homeButton;

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            if (homeButton != null)
            {
                homeButton.onClick.AddListener(OnHomeClicked);
            }
        }

        public void Show()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        private void OnContinueClicked()
        {
            Debug.Log("[PauseUI] Continue button clicked. Resuming game...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        private void OnHomeClicked()
        {
            Debug.Log("[PauseUI] Home button clicked. Returning to main menu...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }
    }
}
