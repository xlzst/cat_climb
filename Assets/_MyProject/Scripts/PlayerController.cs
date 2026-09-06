using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace MyProject
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("The speed of horizontal movement.")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Tilt / Gyro Settings")]
        [Tooltip("If true, the player can move horizontally using device tilt/gyro sensor.")]
        [SerializeField] private bool useTiltControl = true;

        [Tooltip("Multiplier for tilt value sensitivity.")]
        [SerializeField] private float tiltSensitivity = 2f;

        [Tooltip("Ignore tilt input below this threshold to prevent jitter.")]
        [SerializeField] private float tiltDeadZone = 0.05f;

        [Tooltip("If true, inverts the tilt direction.")]
        [SerializeField] private bool invertTilt = false;

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

        [Header("Sprite Settings")]
        [Tooltip("The sprite used when walking or moving (Frame 1).")]
        public Sprite walkingSprite;
        [Tooltip("The sprite used when walking or moving (Frame 2).")]
        public Sprite walkingSprite2;
        [Tooltip("The sprite used when walking or moving (Frame 3).")]
        public Sprite walkingSprite3;
        [Tooltip("The sprite used when walking or moving (Frame 4).")]
        public Sprite walkingSprite4;
        [Tooltip("The sprite used when staying still.")]
        public Sprite idleSprite;
        [Tooltip("The sprite used during transitions between moving and idle.")]
        public Sprite transitionSprite;
        [Tooltip("The sprite used when crouching down before jumping.")]
        public Sprite crouchSprite;
        [Tooltip("The sprite used when crouching deeply before jumping.")]
        public Sprite crouchDeepSprite;
        [Tooltip("The sprite used when taking off / launching into a jump.")]
        public Sprite takeoffSprite;
        [Tooltip("The sprite used when launching vertically.")]
        public Sprite launchSprite;
        [Tooltip("The sprite used when rising in a vertical jump (pose 1).")]
        public Sprite rise1Sprite;
        [Tooltip("The sprite used when rising in a vertical jump (pose 2).")]
        public Sprite rise2Sprite;
        [Tooltip("The sprite used when jumping left or right.")]
        public Sprite jumpSprite;
        [Tooltip("The sprite used when jumping vertically.")]
        public Sprite verticalJumpSprite;
        [Tooltip("The sprite used when climbing a wall (Frame 1).")]
        public Sprite climbSprite;
        [Tooltip("The sprite used when climbing a wall (Frame 2).")]
        public Sprite climbSprite2;
        [Tooltip("The sprite used when climbing a wall (Frame 3).")]
        public Sprite climbSprite3;
        [Tooltip("The sprite used when climbing a wall (Frame 4).")]
        public Sprite climbSprite4;

        [Header("Attack Settings")]
        [Tooltip("The sprite used for the attack animation (Frame 1).")]
        public Sprite attackSprite1;
        [Tooltip("The sprite used for the attack animation (Frame 2).")]
        public Sprite attackSprite2;
        [Tooltip("The sprite used for the attack animation (Frame 3).")]
        public Sprite attackSprite3;
        [Tooltip("The sprite used for the attack animation (Frame 4).")]
        public Sprite attackSprite4;

        [Header("Animation Loop Settings")]
        [Tooltip("Time in seconds per frame for walking and climbing loops.")]
        [SerializeField] private float frameDuration = 0.15f;

        private float animationTimer;
        private int animationFrameIndex;

        private enum CatState
        {
            Idle,
            GoingLeft,
            GoingRight,
            VerticalJump,
            JumpingLeft,
            JumpingRight,
            ClimbingLeft,
            ClimbingRight,
            JumpingTransition,
            LandingTransition,
            Attacking
        }

        private CatState currentVisualState = CatState.Idle;
        private Coroutine transitionCoroutine;
        private Coroutine landingTransitionCoroutine;
        private Coroutine jumpTransitionCoroutine;
        private Coroutine attackCoroutine;
        private readonly WaitForSeconds transitionDelay = new WaitForSeconds(0.08f);
        private readonly WaitForSeconds walkToJumpDelay = new WaitForSeconds(0.07f);
        private bool wasGrounded;

        private Rigidbody2D rb;
        private CircleCollider2D circleCollider;
        private SpriteRenderer spriteRenderer;
        
        private bool isGrounded;
        private bool isTouchingWall;
        private bool isTouchingLeftWall;
        private bool isTouchingRightWall;
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
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Fallback in case walkingSprite is not assigned
            if (spriteRenderer != null && walkingSprite == null)
            {
                walkingSprite = spriteRenderer.sprite;
            }

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

            ApplySelectedSkin();
        }

        private void Start()
        {
            ApplySelectedSkin();
        }

        public void ApplySelectedSkin()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                string selectedSkinId = PlayerPrefs.GetString("CatClimb_SelectedSkin", "orange");
                spriteRenderer.color = GetSkinColor(selectedSkinId);
            }
        }

        private Color GetSkinColor(string skinId)
        {
            switch (skinId)
            {
                case "pink": return new Color(1f, 0.65f, 0.8f, 1f);
                case "ninja": return new Color(0.35f, 0.35f, 0.45f, 1f);
                case "gold": return new Color(1f, 0.84f, 0.1f, 1f);
                case "cyan": return new Color(0.2f, 0.9f, 1f, 1f);
                case "orange":
                default:
                    return Color.white;
            }
        }

        private void OnEnable()
        {
            if (useTiltControl)
            {
                if (GravitySensor.current != null)
                {
                    InputSystem.EnableDevice(GravitySensor.current);
                }
                if (Accelerometer.current != null)
                {
                    InputSystem.EnableDevice(Accelerometer.current);
                }
            }
        }

        private void OnDisable()
        {
            if (GravitySensor.current != null)
            {
                InputSystem.DisableDevice(GravitySensor.current);
            }
            if (Accelerometer.current != null)
            {
                InputSystem.DisableDevice(Accelerometer.current);
            }
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            Vector2 pointerPosition = Vector2.zero;
            if (Pointer.current != null)
            {
                pointerPosition = Pointer.current.position.ReadValue();
            }
            else if (Mouse.current != null)
            {
                pointerPosition = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else
            {
                return false;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = pointerPosition
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject != null &&
                    (result.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null ||
                     result.gameObject.GetComponent<UnityEngine.UI.Button>() != null))
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            // Only process controls if GameManager is in Playing state
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                horizontalInput = 0f;
                isMouseHeld = false;
                return;
            }

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

            // Tilt/Gyro horizontal movement detection
            if (useTiltControl && Mathf.Approximately(horizontalInput, 0f))
            {
                float tiltInput = GetTiltHorizontalInput();
                if (Mathf.Abs(tiltInput) > tiltDeadZone)
                {
                    // Scale and clamp tilt input
                    float processedTilt = tiltInput * tiltSensitivity;
                    horizontalInput = Mathf.Clamp(processedTilt, -1f, 1f);

                    // Update facing direction based on movement direction
                    if (horizontalInput < 0f)
                    {
                        facingDirectionX = -1f;
                    }
                    else if (horizontalInput > 0f)
                    {
                        facingDirectionX = 1f;
                    }
                }
            }

            // Detect touch/mouse/keyboard jump triggers
            isMouseHeld = false;
            bool jumpRequestedThisFrame = false;

            if (Pointer.current != null)
            {
                if (Pointer.current.press.isPressed) isMouseHeld = true;
                if (Pointer.current.press.wasPressedThisFrame) jumpRequestedThisFrame = true;
            }

            if (!jumpRequestedThisFrame && Mouse.current != null)
            {
                if (Mouse.current.leftButton.isPressed) isMouseHeld = true;
                if (Mouse.current.leftButton.wasPressedThisFrame) jumpRequestedThisFrame = true;
            }

            if (!jumpRequestedThisFrame && Touchscreen.current != null)
            {
                if (Touchscreen.current.primaryTouch.press.isPressed) isMouseHeld = true;
                if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame) jumpRequestedThisFrame = true;
            }

            if (!jumpRequestedThisFrame && Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.upArrowKey.wasPressedThisFrame ||
                    Keyboard.current.wKey.wasPressedThisFrame)
                {
                    jumpRequestedThisFrame = true;
                }
            }

            if (jumpRequestedThisFrame && !IsPointerOverUI())
            {
                if (isGrounded || isTouchingWall || isClimbing || allowInfiniteJumps)
                {
                    Jump();
                }
                else
                {
                    MeleeAttack();
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
            isTouchingLeftWall = false;
            isTouchingRightWall = false;

            // Check left wall
            int leftHits = circleCollider.Cast(Vector2.left, contactFilter, castResults, wallCastDistance);
            for (int i = 0; i < leftHits; i++)
            {
                if (castResults[i].collider != null && castResults[i].collider.gameObject != gameObject)
                {
                    isTouchingLeftWall = true;
                    break;
                }
            }

            // Check right wall
            int rightHits = circleCollider.Cast(Vector2.right, contactFilter, castResults, wallCastDistance);
            for (int i = 0; i < rightHits; i++)
            {
                if (castResults[i].collider != null && castResults[i].collider.gameObject != gameObject)
                {
                    isTouchingRightWall = true;
                    break;
                }
            }

            isTouchingWall = isTouchingLeftWall || isTouchingRightWall;

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
            if (isGrounded || isTouchingWall || isClimbing || allowInfiniteJumps)
            {
                // If we are in the air (meaning allowInfiniteJumps is true and we are not physically grounded),
                // or if we are climbing, do an instant jump without crouch delay.
                // Otherwise, do a smooth transition jump.
                bool physicallyGrounded = false;
                if (!allowInfiniteJumps)
                {
                    physicallyGrounded = isGrounded;
                }
                else
                {
                    // Check if we are physically grounded using the cast
                    float castDistance = 0.05f;
                    int hitCount = circleCollider.Cast(Vector2.down, contactFilter, castResults, castDistance);
                    for (int i = 0; i < hitCount; i++)
                    {
                        if (castResults[i].collider != null && castResults[i].collider.gameObject != gameObject)
                        {
                            physicallyGrounded = true;
                            break;
                        }
                    }
                }

                if (physicallyGrounded && !isClimbing)
                {
                    if (landingTransitionCoroutine != null)
                    {
                        StopCoroutine(landingTransitionCoroutine);
                        landingTransitionCoroutine = null;
                    }
                    if (jumpTransitionCoroutine != null)
                    {
                        StopCoroutine(jumpTransitionCoroutine);
                    }
                    if (attackCoroutine != null)
                    {
                        StopCoroutine(attackCoroutine);
                        attackCoroutine = null;
                    }
                    jumpTransitionCoroutine = StartCoroutine(AnimateJumpTransition());
                }
                else
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                }
            }
        }

        private void MeleeAttack()
        {
            Debug.Log("[PlayerController] Melee attack triggered!");

            if (gameObject.activeInHierarchy)
            {
                if (attackCoroutine != null)
                {
                    StopCoroutine(attackCoroutine);
                }
                attackCoroutine = StartCoroutine(AnimateAttack());
            }

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

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Trigger Game Over if we collide with a solid hazard on the "Enemy" layer (e.g. wall spikes)
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Trigger Game Over if we overlap with a trigger hazard on the "Enemy" layer (e.g. platform enemy triangles)
            if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
            }
        }

        private void LateUpdate()
        {
            if (spriteRenderer != null)
            {
                // 1. Detect landing transition trigger
                if (!wasGrounded && isGrounded && !isClimbing && jumpTransitionCoroutine == null)
                {
                    if (landingTransitionCoroutine != null)
                    {
                        StopCoroutine(landingTransitionCoroutine);
                    }
                    if (transitionCoroutine != null)
                    {
                        StopCoroutine(transitionCoroutine);
                        transitionCoroutine = null;
                    }
                    if (attackCoroutine != null)
                    {
                        StopCoroutine(attackCoroutine);
                        attackCoroutine = null;
                    }
                    bool landedVertically = (currentVisualState == CatState.VerticalJump);
                    landingTransitionCoroutine = StartCoroutine(AnimateLandingTransition(landedVertically));
                }

                // 2. If climbing, cancel landing/jump/attack transitions immediately
                if (isClimbing)
                {
                    if (landingTransitionCoroutine != null)
                    {
                        StopCoroutine(landingTransitionCoroutine);
                        landingTransitionCoroutine = null;
                    }
                    if (jumpTransitionCoroutine != null)
                    {
                        StopCoroutine(jumpTransitionCoroutine);
                        jumpTransitionCoroutine = null;
                    }
                    if (attackCoroutine != null)
                    {
                        StopCoroutine(attackCoroutine);
                        attackCoroutine = null;
                    }
                }

                // 3. If jump transition is running, let it control the sprite
                if (jumpTransitionCoroutine != null)
                {
                    wasGrounded = isGrounded;
                    return;
                }

                // 4. If landing transition is running, let it control the sprite (and handle horizontal flip)
                if (landingTransitionCoroutine != null)
                {
                    spriteRenderer.flipX = facingDirectionX > 0f;
                    wasGrounded = isGrounded;
                    return;
                }

                // 5. If attack animation is running, let it control the sprite (and handle horizontal flip)
                if (attackCoroutine != null)
                {
                    spriteRenderer.flipX = facingDirectionX > 0f;
                    wasGrounded = isGrounded;
                    return;
                }

                // Determine target visual state
                CatState targetState = CatState.Idle;

                if (isClimbing)
                {
                    if (isTouchingLeftWall) targetState = CatState.ClimbingLeft;
                    else if (isTouchingRightWall) targetState = CatState.ClimbingRight;
                }
                else if (!isGrounded)
                {
                    if (horizontalInput < 0f) targetState = CatState.JumpingLeft;
                    else if (horizontalInput > 0f) targetState = CatState.JumpingRight;
                    else targetState = CatState.VerticalJump;
                }
                else
                {
                    if (Mathf.Approximately(horizontalInput, 0f)) targetState = CatState.Idle;
                    else targetState = (facingDirectionX < 0f) ? CatState.GoingLeft : CatState.GoingRight;
                }

                if (targetState != currentVisualState)
                {
                    TriggerStateTransition(currentVisualState, targetState);
                    currentVisualState = targetState;
                }

                // Update looping animation frame index over time
                if (IsLoopingState(currentVisualState))
                {
                    animationTimer += Time.deltaTime;
                    if (animationTimer >= frameDuration)
                    {
                        animationTimer -= frameDuration;
                        
                        // For walking states, loop through 4 steps (Frame 1 -> 2 -> 3 -> 2)
                        if (currentVisualState == CatState.GoingLeft || currentVisualState == CatState.GoingRight)
                        {
                            animationFrameIndex = (animationFrameIndex + 1) % 4;
                        }
                        else // For climbing states, loop through 4 steps (Frame 1 -> 2 -> 3 -> 4)
                        {
                            animationFrameIndex = (animationFrameIndex + 1) % 4;
                        }
                    }
                }
                else
                {
                    animationTimer = 0f;
                    animationFrameIndex = 0;
                }

                // If not animating transition, apply base sprite and flip state directly
                if (transitionCoroutine == null)
                {
                    ApplySpriteState(currentVisualState);
                    spriteRenderer.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                }
            }

            wasGrounded = isGrounded;
        }

        private bool ShouldAnimateTransition(CatState from, CatState to)
        {
            // Grounded turn/idle transitions
            bool isFromGroundedState = from == CatState.Idle || from == CatState.GoingLeft || from == CatState.GoingRight;
            bool isToGroundedState = to == CatState.Idle || to == CatState.GoingLeft || to == CatState.GoingRight;
            if (isFromGroundedState && isToGroundedState) return true;

            // Walk-to-jump transitions (GoingLeft -> JumpingLeft, GoingRight -> JumpingRight)
            if (from == CatState.GoingLeft && to == CatState.JumpingLeft) return true;
            if (from == CatState.GoingRight && to == CatState.JumpingRight) return true;

            return false;
        }

        private void TriggerStateTransition(CatState fromState, CatState toState)
        {
            if (gameObject.activeInHierarchy && ShouldAnimateTransition(fromState, toState))
            {
                if (transitionCoroutine != null)
                {
                    StopCoroutine(transitionCoroutine);
                }
                transitionCoroutine = StartCoroutine(AnimateTransition(fromState, toState));
            }
            else
            {
                if (transitionCoroutine != null)
                {
                    StopCoroutine(transitionCoroutine);
                    transitionCoroutine = null;
                }
                ApplySpriteState(toState);
            }
        }

        private System.Collections.IEnumerator AnimateTransition(CatState fromState, CatState toState)
        {
            if ((fromState == CatState.GoingLeft && toState == CatState.JumpingLeft) ||
                (fromState == CatState.GoingRight && toState == CatState.JumpingRight))
            {
                // 1. Crouch frame (Squat)
                ApplyCrouchSpriteState(toState);
                yield return walkToJumpDelay;

                // 2. Takeoff frame (Launch)
                ApplyTakeoffSpriteState(toState);
                yield return walkToJumpDelay;
            }
            else
            {
                // Ground turn transition
                ApplyTransitionSpriteState(fromState, toState);
                yield return transitionDelay;
            }

            // Swap to final destination sprite
            ApplySpriteState(toState);
            transitionCoroutine = null;
        }

        private System.Collections.IEnumerator AnimateJumpTransition()
        {
            currentVisualState = CatState.JumpingTransition;

            // Capture initial jump input at the moment of jump trigger
            float jumpDirectionX = horizontalInput;
            bool isVertical = Mathf.Approximately(jumpDirectionX, 0f);
            bool isRight = jumpDirectionX > 0f;

            // Frame 1: Crouch 1
            if (isVertical)
            {
                if (crouchDeepSprite != null) spriteRenderer.sprite = crouchDeepSprite;
                spriteRenderer.flipX = false;
            }
            else
            {
                if (crouchSprite != null) spriteRenderer.sprite = crouchSprite;
                spriteRenderer.flipX = isRight;
            }
            yield return new WaitForSeconds(0.05f);

            // Frame 2: Crouch 2
            if (isVertical)
            {
                if (crouchDeepSprite != null) spriteRenderer.sprite = crouchDeepSprite;
                spriteRenderer.flipX = false;
            }
            else
            {
                if (crouchSprite != null) spriteRenderer.sprite = crouchSprite;
                spriteRenderer.flipX = isRight;
            }
            yield return new WaitForSeconds(0.05f);

            // Apply physical jump velocity now that the crouch is complete!
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // Frame 3: Takeoff
            if (isVertical)
            {
                if (launchSprite != null) spriteRenderer.sprite = launchSprite;
                spriteRenderer.flipX = false;
            }
            else
            {
                if (takeoffSprite != null) spriteRenderer.sprite = takeoffSprite;
                spriteRenderer.flipX = isRight;
            }
            yield return new WaitForSeconds(0.05f);

            // Frame 4: Rise 1
            if (isVertical)
            {
                if (rise1Sprite != null) spriteRenderer.sprite = rise1Sprite;
                spriteRenderer.flipX = false;
            }
            else
            {
                if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
                spriteRenderer.flipX = isRight;
            }
            yield return new WaitForSeconds(0.05f);

            // Frame 5: Rise 2
            if (isVertical)
            {
                if (rise2Sprite != null) spriteRenderer.sprite = rise2Sprite;
                spriteRenderer.flipX = false;
            }
            else
            {
                if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
                spriteRenderer.flipX = isRight;
            }
            yield return new WaitForSeconds(0.05f);

            jumpTransitionCoroutine = null;
            // Let the standard state machine in LateUpdate take over
            currentVisualState = isVertical ? CatState.VerticalJump : (jumpDirectionX < 0f ? CatState.JumpingLeft : CatState.JumpingRight);
        }

        private System.Collections.IEnumerator AnimateLandingTransition(bool isVertical)
        {
            currentVisualState = CatState.LandingTransition;

            // Frame 1: Crouch 2 (deep impact cushion)
            if (isVertical)
            {
                if (crouchDeepSprite != null) spriteRenderer.sprite = crouchDeepSprite;
                spriteRenderer.flipX = false;
            }
            else
            {
                if (crouchSprite != null) spriteRenderer.sprite = crouchSprite;
                spriteRenderer.flipX = facingDirectionX > 0f;
            }
            yield return new WaitForSeconds(0.06f);

            // Frame 2: Crouch 1 (recovering)
            if (isVertical)
            {
                if (idleSprite != null) spriteRenderer.sprite = idleSprite;
                spriteRenderer.flipX = false;
            }
            else
            {
                if (crouchSprite != null) spriteRenderer.sprite = crouchSprite;
                spriteRenderer.flipX = facingDirectionX > 0f;
            }
            yield return new WaitForSeconds(0.06f);

            landingTransitionCoroutine = null;
            // Let the standard state machine take over in the next frames
            currentVisualState = CatState.Idle;
        }

        private System.Collections.IEnumerator AnimateAttack()
        {
            currentVisualState = CatState.Attacking;

            // Frame 1
            if (attackSprite1 != null) spriteRenderer.sprite = attackSprite1;
            spriteRenderer.flipX = facingDirectionX > 0f;
            yield return new WaitForSeconds(0.05f);

            // Frame 2
            if (attackSprite2 != null) spriteRenderer.sprite = attackSprite2;
            spriteRenderer.flipX = facingDirectionX > 0f;
            yield return new WaitForSeconds(0.05f);

            // Frame 3
            if (attackSprite3 != null) spriteRenderer.sprite = attackSprite3;
            spriteRenderer.flipX = facingDirectionX > 0f;
            yield return new WaitForSeconds(0.05f);

            // Frame 4
            if (attackSprite4 != null) spriteRenderer.sprite = attackSprite4;
            spriteRenderer.flipX = facingDirectionX > 0f;
            yield return new WaitForSeconds(0.05f);

            attackCoroutine = null;
        }

        private void ApplyCrouchSpriteState(CatState targetJumpState)
        {
            if (spriteRenderer == null || crouchSprite == null) return;
            spriteRenderer.sprite = crouchSprite;
            spriteRenderer.flipX = (targetJumpState == CatState.JumpingRight);
        }

        private void ApplyTakeoffSpriteState(CatState targetJumpState)
        {
            if (spriteRenderer == null || takeoffSprite == null) return;
            spriteRenderer.sprite = takeoffSprite;
            spriteRenderer.flipX = (targetJumpState == CatState.JumpingRight);
        }

        private void ApplySpriteState(CatState state)
        {
            if (spriteRenderer == null) return;

            switch (state)
            {
                case CatState.Idle:
                    if (idleSprite != null) spriteRenderer.sprite = idleSprite;
                    break;
                case CatState.GoingLeft:
                    spriteRenderer.sprite = GetWalkingSpriteForFrame(animationFrameIndex);
                    spriteRenderer.flipX = false;
                    break;
                case CatState.GoingRight:
                    spriteRenderer.sprite = GetWalkingSpriteForFrame(animationFrameIndex);
                    spriteRenderer.flipX = true;
                    break;
                case CatState.VerticalJump:
                    if (rise2Sprite != null) spriteRenderer.sprite = rise2Sprite;
                    else if (verticalJumpSprite != null) spriteRenderer.sprite = verticalJumpSprite;
                    break;
                case CatState.JumpingLeft:
                    if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
                    spriteRenderer.flipX = false;
                    break;
                case CatState.JumpingRight:
                    if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
                    spriteRenderer.flipX = true;
                    break;
                case CatState.ClimbingLeft:
                    spriteRenderer.sprite = GetClimbingSpriteForFrame(animationFrameIndex);
                    spriteRenderer.flipX = false;
                    break;
                case CatState.ClimbingRight:
                    spriteRenderer.sprite = GetClimbingSpriteForFrame(animationFrameIndex);
                    spriteRenderer.flipX = true;
                    break;
            }
        }

        private void ApplyTransitionSpriteState(CatState from, CatState to)
        {
            if (spriteRenderer == null || transitionSprite == null) return;

            spriteRenderer.sprite = transitionSprite;

            // Determine flipX based on direction
            // If the target or source state points left:
            if (to == CatState.GoingLeft || to == CatState.JumpingLeft || to == CatState.ClimbingLeft ||
                from == CatState.GoingLeft || from == CatState.JumpingLeft || from == CatState.ClimbingLeft)
            {
                spriteRenderer.flipX = false;
            }
            // If the target or source state points right:
            else if (to == CatState.GoingRight || to == CatState.JumpingRight || to == CatState.ClimbingRight ||
                     from == CatState.GoingRight || from == CatState.JumpingRight || from == CatState.ClimbingRight)
            {
                spriteRenderer.flipX = true;
            }
        }

        private Sprite GetWalkingSpriteForFrame(int frameIndex)
        {
            switch (frameIndex)
            {
                case 0: return walkingSprite;
                case 1: return (walkingSprite2 != null) ? walkingSprite2 : walkingSprite;
                case 2: return (walkingSprite3 != null) ? walkingSprite3 : walkingSprite;
                case 3: return (walkingSprite4 != null) ? walkingSprite4 : walkingSprite;
                default: return walkingSprite;
            }
        }

        private Sprite GetClimbingSpriteForFrame(int frameIndex)
        {
            switch (frameIndex)
            {
                case 0: return climbSprite;
                case 1: return (climbSprite2 != null) ? climbSprite2 : climbSprite;
                case 2: return (climbSprite3 != null) ? climbSprite3 : climbSprite;
                case 3: return (climbSprite4 != null) ? climbSprite4 : climbSprite;
                default: return climbSprite;
            }
        }

        private bool IsLoopingState(CatState state)
        {
            return state == CatState.GoingLeft || state == CatState.GoingRight ||
                   state == CatState.ClimbingLeft || state == CatState.ClimbingRight;
        }

        private float GetTiltHorizontalInput()
        {
            float tiltValue = 0f;

            if (useTiltControl)
            {
                // 1. GravitySensor has cleaner, filtered acceleration due to gravity
                if (GravitySensor.current != null && GravitySensor.current.enabled)
                {
                    tiltValue = GravitySensor.current.gravity.ReadValue().x;
                }
                // 2. Accelerometer fallback
                else if (Accelerometer.current != null && Accelerometer.current.enabled)
                {
                    tiltValue = Accelerometer.current.acceleration.ReadValue().x;
                }
            }

            if (invertTilt)
            {
                tiltValue = -tiltValue;
            }

            return tiltValue;
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
