using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MyProject.UI
{
    public enum CosmeticCategory
    {
        Skins = 0,
        Hats = 1,
        Trails = 2
    }

    [System.Serializable]
    public class CatSkinItem
    {
        public string id;
        public string skinName;
        public Color tintColor;
        public int unlockScore;
        public int coinPrice;
        public string description;
        public CosmeticCategory category = CosmeticCategory.Skins;
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
            new CatSkinItem { id = "orange", skinName = "Classic Orange", tintColor = Color.white, unlockScore = 0, coinPrice = 0, description = "The classic nimble climber.", category = CosmeticCategory.Skins },
            new CatSkinItem { id = "pink", skinName = "Sakura Pink", tintColor = new Color(1f, 0.65f, 0.8f, 1f), unlockScore = 20, coinPrice = 30, description = "Cute & ultra bouncy cat.", category = CosmeticCategory.Skins },
            new CatSkinItem { id = "ninja", skinName = "Shadow Ninja", tintColor = new Color(0.35f, 0.35f, 0.45f, 1f), unlockScore = 50, coinPrice = 60, description = "Silent master of the night.", category = CosmeticCategory.Skins },
            new CatSkinItem { id = "gold", skinName = "Champion Gold", tintColor = new Color(1f, 0.84f, 0.1f, 1f), unlockScore = 100, coinPrice = 120, description = "Shines bright at high altitudes!", category = CosmeticCategory.Skins },
            new CatSkinItem { id = "cyan", skinName = "Cyber Neon", tintColor = new Color(0.2f, 0.9f, 1f, 1f), unlockScore = 200, coinPrice = 200, description = "Futuristic high-tech agility.", category = CosmeticCategory.Skins }
        };

        public static readonly List<CatSkinItem> AvailableHats = new List<CatSkinItem>
        {
            new CatSkinItem { id = "none", skinName = "No Hat", tintColor = new Color(0.35f, 0.40f, 0.52f, 1f), unlockScore = 0, coinPrice = 0, description = "Natural feline ears in the wind.", category = CosmeticCategory.Hats },
            new CatSkinItem { id = "party", skinName = "Party Cone Hat", tintColor = new Color(0.96f, 0.35f, 0.70f, 1f), unlockScore = 25, coinPrice = 40, description = "Festive polka-dot celebration cone!", category = CosmeticCategory.Hats },
            new CatSkinItem { id = "ninja_band", skinName = "Shinobi Headband", tintColor = new Color(0.90f, 0.20f, 0.25f, 1f), unlockScore = 60, coinPrice = 75, description = "Flowing crimson headband of agility.", category = CosmeticCategory.Hats },
            new CatSkinItem { id = "crown", skinName = "Royal Gold Crown", tintColor = new Color(1f, 0.84f, 0.16f, 1f), unlockScore = 120, coinPrice = 140, description = "Fit for the ruler of the rooftops!", category = CosmeticCategory.Hats },
            new CatSkinItem { id = "astro", skinName = "Space Cat Visor", tintColor = new Color(0.38f, 0.86f, 1f, 1f), unlockScore = 220, coinPrice = 220, description = "Pressurized bubble visor for orbit.", category = CosmeticCategory.Hats }
        };

        public static readonly List<CatSkinItem> AvailableTrails = new List<CatSkinItem>
        {
            new CatSkinItem { id = "none", skinName = "No Trail", tintColor = new Color(0.35f, 0.40f, 0.52f, 1f), unlockScore = 0, coinPrice = 0, description = "Stealthy steps with zero trace.", category = CosmeticCategory.Trails },
            new CatSkinItem { id = "sakura", skinName = "Sakura Petal Trail", tintColor = new Color(1f, 0.55f, 0.78f, 1f), unlockScore = 35, coinPrice = 50, description = "Soft pink blossom ribbon in flight.", category = CosmeticCategory.Trails },
            new CatSkinItem { id = "starlight", skinName = "Golden Starlight", tintColor = new Color(1f, 0.84f, 0.15f, 1f), unlockScore = 90, coinPrice = 110, description = "Radiant golden comet tail.", category = CosmeticCategory.Trails },
            new CatSkinItem { id = "cyber", skinName = "Cyber Neon Trail", tintColor = new Color(0.18f, 0.92f, 1f, 1f), unlockScore = 160, coinPrice = 180, description = "Electric cyan plasma stream.", category = CosmeticCategory.Trails }
        };

        private class CardView
        {
            public GameObject root;
            public Image previewIcon;
            public Text nameText;
            public Text descText;
            public Button equipButton;
            public Text statusText;
        }

        private readonly List<Button> skinEquipButtons = new List<Button>();
        private readonly List<Text> skinStatusTexts = new List<Text>();
        private readonly List<CardView> cardViews = new List<CardView>();

        private CosmeticCategory activeCategory = CosmeticCategory.Skins;
        private Button tabSkinsBtn;
        private Button tabHatsBtn;
        private Button tabTrailsBtn;
        private Text loadoutSummaryText;

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

            EnsureCosmeticTabsAndCards();
            RefreshUI();
        }

        public void Hide()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }
        }

        private void EnsureCosmeticTabsAndCards()
        {
            if (shopPanel == null) return;

            // Discover card views from ShopPanel children if not cached yet
            if (cardViews.Count == 0)
            {
                for (int i = 0; i < AvailableSkins.Count; i++)
                {
                    Transform cardTr = shopPanel.transform.Find($"SkinCard_{AvailableSkins[i].id}");
                    if (cardTr == null) continue;

                    CardView cv = new CardView
                    {
                        root = cardTr.gameObject,
                        previewIcon = cardTr.Find("ColorPreview")?.GetComponent<Image>(),
                        nameText = cardTr.Find("SkinName")?.GetComponent<Text>(),
                        descText = cardTr.Find("SkinDesc")?.GetComponent<Text>(),
                        equipButton = cardTr.Find("EquipButton")?.GetComponent<Button>(),
                        statusText = cardTr.Find("EquipButton")?.GetComponentInChildren<Text>()
                    };

                    // Shift cards slightly down to make room for the category tab bar
                    RectTransform rt = cardTr.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        float adjustedY = 0.69f - (i * 0.115f);
                        rt.anchorMin = new Vector2(0.5f, adjustedY);
                        rt.anchorMax = new Vector2(0.5f, adjustedY);
                        rt.sizeDelta = new Vector2(920f, 138f);
                    }

                    int slotIndex = cardViews.Count;
                    if (cv.equipButton != null)
                    {
                        cv.equipButton.onClick.RemoveAllListeners();
                        cv.equipButton.onClick.AddListener(() => OnCardSlotClicked(slotIndex));
                        if (cv.equipButton.GetComponent<ButtonPressFeedback>() == null)
                        {
                            cv.equipButton.gameObject.AddComponent<ButtonPressFeedback>();
                        }
                    }

                    cardViews.Add(cv);
                }
            }

            // Create Category Tab Bar (FUR SKINS | HATS | TRAILS) if not created yet
            if (tabSkinsBtn == null)
            {
                Transform existingTabs = shopPanel.transform.Find("CosmeticTabsBar");
                if (existingTabs == null)
                {
                    Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 24);

                    if (titleText != null)
                    {
                        RectTransform titleRt = titleText.GetComponent<RectTransform>();
                        if (titleRt != null)
                        {
                            titleRt.anchorMin = new Vector2(0.5f, 0.93f);
                            titleRt.anchorMax = new Vector2(0.5f, 0.93f);
                        }
                        titleText.text = "CAT SHOP & COSMETICS";
                    }

                    if (highScoreText != null)
                    {
                        RectTransform hsRt = highScoreText.GetComponent<RectTransform>();
                        if (hsRt != null)
                        {
                            hsRt.anchorMin = new Vector2(0.5f, 0.875f);
                            hsRt.anchorMax = new Vector2(0.5f, 0.875f);
                        }
                    }

                    // Loadout Summary Banner
                    GameObject loadoutGo = new GameObject("LoadoutSummaryText", typeof(RectTransform), typeof(Text));
                    loadoutGo.transform.SetParent(shopPanel.transform, false);
                    RectTransform loadoutRt = loadoutGo.GetComponent<RectTransform>();
                    loadoutRt.anchorMin = new Vector2(0.5f, 0.835f);
                    loadoutRt.anchorMax = new Vector2(0.5f, 0.835f);
                    loadoutRt.sizeDelta = new Vector2(920f, 36f);
                    loadoutSummaryText = loadoutGo.GetComponent<Text>();
                    loadoutSummaryText.font = font;
                    loadoutSummaryText.fontSize = 21;
                    loadoutSummaryText.fontStyle = FontStyle.Bold;
                    loadoutSummaryText.color = new Color(0.95f, 0.88f, 0.55f, 1f);
                    loadoutSummaryText.alignment = TextAnchor.MiddleCenter;
                    loadoutSummaryText.raycastTarget = false;

                    GameObject tabsBar = new GameObject("CosmeticTabsBar", typeof(RectTransform));
                    tabsBar.transform.SetParent(shopPanel.transform, false);
                    RectTransform barRt = tabsBar.GetComponent<RectTransform>();
                    barRt.anchorMin = new Vector2(0.5f, 0.78f);
                    barRt.anchorMax = new Vector2(0.5f, 0.78f);
                    barRt.sizeDelta = new Vector2(920f, 68f);

                    tabSkinsBtn = CreateTabButton(tabsBar.transform, font, "TabSkins", "FUR SKINS", new Vector2(0.18f, 0.5f));
                    tabHatsBtn = CreateTabButton(tabsBar.transform, font, "TabHats", "HATS (4)", new Vector2(0.50f, 0.5f));
                    tabTrailsBtn = CreateTabButton(tabsBar.transform, font, "TabTrails", "TRAILS (3)", new Vector2(0.82f, 0.5f));

                    tabSkinsBtn.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Skins));
                    tabHatsBtn.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Hats));
                    tabTrailsBtn.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Trails));
                }
            }
        }

        private Button CreateTabButton(Transform parent, Font font, string name, string label, Vector2 anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ButtonPressFeedback));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(285f, 64f);

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.20f, 0.24f, 0.36f, 1f);

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            Text txt = txtGo.GetComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            return btn;
        }

        public void SwitchCategory(CosmeticCategory category)
        {
            activeCategory = category;
            AudioManager.Instance?.PlayClick();
            Haptics.Light();
            RefreshUI();
        }

        private List<CatSkinItem> GetActiveItemList()
        {
            switch (activeCategory)
            {
                case CosmeticCategory.Hats: return AvailableHats;
                case CosmeticCategory.Trails: return AvailableTrails;
                case CosmeticCategory.Skins:
                default: return AvailableSkins;
            }
        }

        private string GetSelectedId(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.Hats:
                    return PlayerPrefs.GetString(PlayerController.SELECTED_HAT_KEY, "none");
                case CosmeticCategory.Trails:
                    return PlayerPrefs.GetString(PlayerController.SELECTED_TRAIL_KEY, "none");
                case CosmeticCategory.Skins:
                default:
                    return PlayerPrefs.GetString(PlayerController.SELECTED_SKIN_KEY, "orange");
            }
        }

        private string GetUnlockPrefKey(CosmeticCategory category, string id)
        {
            switch (category)
            {
                case CosmeticCategory.Hats: return "CatClimb_HatUnlocked_" + id;
                case CosmeticCategory.Trails: return "CatClimb_TrailUnlocked_" + id;
                case CosmeticCategory.Skins:
                default: return "CatClimb_SkinUnlocked_" + id;
            }
        }

        public void RefreshUI()
        {
            EnsureCosmeticTabsAndCards();

            int currentHighScore = GameManager.Instance != null ? GameManager.Instance.BestMeters : PlayerPrefs.GetInt(GameManager.BEST_METERS_KEY, 0);
            int totalCoins = GameManager.Instance != null ? GameManager.Instance.TotalCoins : PlayerPrefs.GetInt(GameManager.COINS_KEY, 0);

            if (highScoreText != null)
            {
                highScoreText.text = $"BEST: {currentHighScore}m   •   FISH COINS: {totalCoins}";
            }

            if (coinsText != null)
            {
                coinsText.text = $"FISH COINS: {totalCoins}";
            }

            if (loadoutSummaryText != null)
            {
                string skinId = GetSelectedId(CosmeticCategory.Skins);
                string hatId = GetSelectedId(CosmeticCategory.Hats);
                string trailId = GetSelectedId(CosmeticCategory.Trails);

                string skinTitle = AvailableSkins.Find(s => s.id == skinId)?.skinName ?? "Classic Orange";
                string hatTitle = AvailableHats.Find(h => h.id == hatId)?.skinName ?? "No Hat";
                string trailTitle = AvailableTrails.Find(t => t.id == trailId)?.skinName ?? "No Trail";

                loadoutSummaryText.text = $"EQUIPPED: {skinTitle}  •  {hatTitle}  •  {trailTitle}";
            }

            UpdateTabHighlight(tabSkinsBtn, activeCategory == CosmeticCategory.Skins);
            UpdateTabHighlight(tabHatsBtn, activeCategory == CosmeticCategory.Hats);
            UpdateTabHighlight(tabTrailsBtn, activeCategory == CosmeticCategory.Trails);

            List<CatSkinItem> items = GetActiveItemList();
            string selectedId = GetSelectedId(activeCategory);

            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cv = cardViews[i];
                if (cv == null || cv.root == null) continue;

                if (i >= items.Count)
                {
                    cv.root.SetActive(false);
                    continue;
                }

                cv.root.SetActive(true);
                CatSkinItem item = items[i];

                if (cv.nameText != null) cv.nameText.text = item.skinName;
                if (cv.descText != null) cv.descText.text = item.description;

                if (cv.previewIcon != null)
                {
                    if (activeCategory == CosmeticCategory.Hats && item.id != "none")
                    {
                        Sprite hatSp = PlayerController.GetOrCreateRuntimeHatSprite(item.id);
                        cv.previewIcon.sprite = hatSp;
                        cv.previewIcon.color = hatSp != null ? Color.white : item.tintColor;
                    }
                    else
                    {
                        cv.previewIcon.sprite = null;
                        cv.previewIcon.color = item.tintColor;
                    }
                }

                string unlockKey = GetUnlockPrefKey(activeCategory, item.id);
                bool isUnlocked = (item.unlockScore == 0 && item.coinPrice == 0)
                    || (item.unlockScore > 0 && currentHighScore >= item.unlockScore)
                    || (PlayerPrefs.GetInt(unlockKey, 0) == 1);
                bool isSelected = selectedId == item.id;

                if (cv.statusText != null)
                {
                    if (isSelected)
                    {
                        cv.statusText.text = "EQUIPPED";
                        cv.statusText.color = new Color(0.3f, 0.95f, 0.5f, 1f);
                    }
                    else if (isUnlocked)
                    {
                        cv.statusText.text = "EQUIP";
                        cv.statusText.color = Color.white;
                    }
                    else if (totalCoins >= item.coinPrice)
                    {
                        cv.statusText.text = $"BUY ({item.coinPrice})";
                        cv.statusText.color = new Color(1f, 0.85f, 0.3f, 1f);
                    }
                    else
                    {
                        cv.statusText.text = $"{item.coinPrice} FISH / {item.unlockScore}m";
                        cv.statusText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    }
                }

                if (cv.equipButton != null)
                {
                    bool canInteract = !isSelected && (isUnlocked || totalCoins >= item.coinPrice);
                    cv.equipButton.interactable = canInteract;
                }
            }
        }

        private void UpdateTabHighlight(Button tabBtn, bool isActive)
        {
            if (tabBtn == null) return;
            Image img = tabBtn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive
                    ? new Color(0.28f, 0.58f, 0.88f, 1f)
                    : new Color(0.16f, 0.19f, 0.28f, 0.95f);
            }
            Text txt = tabBtn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.color = isActive ? Color.white : new Color(0.70f, 0.76f, 0.88f, 1f);
            }
        }

        private void OnCardSlotClicked(int slotIndex)
        {
            List<CatSkinItem> items = GetActiveItemList();
            if (slotIndex < 0 || slotIndex >= items.Count) return;

            CatSkinItem item = items[slotIndex];
            OnCosmeticItemClicked(item);
        }

        private void OnCosmeticItemClicked(CatSkinItem item)
        {
            AudioManager.Instance?.PlayClick();
            Haptics.Light();

            int currentHighScore = GameManager.Instance != null ? GameManager.Instance.BestMeters : PlayerPrefs.GetInt(GameManager.BEST_METERS_KEY, 0);
            string unlockKey = GetUnlockPrefKey(item.category, item.id);
            bool isUnlocked = (item.unlockScore == 0 && item.coinPrice == 0)
                || (item.unlockScore > 0 && currentHighScore >= item.unlockScore)
                || (PlayerPrefs.GetInt(unlockKey, 0) == 1);

            if (isUnlocked)
            {
                EquipItem(item.category, item.id);
            }
            else if (GameManager.Instance != null && GameManager.Instance.TrySpendCoins(item.coinPrice))
            {
                PlayerPrefs.SetInt(unlockKey, 1);
                PlayerPrefs.Save();
                AudioManager.Instance?.PlayStreak();
                Haptics.DoublePulse();
                EquipItem(item.category, item.id);
            }
        }

        public void OnSkinCardClicked(string skinId, int coinPrice, int unlockScore)
        {
            CatSkinItem item = AvailableSkins.Find(s => s.id == skinId);
            if (item != null)
            {
                OnCosmeticItemClicked(item);
            }
        }

        private void EquipItem(CosmeticCategory category, string itemId)
        {
            switch (category)
            {
                case CosmeticCategory.Hats:
                    PlayerPrefs.SetString(PlayerController.SELECTED_HAT_KEY, itemId);
                    break;
                case CosmeticCategory.Trails:
                    PlayerPrefs.SetString(PlayerController.SELECTED_TRAIL_KEY, itemId);
                    break;
                case CosmeticCategory.Skins:
                default:
                    PlayerPrefs.SetString(PlayerController.SELECTED_SKIN_KEY, itemId);
                    break;
            }
            PlayerPrefs.Save();

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.ApplySelectedSkin();
            }
            else
            {
                PlayerController player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    player.ApplySelectedSkin();
                }
            }

            RefreshUI();
        }

        public void SelectSkin(string skinId)
        {
            EquipItem(CosmeticCategory.Skins, skinId);
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

            int slotIndex = skinEquipButtons.Count - 1;
            if (equipBtn != null)
            {
                equipBtn.onClick.RemoveAllListeners();
                equipBtn.onClick.AddListener(() => OnCardSlotClicked(slotIndex));
            }
        }
    }
}
