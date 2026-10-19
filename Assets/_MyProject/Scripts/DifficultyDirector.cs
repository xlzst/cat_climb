using UnityEngine;

namespace MyProject
{
    /// <summary>
    /// Single source of truth for altitude-driven difficulty progression.
    /// Evaluates platform spacing, hazard probabilities, moving platform speeds,
    /// and wall spike density based on the altitude (in meters) being generated.
    /// </summary>
    public class DifficultyDirector : MonoBehaviour
    {
        public static DifficultyDirector Instance { get; private set; }

        [Header("Altitude Curves (0m -> 350m)")]
        [Tooltip("Minimum vertical spacing between platforms across altitude (meters).")]
        [SerializeField] private AnimationCurve minSpacingCurve = new AnimationCurve(
            new Keyframe(0f, 1.15f),
            new Keyframe(50f, 1.30f),
            new Keyframe(150f, 1.45f),
            new Keyframe(300f, 1.55f)
        );

        [Tooltip("Maximum vertical spacing between platforms across altitude (meters).")]
        [SerializeField] private AnimationCurve maxSpacingCurve = new AnimationCurve(
            new Keyframe(0f, 1.65f),
            new Keyframe(50f, 1.90f),
            new Keyframe(150f, 2.08f),
            new Keyframe(300f, 2.22f)
        );

        [Tooltip("Enemy mouse spawn probability (0..1) across altitude (meters).")]
        [SerializeField] private AnimationCurve enemyChanceCurve = new AnimationCurve(
            new Keyframe(0f, 0.0f),
            new Keyframe(15f, 0.04f),
            new Keyframe(50f, 0.10f),
            new Keyframe(150f, 0.16f),
            new Keyframe(300f, 0.22f)
        );

        [Tooltip("Fragile crumbling platform probability (0..1) across altitude (meters).")]
        [SerializeField] private AnimationCurve fragileChanceCurve = new AnimationCurve(
            new Keyframe(0f, 0.05f),
            new Keyframe(50f, 0.20f),
            new Keyframe(150f, 0.30f),
            new Keyframe(300f, 0.36f)
        );

        [Tooltip("Moving platform probability (0..1) across altitude (meters).")]
        [SerializeField] private AnimationCurve movingPlatformChanceCurve = new AnimationCurve(
            new Keyframe(0f, 0.0f),
            new Keyframe(18f, 0.0f),
            new Keyframe(20f, 0.18f),
            new Keyframe(50f, 0.28f),
            new Keyframe(150f, 0.42f),
            new Keyframe(300f, 0.52f)
        );

        [Tooltip("Moving platform speed multiplier across altitude (meters).")]
        [SerializeField] private AnimationCurve movingSpeedMultiplierCurve = new AnimationCurve(
            new Keyframe(0f, 0.85f),
            new Keyframe(50f, 1.0f),
            new Keyframe(150f, 1.25f),
            new Keyframe(300f, 1.50f)
        );

        [Tooltip("Per-slot wall spike activation probability across altitude (meters).")]
        [SerializeField] private AnimationCurve spikeActivationChanceCurve = new AnimationCurve(
            new Keyframe(0f, 0.32f),
            new Keyframe(50f, 0.52f),
            new Keyframe(150f, 0.68f),
            new Keyframe(300f, 0.80f)
        );

        private float initialPlayerY = -4.55f;

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

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                initialPlayerY = PlayerController.Instance.transform.position.y;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Converts a world Y coordinate into altitude in meters relative to the player's starting height.
        /// </summary>
        public float WorldYToMeters(float worldY)
        {
            return Mathf.Max(0f, worldY - initialPlayerY);
        }

        public void GetPlatformSpacing(float worldY, out float minSpacing, out float maxSpacing)
        {
            float meters = WorldYToMeters(worldY);
            minSpacing = minSpacingCurve.Evaluate(meters);
            maxSpacing = Mathf.Max(minSpacing + 0.25f, maxSpacingCurve.Evaluate(meters));
        }

        public float GetEnemyChance(float worldY)
        {
            float meters = WorldYToMeters(worldY);
            return Mathf.Clamp01(enemyChanceCurve.Evaluate(meters));
        }

        public float GetFragileChance(float worldY)
        {
            float meters = WorldYToMeters(worldY);
            return Mathf.Clamp01(fragileChanceCurve.Evaluate(meters));
        }

        public float GetMovingPlatformChance(float worldY)
        {
            float meters = WorldYToMeters(worldY);
            return Mathf.Clamp01(movingPlatformChanceCurve.Evaluate(meters));
        }

        public float GetMovingSpeedMultiplier(float worldY)
        {
            float meters = WorldYToMeters(worldY);
            return Mathf.Clamp(movingSpeedMultiplierCurve.Evaluate(meters), 0.5f, 2.2f);
        }

        /// <summary>
        /// Returns how many spike slots (1..4) on a 10m wall segment can be active at the given world Y.
        /// 0-35m: 1 spike, 35-120m: 2 spikes, 120-240m: 3 spikes, 240m+: 4 spikes.
        /// </summary>
        public int GetMaxSpikesPerSegment(float segmentWorldY)
        {
            float meters = WorldYToMeters(segmentWorldY);
            if (meters < 35f) return 1;
            if (meters < 120f) return 2;
            if (meters < 240f) return 3;
            return 4;
        }

        public float GetSpikeActivationChance(float segmentWorldY)
        {
            float meters = WorldYToMeters(segmentWorldY);
            return Mathf.Clamp01(spikeActivationChanceCurve.Evaluate(meters));
        }
    }
}
