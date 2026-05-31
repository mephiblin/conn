using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Conn.MapGenV2.Editor
{
    [Overlay(typeof(SceneView), "MapGenV2", true)]
    public sealed class MapGenV2SceneViewOverlay : Overlay
    {
        public const string ShowMockupGridKey = "Conn.MapGenV2.SceneOverlay.ShowMockupGrid";
        public const string ShowRegionIdsKey = "Conn.MapGenV2.SceneOverlay.ShowRegionIds";
        public const string ShowConnectorsKey = "Conn.MapGenV2.SceneOverlay.ShowConnectors";
        public const string ShowSocketsKey = "Conn.MapGenV2.SceneOverlay.ShowSockets";
        public const string ShowBlockedCellsKey = "Conn.MapGenV2.SceneOverlay.ShowBlockedCells";
        public const string ShowPropChannelsKey = "Conn.MapGenV2.SceneOverlay.ShowPropChannels";
        public const string ShowNavGraphKey = "Conn.MapGenV2.SceneOverlay.ShowNavGraph";
        public const string ShowPrefabBoundsKey = "Conn.MapGenV2.SceneOverlay.ShowPrefabBounds";
        public const string ShowDiagnosticsKey = "Conn.MapGenV2.SceneOverlay.ShowDiagnostics";

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement
            {
                style =
                {
                    minWidth = 260,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4
                }
            };

            var draft = ResolveSelectedDraft();
            var materializedRoot = ResolveSelectedRoot();
            var bakedAsset = ResolveBakedAsset(draft);
            var summary = new Label(BuildOverlaySummary(draft, materializedRoot, bakedAsset, CurrentToolMode))
            {
                style = { whiteSpace = WhiteSpace.Normal }
            };
            root.Add(summary);
            root.Add(new Button(() => MapGenV2Window.Open(draft != null ? draft.Profile : null, draft)) { text = "Open Map Generator" });
            root.Add(new Button(() => GenerateSelectedDraft()) { text = "Generate" });
            root.Add(new Button(() => AcceptSelectedDraft()) { text = "Save Draft" });
            root.Add(new Button(() => MaterializeSelectedDraft()) { text = "Materialize" });
            root.Add(new Button(() => ClearSelectedRoot()) { text = "Clear Root" });
            root.Add(new Button(() => FrameSelectedRoot()) { text = "Frame Root" });
            root.Add(BuildToggle("Mockup Grid", ShowMockupGridKey, true));
            root.Add(BuildToggle("Region IDs", ShowRegionIdsKey, true));
            root.Add(BuildToggle("Connectors", ShowConnectorsKey, true));
            root.Add(BuildToggle("Sockets", ShowSocketsKey, true));
            root.Add(BuildToggle("Blocked Cells", ShowBlockedCellsKey, true));
            root.Add(BuildToggle("Prop Channels", ShowPropChannelsKey, true));
            root.Add(BuildToggle("Nav Graph", ShowNavGraphKey, true));
            root.Add(BuildToggle("Prefab Bounds", ShowPrefabBoundsKey, true));
            root.Add(BuildToggle("Diagnostics", ShowDiagnosticsKey, true));
            return root;
        }

        public static string BuildOverlaySummary(
            MapGenMockupDraftAsset draft,
            GameObject materializedRoot,
            MapGenBakedMapAsset bakedAsset,
            string toolMode)
        {
            var draftLabel = draft != null ? draft.name : "(none)";
            var rootLabel = materializedRoot != null ? materializedRoot.name : "(none)";
            return $"Draft {draftLabel}, Root {rootLabel}, State {BuildSceneStateSummary(draft, materializedRoot, bakedAsset)}, Tool {toolMode}, "
                + "Actions Generate/Save/Materialize/Clear/Frame, "
                + "Toggles Grid/Region IDs/Connectors/Sockets/Blocked/Props/Nav/Bounds/Diagnostics";
        }

        public static string BuildSceneStateSummary(
            MapGenMockupDraftAsset draft,
            GameObject materializedRoot,
            MapGenBakedMapAsset bakedAsset)
        {
            if (draft == null)
            {
                return "no selected draft";
            }

            var mockupState = draft.Accepted
                ? draft.IsAcceptedSignatureCurrent ? "saved draft" : "saved draft stale"
                : "unsaved draft";
            var materializedState = materializedRoot == null
                ? "no materialized output"
                : MapGenMockupMaterializer.ValidateExistingOutput(draft, materializedRoot).IsValid
                    ? "materialized output current"
                    : "stale materialized output";
            var bakedState = bakedAsset == null
                ? "no baked runtime output"
                : MapGenRuntimeBakeUtility.ValidateConsistency(draft, bakedAsset).IsValid
                    ? "baked runtime output current"
                    : "stale baked runtime output";
            return $"{mockupState}; {materializedState}; {bakedState}";
        }

        public static string BuildVisibilityToggleSummary()
        {
            return "Scene visibility toggles: mockup grid, region ids, connectors, sockets, blocked cells, prop channels, nav graph, prefab bounds, diagnostics.";
        }

        private static string CurrentToolMode => Tools.current.ToString();

        private static Toggle BuildToggle(string label, string key, bool defaultValue)
        {
            var toggle = new Toggle(label)
            {
                value = EditorPrefs.GetBool(key, defaultValue)
            };
            toggle.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetBool(key, evt.newValue);
                SceneView.RepaintAll();
            });
            return toggle;
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

        private static MapGenBakedMapAsset ResolveBakedAsset(MapGenMockupDraftAsset draft)
        {
            if (draft == null || draft.Profile == null)
            {
                return null;
            }

            var outputSettings = draft.Profile.OutputSettings;
            var folder = string.IsNullOrWhiteSpace(outputSettings.BakedAssetFolder)
                ? MapGenOutputSettings.Defaults().BakedAssetFolder
                : outputSettings.BakedAssetFolder;
            var path = $"{folder}/{draft.Profile.ProfileId}_{draft.Seed}_BakedMap.asset";
            return AssetDatabase.LoadAssetAtPath<MapGenBakedMapAsset>(path);
        }

        private static GameObject ResolveSelectedRoot()
        {
            var active = Selection.activeGameObject;
            if (active == null)
            {
                return null;
            }

            var marker = active.GetComponentInParent<MapGenV2GeneratedMapMarker>();
            return marker != null ? marker.gameObject : null;
        }

        private static void GenerateSelectedDraft()
        {
            var draft = ResolveSelectedDraft();
            if (draft == null)
            {
                return;
            }

            Undo.RecordObject(draft, "Scene Overlay Generate From Seed");
            draft.GenerateFromProfile();
            EditorUtility.SetDirty(draft);
        }

        private static void AcceptSelectedDraft()
        {
            var draft = ResolveSelectedDraft();
            if (draft == null || string.IsNullOrWhiteSpace(draft.LastGeneratedSignature))
            {
                return;
            }

            Undo.RecordObject(draft, "Scene Overlay Save Draft");
            draft.Accept();
            EditorUtility.SetDirty(draft);
        }

        private static void MaterializeSelectedDraft()
        {
            var draft = ResolveSelectedDraft();
            if (draft == null)
            {
                return;
            }

            MapGenMockupMaterializer.Materialize(draft, MapGenV2SceneOutputMode.ReplacePreviousRoot);
        }

        private static void ClearSelectedRoot()
        {
            var root = ResolveSelectedRoot();
            if (root != null)
            {
                MapGenMockupMaterializer.ClearRoot(root);
            }
        }

        private static void FrameSelectedRoot()
        {
            var root = ResolveSelectedRoot();
            if (root == null)
            {
                return;
            }

            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }
    }
}
