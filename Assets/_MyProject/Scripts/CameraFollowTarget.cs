using UnityEngine;

namespace MyProject
{
    public class CameraFollowTarget : MonoBehaviour
    {
        [Header("Tracking Settings")]
        [Tooltip("The Cat transform to follow.")]
        [SerializeField] private Transform catTransform;

        // Force the camera target to start at Y = 0 so the floor (Y = -5) remains at the bottom edge
        private float highestY = 0f;

        private void Start()
        {
            transform.position = new Vector3(0f, highestY, 0f);
        }

        private void LateUpdate()
        {
            if (catTransform != null)
            {
                // Only move camera target up if the cat climbs above the screen center (Y = 0)
                if (catTransform.position.y > highestY)
                {
                    highestY = catTransform.position.y;
                }

                // Keep X and Z locked to 0, only follow Y upward
                transform.position = new Vector3(0f, highestY, 0f);
            }
        }

        public void SetTarget(Transform target)
        {
            catTransform = target;
            highestY = 0f; // Lock camera center to Y = 0 initially
            transform.position = new Vector3(0f, highestY, 0f);
        }
    }
}
