using System.Collections.Generic;
using UnityEngine;

namespace MyProject
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class FishTreat : MonoBehaviour
    {
        public static readonly List<FishTreat> ActiveFishTreats = new List<FishTreat>(32);

        [Tooltip("The amount of fish coins awarded when collected.")]
        [SerializeField] private int coinValue = 1;

        [Tooltip("Floating amplitude for gentle hover animation.")]
        [SerializeField] private float bobAmplitude = 0.08f;

        [Tooltip("Floating speed for gentle hover animation.")]
        [SerializeField] private float bobSpeed = 3.5f;

        [Tooltip("Speed at which the treat flies toward the player when magnetized.")]
        [SerializeField] private float magnetPullSpeed = 18f;

        private Vector3 initialLocalPos;
        private CircleCollider2D circleCollider;
        private SpriteRenderer spriteRenderer;
        private bool isAttracted = false;

        private void Awake()
        {
            circleCollider = GetComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (!ActiveFishTreats.Contains(this))
            {
                ActiveFishTreats.Add(this);
            }

            isAttracted = false;
            initialLocalPos = transform.localPosition;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }

        private void OnDisable()
        {
            ActiveFishTreats.Remove(this);
            isAttracted = false;
        }

        private void OnDestroy()
        {
            ActiveFishTreats.Remove(this);
        }

        public void SetBasePosition(Vector3 pos)
        {
            isAttracted = false;
            initialLocalPos = pos;
            transform.localPosition = pos;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }

        public void StartAttraction()
        {
            isAttracted = true;
        }

        private void Update()
        {
            PlayerController player = PlayerController.Instance;

            // 1. If currently attracted by magnet, fly directly towards the player
            if (isAttracted)
            {
                if (player != null)
                {
                    FlyTowardsPlayer(player);
                }
                return;
            }

            // 2. Self-detect active magnet power-up on player within suction radius
            if (player != null && player.IsMagnetActive)
            {
                float magnetRadius = player.MagnetRadius;
                float distSq = (player.transform.position - transform.position).sqrMagnitude;
                if (distSq <= magnetRadius * magnetRadius)
                {
                    isAttracted = true;
                    FlyTowardsPlayer(player);
                    return;
                }
            }

            // 3. Normal gentle idle hovering bobbing animation when not magnetized
            float newY = initialLocalPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.localPosition = new Vector3(initialLocalPos.x, newY, initialLocalPos.z);
        }

        private void FlyTowardsPlayer(PlayerController player)
        {
            Vector3 targetPos = player.transform.position;
            float dist = Vector3.Distance(transform.position, targetPos);

            // If close enough to the cat, collect immediately
            if (dist <= 0.65f)
            {
                Collect();
                return;
            }

            // Accelerate speed dynamically so farther coins catch up quickly
            float effectiveSpeed = Mathf.Max(magnetPullSpeed, dist * 4.0f);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, effectiveSpeed * Time.deltaTime);

            // Subtle magnetic sparkle glow
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.95f, 0.65f, 1f);
            }

            // Secondary check after moving this frame
            if (Vector3.Distance(transform.position, targetPos) <= 0.65f)
            {
                Collect();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null || other.CompareTag("Player") || other.name.Contains("Cat"))
            {
                Collect();
            }
        }

        public void Collect()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(coinValue);
            }

            FXManager.Instance?.EmitCoinSparkle(transform.position);
            AudioManager.Instance?.PlayCoin();

            // Deactivate and return to pool without allocations
            gameObject.SetActive(false);
        }
    }
}
