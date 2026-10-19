using UnityEngine;

namespace MyProject
{
    /// <summary>
    /// Executes before any scene loads to configure optimal mobile performance,
    /// screen orientation locking, and power management.
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeMobileSettings()
        {
            // 1. Target 60 FPS for responsive platformer gameplay on mobile
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // 2. Lock screen orientation to Portrait only (prevent accidental landscape or upside-down flip)
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;

            // 3. Keep display awake during active play session
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log("[AppBootstrap] Mobile settings applied: 60 FPS, Portrait orientation locked, Sleep disabled.");
        }
    }
}
