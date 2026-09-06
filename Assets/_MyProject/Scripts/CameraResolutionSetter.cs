using UnityEngine;
using System.Reflection;

namespace MyProject
{
    public class CameraResolutionSetter : MonoBehaviour
    {
        [Header("Target Resolution Settings")]
        [Tooltip("Target design width (e.g. 1080 for 9:16 portrait).")]
        [SerializeField] private float targetWidth = 1080f;

        [Tooltip("Target design height (e.g. 1920 for 9:16 portrait).")]
        [SerializeField] private float targetHeight = 1920f;

        [Tooltip("Base orthographic size matching target aspect ratio (default is 5.0).")]
        [SerializeField] private float baseOrthographicSize = 5f;

        private float lastScreenWidth = 0f;
        private float lastScreenHeight = 0f;

        private void Start()
        {
            SetCameraSize();
        }

        private void Update()
        {
            // Re-apply if screen resolution changes dynamically (e.g. window resize / orientation change)
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                SetCameraSize();
            }
        }

        public void SetCameraSize()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            // Target aspect ratio (1080 / 1920 = 9:16 = 0.5625)
            float targetRatio = targetWidth / targetHeight;
            // Current aspect ratio of active screen/device
            float currentRatio = (float)Screen.width / (float)Screen.height;

            float calculatedOrthoSize = baseOrthographicSize;

            // If current device ratio is narrower/taller than target ratio (e.g. 9:19.5, 9:20 smartphones)
            if (currentRatio < targetRatio)
            {
                // Recalculate camera orthographic size based on target width to prevent side walls from being cropped
                calculatedOrthoSize = baseOrthographicSize * (targetRatio / currentRatio);
            }

            // Apply calculated size to Main Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographicSize = calculatedOrthoSize;
            }

            // Apply calculated size to Cinemachine Virtual Camera if active
            UpdateCinemachineCameraSize(calculatedOrthoSize);
        }

        private void UpdateCinemachineCameraSize(float newOrthoSize)
        {
            // Look for Cinemachine v3 (CinemachineCamera) or v2 (CinemachineVirtualCamera) dynamically via reflection
            Component vcamComp = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>() as Component;
            if (vcamComp == null)
            {
                System.Type vcamType = System.Type.GetType("Cinemachine.CinemachineVirtualCamera,Cinemachine");
                if (vcamType != null)
                {
                    vcamComp = FindFirstObjectByType(vcamType) as Component;
                }
            }

            if (vcamComp == null) return;

            System.Type type = vcamComp.GetType();
            MemberInfo lensProp = (MemberInfo)type.GetField("Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                 (MemberInfo)type.GetProperty("Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                 (MemberInfo)type.GetField("m_Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                 (MemberInfo)type.GetProperty("m_Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (lensProp != null)
            {
                object lensVal = (lensProp is PropertyInfo pi) ? pi.GetValue(vcamComp) : ((FieldInfo)lensProp).GetValue(vcamComp);
                if (lensVal != null)
                {
                    System.Type lensType = lensVal.GetType();
                    FieldInfo sizeField = lensType.GetField("OrthographicSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                          lensType.GetField("m_OrthographicSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (sizeField != null)
                    {
                        sizeField.SetValue(lensVal, newOrthoSize);

                        if (lensProp is PropertyInfo pi2)
                        {
                            pi2.SetValue(vcamComp, lensVal);
                        }
                        else if (lensProp is FieldInfo fi2)
                        {
                            fi2.SetValue(vcamComp, lensVal);
                        }
                    }
                }
            }
        }
    }
}
