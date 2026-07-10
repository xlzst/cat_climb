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

        private List<Transform> leftWallSegments = new List<Transform>();
        private List<Transform> rightWallSegments = new List<Transform>();

        // Associated spikes for each segment index (segment index -> list of spikes)
        private List<List<GameObject>> leftSpikesList = new List<List<GameObject>>();
        private List<List<GameObject>> rightSpikesList = new List<List<GameObject>>();

        private Transform cameraTransform;

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                Debug.LogError("Main Camera not found! WallManager requires a Main Camera.");
                return;
            }

            SetupWallPools();
        }

        private void SetupWallPools()
        {
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
            float y1 = segmentY + Random.Range(-4.0f, -1.0f);
            spikes[0].transform.position = new Vector3(targetX, y1, 0f);
            spikes[0].SetActive(Random.value <= 0.5f); // 50% activation rate for random interval patterns

            // Spike 2 (Upper half of the segment)
            float y2 = segmentY + Random.Range(1.0f, 4.0f);
            spikes[1].transform.position = new Vector3(targetX, y2, 0f);
            spikes[1].SetActive(Random.value <= 0.5f);
        }
    }
}
