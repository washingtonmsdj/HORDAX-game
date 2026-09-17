using System;
using System.Collections.Generic;
using UnityEngine;
using HORDAX.Data;

namespace HORDAX.Core
{
    public sealed class ProgressionService : MonoBehaviour
    {
        private const string SaveKey = "HORDAX_PROGRESS_V1";

        [Serializable]
        private sealed class SaveData
        {
            public int walletCoins;
            public List<string> completedLevelIds = new List<string>();
        }

        private SaveData data;

        public static ProgressionService Instance { get; private set; }
        public int WalletCoins => data != null ? data.walletCoins : 0;
        public IReadOnlyList<string> CompletedLevelIds => data != null && data.completedLevelIds != null\n            ? (IReadOnlyList<string>)data.completedLevelIds\n            : Array.Empty<string>();

        public static ProgressionService GetOrCreate()
        {
            if (Instance != null) return Instance;

            ProgressionService existing = FindObjectOfType<ProgressionService>();
            if (existing != null) return existing;

            GameObject go = new GameObject("HORDAX Progression");
            return go.AddComponent<ProgressionService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public bool IsLevelCompleted(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId) || data == null) return false;
            return data.completedLevelIds.Contains(levelId);
        }

        public void CompleteLevel(string levelId, int earnedCoins)
        {
            EnsureData();
            data.walletCoins = Mathf.Max(0, data.walletCoins + Mathf.Max(0, earnedCoins));

            if (!string.IsNullOrWhiteSpace(levelId) && !data.completedLevelIds.Contains(levelId))
                data.completedLevelIds.Add(levelId);

            Save();
        }

        public bool IsLevelUnlocked(CampaignDefinition campaign, int levelIndex)
        {
            if (campaign == null || levelIndex < 0 || levelIndex >= campaign.LevelCount) return false;
            if (levelIndex == 0) return true;

            LevelDefinition previous = campaign.GetLevel(levelIndex - 1);
            return previous != null && IsLevelCompleted(previous.LevelId);
        }

        public bool TrySpendCoins(int amount)
        {
            EnsureData();
            amount = Mathf.Max(0, amount);
            if (data.walletCoins < amount) return false;

            data.walletCoins -= amount;
            Save();
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            EnsureData();
            data.walletCoins += amount;
            Save();
        }

        public void ResetProgress()
        {
            data = new SaveData();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                data = new SaveData();
                return;
            }

            data = JsonUtility.FromJson<SaveData>(json);
            EnsureData();
        }

        private void Save()
        {
            EnsureData();
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void EnsureData()
        {
            if (data == null) data = new SaveData();
            if (data.completedLevelIds == null) data.completedLevelIds = new List<string>();
        }
    }
}
