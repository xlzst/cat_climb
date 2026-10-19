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
        [SerializeField] private ReviveUI reviveUI;

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
            EnsureReviveUI();
            EnsureTutorialUI();
            EnsureDailyRewardUI();
            EnsureSettingsUI();
            EnsureButtonPressFeedback();
        }

        private void EnsureSettingsUI()
        {
            if (SettingsUI.Instance == null)
            {
                if (GetComponentInChildren<SettingsUI>(true) == null)
                {
                    GameObject settingsGo = new GameObject("SettingsUI");
                    settingsGo.transform.SetParent(transform, false);
                    settingsGo.AddComponent<SettingsUI>();
                    Debug.Log("[UIManager] Dynamically ensured and instantiated SettingsUI on Canvas.");
                }
            }
        }

        private void EnsureButtonPressFeedback()
        {
            UnityEngine.UI.Button[] allButtons = GetComponentsInChildren<UnityEngine.UI.Button>(true);
            for (int i = 0; i < allButtons.Length; i++)
            {
                UnityEngine.UI.Button btn = allButtons[i];
                if (btn != null && btn.GetComponent<ButtonPressFeedback>() == null)
                {
                    btn.gameObject.AddComponent<ButtonPressFeedback>();
                }
            }
        }

        private void EnsureDailyRewardUI()
        {
            if (DailyRewardUI.Instance == null)
            {
                if (GetComponentInChildren<DailyRewardUI>(true) == null)
                {
                    GameObject rewardGo = new GameObject("DailyRewardUI");
                    rewardGo.transform.SetParent(transform, false);
                    rewardGo.AddComponent<DailyRewardUI>();
                    Debug.Log("[UIManager] Dynamically ensured and instantiated DailyRewardUI on Canvas.");
                }
            }
        }

        private void EnsureReviveUI()
        {
            if (reviveUI == null)
            {
                reviveUI = GetComponentInChildren<ReviveUI>(true);
            }

            if (reviveUI == null)
            {
                // Programmatically construct RevivePanel hierarchy dynamically on Canvas
                GameObject reviveGo = CreateUIPanel(transform, "RevivePanel", new Color(0.05f, 0.06f, 0.09f, 0.88f));
                reviveUI = reviveGo.AddComponent<ReviveUI>();

                GameObject reviveCardGo = CreateUIPanel(reviveGo.transform, "ReviveCard", new Color(0.12f, 0.14f, 0.22f, 0.96f));
                RectTransform reviveCardRect = reviveCardGo.GetComponent<RectTransform>();
                reviveCardRect.anchorMin = new Vector2(0.5f, 0.5f);
                reviveCardRect.anchorMax = new Vector2(0.5f, 0.5f);
                reviveCardRect.pivot = new Vector2(0.5f, 0.5f);
                reviveCardRect.sizeDelta = new Vector2(760, 680);

                GameObject reviveTitleGo = CreateUIText(reviveCardGo.transform, "ReviveTitle", "SECOND CHANCE?", 52, FontStyle.Bold, new Color(1f, 0.85f, 0.3f, 1f), TextAnchor.MiddleCenter);
                RectTransform reviveTitleRect = reviveTitleGo.GetComponent<RectTransform>();
                reviveTitleRect.anchorMin = new Vector2(0.5f, 0.84f);
                reviveTitleRect.anchorMax = new Vector2(0.5f, 0.84f);
                reviveTitleRect.sizeDelta = new Vector2(700, 70);

                GameObject countdownGo = CreateUIText(reviveCardGo.transform, "CountdownText", "5", 72, FontStyle.Bold, new Color(1f, 0.4f, 0.4f, 1f), TextAnchor.MiddleCenter);
                RectTransform cdRect = countdownGo.GetComponent<RectTransform>();
                cdRect.anchorMin = new Vector2(0.5f, 0.65f);
                cdRect.anchorMax = new Vector2(0.5f, 0.65f);
                cdRect.sizeDelta = new Vector2(200, 90);

                GameObject coinReviveBtnGo = CreateUIButton(reviveCardGo.transform, "CoinReviveButton", "REVIVE (20 FISH)", 30, new Color(0.2f, 0.75f, 0.4f, 1f), Color.white);
                RectTransform coinReviveBtnRect = coinReviveBtnGo.GetComponent<RectTransform>();
                coinReviveBtnRect.anchorMin = new Vector2(0.5f, 0.46f);
                coinReviveBtnRect.anchorMax = new Vector2(0.5f, 0.46f);
                coinReviveBtnRect.sizeDelta = new Vector2(520, 85);
                UnityEngine.UI.Button coinReviveBtn = coinReviveBtnGo.GetComponent<UnityEngine.UI.Button>();
                UnityEngine.UI.Text coinReviveBtnTxt = coinReviveBtnGo.GetComponentInChildren<UnityEngine.UI.Text>();

                GameObject adReviveBtnGo = CreateUIButton(reviveCardGo.transform, "AdReviveButton", "WATCH AD REVIVE", 28, new Color(0.25f, 0.6f, 0.9f, 1f), Color.white);
                RectTransform adReviveBtnRect = adReviveBtnGo.GetComponent<RectTransform>();
                adReviveBtnRect.anchorMin = new Vector2(0.5f, 0.30f);
                adReviveBtnRect.anchorMax = new Vector2(0.5f, 0.30f);
                adReviveBtnRect.sizeDelta = new Vector2(520, 80);
                UnityEngine.UI.Button adReviveBtn = adReviveBtnGo.GetComponent<UnityEngine.UI.Button>();

                GameObject skipBtnGo = CreateUIButton(reviveCardGo.transform, "SkipButton", "NO THANKS", 26, new Color(0.4f, 0.45f, 0.55f, 0.8f), Color.white);
                RectTransform skipBtnRect = skipBtnGo.GetComponent<RectTransform>();
                skipBtnRect.anchorMin = new Vector2(0.5f, 0.14f);
                skipBtnRect.anchorMax = new Vector2(0.5f, 0.14f);
                skipBtnRect.sizeDelta = new Vector2(400, 65);
                UnityEngine.UI.Button skipBtn = skipBtnGo.GetComponent<UnityEngine.UI.Button>();

                reviveUI.Init(reviveGo, countdownGo.GetComponent<UnityEngine.UI.Text>(), coinReviveBtn, coinReviveBtnTxt, adReviveBtn, skipBtn);
                reviveGo.SetActive(false);
                Debug.Log("[UIManager] Dynamically ensured and instantiated ReviveUI on Canvas.");
            }
        }

        private void EnsureTutorialUI()
        {
            if (TutorialUI.Instance == null)
            {
                if (GetComponentInChildren<TutorialUI>(true) == null)
                {
                    GameObject tutGo = new GameObject("TutorialUI");
                    tutGo.transform.SetParent(transform, false);
                    tutGo.AddComponent<TutorialUI>();
                    Debug.Log("[UIManager] Dynamically ensured and instantiated TutorialUI on Canvas.");
                }
            }
        }

        private GameObject CreateUIPanel(Transform parent, string name, Color bgColor)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            UnityEngine.UI.Image img = panel.GetComponent<UnityEngine.UI.Image>();
            img.color = bgColor;
            if (bgColor.a <= 0.01f)
            {
                img.raycastTarget = false;
            }

            return panel;
        }

        private GameObject CreateUIText(Transform parent, string name, string text, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textGo.transform.SetParent(parent, false);

            UnityEngine.UI.Text txt = textGo.GetComponent<UnityEngine.UI.Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            txt.fontSize = fontSize;
            txt.fontStyle = fontStyle;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            return textGo;
        }

        private GameObject CreateUIButton(Transform parent, string name, string labelText, int fontSize, Color btnColor, Color textColor)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            btnGo.transform.SetParent(parent, false);

            UnityEngine.UI.Image img = btnGo.GetComponent<UnityEngine.UI.Image>();
            img.color = btnColor;

            UnityEngine.UI.Button btn = btnGo.GetComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;

            GameObject labelGo = CreateUIText(btnGo.transform, "Label", labelText, fontSize, FontStyle.Bold, textColor, TextAnchor.MiddleCenter);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return btnGo;
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
                GameManager.Instance.OnCoinsChanged += HandleCoinsChanged;

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
                GameManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
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
                    if (reviveUI != null) reviveUI.Hide();
                    EnsureDailyRewardUI();
                    DailyRewardUI.Instance?.CheckAndPromptIfAvailable();
                    break;

                case GameState.Playing:
                    if (mainMenuUI != null) mainMenuUI.Hide();
                    if (gameplayHUDUI != null) gameplayHUDUI.Show(currentScore, highScore);
                    if (gameOverUI != null) gameOverUI.Hide();
                    if (pauseUI != null) pauseUI.Hide();
                    if (shopUI != null) shopUI.Hide();
                    if (missionUI != null) missionUI.Hide();
                    if (rankingUI != null) rankingUI.Hide();
                    if (reviveUI != null) reviveUI.Hide();
                    break;

                case GameState.Revive:
                    if (mainMenuUI != null) mainMenuUI.Hide();
                    if (gameplayHUDUI != null) gameplayHUDUI.Hide();
                    if (gameOverUI != null) gameOverUI.Hide();
                    if (pauseUI != null) pauseUI.Hide();
                    if (shopUI != null) shopUI.Hide();
                    if (missionUI != null) missionUI.Hide();
                    if (rankingUI != null) rankingUI.Hide();
                    if (reviveUI == null) EnsureReviveUI();
                    if (reviveUI != null)
                    {
                        reviveUI.Show();
                    }
                    else
                    {
                        Debug.LogWarning("[UIManager] ReviveUI missing in scene during Revive state -> Falling back to GameOver.");
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.ConfirmGameOver();
                        }
                    }
                    break;

                case GameState.GameOver:
                    if (mainMenuUI != null) mainMenuUI.Hide();
                    if (gameplayHUDUI != null) gameplayHUDUI.Hide();
                    if (gameOverUI != null) gameOverUI.Show(currentScore, highScore);
                    if (pauseUI != null) pauseUI.Hide();
                    if (shopUI != null) shopUI.Hide();
                    if (missionUI != null) missionUI.Hide();
                    if (rankingUI != null) rankingUI.Hide();
                    if (reviveUI != null) reviveUI.Hide();
                    break;

                case GameState.Paused:
                    if (pauseUI != null)
                    {
                        pauseUI.Show();
                        AnimatePanelOpen(pauseUI);
                    }
                    break;
            }
        }

        public void ShowSettings()
        {
            EnsureSettingsUI();
            SettingsUI.Instance?.Show();
        }

        public void HideSettings()
        {
            SettingsUI.Instance?.Hide();
        }

        public void ShowShop()
        {
            if (mainMenuUI != null) mainMenuUI.Hide();
            if (missionUI != null) missionUI.Hide();
            if (rankingUI != null) rankingUI.Hide();
            if (shopUI != null)
            {
                shopUI.Show();
                AnimatePanelOpen(shopUI);
            }
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
            if (missionUI != null)
            {
                missionUI.Show();
                AnimatePanelOpen(missionUI);
            }
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
            if (rankingUI != null)
            {
                rankingUI.Show();
                AnimatePanelOpen(rankingUI);
            }
        }

        public void HideRanking()
        {
            if (rankingUI != null) rankingUI.Hide();
            if (mainMenuUI != null) mainMenuUI.Show(GameManager.Instance != null ? GameManager.Instance.HighScore : 0);
        }

        private void AnimatePanelOpen(Component panelComp)
        {
            if (panelComp == null || !gameObject.activeInHierarchy) return;
            StartCoroutine(PanelScaleInRoutine(panelComp.transform));
        }

        private System.Collections.IEnumerator PanelScaleInRoutine(Transform target)
        {
            if (target == null) yield break;
            float duration = 0.15f;
            float elapsed = 0f;
            target.localScale = new Vector3(0.92f, 0.92f, 1f);

            while (elapsed < duration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);
                float s = Mathf.Lerp(0.92f, 1.0f, easeOut);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
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

        private void HandleCoinsChanged(int totalCoins)
        {
            if (gameplayHUDUI != null)
            {
                gameplayHUDUI.UpdateCoins(GameManager.Instance != null ? GameManager.Instance.SessionCoins : 0);
            }
            if (shopUI != null)
            {
                shopUI.RefreshUI();
            }
        }

        public void ShowToast(string message, Color? color = null)
        {
            if (gameplayHUDUI != null)
            {
                gameplayHUDUI.ShowToast(message, color);
            }
            else if (GameplayHUDUI.Instance != null)
            {
                GameplayHUDUI.Instance.ShowToast(message, color);
            }
        }

        public void SetReferences(MainMenuUI mainMenu, GameplayHUDUI hud, GameOverUI gameOver, ShopUI shop = null, MissionUI mission = null, RankingUI ranking = null, PauseUI pause = null, ReviveUI revive = null)
        {
            mainMenuUI = mainMenu;
            gameplayHUDUI = hud;
            gameOverUI = gameOver;
            shopUI = shop;
            missionUI = mission;
            rankingUI = ranking;
            pauseUI = pause;
            reviveUI = revive;
        }
    }
}
