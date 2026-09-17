using System.Collections.Generic;
using UnityEngine;

namespace HORDAX.Data
{
    [CreateAssetMenu(fileName = "CampaignDefinition", menuName = "HORDAX/Campaign Definition")]
    public sealed class CampaignDefinition : ScriptableObject
    {
        [SerializeField] private string campaignId = "main";
        [SerializeField] private string displayName = "HORDAX Campaign";
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

        public string CampaignId => campaignId;
        public string DisplayName => displayName;
        public IReadOnlyList<LevelDefinition> Levels => levels ?? (levels = new List<LevelDefinition>());
        public int LevelCount => levels != null ? levels.Count : 0;

        public LevelDefinition GetLevel(int index)
        {
            return index >= 0 && index < LevelCount ? levels[index] : null;
        }

        public void ConfigureRuntime(string id, string label, IEnumerable<LevelDefinition> runtimeLevels)
        {
            campaignId = id;
            displayName = label;
            levels = runtimeLevels != null ? new List<LevelDefinition>(runtimeLevels) : new List<LevelDefinition>();
        }
    }
}
