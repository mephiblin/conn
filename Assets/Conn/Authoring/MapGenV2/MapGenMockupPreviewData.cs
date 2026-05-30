using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public readonly struct MapGenMockupPreviewSummary
    {
        public readonly int RoomCells;
        public readonly int CorridorCells;
        public readonly int BlockedCells;
        public readonly int ConnectorCells;
        public readonly int ReservedCells;
        public readonly int PropChannelCells;
        public readonly int RegionCount;

        public MapGenMockupPreviewSummary(
            int roomCells,
            int corridorCells,
            int blockedCells,
            int connectorCells,
            int reservedCells,
            int propChannelCells,
            int regionCount)
        {
            RoomCells = roomCells;
            CorridorCells = corridorCells;
            BlockedCells = blockedCells;
            ConnectorCells = connectorCells;
            ReservedCells = reservedCells;
            PropChannelCells = propChannelCells;
            RegionCount = regionCount;
        }
    }

    public readonly struct MapGenMockupPreviewData
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Seed;
        public readonly string ProfileId;
        public readonly string LastGeneratedSignature;
        public readonly string CurrentSignature;
        public readonly string AcceptedSignature;
        public readonly bool Accepted;
        public readonly bool GeneratedSignatureCurrent;
        public readonly bool AcceptedSignatureCurrent;
        public readonly MapGenMockupPreviewSummary Summary;
        private readonly MapGenMockupCell[] cells;

        private MapGenMockupPreviewData(
            int width,
            int height,
            int seed,
            string profileId,
            string lastGeneratedSignature,
            string currentSignature,
            string acceptedSignature,
            bool accepted,
            bool generatedSignatureCurrent,
            bool acceptedSignatureCurrent,
            MapGenMockupPreviewSummary summary,
            MapGenMockupCell[] cells)
        {
            Width = width;
            Height = height;
            Seed = seed;
            ProfileId = profileId;
            LastGeneratedSignature = lastGeneratedSignature;
            CurrentSignature = currentSignature;
            AcceptedSignature = acceptedSignature;
            Accepted = accepted;
            GeneratedSignatureCurrent = generatedSignatureCurrent;
            AcceptedSignatureCurrent = acceptedSignatureCurrent;
            Summary = summary;
            this.cells = cells ?? Array.Empty<MapGenMockupCell>();
        }

        public static MapGenMockupPreviewData FromDraft(MapGenMockupDraftAsset draft)
        {
            if (draft == null)
            {
                return Empty;
            }

            draft.EnsureCellArray();
            var width = draft.Width;
            var height = draft.Height;
            var sourceCells = draft.Cells ?? Array.Empty<MapGenMockupCell>();
            var copiedCells = new MapGenMockupCell[width * height];
            var copyLength = Mathf.Min(copiedCells.Length, sourceCells.Length);
            Array.Copy(sourceCells, copiedCells, copyLength);

            return new MapGenMockupPreviewData(
                width,
                height,
                draft.Seed,
                draft.Profile != null ? draft.Profile.ProfileId : string.Empty,
                draft.LastGeneratedSignature,
                draft.ComputeSignature(),
                draft.AcceptedSignature,
                draft.Accepted,
                draft.IsGeneratedSignatureCurrent,
                draft.IsAcceptedSignatureCurrent,
                BuildSummary(copiedCells),
                copiedCells);
        }

        public static MapGenMockupPreviewData Empty => new MapGenMockupPreviewData(
            0,
            0,
            0,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            false,
            true,
            true,
            new MapGenMockupPreviewSummary(0, 0, 0, 0, 0, 0, 0),
            Array.Empty<MapGenMockupCell>());

        public bool TryGetCell(Vector2Int coord, out MapGenMockupCell cell)
        {
            return TryGetCell(coord.x, coord.y, out cell);
        }

        public bool TryGetCell(int x, int y, out MapGenMockupCell cell)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                cell = MapGenMockupCell.Empty;
                return false;
            }

            var index = (y * Width) + x;
            if (index < 0 || index >= cells.Length)
            {
                cell = MapGenMockupCell.Empty;
                return false;
            }

            cell = cells[index];
            return true;
        }

        private static MapGenMockupPreviewSummary BuildSummary(MapGenMockupCell[] cells)
        {
            var roomCells = 0;
            var corridorCells = 0;
            var blockedCells = 0;
            var connectorCells = 0;
            var reservedCells = 0;
            var propChannelCells = 0;
            var regionIds = new int[cells.Length];
            var regionCount = 0;

            for (var i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                switch (cell.State)
                {
                    case MapGenCellState.Room:
                        roomCells++;
                        break;
                    case MapGenCellState.Corridor:
                    case MapGenCellState.Wall:
                        corridorCells++;
                        break;
                    case MapGenCellState.Blocked:
                        blockedCells++;
                        break;
                    case MapGenCellState.Connector:
                        connectorCells++;
                        break;
                    case MapGenCellState.Reserved:
                        reservedCells++;
                        break;
                }

                if (!string.IsNullOrEmpty(cell.PropChannel))
                {
                    propChannelCells++;
                }

                if (cell.RegionId < 0 || Contains(regionIds, regionCount, cell.RegionId))
                {
                    continue;
                }

                regionIds[regionCount] = cell.RegionId;
                regionCount++;
            }

            return new MapGenMockupPreviewSummary(
                roomCells,
                corridorCells,
                blockedCells,
                connectorCells,
                reservedCells,
                propChannelCells,
                regionCount);
        }

        private static bool Contains(int[] values, int count, int value)
        {
            for (var i = 0; i < count; i++)
            {
                if (values[i] == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
