using UnityEngine;

namespace MyProject
{
    /// <summary>
    /// Cross-platform mobile tactile haptic feedback engine.
    /// Provides low-latency micro-vibrations for tactile juice on mobile devices.
    /// </summary>
    public static class Haptics
    {
        public const string HAPTICS_PREF_KEY = "CatClimb_Haptics";

        private static bool isInitialized = false;
        private static bool isEnabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibratorInstance;
        private static AndroidJavaClass vibrationEffectClass;
        private static int apiLevel = -1;
#endif

        public static bool IsEnabled
        {
            get
            {
                EnsureInitialized();
                return isEnabled;
            }
            set
            {
                isEnabled = value;
                PlayerPrefs.SetInt(HAPTICS_PREF_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private static void EnsureInitialized()
        {
            if (isInitialized) return;
            isInitialized = true;
            isEnabled = PlayerPrefs.GetInt(HAPTICS_PREF_KEY, 1) == 1;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    apiLevel = versionClass.GetStatic<int>("SDK_INT");
                }

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibratorInstance = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                if (apiLevel >= 26)
                {
                    vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Haptics] Failed to initialize Android vibrator: {ex.Message}");
                vibratorInstance = null;
            }
#endif
        }

        public static void Light()
        {
            VibrateMillis(18, 64);
        }

        public static void Medium()
        {
            VibrateMillis(35, 128);
        }

        public static void Heavy()
        {
            VibrateMillis(65, 255);
        }

        private static readonly long[] DoublePulsePattern = { 0, 30, 60, 45 };

        public static void DoublePulse()
        {
            if (!IsEnabled) return;
            VibratePattern(DoublePulsePattern);
        }

        private static void VibrateMillis(long milliseconds, int amplitude)
        {
            if (!IsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (vibratorInstance != null)
                {
                    if (apiLevel >= 26 && vibrationEffectClass != null)
                    {
                        using (var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                        {
                            vibratorInstance.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        vibratorInstance.Call("vibrate", milliseconds);
                    }
                    return;
                }
            }
            catch { }
#endif

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private static void VibratePattern(long[] timings)
        {
            if (!IsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (vibratorInstance != null)
                {
                    if (apiLevel >= 26 && vibrationEffectClass != null)
                    {
                        using (var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, -1))
                        {
                            vibratorInstance.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        vibratorInstance.Call("vibrate", timings, -1);
                    }
                    return;
                }
            }
            catch { }
#endif

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
