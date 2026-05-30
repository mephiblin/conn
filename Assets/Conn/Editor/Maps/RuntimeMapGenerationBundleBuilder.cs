using Conn.Authoring.Maps;
using Conn.Authoring.Content;
using Conn.Core.Combat;
using Conn.Core.Maps;
using Conn.Runtime.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class RuntimeMapGenerationBundleBuilder
    {
        public const string DefaultBundleAssetPath = "Assets/Conn/Core/Maps/RuntimeMapGenerationBundle.asset";

        public static RuntimeMapGenerationBundle Build(MapAuthoringSnapshot snapshot, int floor = 1, int difficulty = 0)
        {
            var report = MapAuthoringValidationService.Validate(snapshot);
            MapValidationService.ThrowIfFailed(report);
            floor = Math.Max(1, floor);
            difficulty = Math.Max(0, difficulty);

            var bundle = new RuntimeMapGenerationBundle
            {
                BundleId = "runtime_map_generation_bundle",
                Version = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            };

            foreach (var profile in snapshot.Profiles ?? Array.Empty<MapProfileAsset>())
            {
                if (profile == null)
                {
                    continue;
                }

                bundle.Profiles.Add(BuildProfileEntry(profile, floor, difficulty));
            }

            return bundle;
        }

        public static RuntimeMapGenerationBundle BuildChapterTwoCatalogBundle(int floor = 1, int difficulty = 0)
        {
            floor = Math.Max(1, floor);
            difficulty = Math.Max(0, difficulty);
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            return new RuntimeMapGenerationBundle
            {
                BundleId = "chapter_two_catalog_runtime_map_generation_bundle",
                Version = "catalog",
                Profiles = new List<RuntimeMapProfileEntry>
                {
                    new RuntimeMapProfileEntry
                    {
                        Profile = profile,
                        Floor = floor,
                        Difficulty = difficulty,
                        Chunks = chunks,
                        EncounterPlacementRules = new List<RuntimeEncounterPlacementRule>
                        {
                            new RuntimeEncounterPlacementRule
                            {
                                Id = "catalog_quest_target",
                                PlacementKind = MapPlacementKind.QuestTarget,
                                EncounterId = EncounterCatalog.TestGuardId,
                                PrimaryMonsterId = MonsterCatalog.TestGuardId,
                                SpawnRole = "quest_target",
                                RequiredForQuest = true
                            },
                            new RuntimeEncounterPlacementRule
                            {
                                Id = "catalog_monster",
                                PlacementKind = MapPlacementKind.Monster,
                                EncounterId = EncounterCatalog.TestGuardId,
                                PrimaryMonsterId = MonsterCatalog.TestGuardId,
                                SpawnRole = "trash",
                                SpawnEntries = new List<RuntimeSpawnEntry>
                                {
                                    new RuntimeSpawnEntry
                                    {
                                        EncounterId = EncounterCatalog.TestGuardId,
                                        PrimaryMonsterId = MonsterCatalog.TestGuardId,
                                        SpawnRole = "trash",
                                        Weight = 1,
                                        MinFloor = 1,
                                        MaxFloor = 99,
                                        ThemeTags = new List<string> { profile.Theme },
                                        SpawnRoleTags = new List<string> { "trash" }
                                    }
                                }
                            },
                            new RuntimeEncounterPlacementRule
                            {
                                Id = "catalog_boss",
                                PlacementKind = MapPlacementKind.Boss,
                                EncounterId = EncounterCatalog.TestGuardId,
                                PrimaryMonsterId = MonsterCatalog.TestGuardId,
                                SpawnRole = "boss"
                            }
                        },
                        ValidationHash = $"{profile.ProfileId}:{chunks.Count}:catalog"
                    }
                }
            };
        }

        public static RuntimeMapGenerationBundleAsset SaveBundleAsset(RuntimeMapGenerationBundle bundle, string path = DefaultBundleAssetPath)
        {
            if (bundle == null)
            {
                throw new ArgumentNullException(nameof(bundle));
            }

            EnsureFolderForAsset(path);
            var asset = AssetDatabase.LoadAssetAtPath<RuntimeMapGenerationBundleAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RuntimeMapGenerationBundleAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Bundle = bundle;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static void VerifyRuntimeGenerationFromBundle()
        {
            var bundle = BuildChapterTwoCatalogBundle();
            VerifyRuntimeSafeBundleContract(bundle);
            var compiled = RuntimeMapGenerationService.GenerateCompiled(
                bundle,
                MapGenerationCatalog.ChapterTwoFirstSliceProfileId,
                2001);

            if (compiled.ProfileId != MapGenerationCatalog.ChapterTwoFirstSliceProfileId)
            {
                throw new InvalidOperationException("Runtime map generation bundle did not preserve profile id.");
            }

            if (compiled.Placements.Count == 0)
            {
                throw new InvalidOperationException("Runtime map generation bundle produced no placements.");
            }

            if (compiled.EncounterPlacements.Count == 0)
            {
                throw new InvalidOperationException("Runtime map generation bundle produced no encounter placements.");
            }

            var questTarget = compiled.EncounterPlacements.Find(placement => placement.RequiredForQuest);
            VerifyEncounterPlacement(compiled, questTarget, MapPlacementKind.QuestTarget, EncounterCatalog.TestGuardId, MonsterCatalog.TestGuardId, "quest target");

            var boss = compiled.EncounterPlacements.Find(placement => placement.SpawnRole == "boss");
            VerifyEncounterPlacement(compiled, boss, MapPlacementKind.Boss, EncounterCatalog.TestGuardId, MonsterCatalog.TestGuardId, "boss");

            var weightedBundle = BuildWeightedSpawnProbeBundle();
            VerifyRuntimeSafeBundleContract(weightedBundle);
            var first = RuntimeMapGenerationService.GenerateCompiled(
                weightedBundle,
                MapGenerationCatalog.ChapterTwoFirstSliceProfileId,
                2112);
            var second = RuntimeMapGenerationService.GenerateCompiled(
                weightedBundle,
                MapGenerationCatalog.ChapterTwoFirstSliceProfileId,
                2112);
            var firstMonster = first.EncounterPlacements.Find(placement => placement.SpawnRole == "trash");
            var secondMonster = second.EncounterPlacements.Find(placement => placement.SpawnRole == "trash");
            if (firstMonster == null || secondMonster == null || firstMonster.EncounterId != secondMonster.EncounterId)
            {
                throw new InvalidOperationException("Runtime weighted spawn table resolution must be deterministic for the same profile and seed.");
            }

            if (firstMonster.SpawnSourceId == "probe_spawn_floor_two")
            {
                throw new InvalidOperationException("Runtime weighted spawn table resolution must exclude entries outside the active floor.");
            }

            weightedBundle.Profiles[0].Floor = 2;
            var floorTwo = RuntimeMapGenerationService.GenerateCompiled(
                weightedBundle,
                MapGenerationCatalog.ChapterTwoFirstSliceProfileId,
                2112);
            var floorTwoMonster = floorTwo.EncounterPlacements.Find(placement => placement.SpawnRole == "trash");
            if (floorTwoMonster == null || floorTwoMonster.SpawnSourceId != "probe_spawn_floor_two")
            {
                throw new InvalidOperationException("Runtime weighted spawn table resolution must include entries matching the active floor.");
            }
        }

        private static void VerifyRuntimeSafeBundleContract(RuntimeMapGenerationBundle bundle)
        {
            if (bundle == null)
            {
                throw new InvalidOperationException("Runtime map generation bundle must not be null.");
            }

            var allowedTypes = new[]
            {
                typeof(RuntimeMapGenerationBundle),
                typeof(RuntimeMapProfileEntry),
                typeof(RuntimeEncounterPlacementRule),
                typeof(RuntimeSpawnEntry),
                typeof(MapProfile),
                typeof(RuntimeMapRoomPoolRule),
                typeof(ChunkPreset),
                typeof(RoomChunkSocketDefinition),
                typeof(ChunkAnchor),
                typeof(RoomChunkCell),
                typeof(RoomChunkObjectPlacement)
            };

            foreach (var type in allowedTypes)
            {
                VerifyRuntimeSafeType(type);
            }

            if (string.IsNullOrWhiteSpace(bundle.BundleId))
            {
                throw new InvalidOperationException("Runtime map generation bundle id must not be empty.");
            }

            if (bundle.Profiles == null || bundle.Profiles.Count == 0)
            {
                throw new InvalidOperationException("Runtime map generation bundle must contain at least one profile.");
            }

            foreach (var profile in bundle.Profiles)
            {
                if (profile == null || profile.Profile == null || string.IsNullOrWhiteSpace(profile.Profile.ProfileId))
                {
                    throw new InvalidOperationException("Runtime map generation bundle contains an invalid profile entry.");
                }

                if (profile.Chunks == null || profile.Chunks.Count == 0)
                {
                    throw new InvalidOperationException($"Runtime map generation bundle profile {profile.Profile.ProfileId} has no chunks.");
                }
            }
        }

        private static void VerifyRuntimeSafeType(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var fieldType = field.FieldType;
                if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                {
                    throw new InvalidOperationException($"{type.Name}.{field.Name} must not store UnityEngine.Object references in RuntimeMapGenerationBundle.");
                }

                var fieldNamespace = fieldType.Namespace ?? string.Empty;
                if (fieldNamespace.StartsWith("UnityEditor", StringComparison.Ordinal)
                    || fieldNamespace.StartsWith("Conn.Editor", StringComparison.Ordinal)
                    || fieldNamespace.StartsWith("Conn.Authoring", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"{type.Name}.{field.Name} stores editor-only type {fieldType.FullName}.");
                }
            }
        }

        private static void VerifyEncounterPlacement(CompiledMap compiled, CompiledEncounterPlacement encounterPlacement, MapPlacementKind expectedKind, string expectedEncounterId, string expectedMonsterId, string label)
        {
            if (encounterPlacement == null)
            {
                throw new InvalidOperationException($"Runtime map generation bundle did not produce a {label} encounter placement.");
            }

            var mapPlacement = compiled.Placements.Find(placement => placement.Id == encounterPlacement.MapPlacementId);
            if (mapPlacement == null || mapPlacement.Kind != expectedKind)
            {
                throw new InvalidOperationException($"Runtime map generation bundle {label} encounter placement does not target a {expectedKind} map placement.");
            }

            if (encounterPlacement.EncounterId != expectedEncounterId || encounterPlacement.PrimaryMonsterId != expectedMonsterId)
            {
                throw new InvalidOperationException($"Runtime map generation bundle did not produce a valid {label} encounter placement.");
            }

            var runtimeEncounter = RuntimeContentDatabase.FindEncounter(encounterPlacement.EncounterId);
            if (runtimeEncounter == null || runtimeEncounter.EncounterId != expectedEncounterId || runtimeEncounter.MonsterId != expectedMonsterId)
            {
                throw new InvalidOperationException($"RuntimeContentDatabase could not resolve the {label} encounter placement.");
            }
        }

        private static RuntimeMapGenerationBundle BuildWeightedSpawnProbeBundle()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            return new RuntimeMapGenerationBundle
            {
                BundleId = "weighted_spawn_probe_bundle",
                Version = "probe",
                Profiles = new List<RuntimeMapProfileEntry>
                {
                    new RuntimeMapProfileEntry
                    {
                        Profile = profile,
                        Floor = 1,
                        Difficulty = 0,
                        Chunks = chunks,
                        EncounterPlacementRules = new List<RuntimeEncounterPlacementRule>
                        {
                            new RuntimeEncounterPlacementRule
                            {
                                Id = "weighted_monster_probe",
                                PlacementKind = MapPlacementKind.Monster,
                                SpawnRole = "trash",
                                SpawnEntries = new List<RuntimeSpawnEntry>
                                {
                                    new RuntimeSpawnEntry
                                    {
                                        EncounterId = EncounterCatalog.TestGuardId,
                                        PrimaryMonsterId = MonsterCatalog.TestGuardId,
                                        SpawnSourceId = "probe_spawn_a",
                                        SpawnRole = "trash",
                                        Weight = 1,
                                        MinFloor = 1,
                                        MaxFloor = 1,
                                        ThemeTags = new List<string> { profile.Theme },
                                        SpawnRoleTags = new List<string> { "trash" }
                                    },
                                    new RuntimeSpawnEntry
                                    {
                                        EncounterId = $"generated_single_primary_{MonsterCatalog.TestGuardId}",
                                        PrimaryMonsterId = MonsterCatalog.TestGuardId,
                                        SpawnSourceId = "probe_spawn_b",
                                        SpawnRole = "trash",
                                        Weight = 3,
                                        MinFloor = 1,
                                        MaxFloor = 99,
                                        ThemeTags = new List<string> { "wrong_theme" },
                                        SpawnRoleTags = new List<string> { "trash" }
                                    },
                                    new RuntimeSpawnEntry
                                    {
                                        EncounterId = EncounterCatalog.TestGuardId,
                                        PrimaryMonsterId = MonsterCatalog.TestGuardId,
                                        SpawnSourceId = "probe_spawn_floor_two",
                                        SpawnRole = "trash",
                                        Weight = 1,
                                        MinFloor = 2,
                                        MaxFloor = 99,
                                        ThemeTags = new List<string> { profile.Theme },
                                        SpawnRoleTags = new List<string> { "trash" }
                                    }
                                }
                            }
                        },
                        ValidationHash = $"{profile.ProfileId}:{chunks.Count}:weighted_probe"
                    }
                }
            };
        }

        private static RuntimeMapProfileEntry BuildProfileEntry(MapProfileAsset profile, int floor, int difficulty)
        {
            var runtimeProfile = profile.ToRuntimeProfile();
            var chunks = new List<ChunkPreset>();
            foreach (var chunk in profile.OptionalChunks ?? Array.Empty<RoomChunkAsset>())
            {
                if (chunk != null)
                {
                    chunks.Add(ToChunkPreset(chunk));
                }
            }

            foreach (var landmark in profile.RequiredLandmarkRooms ?? Array.Empty<LandmarkRoomAsset>())
            {
                if (landmark != null)
                {
                    chunks.Add(ToChunkPreset(landmark));
                    runtimeProfile.RequiredLandmarkRoomIds.Add(landmark.Id);
                }
            }

            foreach (var landmark in profile.OptionalLandmarks ?? Array.Empty<LandmarkRoomAsset>())
            {
                if (landmark != null)
                {
                    chunks.Add(ToChunkPreset(landmark));
                    runtimeProfile.OptionalLandmarkRoomIds.Add(landmark.Id);
                }
            }

            return new RuntimeMapProfileEntry
            {
                Profile = runtimeProfile,
                Floor = floor,
                Difficulty = difficulty,
                Chunks = chunks,
                ResourceSetRuntimeIds = new List<string> { runtimeProfile.ResourceSetId },
                SpawnTableIds = new List<string>(runtimeProfile.SpawnTableIds),
                EncounterPlacementRules = BuildEncounterPlacementRules(profile),
                GenerationWeightProfileIds = string.IsNullOrWhiteSpace(runtimeProfile.GenerationWeightProfileId)
                    ? new List<string>()
                    : new List<string> { runtimeProfile.GenerationWeightProfileId },
                ValidationHash = $"{runtimeProfile.ProfileId}:{chunks.Count}:{runtimeProfile.ResourceSetId}:{runtimeProfile.GenerationWeightProfileId}"
            };
        }

        private static List<RuntimeEncounterPlacementRule> BuildEncounterPlacementRules(MapProfileAsset profile)
        {
            var rules = new List<RuntimeEncounterPlacementRule>();
            var directEncounterId = FirstDirectEncounterId(profile);
            var directMonsterId = FirstDirectEncounterMonsterId(profile);
            var spawnSourceId = FirstSpawnTableId(profile);
            var spawnEntries = BuildSpawnEntries(profile, "trash");

            if (!string.IsNullOrWhiteSpace(directEncounterId) && !string.IsNullOrWhiteSpace(directMonsterId))
            {
                rules.Add(new RuntimeEncounterPlacementRule
                {
                    Id = $"{profile.Id}_quest_target",
                    PlacementKind = MapPlacementKind.QuestTarget,
                    SpawnSourceId = spawnSourceId,
                    EncounterId = directEncounterId,
                    PrimaryMonsterId = directMonsterId,
                    SpawnRole = "quest_target",
                    RequiredForQuest = true
                });

                rules.Add(new RuntimeEncounterPlacementRule
                {
                    Id = $"{profile.Id}_boss",
                    PlacementKind = MapPlacementKind.Boss,
                    SpawnSourceId = spawnSourceId,
                    EncounterId = directEncounterId,
                    PrimaryMonsterId = directMonsterId,
                    SpawnRole = "boss"
                });
            }

            if (!string.IsNullOrWhiteSpace(spawnSourceId) && !string.IsNullOrWhiteSpace(directEncounterId) && !string.IsNullOrWhiteSpace(directMonsterId))
            {
                rules.Add(new RuntimeEncounterPlacementRule
                {
                    Id = $"{profile.Id}_monster",
                    PlacementKind = MapPlacementKind.Monster,
                    SpawnSourceId = spawnSourceId,
                    EncounterId = directEncounterId,
                    PrimaryMonsterId = directMonsterId,
                    SpawnRole = "trash",
                    SpawnEntries = spawnEntries
                });
            }

            return rules;
        }

        private static List<RuntimeSpawnEntry> BuildSpawnEntries(MapProfileAsset profile, string spawnRole)
        {
            var entries = new List<RuntimeSpawnEntry>();
            foreach (var spawnTable in profile.AllowedSpawnTables ?? Array.Empty<SpawnTableAsset>())
            {
                if (spawnTable == null)
                {
                    continue;
                }

                foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
                {
                    var encounterId = ResolveEncounterId(entry);
                    var monsterId = ResolveEncounterMonsterId(entry.Encounter);
                    if (string.IsNullOrWhiteSpace(monsterId))
                    {
                        monsterId = FirstDirectEncounterMonsterId(profile);
                    }

                    if (!string.IsNullOrWhiteSpace(encounterId) && !string.IsNullOrWhiteSpace(monsterId))
                    {
                        entries.Add(new RuntimeSpawnEntry
                        {
                            EncounterId = encounterId,
                            PrimaryMonsterId = monsterId,
                            SpawnSourceId = spawnTable.Id,
                            SpawnRole = spawnRole,
                            Weight = entry.Weight,
                            MinFloor = entry.MinFloor,
                            MaxFloor = entry.MaxFloor,
                            MinDifficulty = entry.MinDifficulty,
                            MaxDifficulty = entry.MaxDifficulty,
                            ThemeTags = MergeTags(spawnTable.RequiredThemeTags, entry.Encounter != null ? entry.Encounter.ThemeTags : Array.Empty<string>()),
                            BiomeTags = MergeTags(spawnTable.RequiredBiomeTags),
                            SpawnRoleTags = MergeTags(spawnTable.RequiredSpawnRoleTags, entry.Encounter != null ? entry.Encounter.SpawnRoleTags : Array.Empty<string>()),
                            AllowedMapTags = MergeTags(entry.Encounter != null ? entry.Encounter.AllowedMapTags : Array.Empty<string>()),
                            CompatibilityTags = MergeTags(entry.Encounter != null ? entry.Encounter.CompatibilityTags : Array.Empty<string>()),
                            RoomRoleConstraints = MergeTags(entry.RoomRoleConstraints)
                        });
                    }
                }

                foreach (var entry in spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>())
                {
                    var monsterId = ResolveMonsterId(entry);
                    if (!string.IsNullOrWhiteSpace(monsterId))
                    {
                        entries.Add(new RuntimeSpawnEntry
                        {
                            EncounterId = GeneratedSinglePrimaryEncounterId(monsterId),
                            PrimaryMonsterId = monsterId,
                            SpawnSourceId = spawnTable.Id,
                            SpawnRole = spawnRole,
                            Weight = entry.Weight,
                            MinFloor = entry.MinFloor,
                            MaxFloor = entry.MaxFloor,
                            ThemeTags = MergeTags(spawnTable.RequiredThemeTags, entry.Monster != null ? entry.Monster.ThemeTags : Array.Empty<string>()),
                            BiomeTags = MergeTags(spawnTable.RequiredBiomeTags, entry.Monster != null ? entry.Monster.BiomeTags : Array.Empty<string>()),
                            SpawnRoleTags = MergeTags(spawnTable.RequiredSpawnRoleTags, entry.Monster != null ? entry.Monster.SpawnRoleTags : Array.Empty<string>()),
                            CompatibilityTags = MergeTags(entry.Monster != null ? entry.Monster.CompatibilityTags : Array.Empty<string>()),
                            RoomRoleConstraints = MergeTags(entry.RoomRoleConstraints)
                        });
                    }
                }
            }

            return entries;
        }

        private static List<string> MergeTags(params string[][] tagSets)
        {
            var tags = new List<string>();
            foreach (var tagSet in tagSets ?? Array.Empty<string[]>())
            {
                foreach (var tag in tagSet ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(tag) && !tags.Contains(tag))
                    {
                        tags.Add(tag);
                    }
                }
            }

            return tags;
        }

        private static string ResolveEncounterId(SpawnEncounterEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            if (entry.Encounter != null && !string.IsNullOrWhiteSpace(entry.Encounter.Id))
            {
                return entry.Encounter.Id;
            }

            return entry.EncounterId;
        }

        private static string ResolveMonsterId(SpawnMonsterEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            if (entry.Monster != null && !string.IsNullOrWhiteSpace(entry.Monster.Id))
            {
                return entry.Monster.Id;
            }

            return entry.MonsterId;
        }

        private static string GeneratedSinglePrimaryEncounterId(string monsterId)
        {
            return $"generated_single_primary_{monsterId}";
        }

        private static string FirstDirectEncounterId(MapProfileAsset profile)
        {
            foreach (var encounter in profile.DirectEncounterOverrides ?? Array.Empty<EncounterDefinitionAsset>())
            {
                if (encounter != null && !string.IsNullOrWhiteSpace(encounter.Id))
                {
                    return encounter.Id;
                }
            }

            foreach (var spawnTable in profile.AllowedSpawnTables ?? Array.Empty<SpawnTableAsset>())
            {
                foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
                {
                    if (entry.Encounter != null && !string.IsNullOrWhiteSpace(entry.Encounter.Id))
                    {
                        return entry.Encounter.Id;
                    }

                    if (!string.IsNullOrWhiteSpace(entry.EncounterId))
                    {
                        return entry.EncounterId;
                    }
                }
            }

            return string.Empty;
        }

        private static string FirstDirectEncounterMonsterId(MapProfileAsset profile)
        {
            foreach (var encounter in profile.DirectEncounterOverrides ?? Array.Empty<EncounterDefinitionAsset>())
            {
                var monsterId = ResolveEncounterMonsterId(encounter);
                if (!string.IsNullOrWhiteSpace(monsterId))
                {
                    return monsterId;
                }
            }

            foreach (var spawnTable in profile.AllowedSpawnTables ?? Array.Empty<SpawnTableAsset>())
            {
                foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
                {
                    var monsterId = ResolveEncounterMonsterId(entry.Encounter);
                    if (!string.IsNullOrWhiteSpace(monsterId))
                    {
                        return monsterId;
                    }
                }

                foreach (var entry in spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>())
                {
                    if (entry.Monster != null && !string.IsNullOrWhiteSpace(entry.Monster.Id))
                    {
                        return entry.Monster.Id;
                    }

                    if (!string.IsNullOrWhiteSpace(entry.MonsterId))
                    {
                        return entry.MonsterId;
                    }
                }
            }

            return string.Empty;
        }

        private static string ResolveEncounterMonsterId(EncounterDefinitionAsset encounter)
        {
            if (encounter == null)
            {
                return string.Empty;
            }

            if (encounter.PrimaryMonster != null && !string.IsNullOrWhiteSpace(encounter.PrimaryMonster.Id))
            {
                return encounter.PrimaryMonster.Id;
            }

            return encounter.PrimaryMonsterId;
        }

        private static string FirstSpawnTableId(MapProfileAsset profile)
        {
            foreach (var spawnTable in profile.AllowedSpawnTables ?? Array.Empty<SpawnTableAsset>())
            {
                if (spawnTable != null && !string.IsNullOrWhiteSpace(spawnTable.Id))
                {
                    return spawnTable.Id;
                }
            }

            return string.Empty;
        }

        private static ChunkPreset ToChunkPreset(RoomChunkAsset asset)
        {
            var roleTags = new List<MapRoomRole>();
            foreach (var roleTag in asset.RoleTags ?? Array.Empty<string>())
            {
                if (Enum.TryParse<MapRoomRole>(roleTag, true, out var role))
                {
                    roleTags.Add(role);
                }
            }

            var anchors = new List<ChunkAnchor>();
            foreach (var anchor in asset.Anchors ?? Array.Empty<AuthoringChunkAnchor>())
            {
                anchors.Add(new ChunkAnchor
                {
                    Id = anchor.Id,
                    Kind = anchor.Kind,
                    X = anchor.Cell.x,
                    Y = anchor.Cell.y
                });
            }

            var cells = new List<RoomChunkCell>();
            foreach (var cell in asset.Cells ?? Array.Empty<RoomChunkCell>())
            {
                if (cell == null)
                {
                    continue;
                }

                cells.Add(new RoomChunkCell
                {
                    X = cell.X,
                    Y = cell.Y,
                    Type = cell.Type,
                    Height = cell.Height,
                    Direction = cell.Direction,
                    MaterialId = cell.MaterialId
                });
            }

            var objects = new List<RoomChunkObjectPlacement>();
            foreach (var placement in asset.Objects ?? Array.Empty<RoomChunkObjectPlacement>())
            {
                if (placement == null)
                {
                    continue;
                }

                objects.Add(new RoomChunkObjectPlacement
                {
                    Id = placement.Id,
                    Kind = placement.Kind,
                    X = placement.X,
                    Y = placement.Y,
                    Height = placement.Height,
                    Direction = placement.Direction,
                    Width = placement.Width,
                    Depth = placement.Depth,
                    BlocksMovement = placement.BlocksMovement,
                    PrefabId = placement.PrefabId,
                    MaterialId = placement.MaterialId
                });
            }

            var socketDefinitions = new List<RoomChunkSocketDefinition>();
            foreach (var socket in ResolveSocketDefinitions(asset))
            {
                if (socket == null)
                {
                    continue;
                }

                socketDefinitions.Add(new RoomChunkSocketDefinition
                {
                    Side = socket.Side,
                    SocketType = socket.SocketType,
                    SocketId = socket.SocketId
                });
            }

            return new ChunkPreset
            {
                Id = asset.Id,
                PresetId = asset.Id,
                Theme = asset.ThemeId,
                Width = asset.Size.x,
                Height = asset.Size.y,
                LayoutKind = asset.LayoutKind,
                CorridorLength = asset.CorridorLength,
                CorridorWidth = asset.CorridorWidth,
                DeadEndDepth = asset.DeadEndDepth,
                OpenSides = asset.OpenSides,
                DoorSockets = asset.DoorSockets,
                SocketDefinitions = socketDefinitions,
                VariantGroup = asset is LandmarkRoomAsset landmark ? landmark.LandmarkRole : string.Empty,
                PopulationAllowed = asset.PopulationAllowed,
                RoleTags = roleTags,
                AuthoringRoleTags = new List<string>(asset.RoleTags ?? Array.Empty<string>()),
                Anchors = anchors,
                Cells = cells,
                Objects = objects
            };
        }

        private static IReadOnlyList<RoomChunkSocketDefinition> ResolveSocketDefinitions(RoomChunkAsset asset)
        {
            if (asset.SocketDefinitions != null && asset.SocketDefinitions.Length > 0)
            {
                return asset.SocketDefinitions;
            }

            var sockets = new List<RoomChunkSocketDefinition>();
            foreach (var side in RoomChunkSocketRules.EnumerateSides(
                MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West))
            {
                var isOpen = (asset.OpenSides & side) != MapDirection.None;
                sockets.Add(new RoomChunkSocketDefinition
                {
                    Side = side,
                    SocketType = !isOpen
                        ? RoomChunkSocketType.Blocked
                        : asset.LayoutKind == RoomChunkLayoutKind.Corridor
                            ? RoomChunkSocketType.Corridor
                            : RoomChunkSocketType.Door,
                    SocketId = !isOpen
                        ? string.Empty
                        : asset.LayoutKind == RoomChunkLayoutKind.Corridor
                            ? "corridor"
                            : "door"
                });
            }

            return sockets;
        }

        private static void EnsureFolderForAsset(string assetPath)
        {
            var slash = assetPath.LastIndexOf('/');
            if (slash <= 0)
            {
                return;
            }

            EnsureFolder(assetPath.Substring(0, slash));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = slash > 0 ? path.Substring(0, slash) : "Assets";
            var folder = slash > 0 ? path.Substring(slash + 1) : path;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
