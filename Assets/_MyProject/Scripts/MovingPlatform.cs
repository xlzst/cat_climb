using UnityEngine;

namespace MyProject
{
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("The speed of vertical movement in units per second.")]
        [SerializeField] private float moveSpeed = 1.5f;

        [Tooltip("Maximum distance to move up or down from the initial Y position.")]
        [SerializeField] private float moveRange = 1.2f;

        [Tooltip("Safety clearance distance to prevent passing through adjacent platforms.")]
        [SerializeField] private float safetyMargin = 0.25f;

        private bool isMoving = false;
        private int shapeChoice = -1; // 0 = Square, 1 = Vertical, 2 = Horizontal
        private float initialY;
        private int moveDirection = 1; // 1 = Up, -1 = Down
        private BoxCollider2D boxCollider;

        public bool IsMoving => isMoving;
        public int ShapeChoice => shapeChoice;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        public void ConfigureMovement(int shape, bool shouldMove)
        {
            shapeChoice = shape;
            initialY = transform.position.y;
            moveDirection = Random.value < 0.5f ? 1 : -1;

            // Only Square (0) or Horizontally Long (2) platforms can move up and down
            if (shapeChoice == 1)
            {
                isMoving = false;
            }
            else
            {
                isMoving = shouldMove;
                moveSpeed = Random.Range(1.3f, 2.0f);
            }
        }

        public void SetMoving(bool active)
        {
            if (shapeChoice == 1)
            {
                isMoving = false;
                return;
            }

            if (!isMoving && active)
            {
                initialY = transform.position.y;
            }
            isMoving = active;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // Check if player has reached score threshold (100 for testing, 3000 production)
            int targetScore = PlatformManager.Instance != null ? PlatformManager.Instance.movingPlatformScoreThreshold : 100;
            float targetHeight = targetScore / 10f;
            bool scoreReached = (GameManager.Instance != null && GameManager.Instance.CurrentScore >= targetScore) || transform.position.y >= targetHeight;

            // Enable movement for square or horizontal platforms when score threshold reached
            if (scoreReached && (shapeChoice == 0 || shapeChoice == 2))
            {
                if (!isMoving)
                {
                    SetMoving(true);
                }
            }

            if (!isMoving) return;

            // Calculate vertical step
            float stepDistance = moveSpeed * Time.deltaTime;
            float deltaY = moveDirection * stepDistance;
            float currentY = transform.position.y;
            float targetY = currentY + deltaY;

            // 1. Range bounds check
            if (moveDirection == 1 && targetY >= initialY + moveRange)
            {
                moveDirection = -1;
                deltaY = moveDirection * stepDistance;
            }
            else if (moveDirection == -1 && targetY <= initialY - moveRange)
            {
                moveDirection = 1;
                deltaY = moveDirection * stepDistance;
            }

            // 2. Collision / overlap check to ensure platforms NEVER pass through each other
            if (WillOverlapOtherPlatform(moveDirection, stepDistance))
            {
                moveDirection = -moveDirection;
                deltaY = moveDirection * stepDistance;
            }

            // 3. Apply position change and carry player smoothly if standing on top
            MovePlatform(deltaY);
        }

        private bool WillOverlapOtherPlatform(int dir, float stepDistance)
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) return false;

            Bounds b = boxCollider.bounds;
            float checkDist = stepDistance + safetyMargin;

            Vector2 checkCenter;
            Vector2 checkSize;

            if (dir == 1) // Moving UP
            {
                float topEdge = b.max.y;
                Transform enemyTransform = transform.Find("Enemy");
                if (enemyTransform != null && enemyTransform.gameObject.activeSelf)
                {
                    SpriteRenderer enemyRenderer = enemyTransform.GetComponent<SpriteRenderer>();
                    if (enemyRenderer != null)
                    {
                        topEdge = Mathf.Max(topEdge, enemyRenderer.bounds.max.y);
                    }
                }

                checkCenter = new Vector2(b.center.x, topEdge + checkDist / 2f);
                checkSize = new Vector2(b.size.x * 0.95f, checkDist);
            }
            else // Moving DOWN
            {
                float bottomEdge = b.min.y;
                checkCenter = new Vector2(b.center.x, bottomEdge - checkDist / 2f);
                checkSize = new Vector2(b.size.x * 0.95f, checkDist);
            }

            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit == boxCollider || hit.transform.IsChildOf(transform)) continue;

                // Ignore player and triggers
                if (hit.GetComponentInParent<PlayerController>() != null) continue;
                if (hit.isTrigger) continue;

                // If hit belongs to another platform or physical block collider, prevent passing through
                if (hit.GetComponent<MovingPlatform>() != null || (hit.transform.parent != null && hit.transform.parent == transform.parent))
                {
                    return true;
                }
            }

            return false;
        }

        private void MovePlatform(float deltaY)
        {
            if (Mathf.Approximately(deltaY, 0f)) return;

            // Carry player smoothly if standing on top of this platform
            if (boxCollider != null)
            {
                Bounds bounds = boxCollider.bounds;
                Vector2 checkCenter = new Vector2(bounds.center.x, bounds.max.y + 0.1f);
                Vector2 checkSize = new Vector2(bounds.size.x * 0.9f, 0.2f);

                Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f);
                for (int i = 0; i < hits.Length; i++)
                {
                    PlayerController pc = hits[i].GetComponentInParent<PlayerController>();
                    if (pc != null)
                    {
                        pc.transform.position += new Vector3(0f, deltaY, 0f);
                        break;
                    }
                }
            }

            transform.position += new Vector3(0f, deltaY, 0f);
        }
    }
}
