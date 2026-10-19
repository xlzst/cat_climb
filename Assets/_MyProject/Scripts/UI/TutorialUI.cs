using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI
{
    /// <summary>
    /// First-Time User Experience (FTUE) contextual tutorial.
    /// Guides new players through drag steering, wall climbing, and tap wall-kicking
    /// with zero reading friction and immediate actionable feedback.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        public const string TUTORIAL_PREF_KEY = "CatClimb_TutorialCompleted";

        public static TutorialUI Instance { get; private set; }

        private GameObject tutorialPanel;
        private Text stepTitleText;
        private Text stepDescText;
        private CanvasGroup canvasGroup;

        private int currentStep = 0; // 0 = not started, 1 = Move, 2 = Climb, 3 = Kick, 4 = Spikes/Done
        private bool isTutorialActive = false;
        private float steerTimer = 0f;
        private float initialClimbY = 0f;
        private Rigidbody2D cachedPlayerRb;

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

            EnsureUIHierarchy();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
            if (Instance == this) Instance = null;
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                bool isDone = PlayerPrefs.GetInt(TUTORIAL_PREF_KEY, 0) == 1;
                if (!isDone && !isTutorialActive)
                {
                    StartTutorial();
                }
            }
            else if (newState != GameState.Playing && isTutorialActive)
            {
                Hide();
            }
        }

        public void StartTutorial()
        {
            isTutorialActive = true;
            currentStep = 1;
            steerTimer = 0f;

            // Suppress wall spikes for the first 15 meters during the tutorial run
            if (WallManager.Instance != null)
            {
                WallManager.Instance.SetSpikeSuppressionUntil(15f);
            }

            ShowStep(1);
        }

        private void Update()
        {
            if (!isTutorialActive || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            PlayerController player = PlayerController.Instance;
            if (player == null) return;

            if (cachedPlayerRb == null || cachedPlayerRb.gameObject != player.gameObject)
            {
                cachedPlayerRb = player.GetComponent<Rigidbody2D>();
            }

            switch (currentStep)
            {
                case 1: // Steer
                    if (Mathf.Abs(cachedPlayerRb != null ? cachedPlayerRb.linearVelocity.x : 0f) > 0.8f)
                    {
                        steerTimer += Time.deltaTime;
                        if (steerTimer >= 0.45f)
                        {
                            currentStep = 2;
                            initialClimbY = player.transform.position.y;
                            ShowStep(2);
                        }
                    }
                    break;

                case 2: // Climb
                    if (player.transform.position.y > initialClimbY + 1.2f)
                    {
                        currentStep = 3;
                        ShowStep(3);
                    }
                    break;

                case 3: // Wall-Kick
                    // Completed when player performs a wall kick
                    if (GameManager.Instance.CurrentRunStats.wallKicks > 0)
                    {
                        currentStep = 4;
                        ShowStep(4);
                        StartCoroutine(CompleteTutorialRoutine());
                    }
                    break;
            }
        }

        private void ShowStep(int step)
        {
            EnsureUIHierarchy();
            if (tutorialPanel == null) return;

            tutorialPanel.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            switch (step)
            {
                case 1:
                    stepTitleText.text = "DRAG TO STEER";
                    stepDescText.text = "Hold and drag horizontally to guide your cat";
                    stepTitleText.color = new Color(0.35f, 0.95f, 0.6f, 1f);
                    break;
                case 2:
                    stepTitleText.text = "TOUCH WALL TO CLIMB";
                    stepDescText.text = "Hold against either wall to scamper upward";
                    stepTitleText.color = new Color(0.3f, 0.85f, 1f, 1f);
                    break;
                case 3:
                    stepTitleText.text = "TAP TO WALL-KICK!";
                    stepDescText.text = "Tap or release on a wall to leap across!";
                    stepTitleText.color = new Color(1f, 0.88f, 0.25f, 1f);
                    break;
                case 4:
                    stepTitleText.text = "CLIMB HIGH & AVOID SPIKES!";
                    stepDescText.text = "You're all set! Reach for the stars!";
                    stepTitleText.color = new Color(1f, 0.5f, 0.85f, 1f);
                    Haptics.DoublePulse();
                    AudioManager.Instance?.PlayStreak();
                    break;
            }
        }

        private IEnumerator CompleteTutorialRoutine()
        {
            PlayerPrefs.SetInt(TUTORIAL_PREF_KEY, 1);
            PlayerPrefs.Save();

            yield return new WaitForSeconds(2.0f);

            float elapsed = 0f;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.6f);
                }
                yield return null;
            }

            Hide();
            isTutorialActive = false;
        }

        public void Hide()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }

        private void EnsureUIHierarchy()
        {
            if (tutorialPanel != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            tutorialPanel = new GameObject("TutorialPanel");
            tutorialPanel.transform.SetParent(canvas.transform, false);

            RectTransform rt = tutorialPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.22f);
            rt.anchorMax = new Vector2(0.5f, 0.22f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620, 150);

            canvasGroup = tutorialPanel.AddComponent<CanvasGroup>();

            Image bg = tutorialPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.16f, 0.88f);

            // Title
            GameObject titleGo = new GameObject("TutorialTitle");
            titleGo.transform.SetParent(tutorialPanel.transform, false);
            RectTransform titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(20, 0);
            titleRt.offsetMax = new Vector2(-20, -10);

            stepTitleText = titleGo.AddComponent<Text>();
            stepTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stepTitleText.fontSize = 26;
            stepTitleText.fontStyle = FontStyle.Bold;
            stepTitleText.alignment = TextAnchor.MiddleCenter;
            stepTitleText.color = Color.white;

            // Description
            GameObject descGo = new GameObject("TutorialDesc");
            descGo.transform.SetParent(tutorialPanel.transform, false);
            RectTransform descRt = descGo.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 0.55f);
            descRt.offsetMin = new Vector2(20, 10);
            descRt.offsetMax = new Vector2(-20, 0);

            stepDescText = descGo.AddComponent<Text>();
            stepDescText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stepDescText.fontSize = 17;
            stepDescText.alignment = TextAnchor.MiddleCenter;
            stepDescText.color = new Color(0.85f, 0.88f, 0.95f, 1f);

            tutorialPanel.SetActive(false);
        }
    }
}
