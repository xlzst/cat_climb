using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI
{
    [Serializable]
    public class MissionItem
    {
        public string id;
        public string title;
        public string description;
        public int targetValue;
        public string rewardText;
        public int rewardCoins;
        public MissionType type;
    }

    public enum MissionType
    {
        ReachHeight,
        PlayRuns,
        DefeatEnemies,
        CollectFishSingleRun,
        CollectFishDaily,
        WallKicksDaily,
        ReachCombo
    }

    public class MissionUI : MonoBehaviour
    {
        [Header("UI Panel References")]
        [SerializeField] private GameObject missionPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button backButton;

        /// <summary>
        /// Master pool of 12 daily mission templates.
        /// Each calendar day (seeded by local yyyyMMdd), 5 distinct missions are deterministically selected.
        /// </summary>
        public static readonly List<MissionItem> MissionPool = new List<MissionItem>
        {
            new MissionItem { id = "climb_35", title = "Warmup Climb", description = "Reach 35m in height", targetValue = 35, rewardText = "50 Fish", rewardCoins = 50, type = MissionType.ReachHeight },
            new MissionItem { id = "climb_90", title = "Rooftop Sprinter", description = "Reach 90m in height", targetValue = 90, rewardText = "90 Fish", rewardCoins = 90, type = MissionType.ReachHeight },
            new MissionItem { id = "climb_180", title = "Stratosphere Cat", description = "Reach 180m in height", targetValue = 180, rewardText = "180 Fish", rewardCoins = 180, type = MissionType.ReachHeight },
            new MissionItem { id = "fish_run_12", title = "Snack Grabber", description = "Collect 12 Fish in one run", targetValue = 12, rewardText = "65 Fish", rewardCoins = 65, type = MissionType.CollectFishSingleRun },
            new MissionItem { id = "fish_daily_35", title = "Fish Feast", description = "Collect 35 Fish today", targetValue = 35, rewardText = "100 Fish", rewardCoins = 100, type = MissionType.CollectFishDaily },
            new MissionItem { id = "fish_daily_75", title = "Gourmet Hunter", description = "Collect 75 Fish today", targetValue = 75, rewardText = "160 Fish", rewardCoins = 160, type = MissionType.CollectFishDaily },
            new MissionItem { id = "kicks_20", title = "Wall Acrobat", description = "Perform 20 Wall-Kicks today", targetValue = 20, rewardText = "70 Fish", rewardCoins = 70, type = MissionType.WallKicksDaily },
            new MissionItem { id = "kicks_50", title = "Parkour Ninja", description = "Perform 50 Wall-Kicks today", targetValue = 50, rewardText = "130 Fish", rewardCoins = 130, type = MissionType.WallKicksDaily },
            new MissionItem { id = "combo_5", title = "Rhythm Jumper", description = "Reach a 5x Jump Combo", targetValue = 5, rewardText = "80 Fish", rewardCoins = 80, type = MissionType.ReachCombo },
            new MissionItem { id = "combo_9", title = "Unstoppable Flow", description = "Reach a 9x Jump Combo", targetValue = 9, rewardText = "140 Fish", rewardCoins = 140, type = MissionType.ReachCombo },
            new MissionItem { id = "runs_3", title = "Persistent Paws", description = "Play 3 climb runs today", targetValue = 3, rewardText = "60 Fish", rewardCoins = 60, type = MissionType.PlayRuns },
            new MissionItem { id = "mice_3", title = "Mouse Patrol", description = "Defeat 3 Enemy Mice today", targetValue = 3, rewardText = "120 Fish", rewardCoins = 120, type = MissionType.DefeatEnemies }
        };

        private static string cachedDayKey = "";
        private static readonly List<MissionItem> activeDailyMissions = new List<MissionItem>(5);

        public static List<MissionItem> AvailableMissions
        {
            get
            {
                EnsureDailyMissionsGenerated();
                return activeDailyMissions;
            }
        }

        private readonly List<Button> claimButtons = new List<Button>();
        private readonly List<Text> claimButtonTexts = new List<Text>();
        private readonly List<Text> progressTexts = new List<Text>();
        private readonly List<Text> cardTitleTexts = new List<Text>();
        private readonly List<Text> cardDescTexts = new List<Text>();

        private float timerRefreshAccumulator = 0f;

        public static void EnsureDailyMissionsGenerated()
        {
            SaveData.CheckDailyReset();
            string today = SaveData.GetTodayDateKey();
            if (cachedDayKey == today && activeDailyMissions.Count == 5)
            {
                return;
            }

            cachedDayKey = today;
            activeDailyMissions.Clear();

            // Deterministic hash from yyyyMMdd
            int seed = 17;
            for (int i = 0; i < today.Length; i++)
            {
                seed = seed * 31 + today[i];
            }
            System.Random rng = new System.Random(seed);

            // Pick 5 varied missions from MissionPool
            List<int> indices = new List<int>(MissionPool.Count);
            for (int i = 0; i < MissionPool.Count; i++) indices.Add(i);

            // Fisher-Yates shuffle with deterministic seed
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int swapIdx = rng.Next(i + 1);
                int temp = indices[i];
                indices[i] = indices[swapIdx];
                indices[swapIdx] = temp;
            }

            // First pass: prefer distinct MissionTypes so the 5 daily slots feel varied
            HashSet<MissionType> usedTypes = new HashSet<MissionType>();
            for (int i = 0; i < indices.Count && activeDailyMissions.Count < 5; i++)
            {
                MissionItem candidate = MissionPool[indices[i]];
                if (!usedTypes.Contains(candidate.type))
                {
                    usedTypes.Add(candidate.type);
                    activeDailyMissions.Add(candidate);
                }
            }

            // Fallback fill if fewer than 5 unique types
            for (int i = 0; i < indices.Count && activeDailyMissions.Count < 5; i++)
            {
                MissionItem candidate = MissionPool[indices[i]];
                if (!activeDailyMissions.Contains(candidate))
                {
                    activeDailyMissions.Add(candidate);
                }
            }
        }

        public static int GetMissionProgress(MissionItem mission)
        {
            var save = SaveData.Current;
            int bestMetersAllTime = GameManager.Instance != null ? GameManager.Instance.BestMeters : save.bestMeters;

            return mission.type switch
            {
                MissionType.ReachHeight => Mathf.Max(save.dailyBestMeters, bestMetersAllTime),
                MissionType.PlayRuns => Mathf.Max(save.dailyRunsPlayed, PlayerPrefs.GetInt("CatClimb_TotalRuns", 0)),
                MissionType.DefeatEnemies => Mathf.Max(save.dailyMiceDefeated, PlayerPrefs.GetInt("CatClimb_DefeatedEnemies", 0)),
                MissionType.CollectFishSingleRun => save.dailyMaxFishSingleRun,
                MissionType.CollectFishDaily => save.dailyFishCollected,
                MissionType.WallKicksDaily => save.dailyWallKicks,
                MissionType.ReachCombo => save.dailyMaxCombo,
                _ => 0
            };
        }

        public static bool IsMissionClaimed(string missionId)
        {
            return SaveData.IsDailyMissionClaimed(missionId) ||
                   PlayerPrefs.GetInt($"CatClimb_Mission_Claimed_{missionId}", 0) == 1;
        }

        public static int GetClaimableMissionCount()
        {
            var missions = AvailableMissions;
            int count = 0;
            for (int i = 0; i < missions.Count; i++)
            {
                var m = missions[i];
                if (!IsMissionClaimed(m.id) && GetMissionProgress(m) >= m.targetValue)
                {
                    count++;
                }
            }
            return count;
        }

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
            CacheDynamicCardLabelsIfNeeded();
        }

        private void Update()
        {
            if (missionPanel == null || !missionPanel.activeSelf) return;

            timerRefreshAccumulator += Time.unscaledDeltaTime;
            if (timerRefreshAccumulator >= 1.0f)
            {
                timerRefreshAccumulator = 0f;
                if (SaveData.CheckDailyReset())
                {
                    RefreshUI();
                }
                else
                {
                    UpdateSummaryHeader();
                }
            }
        }

        private void CacheDynamicCardLabelsIfNeeded()
        {
            if (missionPanel == null || cardTitleTexts.Count > 0) return;

            for (int i = 0; i < claimButtons.Count; i++)
            {
                if (claimButtons[i] != null && claimButtons[i].transform.parent != null)
                {
                    Transform card = claimButtons[i].transform.parent;
                    cardTitleTexts.Add(card.Find("MissionTitle")?.GetComponent<Text>());
                    cardDescTexts.Add(card.Find("MissionDesc")?.GetComponent<Text>());
                }
                else
                {
                    cardTitleTexts.Add(null);
                    cardDescTexts.Add(null);
                }
            }
        }

        public void Show()
        {
            if (missionPanel != null)
            {
                missionPanel.SetActive(true);
            }

            RefreshUI();
        }

        public void Hide()
        {
            if (missionPanel != null)
            {
                missionPanel.SetActive(false);
            }
        }

        public void RefreshUI()
        {
            EnsureDailyMissionsGenerated();
            CacheDynamicCardLabelsIfNeeded();

            var missions = AvailableMissions;

            for (int i = 0; i < missions.Count; i++)
            {
                var mission = missions[i];
                int currentProgress = GetMissionProgress(mission);
                bool isClaimed = IsMissionClaimed(mission.id);
                bool isCompleted = currentProgress >= mission.targetValue;

                if (i < cardTitleTexts.Count && cardTitleTexts[i] != null)
                {
                    cardTitleTexts[i].text = mission.title;
                }
                if (i < cardDescTexts.Count && cardDescTexts[i] != null)
                {
                    cardDescTexts[i].text = $"{mission.description} ({mission.rewardText})";
                }

                if (i < progressTexts.Count && progressTexts[i] != null)
                {
                    int displayProgress = Mathf.Min(currentProgress, mission.targetValue);
                    if (mission.type == MissionType.ReachHeight)
                    {
                        progressTexts[i].text = $"{displayProgress}m / {mission.targetValue}m";
                    }
                    else if (mission.type == MissionType.ReachCombo)
                    {
                        progressTexts[i].text = $"{displayProgress}x / {mission.targetValue}x";
                    }
                    else
                    {
                        progressTexts[i].text = $"{displayProgress} / {mission.targetValue}";
                    }
                }

                if (i < claimButtonTexts.Count && claimButtonTexts[i] != null)
                {
                    if (isClaimed)
                    {
                        claimButtonTexts[i].text = "COMPLETED";
                        claimButtonTexts[i].color = new Color(0.6f, 0.9f, 0.6f, 1f);
                    }
                    else if (isCompleted)
                    {
                        claimButtonTexts[i].text = $"CLAIM +{mission.rewardCoins}";
                        claimButtonTexts[i].color = Color.white;
                    }
                    else
                    {
                        claimButtonTexts[i].text = "IN PROGRESS";
                        claimButtonTexts[i].color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    }
                }

                if (i < claimButtons.Count && claimButtons[i] != null)
                {
                    int slotIndex = i;
                    claimButtons[i].onClick.RemoveAllListeners();
                    claimButtons[i].onClick.AddListener(() => ClaimMissionBySlot(slotIndex));

                    claimButtons[i].interactable = isCompleted && !isClaimed;
                    var btnImage = claimButtons[i].GetComponent<Image>();
                    if (btnImage != null)
                    {
                        if (isClaimed)
                        {
                            btnImage.color = new Color(0.2f, 0.35f, 0.25f, 0.8f);
                        }
                        else if (isCompleted)
                        {
                            btnImage.color = new Color(0.2f, 0.75f, 0.4f, 1f);
                        }
                        else
                        {
                            btnImage.color = new Color(0.25f, 0.28f, 0.35f, 0.8f);
                        }
                    }
                }
            }

            UpdateSummaryHeader();
        }

        private void UpdateSummaryHeader()
        {
            if (summaryText == null) return;

            var missions = AvailableMissions;
            int completedCount = 0;
            for (int i = 0; i < missions.Count; i++)
            {
                if (IsMissionClaimed(missions[i].id))
                {
                    completedCount++;
                }
            }

            DateTime now = DateTime.Now;
            DateTime nextMidnight = now.Date.AddDays(1);
            TimeSpan remaining = nextMidnight - now;
            int hours = Mathf.Max(0, (int)remaining.TotalHours);
            int mins = Mathf.Max(0, remaining.Minutes);

            summaryText.text = $"COMPLETED: {completedCount}/{missions.Count}   |   RESETS IN {hours:D2}h {mins:D2}m";
        }

        private void ClaimMissionBySlot(int slotIndex)
        {
            var missions = AvailableMissions;
            if (slotIndex < 0 || slotIndex >= missions.Count) return;
            ClaimMission(missions[slotIndex].id);
        }

        public void ClaimMission(string missionId)
        {
            var mission = AvailableMissions.Find(m => m.id == missionId) ?? MissionPool.Find(m => m.id == missionId);
            if (mission == null || IsMissionClaimed(missionId)) return;

            if (mission.rewardCoins > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(mission.rewardCoins);
            }

            SaveData.MarkDailyMissionClaimed(missionId);
            Debug.Log($"[MissionUI] Claimed daily mission reward for: {missionId} (+{mission.rewardCoins} Fish Coins)");
            AudioManager.Instance?.PlayStreak();
            Haptics.DoublePulse();
            RefreshUI();
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlayClick();
            Hide();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideMissions();
            }
        }

        public void RegisterMissionCard(Button claimBtn, Text claimBtnTxt, Text progressTxt, string missionId)
        {
            int slotIndex = claimButtons.Count;
            claimButtons.Add(claimBtn);
            claimButtonTexts.Add(claimBtnTxt);
            progressTexts.Add(progressTxt);

            if (claimBtn != null && claimBtn.transform.parent != null)
            {
                Transform card = claimBtn.transform.parent;
                cardTitleTexts.Add(card.Find("MissionTitle")?.GetComponent<Text>());
                cardDescTexts.Add(card.Find("MissionDesc")?.GetComponent<Text>());
            }
            else
            {
                cardTitleTexts.Add(null);
                cardDescTexts.Add(null);
            }

            if (claimBtn != null)
            {
                claimBtn.onClick.RemoveAllListeners();
                claimBtn.onClick.AddListener(() => ClaimMissionBySlot(slotIndex));
            }
        }
    }
}
