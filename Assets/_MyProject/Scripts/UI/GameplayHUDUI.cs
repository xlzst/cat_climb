using System.Collections;
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
        [SerializeField] private Text coinText;
        [SerializeField] private GameObject magnetBadge;
        [SerializeField] private Text magnetBadgeText;
        [SerializeField] private Button pauseButton;

        public static GameplayHUDUI Instance { get; private set; }

        private PlayerController cachedPlayer;

        [Header("Toast Banner Feedback")]
        private GameObject toastBannerGo;
        private Text toastBannerText;
        private CanvasGroup toastCanvasGroup;
        private Coroutine toastCoroutine;

        [Header("Combo Badge Feedback")]
        private GameObject comboBadgeGo;
        private Text comboBadgeText;
        private Coroutine comboPulseCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (GetComponent<SafeAreaFitter>() == null)
            {
                gameObject.AddComponent<SafeAreaFitter>();
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }
            EnsureCoinCard();
            EnsureMagnetBadge();
            EnsureComboBadge();
            EnsureToastBanner();
        }

        private void Start()
        {
            PlayerController player = GetPlayer();
            if (player != null)
            {
                player.OnComboChanged += UpdateComboBadge;
            }
        }

        private void OnDestroy()
        {
            if (cachedPlayer != null)
            {
                cachedPlayer.OnComboChanged -= UpdateComboBadge;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnsureCoinCard()
        {
            if (coinText == null)
            {
                Transform found = transform.Find("CoinCard/CoinText");
                if (found != null)
                {
                    coinText = found.GetComponent<Text>();
                }
            }

            if (coinText == null)
            {
                // Create CoinCard container dynamically on HUD
                GameObject coinCard = new GameObject("CoinCard", typeof(RectTransform), typeof(Image));
                coinCard.transform.SetParent(transform, false);
                RectTransform coinCardRect = coinCard.GetComponent<RectTransform>();
                coinCardRect.anchorMin = new Vector2(0.05f, 0.865f);
                coinCardRect.anchorMax = new Vector2(0.05f, 0.865f);
                coinCardRect.pivot = new Vector2(0f, 1f);
                coinCardRect.sizeDelta = new Vector2(240, 70);

                Image img = coinCard.GetComponent<Image>();
                img.color = new Color(0.08f, 0.09f, 0.12f, 0.65f);
                img.raycastTarget = false;

                GameObject coinTextGo = new GameObject("CoinText", typeof(RectTransform), typeof(Text));
                coinTextGo.transform.SetParent(coinCard.transform, false);
                RectTransform textRect = coinTextGo.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.sizeDelta = new Vector2(220, 60);

                coinText = coinTextGo.GetComponent<Text>();
                coinText.text = "FISH: 0";
                coinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 24);
                coinText.fontSize = 24;
                coinText.fontStyle = FontStyle.Bold;
                coinText.color = new Color(1f, 0.84f, 0.3f, 1f);
                coinText.alignment = TextAnchor.MiddleCenter;
                coinText.raycastTarget = false;
            }
        }

        public void EnsureMagnetBadge()
        {
            if (magnetBadge == null)
            {
                Transform found = transform.Find("MagnetBadge");
                if (found != null)
                {
                    magnetBadge = found.gameObject;
                    magnetBadgeText = found.Find("MagnetText")?.GetComponent<Text>();
                }
            }

            if (magnetBadge == null)
            {
                // Create MagnetBadge dynamic container on HUD (positioned below CoinCard)
                GameObject badgeGo = new GameObject("MagnetBadge", typeof(RectTransform), typeof(Image));
                badgeGo.transform.SetParent(transform, false);
                RectTransform badgeRect = badgeGo.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0.05f, 0.795f);
                badgeRect.anchorMax = new Vector2(0.05f, 0.795f);
                badgeRect.pivot = new Vector2(0f, 1f);
                badgeRect.sizeDelta = new Vector2(240, 50);

                Image img = badgeGo.GetComponent<Image>();
                img.color = new Color(0.78f, 0.16f, 0.22f, 0.85f); // Vivid power-up red
                img.raycastTarget = false;

                GameObject textGo = new GameObject("MagnetText", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(badgeGo.transform, false);
                RectTransform textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.sizeDelta = new Vector2(220, 44);

                magnetBadgeText = textGo.GetComponent<Text>();
                magnetBadgeText.text = "MAGNET: 8.0s";
                magnetBadgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 20);
                magnetBadgeText.fontSize = 20;
                magnetBadgeText.fontStyle = FontStyle.Bold;
                magnetBadgeText.color = new Color(1f, 0.95f, 0.6f, 1f);
                magnetBadgeText.alignment = TextAnchor.MiddleCenter;
                magnetBadgeText.raycastTarget = false;

                magnetBadge = badgeGo;
                magnetBadge.SetActive(false);
            }
        }

        public void EnsureToastBanner()
        {
            if (toastBannerGo == null)
            {
                Transform found = transform.Find("ToastBanner");
                if (found != null)
                {
                    toastBannerGo = found.gameObject;
                    toastBannerText = found.Find("ToastText")?.GetComponent<Text>();
                    toastCanvasGroup = found.GetComponent<CanvasGroup>();
                }
            }

            if (toastBannerGo == null)
            {
                // Dynamic ToastBanner card anchored at top-center
                toastBannerGo = new GameObject("ToastBanner", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                toastBannerGo.transform.SetParent(transform, false);

                RectTransform rect = toastBannerGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.75f);
                rect.anchorMax = new Vector2(0.5f, 0.75f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(360, 60);

                Image img = toastBannerGo.GetComponent<Image>();
                img.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);
                img.raycastTarget = false;

                toastCanvasGroup = toastBannerGo.GetComponent<CanvasGroup>();

                GameObject textGo = new GameObject("ToastText", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(toastBannerGo.transform, false);
                RectTransform textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                toastBannerText = textGo.GetComponent<Text>();
                toastBannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 22);
                toastBannerText.fontSize = 22;
                toastBannerText.fontStyle = FontStyle.Bold;
                toastBannerText.color = new Color(1f, 0.88f, 0.25f, 1f);
                toastBannerText.alignment = TextAnchor.MiddleCenter;
                toastBannerText.raycastTarget = false;

                toastBannerGo.SetActive(false);
            }
        }

        public void ShowToast(string message, Color? textColor = null)
        {
            if (toastBannerGo == null)
            {
                EnsureToastBanner();
            }

            if (toastBannerGo == null || toastBannerText == null) return;

            if (toastCoroutine != null)
            {
                StopCoroutine(toastCoroutine);
            }
            toastCoroutine = StartCoroutine(ToastRoutine(message, textColor ?? new Color(1f, 0.88f, 0.25f, 1f)));
        }

        private System.Collections.IEnumerator ToastRoutine(string message, Color col)
        {
            toastBannerGo.SetActive(true);
            toastBannerText.text = message;
            toastBannerText.color = col;

            RectTransform rect = toastBannerGo.GetComponent<RectTransform>();
            if (toastCanvasGroup != null) toastCanvasGroup.alpha = 1f;

            // 1. Pop-in scale bounce (1.35x -> 1.0x)
            float elapsed = 0f;
            float popDuration = 0.14f;
            while (elapsed < popDuration)
            {
                float t = elapsed / popDuration;
                float s = Mathf.Lerp(1.35f, 1f, t);
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            rect.localScale = Vector3.one;

            // 2. Sustain hold
            yield return new WaitForSecondsRealtime(1.0f);

            // 3. Smooth fade-out
            elapsed = 0f;
            float fadeDuration = 0.3f;
            while (elapsed < fadeDuration)
            {
                float t = elapsed / fadeDuration;
                if (toastCanvasGroup != null) toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
            toastBannerGo.SetActive(false);
            toastCoroutine = null;
        }

        private PlayerController GetPlayer()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<PlayerController>();
            }
            return cachedPlayer;
        }

        private void Update()
        {
            UpdateMagnetHUD();
        }

        private void UpdateMagnetHUD()
        {
            PlayerController player = GetPlayer();
            if (player != null && player.IsMagnetActive)
            {
                if (magnetBadge == null)
                {
                    EnsureMagnetBadge();
                }

                if (magnetBadge != null && !magnetBadge.activeSelf)
                {
                    magnetBadge.SetActive(true);
                }

                if (magnetBadgeText != null)
                {
                    magnetBadgeText.text = $"MAGNET: {player.MagnetRemainingTime:F1}s";
                }
            }
            else
            {
                if (magnetBadge != null && magnetBadge.activeSelf)
                {
                    magnetBadge.SetActive(false);
                }
            }
        }

        private void OnPauseClicked()
        {
            AudioManager.Instance?.PlayClick();
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
            UpdateCoins(GameManager.Instance != null ? GameManager.Instance.SessionCoins : 0);
        }

        public void Hide()
        {
            if (hudPanel != null)
            {
                hudPanel.SetActive(false);
            }
        }

        public void UpdateScore(int meters)
        {
            if (scoreText != null)
            {
                scoreText.text = $"{meters}m";
            }
        }

        public void UpdateHighScore(int bestMeters)
        {
            if (highScoreText != null)
            {
                highScoreText.text = $"BEST: {bestMeters}m";
            }
        }

        public void UpdateCoins(int coins)
        {
            if (coinText == null)
            {
                EnsureCoinCard();
            }

            if (coinText != null)
            {
                coinText.text = $"FISH: {coins}";
            }
        }

        public void EnsureComboBadge()
        {
            if (comboBadgeGo == null)
            {
                Transform found = transform.Find("ComboBadge");
                if (found != null)
                {
                    comboBadgeGo = found.gameObject;
                    comboBadgeText = found.Find("ComboText")?.GetComponent<Text>();
                }
            }

            if (comboBadgeGo == null)
            {
                comboBadgeGo = new GameObject("ComboBadge", typeof(RectTransform), typeof(Image));
                comboBadgeGo.transform.SetParent(transform, false);
                RectTransform rect = comboBadgeGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.725f);
                rect.anchorMax = new Vector2(0.05f, 0.725f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(240, 48);

                Image img = comboBadgeGo.GetComponent<Image>();
                img.color = new Color(0.12f, 0.15f, 0.24f, 0.9f);
                img.raycastTarget = false;

                GameObject textGo = new GameObject("ComboText", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(comboBadgeGo.transform, false);
                RectTransform textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                comboBadgeText = textGo.GetComponent<Text>();
                comboBadgeText.text = "2x COMBO";
                comboBadgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 22);
                comboBadgeText.fontSize = 22;
                comboBadgeText.fontStyle = FontStyle.Bold;
                comboBadgeText.color = new Color(0.4f, 0.95f, 0.5f, 1f);
                comboBadgeText.alignment = TextAnchor.MiddleCenter;
                comboBadgeText.raycastTarget = false;

                comboBadgeGo.SetActive(false);
            }
        }

        public void UpdateComboBadge(int combo)
        {
            if (comboBadgeGo == null) EnsureComboBadge();
            if (comboBadgeGo == null) return;

            if (combo <= 1)
            {
                if (comboBadgeGo.activeSelf) comboBadgeGo.SetActive(false);
                return;
            }

            comboBadgeGo.SetActive(true);

            Color col = combo switch
            {
                <= 3 => new Color(0.4f, 0.95f, 0.5f, 1f),
                <= 7 => new Color(0.3f, 0.85f, 1f, 1f),
                <= 11 => new Color(1f, 0.6f, 0.2f, 1f),
                _ => new Color(1f, 0.3f, 0.85f, 1f)
            };

            string label = combo switch
            {
                <= 3 => $"{combo}x COMBO",
                <= 7 => $"{combo}x STREAK!",
                <= 11 => $"{combo}x ON FIRE!",
                _ => $"{combo}x UNSTOPPABLE!"
            };

            if (comboBadgeText != null)
            {
                comboBadgeText.text = label;
                comboBadgeText.color = col;
            }

            if (comboPulseCoroutine != null) StopCoroutine(comboPulseCoroutine);
            comboPulseCoroutine = StartCoroutine(PulseBadgeRoutine(comboBadgeGo.GetComponent<RectTransform>()));
        }

        private IEnumerator PulseBadgeRoutine(RectTransform rect)
        {
            if (rect == null) yield break;
            float elapsed = 0f;
            float duration = 0.12f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float s = Mathf.Lerp(1.25f, 1f, t);
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rect.localScale = Vector3.one;
            comboPulseCoroutine = null;
        }
    }
}
