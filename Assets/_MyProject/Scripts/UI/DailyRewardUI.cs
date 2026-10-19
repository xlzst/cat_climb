using System;
using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI
{
    /// <summary>
    /// 7-Day Daily Login Streak popup.
    /// Automatically checks local date on Main Menu entry and offers escalating Fish Coin rewards
    /// to drive Day-1 to Day-7 retention.
    /// </summary>
    public class DailyRewardUI : MonoBehaviour
    {
        public static DailyRewardUI Instance { get; private set; }

        public static readonly int[] DayRewards = { 25, 40, 60, 80, 100, 150, 250 };

        private GameObject panelGo;
        private Text subtitleText;
        private Button claimButton;
        private Text claimButtonText;
        private readonly Image[] dayCardBgs = new Image[7];
        private readonly Text[] dayStatusTexts = new Text[7];

        private int currentRewardDayIndex = 0; // 0..6 (Day 1..7)
        private bool canClaimToday = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureUIHierarchy();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Checks if today's login reward has not yet been claimed, and if so, opens the popup.
        /// </summary>
        public void CheckAndPromptIfAvailable()
        {
            EvaluateStreakState();
            if (canClaimToday)
            {
                Show();
            }
        }

        private void EvaluateStreakState()
        {
            var save = SaveData.Current;
            string todayStr = SaveData.GetTodayDateKey();

            if (save.lastLoginRewardDate == todayStr)
            {
                canClaimToday = false;
                currentRewardDayIndex = Mathf.Clamp(save.loginStreakDay - 1, 0, 6);
                return;
            }

            canClaimToday = true;

            // Check if yesterday was the last claim date to continue streak
            string yesterdayStr = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            if (save.lastLoginRewardDate == yesterdayStr && save.loginStreakDay >= 1 && save.loginStreakDay < 7)
            {
                currentRewardDayIndex = save.loginStreakDay; // Next day in 0..6
            }
            else
            {
                // Start new 7-day cycle at Day 1 (index 0)
                currentRewardDayIndex = 0;
            }
        }

        public void Show()
        {
            EnsureUIHierarchy();
            EvaluateStreakState();
            if (panelGo != null)
            {
                panelGo.SetActive(true);
            }
            RefreshVisuals();
        }

        public void Hide()
        {
            if (panelGo != null)
            {
                panelGo.SetActive(false);
            }
        }

        private void RefreshVisuals()
        {
            int displayDay = currentRewardDayIndex + 1;
            int rewardAmount = DayRewards[currentRewardDayIndex];

            if (subtitleText != null)
            {
                subtitleText.text = canClaimToday
                    ? $"DAY {displayDay} STREAK BONUS READY!"
                    : $"COME BACK TOMORROW FOR DAY {(displayDay % 7) + 1}!";
            }

            for (int i = 0; i < 7; i++)
            {
                if (dayCardBgs[i] == null || dayStatusTexts[i] == null) continue;

                if (i < currentRewardDayIndex || (!canClaimToday && i == currentRewardDayIndex))
                {
                    // Already claimed
                    dayCardBgs[i].color = new Color(0.16f, 0.32f, 0.24f, 0.95f);
                    dayStatusTexts[i].text = "CLAIMED";
                    dayStatusTexts[i].color = new Color(0.55f, 0.95f, 0.65f, 1f);
                }
                else if (i == currentRewardDayIndex && canClaimToday)
                {
                    // Today's active reward
                    dayCardBgs[i].color = new Color(0.28f, 0.24f, 0.44f, 0.98f);
                    dayStatusTexts[i].text = "TODAY!";
                    dayStatusTexts[i].color = new Color(1f, 0.88f, 0.25f, 1f);
                }
                else
                {
                    // Future day
                    dayCardBgs[i].color = new Color(0.14f, 0.16f, 0.23f, 0.92f);
                    dayStatusTexts[i].text = i == 6 ? "MEGA BONUS" : "LOCKED";
                    dayStatusTexts[i].color = new Color(0.65f, 0.72f, 0.82f, 0.9f);
                }
            }

            if (claimButton != null)
            {
                claimButton.interactable = true;
            }
            if (claimButtonText != null)
            {
                claimButtonText.text = canClaimToday
                    ? $"CLAIM +{rewardAmount} FISH!"
                    : "AWESOME!";
            }
        }

        private void OnClaimClicked()
        {
            if (!canClaimToday)
            {
                AudioManager.Instance?.PlayClick();
                Hide();
                return;
            }

            int rewardAmount = DayRewards[currentRewardDayIndex];
            var save = SaveData.Current;
            save.lastLoginRewardDate = SaveData.GetTodayDateKey();
            save.loginStreakDay = currentRewardDayIndex + 1;
            SaveData.MarkDirty();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(rewardAmount);
                GameManager.Instance.FlushSave();
            }
            else
            {
                SaveData.Flush();
            }

            canClaimToday = false;
            AudioManager.Instance?.PlayStreak();
            Haptics.DoublePulse();
            if (FXManager.Instance != null && Camera.main != null)
            {
                FXManager.Instance.EmitCoinSparkle(Camera.main.transform.position + Vector3.forward * 2f);
            }

            Hide();
        }

        private void EnsureUIHierarchy()
        {
            if (panelGo != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            panelGo = new GameObject("DailyRewardPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas.transform, false);

            RectTransform fullRect = panelGo.GetComponent<RectTransform>();
            fullRect.anchorMin = Vector2.zero;
            fullRect.anchorMax = Vector2.one;
            fullRect.offsetMin = Vector2.zero;
            fullRect.offsetMax = Vector2.zero;

            Image dimBg = panelGo.GetComponent<Image>();
            dimBg.color = new Color(0.04f, 0.05f, 0.09f, 0.90f);

            // Center Modal Card
            GameObject cardGo = new GameObject("RewardModalCard", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(panelGo.transform, false);
            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(880, 1180);

            Image cardImg = cardGo.GetComponent<Image>();
            cardImg.color = new Color(0.10f, 0.12f, 0.20f, 0.98f);

            // Title
            Text titleTxt = CreateText(cardGo.transform, "Title", "DAILY LOGIN BONUS", 50, FontStyle.Bold, new Color(1f, 0.86f, 0.28f, 1f));
            RectTransform titleRt = titleTxt.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.91f);
            titleRt.anchorMax = new Vector2(0.5f, 0.91f);
            titleRt.sizeDelta = new Vector2(800, 70);

            // Subtitle
            subtitleText = CreateText(cardGo.transform, "Subtitle", "DAY 1 STREAK BONUS READY!", 24, FontStyle.Bold, new Color(0.35f, 0.92f, 0.85f, 1f));
            RectTransform subRt = subtitleText.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0.84f);
            subRt.anchorMax = new Vector2(0.5f, 0.84f);
            subRt.sizeDelta = new Vector2(780, 45);

            // 7 Day Rows
            float startY = 0.75f;
            float stepY = 0.085f;
            for (int i = 0; i < 7; i++)
            {
                float rowY = startY - i * stepY;
                GameObject rowGo = new GameObject($"DayRow_{i + 1}", typeof(RectTransform), typeof(Image));
                rowGo.transform.SetParent(cardGo.transform, false);
                RectTransform rowRt = rowGo.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0.5f, rowY);
                rowRt.anchorMax = new Vector2(0.5f, rowY);
                rowRt.sizeDelta = new Vector2(760, 82);

                dayCardBgs[i] = rowGo.GetComponent<Image>();
                dayCardBgs[i].color = new Color(0.14f, 0.16f, 0.23f, 0.95f);

                Text dayLabel = CreateText(rowGo.transform, "DayLabel", $"DAY {i + 1}", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                RectTransform dlRt = dayLabel.GetComponent<RectTransform>();
                dlRt.anchorMin = new Vector2(0.06f, 0.5f);
                dlRt.anchorMax = new Vector2(0.06f, 0.5f);
                dlRt.pivot = new Vector2(0f, 0.5f);
                dlRt.sizeDelta = new Vector2(200, 50);

                Text rewardLabel = CreateText(rowGo.transform, "RewardLabel", $"+{DayRewards[i]} FISH", 28, FontStyle.Bold, new Color(1f, 0.85f, 0.3f, 1f), TextAnchor.MiddleCenter);
                RectTransform rlRt = rewardLabel.GetComponent<RectTransform>();
                rlRt.anchorMin = new Vector2(0.5f, 0.5f);
                rlRt.anchorMax = new Vector2(0.5f, 0.5f);
                rlRt.sizeDelta = new Vector2(260, 50);

                dayStatusTexts[i] = CreateText(rowGo.transform, "StatusLabel", "LOCKED", 22, FontStyle.Bold, new Color(0.7f, 0.75f, 0.85f, 1f), TextAnchor.MiddleRight);
                RectTransform stRt = dayStatusTexts[i].GetComponent<RectTransform>();
                stRt.anchorMin = new Vector2(0.94f, 0.5f);
                stRt.anchorMax = new Vector2(0.94f, 0.5f);
                stRt.pivot = new Vector2(1f, 0.5f);
                stRt.sizeDelta = new Vector2(220, 50);
            }

            // Claim CTA Button
            GameObject btnGo = new GameObject("ClaimRewardButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(cardGo.transform, false);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.10f);
            btnRt.anchorMax = new Vector2(0.5f, 0.10f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(520, 100);

            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.22f, 0.78f, 0.45f, 1f);

            claimButton = btnGo.GetComponent<Button>();
            claimButton.targetGraphic = btnImg;
            claimButton.onClick.AddListener(OnClaimClicked);

            claimButtonText = CreateText(btnGo.transform, "ClaimBtnText", "CLAIM +25 FISH!", 34, FontStyle.Bold, Color.white);
            RectTransform cbtRt = claimButtonText.GetComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero;
            cbtRt.anchorMax = Vector2.one;
            cbtRt.offsetMin = Vector2.zero;
            cbtRt.offsetMax = Vector2.zero;

            panelGo.SetActive(false);
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle style, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text txt = go.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = anchor;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }
    }
}
