using UnityEngine;

namespace MyProject
{
    public class CameraFollowTarget : MonoBehaviour
    {
        public static CameraFollowTarget Instance { get; private set; }

        [Header("Tracking Settings")]
        [Tooltip("The Cat transform to follow.")]
        [SerializeField] private Transform catTransform;

        [Header("Trauma Screen Shake Settings")]
        [Tooltip("Maximum camera position displacement during peak trauma.")]
        [SerializeField] private float maxShakeOffset = 0.35f;

        [Tooltip("Speed at which screen shake trauma decays back to zero.")]
        [SerializeField] private float traumaDecaySpeed = 2.0f;

        // Force the camera target to start at Y = 0 so the floor (Y = -5) remains at the bottom edge
        private float highestY = 0f;
        private float trauma = 0f;
        private float shakeSeed = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            shakeSeed = Random.value * 100f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            transform.position = new Vector3(0f, highestY, 0f);
        }

        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        private void LateUpdate()
        {
            if (catTransform != null)
            {
                // Only move camera target up if the cat climbs above the screen center (Y = 0)
                if (catTransform.position.y > highestY)
                {
                    highestY = catTransform.position.y;
                }

                // Compute trauma screen shake offset if active
                Vector3 shakeOffset = Vector3.zero;
                if (trauma > 0f)
                {
                    float shakeFactor = trauma * trauma; // Non-linear curve for punchy impact
                    float sampleTime = Time.time * 28f + shakeSeed;
                    float offsetX = (Mathf.PerlinNoise(sampleTime, 0f) * 2f - 1f) * maxShakeOffset * shakeFactor;
                    float offsetY = (Mathf.PerlinNoise(0f, sampleTime) * 2f - 1f) * maxShakeOffset * shakeFactor;
                    shakeOffset = new Vector3(offsetX, offsetY, 0f);

                    trauma = Mathf.Max(0f, trauma - traumaDecaySpeed * Time.deltaTime);
                }

                // Keep base X and Z locked to 0, follow Y upward with layered shake offset
                transform.position = new Vector3(shakeOffset.x, highestY + shakeOffset.y, 0f);
            }
        }

        public void SetTarget(Transform target)
        {
            catTransform = target;
            highestY = 0f; // Lock camera center to Y = 0 initially
            trauma = 0f;
            transform.position = new Vector3(0f, highestY, 0f);
        }
    }
}
