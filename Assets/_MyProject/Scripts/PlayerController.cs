using UnityEngine;
using UnityEngine.InputSystem;

namespace MyProject
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("The speed of horizontal movement.")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Climbing Settings")]
        [Tooltip("The speed of vertical climbing on walls.")]
        [SerializeField] private float climbSpeed = 3f;

        [Header("Jump Settings")]
        [Tooltip("The vertical force applied when jumping.")]
        [SerializeField] private float jumpForce = 6f;

        [Tooltip("If true, the player can jump mid-air infinitely. If false, they must be touching the ground.")]
        [SerializeField] private bool allowInfiniteJumps = false;

        [Header("Melee Attack Settings")]
        [Tooltip("The range of the melee attack in front of the cat.")]
        [SerializeField] private float attackRadius = 0.6f;
        [Tooltip("The horizontal offset in front of the cat where the attack hits.")]
        [SerializeField] private float attackOffset = 0.5f;

        private Rigidbody2D rb;
        private CircleCollider2D circleCollider;
        
        private bool isGrounded;
        private bool isTouchingWall;
        private bool isClimbing;

        private float facingDirectionX = 1f; // Default facing right (1.0f)

        private ContactFilter2D contactFilter;
        private readonly RaycastHit2D[] castResults = new RaycastHit2D[5];
        private float horizontalInput;
        private bool isMouseHeld;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            circleCollider = GetComponent<CircleCollider2D>();

            // Configure contact filter to ignore trigger colliders and use project physics layers
            contactFilter.useTriggers = false;
            contactFilter.SetLayerMask(Physics2D.AllLayers);
            contactFilter.useLayerMask = true;

            // Dynamically create a frictionless physics material and assign it to the collider.
            // This removes friction that slows the player down when pushing against walls.
            PhysicsMaterial2D frictionlessMaterial = new PhysicsMaterial2D("Frictionless")
            {
                friction = 0f,
                bounciness = 0f
            };
            circleCollider.sharedMaterial = frictionlessMaterial;
        }

        private void Update()
        {
            // Horizontal movement input detection (Arrow keys)
            horizontalInput = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed)
                {
                    horizontalInput = -1f;
                    facingDirectionX = -1f; // Update facing left
                }
                else if (Keyboard.current.rightArrowKey.isPressed)
                {
                    horizontalInput = 1f;
                    facingDirectionX = 1f; // Update facing right
                }
            }

            // Detect if mouse/touch is held down
            isMouseHeld = false;
            if (Pointer.current != null)
            {
                isMouseHeld = Pointer.current.press.isPressed;

                // Jump or Melee Attack on initial click/tap
                if (Pointer.current.press.wasPressedThisFrame)
                {
                    if (isGrounded || allowInfiniteJumps)
                    {
                        Jump();
                    }
                    else
                    {
                        MeleeAttack();
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            // 1. Ground detection
            if (!allowInfiniteJumps)
            {
                float castDistance = 0.05f;
                int hitCount = circleCollider.Cast(Vector2.down, contactFilter, castResults, castDistance);

                isGrounded = false;
                for (int i = 0; i < hitCount; i++)
                {
                    if (castResults[i].collider != null && castResults[i].collider.gameObject != gameObject)
                    {
                        isGrounded = true;
                        break;
                    }
                }
            }
            else
            {
                isGrounded = true;
            }

            // 2. Wall detection (left and right cast)
            float wallCastDistance = 0.05f;
            isTouchingWall = false;

            // Check left wall
            int leftHits = circleCollider.Cast(Vector2.left, contactFilter, castResults, wallCastDistance);
            for (int i = 0; i < leftHits; i++)
            {
                if (castResults[i].collider != null && castResults[i].collider.gameObject != gameObject)
                {
                    isTouchingWall = true;
                    break;
                }
            }

            // Check right wall
            if (!isTouchingWall)
            {
                int rightHits = circleCollider.Cast(Vector2.right, contactFilter, castResults, wallCastDistance);
                for (int i = 0; i < rightHits; i++)
                {
                    if (castResults[i].collider != null && castResults[i].collider.gameObject != gameObject)
                    {
                        isTouchingWall = true;
                        break;
                    }
                }
            }

            // 3. Apply vertical movement (climbing vs gravity)
            float targetVerticalVelocity = rb.linearVelocity.y;
            isClimbing = isTouchingWall && isMouseHeld;

            if (isClimbing)
            {
                // Override gravity with upward climbing speed
                targetVerticalVelocity = climbSpeed;
            }

            // 4. Apply final velocities
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, targetVerticalVelocity);
        }

        public void Jump()
        {
            if (isGrounded)
            {
                // Reset Y velocity first to make the jump height consistent
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }

        private void MeleeAttack()
        {
            Debug.Log("[PlayerController] Melee attack triggered!");

            // Determine attack center based on facing direction
            float dirX = (facingDirectionX != 0f) ? facingDirectionX : 1f;
            Vector2 attackCenter = (Vector2)transform.position + new Vector2(dirX * attackOffset, 0f);

            // Spatial query on "Enemy" layer using OverlapCircleAll (highly performant, zero GC/instantiation overhead)
            int enemyLayerMask = LayerMask.GetMask("Enemy");
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackCenter, attackRadius, enemyLayerMask);

            for (int i = 0; i < hitEnemies.Length; i++)
            {
                Collider2D hit = hitEnemies[i];
                Enemy enemyComponent = hit.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    enemyComponent.TakeDamage();
                }
            }
        }

        // Draw debug lines in the Scene view to show ground, wall, and attack ranges
        private void OnDrawGizmos()
        {
            if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                // Draw ground check (Green if grounded, Red if not)
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Vector3 center = transform.position + (Vector3)circleCollider.offset;
                float worldRadius = circleCollider.radius * transform.localScale.x;
                Gizmos.DrawWireSphere(center + Vector3.down * 0.05f, worldRadius);

                // Draw wall check range (Blue if touching wall, Orange if not)
                Gizmos.color = isTouchingWall ? Color.blue : new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(center + Vector3.left * 0.05f, worldRadius);
                Gizmos.DrawWireSphere(center + Vector3.right * 0.05f, worldRadius);

                // Draw melee attack check range in front of the cat (Yellow)
                Gizmos.color = Color.yellow;
                float dirX = (facingDirectionX != 0f) ? facingDirectionX : 1f;
                Vector3 attackCenter = transform.position + new Vector3(dirX * attackOffset, 0f, 0f);
                Gizmos.DrawWireSphere(attackCenter, attackRadius);
            }
        }
    }
}
