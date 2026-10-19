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
        private bool touchStartedOverUI;
        private Vector2 currentPointerScreenPos;
        private Camera mainCamera;
        private bool isInvulnerable = false;
        private Coroutine invulnerabilityCoroutine;

        [Header("Input Forgiveness")]
        [Tooltip("Grace period after walking off a ledge during which a jump is still allowed.")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("A jump pressed this long before landing / touching a wall is executed automatically.")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        [Tooltip("Grace period after leaving a wall during which a tap still performs a wall kick.")]
        [SerializeField] private float wallKickGrace = 0.1f;
        [Tooltip("Duration during which wall-kick lateral momentum overrides steering input (blends back to input).")]
        [SerializeField] private float wallKickLockDuration = 0.22f;
        private float kickLockRemaining = 0f;
        private float kickVelocityX = 0f;

        private float lastGroundedTime = -999f;
        private float lastWallContactTime = -999f;
        private float lastWallSideKickDir = 1f;
        private float bufferedJumpTime = -999f;

        [Header("Combo Settings")]
        [Tooltip("Minimum height gain (units) between landings for the landing to count toward the combo.")]
        [SerializeField] private float comboMinHeightGain = 0.6f;
        [Tooltip("Falling more than this many units below the last combo platform breaks the combo.")]
        [SerializeField] private float comboBreakFallDistance = 3f;
        private float lastComboLandY = float.NegativeInfinity;
        public int ComboCount => comboCount;
        public event System.Action<int> OnComboChanged;

        // Zero-GC caches
        private PointerEventData cachedPointerEventData;
        private readonly System.Collections.Generic.List<RaycastResult> uiRaycastResults = new System.Collections.Generic.List<RaycastResult>(16);
        private readonly Collider2D[] attackHitBuffer = new Collider2D[8];
        private ContactFilter2D enemyContactFilter;
        private int enemyLayer = -1;
        private bool pointerOverUIThisFrame;
        private static readonly WaitForSeconds Wait005 = new WaitForSeconds(0.05f);
        private static readonly WaitForSeconds Wait006 = new WaitForSeconds(0.06f);
        private static readonly WaitForSeconds Wait012 = new WaitForSeconds(0.12f);

        public const string TILT_PREF_KEY = "CatClimb_TiltEnabled";
        public const string SELECTED_SKIN_KEY = "CatClimb_SelectedSkin";
        public const string SELECTED_HAT_KEY = "CatClimb_SelectedHat";
        public const string SELECTED_TRAIL_KEY = "CatClimb_SelectedTrail";

        [Header("Cosmetic Hat Sprites")]
        public Sprite hatCrownSprite;
        public Sprite hatNinjaBandSprite;
        public Sprite hatPartySprite;
        public Sprite hatAstroSprite;

        private SpriteRenderer hatRenderer;
        private TrailRenderer cosmeticTrail;
        private static Material sharedTrailMat;
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> runtimeHatSprites = new System.Collections.Generic.Dictionary<string, Sprite>();

        public bool IsTiltControlEnabled => useTiltControl;

        public void SetTiltControlEnabled(bool enabled)
        {
            useTiltControl = enabled;
            PlayerPrefs.SetInt(TILT_PREF_KEY, enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (enabled && isActiveAndEnabled)
            {
                if (GravitySensor.current != null) InputSystem.EnableDevice(GravitySensor.current);
                if (Accelerometer.current != null) InputSystem.EnableDevice(Accelerometer.current);
            }
            else
            {
                if (GravitySensor.current != null) InputSystem.DisableDevice(GravitySensor.current);
                if (Accelerometer.current != null) InputSystem.DisableDevice(Accelerometer.current);
            }
        }

        private void Awake()
        {
            Instance = this;
            gameObject.tag = "Player";

            rb = GetComponent<Rigidbody2D>();
            circleCollider = GetComponent<CircleCollider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            mainCamera = Camera.main;

            // Tilt steering is opt-in (Settings). Unexpected drift from a slightly tilted phone feels like lost control.
            useTiltControl = PlayerPrefs.GetInt(TILT_PREF_KEY, 0) == 1;

            enemyLayer = LayerMask.NameToLayer("Enemy");
            enemyContactFilter = new ContactFilter2D { useTriggers = true };
            enemyContactFilter.SetLayerMask(LayerMask.GetMask("Enemy"));

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
                string selectedSkinId = PlayerPrefs.GetString(SELECTED_SKIN_KEY, "orange");
                spriteRenderer.color = GetSkinColor(selectedSkinId);
            }

            ApplySelectedHat();
            ApplySelectedTrail();
        }

        public static Color GetSkinColor(string skinId)
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

        private void ApplySelectedHat()
        {
            if (spriteRenderer == null) return;

            string hatId = PlayerPrefs.GetString(SELECTED_HAT_KEY, "none");
            if (string.IsNullOrEmpty(hatId) || hatId == "none")
            {
                if (hatRenderer != null)
                {
                    hatRenderer.enabled = false;
                }
                return;
            }

            if (hatRenderer == null)
            {
                Transform existing = spriteRenderer.transform.Find("CatHatVisual");
                if (existing != null)
                {
                    hatRenderer = existing.GetComponent<SpriteRenderer>();
                }
                else
                {
                    GameObject hatGo = new GameObject("CatHatVisual");
                    hatGo.transform.SetParent(spriteRenderer.transform, false);
                    hatRenderer = hatGo.AddComponent<SpriteRenderer>();
                    hatRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
                }
            }

            Sprite hatSprite = GetHatSprite(hatId);
            if (hatSprite != null)
            {
                hatRenderer.sprite = hatSprite;
                hatRenderer.color = Color.white;
                hatRenderer.enabled = true;
                UpdateHatTransform();
            }
            else
            {
                hatRenderer.enabled = false;
            }
        }

        private void UpdateHatTransform()
        {
            if (hatRenderer == null || !hatRenderer.enabled || spriteRenderer == null) return;

            string hatId = PlayerPrefs.GetString(SELECTED_HAT_KEY, "none");
            float hatScale = 0.30f;
            float hatOffsetX = 0f;
            float hatOffsetY = 0.04f;

            switch (hatId)
            {
                case "crown":
                    hatScale = 0.30f;
                    hatOffsetX = 0.00f;
                    hatOffsetY = 0.04f;
                    break;
                case "ninja_band":
                    hatScale = 0.32f;
                    hatOffsetX = 0.03f;
                    hatOffsetY = -0.04f;
                    break;
                case "party":
                    hatScale = 0.30f;
                    hatOffsetX = 0.00f;
                    hatOffsetY = 0.08f;
                    break;
                case "astro":
                    hatScale = 0.38f;
                    hatOffsetX = -0.01f;
                    hatOffsetY = -0.06f;
                    break;
            }

            // Head anchor in default left-facing sprite space (1.0 unit = 100px on the 128x128 canvas)
            Sprite currentCatSprite = spriteRenderer.sprite;
            float headX = -0.22f;
            float headY = 0.20f;

            if (currentCatSprite == idleSprite || currentCatSprite == transitionSprite)
            {
                headX = -0.22f;
                headY = 0.22f;
            }
            else if (currentCatSprite == walkingSprite || currentCatSprite == walkingSprite2 ||
                     currentCatSprite == walkingSprite3 || currentCatSprite == walkingSprite4)
            {
                headX = -0.23f;
                headY = 0.16f;
            }
            else if (currentCatSprite == crouchSprite)
            {
                headX = -0.30f;
                headY = 0.14f;
            }
            else if (currentCatSprite == crouchDeepSprite)
            {
                headX = -0.18f;
                headY = 0.05f;
            }
            else if (currentCatSprite == climbSprite || currentCatSprite == climbSprite2 ||
                     currentCatSprite == climbSprite3 || currentCatSprite == climbSprite4)
            {
                headX = -0.28f;
                headY = 0.34f;
            }
            else if (currentCatSprite == takeoffSprite || currentCatSprite == launchSprite || currentCatSprite == rise1Sprite)
            {
                headX = -0.26f;
                headY = 0.48f;
            }
            else if (currentCatSprite == rise2Sprite || currentCatSprite == verticalJumpSprite)
            {
                headX = -0.20f;
                headY = 0.50f;
            }
            else if (currentCatSprite == jumpSprite)
            {
                headX = -0.24f;
                headY = 0.36f;
            }
            else if (currentCatSprite == attackSprite1)
            {
                headX = -0.12f;
                headY = 0.38f;
            }
            else if (currentCatSprite == attackSprite2 || currentCatSprite == attackSprite3 || currentCatSprite == attackSprite4)
            {
                headX = -0.22f;
                headY = 0.34f;
            }

            float finalX = headX + hatOffsetX;
            float finalY = headY + hatOffsetY;
            if (spriteRenderer.flipX)
            {
                finalX = -finalX;
            }

            hatRenderer.flipX = spriteRenderer.flipX;
            hatRenderer.transform.localScale = new Vector3(hatScale, hatScale, 1f);
            hatRenderer.transform.localPosition = new Vector3(finalX, finalY, 0f);
        }

        public Sprite GetHatSprite(string hatId)
        {
#if UNITY_EDITOR
            if (hatCrownSprite == null) hatCrownSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/HatCrown.png");
            if (hatNinjaBandSprite == null) hatNinjaBandSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/HatNinjaBand.png");
            if (hatPartySprite == null) hatPartySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/HatParty.png");
            if (hatAstroSprite == null) hatAstroSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/HatAstro.png");
#endif
            switch (hatId)
            {
                case "crown": return hatCrownSprite != null ? hatCrownSprite : GetOrCreateRuntimeHatSprite("crown");
                case "ninja_band": return hatNinjaBandSprite != null ? hatNinjaBandSprite : GetOrCreateRuntimeHatSprite("ninja_band");
                case "party": return hatPartySprite != null ? hatPartySprite : GetOrCreateRuntimeHatSprite("party");
                case "astro": return hatAstroSprite != null ? hatAstroSprite : GetOrCreateRuntimeHatSprite("astro");
                default: return null;
            }
        }

        public static Sprite GetOrCreateRuntimeHatSprite(string hatId)
        {
            if (string.IsNullOrEmpty(hatId) || hatId == "none") return null;
            if (runtimeHatSprites.TryGetValue(hatId, out Sprite cached) && cached != null)
            {
                return cached;
            }

#if UNITY_EDITOR
            string assetPath = null;
            if (hatId == "crown") assetPath = "Assets/_MyProject/Sprites/HatCrown.png";
            else if (hatId == "ninja_band") assetPath = "Assets/_MyProject/Sprites/HatNinjaBand.png";
            else if (hatId == "party") assetPath = "Assets/_MyProject/Sprites/HatParty.png";
            else if (hatId == "astro") assetPath = "Assets/_MyProject/Sprites/HatAstro.png";

            if (!string.IsNullOrEmpty(assetPath))
            {
                Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (loaded != null)
                {
                    runtimeHatSprites[hatId] = loaded;
                    return loaded;
                }
            }
#endif
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    tex.SetPixel(x, y, clear);

            if (hatId == "crown")
            {
                Color gold = new Color(1f, 0.84f, 0.16f, 1f);
                Color rim = new Color(0.86f, 0.6f, 0.1f, 1f);
                Color gem = new Color(0.95f, 0.2f, 0.3f, 1f);
                for (int x = 3; x <= 12; x++)
                {
                    tex.SetPixel(x, 5, rim);
                    tex.SetPixel(x, 6, gold);
                    tex.SetPixel(x, 7, gold);
                    tex.SetPixel(x, 8, gold);
                }
                tex.SetPixel(3, 9, gold); tex.SetPixel(3, 10, gold);
                tex.SetPixel(7, 9, gold); tex.SetPixel(8, 9, gold); tex.SetPixel(7, 10, gold); tex.SetPixel(8, 10, gold);
                tex.SetPixel(12, 9, gold); tex.SetPixel(12, 10, gold);
                tex.SetPixel(5, 7, gem); tex.SetPixel(10, 7, gem);
            }
            else if (hatId == "ninja_band")
            {
                Color red = new Color(0.9f, 0.18f, 0.24f, 1f);
                Color plate = new Color(0.86f, 0.9f, 0.96f, 1f);
                for (int x = 2; x <= 13; x++)
                {
                    tex.SetPixel(x, 6, red);
                    tex.SetPixel(x, 7, red);
                    tex.SetPixel(x, 8, red);
                }
                for (int x = 5; x <= 10; x++) tex.SetPixel(x, 7, plate);
                tex.SetPixel(14, 5, red); tex.SetPixel(15, 4, red);
            }
            else if (hatId == "party")
            {
                Color pink = new Color(0.96f, 0.32f, 0.68f, 1f);
                Color cyan = new Color(0.28f, 0.88f, 0.98f, 1f);
                Color pom = new Color(1f, 0.9f, 0.25f, 1f);
                for (int row = 0; row < 7; row++)
                {
                    int half = 5 - (row / 2);
                    int y = 5 + row;
                    for (int x = 8 - half; x <= 7 + half; x++)
                    {
                        tex.SetPixel(x, y, (row == 0) ? cyan : pink);
                    }
                }
                tex.SetPixel(7, 12, pom); tex.SetPixel(8, 12, pom);
                tex.SetPixel(7, 13, pom); tex.SetPixel(8, 13, pom);
            }
            else if (hatId == "astro")
            {
                Color visor = new Color(0.38f, 0.86f, 1f, 0.82f);
                Color rim = new Color(0.92f, 0.95f, 1f, 1f);
                for (int y = 5; y <= 13; y++)
                {
                    for (int x = 2; x <= 13; x++)
                    {
                        float dx = x - 7.5f;
                        float dy = y - 9f;
                        float distSq = dx * dx + dy * dy;
                        if (distSq <= 26f)
                        {
                            tex.SetPixel(x, y, (distSq >= 17f || y == 5) ? rim : visor);
                        }
                    }
                }
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            runtimeHatSprites[hatId] = sp;
            return sp;
        }

        private void ApplySelectedTrail()
        {
            string trailId = PlayerPrefs.GetString(SELECTED_TRAIL_KEY, "none");
            if (string.IsNullOrEmpty(trailId) || trailId == "none")
            {
                if (cosmeticTrail != null)
                {
                    cosmeticTrail.emitting = false;
                    cosmeticTrail.Clear();
                }
                return;
            }

            if (cosmeticTrail == null)
            {
                Transform existing = transform.Find("CatTrailVisual");
                if (existing != null)
                {
                    cosmeticTrail = existing.GetComponent<TrailRenderer>();
                }
                else
                {
                    GameObject trailGo = new GameObject("CatTrailVisual");
                    trailGo.transform.SetParent(transform, false);
                    trailGo.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                    cosmeticTrail = trailGo.AddComponent<TrailRenderer>();
                    cosmeticTrail.time = 0.28f;
                    cosmeticTrail.minVertexDistance = 0.08f;
                    cosmeticTrail.startWidth = 0.24f;
                    cosmeticTrail.endWidth = 0.0f;
                    cosmeticTrail.numCornerVertices = 2;
                    cosmeticTrail.numCapVertices = 2;
                    cosmeticTrail.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 10) - 1;

                    if (sharedTrailMat == null)
                    {
                        Shader sh = Shader.Find("Sprites/Default");
                        if (sh != null) sharedTrailMat = new Material(sh);
                    }
                    if (sharedTrailMat != null)
                    {
                        cosmeticTrail.sharedMaterial = sharedTrailMat;
                    }
                }
            }

            Color startCol = GetTrailColor(trailId, out Color endCol);
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(startCol, 0f), new GradientColorKey(endCol, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.80f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            cosmeticTrail.colorGradient = grad;
            cosmeticTrail.emitting = true;
        }

        public static Color GetTrailColor(string trailId, out Color endColor)
        {
            switch (trailId)
            {
                case "sakura":
                    endColor = new Color(1f, 0.85f, 0.92f, 0f);
                    return new Color(1f, 0.55f, 0.78f, 1f);
                case "starlight":
                    endColor = new Color(1f, 0.95f, 0.5f, 0f);
                    return new Color(1f, 0.84f, 0.15f, 1f);
                case "cyber":
                default:
                    endColor = new Color(0.6f, 0.3f, 1f, 0f);
                    return new Color(0.18f, 0.92f, 1f, 1f);
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

        /// <summary>
        /// Returns whether the active pointer is over an interactive UI element this frame.
        /// The value is computed once per frame in <see cref="Update"/> (zero allocations).
        /// </summary>
        private bool IsPointerOverUI()
        {
            return pointerOverUIThisFrame;
        }

        private bool ComputePointerOverUI(Vector2 pointerPosition)
        {
            if (EventSystem.current == null) return false;

            if (cachedPointerEventData == null)
            {
                cachedPointerEventData = new PointerEventData(EventSystem.current);
            }
            cachedPointerEventData.Reset();
            cachedPointerEventData.position = pointerPosition;

            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(cachedPointerEventData, uiRaycastResults);

            for (int i = 0; i < uiRaycastResults.Count; i++)
            {
                GameObject hitGo = uiRaycastResults[i].gameObject;
                if (hitGo != null && hitGo.GetComponentInParent<UnityEngine.UI.Selectable>() != null)
                {
                    uiRaycastResults.Clear();
                    return true;
                }
            }

            uiRaycastResults.Clear();
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

            // Single UI raycast per frame (only when a pointer is actually active)
            pointerOverUIThisFrame = (isPointerHeld || pointerUpThisFrame) && ComputePointerOverUI(pointerScreenPos);

            // Handle touch begin
            if (pointerDownThisFrame)
            {
                touchStartedOverUI = pointerOverUIThisFrame;
                if (!touchStartedOverUI)
                {
                    touchStartScreenPos = pointerScreenPos;
                    touchStartTime = Time.time;
                    hasSwipedThisTouch = false;
                }
            }

            // A touch that began on a button belongs to the UI for its entire lifetime
            if (touchStartedOverUI)
            {
                pointerOverUIThisFrame = true;
                if (pointerUpThisFrame) touchStartedOverUI = false;
            }
            if (pointerOverUIThisFrame)
            {
                isPointerHeld = false;
            }

            // Execute a buffered jump as soon as it becomes possible
            TryConsumeBufferedJump();

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
            isClimbing = isTouchingWall && climbInput && kickLockRemaining <= 0f;

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
                    HandleTapAction();
                }
            }

            // 7. Keyboard Space / Jump triggers
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                HandleTapAction();
            }
        }

        // ---------------------------------------------------------------
        // Forgiving action resolution (coyote time / wall-kick grace / jump buffer)
        // ---------------------------------------------------------------

        private bool IsInCoyoteWindow()
        {
            return !isGrounded && (Time.time - lastGroundedTime) <= coyoteTime && rb.linearVelocity.y <= 0.15f;
        }

        private bool TryGetWallKickDirection(out float kickDir)
        {
            if (isTouchingWall)
            {
                kickDir = isTouchingLeftWall ? 1f : -1f;
                return true;
            }
            if (!isGrounded && (Time.time - lastWallContactTime) <= wallKickGrace)
            {
                kickDir = lastWallSideKickDir;
                return true;
            }
            kickDir = 0f;
            return false;
        }

        private bool CanGroundJump()
        {
            return isGrounded || allowInfiniteJumps || IsInCoyoteWindow();
        }

        /// <summary>
        /// Tap / Space: wall kick if on (or just left) a wall, jump if grounded (or within coyote time),
        /// otherwise buffer the jump and perform an aerial melee swipe.
        /// </summary>
        private void HandleTapAction()
        {
            if (TryGetWallKickDirection(out float kickDir))
            {
                ExecuteWallKick(kickDir);
                return;
            }

            if (CanGroundJump())
            {
                Jump();
                return;
            }

            bufferedJumpTime = Time.time;
            MeleeAttack();
        }

        private void TryConsumeBufferedJump()
        {
            if (bufferedJumpTime < 0f) return;

            if (Time.time - bufferedJumpTime > jumpBufferTime)
            {
                bufferedJumpTime = -999f;
                return;
            }

            if (isTouchingWall)
            {
                bufferedJumpTime = -999f;
                ExecuteWallKick(isTouchingLeftWall ? 1f : -1f);
            }
            else if (isGrounded)
            {
                bufferedJumpTime = -999f;
                Jump();
            }
        }

        private void ProcessSwipe(Vector2 swipeDelta)
        {
            if (TryGetWallKickDirection(out float wallKickDir))
            {
                // If swipe is directed away from wall OR upwards
                if (Mathf.Sign(swipeDelta.x) == Mathf.Sign(wallKickDir) || swipeDelta.y > 0f)
                {
                    ExecuteWallKick(wallKickDir);
                    return;
                }
            }

            if (swipeDelta.y > 0f)
            {
                if (CanGroundJump())
                {
                    Jump();
                    return;
                }
                bufferedJumpTime = Time.time;
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

            // Preserve the lateral kick momentum briefly so FixedUpdate steering doesn't erase it
            kickVelocityX = kickDirX * wallKickForceX;
            kickLockRemaining = wallKickLockDuration;

            // Consume forgiveness windows so one input can't trigger two actions
            lastWallContactTime = -999f;
            lastGroundedTime = -999f;
            bufferedJumpTime = -999f;

            // Wall-kick lateral stretch and dust puff
            ApplySquashAndStretch(1.22f, 0.8f);
            FXManager.Instance?.EmitJumpDust(transform.position + new Vector3(-kickDirX * 0.25f, -0.1f, 0f), new Vector2(kickDirX, 0.4f));
            AudioManager.Instance?.PlayWallKick();
            Haptics.Light();
            GameManager.Instance?.RecordWallKick();

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
            Haptics.Light();
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
            var treats = FishTreat.ActiveFishTreats;
            Vector3 playerPos = transform.position;
            float magnetRadiusSq = magnetRadius * magnetRadius;

            for (int i = treats.Count - 1; i >= 0; i--)
            {
                FishTreat treat = treats[i];
                if (treat == null) continue;

                float distSq = (playerPos - treat.transform.position).sqrMagnitude;
                if (distSq <= magnetRadiusSq)
                {
                    treat.StartAttraction();
                }
            }
        }

        private void FixedUpdate()
        {
            // 1. Ground detection
            bool wasGroundedBefore = isGrounded;
            Collider2D groundCollider = null;
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
                            groundCollider = castResults[i].collider;
                            break;
                        }
                    }
                }
            }
            else
            {
                isGrounded = true;
            }

            if (isGrounded)
            {
                lastGroundedTime = Time.time;
            }

            if (!wasGroundedBefore && isGrounded)
            {
                OnLanded(groundCollider);
            }
            lastVerticalVelocity = rb.linearVelocity.y;

            // Falling well below the last combo platform breaks the streak
            if (comboCount > 0 && transform.position.y < lastComboLandY - comboBreakFallDistance)
            {
                BreakCombo();
            }

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

            if (isTouchingWall && !isGrounded)
            {
                lastWallContactTime = Time.time;
                lastWallSideKickDir = isTouchingLeftWall ? 1f : -1f;
            }

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

            // 4. Horizontal velocity: steering input, blended with wall-kick momentum right after a kick
            float targetHorizontalVelocity = horizontalInput * moveSpeed;
            if (kickLockRemaining > 0f)
            {
                float kickWeight = Mathf.Clamp01(kickLockRemaining / Mathf.Max(0.01f, wallKickLockDuration));
                targetHorizontalVelocity = Mathf.Lerp(targetHorizontalVelocity, kickVelocityX, kickWeight);
                kickLockRemaining -= Time.fixedDeltaTime;
            }

            // 5. Apply final velocities
            rb.linearVelocity = new Vector2(targetHorizontalVelocity, targetVerticalVelocity);
        }

        public void Jump()
        {
            bool inCoyote = IsInCoyoteWindow();
            if (isGrounded || inCoyote || isTouchingWall || isClimbing || allowInfiniteJumps)
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
                    physicallyGrounded = isGrounded || inCoyote;
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

                // Consume forgiveness windows
                lastGroundedTime = -999f;
                bufferedJumpTime = -999f;

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

                    // Physics impulse is applied on the input frame; the coroutine only plays cosmetic frames.
                    ApplyGroundJumpImpulse();
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

        private void ApplyGroundJumpImpulse()
        {
            isGrounded = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            ApplySquashAndStretch(0.8f, 1.25f);
            FXManager.Instance?.EmitJumpDust(transform.position + Vector3.down * 0.28f, Vector2.down);
            AudioManager.Instance?.PlayJump();
        }

        private void OnLanded(Collider2D groundCollider)
        {
            float impactSpeed = Mathf.Abs(lastVerticalVelocity);
            float squashX = Mathf.Clamp(1.0f + impactSpeed * 0.035f, 1.15f, 1.35f);
            float squashY = Mathf.Clamp(1.0f - impactSpeed * 0.035f, 0.65f, 0.85f);
            ApplySquashAndStretch(squashX, squashY);

            FXManager.Instance?.EmitJumpDust(transform.position + Vector3.down * 0.25f, Vector2.up);

            UpdateComboOnLanding(groundCollider);
        }

        /// <summary>
        /// Combo = consecutive landings on NEW, HIGHER platforms. Re-landing on the same platform does nothing;
        /// touching the starting floor, falling far, or getting hit resets it. Milestones pay fish.
        /// </summary>
        private void UpdateComboOnLanding(Collider2D groundCollider)
        {
            float landY = transform.position.y;
            bool onPlatform = groundCollider != null && PlatformManager.Instance != null &&
                              PlatformManager.Instance.IsPlatform(groundCollider.gameObject);

            if (!onPlatform)
            {
                BreakCombo();
                lastComboLandY = landY;
                return;
            }

            if (landY > lastComboLandY + comboMinHeightGain)
            {
                comboCount++;
                lastComboLandY = landY;
                OnComboChanged?.Invoke(comboCount);
                GameManager.Instance?.ReportCombo(comboCount);
                RewardComboMilestone(comboCount);
            }
            else if (landY < lastComboLandY - comboBreakFallDistance)
            {
                BreakCombo();
                lastComboLandY = landY;
            }
        }

        private void RewardComboMilestone(int combo)
        {
            int bonusFish = 0;
            string label = null;
            Color color = Color.white;

            if (combo == 3) { bonusFish = 1; label = "3x COMBO!"; color = new Color(0.4f, 0.95f, 0.5f, 1f); }
            else if (combo == 5) { bonusFish = 2; label = "5x STREAK!"; color = new Color(0.3f, 0.85f, 1f, 1f); }
            else if (combo == 8) { bonusFish = 3; label = "ON FIRE!"; color = new Color(1f, 0.5f, 0.2f, 1f); }
            else if (combo == 12) { bonusFish = 5; label = "UNSTOPPABLE!"; color = new Color(1f, 0.3f, 0.85f, 1f); }
            else if (combo > 12 && (combo - 12) % 4 == 0) { bonusFish = 5; label = $"{combo}x UNSTOPPABLE!"; color = new Color(1f, 0.3f, 0.85f, 1f); }

            if (bonusFish <= 0) return;

            GameManager.Instance?.AddCoins(bonusFish);
            UIManager.Instance?.ShowToast($"{label}  +{bonusFish} FISH", color);
            AudioManager.Instance?.PlayStreak();
            FXManager.Instance?.EmitCoinSparkle(transform.position + Vector3.up * 0.4f);
            Haptics.Light();
        }

        private void BreakCombo()
        {
            if (comboCount == 0) return;
            comboCount = 0;
            OnComboChanged?.Invoke(0);
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

            // Non-allocating spatial query on the "Enemy" layer
            int hitCount = Physics2D.OverlapCircle(attackCenter, attackRadius, enemyContactFilter, attackHitBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = attackHitBuffer[i];
                if (hit == null) continue;
                Enemy enemyComponent = hit.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    enemyComponent.TakeDamage();
                }
                attackHitBuffer[i] = null;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isInvulnerable) return;

            // Trigger Defeat if we collide with a solid hazard on the "Enemy" layer (e.g. wall spikes)
            if (collision.gameObject.layer == enemyLayer)
            {
                OnHazardHit();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isInvulnerable) return;

            // Trigger Defeat if we overlap with a trigger hazard on the "Enemy" layer (e.g. platform enemy triangles)
            if (other.gameObject.layer == enemyLayer)
            {
                OnHazardHit();
            }
        }

        private void OnHazardHit()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            CameraFollowTarget.Instance?.AddTrauma(0.6f);
            BreakCombo();
            AudioManager.Instance?.PlayHit();
            Haptics.Heavy();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.HandlePlayerDefeat(true);
            }
        }

        public void RespawnAt(Vector3 respawnPos)
        {
            BreakCombo();
            lastComboLandY = float.NegativeInfinity;
            kickLockRemaining = 0f;
            bufferedJumpTime = -999f;
            lastWallContactTime = -999f;
            lastGroundedTime = -999f;

            transform.position = respawnPos;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = new Vector2(0f, 2.5f); // Gentle hop that settles back onto the safe platform
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
                yield return Wait012;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = baseColor;
                }
                yield return Wait012;
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
                    UpdateHatTransform();
                    wasGrounded = isGrounded;
                    return;
                }

                // 4. If landing transition is running, let it control the sprite (and handle horizontal flip)
                if (landingTransitionCoroutine != null)
                {
                    spriteRenderer.flipX = facingDirectionX > 0f;
                    UpdateHatTransform();
                    wasGrounded = isGrounded;
                    return;
                }

                // 5. If attack animation is running, let it control the sprite (and handle horizontal flip)
                if (attackCoroutine != null)
                {
                    spriteRenderer.flipX = facingDirectionX > 0f;
                    UpdateHatTransform();
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
                }

                UpdateHatTransform();
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
            bool isRight = isVertical ? (facingDirectionX > 0f) : (jumpDirectionX > 0f);

            // Frame 1: Immediate Takeoff (physics impulse was already applied in Jump())
            if (isVertical)
            {
                if (launchSprite != null) spriteRenderer.sprite = launchSprite;
            }
            else
            {
                if (takeoffSprite != null) spriteRenderer.sprite = takeoffSprite;
            }
            spriteRenderer.flipX = isRight;
            yield return Wait005;

            // Frame 2: Rise 1
            if (isVertical)
            {
                if (rise1Sprite != null) spriteRenderer.sprite = rise1Sprite;
            }
            else
            {
                if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
            }
            spriteRenderer.flipX = isRight;
            yield return Wait005;

            // Frame 3: Rise 2
            if (isVertical)
            {
                if (rise2Sprite != null) spriteRenderer.sprite = rise2Sprite;
            }
            else
            {
                if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
            }
            spriteRenderer.flipX = isRight;
            yield return Wait005;

            jumpTransitionCoroutine = null;
            // Let the standard state machine in LateUpdate take over
            currentVisualState = isVertical ? CatState.VerticalJump : (jumpDirectionX < 0f ? CatState.JumpingLeft : CatState.JumpingRight);
        }

        private System.Collections.IEnumerator AnimateLandingTransition(bool isVertical)
        {
            currentVisualState = CatState.LandingTransition;
            bool isRight = facingDirectionX > 0f;

            // Frame 1: Deep impact cushion
            if (crouchDeepSprite != null)
            {
                spriteRenderer.sprite = crouchDeepSprite;
            }
            else if (crouchSprite != null)
            {
                spriteRenderer.sprite = crouchSprite;
            }
            spriteRenderer.flipX = isRight;
            yield return Wait006;

            // Frame 2: Mid-crouch recovery
            if (crouchSprite != null)
            {
                spriteRenderer.sprite = crouchSprite;
            }
            else if (idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
            spriteRenderer.flipX = isRight;
            yield return Wait006;

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
            yield return Wait005;

            // Frame 2
            if (attackSprite2 != null) spriteRenderer.sprite = attackSprite2;
            spriteRenderer.flipX = facingDirectionX > 0f;
            yield return Wait005;

            // Frame 3
            if (attackSprite3 != null) spriteRenderer.sprite = attackSprite3;
            spriteRenderer.flipX = facingDirectionX > 0f;
            yield return Wait005;

            // Frame 4
            if (attackSprite4 != null) spriteRenderer.sprite = attackSprite4;
            spriteRenderer.flipX = facingDirectionX > 0f;
            yield return Wait005;

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
                    spriteRenderer.flipX = facingDirectionX > 0f;
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
                    spriteRenderer.flipX = facingDirectionX > 0f;
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
