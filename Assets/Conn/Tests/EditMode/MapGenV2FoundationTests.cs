using Conn.MapGenV2.Core;
using NUnit.Framework;

namespace Conn.Tests.EditMode
{
    public sealed class MapGenV2FoundationTests
    {
        [Test]
        public void GridCoordConvertsToAndFromIndex()
        {
            var coord = new MapGenGridCoord(2, 3);

            var index = coord.ToIndex(8);
            var restored = MapGenGridCoord.FromIndex(index, 8);

            Assert.That(index, Is.EqualTo(26));
            Assert.That(restored, Is.EqualTo(coord));
        }

        [Test]
        public void RoomShapeValidatorRejectsConnectorAwayFromEdge()
        {
            var cells = new MapGenShapeCell[9];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenShapeCell.Empty;
            }

            cells[4] = new MapGenShapeCell
            {
                State = MapGenCellState.Connector,
                SocketKind = MapGenSocketKind.Door,
                SocketId = "door"
            };

            var report = MapGenRoomShapeValidator.Validate(3, 3, cells);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues, Has.Exactly(1).Matches<MapGenIssue>(
                issue => issue.Code == "room_shape_connector_not_on_edge"));
        }
    }
}
