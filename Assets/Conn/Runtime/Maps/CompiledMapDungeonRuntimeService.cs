using Conn.Core.Combat;
using Conn.Core.Maps;
using Conn.Core.Session;
using Conn.MapGenV2.Core;
using Conn.Runtime.Content;
using Conn.Runtime.World;
using System.Collections.Generic;

namespace Conn.Runtime.Maps
{
    public static class CompiledMapDungeonRuntimeService
    {
        public const int DefaultDungeonSeed = 2001;
        private static CompiledMapAsset[] compiledMapAssets = System.Array.Empty<CompiledMapAsset>();
        private static MapGenBakedMapAsset[] mapGenV2BakedMaps = System.Array.Empty<MapGenBakedMapAsset>();
        private static RuntimeMapGenerationBundleAsset[] runtimeMapGenerationBundles = System.Array.Empty<RuntimeMapGenerationBundleAsset>();
        private static CompiledMap currentCompiledMap;

        public static void SetCompiledMapAssets(CompiledMapAsset[] assets)
        {
            compiledMapAssets = assets ?? System.Array.Empty<CompiledMapAsset>();
        }

        public static void SetMapGenV2BakedMaps(MapGenBakedMapAsset[] assets)
        {
            mapGenV2BakedMaps = assets ?? System.Array.Empty<MapGenBakedMapAsset>();
        }

        public static void SetRuntimeMapGenerationBundles(RuntimeMapGenerationBundleAsset[] bundles)
        {
            runtimeMapGenerationBundles = bundles ?? System.Array.Empty<RuntimeMapGenerationBundleAsset>();
        }

        public static CompiledMap BuildQuestCompiledMap(GameSessionState session)
        {
            SyncActiveQuestDefinition(session);

            var profile = ResolveProfile(session);
            var compiledAsset = FindCompiledMapAsset(profile.ProfileId);
            if (compiledAsset != null)
            {
                // Baked editable maps are cropped to authored bounds, while the
                // profile dimensions describe the generation canvas.
                var compiled = CompiledMapRuntimeLoader.LoadFromJson(compiledAsset.Json);
                MapValidationService.ThrowIfFailed(MapValidationService.ValidateCompiled(
                    BuildCompiledAssetValidationProfile(profile, compiled),
                    compiled));
                currentCompiledMap = compiled;
                return currentCompiledMap;
            }

            var mapGenV2BakedMap = FindMapGenV2BakedMap(profile.ProfileId);
            if (mapGenV2BakedMap != null)
            {
                currentCompiledMap = MapGenV2CompiledMapAdapter.ToCompiledMap(mapGenV2BakedMap);
                return currentCompiledMap;
            }

            var runtimeBundle = FindRuntimeMapGenerationBundle(profile.ProfileId);
            if (runtimeBundle != null)
            {
                currentCompiledMap = RuntimeMapGenerationService.GenerateCompiled(runtimeBundle.Bundle, profile.ProfileId, DefaultDungeonSeed);
                return currentCompiledMap;
            }

            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            currentCompiledMap = MapGenerationService.GenerateCompiled(profile, chunks, DefaultDungeonSeed);
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
            if (placement == null)
            {
                return false;
            }

            var encounterPlacement = CompiledMapRuntimeLoader.FindEncounterPlacement(compiledMap, placement.Id);
            var encounterId = ResolveQuestEncounterId(session, encounterPlacement);
            var monsterId = ResolveQuestMonsterId(session, encounterPlacement);
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

        public static string ResolveQuestEncounterId(GameSessionState session, CompiledEncounterPlacement encounterPlacement)
        {
            if (session != null && !string.IsNullOrWhiteSpace(session.Quest.TargetEncounterId))
            {
                return session.Quest.TargetEncounterId;
            }

            if (encounterPlacement != null && !string.IsNullOrWhiteSpace(encounterPlacement.EncounterId))
            {
                return encounterPlacement.EncounterId;
            }

            return EncounterCatalog.TestGuardId;
        }

        public static string ResolveQuestMonsterId(GameSessionState session, CompiledEncounterPlacement encounterPlacement)
        {
            if (session != null && !string.IsNullOrWhiteSpace(session.Quest.TargetMonsterId))
            {
                return session.Quest.TargetMonsterId;
            }

            if (encounterPlacement != null && !string.IsNullOrWhiteSpace(encounterPlacement.PrimaryMonsterId))
            {
                return encounterPlacement.PrimaryMonsterId;
            }

            return MonsterCatalog.TestGuardId;
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

        public static int CountBlockingObjects(CompiledMap compiledMap)
        {
            var count = 0;
            foreach (var placement in BlockingObjects(compiledMap))
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

        public static IEnumerable<CompiledMapObjectPlacement> BlockingObjects(CompiledMap compiledMap)
        {
            for (var i = 0; i < (compiledMap?.Objects?.Count ?? 0); i++)
            {
                var placement = compiledMap.Objects[i];
                if (placement == null)
                {
                    continue;
                }

                if (placement.BlocksMovement || placement.Kind == RoomChunkObjectKind.Blocker)
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

        private static void SyncActiveQuestDefinition(GameSessionState session)
        {
            if (session == null || !session.Quest.HasActiveQuest)
            {
                return;
            }

            var definition = RuntimeContentDatabase.FindQuest(session.Quest.ActiveQuestId);
            if (definition == null)
            {
                return;
            }

            session.Quest.ActiveQuestTitle = definition.DisplayName;
            session.Quest.TargetMonsterId = definition.TargetMonsterId;
            session.Quest.TargetEncounterId = definition.TargetEncounterId;
            session.Quest.MapProfileId = definition.MapProfileId;
            session.Quest.GoldReward = definition.GoldReward;
        }

        private static MapProfile ResolveProfile(GameSessionState session)
        {
            if (session == null
                || string.IsNullOrWhiteSpace(session.Quest.MapProfileId)
                || session.Quest.MapProfileId == MapGenerationCatalog.ChapterTwoFirstSliceProfileId)
            {
                return MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            }

            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            profile.ProfileId = session.Quest.MapProfileId;
            return profile;
        }

        private static MapProfile BuildCompiledAssetValidationProfile(MapProfile profile, CompiledMap compiled)
        {
            return new MapProfile
            {
                ProfileId = profile.ProfileId,
                MapKind = profile.MapKind,
                Theme = profile.Theme,
                Width = compiled.Width,
                Height = compiled.Height,
                RequiredAnchors = new List<MapAnchorKind>(profile.RequiredAnchors)
            };
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

        private static MapGenBakedMapAsset FindMapGenV2BakedMap(string profileId)
        {
            MapGenBakedMapAsset seedFallback = null;
            for (var i = 0; i < mapGenV2BakedMaps.Length; i++)
            {
                var asset = mapGenV2BakedMaps[i];
                if (asset == null || !MapGenBakedMapMigration.IsCompatible(asset))
                {
                    continue;
                }

                if (asset.ProfileId == profileId && asset.Seed == DefaultDungeonSeed)
                {
                    return asset;
                }

                if (seedFallback == null && asset.ProfileId == profileId)
                {
                    seedFallback = asset;
                }
            }

            return seedFallback;
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
