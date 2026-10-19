using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyProject.UI
{
    /// <summary>
    /// Universal tactile press-down scale feedback and light haptic pulse for UI buttons.
    /// Operates in unscaled time so it works seamlessly during Pause, Revive, and Modal screens.
    /// </summary>
    [DisallowMultipleComponent]
    public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float transitionSpeed = 22f;

        private Button cachedButton;
        private Vector3 baseScale = Vector3.one;
        private float currentMultiplier = 1f;
        private bool isPressed = false;
        private bool isAnimating = false;

        private void Awake()
        {
            cachedButton = GetComponent<Button>();
            baseScale = transform.localScale;
            if (baseScale.sqrMagnitude < 0.001f)
            {
                baseScale = Vector3.one;
            }
        }

        private void OnDisable()
        {
            if (isPressed || isAnimating)
            {
                isPressed = false;
                isAnimating = false;
                currentMultiplier = 1f;
                transform.localScale = baseScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (cachedButton != null && !cachedButton.interactable) return;

            if (!isPressed && !isAnimating)
            {
                baseScale = transform.localScale;
            }

            isPressed = true;
            isAnimating = true;
            Haptics.Light();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPressed = false;
        }

        private void Update()
        {
            if (!isAnimating) return;

            float target = isPressed ? pressedScale : 1f;
            currentMultiplier = Mathf.MoveTowards(currentMultiplier, target, Time.unscaledDeltaTime * transitionSpeed);
            transform.localScale = baseScale * currentMultiplier;

            if (!isPressed && Mathf.Approximately(currentMultiplier, 1f))
            {
                currentMultiplier = 1f;
                transform.localScale = baseScale;
                isAnimating = false;
            }
        }
    }
}
