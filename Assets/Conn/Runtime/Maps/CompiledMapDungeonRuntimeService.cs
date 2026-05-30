using Conn.Core.Combat;
using Conn.Core.Maps;
using Conn.Core.Session;
using Conn.Runtime.World;
using System.Collections.Generic;

namespace Conn.Runtime.Maps
{
    public static class CompiledMapDungeonRuntimeService
    {
        public const int DefaultDungeonSeed = 2001;
        private static CompiledMapAsset[] compiledMapAssets = System.Array.Empty<CompiledMapAsset>();
        private static RuntimeMapGenerationBundleAsset[] runtimeMapGenerationBundles = System.Array.Empty<RuntimeMapGenerationBundleAsset>();
        private static CompiledMap currentCompiledMap;

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
                currentCompiledMap = CompiledMapRuntimeLoader.LoadAndValidateFromJson(compiledAsset.Json, profile);
                return currentCompiledMap;
            }

            var runtimeBundle = FindRuntimeMapGenerationBundle(profile.ProfileId);
            if (runtimeBundle != null)
            {
                currentCompiledMap = RuntimeMapGenerationService.GenerateCompiled(runtimeBundle.Bundle, profile.ProfileId, DefaultDungeonSeed);
                return currentCompiledMap;
            }

            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var draft = MapGenerationService.Generate(profile, chunks, DefaultDungeonSeed);
            currentCompiledMap = MapGenerationService.Compile(profile, draft);
            return currentCompiledMap;
        }

        public static CompiledMap CurrentCompiledMap => currentCompiledMap;

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
            FieldMonsterRuntimeService.RegisterAt(
                session,
                StateKeyFor(compiledMap, placement),
                placement.Id,
                encounterId,
                monsterId,
                placement.X,
                placement.Y);
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
                FieldMonsterRuntimeService.RegisterAt(
                    session,
                    StateKeyFor(compiledMap, placement),
                    placement.Id,
                    encounterPlacement != null && !string.IsNullOrWhiteSpace(encounterPlacement.EncounterId) ? encounterPlacement.EncounterId : encounterId,
                    encounterPlacement != null && !string.IsNullOrWhiteSpace(encounterPlacement.PrimaryMonsterId) ? encounterPlacement.PrimaryMonsterId : monsterId,
                    placement.X,
                    placement.Y);
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

        public static int CountBakedCells(CompiledMap compiledMap)
        {
            return compiledMap?.Cells?.Count ?? 0;
        }

        public static int CountBakedObjects(CompiledMap compiledMap)
        {
            return compiledMap?.Objects?.Count ?? 0;
        }

        public static int CountInteractiveObjects(CompiledMap compiledMap)
        {
            var count = 0;
            foreach (var placement in InteractiveObjects(compiledMap))
            {
                if (placement != null)
                {
                    count++;
                }
            }

            return count;
        }

        public static IEnumerable<CompiledMapObjectPlacement> InteractiveObjects(CompiledMap compiledMap)
        {
            for (var i = 0; i < (compiledMap?.Objects?.Count ?? 0); i++)
            {
                var placement = compiledMap.Objects[i];
                if (placement == null)
                {
                    continue;
                }

                if (placement.Kind == RoomChunkObjectKind.Chest
                    || placement.Kind == RoomChunkObjectKind.Barrel
                    || placement.Kind == RoomChunkObjectKind.Torch)
                {
                    yield return placement;
                }
            }
        }

        public static int RegisterBakedSpawnHintPlacements(GameSessionState session, CompiledMap compiledMap)
        {
            if (session == null || compiledMap == null)
            {
                return 0;
            }

            var registered = 0;
            foreach (var bakedObject in compiledMap.Objects ?? new List<CompiledMapObjectPlacement>())
            {
                if (bakedObject == null || bakedObject.Kind != RoomChunkObjectKind.SpawnHint)
                {
                    continue;
                }

                var placement = CompiledMapRuntimeLoader.FindPlacement(compiledMap, MapPlacementKind.Monster);
                if (placement == null)
                {
                    continue;
                }

                FieldMonsterRuntimeService.RegisterAt(
                    session,
                    $"compiled_{compiledMap.MapId}_object_{bakedObject.PlacementId}",
                    bakedObject.PlacementId,
                    session.Quest.TargetEncounterId,
                    session.Quest.TargetMonsterId,
                    bakedObject.X,
                    bakedObject.Y);
                registered++;
            }

            return registered;
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
