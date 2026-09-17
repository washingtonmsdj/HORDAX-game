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
        public IReadOnlyList<LevelDefinition> Levels => levels;
        public int LevelCount => levels.Count;

        public LevelDefinition GetLevel(int index)
        {
            return index >= 0 && index < levels.Count ? levels[index] : null;
        }
    }
}
