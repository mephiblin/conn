using Conn.Authoring.Maps;
using Conn.Core.Maps;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class EditableMapDraftSampleAssetBuilder
    {
        public const string SampleDraftAssetPath = "Assets/Conn/Authoring/Maps/Drafts/EditableMapDraft_Sample.asset";

        [MenuItem("Conn/Maps/Create Or Update Sample Editable Draft")]
        public static void CreateOrUpdateSampleDraftAsset()
        {
            EnsureDraftFolder();

            var asset = AssetDatabase.LoadAssetAtPath<EditableMapDraftAsset>(SampleDraftAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
                AssetDatabase.CreateAsset(asset, SampleDraftAssetPath);
            }

            Populate(asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Populate(EditableMapDraftAsset draft)
        {
            draft.Id = "editable_map_sample";
            draft.SourceProfileId = MapGenerationCatalog.ChapterTwoFirstSliceProfileId;
            draft.Seed = 2001;
            draft.Floor = 1;
            draft.Difficulty = 0;
            draft.Version = 1;
            draft.InitializeBlank(8, 5, 1f, 1f);

            for (var y = 1; y <= 3; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    draft.TrySetCell(new EditableMapCell
                    {
                        X = x,
                        Y = y,
                        RoomId = ResolveRoomId(x),
                        ZoneId = "sample_zone",
                        Terrain = RoomChunkCellType.Floor,
                        Height = 0,
                        Direction = MapDirection.North,
                        MaterialId = x < 2 ? "sample_start_floor" : x < 4 ? "sample_quest_floor" : x < 6 ? "sample_boss_floor" : "sample_exit_floor"
                    });
                }
            }

            draft.TrySetCell(new EditableMapCell
            {
                X = 3,
                Y = 2,
                RoomId = "quest",
                ZoneId = "sample_zone",
                Terrain = RoomChunkCellType.Slope,
                Height = 0,
                Direction = MapDirection.East,
                MaterialId = "sample_transition_floor"
            });
            draft.TrySetCell(new EditableMapCell
            {
                X = 4,
                Y = 2,
                RoomId = "boss",
                ZoneId = "sample_zone",
                Terrain = RoomChunkCellType.Floor,
                Height = 1,
                Direction = MapDirection.North,
                MaterialId = "sample_transition_floor"
            });

            draft.Rooms = new[]
            {
                new EditableMapRoom
                {
                    Id = "start",
                    Role = MapRoomRole.Start,
                    LayoutKind = RoomChunkLayoutKind.Room,
                    X = 0,
                    Y = 1,
                    Width = 2,
                    Height = 3,
                    SocketMask = MapDirection.East,
                    ZoneId = "sample_zone"
                },
                new EditableMapRoom
                {
                    Id = "quest",
                    Role = MapRoomRole.QuestTarget,
                    LayoutKind = RoomChunkLayoutKind.Corridor,
                    X = 2,
                    Y = 1,
                    Width = 2,
                    Height = 3,
                    SocketMask = MapDirection.East | MapDirection.West,
                    ZoneId = "sample_zone"
                },
                new EditableMapRoom
                {
                    Id = "boss",
                    Role = MapRoomRole.Boss,
                    LayoutKind = RoomChunkLayoutKind.HeightTransition,
                    X = 4,
                    Y = 1,
                    Width = 2,
                    Height = 3,
                    SocketMask = MapDirection.East | MapDirection.West,
                    ZoneId = "sample_zone"
                },
                new EditableMapRoom
                {
                    Id = "exit",
                    Role = MapRoomRole.Exit,
                    LayoutKind = RoomChunkLayoutKind.Room,
                    X = 6,
                    Y = 1,
                    Width = 2,
                    Height = 3,
                    SocketMask = MapDirection.West,
                    ZoneId = "sample_zone"
                }
            };
            draft.Zones = new[]
            {
                new EditableMapZone
                {
                    Id = "sample_zone",
                    ThemeId = "ruins",
                    IntendedDifficulty = 0,
                    Purpose = "sample_playable"
                }
            };
            draft.Sockets = new[]
            {
                new EditableMapSocket { Id = "start_quest", RoomId = "start", X = 1, Y = 2, Direction = MapDirection.East, TargetRoomId = "quest", Width = 1 },
                new EditableMapSocket { Id = "quest_start", RoomId = "quest", X = 2, Y = 2, Direction = MapDirection.West, TargetRoomId = "start", Width = 1 },
                new EditableMapSocket { Id = "quest_boss", RoomId = "quest", X = 3, Y = 2, Direction = MapDirection.East, TargetRoomId = "boss", Width = 1 },
                new EditableMapSocket { Id = "boss_quest", RoomId = "boss", X = 4, Y = 2, Direction = MapDirection.West, TargetRoomId = "quest", Width = 1 },
                new EditableMapSocket { Id = "boss_exit", RoomId = "boss", X = 5, Y = 2, Direction = MapDirection.East, TargetRoomId = "exit", Width = 1 },
                new EditableMapSocket { Id = "exit_boss", RoomId = "exit", X = 6, Y = 2, Direction = MapDirection.West, TargetRoomId = "boss", Width = 1 }
            };
            draft.Objects = new[]
            {
                new EditableMapObjectPlacement
                {
                    Id = "sample_spawn_hint",
                    PaletteObjectId = "sample_spawn_hint",
                    Kind = RoomChunkObjectKind.SpawnHint,
                    X = 2,
                    Y = 2,
                    Width = 1,
                    Depth = 1,
                    RuntimeReferenceId = "sample_spawn_runtime"
                },
                new EditableMapObjectPlacement
                {
                    Id = "sample_chest",
                    PaletteObjectId = "sample_chest",
                    Kind = RoomChunkObjectKind.Chest,
                    X = 7,
                    Y = 2,
                    Width = 1,
                    Depth = 1,
                    RuntimeReferenceId = "sample_chest_runtime"
                },
                new EditableMapObjectPlacement
                {
                    Id = "sample_torch",
                    PaletteObjectId = "sample_torch",
                    Kind = RoomChunkObjectKind.Torch,
                    X = 0,
                    Y = 2,
                    Width = 1,
                    Depth = 1,
                    RuntimeReferenceId = "sample_torch_runtime"
                }
            };
        }

        private static string ResolveRoomId(int x)
        {
            if (x < 2)
            {
                return "start";
            }

            if (x < 4)
            {
                return "quest";
            }

            if (x < 6)
            {
                return "boss";
            }

            return "exit";
        }

        private static void EnsureDraftFolder()
        {
            if (!AssetDatabase.IsValidFolder(EditableMapDraftAsset.DefaultDraftFolder))
            {
                AssetDatabase.CreateFolder("Assets/Conn/Authoring/Maps", "Drafts");
            }
        }
    }
}
