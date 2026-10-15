using UnityEngine;
using System.Collections;

namespace MyProject
{
    public class FragilePlatform : MonoBehaviour
    {
        [Tooltip("Delay in seconds after the player steps on the platform before it crumbles.")]
        [SerializeField] private float crumbleDelay = 0.55f;

        [Tooltip("Horizontal wobble shake magnitude before crumbling.")]
        [SerializeField] private float shakeMagnitude = 0.05f;

        private bool isTriggered = false;
        private BoxCollider2D boxCollider;
        private SpriteRenderer spriteRenderer;
        private Color originalColor = Color.white;
        private Coroutine crumbleCoroutine;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        public void ConfigureFragile(bool enableFragile)
        {
            ConfigureFragile(enableFragile, originalColor);
        }

        public void ConfigureFragile(bool enableFragile, Color defaultColor)
        {
            enabled = enableFragile;
            originalColor = defaultColor;

            if (enableFragile && spriteRenderer != null)
            {
                // Distinct cracked amber tone to visually warn player that platform is fragile
                spriteRenderer.color = new Color(0.95f, 0.72f, 0.45f, 1f);
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        public void ResetFragile()
        {
            if (crumbleCoroutine != null)
            {
                StopCoroutine(crumbleCoroutine);
                crumbleCoroutine = null;
            }

            isTriggered = false;

            if (boxCollider != null)
            {
                boxCollider.enabled = true;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = originalColor;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!enabled || isTriggered) return;

            PlayerController player = collision.gameObject.GetComponentInParent<PlayerController>();
            if (player == null && collision.gameObject.CompareTag("Player"))
            {
                player = collision.gameObject.GetComponent<PlayerController>();
            }

            if (player != null)
            {
                // Verify the player is landing on the top surface
                if (collision.contactCount > 0 && collision.contacts[0].normal.y < -0.5f)
                {
                    isTriggered = true;
                    crumbleCoroutine = StartCoroutine(CrumbleRoutine());
                }
            }
        }

        private IEnumerator CrumbleRoutine()
        {
            float elapsed = 0f;
            Vector3 basePos = transform.position;

            while (elapsed < crumbleDelay)
            {
                // Wobble shake effect around current position
                float offsetX = (Random.value - 0.5f) * 2f * shakeMagnitude;
                transform.position = new Vector3(basePos.x + offsetX, basePos.y, basePos.z);

                // Warning flash
                if (spriteRenderer != null)
                {
                    float flash = Mathf.PingPong(elapsed * 12f, 1f);
                    spriteRenderer.color = Color.Lerp(new Color(0.95f, 0.72f, 0.45f, 1f), new Color(1f, 0.35f, 0.35f, 1f), flash);
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            // Restore exact base position
            transform.position = basePos;
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            // Screen shake trauma on platform collapse
            CameraFollowTarget.Instance?.AddTrauma(0.22f);

            // Emit crumble debris particles
            FXManager.Instance?.EmitPlatformDebris(basePos, transform.localScale);

            // Play crumble sound effect
            AudioManager.Instance?.PlayCrumble();

            crumbleCoroutine = null;
        }
    }
}
