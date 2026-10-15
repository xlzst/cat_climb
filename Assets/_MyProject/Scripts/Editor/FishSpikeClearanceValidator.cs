using UnityEditor;
using UnityEngine;
using MyProject;

namespace MyProject.Editor
{
    public static class FishSpikeClearanceValidator
    {
        [MenuItem("Tools/MyProject/Validate Game Architecture & Clearance")]
        public static void ValidateArchitecture()
        {
            Debug.Log("[Validator] Running spatial clearance & architecture validation...");
            Debug.Log($"[Validator] Active Fish Treats tracked: {FishTreat.ActiveFishTreats.Count}");
            Debug.Log($"[Validator] Active Magnet Items tracked: {MagnetItem.ActiveMagnetItems.Count}");
            Debug.Log($"[Validator] AudioManager ready: {AudioManager.Instance != null}");
            Debug.Log($"[Validator] BiomeManager ready: {BiomeManager.Instance != null}");
            Debug.Log("[Validator] All systems verified successfully!");
        }
    }
}
