using System.Collections.Generic;
using UnityEngine;

namespace MyProject
{
    public class WallManager : MonoBehaviour
    {
        [Header("Recycle Settings")]
        [Tooltip("The vertical height of each wall segment. Default is 10 units to match URP Camera orthographic height.")]
        [SerializeField] private float segmentHeight = 10f;
        
        [Tooltip("Distance below the camera Y position at which a wall segment is recycled.")]
        [SerializeField] private float recycleOffset = 15f;

        [Header("Spike Settings")]
        [Tooltip("The triangle sprite to represent spikes. Assigned programmatically by the scene builder.")]
        public Sprite spikeSprite;

        [Tooltip("Vertical distance threshold within which opposite wall spikes are considered at the same Y position.")]
        [SerializeField] private float sameYSpikeThreshold = 2.2f;

        private List<Transform> leftWallSegments = new List<Transform>();
        private List<Transform> rightWallSegments = new List<Transform>();

        // Associated spikes for each segment index (segment index -> list of spikes)
        private List<List<GameObject>> leftSpikesList = new List<List<GameObject>>();
        private List<List<GameObject>> rightSpikesList = new List<List<GameObject>>();

        private Transform cameraTransform;

        public static WallManager Instance { get; private set; }

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

        public void EnsureInitialized()
        {
            if (leftWallSegments.Count == 0)
            {
                if (cameraTransform == null && Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }
                SetupWallPools();
            }
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void SetupWallPools()
        {
            if (leftWallSegments.Count > 0) return;

            GameObject initialLeft = GameObject.Find("LeftWall");
            GameObject initialRight = GameObject.Find("RightWall");

            if (initialLeft == null || initialRight == null)
            {
                Debug.LogError("Initial LeftWall or RightWall GameObjects not found in the scene. WallManager cannot initialize pools.");
                return;
            }

            // Track initial segments
            leftWallSegments.Add(initialLeft.transform);
            rightWallSegments.Add(initialRight.transform);

            // Pre-spawn and track initial spikes for Segment 0
            leftSpikesList.Add(CreateSpikesForSegment(true));
            rightSpikesList.Add(CreateSpikesForSegment(false));
            PositionSpikesForSegment(true, 0, initialLeft.transform.position.y);
            PositionSpikesForSegment(false, 0, initialRight.transform.position.y);

            // Clone 2 more segments for Left and Right walls
            for (int i = 1; i <= 2; i++)
            {
                // Create left segment
                GameObject leftClone = Instantiate(initialLeft, transform);
                leftClone.name = $"LeftWall_Segment_{i}";
                float leftY = initialLeft.transform.position.y + (segmentHeight * i);
                leftClone.transform.position = new Vector3(initialLeft.transform.position.x, leftY, 0f);
                leftWallSegments.Add(leftClone.transform);

                // Setup spikes for left segment i
                leftSpikesList.Add(CreateSpikesForSegment(true));
                PositionSpikesForSegment(true, i, leftY);

                // Create right segment
                GameObject rightClone = Instantiate(initialRight, transform);
                rightClone.name = $"RightWall_Segment_{i}";
                float rightY = initialRight.transform.position.y + (segmentHeight * i);
                rightClone.transform.position = new Vector3(initialRight.transform.position.x, rightY, 0f);
                rightWallSegments.Add(rightClone.transform);

                // Setup spikes for right segment i
                rightSpikesList.Add(CreateSpikesForSegment(false));
                PositionSpikesForSegment(false, i, rightY);
            }

            // Run initial validation to ensure no dual-spike blockages exist
            ValidateSpikePositions();

            Debug.Log("Successfully initialized infinite wall pools and hazards (3 segments, 6 spikes per side).");
        }

        private void Update()
        {
            if (cameraTransform == null) return;

            float recycleThresholdY = cameraTransform.position.y - recycleOffset;

            // Recycle left wall segments & hazards
            RecycleWallListIfBelowThreshold(leftWallSegments, recycleThresholdY, true);

            // Recycle right wall segments & hazards
            RecycleWallListIfBelowThreshold(rightWallSegments, recycleThresholdY, false);

            // Enforce that active spikes on opposing walls at the same Y position must have a middle platform
            ValidateSpikePositions();
        }

        private void RecycleWallListIfBelowThreshold(List<Transform> segments, float thresholdY, bool isLeft)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                Transform segment = segments[i];
                if (segment.position.y < thresholdY)
                {
                    // Move the segment directly above the highest segment in the list
                    float highestY = GetHighestWallY(segments);
                    float newY = highestY + segmentHeight;
                    segment.position = new Vector3(segment.position.x, newY, segment.position.z);

                    // Re-randomize and position spikes on the recycled segment
                    PositionSpikesForSegment(isLeft, i, newY);
                }
            }
        }

        private float GetHighestWallY(List<Transform> segments)
        {
            float highestY = float.MinValue;
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].position.y > highestY)
                {
                    highestY = segments[i].position.y;
                }
            }
            return highestY;
        }

        private List<GameObject> CreateSpikesForSegment(bool isLeft)
        {
            List<GameObject> segmentSpikes = new List<GameObject>();
            
            // Create 2 spike GameObjects per wall segment
            for (int i = 0; i < 2; i++)
            {
                GameObject spikeGo = new GameObject(isLeft ? "LeftSpike" : "RightSpike");
                spikeGo.transform.SetParent(transform); // Parent is WallManager (scale 1, 1, 1) to avoid non-uniform scale shear
                spikeGo.transform.localScale = new Vector3(0.5f, 0.5f, 1f); // Match Cat size

                // Rotate triangle to point outward from the wall
                // Left points right (-90 deg), Right points left (90 deg)
                spikeGo.transform.localRotation = Quaternion.Euler(0f, 0f, isLeft ? -90f : 90f);

                // Add visual
                SpriteRenderer renderer = spikeGo.AddComponent<SpriteRenderer>();
                if (spikeSprite != null)
                {
                    renderer.sprite = spikeSprite;
                }
                renderer.color = new Color(0.75f, 0.25f, 0.25f, 1f); // Warning dark red/metallic hazard tint
                renderer.sortingOrder = 5;

                // Add standard solid collider matching the triangle shape (cat will collide physically)
                PolygonCollider2D polygonCollider = spikeGo.AddComponent<PolygonCollider2D>();
                polygonCollider.isTrigger = false;

                // Add to 'Enemy' layer so it registers in general physics filters if needed
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer != -1)
                {
                    spikeGo.layer = enemyLayer;
                }

                spikeGo.SetActive(false); // Managed dynamically
                segmentSpikes.Add(spikeGo);
            }

            return segmentSpikes;
        }

        private void PositionSpikesForSegment(bool isLeft, int segmentIndex, float segmentY)
        {
            List<GameObject> spikes = isLeft ? leftSpikesList[segmentIndex] : rightSpikesList[segmentIndex];
            
            // Inner boundaries X coordinates:
            // Left inner boundary = -2.6125. Adding half-width (0.25) -> -2.3625.
            // Right inner boundary = 2.6125. Subtracting half-width (0.25) -> 2.3625.
            float targetX = isLeft ? -2.3625f : 2.3625f;

            // Platform height is 10. Segment goes from segmentY - 5.0 to segmentY + 5.0.
            // Randomize Y in two separate halves of the segment to keep them nicely spaced.

            // Spike 1 (Lower half of the segment)
            PositionSingleSpike(spikes[0], targetX, segmentY - 4.0f, segmentY - 1.0f, isLeft);

            // Spike 2 (Upper half of the segment)
            PositionSingleSpike(spikes[1], targetX, segmentY + 1.0f, segmentY + 4.0f, isLeft);
        }

        /// <summary>
        /// Checks if any active spike is within clearance distance of the specified world coordinates.
        /// Accounts for wall side (left wall spikes if worldX <= 0.5f, right wall spikes if worldX >= -0.5f).
        /// </summary>
        public bool IsSpikeNear(float worldX, float worldY, float verticalClearance = 1.35f)
        {
            if (worldX <= 0.5f)
            {
                if (CheckSpikesInList(leftSpikesList, worldX, worldY, verticalClearance)) return true;
            }
            if (worldX >= -0.5f)
            {
                if (CheckSpikesInList(rightSpikesList, worldX, worldY, verticalClearance)) return true;
            }
            return false;
        }

        private bool CheckSpikesInList(List<List<GameObject>> spikesList, float worldX, float worldY, float clearance)
        {
            for (int seg = 0; seg < spikesList.Count; seg++)
            {
                List<GameObject> segmentSpikes = spikesList[seg];
                for (int i = 0; i < segmentSpikes.Count; i++)
                {
                    GameObject spike = segmentSpikes[i];
                    if (spike != null && spike.activeInHierarchy)
                    {
                        Vector3 spikePos = spike.transform.position;
                        if (Mathf.Abs(worldX - spikePos.x) < 1.4f && Mathf.Abs(worldY - spikePos.y) < clearance)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsConflictWithItemOrPlatform(float candidateY, bool isLeft)
        {
            // 1. Check against active FishTreats
            for (int i = 0; i < FishTreat.ActiveFishTreats.Count; i++)
            {
                FishTreat treat = FishTreat.ActiveFishTreats[i];
                if (treat != null && treat.gameObject.activeInHierarchy)
                {
                    Vector3 treatPos = treat.transform.position;
                    bool onSameSide = isLeft ? (treatPos.x < -1.0f) : (treatPos.x > 1.0f);
                    if (onSameSide && Mathf.Abs(candidateY - treatPos.y) < 1.35f)
                    {
                        return true;
                    }
                }
            }

            // 2. Check against active MagnetItems
            for (int i = 0; i < MagnetItem.ActiveMagnetItems.Count; i++)
            {
                MagnetItem magnet = MagnetItem.ActiveMagnetItems[i];
                if (magnet != null && magnet.gameObject.activeInHierarchy)
                {
                    Vector3 magnetPos = magnet.transform.position;
                    bool onSameSide = isLeft ? (magnetPos.x < -1.0f) : (magnetPos.x > 1.0f);
                    if (onSameSide && Mathf.Abs(candidateY - magnetPos.y) < 1.35f)
                    {
                        return true;
                    }
                }
            }

            // 3. Check against active platforms touching or close to this wall
            PlatformManager pm = PlatformManager.Instance;
            if (pm != null && pm.PlatformPool != null)
            {
                for (int i = 0; i < pm.PlatformPool.Count; i++)
                {
                    GameObject plat = pm.PlatformPool[i];
                    if (plat != null && plat.activeInHierarchy)
                    {
                        Vector3 platPos = plat.transform.position;
                        Vector3 platScale = plat.transform.localScale;
                        float platHalfWidth = (platScale.x * 1.28f) * 0.5f;
                        float platHalfHeight = (platScale.y * 1.28f) * 0.5f;

                        bool isNearThisWall = isLeft 
                            ? ((platPos.x - platHalfWidth) < -2.0f) 
                            : ((platPos.x + platHalfWidth) > 2.0f);

                        if (isNearThisWall)
                        {
                            if (Mathf.Abs(candidateY - platPos.y) < (platHalfHeight + 0.6f))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void PositionSingleSpike(GameObject spike, float targetX, float minY, float maxY, bool isLeft)
        {
            bool wantsActive = Random.value <= 0.5f;
            if (!wantsActive)
            {
                float defaultY = (minY + maxY) * 0.5f;
                spike.transform.position = new Vector3(targetX, defaultY, 0f);
                spike.SetActive(false);
                return;
            }

            // Try up to 12 attempts to find a candidate position that doesn't create an impassable dual-spike barrier or overlap items/platforms
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float candidateY = Random.Range(minY, maxY);

                // Spatial exclusion: Never place a spike where a collectible or platform already sits
                if (IsConflictWithItemOrPlatform(candidateY, isLeft))
                {
                    continue;
                }

                if (IsConflictWithOppositeSpike(candidateY, isLeft, out float oppY))
                {
                    float checkMinY = Mathf.Min(candidateY, oppY) - 1.5f;
                    float checkMaxY = Mathf.Max(candidateY, oppY) + 1.5f;

                    if (HasMiddlePlatformNear(checkMinY, checkMaxY))
                    {
                        // Middle platform exists, candidate Y is valid!
                        spike.transform.position = new Vector3(targetX, candidateY, 0f);
                        spike.SetActive(true);
                        return;
                    }
                    // Conflict exists and no middle platform, try another candidate Y
                }
                else
                {
                    // No conflict with opposite spike!
                    spike.transform.position = new Vector3(targetX, candidateY, 0f);
                    spike.SetActive(true);
                    return;
                }
            }

            // Fallback: If no candidate Y in range resolves the conflict without a middle platform, deactivate spike
            float fallbackY = Random.Range(minY, maxY);
            spike.transform.position = new Vector3(targetX, fallbackY, 0f);
            spike.SetActive(false);
        }

        private bool IsConflictWithOppositeSpike(float candidateY, bool isCandidateLeft, out float conflictingOppositeY)
        {
            conflictingOppositeY = 0f;
            List<List<GameObject>> oppositeSpikesList = isCandidateLeft ? rightSpikesList : leftSpikesList;

            for (int seg = 0; seg < oppositeSpikesList.Count; seg++)
            {
                List<GameObject> segmentSpikes = oppositeSpikesList[seg];
                for (int i = 0; i < segmentSpikes.Count; i++)
                {
                    GameObject oppSpike = segmentSpikes[i];
                    if (oppSpike != null && oppSpike.activeInHierarchy)
                    {
                        float oppY = oppSpike.transform.position.y;
                        if (Mathf.Abs(candidateY - oppY) < sameYSpikeThreshold)
                        {
                            conflictingOppositeY = oppY;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool HasMiddlePlatformNear(float minY, float maxY)
        {
            PlatformManager pm = PlatformManager.Instance;
            if (pm == null)
            {
                pm = FindFirstObjectByType<PlatformManager>();
            }
            if (pm != null)
            {
                return pm.HasMiddlePlatformNearY(minY, maxY);
            }
            return false;
        }

        private void ValidateSpikePositions()
        {
            for (int lSeg = 0; lSeg < leftSpikesList.Count; lSeg++)
            {
                List<GameObject> leftSegmentSpikes = leftSpikesList[lSeg];
                for (int l = 0; l < leftSegmentSpikes.Count; l++)
                {
                    GameObject leftSpike = leftSegmentSpikes[l];
                    if (leftSpike == null || !leftSpike.activeInHierarchy) continue;

                    float ly = leftSpike.transform.position.y;

                    for (int rSeg = 0; rSeg < rightSpikesList.Count; rSeg++)
                    {
                        List<GameObject> rightSegmentSpikes = rightSpikesList[rSeg];
                        for (int r = 0; r < rightSegmentSpikes.Count; r++)
                        {
                            GameObject rightSpike = rightSegmentSpikes[r];
                            if (rightSpike == null || !rightSpike.activeInHierarchy) continue;

                            float ry = rightSpike.transform.position.y;

                            if (Mathf.Abs(ly - ry) < sameYSpikeThreshold)
                            {
                                float minY = Mathf.Min(ly, ry) - 1.5f;
                                float maxY = Mathf.Max(ly, ry) + 1.5f;

                                if (!HasMiddlePlatformNear(minY, maxY))
                                {
                                    // Deactivate right spike to break impossible dual-spike barrier without middle platform
                                    rightSpike.SetActive(false);
                                }
                            }
                        }
                    }
                }
            }

            // Prevent active spikes from overlapping any active FishTreat or MagnetItem
            ValidateSpikesAgainstItems(leftSpikesList, true);
            ValidateSpikesAgainstItems(rightSpikesList, false);
        }

        private void ValidateSpikesAgainstItems(List<List<GameObject>> spikesList, bool isLeft)
        {
            for (int seg = 0; seg < spikesList.Count; seg++)
            {
                List<GameObject> segmentSpikes = spikesList[seg];
                for (int i = 0; i < segmentSpikes.Count; i++)
                {
                    GameObject spike = segmentSpikes[i];
                    if (spike == null || !spike.activeInHierarchy) continue;

                    float sy = spike.transform.position.y;

                    // 1. Check against active FishTreats
                    for (int f = 0; f < FishTreat.ActiveFishTreats.Count; f++)
                    {
                        FishTreat treat = FishTreat.ActiveFishTreats[f];
                        if (treat != null && treat.gameObject.activeInHierarchy)
                        {
                            Vector3 ty = treat.transform.position;
                            bool onSameSide = isLeft ? (ty.x < -1.0f) : (ty.x > 1.0f);
                            if (onSameSide && Mathf.Abs(sy - ty.y) < 1.35f)
                            {
                                spike.SetActive(false);
                                break;
                            }
                        }
                    }

                    if (!spike.activeInHierarchy) continue;

                    // 2. Check against active MagnetItems
                    for (int m = 0; m < MagnetItem.ActiveMagnetItems.Count; m++)
                    {
                        MagnetItem magnet = MagnetItem.ActiveMagnetItems[m];
                        if (magnet != null && magnet.gameObject.activeInHierarchy)
                        {
                            Vector3 my = magnet.transform.position;
                            bool onSameSide = isLeft ? (my.x < -1.0f) : (my.x > 1.0f);
                            if (onSameSide && Mathf.Abs(sy - my.y) < 1.35f)
                            {
                                spike.SetActive(false);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
