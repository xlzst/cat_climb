using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MyProject.UI
{
    [System.Serializable]
    public class MissionItem
    {
        public string id;
        public string title;
        public string description;
        public int targetValue;
        public string rewardText;
        public MissionType type;
    }

    public enum MissionType
    {
        ReachHeight,
        PlayRuns,
        DefeatEnemies
    }

    public class MissionUI : MonoBehaviour
    {
        [Header("UI Panel References")]
        [SerializeField] private GameObject missionPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button backButton;

        public static readonly List<MissionItem> AvailableMissions = new List<MissionItem>
        {
            new MissionItem { id = "climb_30", title = "First Climb", description = "Reach 30 FT in height", targetValue = 30, rewardText = "50 Coins", type = MissionType.ReachHeight },
            new MissionItem { id = "climb_100", title = "High Climber", description = "Reach 100 FT in height", targetValue = 100, rewardText = "100 Coins", type = MissionType.ReachHeight },
            new MissionItem { id = "climb_250", title = "Sky Master", description = "Reach 250 FT in height", targetValue = 250, rewardText = "Golden Paw", type = MissionType.ReachHeight },
            new MissionItem { id = "play_3", title = "Persistent Cat", description = "Play 3 climb runs", targetValue = 3, rewardText = "75 Coins", type = MissionType.PlayRuns },
            new MissionItem { id = "defeat_3", title = "Mouse Hunter", description = "Defeat 3 Enemy Mice", targetValue = 3, rewardText = "Hunter Ribbon", type = MissionType.DefeatEnemies }
        };

        private List<Button> claimButtons = new List<Button>();
        private List<Text> claimButtonTexts = new List<Text>();
        private List<Text> progressTexts = new List<Text>();

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
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
            int highScore = GameManager.Instance != null ? GameManager.Instance.HighScore : PlayerPrefs.GetInt(GameManager.HIGH_SCORE_KEY, 0);
            int totalRuns = PlayerPrefs.GetInt("CatClimb_TotalRuns", 0);
            int defeatedEnemies = PlayerPrefs.GetInt("CatClimb_DefeatedEnemies", 0);

            int completedCount = 0;

            for (int i = 0; i < AvailableMissions.Count; i++)
            {
                var mission = AvailableMissions[i];
                int currentProgress = 0;

                switch (mission.type)
                {
                    case MissionType.ReachHeight:
                        currentProgress = highScore;
                        break;
                    case MissionType.PlayRuns:
                        currentProgress = totalRuns;
                        break;
                    case MissionType.DefeatEnemies:
                        currentProgress = defeatedEnemies;
                        break;
                }

                bool isClaimed = PlayerPrefs.GetInt($"CatClimb_Mission_Claimed_{mission.id}", 0) == 1;
                bool isCompleted = currentProgress >= mission.targetValue;

                if (isClaimed)
                {
                    completedCount++;
                }

                if (i < progressTexts.Count && progressTexts[i] != null)
                {
                    int displayProgress = Mathf.Min(currentProgress, mission.targetValue);
                    progressTexts[i].text = $"{displayProgress} / {mission.targetValue}";
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
                        claimButtonTexts[i].text = "CLAIM!";
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

            if (summaryText != null)
            {
                summaryText.text = $"COMPLETED: {completedCount} / {AvailableMissions.Count} MISSIONS";
            }
        }

        public void ClaimMission(string missionId)
        {
            PlayerPrefs.SetInt($"CatClimb_Mission_Claimed_{missionId}", 1);
            PlayerPrefs.Save();
            Debug.Log($"[MissionUI] Claimed mission reward for: {missionId}");
            RefreshUI();
        }

        private void OnBackClicked()
        {
            Hide();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideMissions();
            }
        }

        public void RegisterMissionCard(Button claimBtn, Text claimBtnTxt, Text progressTxt, string missionId)
        {
            claimButtons.Add(claimBtn);
            claimButtonTexts.Add(claimBtnTxt);
            progressTexts.Add(progressTxt);

            if (claimBtn != null)
            {
                claimBtn.onClick.AddListener(() => ClaimMission(missionId));
            }
        }
    }
}
