using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyProject
{
    /// <summary>
    /// Consolidated JSON save manager (CatClimb_Save_v1) with automatic migration from legacy PlayerPrefs keys
    /// and zero-GC dirty-flag batching so disk writes only occur at safe boundaries (pause, game over, app background).
    /// </summary>
    public static class SaveData
    {
        public const string SAVE_PREF_KEY = "CatClimb_Save_v1";

        [Serializable]
        public class SaveModel
        {
            public int version = 1;
            public int bestMeters = 0;
            public int totalCoins = 0;
            public int totalRuns = 0;
            public int lifetimeDefeatedEnemies = 0;
            public int lifetimeWallKicks = 0;
            public string selectedSkin = "orange";
            public List<string> unlockedSkins = new List<string> { "orange" };

            // Daily Missions tracking (resets at local midnight yyyyMMdd)
            public string dailyMissionDate = "";
            public int dailyBestMeters = 0;
            public int dailyFishCollected = 0;
            public int dailyMaxFishSingleRun = 0;
            public int dailyWallKicks = 0;
            public int dailyMiceDefeated = 0;
            public int dailyMaxCombo = 0;
            public int dailyRunsPlayed = 0;
            public List<string> claimedDailyMissionIds = new List<string>();

            // Daily Login Streak (7-day cycle)
            public string lastLoginRewardDate = "";
            public int loginStreakDay = 0; // 0 = not claimed yet, 1..7 = last claimed day
        }

        private static SaveModel data;
        private static bool isDirty = false;

        public static SaveModel Current
        {
            get
            {
                EnsureLoaded();
                return data;
            }
        }

        public static void EnsureLoaded()
        {
            if (data != null) return;

            string rawJson = PlayerPrefs.GetString(SAVE_PREF_KEY, "");
            if (!string.IsNullOrEmpty(rawJson))
            {
                try
                {
                    data = JsonUtility.FromJson<SaveModel>(rawJson);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveData] Failed to parse JSON save, migrating from PlayerPrefs: {ex.Message}");
                    data = null;
                }
            }

            if (data == null)
            {
                data = MigrateFromLegacyPrefs();
                MarkDirty();
                Flush();
            }
            else
            {
                // Keep synced with any direct PlayerPrefs writes
                SyncFromLegacyPrefsIfHigher();
            }

            CheckDailyReset();
        }

        private static SaveModel MigrateFromLegacyPrefs()
        {
            SaveModel model = new SaveModel();

            int legacyHighScore = PlayerPrefs.GetInt(GameManager.HIGH_SCORE_KEY, 0);
            int migratedMeters = legacyHighScore > 0 ? Mathf.Max(1, legacyHighScore / 10) : 0;
            model.bestMeters = Mathf.Max(PlayerPrefs.GetInt(GameManager.BEST_METERS_KEY, 0), migratedMeters);
            model.totalCoins = PlayerPrefs.GetInt(GameManager.COINS_KEY, 0);
            model.totalRuns = PlayerPrefs.GetInt("CatClimb_TotalRuns", 0);
            model.lifetimeDefeatedEnemies = PlayerPrefs.GetInt("CatClimb_DefeatedEnemies", 0);
            model.selectedSkin = PlayerPrefs.GetString("CatClimb_SelectedSkin", "orange");

            string[] skinIds = { "orange", "pink", "ninja", "gold", "cyan" };
            for (int i = 0; i < skinIds.Length; i++)
            {
                string id = skinIds[i];
                if (id == "orange" || PlayerPrefs.GetInt("CatClimb_SkinUnlocked_" + id, 0) == 1)
                {
                    if (!model.unlockedSkins.Contains(id))
                    {
                        model.unlockedSkins.Add(id);
                    }
                }
            }

            model.dailyMissionDate = GetTodayDateKey();
            return model;
        }

        private static void SyncFromLegacyPrefsIfHigher()
        {
            int prefsBest = PlayerPrefs.GetInt(GameManager.BEST_METERS_KEY, 0);
            if (prefsBest > data.bestMeters)
            {
                data.bestMeters = prefsBest;
                isDirty = true;
            }

            int prefsCoins = PlayerPrefs.GetInt(GameManager.COINS_KEY, -1);
            if (prefsCoins >= 0 && prefsCoins != data.totalCoins)
            {
                data.totalCoins = prefsCoins;
                isDirty = true;
            }
        }

        public static string GetTodayDateKey()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }

        /// <summary>
        /// Checks if local midnight has passed since the last daily mission reset.
        /// If so, resets daily counters and claimed daily mission IDs for the new day.
        /// </summary>
        public static bool CheckDailyReset()
        {
            if (data == null) EnsureLoaded();

            string today = GetTodayDateKey();
            if (data.dailyMissionDate != today)
            {
                data.dailyMissionDate = today;
                data.dailyBestMeters = 0;
                data.dailyFishCollected = 0;
                data.dailyMaxFishSingleRun = 0;
                data.dailyWallKicks = 0;
                data.dailyMiceDefeated = 0;
                data.dailyMaxCombo = 0;
                data.dailyRunsPlayed = 0;
                data.claimedDailyMissionIds.Clear();
                isDirty = true;
                return true;
            }
            return false;
        }

        public static void RecordRunCompleted(GameManager.RunStats runStats, int runMeters)
        {
            EnsureLoaded();
            CheckDailyReset();

            data.dailyRunsPlayed++;
            if (runMeters > data.dailyBestMeters)
            {
                data.dailyBestMeters = runMeters;
            }
            data.dailyFishCollected += runStats.sessionCoins;
            if (runStats.sessionCoins > data.dailyMaxFishSingleRun)
            {
                data.dailyMaxFishSingleRun = runStats.sessionCoins;
            }
            data.dailyWallKicks += runStats.wallKicks;
            data.lifetimeWallKicks += runStats.wallKicks;
            data.dailyMiceDefeated += runStats.miceDefeated;
            if (runStats.maxCombo > data.dailyMaxCombo)
            {
                data.dailyMaxCombo = runStats.maxCombo;
            }

            if (runMeters > data.bestMeters)
            {
                data.bestMeters = runMeters;
            }
            data.totalCoins = GameManager.Instance != null ? GameManager.Instance.TotalCoins : data.totalCoins;
            data.totalRuns = PlayerPrefs.GetInt("CatClimb_TotalRuns", data.totalRuns);
            data.lifetimeDefeatedEnemies = PlayerPrefs.GetInt("CatClimb_DefeatedEnemies", data.lifetimeDefeatedEnemies);

            MarkDirty();
        }

        public static bool IsDailyMissionClaimed(string missionId)
        {
            EnsureLoaded();
            CheckDailyReset();
            return data.claimedDailyMissionIds.Contains(missionId);
        }

        public static void MarkDailyMissionClaimed(string missionId)
        {
            EnsureLoaded();
            CheckDailyReset();
            if (!data.claimedDailyMissionIds.Contains(missionId))
            {
                data.claimedDailyMissionIds.Add(missionId);
                MarkDirty();
                Flush();
            }
        }

        public static void MarkDirty()
        {
            isDirty = true;
        }

        public static void Flush()
        {
            if (data == null) return;

            if (isDirty)
            {
                if (GameManager.Instance != null)
                {
                    data.bestMeters = Mathf.Max(data.bestMeters, GameManager.Instance.BestMeters);
                    data.totalCoins = GameManager.Instance.TotalCoins;
                }

                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(SAVE_PREF_KEY, json);
                isDirty = false;
            }

            PlayerPrefs.Save();
        }
    }
}
