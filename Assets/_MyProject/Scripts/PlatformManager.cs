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
        [Tooltip("The enemy triangle sprite. Assigned programmatically by the scene builder.")]
        public Sprite enemySprite;

        [Header("Spawn Settings")]
        [SerializeField] private float minVerticalSpacing = 1.2f;
        [SerializeField] private float maxVerticalSpacing = 1.9f;
        [SerializeField] private float initialSpawnY = -3f;

        [Header("Recycle Settings")]
        [Tooltip("Distance below the camera Y position at which a platform is recycled.")]
        [SerializeField] private float recycleOffset = 6f;

        private List<GameObject> platformPool = new List<GameObject>();
        private float highestPlatformY;
        private Transform cameraTransform;

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

            InitializePool();
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
                
                // Pick a random shape and scale
                Vector3 platformScale = GetRandomPlatformScale();
                platformInstance.transform.localScale = platformScale;

                // Pick a random X position within the wall boundaries, accounting for the dynamic width
                float maxX = 2.6125f - (platformScale.x / 2f);
                float randomX = Random.Range(-maxX, maxX);
                platformInstance.transform.position = new Vector3(randomX, currentSpawnY, 0f);
                platformInstance.SetActive(true);

                // Create and attach the pooled Enemy child GameObject
                CreateEnemyChild(platformInstance);

                platformPool.Add(platformInstance);

                highestPlatformY = currentSpawnY;
                // Increment Y position for the next platform
                currentSpawnY += Random.Range(minVerticalSpacing, maxVerticalSpacing);
            }

            // Cleanup the temporary template if we created one
            if (createdTemporaryTemplate)
            {
                Destroy(template);
            }

            Debug.Log($"Initialized platform pool with {poolSize} randomized platforms and pooled enemies.");
        }

        private void CreateEnemyChild(GameObject platform)
        {
            GameObject enemyGo = new GameObject("Enemy");
            enemyGo.transform.SetParent(platform.transform);

            // Fetch platform scale to calculate dynamic offsets
            Vector3 platformScale = platform.transform.localScale;
            float px = platformScale.x;
            float py = platformScale.y;

            // Calculate Y to sit flush on top surface
            float localY = 0.5f + (0.25f / py);

            // Calculate X to stay fully within platform edges
            float maxWorldX = Mathf.Max(0f, (px / 2f) - 0.25f);
            float maxLocalX = maxWorldX / px;
            float randomLocalX = Random.Range(-maxLocalX, maxLocalX);

            enemyGo.transform.localPosition = new Vector3(randomLocalX, localY, 0f);

            // Compensate for parent platform scale to ensure enemy world scale is exactly (0.5f, 0.5f, 1f)
            enemyGo.transform.localScale = new Vector3(0.5f / px, 0.5f / py, 1f);

            // Add visual component
            SpriteRenderer enemyRenderer = enemyGo.AddComponent<SpriteRenderer>();
            if (enemySprite != null)
            {
                enemyRenderer.sprite = enemySprite;
            }
            enemyRenderer.color = new Color(1f, 0.32f, 0.32f, 1f); // Vibrant warning red-coral

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

            // Roll 1/10 probability to set active
            bool shouldSpawnEnemy = Random.value <= 0.1f;
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
            // Pick a random shape/scale on recycle
            Vector3 platformScale = GetRandomPlatformScale();
            platform.transform.localScale = platformScale;

            // Position the recycled platform above the highest current platform
            float newY = highestPlatformY + Random.Range(minVerticalSpacing, maxVerticalSpacing);
            
            // Calculate dynamic X boundaries to prevent wall clipping
            float maxX = 2.6125f - (platformScale.x / 2f);
            float newX = Random.Range(-maxX, maxX);

            platform.transform.position = new Vector3(newX, newY, 0f);
            
            // Update the highest platform Y tracker
            highestPlatformY = newY;

            // Handle enemy recycling (10% chance to spawn on top)
            Transform enemyTransform = platform.transform.Find("Enemy");
            if (enemyTransform != null)
            {
                bool shouldSpawnEnemy = Random.value <= 0.1f;
                enemyTransform.gameObject.SetActive(shouldSpawnEnemy);
                if (shouldSpawnEnemy)
                {
                    float px = platformScale.x;
                    float py = platformScale.y;

                    // Re-calculate scale compensation
                    enemyTransform.localScale = new Vector3(0.5f / px, 0.5f / py, 1f);

                    // Re-calculate Y coordinate to sit flush on top surface
                    float localY = 0.5f + (0.25f / py);

                    // Re-calculate X coordinate to stay fully within platform edges
                    float maxWorldX = Mathf.Max(0f, (px / 2f) - 0.25f);
                    float maxLocalX = maxWorldX / px;
                    float randomLocalX = Random.Range(-maxLocalX, maxLocalX);

                    enemyTransform.localPosition = new Vector3(randomLocalX, localY, 0f);
                }
            }
        }

        private Vector3 GetRandomPlatformScale()
        {
            int shapeChoice = Random.Range(0, 3); // 0 = Square, 1 = Vertical, 2 = Horizontal
            if (shapeChoice == 0)
            {
                // Square
                return new Vector3(0.8f, 0.8f, 1f);
            }
            else if (shapeChoice == 1)
            {
                // Vertically Long Rectangle
                return new Vector3(0.5f, 1.5f, 1f);
            }
            else
            {
                // Horizontally Long Rectangle
                return new Vector3(1.5f, 0.3f, 1f);
            }
        }

        private GameObject CreateDefaultPlatformTemplate()
        {
            GameObject template = new GameObject("DefaultPlatformTemplate");
            template.SetActive(false);

            // Scale matches a standard 2D platform
            template.transform.localScale = new Vector3(1.2f, 0.2f, 1f);

            // Add SpriteRenderer and load the generated square sprite if available
            SpriteRenderer spriteRenderer = template.AddComponent<SpriteRenderer>();
            Sprite wallSquareSprite = Resources.Load<Sprite>("WallSquare") ?? 
                                      UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_MyProject/Sprites/WallSquare.png");
            
            if (wallSquareSprite != null)
            {
                spriteRenderer.sprite = wallSquareSprite;
            }
            spriteRenderer.color = new Color(0.3f, 0.75f, 0.65f, 1f); // Premium soft teal

            // Add a standard solid BoxCollider2D (cat cannot pass through)
            template.AddComponent<BoxCollider2D>();

            return template;
        }
    }
}
