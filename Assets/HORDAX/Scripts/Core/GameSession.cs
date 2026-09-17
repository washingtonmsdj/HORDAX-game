using HORDAX.Data;

namespace HORDAX.Core
{
    public static class GameSession
    {
        public static CampaignDefinition ActiveCampaign { get; private set; }
        public static LevelDefinition SelectedLevel { get; private set; }
        public static int SelectedLevelIndex { get; private set; } = -1;

        public static bool SelectLevel(CampaignDefinition campaign, int index)
        {
            if (campaign == null || index < 0 || index >= campaign.LevelCount) return false;

            ActiveCampaign = campaign;
            SelectedLevelIndex = index;
            SelectedLevel = campaign.GetLevel(index);
            return SelectedLevel != null;
        }

        public static void Clear()
        {
            ActiveCampaign = null;
            SelectedLevel = null;
            SelectedLevelIndex = -1;
        }
    }
}
