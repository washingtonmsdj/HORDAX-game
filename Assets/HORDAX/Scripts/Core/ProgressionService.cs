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
        private sealed class UpgradeEntry
        {
            public string id;
            public int level;
        }

        [Serializable]
        private sealed class SaveData
        {
            public int walletCoins;
            public List<string> completedLevelIds = new List<string>();
            public List<UpgradeEntry> upgrades = new List<UpgradeEntry>();
            public List<string> unlockedWeaponIds = new List<string> { "rifle" };
            public string equippedWeaponId = "rifle";
        }

        private SaveData data;

        public static ProgressionService Instance { get; private set; }
        public int WalletCoins => data != null ? data.walletCoins : 0;
        public string EquippedWeaponId
        {
            get
            {
                EnsureData();
                return string.IsNullOrWhiteSpace(data.equippedWeaponId) ? "rifle" : data.equippedWeaponId;
            }
        }

        public IReadOnlyList<string> CompletedLevelIds =>
            data != null && data.completedLevelIds != null
                ? (IReadOnlyList<string>)data.completedLevelIds
                : Array.Empty<string>();

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
            EnsureData();
            return !string.IsNullOrWhiteSpace(levelId) && data.completedLevelIds.Contains(levelId);
        }

        public bool IsLevelUnlocked(CampaignDefinition campaign, int levelIndex)
        {
            if (campaign == null || levelIndex < 0 || levelIndex >= campaign.LevelCount) return false;
            if (levelIndex == 0) return true;

            LevelDefinition previous = campaign.GetLevel(levelIndex - 1);
            return previous != null && IsLevelCompleted(previous.LevelId);
        }

        public void CompleteLevel(string levelId, int earnedCoins)
        {
            EnsureData();
            data.walletCoins = Mathf.Max(0, data.walletCoins + Mathf.Max(0, earnedCoins));

            if (!string.IsNullOrWhiteSpace(levelId) && !data.completedLevelIds.Contains(levelId))
                data.completedLevelIds.Add(levelId);

            Save();
        }

        public bool IsWeaponUnlocked(string weaponId)
        {
            EnsureData();
            return !string.IsNullOrWhiteSpace(weaponId) && data.unlockedWeaponIds.Contains(weaponId);
        }

        public bool TryUnlockWeapon(WeaponUnlockDefinition definition)
        {
            if (definition == null) return false;
            EnsureData();

            if (IsWeaponUnlocked(definition.WeaponId)) return true;
            if (!TrySpendCoins(definition.UnlockCost)) return false;

            data.unlockedWeaponIds.Add(definition.WeaponId);
            Save();
            return true;
        }

        public bool EquipWeapon(WeaponUnlockDefinition definition)
        {
            if (definition == null || !IsWeaponUnlocked(definition.WeaponId)) return false;

            EnsureData();
            data.equippedWeaponId = definition.WeaponId;
            Save();
            return true;
        }

        public int GetUpgradeLevel(string upgradeId)
        {
            UpgradeEntry entry = FindUpgrade(upgradeId);
            return entry != null ? Mathf.Max(0, entry.level) : 0;
        }

        public float GetUpgradeMultiplier(PermanentUpgradeDefinition definition)
        {
            if (definition == null) return 1f;
            return definition.GetMultiplier(GetUpgradeLevel(definition.UpgradeId));
        }

        public int GetUpgradeCost(PermanentUpgradeDefinition definition)
        {
            if (definition == null) return int.MaxValue;
            return definition.GetCost(GetUpgradeLevel(definition.UpgradeId));
        }

        public bool TryPurchaseUpgrade(PermanentUpgradeDefinition definition)
        {
            if (definition == null) return false;

            EnsureData();
            int level = GetUpgradeLevel(definition.UpgradeId);
            if (level >= definition.MaxLevel) return false;

            int cost = definition.GetCost(level);
            if (!TrySpendCoins(cost)) return false;

            UpgradeEntry entry = FindUpgrade(definition.UpgradeId);
            if (entry == null)
            {
                entry = new UpgradeEntry { id = definition.UpgradeId, level = 0 };
                data.upgrades.Add(entry);
            }

            entry.level = Mathf.Min(definition.MaxLevel, entry.level + 1);
            Save();
            return true;
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

        private UpgradeEntry FindUpgrade(string id)
        {
            EnsureData();
            if (string.IsNullOrWhiteSpace(id)) return null;

            for (int i = 0; i < data.upgrades.Count; i++)
            {
                UpgradeEntry entry = data.upgrades[i];
                if (entry != null && entry.id == id)
                    return entry;
            }

            return null;
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
            if (data.upgrades == null) data.upgrades = new List<UpgradeEntry>();
            if (data.unlockedWeaponIds == null) data.unlockedWeaponIds = new List<string>();
            if (!data.unlockedWeaponIds.Contains("rifle")) data.unlockedWeaponIds.Add("rifle");
            if (string.IsNullOrWhiteSpace(data.equippedWeaponId) || !data.unlockedWeaponIds.Contains(data.equippedWeaponId))
                data.equippedWeaponId = "rifle";
        }
    }
}
