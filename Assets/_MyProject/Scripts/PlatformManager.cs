using System.Collections.Generic;
using UnityEngine;

namespace MyProject
{
    public class PlatformManager : MonoBehaviour
    {
        [Header("Pool Settings")]
        [Tooltip("The platform prefab. If null, a default 2D platform will be generated automatically.")]
        [SerializeField] private GameObject platformPrefab;
        [SerializeField] private int poolSize = 20;

        [Header("Enemy Settings")]
        [Tooltip("The enemy sprite. Assigned programmatically by the scene builder.")]
        public Sprite enemySprite;
        [Tooltip("The target world scale for the enemy sprite (default is 0.5 to match the cat).")]
        [SerializeField] private float enemyScale = 0.5f;

        [Header("Spawn Settings")]
        [SerializeField] private float minVerticalSpacing = 1.2f;
        [SerializeField] private float maxVerticalSpacing = 1.9f;
        [SerializeField] private float initialSpawnY = -3f;

        [Header("Recycle Settings")]
        [Tooltip("Distance below the camera Y position at which a platform is recycled.")]
        [SerializeField] private float recycleOffset = 6f;

        [Header("Moving Platform Settings")]
        [Tooltip("Score threshold required to unlock moving platforms. Set to 100 for testing (change to 3000 for production).")]
        public int movingPlatformScoreThreshold = 100;

        private List<GameObject> platformPool = new List<GameObject>();
        private float highestPlatformY;
        private Transform cameraTransform;

        [Header("Platform Sprites")]
        public Sprite groundSprite;
        public Sprite groundSquareSprite;
        public Sprite groundVerticalSprite;
        public Sprite groundDirtOnlySprite;
        public Sprite wallSquareSprite;

        public static PlatformManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
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
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogError("Main Camera not found in the scene! PlatformManager requires a Main Camera.");
            }

            // Cache sprites once on start (using UnityEditor.AssetDatabase fallback only in Editor builds)
            if (groundSprite == null)
            {
                groundSprite = Resources.Load<Sprite>("GroundSprite");
#if UNITY_EDITOR
                if (groundSprite == null)
                {
                    groundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/GroundSprite.png");
                }
#endif
            }

            if (groundSquareSprite == null)
            {
                groundSquareSprite = Resources.Load<Sprite>("GroundSquare");
#if UNITY_EDITOR
                if (groundSquareSprite == null)
                {
                    groundSquareSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/GroundSquare.png");
                }
#endif
            }

            if (groundVerticalSprite == null)
            {
                groundVerticalSprite = Resources.Load<Sprite>("GroundVertical");
#if UNITY_EDITOR
                if (groundVerticalSprite == null)
                {
                    groundVerticalSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/GroundVertical.png");
                }
#endif
            }

            if (groundDirtOnlySprite == null)
            {
                groundDirtOnlySprite = Resources.Load<Sprite>("GroundDirtOnly");
#if UNITY_EDITOR
                if (groundDirtOnlySprite == null)
                {
                    groundDirtOnlySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/GroundDirtOnly.png");
                }
#endif
            }

            if (wallSquareSprite == null)
            {
                wallSquareSprite = Resources.Load<Sprite>("WallSquare");
#if UNITY_EDITOR
                if (wallSquareSprite == null)
                {
                    wallSquareSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/WallSquare.png");
                }
#endif
            }

            InitializePool();
        }

        private Vector3 GetPlatformScale(int shapeChoice)
        {
            if (shapeChoice == 0) return new Vector3(0.8f, 0.8f, 1f);  // Square
            if (shapeChoice == 1) return new Vector3(0.5f, 1.5f, 1f);  // Vertical
            return new Vector3(1.5f, 0.3f, 1f);                       // Horizontal
        }

        private float GetPlatformWidthForChoice(int shapeChoice)
        {
            return GetPlatformScale(shapeChoice).x * 1.28f;
        }

        private Rect GetPlatformWorldRect(Vector3 platformPos, Vector3 platformScale)
        {
            float worldWidth = platformScale.x * 1.28f;
            float worldHeight = platformScale.y * 1.28f;
            return new Rect(platformPos.x - worldWidth / 2f, platformPos.y - worldHeight / 2f, worldWidth, worldHeight);
        }

        private Rect GetEnemyWorldRect(Vector3 platformPos, Vector3 platformScale, Vector3 enemyLocalPos)
        {
            float enemyWorldX = platformPos.x + enemyLocalPos.x * platformScale.x;
            float enemyWorldY = platformPos.y + enemyLocalPos.y * platformScale.y;
            return new Rect(enemyWorldX - enemyScale / 2f, enemyWorldY - enemyScale / 2f, enemyScale, enemyScale);
        }

        private bool RectsOverlap(Rect r1, Rect r2, float margin = 0.05f)
        {
            return (r1.xMin - margin < r2.xMax &&
                    r1.xMax + margin > r2.xMin &&
                    r1.yMin - margin < r2.yMax &&
                    r1.yMax + margin > r2.yMin);
        }

        private bool IsWallGapInvalid(Rect platformRect, float minClearance = 0.55f)
        {
            float leftWallX = -2.6125f;
            float rightWallX = 2.6125f;

            float leftWallGap = platformRect.xMin - leftWallX;
            float rightWallGap = rightWallX - platformRect.xMax;

            // Reject if penetrating either wall
            if (leftWallGap < -0.02f || rightWallGap < -0.02f)
            {
                return true;
            }

            // Reject if gap to left wall is narrow (between 0.02 and minClearance)
            if (leftWallGap >= 0.02f && leftWallGap < minClearance)
            {
                return true;
            }

            // Reject if gap to right wall is narrow (between 0.02 and minClearance)
            if (rightWallGap >= 0.02f && rightWallGap < minClearance)
            {
                return true;
            }

            return false;
        }

        private bool RectsOverlapOrNarrowGap(Rect r1, Rect r2, float minClearance = 0.55f)
        {
            // 1. Direct overlap check with safety margin
            if (r1.xMin - 0.05f < r2.xMax &&
                r1.xMax + 0.05f > r2.xMin &&
                r1.yMin - 0.05f < r2.yMax &&
                r1.yMax + 0.05f > r2.yMin)
            {
                return true;
            }

            // 2. Only check horizontal gap if platforms are close in vertical height range
            float yOverlap = Mathf.Min(r1.yMax, r2.yMax) - Mathf.Max(r1.yMin, r2.yMin);
            bool nearVertically = yOverlap > -0.6f;

            if (nearVertically)
            {
                float hGap = -1f;
                if (r1.xMin > r2.xMax) hGap = r1.xMin - r2.xMax;
                else if (r2.xMin > r1.xMax) hGap = r2.xMin - r1.xMax;

                // Reject if horizontal gap is positive but smaller than minClearance (narrow impassable gap)
                if (hGap >= 0f && hGap < minClearance)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsValidPlacement(GameObject currentPlatform, Vector3 candidatePos, Vector3 platformScale, bool hasEnemy, Vector3 enemyLocalPos)
        {
            Rect candidatePlatformRect = GetPlatformWorldRect(candidatePos, platformScale);

            // 1. Check Platform vs Wall Gap (Left Wall & Right Wall)
            if (IsWallGapInvalid(candidatePlatformRect, 0.55f))
            {
                return false;
            }

            Rect candidateEnemyRect = hasEnemy ? GetEnemyWorldRect(candidatePos, platformScale, enemyLocalPos) : new Rect();

            for (int i = 0; i < platformPool.Count; i++)
            {
                GameObject other = platformPool[i];
                if (other == null || other == currentPlatform || !other.activeInHierarchy)
                    continue;

                Vector3 otherPos = other.transform.position;
                Vector3 otherScale = other.transform.localScale;
                Rect otherPlatformRect = GetPlatformWorldRect(otherPos, otherScale);

                // 2. Check Platform vs Platform overlap & narrow gap (Cat diameter 0.50f + 0.05f = 0.55f)
                if (RectsOverlapOrNarrowGap(candidatePlatformRect, otherPlatformRect, 0.55f))
                {
                    return false;
                }

                // 3. Check Platform vs Other Platform's active Enemy
                Transform otherEnemyTransform = other.transform.Find("Enemy");
                if (otherEnemyTransform != null && otherEnemyTransform.gameObject.activeSelf)
                {
                    Rect otherEnemyRect = new Rect(
                        otherEnemyTransform.position.x - enemyScale / 2f,
                        otherEnemyTransform.position.y - enemyScale / 2f,
                        enemyScale,
                        enemyScale
                    );

                    if (RectsOverlap(candidatePlatformRect, otherEnemyRect))
                    {
                        return false;
                    }

                    if (hasEnemy && RectsOverlap(candidateEnemyRect, otherEnemyRect))
                    {
                        return false;
                    }
                }

                // 4. Check Candidate Enemy vs Other Platform
                if (hasEnemy && RectsOverlap(candidateEnemyRect, otherPlatformRect))
                {
                    return false;
                }
            }

            return true;
        }

        private void InitializePool()
        {
            GameObject template = platformPrefab;

            // If no prefab is assigned, programmatically create a default platform
            bool createdTemporaryTemplate = false;
            if (template == null)
            {
                template = CreateDefaultPlatformTemplate();
                createdTemporaryTemplate = true;
            }

            float currentSpawnY = initialSpawnY;

            for (int i = 0; i < poolSize; i++)
            {
                GameObject platformInstance = Instantiate(template, transform);
                platformPool.Add(platformInstance);

                // Pick a random shape choice (0 = Square, 1 = Vertical, 2 = Horizontal)
                int shapeChoice = Random.Range(0, 3);
                ConfigurePlatformShape(platformInstance, shapeChoice);

                Vector3 platformScale = platformInstance.transform.localScale;
                float worldWidth = platformScale.x * 1.28f;
                float maxX = Mathf.Max(0f, 2.6125f - (worldWidth / 2f));
                float airMaxX = Mathf.Max(0f, 2.0625f - (worldWidth / 2f));

                bool shouldSpawnEnemy = Random.value <= 0.1f;

                Vector3 chosenPos = Vector3.zero;
                Vector3 chosenEnemyLocalPos = Vector3.zero;
                bool found = false;

                float targetY = currentSpawnY;
                for (int yStep = 0; yStep < 30 && !found; yStep++)
                {
                    for (int xStep = 0; xStep < 20; xStep++)
                    {
                        float candidateX;
                        float randVal = Random.value;
                        if (randVal < 0.15f)
                        {
                            candidateX = -maxX; // Flush against left wall (15%)
                        }
                        else if (randVal < 0.30f)
                        {
                            candidateX = maxX; // Flush against right wall (15%)
                        }
                        else
                        {
                            candidateX = Random.Range(-airMaxX, airMaxX); // Spawning freely in middle/air (70%)
                        }

                        Vector3 candidatePos = new Vector3(candidateX, targetY, 0f);

                        Vector3 candidateEnemyLocalPos = Vector3.zero;
                        if (shouldSpawnEnemy)
                        {
                            float maxWorldOffsetX = Mathf.Max(0f, (worldWidth / 2f) - (enemyScale / 2f));
                            float maxLocalX = maxWorldOffsetX / platformScale.x;
                            float randomLocalX = Random.Range(-maxLocalX, maxLocalX);
                            float localY = 0.64f + ((enemyScale / 2f) / platformScale.y);
                            candidateEnemyLocalPos = new Vector3(randomLocalX, localY, 0f);
                        }

                        if (IsValidPlacement(platformInstance, candidatePos, platformScale, shouldSpawnEnemy, candidateEnemyLocalPos))
                        {
                            chosenPos = candidatePos;
                            chosenEnemyLocalPos = candidateEnemyLocalPos;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        targetY += 0.2f;
                    }
                }

                if (!found)
                {
                    float candidateX = Random.Range(-maxX, maxX);
                    chosenPos = new Vector3(candidateX, targetY, 0f);
                    if (shouldSpawnEnemy)
                    {
                        float localY = 0.64f + ((enemyScale / 2f) / platformScale.y);
                        chosenEnemyLocalPos = new Vector3(0f, localY, 0f);
                    }
                }

                platformInstance.transform.position = chosenPos;
                platformInstance.SetActive(true);

                // Create and attach the pooled Enemy child GameObject
                CreateEnemyChild(platformInstance, shouldSpawnEnemy, chosenEnemyLocalPos);

                highestPlatformY = Mathf.Max(highestPlatformY, chosenPos.y);
                currentSpawnY = highestPlatformY + Random.Range(minVerticalSpacing, maxVerticalSpacing);
            }

            // Cleanup the temporary template if we created one
            if (createdTemporaryTemplate)
            {
                Destroy(template);
            }

            Debug.Log($"Initialized platform pool with {poolSize} randomized platforms and pooled enemies.");
        }

        private void ConfigurePlatformShape(GameObject platform, int shapeChoice)
        {
            SpriteRenderer spriteRenderer = platform.GetComponent<SpriteRenderer>();
            BoxCollider2D collider = platform.GetComponent<BoxCollider2D>();
            // Remove any PlatformEffector2D so platforms are standard solid blocks
            PlatformEffector2D effector = platform.GetComponent<PlatformEffector2D>();
            if (effector != null)
            {
                Destroy(effector);
            }

            if (collider != null)
            {
                collider.usedByEffector = false;
            }

            // Always destroy any dynamic child rendering objects from previous recycles (cleanup fallback)
            Transform oldGrassCap = platform.transform.Find("GrassCap");
            if (oldGrassCap != null) Destroy(oldGrassCap.gameObject);
            Transform oldDirtBody = platform.transform.Find("DirtBody");
            if (oldDirtBody != null) Destroy(oldDirtBody.gameObject);

            // Re-enable main SpriteRenderer
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            Vector3 scale = GetPlatformScale(shapeChoice);
            Sprite spriteToUse = wallSquareSprite;
            Color colorToUse = new Color(0.3f, 0.75f, 0.65f, 1f); // Premium soft teal for blocks

            if (shapeChoice == 0)
            {
                // Square
                if (groundSquareSprite != null)
                {
                    spriteToUse = groundSquareSprite;
                    colorToUse = Color.white;
                }
            }
            else if (shapeChoice == 1)
            {
                // Vertically Long Rectangle
                if (groundVerticalSprite != null)
                {
                    spriteToUse = groundVerticalSprite;
                    colorToUse = Color.white;
                }
            }
            else
            {
                // Horizontally Long Rectangle - use groundSquareSprite (128x128) for 1:1 scale matching with collider
                if (groundSquareSprite != null)
                {
                    spriteToUse = groundSquareSprite;
                    colorToUse = Color.white;
                }
                else if (groundSprite != null)
                {
                    spriteToUse = groundSprite;
                    colorToUse = Color.white; // Render full grass/dirt colors
                }
            }

            platform.transform.localScale = scale;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = spriteToUse;
                spriteRenderer.color = colorToUse;
            }

            // Set BoxCollider2D size to exact sprite boundaries
            if (collider != null)
            {
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    collider.size = spriteRenderer.sprite.bounds.size;
                }
                else
                {
                    collider.size = new Vector2(1.28f, 1.28f);
                }
            }

            // Attach and configure MovingPlatform component
            MovingPlatform movingComp = platform.GetComponent<MovingPlatform>();
            if (movingComp == null)
            {
                movingComp = platform.AddComponent<MovingPlatform>();
            }

            bool isScoreReached = (GameManager.Instance != null && GameManager.Instance.CurrentScore >= movingPlatformScoreThreshold) || platform.transform.position.y >= (movingPlatformScoreThreshold / 10f);
            bool shouldMove = isScoreReached && (shapeChoice == 0 || shapeChoice == 2);
            movingComp.ConfigureMovement(shapeChoice, shouldMove);
        }

        private void CreateEnemyChild(GameObject platform, bool shouldSpawnEnemy, Vector3 enemyLocalPos)
        {
            GameObject enemyGo = new GameObject("Enemy");
            enemyGo.transform.SetParent(platform.transform);

            Vector3 platformScale = platform.transform.localScale;
            float px = platformScale.x;
            float py = platformScale.y;

            enemyGo.transform.localPosition = enemyLocalPos;

            // Compensate for parent platform scale to ensure enemy world scale matches enemyScale
            enemyGo.transform.localScale = new Vector3(enemyScale / px, enemyScale / py, 1f);

            // Add visual component
            SpriteRenderer enemyRenderer = enemyGo.AddComponent<SpriteRenderer>();
            if (enemySprite != null)
            {
                enemyRenderer.sprite = enemySprite;
            }
            enemyRenderer.color = Color.white; // Render actual sprite colors (e.g. mouse texture)
            enemyRenderer.sortingOrder = 5;

            // Add standard trigger collider matching the triangle shape
            PolygonCollider2D polygonCollider = enemyGo.AddComponent<PolygonCollider2D>();
            polygonCollider.isTrigger = true;

            // Assign to 'Enemy' layer for spatial query matching
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex != -1)
            {
                enemyGo.layer = enemyLayerIndex;
            }

            // Attach the Enemy component to handle TakeDamage calls
            enemyGo.AddComponent<Enemy>();

            enemyGo.SetActive(shouldSpawnEnemy);
        }

        private void Update()
        {
            if (cameraTransform == null) return;

            // Calculate the recycle threshold based on camera height
            float recycleThresholdY = cameraTransform.position.y - recycleOffset;

            // Check if any platform has fallen below the camera viewport
            for (int i = 0; i < platformPool.Count; i++)
            {
                GameObject platform = platformPool[i];
                if (platform.transform.position.y < recycleThresholdY)
                {
                    RecyclePlatform(platform);
                }
            }
        }

        private void RecyclePlatform(GameObject platform)
        {
            // Pick a random shape choice
            int shapeChoice = Random.Range(0, 3);
            ConfigurePlatformShape(platform, shapeChoice);

            Vector3 platformScale = platform.transform.localScale;
            float worldWidth = platformScale.x * 1.28f;
            float maxX = Mathf.Max(0f, 2.6125f - (worldWidth / 2f));
            float airMaxX = Mathf.Max(0f, 2.0625f - (worldWidth / 2f));

            bool shouldSpawnEnemy = Random.value <= 0.1f;

            Vector3 chosenPos = Vector3.zero;
            Vector3 chosenEnemyLocalPos = Vector3.zero;
            bool found = false;

            float targetY = highestPlatformY + Random.Range(minVerticalSpacing, maxVerticalSpacing);

            for (int yStep = 0; yStep < 30 && !found; yStep++)
            {
                for (int xStep = 0; xStep < 20; xStep++)
                {
                    float candidateX;
                    float randVal = Random.value;
                    if (randVal < 0.15f)
                    {
                        candidateX = -maxX; // Flush against left wall (15%)
                    }
                    else if (randVal < 0.30f)
                    {
                        candidateX = maxX; // Flush against right wall (15%)
                    }
                    else
                    {
                        candidateX = Random.Range(-airMaxX, airMaxX); // Spawning freely in middle/air (70%)
                    }

                    Vector3 candidatePos = new Vector3(candidateX, targetY, 0f);

                    Vector3 candidateEnemyLocalPos = Vector3.zero;
                    if (shouldSpawnEnemy)
                    {
                        float maxWorldOffsetX = Mathf.Max(0f, (worldWidth / 2f) - (enemyScale / 2f));
                        float maxLocalX = maxWorldOffsetX / platformScale.x;
                        float randomLocalX = Random.Range(-maxLocalX, maxLocalX);
                        float localY = 0.64f + ((enemyScale / 2f) / platformScale.y);
                        candidateEnemyLocalPos = new Vector3(randomLocalX, localY, 0f);
                    }

                    if (IsValidPlacement(platform, candidatePos, platformScale, shouldSpawnEnemy, candidateEnemyLocalPos))
                    {
                        chosenPos = candidatePos;
                        chosenEnemyLocalPos = candidateEnemyLocalPos;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    targetY += 0.2f;
                }
            }

            if (!found)
            {
                float candidateX = Random.Range(-maxX, maxX);
                chosenPos = new Vector3(candidateX, targetY, 0f);
                if (shouldSpawnEnemy)
                {
                    float localY = 0.64f + ((enemyScale / 2f) / platformScale.y);
                    chosenEnemyLocalPos = new Vector3(0f, localY, 0f);
                }
            }

            platform.transform.position = chosenPos;
            highestPlatformY = Mathf.Max(highestPlatformY, chosenPos.y);

            // Re-anchor MovingPlatform initial Y position after recycling
            MovingPlatform movingComp = platform.GetComponent<MovingPlatform>();
            if (movingComp != null)
            {
                bool isScoreReached = (GameManager.Instance != null && GameManager.Instance.CurrentScore >= movingPlatformScoreThreshold) || chosenPos.y >= (movingPlatformScoreThreshold / 10f);
                bool shouldMove = isScoreReached && (movingComp.ShapeChoice == 0 || movingComp.ShapeChoice == 2);
                movingComp.ConfigureMovement(movingComp.ShapeChoice, shouldMove);
            }

            // Handle enemy recycling (10% chance to spawn on top)
            Transform enemyTransform = platform.transform.Find("Enemy");
            if (enemyTransform != null)
            {
                enemyTransform.gameObject.SetActive(shouldSpawnEnemy);
                if (shouldSpawnEnemy)
                {
                    float px = platformScale.x;
                    float py = platformScale.y;

                    // Re-calculate scale compensation
                    enemyTransform.localScale = new Vector3(enemyScale / px, enemyScale / py, 1f);

                    // Set position flush on top surface
                    enemyTransform.localPosition = chosenEnemyLocalPos;
                }
            }
        }

        private GameObject CreateDefaultPlatformTemplate()
        {
            GameObject template = new GameObject("DefaultPlatformTemplate");
            template.SetActive(false);

            // Scale matches a standard 2D platform
            template.transform.localScale = new Vector3(1.2f, 0.2f, 1f);

            // Add SpriteRenderer and configure sorting order
            SpriteRenderer spriteRenderer = template.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 2;

            // Add standard solid BoxCollider2D (cat cannot pass through from any direction)
            BoxCollider2D collider = template.AddComponent<BoxCollider2D>();
            collider.usedByEffector = false;

            return template;
        }

        public bool HasMiddlePlatformNearY(float minY, float maxY)
        {
            for (int i = 0; i < platformPool.Count; i++)
            {
                GameObject p = platformPool[i];
                if (p == null || !p.activeInHierarchy) continue;

                float py = p.transform.position.y;
                if (py >= minY && py <= maxY)
                {
                    float px = p.transform.position.x;
                    // Check if platform is in the middle (not flush against either wall)
                    if (Mathf.Abs(px) < 1.8f)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

