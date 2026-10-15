using System.Collections.Generic;
using UnityEngine;

namespace MyProject
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class MagnetItem : MonoBehaviour
    {
        public static readonly List<MagnetItem> ActiveMagnetItems = new List<MagnetItem>(16);

        [Tooltip("Duration in seconds that the coin magnet power-up remains active.")]
        [SerializeField] private float duration = 8f;

        [Tooltip("Floating amplitude for gentle hover animation.")]
        [SerializeField] private float bobAmplitude = 0.08f;

        [Tooltip("Floating speed for gentle hover animation.")]
        [SerializeField] private float bobSpeed = 3.2f;

        private Vector3 initialLocalPos;
        private CircleCollider2D circleCollider;

        private void Awake()
        {
            circleCollider = GetComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            if (!ActiveMagnetItems.Contains(this))
            {
                ActiveMagnetItems.Add(this);
            }

            initialLocalPos = transform.localPosition;
        }

        private void OnDisable()
        {
            ActiveMagnetItems.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveMagnetItems.Remove(this);
        }

        public void SetBasePosition(Vector3 pos)
        {
            initialLocalPos = pos;
            transform.localPosition = pos;
        }

        private void Update()
        {
            // Gentle hovering bobbing animation
            float newY = initialLocalPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.localPosition = new Vector3(initialLocalPos.x, newY, initialLocalPos.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null && other.CompareTag("Player"))
            {
                player = other.GetComponent<PlayerController>();
            }
            if (player == null && (other.name.Contains("Cat") || other.name.Contains("Player")))
            {
                player = PlayerController.Instance;
            }

            if (player != null)
            {
                Collect(player);
            }
        }

        public void Collect(PlayerController player)
        {
            if (player != null)
            {
                player.ActivateMagnet(duration);
            }

            AudioManager.Instance?.PlayMagnet();

            // Deactivate back into pool without allocations
            gameObject.SetActive(false);
        }
    }
}
