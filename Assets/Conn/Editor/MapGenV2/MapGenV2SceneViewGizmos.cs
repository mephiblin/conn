using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [InitializeOnLoad]
    public static class MapGenV2SceneViewGizmos
    {
        static MapGenV2SceneViewGizmos()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
            SceneView.duringSceneGui += DuringSceneGui;
        }

        public static string BuildSceneGizmoSummary()
        {
            return "Scene gizmos: generated bounds, cell grid, selected region outline, room id, connector arrows, door/blocker markers, prop channels, stale materialized root, nav/build bounds.";
        }

        private static void DuringSceneGui(SceneView sceneView)
        {
            var draft = ResolveSelectedDraft();
            var root = ResolveSelectedRoot();
            if (draft != null)
            {
                DrawDraftGizmos(draft);
            }

            if (root != null)
            {
                DrawRootGizmos(root, draft);
            }
        }

        private static MapGenMockupDraftAsset ResolveSelectedDraft()
        {
            if (Selection.activeObject is MapGenMockupDraftAsset draft)
            {
                return draft;
            }

            var root = ResolveSelectedRoot();
            var marker = root != null ? root.GetComponent<MapGenV2GeneratedMapMarker>() : null;
            return marker != null ? marker.SourceDraft : null;
        }

        private static GameObject ResolveSelectedRoot()
        {
            var active = Selection.activeGameObject;
            if (active == null)
            {
                return null;
            }

            var rootMarker = active.GetComponentInParent<MapGenV2GeneratedMapMarker>();
            return rootMarker != null ? rootMarker.gameObject : null;
        }

        private static void DrawDraftGizmos(MapGenMockupDraftAsset draft)
        {
            draft.EnsureCellArray();
            var cellSize = draft.Profile != null ? Mathf.Max(0.1f, draft.Profile.CellSize) : 1f;
            var selectedRegion = ResolveSelectedRegion();
            var showGrid = EditorPrefs.GetBool(MapGenV2SceneViewOverlay.ShowMockupGridKey, true);
            var showRegionIds = EditorPrefs.GetBool(MapGenV2SceneViewOverlay.ShowRegionIdsKey, true);
            var showConnectors = EditorPrefs.GetBool(MapGenV2SceneViewOverlay.ShowConnectorsKey, true);
            var showSockets = EditorPrefs.GetBool(MapGenV2SceneViewOverlay.ShowSocketsKey, true);
            var showBlocked = EditorPrefs.GetBool(MapGenV2SceneViewOverlay.ShowBlockedCellsKey, true);
            var showProps = EditorPrefs.GetBool(MapGenV2SceneViewOverlay.ShowPropChannelsKey, true);

            for (var y = 0; y < draft.Height; y++)
            {
                for (var x = 0; x < draft.Width; x++)
                {
                    var index = (y * draft.Width) + x;
                    if (index < 0 || index >= draft.Cells.Length)
                    {
                        continue;
                    }

                    var cell = draft.Cells[index];
                    var center = ToWorld(x, y, cellSize);
                    if (showGrid)
                    {
                        DrawCellRect(center, cellSize, ColorForCell(cell.State));
                    }

                    if (selectedRegion >= 0 && cell.RegionId == selectedRegion)
                    {
                        DrawCellRect(center, cellSize * 1.08f, Color.yellow);
                    }

                    if (showRegionIds && cell.RegionId >= 0 && IsNavigable(cell.State))
                    {
                        Handles.Label(center + Vector3.up * 0.05f, $"R{cell.RegionId}");
                    }

                    if (showConnectors && cell.State == MapGenCellState.Connector)
                    {
                        Handles.color = Color.cyan;
                        Handles.ArrowHandleCap(0, center + Vector3.up * 0.18f, Quaternion.LookRotation(Vector3.forward), cellSize * 0.32f, EventType.Repaint);
                    }

                    if (showSockets && cell.SocketKind != MapGenSocketKind.None)
                    {
                        Handles.Label(center + Vector3.up * 0.25f, $"{cell.SocketKind}:{cell.SocketId}");
                    }

                    if (showBlocked && (cell.State == MapGenCellState.Blocked || cell.State == MapGenCellState.Reserved))
                    {
                        Handles.Label(center + Vector3.up * 0.12f, cell.State == MapGenCellState.Blocked ? "Blocked" : "Reserved");
                    }

                    if (showProps && !string.IsNullOrWhiteSpace(cell.PropChannel))
                    {
                        Handles.Label(center + Vector3.up * 0.18f, $"Prop:{cell.PropChannel}");
                    }
                }
            }
        }

        private static void DrawRootGizmos(GameObject root, MapGenMockupDraftAsset draft)
        {
            var showBounds = EditorPrefs.GetBool(MapGenV2SceneViewOverlay.ShowPrefabBoundsKey, true);
            var showNav = EditorPrefs.GetBool(MapGenV2SceneViewOverlay.ShowNavGraphKey, true);
            var showDiagnostics = EditorPrefs.GetBool(MapGenV2SceneViewOverlay.ShowDiagnosticsKey, true);
            var marker = root.GetComponent<MapGenV2GeneratedMapMarker>();

            if (showBounds && TryGetBounds(root, out var bounds))
            {
                Handles.color = Color.white;
                Handles.DrawWireCube(bounds.center, bounds.size);
            }

            if (showNav)
            {
                foreach (var module in root.GetComponentsInChildren<MapGenV2MaterializedModuleMarker>(true))
                {
                    if (module.ModuleCategory != MapGenModuleCategory.NavigationHelper)
                    {
                        continue;
                    }

                    Handles.color = Color.green;
                    Handles.SphereHandleCap(0, module.transform.position + Vector3.up * 0.12f, Quaternion.identity, 0.18f, EventType.Repaint);
                }
            }

            if (showDiagnostics && marker != null)
            {
                var stale = draft != null && marker.DraftSignature != draft.AcceptedSignature;
                var label = stale ? "STALE MapGenV2 root" : "MapGenV2 root current";
                Handles.color = stale ? Color.yellow : Color.green;
                Handles.Label(root.transform.position + Vector3.up * 1.8f, label);
            }
        }

        private static int ResolveSelectedRegion()
        {
            var selectedMarker = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<MapGenV2MaterializedModuleMarker>()
                : null;
            return selectedMarker != null ? selectedMarker.RegionId : -1;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = new Bounds(root.transform.position, Vector3.one);
                return root != null;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static Vector3 ToWorld(int x, int y, float cellSize)
        {
            return new Vector3(x * cellSize, 0f, y * cellSize);
        }

        private static void DrawCellRect(Vector3 center, float cellSize, Color color)
        {
            var half = cellSize * 0.5f;
            var a = center + new Vector3(-half, 0f, -half);
            var b = center + new Vector3(half, 0f, -half);
            var c = center + new Vector3(half, 0f, half);
            var d = center + new Vector3(-half, 0f, half);
            Handles.color = color;
            Handles.DrawAAPolyLine(2f, a, b, c, d, a);
        }

        private static bool IsNavigable(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }

        private static Color ColorForCell(MapGenCellState state)
        {
            switch (state)
            {
                case MapGenCellState.Room:
                    return Color.red;
                case MapGenCellState.Corridor:
                case MapGenCellState.Connector:
                    return Color.black;
                case MapGenCellState.Blocked:
                case MapGenCellState.Reserved:
                    return Color.gray;
                default:
                    return Color.blue;
            }
        }
    }
}
