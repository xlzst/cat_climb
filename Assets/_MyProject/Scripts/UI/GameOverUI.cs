using System.Collections;
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
        [SerializeField] private Button homeButton;
        [SerializeField] private Text fishEarnedText;
        [SerializeField] private Text runStatsText;
        [SerializeField] private Text celebrationBannerText;

        [Header("Juice & Animation")]
        [SerializeField] private bool pulseRestartButton = true;
        private Vector3 originalRestartScale = Vector3.one;

        private Coroutine countUpCoroutine;

        private void Awake()
        {
            EnsureUIReferences();
            BindButtons();
        }

        private void Start()
        {
            if (restartButton != null)
            {
                originalRestartScale = restartButton.transform.localScale;
            }
        }

        private void Update()
        {
            // Subtle pulse on the primary Try Again CTA button
            if (pulseRestartButton && restartButton != null && gameOverPanel != null && gameOverPanel.activeSelf)
            {
                float scaleOffset = Mathf.Sin(Time.unscaledTime * 3.2f) * 0.045f;
                restartButton.transform.localScale = originalRestartScale * (1f + scaleOffset);
            }
        }

        private void BindButtons()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(OnHomeClicked);
                homeButton.onClick.AddListener(OnHomeClicked);
            }
        }

        public void Show(int finalMeters, int bestMeters)
        {
            EnsureUIReferences();
            BindButtons();
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            bool isNewRecord = finalMeters > 0 && finalMeters >= bestMeters;

            if (celebrationBannerText != null)
            {
                if (isNewRecord)
                {
                    celebrationBannerText.gameObject.SetActive(true);
                    celebrationBannerText.text = "★ NEW BEST RECORD! ★";
                    celebrationBannerText.color = new Color(1f, 0.88f, 0.2f, 1f);
                    AudioManager.Instance?.PlayStreak();
                    Haptics.DoublePulse();
                    if (FXManager.Instance != null && Camera.main != null)
                    {
                        FXManager.Instance.EmitCoinSparkle(Camera.main.transform.position + Vector3.forward * 2f);
                    }
                }
                else
                {
                    int diff = Mathf.Max(1, bestMeters - finalMeters);
                    celebrationBannerText.gameObject.SetActive(true);
                    celebrationBannerText.text = $"Only {diff}m from your best!";
                    celebrationBannerText.color = new Color(0.75f, 0.85f, 0.95f, 1f);
                }
            }

            if (highScoreText != null)
            {
                highScoreText.text = $"BEST: {bestMeters}m";
            }

            // Run stats summary
            if (GameManager.Instance != null)
            {
                var stats = GameManager.Instance.CurrentRunStats;
                int claimableMissions = MissionUI.GetClaimableMissionCount();
                if (fishEarnedText != null)
                {
                    fishEarnedText.text = claimableMissions > 0
                        ? $"+{stats.sessionCoins} FISH   •   {claimableMissions} MISSION READY!"
                        : $"+{stats.sessionCoins} FISH";
                }
                if (runStatsText != null)
                {
                    runStatsText.text = $"MAX COMBO: {stats.maxCombo}x   |   WALL KICKS: {stats.wallKicks}   |   MICE: {stats.miceDefeated}";
                }
            }

            if (countUpCoroutine != null) StopCoroutine(countUpCoroutine);
            countUpCoroutine = StartCoroutine(CountUpMetersRoutine(finalMeters));
        }

        private IEnumerator CountUpMetersRoutine(int targetMeters)
        {
            float duration = 0.65f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                int current = Mathf.RoundToInt(Mathf.Lerp(0, targetMeters, elapsed / duration));
                if (finalScoreText != null)
                {
                    finalScoreText.text = $"{current}m";
                }
                yield return null;
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = $"{targetMeters}m";
            }
        }

        public void Hide()
        {
            if (countUpCoroutine != null)
            {
                StopCoroutine(countUpCoroutine);
                countUpCoroutine = null;
            }

            if (restartButton != null)
            {
                restartButton.transform.localScale = originalRestartScale;
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void OnRestartClicked()
        {
            if (restartButton != null) restartButton.transform.localScale = originalRestartScale;
            AudioManager.Instance?.PlayClick();
            Haptics.Light();
            Debug.Log("[GameOverUI] Quick Retry clicked.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RetryRun();
            }
        }

        private void OnHomeClicked()
        {
            if (restartButton != null) restartButton.transform.localScale = originalRestartScale;
            AudioManager.Instance?.PlayClick();
            Haptics.Light();
            Debug.Log("[GameOverUI] Home button clicked.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }

        private void EnsureUIReferences()
        {
            if (gameOverPanel == null)
            {
                gameOverPanel = gameObject;
            }

            // 1. Celebration Banner (Top Feedback: New Best Record or Near-miss)
            if (celebrationBannerText == null && gameOverPanel != null)
            {
                Transform existing = gameOverPanel.transform.Find("CelebrationBanner");
                if (existing != null)
                {
                    celebrationBannerText = existing.GetComponent<Text>();
                }
                else
                {
                    GameObject bannerGo = new GameObject("CelebrationBanner");
                    bannerGo.transform.SetParent(gameOverPanel.transform, false);
                    celebrationBannerText = bannerGo.AddComponent<Text>();
                    celebrationBannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    celebrationBannerText.fontSize = 24;
                    celebrationBannerText.fontStyle = FontStyle.Bold;
                    celebrationBannerText.alignment = TextAnchor.MiddleCenter;
                    celebrationBannerText.color = new Color(1f, 0.88f, 0.2f, 1f);
                }
            }
            if (celebrationBannerText != null)
            {
                RectTransform rt = celebrationBannerText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.77f);
                rt.anchorMax = new Vector2(0.5f, 0.77f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(720, 50);
                rt.anchoredPosition = Vector2.zero;
            }

            // 2. Title ("GAME OVER")
            if (titleText == null && gameOverPanel != null)
            {
                titleText = gameOverPanel.transform.Find("GameOverTitle")?.GetComponent<Text>();
            }
            if (titleText != null)
            {
                RectTransform rt = titleText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.69f);
                rt.anchorMax = new Vector2(0.5f, 0.69f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(800, 90);
                rt.anchoredPosition = Vector2.zero;
            }

            // 3. Final Score Text ("124m")
            if (finalScoreText == null && gameOverPanel != null)
            {
                finalScoreText = gameOverPanel.transform.Find("FinalScoreText")?.GetComponent<Text>();
            }
            if (finalScoreText != null)
            {
                RectTransform rt = finalScoreText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.57f);
                rt.anchorMax = new Vector2(0.5f, 0.57f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(600, 110);
                rt.anchoredPosition = Vector2.zero;
                finalScoreText.fontSize = 58;
                finalScoreText.color = new Color(0.3f, 0.95f, 0.8f, 1f);
            }

            // 4. Fish Earned Text ("+N FISH")
            if (fishEarnedText == null && gameOverPanel != null)
            {
                Transform existing = gameOverPanel.transform.Find("FishEarnedText");
                if (existing != null)
                {
                    fishEarnedText = existing.GetComponent<Text>();
                }
                else
                {
                    GameObject fishGo = new GameObject("FishEarnedText");
                    fishGo.transform.SetParent(gameOverPanel.transform, false);
                    fishEarnedText = fishGo.AddComponent<Text>();
                    fishEarnedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    fishEarnedText.fontSize = 26;
                    fishEarnedText.fontStyle = FontStyle.Bold;
                    fishEarnedText.alignment = TextAnchor.MiddleCenter;
                    fishEarnedText.color = new Color(1f, 0.84f, 0.28f, 1f);
                }
            }
            if (fishEarnedText != null)
            {
                RectTransform rt = fishEarnedText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.47f);
                rt.anchorMax = new Vector2(0.5f, 0.47f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(500, 42);
                rt.anchoredPosition = Vector2.zero;
            }

            // 5. High Score Text ("BEST: 210m")
            if (highScoreText == null && gameOverPanel != null)
            {
                highScoreText = gameOverPanel.transform.Find("HighScoreText")?.GetComponent<Text>();
            }
            if (highScoreText != null)
            {
                RectTransform rt = highScoreText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.41f);
                rt.anchorMax = new Vector2(0.5f, 0.41f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(600, 45);
                rt.anchoredPosition = Vector2.zero;
                highScoreText.fontSize = 26;
                highScoreText.color = new Color(1f, 0.92f, 0.45f, 1f);
            }

            // 6. Run Stats Text ("MAX COMBO: 5x | WALL KICKS: 12 | MICE: 2")
            if (runStatsText == null && gameOverPanel != null)
            {
                Transform existing = gameOverPanel.transform.Find("RunStatsText");
                if (existing != null)
                {
                    runStatsText = existing.GetComponent<Text>();
                }
                else
                {
                    GameObject statsGo = new GameObject("RunStatsText");
                    statsGo.transform.SetParent(gameOverPanel.transform, false);
                    runStatsText = statsGo.AddComponent<Text>();
                    runStatsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    runStatsText.fontSize = 17;
                    runStatsText.alignment = TextAnchor.MiddleCenter;
                    runStatsText.color = new Color(0.80f, 0.86f, 0.96f, 0.95f);
                }
            }
            if (runStatsText != null)
            {
                RectTransform rt = runStatsText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.33f);
                rt.anchorMax = new Vector2(0.5f, 0.33f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(720, 36);
                rt.anchoredPosition = Vector2.zero;
            }

            // 7. Restart Button ("TRY AGAIN") - Prominent, centered primary CTA
            if (restartButton == null && gameOverPanel != null)
            {
                restartButton = gameOverPanel.transform.Find("RestartButton")?.GetComponent<Button>();
            }
            if (restartButton != null)
            {
                RectTransform rt = restartButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.21f);
                rt.anchorMax = new Vector2(0.5f, 0.21f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(460, 96);
                rt.anchoredPosition = Vector2.zero;

                Image img = restartButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(1f, 0.38f, 0.42f, 1f); // Vibrant Coral CTA
                }

                Text txt = restartButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = "TRY AGAIN";
                    txt.fontSize = 34;
                    txt.fontStyle = FontStyle.Bold;
                    txt.color = Color.white;
                }
            }

            // 8. Home / Main Menu Button ("MAIN MENU") - Clean, beautifully centered secondary button
            if (homeButton == null && gameOverPanel != null)
            {
                Transform existingHome = gameOverPanel.transform.Find("HomeButton");
                if (existingHome != null)
                {
                    homeButton = existingHome.GetComponent<Button>();
                }
            }

            if (homeButton == null && gameOverPanel != null)
            {
                GameObject homeGo = new GameObject("HomeButton", typeof(RectTransform), typeof(Image), typeof(Button));
                homeGo.transform.SetParent(gameOverPanel.transform, false);

                homeButton = homeGo.GetComponent<Button>();

                GameObject txtGo = new GameObject("HomeBtnText", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(homeGo.transform, false);
                RectTransform txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;

                Text txt = txtGo.GetComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 24;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = new Color(0.92f, 0.95f, 1f, 1f);
                txt.text = "MAIN MENU";
            }

            if (homeButton != null)
            {
                RectTransform rt = homeButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.11f);
                rt.anchorMax = new Vector2(0.5f, 0.11f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(360, 76);
                rt.anchoredPosition = Vector2.zero;

                Image img = homeButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.20f, 0.24f, 0.36f, 0.95f); // Sleek Midnight Indigo Card
                }

                Text txt = homeButton.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = "MAIN MENU";
                    txt.fontSize = 24;
                    txt.fontStyle = FontStyle.Bold;
                    txt.color = new Color(0.92f, 0.95f, 1f, 1f);
                }
            }
        }
    }
}
