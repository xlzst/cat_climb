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
        [SerializeField] private Button settingsButton;
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

            EnsureSettingsButton();
        }

        private void EnsureSettingsButton()
        {
            if (pausePanel == null) return;

            Transform card = pausePanel.transform.Find("PauseCard");
            Transform parentTransform = card != null ? card : pausePanel.transform;

            if (settingsButton == null)
            {
                Transform existing = parentTransform.Find("PauseSettingsButton");
                if (existing != null)
                {
                    settingsButton = existing.GetComponent<Button>();
                }
                else
                {
                    // Adjust card and existing buttons slightly to fit Continue, Settings, and Home cleanly
                    RectTransform cardRt = card != null ? card.GetComponent<RectTransform>() : null;
                    if (cardRt != null && cardRt.sizeDelta.y < 600f)
                    {
                        cardRt.sizeDelta = new Vector2(cardRt.sizeDelta.x, 620f);
                    }

                    if (continueButton != null)
                    {
                        RectTransform contRt = continueButton.GetComponent<RectTransform>();
                        if (contRt != null)
                        {
                            contRt.anchorMin = new Vector2(0.5f, 0.56f);
                            contRt.anchorMax = new Vector2(0.5f, 0.56f);
                        }
                    }

                    if (homeButton != null)
                    {
                        RectTransform homeRt = homeButton.GetComponent<RectTransform>();
                        if (homeRt != null)
                        {
                            homeRt.anchorMin = new Vector2(0.5f, 0.18f);
                            homeRt.anchorMax = new Vector2(0.5f, 0.18f);
                        }
                    }

                    GameObject btnGo = new GameObject("PauseSettingsButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ButtonPressFeedback));
                    btnGo.transform.SetParent(parentTransform, false);
                    RectTransform rt = btnGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.37f);
                    rt.anchorMax = new Vector2(0.5f, 0.37f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(460f, 82f);

                    Image img = btnGo.GetComponent<Image>();
                    img.color = new Color(0.24f, 0.32f, 0.50f, 1f);

                    settingsButton = btnGo.GetComponent<Button>();
                    settingsButton.targetGraphic = img;

                    GameObject lblGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    lblGo.transform.SetParent(btnGo.transform, false);
                    RectTransform lblRt = lblGo.GetComponent<RectTransform>();
                    lblRt.anchorMin = Vector2.zero;
                    lblRt.anchorMax = Vector2.one;
                    lblRt.sizeDelta = Vector2.zero;

                    Text txt = lblGo.GetComponent<Text>();
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 28);
                    txt.text = "SETTINGS";
                    txt.fontSize = 28;
                    txt.fontStyle = FontStyle.Bold;
                    txt.color = Color.white;
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.raycastTarget = false;
                }
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }
        }

        public void Show()
        {
            EnsureSettingsButton();
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
            AudioManager.Instance?.PlayClick();
            Debug.Log("[PauseUI] Continue button clicked. Resuming game...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        private void OnSettingsClicked()
        {
            AudioManager.Instance?.PlayClick();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowSettings();
            }
            else
            {
                SettingsUI.Instance?.Show();
            }
        }

        private void OnHomeClicked()
        {
            AudioManager.Instance?.PlayClick();
            Debug.Log("[PauseUI] Home button clicked. Returning to main menu...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }
    }
}
