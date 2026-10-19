using UnityEngine;
using UnityEngine.UI;

namespace MyProject.UI
{
    /// <summary>
    /// Full Settings Modal for Music (BGM), Sound FX, Haptics (Vibration), Tilt Steering (Gyro),
    /// Tutorial Replay, and Game Credits. Self-healing on the Canvas.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        public static SettingsUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private Button musicToggleBtn;
        [SerializeField] private Text musicToggleText;
        [SerializeField] private Button musicVolBtn;
        [SerializeField] private Text musicVolText;
        [SerializeField] private Button sfxToggleBtn;
        [SerializeField] private Text sfxToggleText;
        [SerializeField] private Button sfxVolBtn;
        [SerializeField] private Text sfxVolText;
        [SerializeField] private Button hapticsToggleBtn;
        [SerializeField] private Text hapticsToggleText;
        [SerializeField] private Button tiltToggleBtn;
        [SerializeField] private Text tiltToggleText;
        [SerializeField] private Button replayTutorialBtn;
        [SerializeField] private Text replayTutorialText;
        [SerializeField] private Button closeBtn;

        private static readonly float[] VolumeSteps = { 0.25f, 0.50f, 0.75f, 1.00f };
        private float popAnimTimer = 0f;

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

        private void Update()
        {
            if (settingsPanel != null && settingsPanel.activeSelf && cardRect != null && popAnimTimer < 0.16f)
            {
                popAnimTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(popAnimTimer / 0.16f);
                float easeOutBack = 1f + 1.8f * Mathf.Pow(t - 1f, 3f) + 0.8f * Mathf.Pow(t - 1f, 2f);
                float scale = Mathf.LerpUnclamped(0.88f, 1.0f, easeOutBack);
                cardRect.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void Show()
        {
            EnsureUIHierarchy();
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                settingsPanel.transform.SetAsLastSibling();
            }

            if (cardRect != null)
            {
                popAnimTimer = 0f;
                cardRect.localScale = new Vector3(0.88f, 0.88f, 1f);
            }

            RefreshUI();
        }

        public void Hide()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void RefreshUI()
        {
            bool musicOn = AudioManager.Instance != null ? AudioManager.Instance.IsMusicEnabled : (PlayerPrefs.GetInt("CatClimb_MusicEnabled", 1) == 1);
            float musicVol = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : PlayerPrefs.GetFloat("CatClimb_MusicVolume", 0.65f);

            bool sfxOn = AudioManager.Instance != null ? AudioManager.Instance.IsAudioEnabled : (PlayerPrefs.GetInt("CatClimb_AudioEnabled", 1) == 1);
            float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : PlayerPrefs.GetFloat("CatClimb_SFXVolume", 1.0f);

            bool hapticsOn = Haptics.IsEnabled;
            bool tiltOn = PlayerController.Instance != null
                ? PlayerController.Instance.IsTiltControlEnabled
                : (PlayerPrefs.GetInt(PlayerController.TILT_PREF_KEY, 0) == 1);

            bool tutorialDone = PlayerPrefs.GetInt(TutorialUI.TUTORIAL_PREF_KEY, 0) == 1;

            ApplyToggleStyle(musicToggleBtn, musicToggleText, musicOn);
            if (musicVolText != null)
            {
                musicVolText.text = $"VOL {Mathf.RoundToInt(musicVol * 100f)}%";
            }

            ApplyToggleStyle(sfxToggleBtn, sfxToggleText, sfxOn);
            if (sfxVolText != null)
            {
                sfxVolText.text = $"VOL {Mathf.RoundToInt(sfxVol * 100f)}%";
            }

            ApplyToggleStyle(hapticsToggleBtn, hapticsToggleText, hapticsOn);
            ApplyToggleStyle(tiltToggleBtn, tiltToggleText, tiltOn);

            if (replayTutorialText != null)
            {
                replayTutorialText.text = tutorialDone ? "REPLAY TUTORIAL" : "TUTORIAL QUEUED ✓";
                replayTutorialText.color = tutorialDone ? Color.white : new Color(0.4f, 0.95f, 0.6f, 1f);
            }
        }

        private void ApplyToggleStyle(Button btn, Text txt, bool isOn)
        {
            if (txt != null)
            {
                txt.text = isOn ? "ON" : "OFF";
                txt.color = Color.white;
            }
            if (btn != null)
            {
                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isOn
                        ? new Color(0.20f, 0.76f, 0.44f, 1f)
                        : new Color(0.32f, 0.36f, 0.46f, 1f);
                }
            }
        }

        private void OnMusicToggleClicked()
        {
            AudioManager.Instance?.ToggleMusic();
            AudioManager.Instance?.PlayClick();
            Haptics.Light();
            RefreshUI();
        }

        private void OnMusicVolClicked()
        {
            if (AudioManager.Instance == null) return;
            float nextVol = GetNextVolumeStep(AudioManager.Instance.MusicVolume);
            AudioManager.Instance.SetMusicVolume(nextVol);
            if (!AudioManager.Instance.IsMusicEnabled)
            {
                AudioManager.Instance.SetMusicEnabled(true);
            }
            AudioManager.Instance.PlayClick();
            Haptics.Light();
            RefreshUI();
        }

        private void OnSfxToggleClicked()
        {
            AudioManager.Instance?.ToggleAudio();
            AudioManager.Instance?.PlayClick();
            Haptics.Light();
            RefreshUI();
        }

        private void OnSfxVolClicked()
        {
            if (AudioManager.Instance == null) return;
            float nextVol = GetNextVolumeStep(AudioManager.Instance.SFXVolume);
            AudioManager.Instance.SetSFXVolume(nextVol);
            if (!AudioManager.Instance.IsAudioEnabled)
            {
                AudioManager.Instance.SetAudioEnabled(true);
            }
            AudioManager.Instance.PlayClick();
            Haptics.Light();
            RefreshUI();
        }

        private float GetNextVolumeStep(float current)
        {
            for (int i = 0; i < VolumeSteps.Length; i++)
            {
                if (current < VolumeSteps[i] - 0.05f)
                {
                    return VolumeSteps[i];
                }
            }
            return VolumeSteps[0];
        }

        private void OnHapticsToggleClicked()
        {
            Haptics.IsEnabled = !Haptics.IsEnabled;
            AudioManager.Instance?.PlayClick();
            if (Haptics.IsEnabled) Haptics.Medium();
            RefreshUI();
        }

        private void OnTiltToggleClicked()
        {
            bool currentlyOn = PlayerController.Instance != null
                ? PlayerController.Instance.IsTiltControlEnabled
                : (PlayerPrefs.GetInt(PlayerController.TILT_PREF_KEY, 0) == 1);
            bool nextState = !currentlyOn;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetTiltControlEnabled(nextState);
            }
            else
            {
                PlayerPrefs.SetInt(PlayerController.TILT_PREF_KEY, nextState ? 1 : 0);
                PlayerPrefs.Save();
            }

            AudioManager.Instance?.PlayClick();
            Haptics.Light();
            RefreshUI();
        }

        private void OnReplayTutorialClicked()
        {
            PlayerPrefs.SetInt(TutorialUI.TUTORIAL_PREF_KEY, 0);
            PlayerPrefs.Save();
            AudioManager.Instance?.PlayClick();
            Haptics.Light();
            RefreshUI();
        }

        private void OnCloseClicked()
        {
            AudioManager.Instance?.PlayClick();
            Haptics.Light();
            Hide();
        }

        private void EnsureUIHierarchy()
        {
            if (settingsPanel != null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 28);

            settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            settingsPanel.transform.SetParent(transform, false);
            RectTransform overlayRect = settingsPanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            settingsPanel.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.08f, 0.88f);

            GameObject cardGo = new GameObject("SettingsCard", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(settingsPanel.transform, false);
            cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(880f, 1060f);
            cardGo.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.24f, 0.98f);

            CreateLabel(cardGo.transform, "HeaderTitle", "SETTINGS", font, 52, FontStyle.Bold,
                new Color(1f, 0.88f, 0.32f, 1f), new Vector2(0.5f, 0.91f), new Vector2(720f, 70f));

            CreateLabel(cardGo.transform, "HeaderSub", "AUDIO, CONTROLS & PREFERENCES", font, 22, FontStyle.Bold,
                new Color(0.72f, 0.82f, 0.96f, 1f), new Vector2(0.5f, 0.85f), new Vector2(720f, 40f));

            // Row 1: Music (BGM)
            CreateSettingRowWithVolume(cardGo.transform, font, "RowMusic", "BGM MUSIC", 0.74f,
                out musicToggleBtn, out musicToggleText, out musicVolBtn, out musicVolText);
            musicToggleBtn.onClick.AddListener(OnMusicToggleClicked);
            musicVolBtn.onClick.AddListener(OnMusicVolClicked);

            // Row 2: Sound FX
            CreateSettingRowWithVolume(cardGo.transform, font, "RowSFX", "SOUND FX", 0.61f,
                out sfxToggleBtn, out sfxToggleText, out sfxVolBtn, out sfxVolText);
            sfxToggleBtn.onClick.AddListener(OnSfxToggleClicked);
            sfxVolBtn.onClick.AddListener(OnSfxVolClicked);

            // Row 3: Haptics
            CreateSettingRowSingle(cardGo.transform, font, "RowHaptics", "VIBRATION / HAPTICS", 0.48f,
                out hapticsToggleBtn, out hapticsToggleText);
            hapticsToggleBtn.onClick.AddListener(OnHapticsToggleClicked);

            // Row 4: Tilt / Gyro Steering
            CreateSettingRowSingle(cardGo.transform, font, "RowTilt", "TILT STEERING (GYRO)", 0.35f,
                out tiltToggleBtn, out tiltToggleText);
            tiltToggleBtn.onClick.AddListener(OnTiltToggleClicked);

            // Replay Tutorial Button
            replayTutorialBtn = CreateActionBtn(cardGo.transform, font, "ReplayTutorialBtn", "REPLAY TUTORIAL",
                new Vector2(0.5f, 0.22f), new Vector2(740f, 80f), new Color(0.22f, 0.32f, 0.52f, 1f), out replayTutorialText);
            replayTutorialBtn.onClick.AddListener(OnReplayTutorialClicked);

            // Credits text
            CreateLabel(cardGo.transform, "CreditsText", "CAT CLIMB v1.0  •  STUDIO PAWPRINT", font, 20, FontStyle.Bold,
                new Color(0.55f, 0.64f, 0.78f, 0.85f), new Vector2(0.5f, 0.14f), new Vector2(760f, 36f));

            // Close Button
            closeBtn = CreateActionBtn(cardGo.transform, font, "CloseSettingsBtn", "DONE",
                new Vector2(0.5f, 0.065f), new Vector2(440f, 84f), new Color(0.95f, 0.38f, 0.42f, 1f), out _);
            closeBtn.onClick.AddListener(OnCloseClicked);

            settingsPanel.SetActive(false);
        }

        private void CreateSettingRowWithVolume(Transform parent, Font font, string name, string title, float anchorY,
            out Button toggleBtn, out Text toggleTxt, out Button volBtn, out Text volTxt)
        {
            GameObject rowGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            rowGo.transform.SetParent(parent, false);
            RectTransform rt = rowGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, anchorY);
            rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.sizeDelta = new Vector2(780f, 104f);
            rowGo.GetComponent<Image>().color = new Color(0.17f, 0.21f, 0.32f, 0.96f);

            Text label = CreateLabel(rowGo.transform, "Label", title, font, 28, FontStyle.Bold,
                Color.white, new Vector2(0.27f, 0.5f), new Vector2(360f, 60f));
            label.alignment = TextAnchor.MiddleLeft;

            volBtn = CreateActionBtn(rowGo.transform, font, "VolBtn", "VOL 100%",
                new Vector2(0.62f, 0.5f), new Vector2(180f, 70f), new Color(0.25f, 0.34f, 0.52f, 1f), out volTxt);
            volTxt.fontSize = 22;

            toggleBtn = CreateActionBtn(rowGo.transform, font, "ToggleBtn", "ON",
                new Vector2(0.86f, 0.5f), new Vector2(150f, 70f), new Color(0.20f, 0.76f, 0.44f, 1f), out toggleTxt);
            toggleTxt.fontSize = 26;
        }

        private void CreateSettingRowSingle(Transform parent, Font font, string name, string title, float anchorY,
            out Button toggleBtn, out Text toggleTxt)
        {
            GameObject rowGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            rowGo.transform.SetParent(parent, false);
            RectTransform rt = rowGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, anchorY);
            rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.sizeDelta = new Vector2(780f, 104f);
            rowGo.GetComponent<Image>().color = new Color(0.17f, 0.21f, 0.32f, 0.96f);

            Text label = CreateLabel(rowGo.transform, "Label", title, font, 28, FontStyle.Bold,
                Color.white, new Vector2(0.35f, 0.5f), new Vector2(480f, 60f));
            label.alignment = TextAnchor.MiddleLeft;

            toggleBtn = CreateActionBtn(rowGo.transform, font, "ToggleBtn", "ON",
                new Vector2(0.82f, 0.5f), new Vector2(210f, 70f), new Color(0.20f, 0.76f, 0.44f, 1f), out toggleTxt);
            toggleTxt.fontSize = 26;
        }

        private Button CreateActionBtn(Transform parent, Font font, string name, string label, Vector2 anchor, Vector2 size, Color bgColor, out Text btnText)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ButtonPressFeedback));
            btnGo.transform.SetParent(parent, false);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.color = bgColor;

            Button btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;

            btnText = CreateLabel(btnGo.transform, "Label", label, font, 28, FontStyle.Bold, Color.white, new Vector2(0.5f, 0.5f), size);
            return btn;
        }

        private Text CreateLabel(Transform parent, string name, string content, Font font, int size, FontStyle style, Color color, Vector2 anchor, Vector2 dims)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = dims;

            Text txt = go.GetComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }
    }
}
