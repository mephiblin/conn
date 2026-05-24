namespace Conn.Core.Quests
{
    using Conn.Core.Maps;

    public static class QuestCatalog
    {
        public const string TestHuntId = "quest_test_hunt";
        public const string GuardPatrolId = "quest_guard_patrol";
        public const string DeepScavengerId = "quest_deep_scavenger";

        private static readonly QuestDefinition[] BoardQuests =
        {
            new QuestDefinition(TestHuntId, "Cull the Gate Guard", "monster_test_guard", 10, MapGenerationCatalog.ChapterTwoFirstSliceProfileId, MapPlacementKind.QuestTarget),
            new QuestDefinition(GuardPatrolId, "Break the Guard Patrol", "monster_test_guard", 14, MapGenerationCatalog.ChapterTwoFirstSliceProfileId, MapPlacementKind.QuestTarget),
            new QuestDefinition(DeepScavengerId, "Clear the Deep Scavenger", "monster_test_guard", 18, MapGenerationCatalog.ChapterTwoFirstSliceProfileId, MapPlacementKind.QuestTarget)
        };

        public static QuestDefinition[] AllBoardQuests => BoardQuests;

        public static QuestDefinition Find(string questId)
        {
            for (var i = 0; i < BoardQuests.Length; i++)
            {
                if (BoardQuests[i].QuestId == questId)
                {
                    return BoardQuests[i];
                }
            }

            return null;
        }

        public static QuestDefinition BoardOffer(int offerIndex)
        {
            if (BoardQuests.Length == 0)
            {
                return null;
            }

            var index = offerIndex % BoardQuests.Length;
            if (index < 0)
            {
                index += BoardQuests.Length;
            }

            return BoardQuests[index];
        }
    }
}
