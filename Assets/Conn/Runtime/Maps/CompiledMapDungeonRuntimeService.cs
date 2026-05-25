using Conn.Core.Combat;
using Conn.Core.Maps;
using Conn.Core.Session;
using Conn.Runtime.World;

namespace Conn.Runtime.Maps
{
    public static class CompiledMapDungeonRuntimeService
    {
        public const int DefaultDungeonSeed = 2001;
        private static CompiledMapAsset[] compiledMapAssets = System.Array.Empty<CompiledMapAsset>();
        private static RuntimeMapGenerationBundleAsset[] runtimeMapGenerationBundles = System.Array.Empty<RuntimeMapGenerationBundleAsset>();

        public static void SetCompiledMapAssets(CompiledMapAsset[] assets)
        {
            compiledMapAssets = assets ?? System.Array.Empty<CompiledMapAsset>();
        }

        public static void SetRuntimeMapGenerationBundles(RuntimeMapGenerationBundleAsset[] bundles)
        {
            runtimeMapGenerationBundles = bundles ?? System.Array.Empty<RuntimeMapGenerationBundleAsset>();
        }

        public static CompiledMap BuildQuestCompiledMap(GameSessionState session)
        {
            var profile = ResolveProfile(session);
            var compiledAsset = FindCompiledMapAsset(profile.ProfileId);
            if (compiledAsset != null)
            {
                return CompiledMapRuntimeLoader.LoadAndValidateFromJson(compiledAsset.Json, profile);
            }

            var runtimeBundle = FindRuntimeMapGenerationBundle(profile.ProfileId);
            if (runtimeBundle != null)
            {
                return RuntimeMapGenerationService.GenerateCompiled(runtimeBundle.Bundle, profile.ProfileId, DefaultDungeonSeed);
            }

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
            var encounterPlacement = CompiledMapRuntimeLoader.FindEncounterPlacement(compiledMap, placement.Id);
            var encounterId = encounterPlacement != null && !string.IsNullOrWhiteSpace(encounterPlacement.EncounterId)
                ? encounterPlacement.EncounterId
                : string.IsNullOrWhiteSpace(session.Quest.TargetEncounterId)
                ? EncounterCatalog.TestGuardId
                : session.Quest.TargetEncounterId;
            var monsterId = encounterPlacement != null && !string.IsNullOrWhiteSpace(encounterPlacement.PrimaryMonsterId)
                ? encounterPlacement.PrimaryMonsterId
                : string.IsNullOrWhiteSpace(session.Quest.TargetMonsterId)
                ? MonsterCatalog.TestGuardId
                : session.Quest.TargetMonsterId;
            FieldMonsterRuntimeService.Register(
                session,
                StateKeyFor(compiledMap, placement),
                placement.Id,
                encounterId,
                monsterId);
            RegisterMonsterPlacements(session, compiledMap, encounterId, monsterId);
            return true;
        }

        public static int RegisterMonsterPlacements(GameSessionState session, CompiledMap compiledMap, string encounterId, string monsterId)
        {
            if (session == null || compiledMap == null)
            {
                return 0;
            }

            var registered = 0;
            for (var i = 0; i < compiledMap.Placements.Count; i++)
            {
                var placement = compiledMap.Placements[i];
                if (placement.Kind != MapPlacementKind.Monster)
                {
                    continue;
                }

                var encounterPlacement = CompiledMapRuntimeLoader.FindEncounterPlacement(compiledMap, placement.Id);
                FieldMonsterRuntimeService.Register(
                    session,
                    StateKeyFor(compiledMap, placement),
                    placement.Id,
                    encounterPlacement != null && !string.IsNullOrWhiteSpace(encounterPlacement.EncounterId) ? encounterPlacement.EncounterId : encounterId,
                    encounterPlacement != null && !string.IsNullOrWhiteSpace(encounterPlacement.PrimaryMonsterId) ? encounterPlacement.PrimaryMonsterId : monsterId);
                registered++;
            }

            return registered;
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

        private static CompiledMapAsset FindCompiledMapAsset(string profileId)
        {
            for (var i = 0; i < compiledMapAssets.Length; i++)
            {
                var asset = compiledMapAssets[i];
                if (asset != null && asset.ProfileId == profileId && !string.IsNullOrWhiteSpace(asset.Json))
                {
                    return asset;
                }
            }

            return null;
        }

        private static RuntimeMapGenerationBundleAsset FindRuntimeMapGenerationBundle(string profileId)
        {
            for (var i = 0; i < runtimeMapGenerationBundles.Length; i++)
            {
                var asset = runtimeMapGenerationBundles[i];
                if (asset != null && asset.Bundle != null && asset.Bundle.FindProfile(profileId) != null)
                {
                    return asset;
                }
            }

            return null;
        }
    }
}
