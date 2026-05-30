using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System.Collections.Generic;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class EditableMapGeneratedResultBuilder
    {
        public static GeneratedEditableMapResult Build(
            MapProfile profile,
            IReadOnlyList<ChunkPreset> chunks,
            int seed,
            int floor,
            int difficulty,
            float cellSize = 1f,
            float heightStep = 1f)
        {
            var draft = EditableCellMapGenerator.Generate(
                profile,
                seed,
                floor,
                difficulty,
                cellSize,
                heightStep);
            draft.hideFlags = HideFlags.HideAndDontSave;

            var report = EditableMapValidationService.Validate(draft);
            CompiledMap compiled = null;
            if (report.Passed)
            {
                compiled = EditableMapBakeService.Bake(draft);
            }

            return new GeneratedEditableMapResult(draft, compiled, report);
        }

        public readonly struct GeneratedEditableMapResult
        {
            public readonly EditableMapDraftAsset Draft;
            public readonly CompiledMap Compiled;
            public readonly MapValidationReport Report;

            public GeneratedEditableMapResult(EditableMapDraftAsset draft, CompiledMap compiled, MapValidationReport report)
            {
                Draft = draft;
                Compiled = compiled;
                Report = report;
            }
        }
    }
}
