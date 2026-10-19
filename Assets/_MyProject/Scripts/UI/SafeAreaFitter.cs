using UnityEngine;

namespace MyProject.UI
{
    /// <summary>
    /// Adjusts RectTransform anchors to respect Screen.safeArea,
    /// ensuring HUD elements, buttons, and badges are never obscured by camera notches,
    /// rounded corners, or home indicator bars on modern mobile devices.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform panelRectTransform;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = (ScreenOrientation)(-1);

        private void Awake()
        {
            panelRectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea ||
                lastScreenSize.x != Screen.width ||
                lastScreenSize.y != Screen.height ||
                lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (panelRectTransform == null)
            {
                panelRectTransform = GetComponent<RectTransform>();
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            lastOrientation = Screen.orientation;

            // Convert safe area rectangle from absolute screen pixels to normalized [0, 1] anchor coordinates
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            panelRectTransform.anchorMin = anchorMin;
            panelRectTransform.anchorMax = anchorMax;
            panelRectTransform.offsetMin = Vector2.zero;
            panelRectTransform.offsetMax = Vector2.zero;
        }
    }
}
