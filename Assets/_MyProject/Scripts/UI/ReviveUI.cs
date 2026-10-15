using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MyProject.UI
{
    public class ReviveUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject revivePanel;
        [SerializeField] private Text countdownText;
        [SerializeField] private Button coinReviveButton;
        [SerializeField] private Text coinReviveButtonText;
        [SerializeField] private Button adReviveButton;
        [SerializeField] private Button skipButton;

        [Header("Settings")]
        [SerializeField] private int reviveCoinCost = 20;
        [SerializeField] private float countdownDuration = 5f;

        private Coroutine countdownCoroutine;

        private void Awake()
        {
            BindButtonListeners();
        }

        private void BindButtonListeners()
        {
            if (coinReviveButton != null)
            {
                coinReviveButton.onClick.RemoveListener(OnCoinReviveClicked);
                coinReviveButton.onClick.AddListener(OnCoinReviveClicked);
            }
            if (adReviveButton != null)
            {
                adReviveButton.onClick.RemoveListener(OnAdReviveClicked);
                adReviveButton.onClick.AddListener(OnAdReviveClicked);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(OnSkipClicked);
                skipButton.onClick.AddListener(OnSkipClicked);
            }
        }

        public void Init(GameObject panel, Text cdText, Button coinBtn, Text coinBtnTxt, Button adBtn, Button skipBtn)
        {
            revivePanel = panel;
            countdownText = cdText;
            coinReviveButton = coinBtn;
            coinReviveButtonText = coinBtnTxt;
            adReviveButton = adBtn;
            skipButton = skipBtn;

            BindButtonListeners();
        }

        public void Show()
        {
            if (revivePanel == null)
            {
                revivePanel = gameObject;
            }

            revivePanel.SetActive(true);

            int currentCoins = GameManager.Instance != null ? GameManager.Instance.TotalCoins : 0;
            if (coinReviveButtonText != null)
            {
                coinReviveButtonText.text = $"REVIVE ({reviveCoinCost} FISH)";
            }
            if (coinReviveButton != null)
            {
                coinReviveButton.interactable = currentCoins >= reviveCoinCost;
            }

            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
            }
            countdownCoroutine = StartCoroutine(CountdownRoutine());
        }

        public void Hide()
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            if (revivePanel == null)
            {
                revivePanel = gameObject;
            }

            revivePanel.SetActive(false);
        }

        private IEnumerator CountdownRoutine()
        {
            float remaining = countdownDuration;
            while (remaining > 0f)
            {
                if (countdownText != null)
                {
                    countdownText.text = Mathf.CeilToInt(remaining).ToString();
                }
                yield return new WaitForSecondsRealtime(0.1f);
                remaining -= 0.1f;
            }

            // Time expired without revive -> confirm game over
            OnSkipClicked();
        }

        private void OnCoinReviveClicked()
        {
            AudioManager.Instance?.PlayClick();
            if (GameManager.Instance != null && GameManager.Instance.TrySpendCoins(reviveCoinCost))
            {
                Hide();
                GameManager.Instance.RevivePlayer();
            }
        }

        private void OnAdReviveClicked()
        {
            AudioManager.Instance?.PlayClick();
            Debug.Log("[ReviveUI] Watched rewarded video ad -> Free revive granted!");
            Hide();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RevivePlayer();
            }
        }

        private void OnSkipClicked()
        {
            AudioManager.Instance?.PlayClick();
            Hide();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ConfirmGameOver();
            }
        }
    }
}
