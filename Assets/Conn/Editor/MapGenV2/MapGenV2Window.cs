using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public enum MapGenV2EditorLanguage
    {
        Auto,
        Korean,
        English
    }

    public readonly struct MapGenV2WindowStateSnapshot
    {
        public readonly string ProfilePath;
        public readonly string DraftPath;
        public readonly float PreviewCellSize;
        public readonly Vector2 PreviewScroll;
        public readonly bool HasSelectedCell;
        public readonly Vector2Int SelectedCell;
        public readonly int SelectedRegionId;
        public readonly bool ShowPropOverlay;
        public readonly bool ShowPostProcessOverlay;
        public readonly MapGenV2SceneOutputMode OutputMode;
        public readonly bool DiagnosticsFoldout;
        public readonly MapGenV2EditorLanguage Language;

        public MapGenV2WindowStateSnapshot(
            string profilePath,
            string draftPath,
            float previewCellSize,
            Vector2 previewScroll,
            bool hasSelectedCell,
            Vector2Int selectedCell,
            int selectedRegionId,
            bool showPropOverlay,
            bool showPostProcessOverlay,
            MapGenV2SceneOutputMode outputMode,
            bool diagnosticsFoldout,
            MapGenV2EditorLanguage language)
        {
            ProfilePath = profilePath ?? string.Empty;
            DraftPath = draftPath ?? string.Empty;
            PreviewCellSize = previewCellSize;
            PreviewScroll = previewScroll;
            HasSelectedCell = hasSelectedCell;
            SelectedCell = selectedCell;
            SelectedRegionId = selectedRegionId;
            ShowPropOverlay = showPropOverlay;
            ShowPostProcessOverlay = showPostProcessOverlay;
            OutputMode = outputMode;
            DiagnosticsFoldout = diagnosticsFoldout;
            Language = language;
        }
    }

    public static class MapGenV2EditorText
    {
        private const string LanguagePreferenceKey = "Conn.MapGenV2.Editor.Language";

        public static MapGenV2EditorLanguage LanguagePreference
        {
            get => (MapGenV2EditorLanguage)EditorPrefs.GetInt(LanguagePreferenceKey, (int)MapGenV2EditorLanguage.Auto);
            set => EditorPrefs.SetInt(LanguagePreferenceKey, (int)value);
        }

        public static MapGenV2EditorLanguage ActiveLanguage
        {
            get
            {
                var preference = LanguagePreference;
                if (preference != MapGenV2EditorLanguage.Auto)
                {
                    return preference;
                }

                return Application.systemLanguage == SystemLanguage.Korean
                    ? MapGenV2EditorLanguage.Korean
                    : MapGenV2EditorLanguage.English;
            }
        }

        public static string Get(string key)
        {
            var korean = ActiveLanguage == MapGenV2EditorLanguage.Korean;
            switch (key)
            {
                case "mapgenv2.language":
                    return korean ? "언어" : "Language";
                case "mapgenv2.createStarterSetup":
                    return korean ? "스타터 설정 생성" : "Create Starter Setup";
                case "mapgenv2.createDefaultFolders":
                    return korean ? "기본 폴더 생성" : "Create Default Folders";
                case "mapgenv2.createDraft":
                    return korean ? "드래프트 생성" : "Create Draft";
                case "mapgenv2.generateMockup":
                    return korean ? "목업 생성" : "Generate Mockup";
                case "mapgenv2.regenerateMockup":
                    return korean ? "목업 재생성" : "Regenerate Mockup";
                case "mapgenv2.acceptMockup":
                    return korean ? "목업 수락" : "Accept Mockup";
                case "mapgenv2.reacceptMockup":
                    return korean ? "목업 재수락" : "Reaccept Mockup";
                case "mapgenv2.materializeToScene":
                    return korean ? "씬 생성" : "Materialize To Scene";
                case "mapgenv2.rematerializeToScene":
                    return korean ? "씬 재생성" : "Rematerialize To Scene";
                case "mapgenv2.bakeRuntimeAsset":
                    return korean ? "런타임 베이크" : "Bake Runtime Asset";
                case "mapgenv2.rebakeRuntimeAsset":
                    return korean ? "런타임 재베이크" : "Rebake Runtime Asset";
                case "mapgenv2.profileMissing":
                    return korean ? "프로필을 지정하세요." : "Assign a profile.";
                case "mapgenv2.draftStale":
                    return korean ? "소스 변경됨: 재생성 필요" : "Stale: regenerate required";
                default:
                    return key;
            }
        }

        public static string PseudoLocalize(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            return $"［{source} 가나다 {source}］";
        }

        public static bool IsLongTextSafe(string source, int maxCharacters)
        {
            if (string.IsNullOrEmpty(source))
            {
                return true;
            }

            return PseudoLocalize(source).Length <= maxCharacters;
        }
    }

    public sealed class MapGenV2Window : EditorWindow
    {
        public static readonly Vector2 MinimumWindowSize = new Vector2(860f, 620f);
        private const string ProfilePathKey = "Conn.MapGenV2.Window.ProfilePath";
        private const string DraftPathKey = "Conn.MapGenV2.Window.DraftPath";
        private const string PreviewCellSizeKey = "Conn.MapGenV2.Window.PreviewCellSize";
        private const string OutputModeKey = "Conn.MapGenV2.Window.OutputMode";
        private const string ShowPropOverlayKey = "Conn.MapGenV2.Window.ShowPropOverlay";
        private const string ShowPostProcessOverlayKey = "Conn.MapGenV2.Window.ShowPostProcessOverlay";
        private const string PreviewScrollXKey = "Conn.MapGenV2.Window.PreviewScrollX";
        private const string PreviewScrollYKey = "Conn.MapGenV2.Window.PreviewScrollY";
        private const string HasSelectedCellKey = "Conn.MapGenV2.Window.HasSelectedCell";
        private const string SelectedCellXKey = "Conn.MapGenV2.Window.SelectedCellX";
        private const string SelectedCellYKey = "Conn.MapGenV2.Window.SelectedCellY";
        private const string SelectedRegionIdKey = "Conn.MapGenV2.Window.SelectedRegionId";
        private const string DiagnosticsFoldoutKey = "Conn.MapGenV2.Window.DiagnosticsFoldout";
        private static readonly string[] LanguageOptions = { "Auto", "한국어", "English" };
        private static readonly Color EmptyColor = new Color(0.04f, 0.08f, 0.9f, 1f);
        private static readonly Color RoomColor = new Color(0.9f, 0f, 0f, 1f);
        private static readonly Color CorridorColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color BlockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color ConnectorColor = CorridorColor;
        private static readonly Color ReservedColor = BlockedColor;
        private static readonly Color GridLineColor = new Color(0f, 0f, 0f, 0.28f);
        private static readonly Color SelectedRegionColor = new Color(1f, 1f, 1f, 0.38f);
        private static readonly Color SelectedConnectorColor = new Color(1f, 0.72f, 0.05f, 0.58f);
        private static readonly Color AdjacentLinkColor = new Color(0.1f, 0.95f, 1f, 0.48f);
        private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color PropOverlayColor = new Color(0.1f, 1f, 0.35f, 0.9f);
        private static readonly Color BlockerPropOverlayColor = new Color(1f, 0.45f, 0.05f, 0.9f);
        private static readonly Color ObjectivePropOverlayColor = new Color(1f, 0.95f, 0.05f, 0.9f);
        private MapGenProfileAsset profile;
        private MapGenMockupDraftAsset draft;
        private Vector2 scroll;
        private Vector2 previewScroll;
        private Vector2Int hoveredCell;
        private Vector2Int selectedCell;
        private bool hasHoveredCell;
        private bool hasSelectedCell;
        private int selectedRegionId = -1;
        private float previewCellSize = 18f;
        private GameObject selectedMaterializedRoot;
        private MapGenV2SceneOutputMode outputMode = MapGenV2SceneOutputMode.ReplacePreviousRoot;
        private bool showPropPlacementOverlay = true;
        private bool showPostProcessOverlay = true;
        private bool showDiagnosticsFoldout = true;
        private int selectedPostProcessPassIndex;
        private MapGenPostProcessReport lastPostProcessReport;
        private string lastOperationResult = "아직 실행한 작업이 없습니다. / No operation has run yet.";
        private readonly MapGenV2PreviewTextureCache previewTextureCache = new MapGenV2PreviewTextureCache();

        [MenuItem("Conn/MapGenV2/Map Generator")]
        public static void Open()
        {
            GetWindow<MapGenV2Window>("MapGenV2");
        }

        public static void Open(MapGenProfileAsset selectedProfile, MapGenMockupDraftAsset selectedDraft)
        {
            var window = GetWindow<MapGenV2Window>("MapGenV2");
            window.profile = selectedProfile;
            window.draft = selectedDraft;
            window.Repaint();
        }

        private void OnEnable()
        {
            minSize = MinimumWindowSize;
            previewCellSize = EditorPrefs.GetFloat(PreviewCellSizeKey, previewCellSize);
            outputMode = (MapGenV2SceneOutputMode)EditorPrefs.GetInt(OutputModeKey, (int)outputMode);
            showPropPlacementOverlay = EditorPrefs.GetBool(ShowPropOverlayKey, showPropPlacementOverlay);
            showPostProcessOverlay = EditorPrefs.GetBool(ShowPostProcessOverlayKey, showPostProcessOverlay);
            previewScroll = new Vector2(
                EditorPrefs.GetFloat(PreviewScrollXKey, previewScroll.x),
                EditorPrefs.GetFloat(PreviewScrollYKey, previewScroll.y));
            hasSelectedCell = EditorPrefs.GetBool(HasSelectedCellKey, hasSelectedCell);
            selectedCell = new Vector2Int(
                EditorPrefs.GetInt(SelectedCellXKey, selectedCell.x),
                EditorPrefs.GetInt(SelectedCellYKey, selectedCell.y));
            selectedRegionId = EditorPrefs.GetInt(SelectedRegionIdKey, selectedRegionId);
            showDiagnosticsFoldout = EditorPrefs.GetBool(DiagnosticsFoldoutKey, showDiagnosticsFoldout);
            profile = LoadAssetFromEditorPrefs<MapGenProfileAsset>(ProfilePathKey);
            draft = LoadAssetFromEditorPrefs<MapGenMockupDraftAsset>(DraftPathKey);
        }

        private void OnDisable()
        {
            SaveWindowState();
            previewTextureCache.Dispose();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("MapGenV2", EditorStyles.boldLabel);
            DrawLanguageSelector();
            var workflow = MapGenV2WorkflowStatus.From(profile, draft);
            DrawWorkflowStatus(workflow);
            workflow = MapGenV2WorkflowStatus.From(profile, draft);
            DrawThreePaneWorkspace(workflow);
            EditorGUILayout.EndScrollView();
        }

        public static string BuildThreePaneLayoutSummary()
        {
            return "Three-pane authoring layout: left setup/assets/output, center mockup actions and visual preview, right next action, validation, diagnostics, and scene output details.";
        }

        private void DrawThreePaneWorkspace(MapGenV2WorkflowStatus workflow)
        {
            DrawWrappingHelpBox(BuildThreePaneLayoutSummary(), MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(260f), GUILayout.MaxWidth(340f)))
                {
                    EditorGUILayout.LabelField("설정/에셋 / Setup & Assets", EditorStyles.boldLabel);
                    DrawReferenceFields();
                    DrawSetupActions();
                    DrawOutputPaths();
                    DrawLinkedAssetShortcuts();
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(360f), GUILayout.ExpandWidth(true)))
                {
                    EditorGUILayout.LabelField("목업 제작 / Mockup Authoring", EditorStyles.boldLabel);
                    DrawDraftActions(workflow);
                    DrawDraftSummaryAndPreview();
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(280f), GUILayout.MaxWidth(380f)))
                {
                    EditorGUILayout.LabelField("상세/진단 / Details & Diagnostics", EditorStyles.boldLabel);
                    DrawNextAction(workflow);
                    DrawProfileValidation();
                    DrawDiagnosticsPanel();
                    DrawSceneOutputControls(workflow);
                }
            }
        }

        private void DrawSetupActions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("빠른 설정 / Quick Setup", EditorStyles.boldLabel);
                if (GUILayout.Button(MapGenV2EditorText.Get("mapgenv2.createStarterSetup")))
                {
                    var setup = MapGenV2StarterSetupBuilder.CreateStarterProfileSetup();
                    profile = setup.Profile;
                    draft = setup.Draft;
                    Selection.activeObject = draft != null ? draft : profile;
                    SaveWindowState();
                    SetLastOperationResult("Starter setup created.");
                }

                if (GUILayout.Button(MapGenV2EditorText.Get("mapgenv2.createDefaultFolders")))
                {
                    MapGenV2AssetFolderUtility.CreateDefaultFolders();
                    SetLastOperationResult("Default MapGenV2 folders created or already existed.");
                }

                if (GUILayout.Button(MapGenV2EditorText.Get("mapgenv2.createDraft")))
                {
                    CreateDraft();
                    SaveWindowState();
                }

                if (GUILayout.Button("Cleanup Starter Generated Assets"))
                {
                    var deleted = MapGenV2StarterSetupBuilder.CleanupStarterGeneratedAssets();
                    SetLastOperationResult($"Starter generated asset cleanup deleted {deleted} assets.");
                }
            }
        }

        private void DrawWorkflowStatus(MapGenV2WorkflowStatus workflow)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("워크플로우 상태 / Workflow Status", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawStepBadge("Setup", workflow.HasProfile && workflow.ProfileValid && workflow.HasDraft, !workflow.HasProfile || !workflow.ProfileValid || !workflow.HasDraft);
                    DrawStepBadge("Generate", workflow.HasGeneratedMockup, workflow.CanGenerate && !workflow.HasGeneratedMockup);
                    DrawStepBadge("Post-Process", workflow.HasGeneratedMockup, workflow.CanPostProcess && !workflow.Accepted);
                    DrawStepBadge("Accept", workflow.Accepted && workflow.AcceptedCurrent, workflow.CanAccept && (!workflow.Accepted || !workflow.AcceptedCurrent));
                    DrawStepBadge("Materialize", ResolveMaterializedRoot() != null, workflow.CanMaterialize);
                    DrawStepBadge("Bake", LoadExpectedBakedAsset() != null, workflow.CanBakeRuntime);
                }

                var profileName = profile != null ? profile.ProfileId : "(none)";
                var draftName = draft != null ? draft.name : "(none)";
                var seed = draft != null ? draft.Seed : (int?)null;
                var validationState = workflow.HasProfile ? (workflow.ProfileValid ? "Valid" : "Invalid") : "(none)";
                var acceptedState = workflow.Accepted
                    ? workflow.AcceptedCurrent ? "Accepted current" : "Accepted stale"
                    : "Not accepted";
                var materializedRoot = ResolveMaterializedRoot();
                var bakedAsset = LoadExpectedBakedAsset();
                EditorGUILayout.LabelField(
                    "현재 / Current",
                    BuildTopStatusStripText(
                        profileName,
                        draftName,
                        seed,
                        validationState,
                        acceptedState,
                        materializedRoot != null ? materializedRoot.name : "(none)",
                        bakedAsset != null ? AssetDatabase.GetAssetPath(bakedAsset) : "(none)"));
                EditorGUILayout.LabelField("마지막 작업 / Last Result", lastOperationResult);
            }
        }

        public static string BuildTopStatusStripText(
            string profileName,
            string draftName,
            int? seed,
            string validationState,
            string acceptedState,
            string materializedRootName,
            string bakedAssetPath)
        {
            return $"Profile {NullLabel(profileName)}, Draft {NullLabel(draftName)}, Seed {(seed.HasValue ? seed.Value.ToString() : "-")}, "
                + $"Validation {NullLabel(validationState)}, Accepted {NullLabel(acceptedState)}, "
                + $"Materialized Root {NullLabel(materializedRootName)}, Baked Asset {NullLabel(bakedAssetPath)}";
        }

        public static string AppendNextStep(string result, string nextAction)
        {
            if (string.IsNullOrWhiteSpace(nextAction))
            {
                return result ?? string.Empty;
            }

            return $"{result ?? string.Empty} Next: {nextAction}";
        }

        public static string BuildPrimaryMockupUxSummary()
        {
            return "Primary mockup UX: MapGenV2 window supports Generate, Post-Process, Accept, selected-region inspect/edit, "
                + "prop/post overlays, Materialize, Bake, and clear/frame/select output without requiring Scene View. "
                + "Scene View remains a secondary inspection/materialization surface.";
        }

        public static string BuildPersistedWindowStateSummary(MapGenV2WindowStateSnapshot state)
        {
            return $"Persisted state: profile {NullLabel(state.ProfilePath)}, draft {NullLabel(state.DraftPath)}, "
                + $"preview zoom {state.PreviewCellSize:0.##}, pan {state.PreviewScroll.x:0.##},{state.PreviewScroll.y:0.##}, "
                + $"selected cell {(state.HasSelectedCell ? $"{state.SelectedCell.x},{state.SelectedCell.y}" : "(none)")}, "
                + $"selected region {state.SelectedRegionId}, prop overlay {state.ShowPropOverlay}, post overlay {state.ShowPostProcessOverlay}, "
                + $"output mode {state.OutputMode}, diagnostics foldout {state.DiagnosticsFoldout}, language {state.Language}";
        }

        public static string BuildPrimaryActionExplanation(
            string actionName,
            bool enabled,
            string reason,
            string change,
            string outputLocation)
        {
            return $"{actionName}: {(enabled ? "Enabled" : "Disabled")} because {NullLabel(reason)} "
                + $"Change: {NullLabel(change)} Output: {NullLabel(outputLocation)}";
        }

        private static string NullLabel(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
        }

        private void SetLastOperationResult(string result)
        {
            lastOperationResult = AppendNextStep(result, BuildGuidedNextAction(MapGenV2WorkflowStatus.From(profile, draft)));
        }

        private static void DrawStepBadge(string label, bool complete, bool active)
        {
            var previousColor = GUI.backgroundColor;
            var status = complete
                ? MapGenV2StatusKind.Accepted
                : active ? MapGenV2StatusKind.Stale : MapGenV2StatusKind.Pending;
            GUI.backgroundColor = MapGenV2StatusPresentation.For(status).Color;
            GUILayout.Label(label, EditorStyles.miniButton, GUILayout.MinWidth(92f));
            GUI.backgroundColor = previousColor;
        }

        private void DrawLanguageSelector()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var preference = MapGenV2EditorText.LanguagePreference;
                EditorGUI.BeginChangeCheck();
                var selected = EditorGUILayout.Popup(
                    MapGenV2EditorText.Get("mapgenv2.language"),
                    (int)preference,
                    LanguageOptions,
                    GUILayout.Width(220f));
                if (EditorGUI.EndChangeCheck())
                {
                    MapGenV2EditorText.LanguagePreference = (MapGenV2EditorLanguage)selected;
                    Repaint();
                }
            }
        }

        private void DrawReferenceFields()
        {
            EditorGUI.BeginChangeCheck();
            profile = (MapGenProfileAsset)EditorGUILayout.ObjectField("프로필 / Profile", profile, typeof(MapGenProfileAsset), false);
            draft = (MapGenMockupDraftAsset)EditorGUILayout.ObjectField("드래프트 / Draft", draft, typeof(MapGenMockupDraftAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                SaveWindowState();
            }
        }

        private static void DrawWrappingHelpBox(string message, MessageType messageType)
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                richText = true
            };
            var content = EditorGUIUtility.TrTextContent(message);
            var height = Mathf.Max(38f, style.CalcHeight(content, Mathf.Max(320f, EditorGUIUtility.currentViewWidth - 36f)));
            EditorGUILayout.LabelField(content, style, GUILayout.MinHeight(height));
        }

        private void DrawNextAction(MapGenV2WorkflowStatus workflow)
        {
            DrawWrappingHelpBox($"다음 작업 / Next Action: {BuildGuidedNextAction(workflow)}", MessageType.Info);
        }

        private void DrawProfileValidation()
        {
            if (profile == null)
            {
                EditorGUILayout.HelpBox(MapGenV2EditorText.Get("mapgenv2.profileMissing"), MessageType.Info);
                return;
            }

            MapGenValidationReportEditorGUI.Draw(profile.Validate(), profile, "Profile is valid.");
        }

        private void DrawDiagnosticsPanel()
        {
            EditorGUILayout.Space(4f);
            EditorGUI.BeginChangeCheck();
            showDiagnosticsFoldout = EditorGUILayout.Foldout(showDiagnosticsFoldout, "진단 / Diagnostics", true);
            if (EditorGUI.EndChangeCheck())
            {
                SaveWindowState();
            }

            if (!showDiagnosticsFoldout)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var report = BuildDiagnosticsReport();
                EditorGUILayout.LabelField(
                    "Summary",
                    $"Fatal {report.FatalCount}, Error {report.ErrorCount}, Warning {report.WarningCount}, Info {report.InfoCount}");
                MapGenValidationReportEditorGUI.Draw(report, "No diagnostics for the current MapGenV2 selection.");
            }
        }

        private MapGenAuthoringValidationReport BuildDiagnosticsReport()
        {
            var report = new MapGenAuthoringValidationReport();
            if (profile != null)
            {
                report.AddRange(profile.Validate(), profile);
                report.AddRange(MapGenProfileGraphValidator.Validate(profile), profile);
            }

            if (draft != null && !string.IsNullOrWhiteSpace(draft.LastGeneratedSignature))
            {
                report.AddRange(MapGenMockupFeasibilityValidator.Validate(draft.Width, draft.Height, draft.Cells), draft);
                report.AddRange(MapGenPropPlacementValidator.Validate(draft.Width, draft.Height, draft.Cells), draft);
                if (draft.Profile != null && draft.Profile.LayoutRules != null)
                {
                    var postProcessRules = draft.Profile.LayoutRules.PostProcessRules;
                    report.AddRange(
                        MapGenMockupPostProcessor.ValidateSafety(
                            draft.Width,
                            draft.Height,
                            draft.Cells,
                            new MapGenPostProcessOptions
                            {
                                UseDirectRoutes = postProcessRules.UseDirectRoutes,
                                ReduceDeadEnds = postProcessRules.ReduceDeadEnds,
                                RemoveSmallRooms = postProcessRules.RemoveSmallRooms,
                                FillEnclosedEmptySpace = postProcessRules.FillEnclosedEmptySpace,
                                MaxPasses = postProcessRules.MaxPasses
                            }),
                        draft);
                }
            }

            if (draft != null
                && draft.Accepted
                && draft.IsAcceptedSignatureCurrent
                && draft.Profile != null
                && draft.Profile.StyleSet != null
                && draft.Profile.StyleSet.ModuleSet != null)
            {
                var materializationPlan = MapGenMockupMaterializer.BuildPlan(draft);
                var materializationReport = MapGenMockupMaterializer.BuildReport(
                    draft.Profile.StyleSet.ModuleSet,
                    materializationPlan,
                    draft.Seed);
                report.AddRange(MapGenMockupMaterializer.ValidateCoverage(materializationReport), draft.Profile.StyleSet.ModuleSet);
            }

            if (selectedMaterializedRoot != null)
            {
                report.AddRange(MapGenMockupMaterializer.ValidateExistingOutput(draft, selectedMaterializedRoot), selectedMaterializedRoot);
            }
            else
            {
                var materializedMarker = MapGenMockupMaterializer.FindExistingMarker(draft);
                if (materializedMarker != null)
                {
                    report.AddRange(MapGenMockupMaterializer.ValidateExistingOutput(draft, materializedMarker.gameObject), materializedMarker);
                }
            }

            var bakedAsset = LoadExpectedBakedAsset();
            if (bakedAsset != null)
            {
                report.AddRange(MapGenRuntimeBakeUtility.ValidateConsistency(draft, bakedAsset), bakedAsset);
            }

            return report;
        }

        private void DrawOutputPaths()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("출력 경로 / Output Paths", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Draft Folder", GetDraftFolder());
                EditorGUILayout.LabelField("Selected Draft", draft != null ? AssetDatabase.GetAssetPath(draft) : "(none)");
                EditorGUILayout.LabelField("Materialized Prefab Folder", GetMaterializedPrefabFolder());
                EditorGUILayout.LabelField("Baked Asset", BuildExpectedBakedAssetPath());
            }
        }

        private void DrawLinkedAssetShortcuts()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("연결 에셋 / Linked Assets", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                EditorGUILayout.LabelField(BuildLinkedAssetShortcutSummary(), style);
                DrawObjectShortcutRow("Profile", profile, CreateStarterSetupFromShortcut);
                DrawObjectShortcutRow("Draft", draft, profile != null ? CreateDraft : null);
                DrawObjectShortcutRow("Materialized Root", selectedMaterializedRoot);
                DrawObjectShortcutRow("Baked Asset", LoadExpectedBakedAsset());

                if (profile == null)
                {
                    return;
                }

                DrawObjectShortcutRow("Rule Set", profile.LayoutRules);
                DrawObjectShortcutRow("Style Set", profile.StyleSet);
                DrawObjectShortcutRow("Module Set", profile.StyleSet != null ? profile.StyleSet.ModuleSet : null);

                var roomShapes = profile.RoomShapes ?? System.Array.Empty<MapGenRoomShapeAsset>();
                for (var i = 0; i < roomShapes.Length; i++)
                {
                    DrawObjectShortcutRow($"Room Shape {i}", roomShapes[i]);
                }

                if (profile.StyleSet != null)
                {
                    var roomTemplates = profile.StyleSet.RoomTemplates ?? System.Array.Empty<MapGenRoomTemplateAsset>();
                    for (var i = 0; i < roomTemplates.Length; i++)
                    {
                        DrawObjectShortcutRow($"Room Template {i}", roomTemplates[i]);
                    }

                    var corridorTemplates = profile.StyleSet.CorridorTemplates ?? System.Array.Empty<MapGenCorridorTemplateAsset>();
                    for (var i = 0; i < corridorTemplates.Length; i++)
                    {
                        DrawObjectShortcutRow($"Corridor Template {i}", corridorTemplates[i]);
                    }
                }
            }
        }

        public static string BuildLinkedAssetShortcutSummary()
        {
            return "연결 에셋 작업: Ping, Select, Open, Create, Duplicate, Validate, Fix/Create Missing. "
                + "중요 참조 옆에서 즉시 찾기, 열기, 복제, 검증, 누락 생성 흐름을 실행합니다.";
        }

        public static string BuildUndoCoverageSummary()
        {
            return "Undo/Redo coverage: serialized inspector asset edits, room-shape paint/resize/rotate/flip, "
                + "draft generate/post-process/accept/clear, selected-region category/lock/regenerate/state edits, "
                + "profile output settings, and scene materialization create/update/clear via Undo object creation/destruction.";
        }

        private void DrawObjectShortcutRow(string label, Object target, System.Action createAction = null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(label, target, typeof(Object), false);
                using (new EditorGUI.DisabledScope(createAction == null || target != null))
                {
                    if (GUILayout.Button(new GUIContent("Create", "누락된 기본 에셋을 생성합니다."), GUILayout.Width(58f)))
                    {
                        createAction();
                    }
                }

                using (new EditorGUI.DisabledScope(target == null))
                {
                    if (GUILayout.Button(new GUIContent("Ping", "Project 또는 Hierarchy에서 에셋 위치를 표시합니다."), GUILayout.Width(52f)))
                    {
                        EditorGUIUtility.PingObject(target);
                    }

                    if (GUILayout.Button(new GUIContent("Select", "현재 Selection으로 지정합니다."), GUILayout.Width(58f)))
                    {
                        Selection.activeObject = target;
                    }

                    if (GUILayout.Button(new GUIContent("Open", "Inspector 또는 에셋 편집기를 엽니다."), GUILayout.Width(52f)))
                    {
                        AssetDatabase.OpenAsset(target);
                    }

                    using (new EditorGUI.DisabledScope(!CanDuplicateShortcutTarget(target)))
                    {
                        if (GUILayout.Button(new GUIContent("Duplicate", "Project 에셋을 같은 폴더에 복제합니다."), GUILayout.Width(76f)))
                        {
                            DuplicateShortcutTarget(target);
                        }
                    }

                    if (GUILayout.Button(new GUIContent("Validate", "선택한 MapGenV2 에셋의 검증 결과를 요약합니다."), GUILayout.Width(70f)))
                    {
                        var report = ValidateShortcutTarget(target);
                        var summary = BuildShortcutValidationSummary(target, report);
                        EditorUtility.DisplayDialog("MapGenV2 Validate", summary, "OK");
                        SetLastOperationResult(summary);
                    }
                }

                using (new EditorGUI.DisabledScope(target != null || createAction == null))
                {
                    if (GUILayout.Button(new GUIContent("Fix/Create Missing", "누락된 참조를 만들거나 starter 흐름으로 보정합니다."), GUILayout.Width(128f)))
                    {
                        createAction();
                    }
                }
            }
        }

        public static string BuildShortcutValidationSummary(Object target, MapGenValidationReport report)
        {
            var targetName = target != null ? target.name : "(none)";
            if (report == null)
            {
                return $"Validate {targetName}: unsupported target.";
            }

            return $"Validate {targetName}: {(report.IsValid ? "Valid" : $"Issues {report.Issues.Count}")}.";
        }

        private static MapGenValidationReport ValidateShortcutTarget(Object target)
        {
            var report = new MapGenValidationReport();
            switch (target)
            {
                case MapGenProfileAsset profileAsset:
                    report.AddRange(profileAsset.Validate(), profileAsset.name);
                    report.AddRange(MapGenProfileGraphValidator.Validate(profileAsset), profileAsset.name);
                    return report;
                case MapGenRuleSetAsset ruleSet:
                    report.AddRange(ruleSet.Validate(), ruleSet.name);
                    return report;
                case MapGenStyleSetAsset styleSet:
                    report.AddRange(styleSet.Validate(), styleSet.name);
                    return report;
                case MapGenModuleSetAsset moduleSet:
                    report.AddRange(moduleSet.Validate(), moduleSet.name);
                    return report;
                case MapGenRoomShapeAsset roomShape:
                    report.AddRange(roomShape.Validate(), roomShape.name);
                    return report;
                case MapGenRoomTemplateAsset roomTemplate:
                    report.AddRange(roomTemplate.Validate(), roomTemplate.name);
                    return report;
                case MapGenCorridorTemplateAsset corridorTemplate:
                    report.AddRange(corridorTemplate.Validate(), corridorTemplate.name);
                    return report;
                case MapGenBakedMapAsset bakedMap:
                    var migration = MapGenBakedMapMigration.MigrateInMemory(bakedMap);
                    if (!migration.IsValid)
                    {
                        report.Add(new MapGenIssue(
                            MapGenGenerationPhase.BakeRuntime,
                            "runtime_bake_incompatible_version",
                            migration.Message,
                            "Rebake this runtime asset with the current MapGenV2 runtime.",
                            severity: MapGenIssueSeverity.Fatal));
                    }

                    return report;
                case MapGenMockupDraftAsset mockupDraft when mockupDraft.Profile != null:
                    report.AddRange(mockupDraft.Profile.Validate(), mockupDraft.Profile.name);
                    report.AddRange(MapGenMockupFeasibilityValidator.Validate(mockupDraft.Width, mockupDraft.Height, mockupDraft.Cells), mockupDraft.name);
                    return report;
                default:
                    return report;
            }
        }

        private static bool CanDuplicateShortcutTarget(Object target)
        {
            return target != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target));
        }

        private static void DuplicateShortcutTarget(Object target)
        {
            var sourcePath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(sourcePath))
            {
                return;
            }

            var directory = System.IO.Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? "Assets";
            var fileName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
            var extension = System.IO.Path.GetExtension(sourcePath);
            var destinationPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{fileName}_Copy{extension}");
            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
            {
                EditorUtility.DisplayDialog("MapGenV2 Duplicate", $"Failed to duplicate {sourcePath}.", "OK");
                return;
            }

            AssetDatabase.ImportAsset(destinationPath);
            var copy = AssetDatabase.LoadAssetAtPath<Object>(destinationPath);
            Selection.activeObject = copy;
            EditorGUIUtility.PingObject(copy);
        }

        private void CreateStarterSetupFromShortcut()
        {
            var setup = MapGenV2StarterSetupBuilder.CreateStarterProfileSetup();
            profile = setup.Profile;
            draft = setup.Draft;
            Selection.activeObject = draft != null ? draft : profile;
            SetLastOperationResult("Starter setup created from linked asset shortcut.");
        }

        private void DrawSceneOutputControls(MapGenV2WorkflowStatus workflow)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("씬 출력 / Scene Output", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                DrawOverwritePolicyField();
                selectedMaterializedRoot = (GameObject)EditorGUILayout.ObjectField("Materialized Root", selectedMaterializedRoot, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck())
                {
                    SaveWindowState();
                }

                DrawWrappingHelpBox(BuildOverwritePolicyHelp(GetOverwriteMode()), MessageType.Info);

                DrawMaterializedPrefabFolderField();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Find Previous Root"))
                    {
                        var marker = MapGenMockupMaterializer.FindExistingMarker(draft);
                        selectedMaterializedRoot = marker != null ? marker.gameObject : null;
                        SetLastOperationResult(selectedMaterializedRoot != null
                            ? $"Found materialized root: {selectedMaterializedRoot.name}."
                            : "No previous materialized root found for the selected draft.");
                    }

                    using (new EditorGUI.DisabledScope(selectedMaterializedRoot == null))
                    {
                        if (GUILayout.Button("Select Materialized Root"))
                        {
                            Selection.activeGameObject = selectedMaterializedRoot;
                            EditorGUIUtility.PingObject(selectedMaterializedRoot);
                        }

                        if (GUILayout.Button("Frame Materialized Root"))
                        {
                            FrameSelectedRoot();
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(selectedMaterializedRoot == null))
                    {
                        if (GUILayout.Button("Clear Previous Materialization"))
                        {
                            MapGenMockupMaterializer.ClearRoot(selectedMaterializedRoot);
                            selectedMaterializedRoot = null;
                            SetLastOperationResult("Cleared selected materialized root.");
                        }

                        if (GUILayout.Button("Save Materialized As Prefab"))
                        {
                            SaveSelectedRootAsPrefab();
                        }
                    }
                }

                if (!workflow.CanMaterialize)
                {
                    DrawWrappingHelpBox($"Materialize To Scene is unavailable: {workflow.MaterializeReason}", MessageType.Info);
                }
            }
        }

        private void DrawMaterializedPrefabFolderField()
        {
            using (new EditorGUI.DisabledScope(profile == null))
            {
                var currentFolder = GetMaterializedPrefabFolder();
                EditorGUI.BeginChangeCheck();
                var newFolder = EditorGUILayout.TextField("Materialized Prefab Folder", currentFolder);
                if (EditorGUI.EndChangeCheck() && profile != null)
                {
                    Undo.RecordObject(profile, "Change MapGen Materialized Prefab Folder");
                    profile.OutputSettings.MaterializedPrefabFolder = newFolder;
                    EditorUtility.SetDirty(profile);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Ensure Prefab Folder", GUILayout.Width(170f)))
                    {
                        MapGenV2AssetFolderUtility.EnsureAssetFolder(GetMaterializedPrefabFolder());
                        SetLastOperationResult($"Materialized prefab folder ready: {GetMaterializedPrefabFolder()}.");
                    }
                }
            }
        }

        private void FrameSelectedRoot()
        {
            if (selectedMaterializedRoot == null)
            {
                return;
            }

            Selection.activeGameObject = selectedMaterializedRoot;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        private void SaveSelectedRootAsPrefab()
        {
            if (selectedMaterializedRoot == null)
            {
                return;
            }

            MapGenV2AssetFolderUtility.CreateDefaultFolders();
            var prefabFolder = GetMaterializedPrefabFolder();
            MapGenV2AssetFolderUtility.EnsureAssetFolder(prefabFolder);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{prefabFolder}/{selectedMaterializedRoot.name}.prefab");
            var prefab = PrefabUtility.SaveAsPrefabAsset(selectedMaterializedRoot, path);
            SetLastOperationResult(prefab != null
                ? $"Saved materialized prefab: {path}."
                : "Save Materialized As Prefab failed.");
        }

        private void DrawDraftActions(MapGenV2WorkflowStatus workflow)
        {
            if (draft == null)
            {
                DrawWrappingHelpBox("Create or assign a draft.", MessageType.Info);
                return;
            }

            if (profile != null && draft.Profile != profile)
            {
                if (GUILayout.Button("Assign Profile To Draft"))
                {
                    Undo.RecordObject(draft, "Assign MapGen Profile");
                    draft.Profile = profile;
                    EditorUtility.SetDirty(draft);
                    SaveWindowState();
                    SetLastOperationResult("Selected profile assigned to draft.");
                }
            }

            DrawPrimaryActionSummary(workflow);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!workflow.CanGenerate))
                {
                    var generateLabel = workflow.HasGeneratedMockup && !workflow.GeneratedCurrent
                        ? MapGenV2EditorText.Get("mapgenv2.regenerateMockup")
                        : MapGenV2EditorText.Get("mapgenv2.generateMockup");
                    if (GUILayout.Button(generateLabel))
                    {
                        GenerateMockup("Generate Mockup");
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanGenerate))
                {
                    if (GUILayout.Button("Regenerate Same Seed / 같은 시드 재생성"))
                    {
                        GenerateMockup("Regenerate Same Seed", preserveLockedRegions: true);
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanPostProcess))
                {
                    if (GUILayout.Button("Repostprocess Mockup / 후처리 재실행"))
                    {
                        RunPostProcess();
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanAccept))
                {
                    var acceptLabel = workflow.Accepted
                        ? MapGenV2EditorText.Get("mapgenv2.reacceptMockup")
                        : MapGenV2EditorText.Get("mapgenv2.acceptMockup");
                    if (GUILayout.Button(acceptLabel))
                    {
                        Undo.RecordObject(draft, "Accept Mockup");
                        draft.Accept();
                        EditorUtility.SetDirty(draft);
                        SetLastOperationResult($"Accepted mockup signature {draft.AcceptedSignature}.");
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(draft == null))
                {
                    if (GUILayout.Button("Randomize Seed"))
                    {
                        Undo.RecordObject(draft, "Randomize MapGen Seed");
                        draft.Seed = CreateRandomSeed();
                        draft.ClearDraft();
                        ClearSelection();
                        EditorUtility.SetDirty(draft);
                        SetLastOperationResult($"Randomized seed to {draft.Seed}.");
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanGenerate))
                {
                    if (GUILayout.Button("Randomize Seed + Generate"))
                    {
                        Undo.RecordObject(draft, "Randomize Seed And Generate Mockup");
                        draft.Seed = CreateRandomSeed();
                        GenerateMockup("Randomize Seed + Generate", recordUndo: false);
                    }
                }

                using (new EditorGUI.DisabledScope(draft == null))
                {
                    if (GUILayout.Button("Clear Draft"))
                    {
                        Undo.RecordObject(draft, "Clear MapGen Draft");
                        draft.ClearDraft();
                        ClearSelection();
                        EditorUtility.SetDirty(draft);
                        SetLastOperationResult("Cleared draft cells, generated signature, post-process report, and acceptance.");
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!workflow.CanMaterialize))
                {
                    var materializeLabel = ShouldRematerialize()
                        ? MapGenV2EditorText.Get("mapgenv2.rematerializeToScene")
                        : MapGenV2EditorText.Get("mapgenv2.materializeToScene");
                    if (GUILayout.Button(materializeLabel))
                    {
                        MaterializeToScene();
                    }
                }

                using (new EditorGUI.DisabledScope(!workflow.CanBakeRuntime))
                {
                    var bakeLabel = ShouldRebake()
                        ? MapGenV2EditorText.Get("mapgenv2.rebakeRuntimeAsset")
                        : MapGenV2EditorText.Get("mapgenv2.bakeRuntimeAsset");
                    if (GUILayout.Button(bakeLabel))
                    {
                        BakeRuntimeAsset();
                    }
                }
            }

            if (!workflow.CanGenerate)
            {
                DrawWrappingHelpBox($"Generate Mockup disabled: {workflow.GenerateReason}", MessageType.Info);
            }

            if (!workflow.CanMaterialize)
            {
                DrawWrappingHelpBox($"Materialize To Scene disabled: {workflow.MaterializeReason}", MessageType.Info);
            }

            if (!workflow.CanBakeRuntime)
            {
                DrawWrappingHelpBox($"Bake Runtime Asset disabled: {workflow.BakeRuntimeReason}", MessageType.Info);
            }
        }

        private void DrawPrimaryActionSummary(MapGenV2WorkflowStatus workflow)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("주요 작업 설명 / Primary Action Impact", EditorStyles.boldLabel);
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
                EditorGUILayout.LabelField(
                    BuildPrimaryActionExplanation(
                        "Generate Mockup",
                        workflow.CanGenerate,
                        workflow.GenerateReason,
                        "writes a new blue/red/black/gray preview into the selected draft",
                        draft != null ? AssetDatabase.GetAssetPath(draft) : "(draft asset)"),
                    style);
                EditorGUILayout.LabelField(
                    BuildPrimaryActionExplanation(
                        "Post-Process",
                        workflow.CanPostProcess,
                        workflow.PostProcessReason,
                        "updates generated cells using the rule set pass list",
                        draft != null ? AssetDatabase.GetAssetPath(draft) : "(draft asset)"),
                    style);
                EditorGUILayout.LabelField(
                    BuildPrimaryActionExplanation(
                        "Accept Mockup",
                        workflow.CanAccept,
                        workflow.AcceptReason,
                        "records the current draft signature as the materialization source",
                        draft != null ? AssetDatabase.GetAssetPath(draft) : "(draft asset)"),
                    style);
                EditorGUILayout.LabelField(
                    BuildPrimaryActionExplanation(
                        "Materialize To Scene",
                        workflow.CanMaterialize,
                        workflow.MaterializeReason,
                        "instantiates prefab or placeholder modules from the accepted mockup",
                        $"Scene root MapGenV2_<Profile>_<Seed>; prefab folder {GetMaterializedPrefabFolder()}"),
                    style);
                EditorGUILayout.LabelField(
                    BuildPrimaryActionExplanation(
                        "Bake Runtime Asset",
                        workflow.CanBakeRuntime,
                        workflow.BakeRuntimeReason,
                        "writes runtime-safe grid, region, connector, prop, marker, and traversal data",
                        BuildExpectedBakedAssetPath()),
                    style);
            }
        }

        private void GenerateMockup(string operationName, bool recordUndo = true, bool preserveLockedRegions = false)
        {
            if (draft == null)
            {
                return;
            }

            if (recordUndo)
            {
                Undo.RecordObject(draft, operationName);
            }

            var budget = MapGenV2PerformanceProfile.SelectBudget(draft.Width, draft.Height);
            if (MapGenV2EditorProgress.Begin(operationName, "Solving mockup layout..."))
            {
                SetLastOperationResult($"{operationName} cancelled before solving.");
                MapGenV2EditorProgress.End();
                return;
            }

            try
            {
                MapGenV2EditorProgress.Report(operationName, "Solving mockup layout...", 0.35f);
                var report = MapGenV2PerformanceProfiler.Measure(
                    operationName,
                    draft.Width,
                    draft.Height,
                    budget.GenerationBudgetMs,
                    () => preserveLockedRegions
                        ? draft.RegenerateUnlockedFromProfile(() => MapGenV2EditorProgress.Begin(operationName, "Solving mockup layout..."))
                        : draft.GenerateFromProfile(() => MapGenV2EditorProgress.Begin(operationName, "Solving mockup layout...")),
                    result => MapGenV2PerformanceDetails.ForValidationReport(result, 1, $"Seed={draft.Seed}"),
                    out var sample);
                MapGenV2EditorProgress.Report(operationName, "Building preview summary...", 0.85f);
                ClearSelection();
                EditorUtility.SetDirty(draft);
                var preview = MapGenMockupPreviewData.FromDraft(draft);
                SetLastOperationResult(report.IsValid
                    ? $"{operationName} complete. Seed {draft.Seed}, Retry 0, Rooms {preview.Summary.RoomCells}, Corridors {preview.Summary.CorridorCells}. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}."
                    : $"{operationName} failed. See validation messages above. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}.");
            }
            finally
            {
                MapGenV2EditorProgress.End();
            }
        }

        private void RunPostProcess()
        {
            if (draft == null)
            {
                return;
            }

            var budget = MapGenV2PerformanceProfile.SelectBudget(draft.Width, draft.Height);
            if (MapGenV2EditorProgress.Begin("Post-Process Mockup", "Running post-process passes..."))
            {
                SetLastOperationResult("Post-process cancelled before running.");
                MapGenV2EditorProgress.End();
                return;
            }

            try
            {
                Undo.RecordObject(draft, "Post-Process Mockup");
                MapGenV2EditorProgress.Report("Post-Process Mockup", "Applying pass list...", 0.5f);
                var report = MapGenV2PerformanceProfiler.Measure(
                    "Post-Process Mockup",
                    draft.Width,
                    draft.Height,
                    budget.GenerationBudgetMs,
                    () => draft.ApplyPostProcessingFromProfile(() => MapGenV2EditorProgress.Begin("Post-Process Mockup", "Running post-process passes...")),
                    result => MapGenV2PerformanceDetails.ForPostProcess(result, $"Seed={draft.Seed}"),
                    out var sample);
                lastPostProcessReport = report;
                selectedPostProcessPassIndex = Mathf.Clamp(
                    selectedPostProcessPassIndex,
                    0,
                    Mathf.Max(0, (report.PassReports?.Length ?? 1) - 1));
                EditorUtility.SetDirty(draft);
                SetLastOperationResult($"Post-process complete. Passes {report.PassesRun}, Direct routes +{report.DirectRouteCellsAdded}, reserved masks filled {report.ReservedMaskCellsFilled}, dead ends removed {report.DeadEndCorridorsRemoved}, isolated rooms removed {report.IsolatedRoomsRemoved}. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}.");
            }
            finally
            {
                MapGenV2EditorProgress.End();
            }
        }

        private void MaterializeToScene()
        {
            if (draft == null)
            {
                return;
            }

            var budget = MapGenV2PerformanceProfile.SelectBudget(draft.Width, draft.Height);
            if (MapGenV2EditorProgress.Begin("Materialize To Scene", "Instantiating prefab modules..."))
            {
                SetLastOperationResult("Materialize To Scene cancelled before instantiation.");
                MapGenV2EditorProgress.End();
                return;
            }

            try
            {
                MapGenV2EditorProgress.Report("Materialize To Scene", "Stamping modules...", 0.45f);
                var root = MapGenV2PerformanceProfiler.Measure(
                    "Materialize To Scene",
                    draft.Width,
                    draft.Height,
                    budget.MaterializationBudgetMs,
                    () => MapGenMockupMaterializer.Materialize(
                        draft,
                        GetSceneOutputMode(),
                        selectedMaterializedRoot,
                        () => MapGenV2EditorProgress.Begin("Materialize To Scene", "Instantiating prefab modules...")),
                    _ => MapGenV2PerformanceDetails.ForMaterialization(
                        MapGenMockupMaterializer.BuildReport(
                            draft.Profile != null && draft.Profile.StyleSet != null ? draft.Profile.StyleSet.ModuleSet : null,
                            MapGenMockupMaterializer.BuildPlan(draft),
                            draft.Seed),
                        $"Seed={draft.Seed}"),
                    out var sample);
                selectedMaterializedRoot = root;
                SetLastOperationResult(root != null
                    ? $"Materialized scene root: {root.name}. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}."
                    : $"Materialize To Scene failed. Check accepted state and module coverage. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}.");
            }
            finally
            {
                MapGenV2EditorProgress.End();
            }
        }

        private void BakeRuntimeAsset()
        {
            if (draft == null)
            {
                return;
            }

            var budget = MapGenV2PerformanceProfile.SelectBudget(draft.Width, draft.Height);
            if (MapGenV2EditorProgress.Begin("Bake Runtime Asset", "Writing runtime-safe baked map..."))
            {
                SetLastOperationResult("Bake Runtime Asset cancelled before writing.");
                MapGenV2EditorProgress.End();
                return;
            }

            try
            {
                MapGenV2EditorProgress.Report("Bake Runtime Asset", "Building baked cells and traversal...", 0.55f);
                var bakedAsset = MapGenV2PerformanceProfiler.Measure(
                    "Bake Runtime Asset",
                    draft.Width,
                    draft.Height,
                    budget.GenerationBudgetMs,
                    () => MapGenRuntimeBakeUtility.Bake(
                        draft,
                        () => MapGenV2EditorProgress.Begin("Bake Runtime Asset", "Writing runtime-safe baked map...")),
                    result => MapGenV2PerformanceDetails.ForBakedMap(result, $"Seed={draft.Seed}"),
                    out var sample);
                SetLastOperationResult(bakedAsset != null
                    ? $"Baked runtime asset: {AssetDatabase.GetAssetPath(bakedAsset)}. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}."
                    : $"Bake Runtime Asset failed. Check accepted state. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}.");
            }
            finally
            {
                MapGenV2EditorProgress.End();
            }
        }

        private string BuildGuidedNextAction(MapGenV2WorkflowStatus workflow)
        {
            if (!workflow.CanMaterialize)
            {
                return workflow.NextAction;
            }

            var materializedRoot = ResolveMaterializedRoot();
            if (materializedRoot == null)
            {
                return "Materialize To Scene / accepted mockup을 씬 출력으로 생성.";
            }

            var materializedReport = MapGenMockupMaterializer.ValidateExistingOutput(draft, materializedRoot);
            if (!materializedReport.IsValid)
            {
                return "Rematerialize To Scene / stale materialized root를 현재 accepted mockup과 module set으로 재생성.";
            }

            if (!workflow.CanBakeRuntime)
            {
                return workflow.NextAction;
            }

            var bakedAsset = LoadExpectedBakedAsset();
            if (bakedAsset == null)
            {
                return "Bake Runtime Asset / runtime-safe baked map asset 생성.";
            }

            var bakeReport = MapGenRuntimeBakeUtility.ValidateConsistency(draft, bakedAsset);
            if (!bakeReport.IsValid)
            {
                return "Rebake Runtime Asset / stale baked asset을 현재 accepted mockup으로 재생성.";
            }

            return "Outputs are current / materialized scene output과 runtime bake가 현재 accepted mockup과 일치.";
        }

        private bool ShouldRematerialize()
        {
            var materializedRoot = ResolveMaterializedRoot();
            return materializedRoot != null
                && !MapGenMockupMaterializer.ValidateExistingOutput(draft, materializedRoot).IsValid;
        }

        private bool ShouldRebake()
        {
            if (draft == null || !draft.Accepted || !draft.IsAcceptedSignatureCurrent)
            {
                return false;
            }

            var bakedAsset = LoadExpectedBakedAsset();
            return bakedAsset != null && !MapGenRuntimeBakeUtility.ValidateConsistency(draft, bakedAsset).IsValid;
        }

        private GameObject ResolveMaterializedRoot()
        {
            if (selectedMaterializedRoot != null)
            {
                return selectedMaterializedRoot;
            }

            var marker = MapGenMockupMaterializer.FindExistingMarker(draft);
            return marker != null ? marker.gameObject : null;
        }

        private void DrawOverwritePolicyField()
        {
            if (profile == null)
            {
                outputMode = (MapGenV2SceneOutputMode)EditorGUILayout.EnumPopup("출력 모드 / Output Mode", outputMode);
                return;
            }

            var overwriteMode = profile.OutputSettings.OverwriteMode;
            overwriteMode = (MapGenV2OutputOverwriteMode)EditorGUILayout.EnumPopup("덮어쓰기 정책 / Overwrite Policy", overwriteMode);
            if (overwriteMode != profile.OutputSettings.OverwriteMode)
            {
                Undo.RecordObject(profile, "Change MapGen Overwrite Policy");
                profile.OutputSettings.OverwriteMode = overwriteMode;
                outputMode = ToSceneOutputMode(overwriteMode);
                EditorUtility.SetDirty(profile);
            }
        }

        private MapGenV2OutputOverwriteMode GetOverwriteMode()
        {
            if (profile != null)
            {
                return profile.OutputSettings.OverwriteMode;
            }

            return outputMode switch
            {
                MapGenV2SceneOutputMode.CreateNewRoot => MapGenV2OutputOverwriteMode.CreateUnique,
                MapGenV2SceneOutputMode.UpdateSelectedRoot => MapGenV2OutputOverwriteMode.UpdateSelected,
                _ => MapGenV2OutputOverwriteMode.ReplacePrevious
            };
        }

        private MapGenV2SceneOutputMode GetSceneOutputMode()
        {
            return profile != null ? ToSceneOutputMode(profile.OutputSettings.OverwriteMode) : outputMode;
        }

        private static MapGenV2SceneOutputMode ToSceneOutputMode(MapGenV2OutputOverwriteMode overwriteMode)
        {
            return overwriteMode switch
            {
                MapGenV2OutputOverwriteMode.CreateUnique => MapGenV2SceneOutputMode.CreateNewRoot,
                MapGenV2OutputOverwriteMode.UpdateSelected => MapGenV2SceneOutputMode.UpdateSelectedRoot,
                _ => MapGenV2SceneOutputMode.ReplacePreviousRoot
            };
        }

        private static string BuildOverwritePolicyHelp(MapGenV2OutputOverwriteMode overwriteMode)
        {
            return overwriteMode switch
            {
                MapGenV2OutputOverwriteMode.CreateUnique =>
                    "CreateUnique: 기존 씬 출력을 보존하고 새 root를 생성합니다.",
                MapGenV2OutputOverwriteMode.UpdateSelected =>
                    "UpdateSelected: 선택한 Materialized Root의 children/marker만 교체합니다. 선택 root가 없으면 materialize를 실행하지 않습니다.",
                _ =>
                    "ReplacePrevious: 같은 draft/profile/seed/signature의 이전 MapGenV2 root를 삭제한 뒤 새 root를 생성합니다."
            };
        }

        private static int CreateRandomSeed()
        {
            return System.Guid.NewGuid().GetHashCode() & 0x7fffffff;
        }

        private void CreateDraft()
        {
            MapGenV2AssetFolderUtility.CreateDefaultFolders();
            var draftFolder = GetDraftFolder();
            MapGenV2AssetFolderUtility.EnsureAssetFolder(draftFolder);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{draftFolder}/MapGenMockupDraft.asset");
            draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            draft.Profile = profile;
            AssetDatabase.CreateAsset(draft, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = draft;
            SetLastOperationResult($"Created draft: {path}.");
        }

        private string BuildExpectedBakedAssetPath()
        {
            if (draft == null || draft.Profile == null)
            {
                return GetBakedAssetFolder();
            }

            return $"{GetBakedAssetFolder()}/{draft.Profile.ProfileId}_{draft.Seed}_BakedMap.asset";
        }

        private MapGenBakedMapAsset LoadExpectedBakedAsset()
        {
            var path = BuildExpectedBakedAssetPath();
            return AssetDatabase.LoadAssetAtPath<MapGenBakedMapAsset>(path);
        }

        private string GetDraftFolder()
        {
            return profile != null && !string.IsNullOrWhiteSpace(profile.OutputSettings.DraftFolder)
                ? profile.OutputSettings.DraftFolder
                : MapGenOutputSettings.Defaults().DraftFolder;
        }

        private string GetMaterializedPrefabFolder()
        {
            return profile != null && !string.IsNullOrWhiteSpace(profile.OutputSettings.MaterializedPrefabFolder)
                ? profile.OutputSettings.MaterializedPrefabFolder
                : MapGenOutputSettings.Defaults().MaterializedPrefabFolder;
        }

        private string GetBakedAssetFolder()
        {
            return profile != null && !string.IsNullOrWhiteSpace(profile.OutputSettings.BakedAssetFolder)
                ? profile.OutputSettings.BakedAssetFolder
                : MapGenOutputSettings.Defaults().BakedAssetFolder;
        }

        private void SaveWindowState()
        {
            SaveAssetToEditorPrefs(ProfilePathKey, profile);
            SaveAssetToEditorPrefs(DraftPathKey, draft);
            EditorPrefs.SetFloat(PreviewCellSizeKey, previewCellSize);
            EditorPrefs.SetInt(OutputModeKey, (int)outputMode);
            EditorPrefs.SetBool(ShowPropOverlayKey, showPropPlacementOverlay);
            EditorPrefs.SetBool(ShowPostProcessOverlayKey, showPostProcessOverlay);
            EditorPrefs.SetFloat(PreviewScrollXKey, previewScroll.x);
            EditorPrefs.SetFloat(PreviewScrollYKey, previewScroll.y);
            EditorPrefs.SetBool(HasSelectedCellKey, hasSelectedCell);
            EditorPrefs.SetInt(SelectedCellXKey, selectedCell.x);
            EditorPrefs.SetInt(SelectedCellYKey, selectedCell.y);
            EditorPrefs.SetInt(SelectedRegionIdKey, selectedRegionId);
            EditorPrefs.SetBool(DiagnosticsFoldoutKey, showDiagnosticsFoldout);
        }

        private static void SaveAssetToEditorPrefs(string key, Object asset)
        {
            if (asset == null)
            {
                EditorPrefs.DeleteKey(key);
                return;
            }

            EditorPrefs.SetString(key, AssetDatabase.GetAssetPath(asset));
        }

        private static T LoadAssetFromEditorPrefs<T>(string key) where T : Object
        {
            var path = EditorPrefs.GetString(key, string.Empty);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private void DrawDraftSummaryAndPreview()
        {
            if (draft == null)
            {
                return;
            }

            var previewData = MapGenMockupPreviewData.FromDraft(draft);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("목업 요약 / Mockup Summary", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("프로필 / Profile", string.IsNullOrEmpty(previewData.ProfileId) ? "(none)" : previewData.ProfileId);
                EditorGUILayout.LabelField("시드 / Seed", previewData.Seed.ToString());
                EditorGUILayout.LabelField("그리드 / Grid", $"{previewData.Width} x {previewData.Height}");
                EditorGUILayout.LabelField("생성 서명 / Generated Signature", string.IsNullOrEmpty(previewData.LastGeneratedSignature) ? "(none)" : previewData.LastGeneratedSignature);
                EditorGUILayout.LabelField("현재 서명 / Current Signature", previewData.CurrentSignature);
                EditorGUILayout.LabelField("수락 서명 / Accepted Signature", string.IsNullOrEmpty(previewData.AcceptedSignature) ? "(none)" : previewData.AcceptedSignature);
                EditorGUILayout.LabelField(
                    "생성 상태 / Generated State",
                    previewData.GeneratedSignatureCurrent ? "최신 / Current" : MapGenV2EditorText.Get("mapgenv2.draftStale"));
                EditorGUILayout.LabelField(
                    "상태 / State",
                    previewData.Accepted
                        ? previewData.AcceptedSignatureCurrent ? "수락됨 / Accepted" : "변경됨: 다시 수락 필요 / Stale"
                        : "미수락 / Not accepted");
                EditorGUILayout.LabelField(
                    "셀 / Cells",
                    $"Rooms {previewData.Summary.RoomCells}, Corridors {previewData.Summary.CorridorCells}, Blocked {previewData.Summary.BlockedCells}, Connectors {previewData.Summary.ConnectorCells}, Reserved {previewData.Summary.ReservedCells}");
                EditorGUILayout.LabelField("리전 / Regions", previewData.Summary.RegionCount.ToString());
            }

            var propPlacement = MapGenPropPlacementPlanner.BuildForDraft(draft);
            DrawPropPlacementPreviewSummary(propPlacement);
            DrawPostProcessPassSummary();
            DrawWrappingHelpBox(BuildPrimaryMockupUxSummary(), MessageType.Info);
            DrawMockupPreview(previewData);
            DrawCellDetails(previewData);
        }

        private void DrawMockupPreview(MapGenMockupPreviewData previewData)
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("목업 미리보기 / Mockup Preview", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("확대 / Zoom", GUILayout.Width(72f));
                previewCellSize = EditorGUILayout.Slider(previewCellSize, 6f, 32f, GUILayout.Width(180f));
                showPropPlacementOverlay = EditorGUILayout.ToggleLeft("프롭 오버레이 / Props", showPropPlacementOverlay, GUILayout.Width(150f));
                using (new EditorGUI.DisabledScope(!HasPostProcessPassOverlay()))
                {
                    showPostProcessOverlay = EditorGUILayout.ToggleLeft("후처리 오버레이 / Post", showPostProcessOverlay, GUILayout.Width(170f));
                }
            }

            DrawLegend();

            if (previewData.Width <= 0 || previewData.Height <= 0)
            {
                EditorGUILayout.HelpBox("생성된 목업이 없습니다. Generate Mockup을 누르면 이 영역에 파랑/빨강/검정/회색 그리드가 표시됩니다.", MessageType.Info);
                return;
            }

            var previewWidth = previewData.Width * previewCellSize;
            var previewHeight = previewData.Height * previewCellSize;
            var viewHeight = Mathf.Min(Mathf.Max(180f, previewHeight + 20f), 520f);
            previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.MinHeight(viewHeight), GUILayout.MaxHeight(viewHeight));
            var rect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            var propPlacement = showPropPlacementOverlay ? MapGenPropPlacementPlanner.BuildForDraft(draft) : null;
            HandlePreviewInput(previewData, rect);
            DrawPreviewCells(previewData, rect);
            DrawPropPlacementOverlay(previewData, rect, propPlacement);
            DrawPostProcessPassOverlay(previewData, rect);
            EditorGUILayout.EndScrollView();
        }

        private void DrawLegend()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                DrawLegendItem(EmptyColor, "파랑 Empty");
                DrawLegendItem(RoomColor, "빨강 Room");
                DrawLegendItem(CorridorColor, "검정 Corridor");
                DrawLegendItem(BlockedColor, "회색 Blocked");
                DrawLegendItem(ConnectorColor, "검정 Connector");
                DrawLegendItem(ReservedColor, "회색 Reserved");
                DrawLegendItem(PropOverlayColor, "초록 Prop");
                DrawLegendItem(BlockerPropOverlayColor, "주황 Blocker");
            }
        }

        private static void DrawPropPlacementPreviewSummary(MapGenPropPlacementResult propPlacement)
        {
            if (propPlacement == null || propPlacement.PlacedProps.Length == 0 && propPlacement.Report.TotalCandidateCells == 0)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("프롭 배치 미리보기 / Prop Placement Preview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "요약 / Summary",
                    $"Candidates {propPlacement.Report.TotalCandidateCells}, Placed {propPlacement.Report.PlacedCount}, Spacing Rejects {propPlacement.Report.RejectedBySpacing}, Blocker Issues {propPlacement.Report.BlockerTraversalIssues}");
                if (!propPlacement.Report.IsValid)
                {
                    EditorGUILayout.HelpBox("프롭 배치에 문제가 있습니다. Materialize 전에 rule/filter/blocker 설정을 확인하세요.", MessageType.Warning);
                }
            }
        }

        private void DrawPostProcessPassSummary()
        {
            if (!HasPostProcessPassOverlay())
            {
                return;
            }

            var passReports = lastPostProcessReport.PassReports;
            selectedPostProcessPassIndex = Mathf.Clamp(selectedPostProcessPassIndex, 0, passReports.Length - 1);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("후처리 패스 미리보기 / Post-Process Pass Preview", EditorStyles.boldLabel);
                var labels = new string[passReports.Length];
                for (var i = 0; i < passReports.Length; i++)
                {
                    var pass = passReports[i];
                    labels[i] = $"{i + 1}. {pass.PassKind} changed {pass.ChangedCells}"
                        + (pass.RolledBack ? " (rollback)" : string.Empty);
                }

                selectedPostProcessPassIndex = EditorGUILayout.Popup("Overlay Pass", selectedPostProcessPassIndex, labels);
                var selected = passReports[selectedPostProcessPassIndex];
                EditorGUILayout.LabelField(
                    "Selected Pass",
                    $"{selected.PassKind}, Changed {selected.ChangedCells}, Connectivity {(selected.ConnectivityValid ? "valid" : "invalid")}, Before {selected.BeforeSignature}, After {selected.AfterSignature}");
                if (selected.RolledBack)
                {
                    EditorGUILayout.HelpBox("이 패스는 필수 경로를 깨서 rollback되었습니다. / This pass was rolled back because required traversal broke.", MessageType.Warning);
                }
            }
        }

        private static void DrawLegendItem(Color color, string label)
        {
            var rect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
            EditorGUI.DrawRect(rect, color);
            EditorGUILayout.LabelField(label, GUILayout.Width(104f));
        }

        private void DrawPreviewCells(MapGenMockupPreviewData previewData, Rect rect)
        {
            var texture = previewTextureCache.GetOrCreate(previewData, new MapGenV2PreviewPalette(
                EmptyColor,
                RoomColor,
                CorridorColor,
                BlockedColor,
                ConnectorColor,
                ReservedColor));
            if (texture != null)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
            }

            for (var y = previewData.Height - 1; y >= 0; y--)
            {
                for (var x = 0; x < previewData.Width; x++)
                {
                    previewData.TryGetCell(x, y, out var cell);
                    var cellRect = RectForCell(previewData, rect, x, y);

                    if (hasSelectedCell && IsSelectedCellOrRegion(cell, x, y))
                    {
                        EditorGUI.DrawRect(cellRect, SelectedRegionColor);
                    }

                    if (hasSelectedCell && IsSelectedRegionConnector(cell))
                    {
                        EditorGUI.DrawRect(cellRect, SelectedConnectorColor);
                    }

                    if (hasSelectedCell && IsAdjacentRegionLink(previewData, cell, x, y))
                    {
                        EditorGUI.DrawRect(cellRect, AdjacentLinkColor);
                    }

                    if (hasHoveredCell && hoveredCell.x == x && hoveredCell.y == y)
                    {
                        EditorGUI.DrawRect(cellRect, HoverColor);
                    }

                    if (previewCellSize >= 8f)
                    {
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1f), GridLineColor);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - 1f, cellRect.width, 1f), GridLineColor);
                        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1f, cellRect.height), GridLineColor);
                        EditorGUI.DrawRect(new Rect(cellRect.xMax - 1f, cellRect.y, 1f, cellRect.height), GridLineColor);
                    }
                }
            }
        }

        private bool HasPostProcessPassOverlay()
        {
            return lastPostProcessReport != null
                && lastPostProcessReport.PassReports != null
                && lastPostProcessReport.PassReports.Length > 0;
        }

        private void DrawPostProcessPassOverlay(MapGenMockupPreviewData previewData, Rect rect)
        {
            if (!showPostProcessOverlay || !HasPostProcessPassOverlay())
            {
                return;
            }

            var passReports = lastPostProcessReport.PassReports;
            selectedPostProcessPassIndex = Mathf.Clamp(selectedPostProcessPassIndex, 0, passReports.Length - 1);
            var selected = passReports[selectedPostProcessPassIndex];
            foreach (var coord in selected.ChangedCoords ?? System.Array.Empty<MapGenGridCoord>())
            {
                if (!coord.IsInBounds(previewData.Width, previewData.Height))
                {
                    continue;
                }

                var cellRect = RectForCell(previewData, rect, coord.X, coord.Y);
                var overlay = selected.RolledBack
                    ? new Color(1f, 0.25f, 0.05f, 0.48f)
                    : new Color(1f, 0.95f, 0.05f, 0.42f);
                EditorGUI.DrawRect(cellRect, overlay);
            }
        }

        private void DrawPropPlacementOverlay(
            MapGenMockupPreviewData previewData,
            Rect rect,
            MapGenPropPlacementResult propPlacement)
        {
            if (propPlacement == null || propPlacement.PlacedProps == null || propPlacement.PlacedProps.Length == 0)
            {
                return;
            }

            var placed = new HashSet<MapGenGridCoord>();
            foreach (var prop in propPlacement.PlacedProps)
            {
                if (!placed.Add(prop.Coord))
                {
                    continue;
                }

                var cellRect = RectForCell(previewData, rect, prop.Coord.X, prop.Coord.Y);
                var size = Mathf.Max(4f, previewCellSize * 0.42f);
                var markerRect = new Rect(
                    cellRect.center.x - size * 0.5f,
                    cellRect.center.y - size * 0.5f,
                    size,
                    size);
                EditorGUI.DrawRect(markerRect, ColorForProp(prop));
            }
        }

        private Rect RectForCell(MapGenMockupPreviewData previewData, Rect rect, int x, int y)
        {
            return new Rect(
                rect.x + x * previewCellSize,
                rect.y + (previewData.Height - 1 - y) * previewCellSize,
                Mathf.Ceil(previewCellSize),
                Mathf.Ceil(previewCellSize));
        }

        private void HandlePreviewInput(MapGenMockupPreviewData previewData, Rect rect)
        {
            var current = Event.current;
            if (current == null)
            {
                return;
            }

            var inside = rect.Contains(current.mousePosition);
            if (!inside)
            {
                if (hasHoveredCell && current.type == EventType.MouseMove)
                {
                    hasHoveredCell = false;
                    Repaint();
                }

                return;
            }

            var x = Mathf.FloorToInt((current.mousePosition.x - rect.x) / previewCellSize);
            var drawY = Mathf.FloorToInt((current.mousePosition.y - rect.y) / previewCellSize);
            var y = previewData.Height - 1 - drawY;
            var coord = new Vector2Int(x, y);
            if (!previewData.TryGetCell(coord, out var cell))
            {
                return;
            }

            if (!hasHoveredCell || hoveredCell != coord)
            {
                hoveredCell = coord;
                hasHoveredCell = true;
                Repaint();
            }

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                selectedCell = coord;
                hasSelectedCell = true;
                selectedRegionId = cell.RegionId;
                SaveWindowState();
                current.Use();
                Repaint();
            }
        }

        private void DrawCellDetails(MapGenMockupPreviewData previewData)
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("선택/호버 정보 / Selection", EditorStyles.boldLabel);
                DrawCellLine("호버 / Hover", previewData, hasHoveredCell, hoveredCell);
                DrawCellLine("선택 / Selected", previewData, hasSelectedCell, selectedCell);
                if (hasSelectedCell)
                {
                    EditorGUILayout.LabelField("선택 리전 / Selected Region", selectedRegionId >= 0 ? selectedRegionId.ToString() : "(none)");
                }
            }

            DrawSelectedRegionInspector(previewData);
        }

        private void DrawSelectedRegionInspector(MapGenMockupPreviewData previewData)
        {
            if (draft == null || !hasSelectedCell || selectedRegionId < 0)
            {
                return;
            }

            var cellCount = 0;
            var roomCount = 0;
            var corridorCount = 0;
            var connectorCount = 0;
            var adjacentLinkCount = 0;
            var blockedCount = 0;
            var reservedCount = 0;
            var category = MapGenRoomCategory.Main;
            var sourceTemplateId = string.Empty;
            var sourceShapeId = string.Empty;
            var sawCategory = false;
            for (var y = 0; y < previewData.Height; y++)
            {
                for (var x = 0; x < previewData.Width; x++)
                {
                    if (!previewData.TryGetCell(x, y, out var cell) || cell.RegionId != selectedRegionId)
                    {
                        continue;
                    }

                    cellCount++;
                    if (!sawCategory)
                    {
                        category = cell.RoomCategory;
                        sawCategory = true;
                    }

                    if (string.IsNullOrEmpty(sourceTemplateId) && !string.IsNullOrEmpty(cell.SourceTemplateId))
                    {
                        sourceTemplateId = cell.SourceTemplateId;
                    }

                    if (string.IsNullOrEmpty(sourceShapeId) && !string.IsNullOrEmpty(cell.SourceShapeId))
                    {
                        sourceShapeId = cell.SourceShapeId;
                    }

                    switch (cell.State)
                    {
                        case MapGenCellState.Room:
                            roomCount++;
                            break;
                        case MapGenCellState.Corridor:
                            corridorCount++;
                            break;
                        case MapGenCellState.Connector:
                            connectorCount++;
                            break;
                        case MapGenCellState.Blocked:
                            blockedCount++;
                            break;
                        case MapGenCellState.Reserved:
                            reservedCount++;
                            break;
                    }
                }
            }

            adjacentLinkCount = CountAdjacentRegionLinks(previewData);
            draft.TryGetRegionOverride(selectedRegionId, out var regionOverride);
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("선택 리전 편집 / Selected Region", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Region Id", selectedRegionId.ToString());
                EditorGUILayout.LabelField("Type / 타입", DescribeRegionType(roomCount, corridorCount, connectorCount, blockedCount, reservedCount));
                EditorGUILayout.LabelField("Cell Count", cellCount.ToString());
                EditorGUILayout.LabelField(
                    "State Counts",
                    $"Room {roomCount}, Corridor {corridorCount}, Connector {connectorCount}, Blocked {blockedCount}, Reserved {reservedCount}");
                EditorGUILayout.LabelField("Connector Count", connectorCount.ToString());
                EditorGUILayout.LabelField("Adjacent Links / 인접 링크", adjacentLinkCount.ToString());
                EditorGUILayout.LabelField("Locked", regionOverride.Locked ? "Yes" : "No");
                EditorGUILayout.LabelField("Category / 카테고리", category.ToString());
                EditorGUILayout.LabelField(
                    "Override",
                    regionOverride.HasCategoryOverride ? $"Category {regionOverride.CategoryOverride}" : "(none)");
                EditorGUILayout.LabelField("Source Template", string.IsNullOrEmpty(sourceTemplateId) ? "(none)" : sourceTemplateId);
                EditorGUILayout.LabelField("Source Shape", string.IsNullOrEmpty(sourceShapeId) ? "(none)" : sourceShapeId);
                EditorGUILayout.LabelField("Post-process Tags", BuildPostProcessTags(draft));
                EditorGUILayout.LabelField(
                    "Materialization",
                    DescribeMaterializationHint(roomCount, corridorCount, connectorCount, blockedCount, reservedCount));

                EditorGUI.BeginChangeCheck();
                var newCategory = (MapGenRoomCategory)EditorGUILayout.EnumPopup("Room Category", category);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(draft, "Change MapGen Region Category");
                    draft.SetRegionCategory(selectedRegionId, newCategory);
                    EditorUtility.SetDirty(draft);
                    SetLastOperationResult($"Changed region {selectedRegionId} category to {newCategory}. Accepted output is now stale until reaccepted.");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(regionOverride.Locked ? "Unlock Region" : "Lock Region"))
                    {
                        Undo.RecordObject(draft, "Toggle MapGen Region Lock");
                        draft.SetRegionLocked(selectedRegionId, !regionOverride.Locked);
                        EditorUtility.SetDirty(draft);
                        SetLastOperationResult($"{(regionOverride.Locked ? "Unlocked" : "Locked")} region {selectedRegionId}.");
                    }

                    if (GUILayout.Button("Clear Region Override"))
                    {
                        Undo.RecordObject(draft, "Clear MapGen Region Override");
                        draft.ClearRegionOverride(selectedRegionId);
                        EditorUtility.SetDirty(draft);
                        SetLastOperationResult($"Cleared region {selectedRegionId} override metadata.");
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(regionOverride.Locked || roomCount + connectorCount <= 0))
                    {
                        if (GUILayout.Button("Reroll Shape/Template / 형태·템플릿 재선택"))
                        {
                            RegenerateSelectedRegion("Rerolled shape/template");
                        }
                    }

                    using (new EditorGUI.DisabledScope(regionOverride.Locked || corridorCount + connectorCount <= 0))
                    {
                        if (GUILayout.Button("Reroute Connectors / 연결 재경로"))
                        {
                            RegenerateSelectedRegion("Rerouted connectors");
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Delete Region / 삭제"))
                    {
                        ChangeSelectedRegionState(MapGenCellState.Empty, "Deleted");
                        ClearSelection();
                    }

                    if (GUILayout.Button("Mark Blocked / 차단"))
                    {
                        ChangeSelectedRegionState(MapGenCellState.Blocked, "Marked blocked");
                    }

                    if (GUILayout.Button("Mark Reserved / 예약"))
                    {
                        ChangeSelectedRegionState(MapGenCellState.Reserved, "Marked reserved");
                    }
                }
            }
        }

        private void RegenerateSelectedRegion(string actionLabel)
        {
            if (draft == null || selectedRegionId < 0)
            {
                return;
            }

            Undo.RecordObject(draft, "Regenerate MapGen Room Region");
            draft.Seed = CreateRandomSeed();
            var report = draft.RegenerateRegionFromProfile(selectedRegionId);
            EditorUtility.SetDirty(draft);
            var preview = MapGenMockupPreviewData.FromDraft(draft);
            SetLastOperationResult(report.IsValid
                ? $"{actionLabel} for region {selectedRegionId}. Seed {draft.Seed}, Rooms {preview.Summary.RoomCells}, Corridors {preview.Summary.CorridorCells}."
                : $"Regenerate region failed: {report.Issues[0].Message}");
            Repaint();
        }

        private static string BuildPostProcessTags(MapGenMockupDraftAsset draft)
        {
            if (draft == null)
            {
                return "(none)";
            }

            var tags = new System.Collections.Generic.List<string>();
            if (draft.LastDirectRouteCellsAdded > 0)
            {
                tags.Add($"direct-route +{draft.LastDirectRouteCellsAdded}");
            }

            if (draft.LastDeadEndCorridorsRemoved > 0)
            {
                tags.Add($"dead-end-pruned {draft.LastDeadEndCorridorsRemoved}");
            }

            if (draft.LastIsolatedRoomsRemoved > 0)
            {
                tags.Add($"isolated-room-removed {draft.LastIsolatedRoomsRemoved}");
            }

            if (draft.LastEnclosedEmptyCellsFilled > 0)
            {
                tags.Add($"enclosed-fill {draft.LastEnclosedEmptyCellsFilled}");
            }

            if (draft.LastReservedMaskCellsFilled > 0)
            {
                tags.Add($"reserved-mask-fill {draft.LastReservedMaskCellsFilled}");
            }

            return tags.Count > 0 ? string.Join(", ", tags) : "(none)";
        }

        private static string DescribeRegionType(
            int roomCount,
            int corridorCount,
            int connectorCount,
            int blockedCount,
            int reservedCount)
        {
            if (roomCount > 0)
            {
                return connectorCount > 0 ? "Room with connectors / 방+커넥터" : "Room / 방";
            }

            if (corridorCount > 0)
            {
                return "Corridor / 복도";
            }

            if (blockedCount > 0)
            {
                return "Blocked / 차단";
            }

            if (reservedCount > 0)
            {
                return "Reserved / 예약";
            }

            return "Unknown / 알 수 없음";
        }

        private static string DescribeMaterializationHint(
            int roomCount,
            int corridorCount,
            int connectorCount,
            int blockedCount,
            int reservedCount)
        {
            if (roomCount + corridorCount + connectorCount > 0)
            {
                return "Navigable cells stamp floors/walls; connectors request doors. / 이동 가능 셀은 바닥/벽, 커넥터는 문 요청";
            }

            if (blockedCount + reservedCount > 0)
            {
                return "Non-navigable; skipped by current materialization/runtime bake. / 현재 출력과 런타임 베이크에서 제외";
            }

            return "No materialization output expected. / 출력 없음";
        }

        private void ChangeSelectedRegionState(MapGenCellState state, string actionLabel)
        {
            if (draft == null || selectedRegionId < 0)
            {
                return;
            }

            Undo.RecordObject(draft, $"MapGen {actionLabel} Region");
            draft.SetRegionState(selectedRegionId, state);
            EditorUtility.SetDirty(draft);
            SetLastOperationResult($"{actionLabel} region {selectedRegionId}. Accepted output is now stale until reaccepted.");
            Repaint();
        }

        private static void DrawCellLine(string label, MapGenMockupPreviewData previewData, bool hasCell, Vector2Int coord)
        {
            if (!hasCell || !previewData.TryGetCell(coord, out var cell))
            {
                EditorGUILayout.LabelField(label, "(none)");
                return;
            }

            var socketId = string.IsNullOrEmpty(cell.SocketId) ? "-" : cell.SocketId;
            var propChannel = string.IsNullOrEmpty(cell.PropChannel) ? "-" : cell.PropChannel;
            EditorGUILayout.LabelField(
                label,
                $"({coord.x}, {coord.y}) State {cell.State}, Region {cell.RegionId}, Category {cell.RoomCategory}, Socket {cell.SocketKind}/{socketId}, Prop {propChannel}");
        }

        private bool IsSelectedCellOrRegion(MapGenMockupCell cell, int x, int y)
        {
            if (!hasSelectedCell)
            {
                return false;
            }

            if (selectedRegionId >= 0)
            {
                return cell.RegionId == selectedRegionId;
            }

            return selectedCell.x == x && selectedCell.y == y;
        }

        private bool IsSelectedRegionConnector(MapGenMockupCell cell)
        {
            return selectedRegionId >= 0
                && cell.RegionId == selectedRegionId
                && cell.State == MapGenCellState.Connector;
        }

        private bool IsAdjacentRegionLink(MapGenMockupPreviewData previewData, MapGenMockupCell cell, int x, int y)
        {
            if (selectedRegionId < 0 || cell.RegionId == selectedRegionId || !IsLinkState(cell.State))
            {
                return false;
            }

            return HasSelectedNeighbor(previewData, x + 1, y)
                || HasSelectedNeighbor(previewData, x - 1, y)
                || HasSelectedNeighbor(previewData, x, y + 1)
                || HasSelectedNeighbor(previewData, x, y - 1);
        }

        private bool HasSelectedNeighbor(MapGenMockupPreviewData previewData, int x, int y)
        {
            return previewData.TryGetCell(x, y, out var neighbor)
                && neighbor.RegionId == selectedRegionId
                && IsLinkAnchorState(neighbor.State);
        }

        private int CountAdjacentRegionLinks(MapGenMockupPreviewData previewData)
        {
            if (selectedRegionId < 0)
            {
                return 0;
            }

            var count = 0;
            for (var y = 0; y < previewData.Height; y++)
            {
                for (var x = 0; x < previewData.Width; x++)
                {
                    if (!previewData.TryGetCell(x, y, out var cell))
                    {
                        continue;
                    }

                    if (IsAdjacentRegionLink(previewData, cell, x, y))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool IsLinkState(MapGenCellState state)
        {
            return state == MapGenCellState.Corridor || state == MapGenCellState.Connector;
        }

        private static bool IsLinkAnchorState(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }

        private void ClearSelection()
        {
            hasHoveredCell = false;
            hasSelectedCell = false;
            selectedRegionId = -1;
        }

        private static Color ColorForCell(MapGenCellState state)
        {
            switch (state)
            {
                case MapGenCellState.Room:
                    return RoomColor;
                case MapGenCellState.Corridor:
                case MapGenCellState.Wall:
                    return CorridorColor;
                case MapGenCellState.Blocked:
                    return BlockedColor;
                case MapGenCellState.Connector:
                    return ConnectorColor;
                case MapGenCellState.Reserved:
                    return ReservedColor;
                default:
                    return EmptyColor;
            }
        }

        private static Color ColorForProp(MapGenPlacedProp prop)
        {
            switch (prop.ChannelKind)
            {
                case MapGenPropPlacementChannelKind.Blocker:
                    return BlockerPropOverlayColor;
                case MapGenPropPlacementChannelKind.Objective:
                    return ObjectivePropOverlayColor;
                default:
                    return PropOverlayColor;
            }
        }
    }
}
