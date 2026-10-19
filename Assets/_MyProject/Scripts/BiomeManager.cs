using UnityEngine;

namespace MyProject
{
    public class BiomeManager : MonoBehaviour
    {
        public static BiomeManager Instance { get; private set; }

        [System.Serializable]
        public struct BiomeTier
        {
            public string name;
            public float minAltitude;
            public Color skyColor;
            public string subtitle;
        }

        [Header("Biome Altitude Tiers")]
        [SerializeField]
        private BiomeTier[] biomes = new BiomeTier[]
        {
            new BiomeTier { name = "ALLEY DAWN", minAltitude = 0f, skyColor = new Color(0.10f, 0.12f, 0.17f, 1f), subtitle = "Starting Out" },
            new BiomeTier { name = "ROOFTOPS SUNSET", minAltitude = 50f, skyColor = new Color(0.24f, 0.13f, 0.22f, 1f), subtitle = "50m Altitude" },
            new BiomeTier { name = "STRATOSPHERE", minAltitude = 150f, skyColor = new Color(0.07f, 0.09f, 0.19f, 1f), subtitle = "150m Altitude" },
            new BiomeTier { name = "DEEP SPACE", minAltitude = 300f, skyColor = new Color(0.04f, 0.04f, 0.08f, 1f), subtitle = "300m Orbit" }
        };

        [Header("Transition Settings")]
        [SerializeField] private float colorTransitionSpeed = 1.8f;

        private Camera targetCamera;
        private int currentBiomeIndex = 0;
        private int highestReachedBiomeIndex = 0;

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
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            targetCamera = Camera.main;
            if (targetCamera != null && biomes.Length > 0)
            {
                targetCamera.backgroundColor = biomes[0].skyColor;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.MainMenu || state == GameState.GameOver)
            {
                currentBiomeIndex = 0;
                highestReachedBiomeIndex = 0;
                if (targetCamera != null && biomes.Length > 0)
                {
                    targetCamera.backgroundColor = biomes[0].skyColor;
                }
                AudioManager.Instance?.PlayBiomeMusic(0);
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            // Current player altitude (GameManager.CurrentAltitude)
            float altitude = GameManager.Instance != null ? GameManager.Instance.CurrentAltitude : targetCamera.transform.position.y;
            if (altitude < 0f) altitude = 0f;

            // Determine active biome tier
            int activeIndex = 0;
            for (int i = biomes.Length - 1; i >= 0; i--)
            {
                if (altitude >= biomes[i].minAltitude)
                {
                    activeIndex = i;
                    break;
                }
            }

            // Check if player entered a new higher biome tier
            if (activeIndex > highestReachedBiomeIndex)
            {
                highestReachedBiomeIndex = activeIndex;
                currentBiomeIndex = activeIndex;

                // Announce new biome and crossfade to its chiptune track
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
                {
                    BiomeTier reached = biomes[activeIndex];
                    UI.UIManager.Instance?.ShowToast($"BIOME: {reached.name}!", new Color(0.9f, 0.7f, 1f, 1f));
                    AudioManager.Instance?.PlayStreak();
                    AudioManager.Instance?.PlayBiomeMusic(activeIndex);
                }
            }
            else if (activeIndex != currentBiomeIndex)
            {
                currentBiomeIndex = activeIndex;
                AudioManager.Instance?.PlayBiomeMusic(currentBiomeIndex);
            }

            // Smoothly interpolate sky background color
            Color targetColor = biomes[currentBiomeIndex].skyColor;
            targetCamera.backgroundColor = Color.Lerp(targetCamera.backgroundColor, targetColor, Time.deltaTime * colorTransitionSpeed);
        }
    }
}
