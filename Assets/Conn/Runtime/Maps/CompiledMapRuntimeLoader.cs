using System;
using Conn.Core.Maps;
using UnityEngine;

namespace Conn.Runtime.Maps
{
    public static class CompiledMapRuntimeLoader
    {
        public static CompiledMap LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Compiled map json is missing.", nameof(json));
            }

            var compiled = JsonUtility.FromJson<CompiledMap>(json);
            if (compiled == null)
            {
                throw new InvalidOperationException("Compiled map json could not be read.");
            }

            return compiled;
        }

        public static CompiledMap LoadAndValidateFromJson(string json, MapProfile profile)
        {
            var compiled = LoadFromJson(json);
            MapValidationService.ThrowIfFailed(MapValidationService.ValidateCompiled(profile, compiled));
            return compiled;
        }

        public static MapPlacement FindPlacement(CompiledMap compiled, MapPlacementKind kind)
        {
            if (compiled == null)
            {
                throw new ArgumentNullException(nameof(compiled));
            }

            for (var i = 0; i < compiled.Placements.Count; i++)
            {
                if (compiled.Placements[i].Kind == kind)
                {
                    return compiled.Placements[i];
                }
            }

            throw new InvalidOperationException($"Compiled map is missing placement {kind}.");
        }

        public static CompiledEncounterPlacement FindEncounterPlacement(CompiledMap compiled, string mapPlacementId)
        {
            if (compiled == null)
            {
                throw new ArgumentNullException(nameof(compiled));
            }

            for (var i = 0; i < (compiled.EncounterPlacements?.Count ?? 0); i++)
            {
                var placement = compiled.EncounterPlacements[i];
                if (placement.MapPlacementId == mapPlacementId)
                {
                    return placement;
                }
            }

            return null;
        }

        public static CompiledMapObjectPlacement FindObjectPlacement(CompiledMap compiled, string placementId)
        {
            if (compiled == null)
            {
                throw new ArgumentNullException(nameof(compiled));
            }

            for (var i = 0; i < (compiled.Objects?.Count ?? 0); i++)
            {
                var placement = compiled.Objects[i];
                if (placement != null && placement.PlacementId == placementId)
                {
                    return placement;
                }
            }

            return null;
        }
    }
}
