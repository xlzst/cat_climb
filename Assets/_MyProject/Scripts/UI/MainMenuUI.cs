using UnityEngine;
using UnityEngine.UI;
using MyProject;

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
        [SerializeField] private Button soundButton;
        [SerializeField] private Text soundButtonText;

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

            EnsureSoundButton();
            UpdateSoundButtonVisual();
        }

        private void EnsureSoundButton()
        {
            if (soundButton == null && menuPanel != null)
            {
                Transform existing = menuPanel.transform.Find("SoundToggleBtn");
                if (existing != null)
                {
                    soundButton = existing.GetComponent<Button>();
                    soundButtonText = existing.GetComponentInChildren<Text>();
                }
                else
                {
                    // Dynamically instantiate clean Settings button in top right of menu
                    GameObject btnGo = new GameObject("SoundToggleBtn");
                    btnGo.transform.SetParent(menuPanel.transform, false);

                    RectTransform rt = btnGo.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.85f, 0.94f);
                    rt.anchorMax = new Vector2(0.85f, 0.94f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(180, 76);

                    Image img = btnGo.AddComponent<Image>();
                    img.color = new Color(0.18f, 0.22f, 0.34f, 0.94f);

                    soundButton = btnGo.AddComponent<Button>();
                    btnGo.AddComponent<ButtonPressFeedback>();

                    GameObject txtGo = new GameObject("SoundBtnText");
                    txtGo.transform.SetParent(btnGo.transform, false);
                    RectTransform txtRt = txtGo.AddComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;

                    soundButtonText = txtGo.AddComponent<Text>();
                    soundButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    soundButtonText.fontSize = 22;
                    soundButtonText.fontStyle = FontStyle.Bold;
                    soundButtonText.alignment = TextAnchor.MiddleCenter;
                    soundButtonText.color = Color.white;
                }
            }

            if (soundButton != null)
            {
                RectTransform rt = soundButton.GetComponent<RectTransform>();
                if (rt != null && rt.sizeDelta.x < 140f)
                {
                    rt.anchorMin = new Vector2(0.85f, 0.94f);
                    rt.anchorMax = new Vector2(0.85f, 0.94f);
                    rt.sizeDelta = new Vector2(180, 76);
                }

                soundButton.onClick.RemoveListener(OnSoundClicked);
                soundButton.onClick.AddListener(OnSoundClicked);
            }
        }

        private void UpdateSoundButtonVisual()
        {
            if (soundButtonText != null)
            {
                soundButtonText.text = "SETTINGS";
                soundButtonText.color = new Color(0.90f, 0.95f, 1.0f, 1f);
            }
        }

        private void OnSoundClicked()
        {
            AudioManager.Instance?.PlayClick();
            Haptics.Light();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowSettings();
            }
            else
            {
                SettingsUI.Instance?.Show();
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
            UpdateSoundButtonVisual();
            UpdateMissionButtonBadge();
        }

        private void UpdateMissionButtonBadge()
        {
            if (missionButton == null) return;
            Text btnText = missionButton.GetComponentInChildren<Text>();
            if (btnText == null) return;

            int claimable = MissionUI.GetClaimableMissionCount();
            if (claimable > 0)
            {
                btnText.text = $"DAILY MISSIONS ({claimable}!)";
                btnText.color = new Color(1f, 0.95f, 0.35f, 1f);
            }
            else
            {
                btnText.text = "DAILY MISSIONS";
                btnText.color = Color.white;
            }
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
                highScoreText.text = $"BEST: {highScore}m";
            }
        }

        private void OnPlayClicked()
        {
            AudioManager.Instance?.PlayClick();
            Debug.Log("[MainMenuUI] Play button pressed. Starting gameplay...");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        private void OnSkinsClicked()
        {
            AudioManager.Instance?.PlayClick();
            Debug.Log("[MainMenuUI] Skins button pressed. Opening Shop...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowShop();
            }
        }

        private void OnMissionClicked()
        {
            AudioManager.Instance?.PlayClick();
            Debug.Log("[MainMenuUI] Mission button pressed. Opening Missions...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMissions();
            }
        }

        private void OnRankingClicked()
        {
            AudioManager.Instance?.PlayClick();
            Debug.Log("[MainMenuUI] Ranking button pressed. Opening Leaderboard...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowRanking();
            }
        }
    }
}
