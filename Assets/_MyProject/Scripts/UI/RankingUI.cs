using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace MyProject.UI
{
    [System.Serializable]
    public class RankingEntry
    {
        public string playerName;
        public int score;
        public bool isUser;

        public RankingEntry(string name, int scoreVal, bool user = false)
        {
            playerName = name;
            score = scoreVal;
            isUser = user;
        }
    }

    public class RankingUI : MonoBehaviour
    {
        [Header("UI Panel References")]
        [SerializeField] private GameObject rankingPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text userRankText;
        [SerializeField] private Button backButton;

        private List<Text> rankNumTexts = new List<Text>();
        private List<Text> nameTexts = new List<Text>();
        private List<Text> scoreTexts = new List<Text>();
        private List<Image> cardBackgrounds = new List<Image>();

        private readonly List<RankingEntry> baseBots = new List<RankingEntry>
        {
            new RankingEntry("SuperCat_99", 520),
            new RankingEntry("NyankoMaster", 410),
            new RankingEntry("WhiskerKnight", 320),
            new RankingEntry("ShadowNeko", 240),
            new RankingEntry("CozyTabby", 170),
            new RankingEntry("CyberPaw", 120),
            new RankingEntry("FluffyClimber", 80),
            new RankingEntry("MiniKitten", 40)
        };

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        public void Show()
        {
            if (rankingPanel != null)
            {
                rankingPanel.SetActive(true);
            }

            RefreshUI();
        }

        public void Hide()
        {
            if (rankingPanel != null)
            {
                rankingPanel.SetActive(false);
            }
        }

        public void RefreshUI()
        {
            int userHighScore = GameManager.Instance != null ? GameManager.Instance.HighScore : PlayerPrefs.GetInt(GameManager.HIGH_SCORE_KEY, 0);

            // Construct leaderboard list including user
            List<RankingEntry> leaderboard = new List<RankingEntry>(baseBots);
            RankingEntry userEntry = new RankingEntry("YOU (Your Cat)", userHighScore, true);
            leaderboard.Add(userEntry);

            // Sort descending by score
            leaderboard = leaderboard.OrderByDescending(e => e.score).ToList();

            // Find user rank (1-indexed)
            int userRank = leaderboard.IndexOf(userEntry) + 1;

            if (userRankText != null)
            {
                userRankText.text = $"YOUR RANK: #{userRank}  (BEST: {userHighScore} FT)";
            }

            int cardCount = rankNumTexts.Count;
            for (int i = 0; i < cardCount; i++)
            {
                if (i < leaderboard.Count)
                {
                    var entry = leaderboard[i];
                    int rankPos = i + 1;

                    if (rankNumTexts[i] != null)
                    {
                        if (rankPos == 1)
                        {
                            rankNumTexts[i].text = "#1 🥇";
                            rankNumTexts[i].color = new Color(1f, 0.85f, 0.2f, 1f);
                        }
                        else if (rankPos == 2)
                        {
                            rankNumTexts[i].text = "#2 🥈";
                            rankNumTexts[i].color = new Color(0.85f, 0.88f, 0.92f, 1f);
                        }
                        else if (rankPos == 3)
                        {
                            rankNumTexts[i].text = "#3 🥉";
                            rankNumTexts[i].color = new Color(0.9f, 0.55f, 0.35f, 1f);
                        }
                        else
                        {
                            rankNumTexts[i].text = $"#{rankPos}";
                            rankNumTexts[i].color = new Color(0.75f, 0.8f, 0.85f, 1f);
                        }
                    }

                    if (nameTexts[i] != null)
                    {
                        nameTexts[i].text = entry.playerName;
                        nameTexts[i].color = entry.isUser ? new Color(1f, 0.9f, 0.3f, 1f) : Color.white;
                    }

                    if (scoreTexts[i] != null)
                    {
                        scoreTexts[i].text = $"{entry.score} FT";
                        scoreTexts[i].color = entry.isUser ? new Color(0.3f, 0.95f, 0.8f, 1f) : new Color(0.8f, 0.85f, 0.9f, 1f);
                    }

                    if (i < cardBackgrounds.Count && cardBackgrounds[i] != null)
                    {
                        if (entry.isUser)
                        {
                            cardBackgrounds[i].color = new Color(0.22f, 0.28f, 0.42f, 0.98f);
                        }
                        else
                        {
                            cardBackgrounds[i].color = new Color(0.14f, 0.16f, 0.23f, 0.95f);
                        }
                    }
                }
            }
        }

        private void OnBackClicked()
        {
            Hide();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideRanking();
            }
        }

        public void RegisterRankingCard(Text rankTxt, Text nameTxt, Text scoreTxt, Image bgImg)
        {
            rankNumTexts.Add(rankTxt);
            nameTexts.Add(nameTxt);
            scoreTexts.Add(scoreTxt);
            cardBackgrounds.Add(bgImg);
        }
    }
}
