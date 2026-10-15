using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MyProject.UI
{
    [System.Serializable]
    public class CatSkinItem
    {
        public string id;
        public string skinName;
        public Color tintColor;
        public int unlockScore;
        public int coinPrice;
        public string description;
    }

    public class ShopUI : MonoBehaviour
    {
        [Header("UI Panel References")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Button backButton;

        public static readonly List<CatSkinItem> AvailableSkins = new List<CatSkinItem>
        {
            new CatSkinItem { id = "orange", skinName = "Classic Orange", tintColor = Color.white, unlockScore = 0, coinPrice = 0, description = "The classic nimble climber." },
            new CatSkinItem { id = "pink", skinName = "Sakura Pink", tintColor = new Color(1f, 0.65f, 0.8f, 1f), unlockScore = 20, coinPrice = 30, description = "Cute & ultra bouncy cat." },
            new CatSkinItem { id = "ninja", skinName = "Shadow Ninja", tintColor = new Color(0.35f, 0.35f, 0.45f, 1f), unlockScore = 50, coinPrice = 60, description = "Silent master of the night." },
            new CatSkinItem { id = "gold", skinName = "Champion Gold", tintColor = new Color(1f, 0.84f, 0.1f, 1f), unlockScore = 100, coinPrice = 120, description = "Shines bright at high altitudes!" },
            new CatSkinItem { id = "cyan", skinName = "Cyber Neon", tintColor = new Color(0.2f, 0.9f, 1f, 1f), unlockScore = 200, coinPrice = 200, description = "Futuristic high-tech agility." }
        };

        private List<Button> skinEquipButtons = new List<Button>();
        private List<Text> skinStatusTexts = new List<Text>();

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        public void Show()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
            }

            RefreshUI();
        }

        public void Hide()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }
        }

        public void RefreshUI()
        {
            int currentHighScore = GameManager.Instance != null ? GameManager.Instance.HighScore : PlayerPrefs.GetInt(GameManager.HIGH_SCORE_KEY, 0);
            int totalCoins = GameManager.Instance != null ? GameManager.Instance.TotalCoins : PlayerPrefs.GetInt(GameManager.COINS_KEY, 0);
            string selectedSkinId = PlayerPrefs.GetString("CatClimb_SelectedSkin", "orange");

            if (highScoreText != null)
            {
                highScoreText.text = $"BEST RECORD: {currentHighScore} FT";
            }

            if (coinsText != null)
            {
                coinsText.text = $"FISH COINS: {totalCoins}";
            }

            for (int i = 0; i < AvailableSkins.Count; i++)
            {
                var skin = AvailableSkins[i];
                bool isUnlocked = (skin.unlockScore == 0) || (currentHighScore >= skin.unlockScore) || (PlayerPrefs.GetInt("CatClimb_SkinUnlocked_" + skin.id, 0) == 1);
                bool isSelected = selectedSkinId == skin.id;

                if (i < skinStatusTexts.Count && skinStatusTexts[i] != null)
                {
                    if (isSelected)
                    {
                        skinStatusTexts[i].text = "EQUIPPED";
                        skinStatusTexts[i].color = new Color(0.3f, 0.95f, 0.5f, 1f);
                    }
                    else if (isUnlocked)
                    {
                        skinStatusTexts[i].text = "EQUIP";
                        skinStatusTexts[i].color = Color.white;
                    }
                    else if (totalCoins >= skin.coinPrice)
                    {
                        skinStatusTexts[i].text = $"BUY ({skin.coinPrice})";
                        skinStatusTexts[i].color = new Color(1f, 0.85f, 0.3f, 1f);
                    }
                    else
                    {
                        skinStatusTexts[i].text = $"{skin.coinPrice} FISH / {skin.unlockScore}FT";
                        skinStatusTexts[i].color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    }
                }

                if (i < skinEquipButtons.Count && skinEquipButtons[i] != null)
                {
                    bool canInteract = !isSelected && (isUnlocked || totalCoins >= skin.coinPrice);
                    skinEquipButtons[i].interactable = canInteract;
                }
            }
        }

        public void OnSkinCardClicked(string skinId, int coinPrice, int unlockScore)
        {
            AudioManager.Instance?.PlayClick();
            int currentHighScore = GameManager.Instance != null ? GameManager.Instance.HighScore : PlayerPrefs.GetInt(GameManager.HIGH_SCORE_KEY, 0);
            bool isUnlocked = (unlockScore == 0) || (currentHighScore >= unlockScore) || (PlayerPrefs.GetInt("CatClimb_SkinUnlocked_" + skinId, 0) == 1);

            if (isUnlocked)
            {
                SelectSkin(skinId);
            }
            else
            {
                // Try purchase with Fish Coins
                if (GameManager.Instance != null && GameManager.Instance.TrySpendCoins(coinPrice))
                {
                    PlayerPrefs.SetInt("CatClimb_SkinUnlocked_" + skinId, 1);
                    PlayerPrefs.Save();
                    Debug.Log($"[ShopUI] Successfully purchased skin {skinId} with {coinPrice} Fish Coins!");
                    AudioManager.Instance?.PlayStreak();
                    SelectSkin(skinId);
                }
            }
        }

        public void SelectSkin(string skinId)
        {
            PlayerPrefs.SetString("CatClimb_SelectedSkin", skinId);
            PlayerPrefs.Save();

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.ApplySelectedSkin();
            }

            RefreshUI();
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlayClick();
            Hide();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideShop();
            }
        }

        public void RegisterSkinCard(Button equipBtn, Text statusTxt, string skinId)
        {
            skinEquipButtons.Add(equipBtn);
            skinStatusTexts.Add(statusTxt);

            var skin = AvailableSkins.Find(s => s.id == skinId);
            int price = skin != null ? skin.coinPrice : 50;
            int unlock = skin != null ? skin.unlockScore : 0;

            if (equipBtn != null)
            {
                equipBtn.onClick.AddListener(() => OnSkinCardClicked(skinId, price, unlock));
            }
        }
    }
}
