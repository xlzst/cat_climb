using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using MyProject.UI;

namespace MyProject
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<PlayerController>();
                }
                return instance;
            }
            private set => instance = value;
        }
        private static PlayerController instance;

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

        [Header("Climbing & Wall Locomotion Settings")]
        [Tooltip("The speed of vertical climbing on walls.")]
        [SerializeField] private float climbSpeed = 3.5f;

        [Tooltip("The downward slide speed when sliding along a wall.")]
        [SerializeField] private float wallSlideSpeed = -2f;

        [Tooltip("Horizontal impulse applied during a wall kick.")]
        [SerializeField] private float wallKickForceX = 6.5f;

        [Tooltip("Vertical impulse applied during a wall kick.")]
        [SerializeField] private float wallKickForceY = 7.5f;

        [Header("Touch & Drag Steering Settings")]
        [Tooltip("Responsiveness multiplier for following finger/pointer horizontally.")]
        [SerializeField] private float dragResponsiveness = 8f;

        [Tooltip("Minimum swipe distance in pixels to trigger a swipe gesture.")]
        [SerializeField] private float minSwipeDistance = 35f;

        [Tooltip("Maximum duration in seconds for a gesture to be considered a swipe.")]
        [SerializeField] private float maxSwipeDuration = 0.35f;

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

        [Header("Squash & Stretch Juice")]
        [SerializeField] private float squashRecoverySpeed = 14f;
        private readonly Vector3 baseVisualScale = new Vector3(0.5f, 0.5f, 1f);
        private float lastVerticalVelocity = 0f;
        private int comboCount = 0;

        private Rigidbody2D rb;
        private CircleCollider2D circleCollider;
        private SpriteRenderer spriteRenderer;
        
        private bool isGrounded;
        private bool isTouchingWall;
        private bool isTouchingLeftWall;
        private bool isTouchingRightWall;
        private bool isClimbing;
        private bool isWallSliding;

        private float facingDirectionX = 1f; // Default facing right (1.0f)

        private ContactFilter2D contactFilter;
        private readonly RaycastHit2D[] castResults = new RaycastHit2D[5];
        private float horizontalInput;
        private bool isPointerHeld;
        private Vector2 touchStartScreenPos;
        private float touchStartTime;
        private bool hasSwipedThisTouch;
        private Vector2 currentPointerScreenPos;
        private Camera mainCamera;
        private bool isInvulnerable = false;
        private Coroutine invulnerabilityCoroutine;

        private void Awake()
        {
            Instance = this;
            gameObject.tag = "Player";

            rb = GetComponent<Rigidbody2D>();
            circleCollider = GetComponent<CircleCollider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            mainCamera = Camera.main;

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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
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
                isPointerHeld = false;
                isClimbing = false;
                isWallSliding = false;
                return;
            }

            UpdateMagnet();

            // Smooth procedural squash & stretch recovery
            if (spriteRenderer != null && spriteRenderer.transform.localScale != baseVisualScale)
            {
                spriteRenderer.transform.localScale = Vector3.Lerp(
                    spriteRenderer.transform.localScale,
                    baseVisualScale,
                    Time.deltaTime * squashRecoverySpeed
                );
            }

            // 1. Detect pointer / touch state
            isPointerHeld = false;
            bool pointerDownThisFrame = false;
            bool pointerUpThisFrame = false;
            Vector2 pointerScreenPos = Vector2.zero;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                isPointerHeld = true;
                pointerScreenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame) pointerDownThisFrame = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                pointerUpThisFrame = true;
                pointerScreenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                isPointerHeld = true;
                pointerScreenPos = Pointer.current.position.ReadValue();
                if (Pointer.current.press.wasPressedThisFrame) pointerDownThisFrame = true;
            }
            else if (Pointer.current != null && Pointer.current.press.wasReleasedThisFrame)
            {
                pointerUpThisFrame = true;
                pointerScreenPos = Pointer.current.position.ReadValue();
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                isPointerHeld = true;
                pointerScreenPos = Mouse.current.position.ReadValue();
                if (Mouse.current.leftButton.wasPressedThisFrame) pointerDownThisFrame = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                pointerUpThisFrame = true;
                pointerScreenPos = Mouse.current.position.ReadValue();
            }

            currentPointerScreenPos = pointerScreenPos;

            // Handle touch begin
            if (pointerDownThisFrame)
            {
                if (!IsPointerOverUI())
                {
                    touchStartScreenPos = pointerScreenPos;
                    touchStartTime = Time.time;
                    hasSwipedThisTouch = false;
                }
                else
                {
                    isPointerHeld = false;
                }
            }

            // 2. Keyboard horizontal input detection (Arrow keys / A & D)
            horizontalInput = 0f;
            bool keyboardActive = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
                {
                    horizontalInput = -1f;
                    facingDirectionX = -1f;
                    keyboardActive = true;
                }
                else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
                {
                    horizontalInput = 1f;
                    facingDirectionX = 1f;
                    keyboardActive = true;
                }
            }

            // 3. Touch-and-Drag horizontal steering (when not actively using keyboard)
            if (!keyboardActive && isPointerHeld && !IsPointerOverUI())
            {
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(pointerScreenPos.x, pointerScreenPos.y, -mainCamera.transform.position.z));
                    float deltaX = worldPos.x - transform.position.x;
                    if (Mathf.Abs(deltaX) > 0.12f)
                    {
                        horizontalInput = Mathf.Clamp(deltaX * dragResponsiveness, -1f, 1f);
                        facingDirectionX = Mathf.Sign(deltaX);
                    }
                }
            }
            else if (!keyboardActive && !isPointerHeld && useTiltControl && Mathf.Approximately(horizontalInput, 0f))
            {
                // Tilt/Gyro horizontal movement detection fallback
                float tiltInput = GetTiltHorizontalInput();
                if (Mathf.Abs(tiltInput) > tiltDeadZone)
                {
                    float processedTilt = tiltInput * tiltSensitivity;
                    horizontalInput = Mathf.Clamp(processedTilt, -1f, 1f);
                    if (horizontalInput < 0f) facingDirectionX = -1f;
                    else if (horizontalInput > 0f) facingDirectionX = 1f;
                }
            }

            // 4. Wall Climb input detection
            bool climbInput = false;
            if (isTouchingWall)
            {
                if (Keyboard.current != null && (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed))
                {
                    climbInput = true;
                }
                else if (isPointerHeld && !IsPointerOverUI())
                {
                    // Holding touch against wall commands upward climb
                    climbInput = true;
                }
            }
            isClimbing = isTouchingWall && climbInput;

            // 5. Swipe gesture detection
            if (isPointerHeld && !hasSwipedThisTouch && !IsPointerOverUI())
            {
                Vector2 dragDelta = pointerScreenPos - touchStartScreenPos;
                float dragDuration = Time.time - touchStartTime;

                if (dragDelta.magnitude >= minSwipeDistance && dragDuration <= maxSwipeDuration)
                {
                    hasSwipedThisTouch = true;
                    ProcessSwipe(dragDelta);
                }
            }

            // 6. Tap release detection (touch ended quickly without dragging a full swipe)
            if (pointerUpThisFrame && !hasSwipedThisTouch && !IsPointerOverUI())
            {
                float touchDuration = Time.time - touchStartTime;
                float touchDistance = (pointerScreenPos - touchStartScreenPos).magnitude;

                if (touchDuration <= maxSwipeDuration && touchDistance < minSwipeDistance)
                {
                    if (isTouchingWall)
                    {
                        // Tap while touching wall executes a Wall Kick away from the wall
                        float wallKickDir = isTouchingLeftWall ? 1f : -1f;
                        ExecuteWallKick(wallKickDir);
                    }
                    else if (isGrounded || allowInfiniteJumps)
                    {
                        Jump();
                    }
                    else
                    {
                        MeleeAttack();
                    }
                }
            }

            // 7. Keyboard Space / Jump triggers
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (isTouchingWall)
                {
                    float wallKickDir = isTouchingLeftWall ? 1f : -1f;
                    ExecuteWallKick(wallKickDir);
                }
                else if (isGrounded || allowInfiniteJumps)
                {
                    Jump();
                }
                else
                {
                    MeleeAttack();
                }
            }
        }

        private void ProcessSwipe(Vector2 swipeDelta)
        {
            if (isTouchingWall)
            {
                // Left wall kicks right (+1), Right wall kicks left (-1)
                float wallKickDir = isTouchingLeftWall ? 1f : -1f;

                // If swipe is directed away from wall OR upwards
                if (Mathf.Sign(swipeDelta.x) == Mathf.Sign(wallKickDir) || swipeDelta.y > 0f)
                {
                    ExecuteWallKick(wallKickDir);
                    return;
                }
            }

            if (isGrounded || allowInfiniteJumps)
            {
                if (swipeDelta.y > 0f)
                {
                    Jump();
                    return;
                }
            }

            // Mid-air swipe triggers melee attack
            if (!isGrounded && !isTouchingWall)
            {
                MeleeAttack();
            }
        }

        public void ExecuteWallKick(float kickDirX)
        {
            isClimbing = false;
            isWallSliding = false;
            facingDirectionX = kickDirX;

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

            // Apply diagonal wall kick impulse
            rb.linearVelocity = new Vector2(kickDirX * wallKickForceX, wallKickForceY);

            // Wall-kick lateral stretch and dust puff
            ApplySquashAndStretch(1.22f, 0.8f);
            FXManager.Instance?.EmitJumpDust(transform.position + new Vector3(-kickDirX * 0.25f, -0.1f, 0f), new Vector2(kickDirX, 0.4f));
            AudioManager.Instance?.PlayWallKick();

            // Snappy visual response
            currentVisualState = (kickDirX < 0f) ? CatState.JumpingLeft : CatState.JumpingRight;
            ApplySpriteState(currentVisualState);
        }

        public void SuperBounce(float bounceForce)
        {
            isClimbing = false;
            isWallSliding = false;
            isGrounded = false;

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

            // High launch velocity upward
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);

            // Responsive aerial pose
            currentVisualState = (Mathf.Abs(rb.linearVelocity.x) > 0.4f) ? 
                (rb.linearVelocity.x < 0f ? CatState.JumpingLeft : CatState.JumpingRight) : 
                CatState.VerticalJump;
            ApplySpriteState(currentVisualState);
        }

        [Header("Power-up State")]
        [Tooltip("Suction radius in units for the coin magnet power-up.")]
        [SerializeField] private float magnetRadius = 7.5f;

        [Tooltip("Base speed at which attracted coins fly toward the cat.")]
        [SerializeField] private float magnetPullSpeed = 18f;

        private float magnetRemainingTime = 0f;
        public bool IsMagnetActive => magnetRemainingTime > 0f;
        public float MagnetRemainingTime => magnetRemainingTime;
        public float MagnetRadius => magnetRadius;
        public float MagnetPullSpeed => magnetPullSpeed;

        public void ActivateMagnet(float duration)
        {
            magnetRemainingTime = Mathf.Max(magnetRemainingTime, duration);
            Debug.Log($"[PlayerController] Magnet power-up activated for {duration} seconds!");
        }

        private void UpdateMagnet()
        {
            if (magnetRemainingTime > 0f)
            {
                magnetRemainingTime -= Time.deltaTime;
                AttractNearbyFish();
            }
        }

        private void AttractNearbyFish()
        {
            if (PlatformManager.Instance == null) return;
            var pool = PlatformManager.Instance.PlatformPool;
            if (pool == null) return;

            Vector3 playerPos = transform.position;
            float magnetRadiusSq = magnetRadius * magnetRadius;

            for (int i = 0; i < pool.Count; i++)
            {
                GameObject platform = pool[i];
                if (platform == null || !platform.activeInHierarchy) continue;

                Transform treatTransform = platform.transform.Find("FishTreat");
                if (treatTransform != null && treatTransform.gameObject.activeSelf)
                {
                    Vector3 treatPos = treatTransform.position;
                    float distSq = (playerPos - treatPos).sqrMagnitude;
                    if (distSq <= magnetRadiusSq)
                    {
                        FishTreat treat = treatTransform.GetComponent<FishTreat>();
                        if (treat != null)
                        {
                            treat.StartAttraction();
                        }
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            // 1. Ground detection
            bool wasGroundedBefore = isGrounded;
            if (!allowInfiniteJumps)
            {
                // When leaping rapidly upwards through one-way platforms, player cannot be grounded
                if (rb.linearVelocity.y > 0.15f)
                {
                    isGrounded = false;
                }
                else
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
            }
            else
            {
                isGrounded = true;
            }

            if (!wasGroundedBefore && isGrounded)
            {
                OnLanded();
            }
            lastVerticalVelocity = rb.linearVelocity.y;

            // 2. Wall detection (left and right cast)
            float wallCastDistance = 0.06f;
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

            // 3. Apply vertical movement (climbing vs wall slide vs gravity)
            float targetVerticalVelocity = rb.linearVelocity.y;
            isWallSliding = false;

            if (isClimbing)
            {
                // Override gravity with upward climbing speed
                targetVerticalVelocity = climbSpeed;
            }
            else if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f)
            {
                // Wall slide: clamp downward fall speed
                isWallSliding = true;
                targetVerticalVelocity = Mathf.Max(rb.linearVelocity.y, wallSlideSpeed);
            }

            // 4. Apply final velocities
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, targetVerticalVelocity);
        }

        public void Jump()
        {
            if (isGrounded || isTouchingWall || isClimbing || allowInfiniteJumps)
            {
                if (isTouchingWall)
                {
                    float wallKickDir = isTouchingLeftWall ? 1f : -1f;
                    ExecuteWallKick(wallKickDir);
                    return;
                }

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
                    ApplySquashAndStretch(0.82f, 1.22f);
                    FXManager.Instance?.EmitJumpDust(transform.position + Vector3.down * 0.25f, Vector2.down);
                    AudioManager.Instance?.PlayJump();
                }
            }
        }

        private void OnLanded()
        {
            float impactSpeed = Mathf.Abs(lastVerticalVelocity);
            float squashX = Mathf.Clamp(1.0f + impactSpeed * 0.035f, 1.15f, 1.35f);
            float squashY = Mathf.Clamp(1.0f - impactSpeed * 0.035f, 0.65f, 0.85f);
            ApplySquashAndStretch(squashX, squashY);

            FXManager.Instance?.EmitJumpDust(transform.position + Vector3.down * 0.25f, Vector2.up);

            comboCount++;
            if (comboCount == 3)
            {
                UIManager.Instance?.ShowToast("3x COMBO!", new Color(0.4f, 0.95f, 0.5f, 1f));
                AudioManager.Instance?.PlayStreak();
            }
            else if (comboCount == 5)
            {
                UIManager.Instance?.ShowToast("5x STREAK!", new Color(0.3f, 0.85f, 1f, 1f));
                AudioManager.Instance?.PlayStreak();
            }
            else if (comboCount == 8)
            {
                UIManager.Instance?.ShowToast("ON FIRE!", new Color(1f, 0.5f, 0.2f, 1f));
                AudioManager.Instance?.PlayStreak();
            }
            else if (comboCount == 12)
            {
                UIManager.Instance?.ShowToast("UNSTOPPABLE!", new Color(1f, 0.3f, 0.85f, 1f));
                AudioManager.Instance?.PlayStreak();
            }
        }

        private void ApplySquashAndStretch(float multX, float multY)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.localScale = new Vector3(baseVisualScale.x * multX, baseVisualScale.y * multY, 1f);
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
            if (isInvulnerable) return;

            // Trigger Defeat if we collide with a solid hazard on the "Enemy" layer (e.g. wall spikes)
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                CameraFollowTarget.Instance?.AddTrauma(0.6f);
                comboCount = 0;
                AudioManager.Instance?.PlayHit();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.HandlePlayerDefeat();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isInvulnerable) return;

            // Trigger Defeat if we overlap with a trigger hazard on the "Enemy" layer (e.g. platform enemy triangles)
            if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                CameraFollowTarget.Instance?.AddTrauma(0.6f);
                comboCount = 0;
                AudioManager.Instance?.PlayHit();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.HandlePlayerDefeat();
                }
            }
        }

        public void RespawnAt(Vector3 respawnPos)
        {
            comboCount = 0;
            transform.position = respawnPos;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = new Vector2(0f, 4.5f); // Gentle upward hop on revive
            }
            enabled = true;
            isClimbing = false;
            isWallSliding = false;
            TriggerInvulnerability(2.5f);
        }

        public void TriggerInvulnerability(float duration = 2.5f)
        {
            if (gameObject.activeInHierarchy)
            {
                if (invulnerabilityCoroutine != null)
                {
                    StopCoroutine(invulnerabilityCoroutine);
                }
                invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine(duration));
            }
        }

        private System.Collections.IEnumerator InvulnerabilityRoutine(float duration)
        {
            isInvulnerable = true;
            float elapsed = 0f;
            Color baseColor = (spriteRenderer != null) ? spriteRenderer.color : Color.white;

            while (elapsed < duration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
                }
                yield return new WaitForSeconds(0.12f);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = baseColor;
                }
                yield return new WaitForSeconds(0.12f);
                elapsed += 0.24f;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }
            isInvulnerable = false;
            invulnerabilityCoroutine = null;
        }

        private void LateUpdate()
        {
            if (spriteRenderer != null)
            {
                // 1. Detect landing transition trigger
                if (!wasGrounded && isGrounded && !isClimbing && !isWallSliding && jumpTransitionCoroutine == null)
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

                // 2. If climbing or wall sliding, cancel landing/jump/attack transitions immediately
                if (isClimbing || isWallSliding)
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

                if (isClimbing || isWallSliding)
                {
                    if (isTouchingLeftWall) targetState = CatState.ClimbingLeft;
                    else if (isTouchingRightWall) targetState = CatState.ClimbingRight;
                }
                else if (!isGrounded)
                {
                    if (horizontalInput < -0.1f) targetState = CatState.JumpingLeft;
                    else if (horizontalInput > 0.1f) targetState = CatState.JumpingRight;
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
                    float currentFrameDuration = isWallSliding ? (frameDuration * 2.5f) : frameDuration;
                    animationTimer += Time.deltaTime;
                    if (animationTimer >= currentFrameDuration)
                    {
                        animationTimer -= currentFrameDuration;
                        
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
            ApplySquashAndStretch(0.8f, 1.25f);
            FXManager.Instance?.EmitJumpDust(transform.position + Vector3.down * 0.28f, Vector2.down);
            AudioManager.Instance?.PlayJump();

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
