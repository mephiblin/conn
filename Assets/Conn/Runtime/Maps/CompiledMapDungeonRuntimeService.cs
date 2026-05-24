using Conn.Core.Combat;
using Conn.Core.Maps;
using Conn.Core.Session;
using Conn.Runtime.World;

namespace Conn.Runtime.Maps
{
    public static class CompiledMapDungeonRuntimeService
    {
        public const int DefaultDungeonSeed = 2001;

        public static CompiledMap BuildQuestCompiledMap(GameSessionState session)
        {
            var profile = ResolveProfile(session);
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = MapGenerationService.Generate(profile, chunks, DefaultDungeonSeed);
            return MapGenerationService.Compile(profile, draft);
        }

        public static bool RegisterQuestTargetFieldMonster(GameSessionState session, CompiledMap compiledMap)
        {
            if (session == null || compiledMap == null || !session.Quest.HasActiveQuest)
            {
                return false;
            }

            var placement = CompiledMapRuntimeLoader.FindPlacement(compiledMap, MapPlacementKind.QuestTarget);
            var encounterId = string.IsNullOrWhiteSpace(session.Quest.TargetEncounterId)
                ? EncounterCatalog.TestGuardId
                : session.Quest.TargetEncounterId;
            var monsterId = string.IsNullOrWhiteSpace(session.Quest.TargetMonsterId)
                ? MonsterCatalog.TestGuardId
                : session.Quest.TargetMonsterId;
            FieldMonsterRuntimeService.Register(
                session,
                StateKeyFor(compiledMap, placement),
                placement.Id,
                encounterId,
                monsterId);
            return true;
        }

        public static MapPlacement FindExitAnchor(CompiledMap compiledMap)
        {
            return CompiledMapRuntimeLoader.FindPlacement(compiledMap, MapPlacementKind.Exit);
        }

        public static MapPlacement FindStartAnchor(CompiledMap compiledMap)
        {
            return CompiledMapRuntimeLoader.FindPlacement(compiledMap, MapPlacementKind.Start);
        }

        public static string StateKeyFor(CompiledMap compiledMap, MapPlacement placement)
        {
            return $"compiled_{compiledMap.MapId}_{placement.Id}";
        }

        private static MapProfile ResolveProfile(GameSessionState session)
        {
            if (session == null
                || string.IsNullOrWhiteSpace(session.Quest.MapProfileId)
                || session.Quest.MapProfileId == MapGenerationCatalog.ChapterTwoFirstSliceProfileId)
            {
                return MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            }

            return MapGenerationCatalog.ChapterTwoFirstSliceProfile();
        }
    }
}
