using System;
using System.Collections.Generic;

namespace Conn.Core.Maps
{
    public static class RuntimeMapGenerationService
    {
        public static CompiledMap GenerateCompiled(RuntimeMapGenerationBundle bundle, string profileId, int seed)
        {
            var entry = RequireEntry(bundle, profileId);
            var draft = MapGenerationService.Generate(entry.Profile, entry.Chunks, seed);
            MapValidationService.ThrowIfFailed(MapValidationService.Validate(entry.Profile, draft));
            var compiled = MapGenerationService.Compile(entry.Profile, draft);
            ApplyEncounterPlacements(entry, compiled);
            MapValidationService.ThrowIfFailed(MapValidationService.ValidateCompiled(entry.Profile, compiled));
            return compiled;
        }

        public static void ApplyEncounterPlacements(RuntimeMapProfileEntry entry, CompiledMap compiled)
        {
            if (entry == null || compiled == null || entry.EncounterPlacementRules == null)
            {
                return;
            }

            compiled.EncounterPlacements.Clear();
            foreach (var placement in compiled.Placements)
            {
                var rule = FindRule(entry, placement);
                if (rule == null)
                {
                    continue;
                }

                var spawn = ResolveSpawn(entry, rule, compiled.Seed, placement);
                var encounterId = spawn != null ? spawn.EncounterId : rule.EncounterId;
                var primaryMonsterId = spawn != null ? spawn.PrimaryMonsterId : rule.PrimaryMonsterId;
                var spawnSourceId = spawn != null ? spawn.SpawnSourceId : rule.SpawnSourceId;
                var spawnRole = spawn != null ? spawn.SpawnRole : rule.SpawnRole;
                compiled.EncounterPlacements.Add(new CompiledEncounterPlacement
                {
                    PlacementId = $"{placement.Id}_encounter",
                    MapPlacementId = placement.Id,
                    RoomId = placement.RoomId,
                    EncounterId = encounterId,
                    SpawnSourceId = spawnSourceId,
                    PrimaryMonsterId = primaryMonsterId,
                    SpawnRole = spawnRole,
                    X = placement.X,
                    Y = placement.Y,
                    StateKey = $"compiled_{compiled.MapId}_{placement.Id}",
                    RequiredForQuest = rule.RequiredForQuest
                });
            }
        }

        private static RuntimeEncounterPlacementRule FindRule(RuntimeMapProfileEntry entry, MapPlacement placement)
        {
            foreach (var rule in entry.EncounterPlacementRules)
            {
                if (rule != null && rule.PlacementKind == placement.Kind)
                {
                    return rule;
                }
            }

            return null;
        }

        private static RuntimeSpawnEntry ResolveSpawn(RuntimeMapProfileEntry profileEntry, RuntimeEncounterPlacementRule rule, int seed, MapPlacement placement)
        {
            if (rule.SpawnEntries == null || rule.SpawnEntries.Count == 0)
            {
                return null;
            }

            var totalWeight = 0;
            foreach (var entry in rule.SpawnEntries)
            {
                if (entry != null && entry.Weight > 0 && IsEntryAllowed(profileEntry, rule, entry))
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            var roll = PositiveHash(seed, placement.Id, rule.Id) % totalWeight;
            foreach (var entry in rule.SpawnEntries)
            {
                if (entry == null || entry.Weight <= 0 || !IsEntryAllowed(profileEntry, rule, entry))
                {
                    continue;
                }

                if (roll < entry.Weight)
                {
                    return entry;
                }

                roll -= entry.Weight;
            }

            return null;
        }

        private static bool IsEntryAllowed(RuntimeMapProfileEntry profileEntry, RuntimeEncounterPlacementRule rule, RuntimeSpawnEntry entry)
        {
            var floor = profileEntry.Floor <= 0 ? 1 : profileEntry.Floor;
            var difficulty = profileEntry.Difficulty;
            return entry.MinFloor <= floor
                && entry.MaxFloor >= floor
                && entry.MinDifficulty <= difficulty
                && (entry.MaxDifficulty <= 0 || entry.MaxDifficulty >= difficulty)
                && TagsAllowProfile(profileEntry.Profile, entry)
                && TagsAllowRole(rule, entry)
                && TagsAllowMapKind(profileEntry.Profile, entry)
                && RoomRoleAllows(rule, entry);
        }

        private static bool TagsAllowProfile(MapProfile profile, RuntimeSpawnEntry entry)
        {
            if (profile == null)
            {
                return false;
            }

            return TagListAllows(entry.ThemeTags, profile.Theme)
                || TagListAllows(entry.CompatibilityTags, profile.Theme);
        }

        private static bool TagsAllowRole(RuntimeEncounterPlacementRule rule, RuntimeSpawnEntry entry)
        {
            return string.IsNullOrWhiteSpace(rule.SpawnRole)
                || TagListAllows(entry.SpawnRoleTags, rule.SpawnRole)
                || string.Equals(entry.SpawnRole, rule.SpawnRole, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TagsAllowMapKind(MapProfile profile, RuntimeSpawnEntry entry)
        {
            return entry.AllowedMapTags == null
                || entry.AllowedMapTags.Count == 0
                || TagListAllows(entry.AllowedMapTags, profile?.MapKind)
                || TagListAllows(entry.CompatibilityTags, profile?.MapKind);
        }

        private static bool RoomRoleAllows(RuntimeEncounterPlacementRule rule, RuntimeSpawnEntry entry)
        {
            return entry.RoomRoleConstraints == null
                || entry.RoomRoleConstraints.Count == 0
                || TagListAllows(entry.RoomRoleConstraints, rule.RoomRole);
        }

        private static bool TagListAllows(List<string> tags, string value)
        {
            if (tags == null || tags.Count == 0 || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (string.Equals(tag, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int PositiveHash(int seed, string placementId, string ruleId)
        {
            unchecked
            {
                var hash = seed;
                hash = (hash * 397) ^ StableHash(placementId);
                hash = (hash * 397) ^ StableHash(ruleId);
                return hash == int.MinValue ? 0 : Math.Abs(hash);
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < (value?.Length ?? 0); i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private static RuntimeMapProfileEntry RequireEntry(RuntimeMapGenerationBundle bundle, string profileId)
        {
            if (bundle == null)
            {
                throw new ArgumentNullException(nameof(bundle));
            }

            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException("Profile id must not be empty.", nameof(profileId));
            }

            var entry = bundle.FindProfile(profileId);
            if (entry == null)
            {
                throw new InvalidOperationException($"Runtime map generation bundle does not contain profile: {profileId}");
            }

            if (entry.Profile == null)
            {
                throw new InvalidOperationException($"Runtime map generation profile entry is missing profile data: {profileId}");
            }

            if (entry.Chunks == null || entry.Chunks.Count == 0)
            {
                throw new InvalidOperationException($"Runtime map generation profile has no chunks: {profileId}");
            }

            return entry;
        }
    }
}
