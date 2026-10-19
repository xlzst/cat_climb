using UnityEngine;
using MyProject.UI;

namespace MyProject
{
    /// <summary>
    /// Displays a celebratory visual marker line in the world at the player's personal best altitude.
    /// When the player surpasses their best height during a run, triggers celebratory fanfare and haptic feedback.
    /// </summary>
    public class BestHeightMarker : MonoBehaviour
    {
        public static BestHeightMarker Instance { get; private set; }

        private GameObject lineObject;
        private SpriteRenderer lineRenderer;
        private TextMesh labelTextMesh;
        private float markerAltitudeMeters = 0f;
        private float markerWorldY = 0f;
        private bool hasPassedRecord = false;
        private bool isInitialized = false;

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
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
            InitializeMarker();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
            if (Instance == this) Instance = null;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                hasPassedRecord = false;
                InitializeMarker();
            }
        }

        public void InitializeMarker()
        {
            int bestMeters = GameManager.Instance != null ? GameManager.Instance.BestMeters : 0;
            markerAltitudeMeters = bestMeters;

            // Don't show marker if player has no previous climb record (<= 5m)
            if (markerAltitudeMeters < 5f)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            float initialY = (PlayerController.Instance != null) ? PlayerController.Instance.transform.position.y : 0f;
            markerWorldY = initialY + markerAltitudeMeters;
            transform.position = new Vector3(0f, markerWorldY, 0f);

            if (!isInitialized)
            {
                isInitialized = true;
                BuildVisuals();
            }
            else
            {
                if (labelTextMesh != null)
                {
                    labelTextMesh.text = $"BEST: {Mathf.RoundToInt(markerAltitudeMeters)}m";
                    labelTextMesh.color = new Color(1f, 0.9f, 0.35f, 1f);
                }
                if (lineRenderer != null)
                {
                    lineRenderer.color = new Color(1f, 0.85f, 0.2f, 0.85f);
                }
            }
        }

        private void BuildVisuals()
        {
            // 1. Horizontal dashed line across the climbing corridor
            lineObject = new GameObject("RecordLine");
            lineObject.transform.SetParent(transform, false);
            lineRenderer = lineObject.AddComponent<SpriteRenderer>();

            Texture2D lineTex = CreateDashedLineTexture(128, 8);
            Sprite lineSprite = Sprite.Create(lineTex, new Rect(0, 0, 128, 8), new Vector2(0.5f, 0.5f), 100f);
            lineRenderer.sprite = lineSprite;
            lineRenderer.color = new Color(1f, 0.85f, 0.2f, 0.85f); // Golden amber
            lineRenderer.sortingOrder = 3;
            lineObject.transform.localScale = new Vector3(4.2f, 1.2f, 1f);

            // 2. World text label showing "★ BEST: 120m"
            GameObject labelGo = new GameObject("RecordLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.28f, 0f);

            labelTextMesh = labelGo.AddComponent<TextMesh>();
            labelTextMesh.text = $"BEST: {Mathf.RoundToInt(markerAltitudeMeters)}m";
            labelTextMesh.fontSize = 24;
            labelTextMesh.characterSize = 0.08f;
            labelTextMesh.color = new Color(1f, 0.9f, 0.35f, 1f);
            labelTextMesh.anchor = TextAnchor.MiddleCenter;
            labelTextMesh.alignment = TextAlignment.Center;
            labelTextMesh.fontStyle = FontStyle.Bold;

            MeshRenderer mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 4;
            }
        }

        private Texture2D CreateDashedLineTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color yellow = new Color(1f, 0.85f, 0.2f, 1f);
            Color transparent = Color.clear;

            for (int x = 0; x < width; x++)
            {
                bool isDash = (x % 16) < 10;
                Color col = isDash ? yellow : transparent;
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        private void Update()
        {
            if (hasPassedRecord) return;

            PlayerController player = PlayerController.Instance;
            if (player != null && player.transform.position.y > markerWorldY)
            {
                hasPassedRecord = true;
                TriggerCelebration();
            }
        }

        private void TriggerCelebration()
        {
            UIManager.Instance?.ShowToast("NEW BEST RECORD!", new Color(1f, 0.88f, 0.2f, 1f));
            AudioManager.Instance?.PlayStreak();
            CameraFollowTarget.Instance?.AddTrauma(0.25f);
            Haptics.DoublePulse();

            // Fade out the line so it doesn't distract the player as they climb higher
            StartCoroutine(FadeOutRoutine());
        }

        private System.Collections.IEnumerator FadeOutRoutine()
        {
            float elapsed = 0f;
            float duration = 1.2f;
            Color baseLineCol = lineRenderer != null ? lineRenderer.color : Color.yellow;
            Color baseTxtCol = labelTextMesh != null ? labelTextMesh.color : Color.yellow;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

                if (lineRenderer != null)
                {
                    lineRenderer.color = new Color(baseLineCol.r, baseLineCol.g, baseLineCol.b, alpha * baseLineCol.a);
                }
                if (labelTextMesh != null)
                {
                    labelTextMesh.color = new Color(baseTxtCol.r, baseTxtCol.g, baseTxtCol.b, alpha);
                }
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
