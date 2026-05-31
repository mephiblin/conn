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
                    return korean ? "새 드래프트 생성" : "Create Draft";
                case "mapgenv2.generateMockup":
                    return korean ? "지정한 시드로 생성" : "Generate From Seed";
                case "mapgenv2.regenerateMockup":
                    return korean ? "같은 시드 재생성" : "Regenerate Same Seed";
                case "mapgenv2.acceptMockup":
                    return korean ? "드래프트 저장" : "Save Draft";
                case "mapgenv2.materializeToScene":
                    return korean ? "씬 생성" : "Materialize To Scene";
                case "mapgenv2.rematerializeToScene":
                    return korean ? "씬 재생성" : "Rematerialize To Scene";
                case "mapgenv2.bakeRuntimeAsset":
                    return korean ? "런타임 베이크" : "Bake Runtime Asset";
                case "mapgenv2.rebakeRuntimeAsset":
                    return korean ? "런타임 재베이크" : "Rebake Runtime Asset";
                case "mapgenv2.profileMissing":
                    return korean ? "드래프트를 생성하거나 임포트하세요." : "Create or import a draft.";
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
        private enum MapGenDraftPaintTool
        {
            Select,
            Room,
            Corridor,
            Empty,
            Blocked,
            Reserved
        }

        private enum MapGenDraftPrefabSlot
        {
            RoomFloor,
            CorridorFloor,
            Wall,
            InsideCorner,
            OutsideCorner,
            Ceiling,
            Door,
            Blocker,
            Prop
        }

        public static readonly Vector2 MinimumWindowSize = new Vector2(420f, 620f);
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
        private const string SetupAssetsFoldoutKey = "Conn.MapGenV2.Window.SetupAssetsFoldout";
        private const string MapAssetsFoldoutKey = "Conn.MapGenV2.Window.MapAssetsFoldout";
        private const string OutputPathsFoldoutKey = "Conn.MapGenV2.Window.OutputPathsFoldout";
        private const string LinkedAssetsFoldoutKey = "Conn.MapGenV2.Window.LinkedAssetsFoldout";
        private const string MockupActionsFoldoutKey = "Conn.MapGenV2.Window.MockupActionsFoldout";
        private const string MockupPreviewFoldoutKey = "Conn.MapGenV2.Window.MockupPreviewFoldout";
        private const string DetailsFoldoutKey = "Conn.MapGenV2.Window.DetailsFoldout";
        private const string SceneOutputFoldoutKey = "Conn.MapGenV2.Window.SceneOutputFoldout";
        private const string PrimaryActionImpactFoldoutKey = "Conn.MapGenV2.Window.PrimaryActionImpactFoldout";
        private const string RuntimeBakeFoldoutKey = "Conn.MapGenV2.Window.RuntimeBakeFoldout";
        private const string SetupUtilitiesFoldoutKey = "Conn.MapGenV2.Window.SetupUtilitiesFoldout";
        private const string DraftUtilitiesFoldoutKey = "Conn.MapGenV2.Window.DraftUtilitiesFoldout";
        private const string SceneOutputUtilitiesFoldoutKey = "Conn.MapGenV2.Window.SceneOutputUtilitiesFoldout";
        private const string MockupTechnicalDetailsFoldoutKey = "Conn.MapGenV2.Window.MockupTechnicalDetailsFoldout";
        private const string RegionAdvancedEditFoldoutKey = "Conn.MapGenV2.Window.RegionAdvancedEditFoldout";
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
        private MapGenMockupDraftAsset importDraft;
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
        private bool showSetupAssetsFoldout = true;
        private bool showMapAssetsFoldout = true;
        private bool showOutputPathsFoldout = false;
        private bool showLinkedAssetsFoldout = false;
        private bool showMockupActionsFoldout = true;
        private bool showMockupPreviewFoldout = true;
        private bool showDetailsFoldout = true;
        private bool showSceneOutputFoldout = true;
        private bool showPrimaryActionImpactFoldout;
        private bool showRuntimeBakeFoldout = true;
        private bool showSetupUtilitiesFoldout;
        private bool showDraftUtilitiesFoldout;
        private bool showSceneOutputUtilitiesFoldout;
        private bool showMockupTechnicalDetailsFoldout;
        private bool showRegionAdvancedEditFoldout;
        private MapGenDraftPaintTool paintTool = MapGenDraftPaintTool.Select;
        private int paintStrokeRegionId = -1;
        private bool paintUndoRecorded;
        private int selectedPostProcessPassIndex;
        private MapGenPostProcessReport lastPostProcessReport;
        private string lastOperationResult = "아직 실행한 작업이 없습니다. / No operation has run yet.";
        private readonly MapGenV2PreviewTextureCache previewTextureCache = new MapGenV2PreviewTextureCache();

        private readonly struct StepBadge
        {
            public readonly string Label;
            public readonly bool Complete;
            public readonly bool Active;

            public StepBadge(string label, bool complete, bool active)
            {
                Label = label;
                Complete = complete;
                Active = active;
            }
        }

        [MenuItem("Conn/MapGenV2/Map Generator")]
        public static void Open()
        {
            GetWindow<MapGenV2Window>("MapGenV2");
        }

        public static void Open(MapGenProfileAsset selectedProfile, MapGenMockupDraftAsset selectedDraft)
        {
            var window = GetWindow<MapGenV2Window>("MapGenV2");
            window.draft = selectedDraft;
            window.profile = selectedDraft != null ? selectedDraft.Profile : selectedProfile;
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
            showSetupAssetsFoldout = EditorPrefs.GetBool(SetupAssetsFoldoutKey, showSetupAssetsFoldout);
            showMapAssetsFoldout = EditorPrefs.GetBool(MapAssetsFoldoutKey, showMapAssetsFoldout);
            showOutputPathsFoldout = EditorPrefs.GetBool(OutputPathsFoldoutKey, showOutputPathsFoldout);
            showLinkedAssetsFoldout = EditorPrefs.GetBool(LinkedAssetsFoldoutKey, showLinkedAssetsFoldout);
            showMockupActionsFoldout = EditorPrefs.GetBool(MockupActionsFoldoutKey, showMockupActionsFoldout);
            showMockupPreviewFoldout = EditorPrefs.GetBool(MockupPreviewFoldoutKey, showMockupPreviewFoldout);
            showDetailsFoldout = EditorPrefs.GetBool(DetailsFoldoutKey, showDetailsFoldout);
            showSceneOutputFoldout = EditorPrefs.GetBool(SceneOutputFoldoutKey, showSceneOutputFoldout);
            showPrimaryActionImpactFoldout = EditorPrefs.GetBool(PrimaryActionImpactFoldoutKey, showPrimaryActionImpactFoldout);
            showRuntimeBakeFoldout = EditorPrefs.GetBool(RuntimeBakeFoldoutKey, showRuntimeBakeFoldout);
            showSetupUtilitiesFoldout = EditorPrefs.GetBool(SetupUtilitiesFoldoutKey, showSetupUtilitiesFoldout);
            showDraftUtilitiesFoldout = EditorPrefs.GetBool(DraftUtilitiesFoldoutKey, showDraftUtilitiesFoldout);
            showSceneOutputUtilitiesFoldout = EditorPrefs.GetBool(SceneOutputUtilitiesFoldoutKey, showSceneOutputUtilitiesFoldout);
            showMockupTechnicalDetailsFoldout = EditorPrefs.GetBool(MockupTechnicalDetailsFoldoutKey, showMockupTechnicalDetailsFoldout);
            showRegionAdvancedEditFoldout = EditorPrefs.GetBool(RegionAdvancedEditFoldoutKey, showRegionAdvancedEditFoldout);
            profile = LoadAssetFromEditorPrefs<MapGenProfileAsset>(ProfilePathKey);
            draft = LoadAssetFromEditorPrefs<MapGenMockupDraftAsset>(DraftPathKey);
            if (draft != null)
            {
                profile = draft.Profile;
            }
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
            DrawInspectorWorkspace(workflow);
            EditorGUILayout.EndScrollView();
        }

        public static string BuildInspectorLayoutSummary()
        {
            return "Draft-centered authoring layout: draft file create/import/save, map prefab slots, seed controls, preview/drawing, output, diagnostics, and advanced utility sections are stacked vertically with foldout sections and responsive wrapping for narrow inspector widths.";
        }

        public static string BuildEditorTechnologySummary()
        {
            return "Editor technology policy: new Scene View overlay uses UI Toolkit; existing MapGenV2 window and inspectors stay IMGUI because their maintained code path already owns preview drawing, Undo, and serialized inspector workflows.";
        }

        public static string BuildInlineHelpCoverageSummary()
        {
            return "Inline help/tooltips cover the draft file, map prefab slots, seed controls, preview drawing, connectors, post-process, prop placement, draft output settings, bake settings, materialization output, and legacy compatibility details.";
        }

        public static string BuildKoreanCoverageSummary()
        {
            return "Korean UI coverage: labels, buttons, tooltips, warnings, errors, validation summaries, documentation summaries, and workflow result text use Korean/English bilingual wording while ids/enums/API names remain English.";
        }

        private void DrawInspectorWorkspace(MapGenV2WorkflowStatus workflow)
        {
            DrawFoldoutSection("드래프트 파일 / Draft File", ref showSetupAssetsFoldout, DrawDraftFileSection);
            DrawFoldoutSection("맵 에셋 / Map Assets", ref showMapAssetsFoldout, DrawMapAssetSection);
            DrawFoldoutSection("시드 / Seed", ref showMockupActionsFoldout, () => DrawSeedSection(workflow));
            DrawFoldoutSection("프리뷰 & 드로잉 / Preview & Drawing", ref showMockupPreviewFoldout, DrawDraftSummaryAndPreview);
            DrawFoldoutSection("출력 / Output", ref showSceneOutputFoldout, () =>
            {
                DrawSceneOutputControls(workflow);
                DrawRuntimeBakeControls(workflow);
            });
            DrawFoldoutSection("상세/진단 / Details & Diagnostics", ref showDetailsFoldout, () =>
            {
                DrawNextAction(workflow);
                DrawProfileValidation();
                DrawDiagnosticsPanel();
            });
            DrawFoldoutSection("레거시 호환 / Legacy Compatibility", ref showOutputPathsFoldout, () =>
            {
                DrawLegacyReferenceFields();
                DrawLinkedAssetShortcuts();
            });
        }

        private void DrawFoldoutSection(string title, ref bool expanded, System.Action drawContent)
        {
            EditorGUI.BeginChangeCheck();
            expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
            if (EditorGUI.EndChangeCheck())
            {
                SaveWindowState();
            }

            if (!expanded)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                drawContent();
            }
        }

        private void DrawDraftFileSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("기본 파일 / Primary File", EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("드래프트 / Draft", draft, typeof(MapGenMockupDraftAsset), false);
                importDraft = (MapGenMockupDraftAsset)EditorGUILayout.ObjectField("임포트 항목 / Import", importDraft, typeof(MapGenMockupDraftAsset), false);

                if (GUILayout.Button("새 드래프트 생성 / Create Draft"))
                {
                    CreateDraft();
                    SaveWindowState();
                }

                using (new EditorGUI.DisabledScope(importDraft == null))
                {
                    if (GUILayout.Button("드래프트 임포트 / Import Draft"))
                    {
                        draft = importDraft;
                        profile = draft != null ? draft.Profile : null;
                        var moduleSet = GetActiveModuleSet();
                        EnsureDraftPrefabPalette(moduleSet);
                        if (draft != null && draft.PrefabPalette != null)
                        {
                            EditorUtility.SetDirty(draft);
                        }

                        selectedMaterializedRoot = null;
                        ClearSelection();
                        Selection.activeObject = draft;
                        AssetDatabase.SaveAssets();
                        SaveWindowState();
                        SetLastOperationResult(draft != null
                            ? $"Imported draft: {draft.name}."
                            : "Import draft failed.");
                    }
                }

                using (new EditorGUI.DisabledScope(draft == null))
                {
                    if (GUILayout.Button("드래프트 저장 / Save Draft"))
                    {
                        SaveCurrentDraft();
                    }
                }

                DrawFoldoutSection("레거시 설정 유틸리티 / Legacy Setup Utilities", ref showSetupUtilitiesFoldout, () =>
                {
                    if (GUILayout.Button(MapGenV2EditorText.Get("mapgenv2.createDefaultFolders")))
                    {
                        MapGenV2AssetFolderUtility.CreateDefaultFolders();
                        SetLastOperationResult("기본 MapGenV2 폴더 준비 완료 / Default MapGenV2 folders created or already existed.");
                    }

                    if (GUILayout.Button("스타터 생성 에셋 정리 / Cleanup Starter Generated Assets"))
                    {
                        var deleted = MapGenV2StarterSetupBuilder.CleanupStarterGeneratedAssets();
                        SetLastOperationResult($"스타터 생성 에셋 정리 완료 / Starter cleanup deleted {deleted} assets.");
                    }
                });
            }
        }

        private void DrawMapAssetSection()
        {
            if (draft == null)
            {
                DrawWrappingHelpBox("먼저 드래프트를 생성하거나 임포트하세요. / Create or import a draft first.", MessageType.Info);
                return;
            }

            var moduleSet = GetActiveModuleSet();
            EnsureDraftPrefabPalette(moduleSet);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("프리팹 슬롯 / Prefab Slots", EditorStyles.boldLabel);
                DrawPrefabSlot(MapGenDraftPrefabSlot.RoomFloor);
                DrawPrefabSlot(MapGenDraftPrefabSlot.CorridorFloor);
                DrawPrefabSlot(MapGenDraftPrefabSlot.Wall);
                DrawPrefabSlot(MapGenDraftPrefabSlot.InsideCorner);
                DrawPrefabSlot(MapGenDraftPrefabSlot.OutsideCorner);
                DrawPrefabSlot(MapGenDraftPrefabSlot.Ceiling);
                DrawPrefabSlot(MapGenDraftPrefabSlot.Door);
                DrawPrefabSlot(MapGenDraftPrefabSlot.Blocker);
                DrawPrefabSlot(MapGenDraftPrefabSlot.Prop);

                using (new EditorGUI.DisabledScope(moduleSet == null))
                {
                    if (GUILayout.Button("레거시 기본값에서 빈 슬롯 채우기 / Fill Empty Slots From Legacy Defaults"))
                    {
                        RefreshDraftAssets(moduleSet);
                    }
                }
            }
        }

        private void DrawSeedSection(MapGenV2WorkflowStatus workflow)
        {
            if (draft == null)
            {
                DrawWrappingHelpBox("먼저 드래프트를 생성하거나 임포트하세요. / Create or import a draft first.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                var nextSeed = EditorGUILayout.IntField("시드 / Seed", draft.Seed);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(draft, "Edit MapGen Draft Seed");
                    draft.Seed = nextSeed;
                    EditorUtility.SetDirty(draft);
                    SaveWindowState();
                }

                if (GUILayout.Button("랜덤 시드 입력 / Fill Random Seed"))
                {
                    Undo.RecordObject(draft, "Randomize MapGen Draft Seed");
                    draft.Seed = CreateRandomSeed();
                    EditorUtility.SetDirty(draft);
                    SaveWindowState();
                    SetLastOperationResult($"Random seed filled: {draft.Seed}. Generate with the seed when ready.");
                }

                using (new EditorGUI.DisabledScope(!workflow.CanGenerate))
                {
                    if (GUILayout.Button("지정한 시드로 생성 / Generate From Seed"))
                    {
                        GenerateMockup("Generate From Seed");
                    }
                }

                DrawFoldoutSection("고급 생성 작업 / Advanced Generation", ref showDraftUtilitiesFoldout, () =>
                {
                    using (new EditorGUI.DisabledScope(!workflow.CanGenerate))
                    {
                        if (GUILayout.Button("같은 시드 재생성 / Regenerate Same Seed"))
                        {
                            GenerateMockup("Regenerate Same Seed", preserveLockedRegions: true);
                        }
                    }

                    using (new EditorGUI.DisabledScope(!workflow.CanPostProcess))
                    {
                        if (GUILayout.Button("후처리 재실행 / Repostprocess Mockup"))
                        {
                            RunPostProcess();
                        }
                    }

                    if (GUILayout.Button("현재 드래프트 비우기 / Clear Current Draft"))
                    {
                        Undo.RecordObject(draft, "Clear MapGen Draft");
                        draft.ClearDraft();
                        ClearSelection();
                        EditorUtility.SetDirty(draft);
                        SetLastOperationResult("Cleared current draft cells, generated signature, post-process report, and save state.");
                    }
                });
            }
        }

        private void DrawPrefabSlot(MapGenDraftPrefabSlot slot)
        {
            var currentPrefab = GetDraftPrefab(slot, null);
            EditorGUI.BeginChangeCheck();
            var nextPrefab = (GameObject)EditorGUILayout.ObjectField(
                LabelFor(slot),
                currentPrefab,
                typeof(GameObject),
                false);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(draft, "Change MapGen Draft Prefab Slot");
            SetDraftPrefab(slot, nextPrefab);
            EditorUtility.SetDirty(draft);
            SetLastOperationResult($"Updated {slot} prefab mapping. Seed remains {draft?.Seed.ToString() ?? "-"}.");
        }

        private MapGenModuleSetAsset GetActiveModuleSet()
        {
            return draft != null
                && !draft.EmbeddedSourceImported
                && draft.Profile != null
                && draft.Profile.StyleSet != null
                    ? draft.Profile.StyleSet.ModuleSet
                    : null;
        }

        private void EnsureDraftPrefabPalette(MapGenModuleSetAsset moduleSet)
        {
            if (draft == null)
            {
                return;
            }

            draft.PrefabPalette ??= new MapGenDraftPrefabPalette();
            if (!draft.PrefabPalette.HasAnyPrefab() && moduleSet != null)
            {
                CaptureModuleSetPrefabPalette(moduleSet);
                if (draft.PrefabPalette.HasAnyPrefab())
                {
                    EditorUtility.SetDirty(draft);
                }
            }
        }

        private void CaptureModuleSetPrefabPalette(MapGenModuleSetAsset moduleSet)
        {
            if (draft == null || moduleSet == null)
            {
                return;
            }

            draft.PrefabPalette.RoomFloor = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.RoomFloor);
            draft.PrefabPalette.CorridorFloor = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.CorridorFloor);
            draft.PrefabPalette.Wall = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Wall);
            draft.PrefabPalette.InsideCorner = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.InsideCorner);
            draft.PrefabPalette.OutsideCorner = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.OutsideCorner);
            draft.PrefabPalette.Ceiling = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Ceiling);
            draft.PrefabPalette.Door = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Door);
            draft.PrefabPalette.Blocker = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Blocker);
            draft.PrefabPalette.Prop = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Prop);
        }

        private void FillEmptyDraftPrefabSlotsFromModuleSet(MapGenModuleSetAsset moduleSet)
        {
            if (draft == null || moduleSet == null)
            {
                return;
            }

            draft.PrefabPalette ??= new MapGenDraftPrefabPalette();
            if (draft.PrefabPalette.RoomFloor == null)
            {
                draft.PrefabPalette.RoomFloor = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.RoomFloor);
            }

            if (draft.PrefabPalette.CorridorFloor == null)
            {
                draft.PrefabPalette.CorridorFloor = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.CorridorFloor);
            }

            if (draft.PrefabPalette.Wall == null)
            {
                draft.PrefabPalette.Wall = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Wall);
            }

            if (draft.PrefabPalette.InsideCorner == null)
            {
                draft.PrefabPalette.InsideCorner = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.InsideCorner);
            }

            if (draft.PrefabPalette.OutsideCorner == null)
            {
                draft.PrefabPalette.OutsideCorner = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.OutsideCorner);
            }

            if (draft.PrefabPalette.Ceiling == null)
            {
                draft.PrefabPalette.Ceiling = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Ceiling);
            }

            if (draft.PrefabPalette.Door == null)
            {
                draft.PrefabPalette.Door = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Door);
            }

            if (draft.PrefabPalette.Blocker == null)
            {
                draft.PrefabPalette.Blocker = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Blocker);
            }

            if (draft.PrefabPalette.Prop == null)
            {
                draft.PrefabPalette.Prop = GetQuickPrefab(moduleSet, MapGenDraftPrefabSlot.Prop);
            }
        }

        private GameObject GetDraftPrefab(MapGenDraftPrefabSlot slot, MapGenModuleSetAsset moduleSet)
        {
            if (draft?.PrefabPalette == null)
            {
                return GetQuickPrefab(moduleSet, slot);
            }

            return slot switch
            {
                MapGenDraftPrefabSlot.RoomFloor => draft.PrefabPalette.RoomFloor,
                MapGenDraftPrefabSlot.CorridorFloor => draft.PrefabPalette.CorridorFloor,
                MapGenDraftPrefabSlot.Wall => draft.PrefabPalette.Wall,
                MapGenDraftPrefabSlot.InsideCorner => draft.PrefabPalette.InsideCorner,
                MapGenDraftPrefabSlot.OutsideCorner => draft.PrefabPalette.OutsideCorner,
                MapGenDraftPrefabSlot.Ceiling => draft.PrefabPalette.Ceiling,
                MapGenDraftPrefabSlot.Door => draft.PrefabPalette.Door,
                MapGenDraftPrefabSlot.Blocker => draft.PrefabPalette.Blocker,
                MapGenDraftPrefabSlot.Prop => draft.PrefabPalette.Prop,
                _ => GetQuickPrefab(moduleSet, slot)
            };
        }

        private void SetDraftPrefab(MapGenDraftPrefabSlot slot, GameObject prefab)
        {
            if (draft == null)
            {
                return;
            }

            draft.PrefabPalette ??= new MapGenDraftPrefabPalette();
            switch (slot)
            {
                case MapGenDraftPrefabSlot.RoomFloor:
                    draft.PrefabPalette.RoomFloor = prefab;
                    break;
                case MapGenDraftPrefabSlot.CorridorFloor:
                    draft.PrefabPalette.CorridorFloor = prefab;
                    break;
                case MapGenDraftPrefabSlot.Wall:
                    draft.PrefabPalette.Wall = prefab;
                    break;
                case MapGenDraftPrefabSlot.InsideCorner:
                    draft.PrefabPalette.InsideCorner = prefab;
                    break;
                case MapGenDraftPrefabSlot.OutsideCorner:
                    draft.PrefabPalette.OutsideCorner = prefab;
                    break;
                case MapGenDraftPrefabSlot.Ceiling:
                    draft.PrefabPalette.Ceiling = prefab;
                    break;
                case MapGenDraftPrefabSlot.Door:
                    draft.PrefabPalette.Door = prefab;
                    break;
                case MapGenDraftPrefabSlot.Blocker:
                    draft.PrefabPalette.Blocker = prefab;
                    break;
                case MapGenDraftPrefabSlot.Prop:
                    draft.PrefabPalette.Prop = prefab;
                    break;
            }

            SetDraftModuleEntry(slot, prefab);
        }

        private void SetDraftModuleEntry(MapGenDraftPrefabSlot slot, GameObject prefab)
        {
            if (draft == null)
            {
                return;
            }

            draft.ModuleData ??= new MapGenDraftModuleSet();
            var entries = prefab != null
                ? new[]
                {
                    new MapGenModuleEntry
                    {
                        Prefab = prefab,
                        Weight = 1,
                        Footprint = Vector2Int.one,
                        RotationPolicy = RotationPolicyFor(slot)
                    }
                }
                : System.Array.Empty<MapGenModuleEntry>();

            switch (slot)
            {
                case MapGenDraftPrefabSlot.RoomFloor:
                    draft.ModuleData.FloorsA = entries;
                    break;
                case MapGenDraftPrefabSlot.CorridorFloor:
                    draft.ModuleData.FloorsB = entries;
                    break;
                case MapGenDraftPrefabSlot.Wall:
                    draft.ModuleData.WallsStraight = entries;
                    break;
                case MapGenDraftPrefabSlot.InsideCorner:
                    draft.ModuleData.WallsCornerInside = entries;
                    break;
                case MapGenDraftPrefabSlot.OutsideCorner:
                    draft.ModuleData.WallsCornerOutside = entries;
                    break;
                case MapGenDraftPrefabSlot.Ceiling:
                    draft.ModuleData.InteriorCeilings = entries;
                    draft.ModuleData.ExteriorCeilings = entries;
                    break;
                case MapGenDraftPrefabSlot.Door:
                    draft.ModuleData.WholeDoors = entries;
                    draft.ModuleData.HalfDoorFrames = entries;
                    draft.ModuleData.HalfDoorPanels = entries;
                    break;
                case MapGenDraftPrefabSlot.Blocker:
                    draft.ModuleData.Blockers = entries;
                    break;
                case MapGenDraftPrefabSlot.Prop:
                    draft.ModuleData.PropCategories = entries;
                    break;
            }
        }

        private static MapGenModuleRotationPolicy RotationPolicyFor(MapGenDraftPrefabSlot slot)
        {
            return slot == MapGenDraftPrefabSlot.Wall
                || slot == MapGenDraftPrefabSlot.InsideCorner
                || slot == MapGenDraftPrefabSlot.OutsideCorner
                || slot == MapGenDraftPrefabSlot.Door
                    ? MapGenModuleRotationPolicy.AnyOrthogonal
                    : MapGenModuleRotationPolicy.None;
        }

        private void RefreshDraftAssets(MapGenModuleSetAsset moduleSet)
        {
            if (draft == null || moduleSet == null)
            {
                return;
            }

            EnsureDraftPrefabPalette(moduleSet);
            Undo.RecordObject(draft, "Refresh MapGen Draft Asset Mapping");
            FillEmptyDraftPrefabSlotsFromModuleSet(moduleSet);
            SyncDraftModuleDataFromPalette();
            EditorUtility.SetDirty(draft);
            AssetDatabase.SaveAssets();
            SetLastOperationResult($"Empty draft prefab slots filled without changing seed {draft?.Seed.ToString() ?? "-"}.");
        }

        private void SyncDraftModuleDataFromPalette()
        {
            if (draft?.PrefabPalette == null)
            {
                return;
            }

            SetDraftModuleEntry(MapGenDraftPrefabSlot.RoomFloor, draft.PrefabPalette.RoomFloor);
            SetDraftModuleEntry(MapGenDraftPrefabSlot.CorridorFloor, draft.PrefabPalette.CorridorFloor);
            SetDraftModuleEntry(MapGenDraftPrefabSlot.Wall, draft.PrefabPalette.Wall);
            SetDraftModuleEntry(MapGenDraftPrefabSlot.InsideCorner, draft.PrefabPalette.InsideCorner);
            SetDraftModuleEntry(MapGenDraftPrefabSlot.OutsideCorner, draft.PrefabPalette.OutsideCorner);
            SetDraftModuleEntry(MapGenDraftPrefabSlot.Ceiling, draft.PrefabPalette.Ceiling);
            SetDraftModuleEntry(MapGenDraftPrefabSlot.Door, draft.PrefabPalette.Door);
            SetDraftModuleEntry(MapGenDraftPrefabSlot.Blocker, draft.PrefabPalette.Blocker);
            SetDraftModuleEntry(MapGenDraftPrefabSlot.Prop, draft.PrefabPalette.Prop);
        }

        private void SaveCurrentDraft()
        {
            if (draft == null)
            {
                return;
            }

            Undo.RecordObject(draft, "Save MapGen Draft");
            var canMarkSceneSource = MapGenV2WorkflowStatus.From(profile, draft).CanAccept;
            if (canMarkSceneSource)
            {
                draft.Accept();
            }

            EditorUtility.SetDirty(draft);
            AssetDatabase.SaveAssets();
            SetLastOperationResult(canMarkSceneSource
                ? $"Saved draft: {AssetDatabase.GetAssetPath(draft)}."
                : $"Saved draft settings: {AssetDatabase.GetAssetPath(draft)}. Generate or draw a map before scene output.");
        }

        private static string LabelFor(MapGenDraftPrefabSlot slot)
        {
            return slot switch
            {
                MapGenDraftPrefabSlot.RoomFloor => "방 바닥 / Room Floor",
                MapGenDraftPrefabSlot.CorridorFloor => "복도 바닥 / Corridor Floor",
                MapGenDraftPrefabSlot.Wall => "벽 / Wall",
                MapGenDraftPrefabSlot.InsideCorner => "안쪽 코너 / Inside Corner",
                MapGenDraftPrefabSlot.OutsideCorner => "바깥 코너 / Outside Corner",
                MapGenDraftPrefabSlot.Ceiling => "천장 / Ceiling",
                MapGenDraftPrefabSlot.Door => "문 / Door",
                MapGenDraftPrefabSlot.Blocker => "차단물 / Blocker",
                MapGenDraftPrefabSlot.Prop => "장식 프롭 / Prop",
                _ => slot.ToString()
            };
        }

        private static GameObject GetQuickPrefab(MapGenModuleSetAsset moduleSet, MapGenDraftPrefabSlot slot)
        {
            if (moduleSet == null)
            {
                return null;
            }

            return slot switch
            {
                MapGenDraftPrefabSlot.RoomFloor => FirstPrefab(moduleSet.FloorsA),
                MapGenDraftPrefabSlot.CorridorFloor => FirstPrefab(moduleSet.FloorsB),
                MapGenDraftPrefabSlot.Wall => FirstPrefab(moduleSet.WallsStraight),
                MapGenDraftPrefabSlot.InsideCorner => FirstPrefab(moduleSet.WallsCornerInside),
                MapGenDraftPrefabSlot.OutsideCorner => FirstPrefab(moduleSet.WallsCornerOutside),
                MapGenDraftPrefabSlot.Ceiling => FirstPrefab(moduleSet.InteriorCeilings) != null
                    ? FirstPrefab(moduleSet.InteriorCeilings)
                    : FirstPrefab(moduleSet.ExteriorCeilings),
                MapGenDraftPrefabSlot.Door => FirstPrefab(moduleSet.WholeDoors),
                MapGenDraftPrefabSlot.Blocker => FirstPrefab(moduleSet.Blockers),
                MapGenDraftPrefabSlot.Prop => FirstPrefab(moduleSet.PropCategories),
                _ => null
            };
        }

        private static GameObject FirstPrefab(MapGenModuleEntry[] entries)
        {
            return entries != null && entries.Length > 0 && entries[0] != null ? entries[0].Prefab : null;
        }

        private void DrawWorkflowStatus(MapGenV2WorkflowStatus workflow)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("워크플로우 상태 / Workflow Status", EditorStyles.boldLabel);
                DrawWorkflowStepBadges(workflow);

                var draftName = draft != null ? draft.name : "(none)";
                var seed = draft != null ? draft.Seed : (int?)null;
                var validationState = workflow.HasProfile ? (workflow.ProfileValid ? "Valid" : "Invalid") : "(none)";
                var savedState = !workflow.HasGeneratedMockup
                    ? "No map yet"
                    : workflow.Accepted
                        ? workflow.AcceptedCurrent ? "Saved current" : "Changed since save"
                        : "Needs save";
                var materializedRoot = ResolveMaterializedRoot();
                var bakedAsset = LoadExpectedBakedAsset();
                DrawWrappedReadOnlyLine(
                    "현재 / Current",
                    BuildTopStatusStripText(
                        profile != null ? profile.ProfileId : "(none)",
                        draftName,
                        seed,
                        validationState,
                        savedState,
                        materializedRoot != null ? materializedRoot.name : "(none)",
                        bakedAsset != null ? AssetDatabase.GetAssetPath(bakedAsset) : "(none)"));
                DrawWrappedReadOnlyLine("마지막 작업 / Last Result", lastOperationResult);
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
            return $"Draft {NullLabel(draftName)}, Seed {(seed.HasValue ? seed.Value.ToString() : "-")}, "
                + $"Setup {NullLabel(validationState)}, Save State {NullLabel(acceptedState)}, "
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
            return "Primary mockup UX: MapGenV2 window supports Generate, Post-Process, Save Draft, selected-region inspect/edit, "
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

        private void DrawWorkflowStepBadges(MapGenV2WorkflowStatus workflow)
        {
            var materializedRoot = ResolveMaterializedRoot();
            var badges = new[]
            {
                new StepBadge("Draft", workflow.HasDraft && workflow.ProfileValid, !workflow.HasDraft || !workflow.ProfileValid),
                new StepBadge("Generate", workflow.HasGeneratedMockup, workflow.CanGenerate && !workflow.HasGeneratedMockup),
                new StepBadge("Edit", workflow.HasGeneratedMockup, workflow.CanPostProcess && !workflow.Accepted),
                new StepBadge("Save", workflow.Accepted && workflow.AcceptedCurrent, workflow.CanAccept && (!workflow.Accepted || !workflow.AcceptedCurrent)),
                new StepBadge("Scene", materializedRoot != null, workflow.CanMaterialize),
                new StepBadge("Bake", LoadExpectedBakedAsset() != null, workflow.CanBakeRuntime)
            };
            var availableWidth = Mathf.Max(180f, EditorGUIUtility.currentViewWidth - 36f);
            var badgeWidth = availableWidth < 520f ? 118f : 96f;
            var columns = Mathf.Clamp(Mathf.FloorToInt(availableWidth / badgeWidth), 1, badges.Length);

            for (var index = 0; index < badges.Length; index += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var rowEnd = Mathf.Min(index + columns, badges.Length);
                    for (var badgeIndex = index; badgeIndex < rowEnd; badgeIndex++)
                    {
                        var badge = badges[badgeIndex];
                        DrawStepBadge(badge.Label, badge.Complete, badge.Active, badgeWidth - 4f);
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private static void DrawStepBadge(string label, bool complete, bool active, float width)
        {
            var previousColor = GUI.backgroundColor;
            var status = complete
                ? MapGenV2StatusKind.Accepted
                : active ? MapGenV2StatusKind.Stale : MapGenV2StatusKind.Pending;
            GUI.backgroundColor = MapGenV2StatusPresentation.For(status).Color;
            GUILayout.Label(label, EditorStyles.miniButton, GUILayout.Width(width));
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

        private void DrawLegacyReferenceFields()
        {
            DrawWrappingHelpBox(
                "일반 제작 플로우는 Draft만 선택합니다. 아래 내부 설정 링크는 기존 에셋 호환/복구가 필요할 때만 사용하세요.",
                MessageType.Info);
            EditorGUI.BeginChangeCheck();
            draft = (MapGenMockupDraftAsset)EditorGUILayout.ObjectField("드래프트 / Draft", draft, typeof(MapGenMockupDraftAsset), false);
            profile = (MapGenProfileAsset)EditorGUILayout.ObjectField("내부 설정 / Internal Profile", profile, typeof(MapGenProfileAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (draft != null)
                {
                    profile = draft.Profile;
                }

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
            var height = Mathf.Max(38f, style.CalcHeight(content, Mathf.Max(220f, EditorGUIUtility.currentViewWidth - 36f)));
            EditorGUILayout.LabelField(content, style, GUILayout.MinHeight(height));
        }

        private static void DrawWrappedReadOnlyLine(string label, string value)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                richText = true
            };
            var content = EditorGUIUtility.TrTextContent(value ?? string.Empty);
            var width = Mathf.Max(220f, EditorGUIUtility.currentViewWidth - 36f);
            var height = Mathf.Max(24f, style.CalcHeight(content, width));
            EditorGUILayout.LabelField(content, style, GUILayout.MinHeight(height));
        }

        private void DrawNextAction(MapGenV2WorkflowStatus workflow)
        {
            DrawWrappingHelpBox($"다음 작업 / Next Action: {BuildGuidedNextAction(workflow)}", MessageType.Info);
        }

        private void DrawProfileValidation()
        {
            if (draft != null)
            {
                MapGenValidationReportEditorGUI.Draw(draft.ValidateDraftSource(), draft, "Draft setup is valid.");
                return;
            }

            if (profile == null)
            {
                EditorGUILayout.HelpBox(MapGenV2EditorText.Get("mapgenv2.profileMissing"), MessageType.Info);
                return;
            }

            MapGenValidationReportEditorGUI.Draw(profile.Validate(), profile, "Draft setup is valid.");
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
                var postProcessRules = draft.GetPostProcessRules();
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

            if (draft != null
                && draft.Accepted
                && draft.IsAcceptedSignatureCurrent)
            {
                var moduleSet = GetActiveModuleSet();
                var materializationPlan = MapGenMockupMaterializer.BuildPlan(draft);
                var materializationReport = MapGenMockupMaterializer.BuildReport(
                    moduleSet,
                    materializationPlan,
                    draft.Seed,
                    draft);
                report.AddRange(MapGenMockupMaterializer.ValidateCoverage(materializationReport), moduleSet);
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
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("드래프트 폴더 / Draft Folder", GetDraftFolder());
                EditorGUILayout.LabelField("드래프트 에셋 / Draft Asset", draft != null ? AssetDatabase.GetAssetPath(draft) : "(none)");
                EditorGUILayout.LabelField("Materialized Prefab 폴더 / Materialized Prefab Folder", GetMaterializedPrefabFolder());
                EditorGUILayout.LabelField("베이크 에셋 / Baked Asset", BuildExpectedBakedAssetPath());
                EditorGUILayout.LabelField(
                    "출력 설정 소스 / Output Settings Source",
                    MapGenV2DraftOutputSettingsUtility.HasDraftOutputSettings(draft)
                        ? "Draft.OutputSettings"
                        : "Legacy profile/default compatibility");
            }
        }

        private void DrawLinkedAssetShortcuts()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                EditorGUILayout.LabelField(BuildLinkedAssetShortcutSummary(), style);
                DrawObjectShortcutRow("드래프트 / Draft", draft, profile != null ? CreateDraft : null);
                DrawObjectShortcutRow("씬 출력 루트 / Materialized Root", selectedMaterializedRoot);
                DrawObjectShortcutRow("베이크 에셋 / Baked Asset", LoadExpectedBakedAsset());

                if (profile == null)
                {
                    return;
                }

                DrawFoldoutSection("내부 에셋 링크 / Internal Legacy Asset Links", ref showLinkedAssetsFoldout, () =>
                {
                    DrawObjectShortcutRow("내부 설정 / Internal Profile", profile, CreateStarterSetupFromShortcut);
                    DrawObjectShortcutRow("규칙 세트 / Rule Set", profile.LayoutRules);
                    DrawObjectShortcutRow("스타일 세트 / Style Set", profile.StyleSet);
                    DrawObjectShortcutRow("모듈 세트 / Module Set", profile.StyleSet != null ? profile.StyleSet.ModuleSet : null);

                    var roomShapes = profile.RoomShapes ?? System.Array.Empty<MapGenRoomShapeAsset>();
                    for (var i = 0; i < roomShapes.Length; i++)
                    {
                        DrawObjectShortcutRow($"룸 셰이프 / Room Shape {i}", roomShapes[i]);
                    }

                    if (profile.StyleSet != null)
                    {
                        var roomTemplates = profile.StyleSet.RoomTemplates ?? System.Array.Empty<MapGenRoomTemplateAsset>();
                        for (var i = 0; i < roomTemplates.Length; i++)
                        {
                            DrawObjectShortcutRow($"방 템플릿 / Room Template {i}", roomTemplates[i]);
                        }

                        var corridorTemplates = profile.StyleSet.CorridorTemplates ?? System.Array.Empty<MapGenCorridorTemplateAsset>();
                        for (var i = 0; i < corridorTemplates.Length; i++)
                        {
                            DrawObjectShortcutRow($"복도 템플릿 / Corridor Template {i}", corridorTemplates[i]);
                        }
                    }
                });
            }
        }

        public static string BuildLinkedAssetShortcutSummary()
        {
            return "연결 에셋 작업: Draft, scene root, baked asset은 바로 위치 표시(Ping), 선택(Select), 열기(Open), 생성(Create), 복제(Duplicate), 검증(Validate), 누락 생성(Fix/Create Missing)을 제공합니다. "
                + "Profile/Rule/Style/Module 링크는 내부 에셋 링크 foldout에 숨겨진 legacy compatibility 도구입니다.";
        }

        public static string BuildUndoCoverageSummary()
        {
            return "Undo/Redo coverage: serialized inspector asset edits, room-shape paint/resize/rotate/flip, "
                + "draft generate/post-process/save/clear, selected-region category/lock/regenerate/state edits, "
                + "draft output settings, and scene materialization create/update/clear via Undo object creation/destruction.";
        }

        private void DrawObjectShortcutRow(string label, Object target, System.Action createAction = null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(label, target, typeof(Object), false);
                using (new EditorGUI.DisabledScope(createAction == null || target != null))
                {
                    if (GUILayout.Button(new GUIContent("생성 / Create", "누락된 기본 에셋을 생성합니다."), GUILayout.Width(88f)))
                    {
                        createAction();
                    }
                }

                using (new EditorGUI.DisabledScope(target == null))
                {
                    if (GUILayout.Button(new GUIContent("위치 / Ping", "Project 또는 Hierarchy에서 에셋 위치를 표시합니다."), GUILayout.Width(78f)))
                    {
                        EditorGUIUtility.PingObject(target);
                    }

                    if (GUILayout.Button(new GUIContent("선택 / Select", "현재 Selection으로 지정합니다."), GUILayout.Width(92f)))
                    {
                        Selection.activeObject = target;
                    }

                    if (GUILayout.Button(new GUIContent("열기 / Open", "Inspector 또는 에셋 편집기를 엽니다."), GUILayout.Width(82f)))
                    {
                        AssetDatabase.OpenAsset(target);
                    }

                    using (new EditorGUI.DisabledScope(!CanDuplicateShortcutTarget(target)))
                    {
                        if (GUILayout.Button(new GUIContent("복제 / Duplicate", "Project 에셋을 같은 폴더에 복제합니다."), GUILayout.Width(112f)))
                        {
                            DuplicateShortcutTarget(target);
                        }
                    }

                    if (GUILayout.Button(new GUIContent("검증 / Validate", "선택한 MapGenV2 에셋의 검증 결과를 요약합니다."), GUILayout.Width(106f)))
                    {
                        var report = ValidateShortcutTarget(target);
                        var summary = BuildShortcutValidationSummary(target, report);
                        EditorUtility.DisplayDialog("MapGenV2 검증 / Validate", summary, "OK");
                        SetLastOperationResult(summary);
                    }
                }

                using (new EditorGUI.DisabledScope(target != null || createAction == null))
                {
                    if (GUILayout.Button(new GUIContent("누락 생성 / Fix/Create Missing", "누락된 참조를 만들거나 starter 흐름으로 보정합니다."), GUILayout.Width(184f)))
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
                case MapGenMockupDraftAsset mockupDraft:
                    report.AddRange(mockupDraft.ValidateDraftSource(), mockupDraft.name);
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
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawOutputPaths();
                EditorGUI.BeginChangeCheck();
                DrawOverwritePolicyField();
                var overwriteMode = GetOverwriteMode();
                if (overwriteMode == MapGenV2OutputOverwriteMode.UpdateSelected)
                {
                    selectedMaterializedRoot = (GameObject)EditorGUILayout.ObjectField("Materialized Root", selectedMaterializedRoot, typeof(GameObject), true);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    SaveWindowState();
                }

                if (overwriteMode == MapGenV2OutputOverwriteMode.UpdateSelected && selectedMaterializedRoot == null)
                {
                    DrawWrappingHelpBox("UpdateSelected는 Materialized Root가 필요합니다. 기존 출력을 갱신하려면 root를 지정하거나 아래 유틸리티에서 Find Previous Root를 사용하세요.", MessageType.Info);
                }

            if (!workflow.CanMaterialize)
            {
                DrawWrappingHelpBox($"씬 생성 불가 / Materialize unavailable: {workflow.MaterializeReason}", MessageType.Info);
            }

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

                DrawFoldoutSection("씬 출력 유틸리티 / Scene Output Utilities", ref showSceneOutputUtilitiesFoldout, () =>
                {
                    DrawWrappingHelpBox(BuildOverwritePolicyHelp(GetOverwriteMode()), MessageType.Info);
                    if (overwriteMode != MapGenV2OutputOverwriteMode.UpdateSelected)
                    {
                        EditorGUI.BeginChangeCheck();
                        selectedMaterializedRoot = (GameObject)EditorGUILayout.ObjectField("Materialized Root", selectedMaterializedRoot, typeof(GameObject), true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SaveWindowState();
                        }
                    }

                    DrawMaterializedPrefabFolderField();

                    if (GUILayout.Button("Find Previous Root"))
                    {
                        var marker = MapGenMockupMaterializer.FindExistingMarker(draft);
                        selectedMaterializedRoot = marker != null ? marker.gameObject : null;
                        SetLastOperationResult(selectedMaterializedRoot != null
                            ? $"Found materialized root: {selectedMaterializedRoot.name}."
                            : "No previous materialized root found for the current draft.");
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
                });
            }
        }

        private void DrawRuntimeBakeControls(MapGenV2WorkflowStatus workflow)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("베이크 에셋 / Baked Asset", BuildExpectedBakedAssetPath());
                DrawBakedAssetFolderField();
            if (!workflow.CanBakeRuntime)
            {
                DrawWrappingHelpBox($"런타임 베이크 불가 / Bake unavailable: {workflow.BakeRuntimeReason}", MessageType.Info);
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
        }

        private void DrawMaterializedPrefabFolderField()
        {
            using (new EditorGUI.DisabledScope(draft == null && profile == null))
            {
                var currentFolder = GetMaterializedPrefabFolder();
                EditorGUI.BeginChangeCheck();
                var newFolder = EditorGUILayout.TextField("Materialized Prefab Folder", currentFolder);
                if (EditorGUI.EndChangeCheck())
                {
                    var outputSettings = MapGenV2DraftOutputSettingsUtility.Get(draft, profile);
                    outputSettings.MaterializedPrefabFolder = newFolder;
                    MapGenV2DraftOutputSettingsUtility.Set(
                        draft,
                        profile,
                        outputSettings,
                        "Change MapGen Materialized Prefab Folder");
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

        private void DrawBakedAssetFolderField()
        {
            using (new EditorGUI.DisabledScope(draft == null && profile == null))
            {
                var currentFolder = GetBakedAssetFolder();
                EditorGUI.BeginChangeCheck();
                var newFolder = EditorGUILayout.TextField("Baked Asset Folder", currentFolder);
                if (EditorGUI.EndChangeCheck())
                {
                    var outputSettings = MapGenV2DraftOutputSettingsUtility.Get(draft, profile);
                    outputSettings.BakedAssetFolder = newFolder;
                    MapGenV2DraftOutputSettingsUtility.Set(
                        draft,
                        profile,
                        outputSettings,
                        "Change MapGen Baked Asset Folder");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Ensure Baked Folder", GUILayout.Width(170f)))
                    {
                        MapGenV2AssetFolderUtility.EnsureAssetFolder(GetBakedAssetFolder());
                        SetLastOperationResult($"Baked asset folder ready: {GetBakedAssetFolder()}.");
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
                        ? draft.RegenerateUnlockedFromDraft(() => MapGenV2EditorProgress.Begin(operationName, "Solving mockup layout..."))
                        : draft.GenerateFromDraft(() => MapGenV2EditorProgress.Begin(operationName, "Solving mockup layout...")),
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
                    () => draft.ApplyPostProcessingFromDraft(() => MapGenV2EditorProgress.Begin("Post-Process Mockup", "Running post-process passes...")),
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
                            GetActiveModuleSet(),
                            MapGenMockupMaterializer.BuildPlan(draft),
                            draft.Seed,
                            draft),
                        $"Seed={draft.Seed}"),
                    out var sample);
                selectedMaterializedRoot = root;
                SetLastOperationResult(root != null
                    ? $"Materialized scene root: {root.name}. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}."
                    : $"Materialize To Scene failed. Check saved draft state and module coverage. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}.");
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
                    : $"Bake Runtime Asset failed. Check saved draft state. Perf {sample.ElapsedMs}ms/{sample.BudgetMs}ms {sample.Target}.");
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
                return "씬 생성 / 저장된 드래프트를 씬 출력으로 생성.";
            }

            var materializedReport = MapGenMockupMaterializer.ValidateExistingOutput(draft, materializedRoot);
            if (!materializedReport.IsValid)
            {
                return "씬 재생성 / 오래된 씬 출력을 현재 저장된 드래프트와 에셋 구성으로 재생성.";
            }

            if (!workflow.CanBakeRuntime)
            {
                return workflow.NextAction;
            }

            var bakedAsset = LoadExpectedBakedAsset();
            if (bakedAsset == null)
            {
                return "런타임 베이크 / runtime-safe baked map asset 생성.";
            }

            var bakeReport = MapGenRuntimeBakeUtility.ValidateConsistency(draft, bakedAsset);
            if (!bakeReport.IsValid)
            {
                return "런타임 재베이크 / 오래된 baked asset을 현재 저장된 드래프트로 재생성.";
            }

            return "출력 최신 / 씬 출력과 runtime bake가 현재 저장된 드래프트와 일치.";
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
            if (draft == null && profile == null)
            {
                outputMode = (MapGenV2SceneOutputMode)EditorGUILayout.EnumPopup("출력 모드 / Output Mode", outputMode);
                return;
            }

            var outputSettings = MapGenV2DraftOutputSettingsUtility.Get(draft, profile);
            var overwriteMode = outputSettings.OverwriteMode;
            overwriteMode = (MapGenV2OutputOverwriteMode)EditorGUILayout.EnumPopup("덮어쓰기 정책 / Overwrite Policy", overwriteMode);
            if (overwriteMode != outputSettings.OverwriteMode)
            {
                outputSettings.OverwriteMode = overwriteMode;
                MapGenV2DraftOutputSettingsUtility.Set(
                    draft,
                    profile,
                    outputSettings,
                    "Change MapGen Overwrite Policy");
                outputMode = ToSceneOutputMode(overwriteMode);
            }
        }

        private MapGenV2OutputOverwriteMode GetOverwriteMode()
        {
            if (draft != null || profile != null)
            {
                return MapGenV2DraftOutputSettingsUtility.Get(draft, profile).OverwriteMode;
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
            return draft != null || profile != null
                ? ToSceneOutputMode(MapGenV2DraftOutputSettingsUtility.Get(draft, profile).OverwriteMode)
                : outputMode;
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
                    "ReplacePrevious: 같은 draft/seed/signature의 이전 MapGenV2 root를 삭제한 뒤 새 root를 생성합니다."
            };
        }

        private static int CreateRandomSeed()
        {
            return System.Guid.NewGuid().GetHashCode() & 0x7fffffff;
        }

        private void CreateDraft()
        {
            if (profile == null)
            {
                var setup = MapGenV2StarterSetupBuilder.CreateStarterProfileSetup();
                profile = setup.Profile;
                draft = setup.Draft;
                importDraft = draft;
                Selection.activeObject = draft != null ? draft : profile;
                var moduleSet = GetActiveModuleSet();
                EnsureDraftPrefabPalette(moduleSet);
                if (draft != null)
                {
                    EditorUtility.SetDirty(draft);
                }

                AssetDatabase.SaveAssets();
                SetLastOperationResult("Created a single draft asset with hidden internal setup. Replace prefab slots before generating if needed.");
                return;
            }

            var draftFolder = GetDraftFolder();
            MapGenV2AssetFolderUtility.EnsureAssetFolder(draftFolder);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{draftFolder}/MapGenMockupDraft.asset");
            draft = ScriptableObject.CreateInstance<MapGenMockupDraftAsset>();
            draft.ImportFromProfileSource(profile, true);
            CaptureModuleSetPrefabPalette(profile.StyleSet != null ? profile.StyleSet.ModuleSet : null);
            AssetDatabase.CreateAsset(draft, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = draft;
            SetLastOperationResult($"Created draft asset: {path}.");
        }

        private string BuildExpectedBakedAssetPath()
        {
            return MapGenV2DraftOutputSettingsUtility.GetBakedAssetPath(draft, profile);
        }

        private MapGenBakedMapAsset LoadExpectedBakedAsset()
        {
            var path = BuildExpectedBakedAssetPath();
            return AssetDatabase.LoadAssetAtPath<MapGenBakedMapAsset>(path);
        }

        private string GetDraftFolder()
        {
            return MapGenV2DraftOutputSettingsUtility.Get(draft, profile).DraftFolder;
        }

        private string GetMaterializedPrefabFolder()
        {
            return MapGenV2DraftOutputSettingsUtility.Get(draft, profile).MaterializedPrefabFolder;
        }

        private string GetBakedAssetFolder()
        {
            return MapGenV2DraftOutputSettingsUtility.Get(draft, profile).BakedAssetFolder;
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
            EditorPrefs.SetBool(SetupAssetsFoldoutKey, showSetupAssetsFoldout);
            EditorPrefs.SetBool(MapAssetsFoldoutKey, showMapAssetsFoldout);
            EditorPrefs.SetBool(OutputPathsFoldoutKey, showOutputPathsFoldout);
            EditorPrefs.SetBool(LinkedAssetsFoldoutKey, showLinkedAssetsFoldout);
            EditorPrefs.SetBool(MockupActionsFoldoutKey, showMockupActionsFoldout);
            EditorPrefs.SetBool(MockupPreviewFoldoutKey, showMockupPreviewFoldout);
            EditorPrefs.SetBool(DetailsFoldoutKey, showDetailsFoldout);
            EditorPrefs.SetBool(SceneOutputFoldoutKey, showSceneOutputFoldout);
            EditorPrefs.SetBool(PrimaryActionImpactFoldoutKey, showPrimaryActionImpactFoldout);
            EditorPrefs.SetBool(RuntimeBakeFoldoutKey, showRuntimeBakeFoldout);
            EditorPrefs.SetBool(SetupUtilitiesFoldoutKey, showSetupUtilitiesFoldout);
            EditorPrefs.SetBool(DraftUtilitiesFoldoutKey, showDraftUtilitiesFoldout);
            EditorPrefs.SetBool(SceneOutputUtilitiesFoldoutKey, showSceneOutputUtilitiesFoldout);
            EditorPrefs.SetBool(MockupTechnicalDetailsFoldoutKey, showMockupTechnicalDetailsFoldout);
            EditorPrefs.SetBool(RegionAdvancedEditFoldoutKey, showRegionAdvancedEditFoldout);
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
                EditorGUILayout.LabelField("드래프트 설정 ID / Draft Setup ID", string.IsNullOrEmpty(previewData.ProfileId) ? "(none)" : previewData.ProfileId);
                EditorGUILayout.LabelField("시드 / Seed", previewData.Seed.ToString());
                EditorGUILayout.LabelField("그리드 / Grid", $"{previewData.Width} x {previewData.Height}");
                EditorGUILayout.LabelField(
                    "생성 상태 / Generated State",
                    previewData.GeneratedSignatureCurrent ? "최신 / Current" : MapGenV2EditorText.Get("mapgenv2.draftStale"));
                EditorGUILayout.LabelField(
                    "저장 상태 / Save State",
                    previewData.Accepted
                        ? previewData.AcceptedSignatureCurrent ? "저장됨 / Saved" : "변경됨: 다시 저장 필요 / Changed"
                        : "저장 필요 / Needs save");
                EditorGUILayout.LabelField(
                    "셀 / Cells",
                    $"Rooms {previewData.Summary.RoomCells}, Corridors {previewData.Summary.CorridorCells}, Blocked {previewData.Summary.BlockedCells}, Connectors {previewData.Summary.ConnectorCells}, Reserved {previewData.Summary.ReservedCells}");
                EditorGUILayout.LabelField("리전 / Regions", previewData.Summary.RegionCount.ToString());
            }

            DrawFoldoutSection("기술 정보 / Technical Details", ref showMockupTechnicalDetailsFoldout, () =>
            {
                EditorGUILayout.LabelField("생성 서명 / Generated Signature", string.IsNullOrEmpty(previewData.LastGeneratedSignature) ? "(none)" : previewData.LastGeneratedSignature);
                EditorGUILayout.LabelField("현재 서명 / Current Signature", previewData.CurrentSignature);
                EditorGUILayout.LabelField("저장 서명 / Saved Signature", string.IsNullOrEmpty(previewData.AcceptedSignature) ? "(none)" : previewData.AcceptedSignature);
                DrawWrappingHelpBox(BuildPrimaryMockupUxSummary(), MessageType.Info);
            });

            var propPlacement = MapGenPropPlacementPlanner.BuildForDraft(draft);
            DrawPropPlacementPreviewSummary(propPlacement);
            DrawPostProcessPassSummary();
            DrawDrawingToolbar();
            DrawMockupPreview(previewData);
            DrawCellDetails(previewData);
        }

        private void DrawDrawingToolbar()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("드로잉 도구 / Drawing Tools", EditorStyles.boldLabel);
                var labels = new[]
                {
                    "선택 / Select",
                    "방 / Room",
                    "복도 / Corridor",
                    "비우기 / Empty",
                    "차단 / Blocked",
                    "예약 / Reserved"
                };
                paintTool = (MapGenDraftPaintTool)GUILayout.Toolbar((int)paintTool, labels);
                EditorGUILayout.HelpBox(
                    paintTool == MapGenDraftPaintTool.Select
                        ? "클릭하면 셀/리전을 선택합니다. / Click cells to inspect regions."
                        : "클릭 또는 드래그로 현재 드래프트 셀을 직접 수정합니다. 저장 전까지 씬 출력에는 반영하지 않습니다.",
                    MessageType.Info);
            }
        }

        private void DrawMockupPreview(MapGenMockupPreviewData previewData)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("목업 미리보기 / Mockup Preview", EditorStyles.boldLabel);
            if (EditorGUIUtility.currentViewWidth < 620f)
            {
                previewCellSize = EditorGUILayout.Slider("확대 / Zoom", previewCellSize, 6f, 32f);
                showPropPlacementOverlay = EditorGUILayout.ToggleLeft("프롭 오버레이 / Props", showPropPlacementOverlay);
                using (new EditorGUI.DisabledScope(!HasPostProcessPassOverlay()))
                {
                    showPostProcessOverlay = EditorGUILayout.ToggleLeft("후처리 오버레이 / Post", showPostProcessOverlay);
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("확대 / Zoom", GUILayout.Width(72f));
                    previewCellSize = EditorGUILayout.Slider(previewCellSize, 6f, 32f, GUILayout.Width(180f));
                    showPropPlacementOverlay = EditorGUILayout.ToggleLeft("프롭 오버레이 / Props", showPropPlacementOverlay, GUILayout.Width(150f));
                    using (new EditorGUI.DisabledScope(!HasPostProcessPassOverlay()))
                    {
                        showPostProcessOverlay = EditorGUILayout.ToggleLeft("후처리 오버레이 / Post", showPostProcessOverlay, GUILayout.Width(170f));
                    }
                }
            }

            DrawLegend();

            if (previewData.Width <= 0 || previewData.Height <= 0)
            {
                EditorGUILayout.HelpBox("생성된 드래프트 맵이 없습니다. 지정한 시드로 생성을 누르면 이 영역에 파랑/빨강/검정/회색 그리드가 표시됩니다.", MessageType.Info);
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
            var colors = new[]
            {
                EmptyColor,
                RoomColor,
                CorridorColor,
                BlockedColor,
                ConnectorColor,
                ReservedColor,
                PropOverlayColor,
                BlockerPropOverlayColor
            };
            var labels = new[]
            {
                "파랑 Empty",
                "빨강 Room",
                "검정 Corridor",
                "회색 Blocked",
                "검정 Connector",
                "회색 Reserved",
                "초록 Prop",
                "주황 Blocker"
            };
            var availableWidth = Mathf.Max(180f, EditorGUIUtility.currentViewWidth - 36f);
            var itemWidth = availableWidth < 520f ? 128f : 136f;
            var columns = Mathf.Clamp(Mathf.FloorToInt(availableWidth / itemWidth), 1, labels.Length);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (var index = 0; index < labels.Length; index += columns)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var rowEnd = Mathf.Min(index + columns, labels.Length);
                        for (var itemIndex = index; itemIndex < rowEnd; itemIndex++)
                        {
                            DrawLegendItem(colors[itemIndex], labels[itemIndex], itemWidth - 4f);
                        }

                        GUILayout.FlexibleSpace();
                    }
                }
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

        private static void DrawLegendItem(Color color, string label, float width)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(width)))
            {
                var rect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
                EditorGUI.DrawRect(rect, color);
                EditorGUILayout.LabelField(label, GUILayout.Width(Mathf.Max(72f, width - 18f)));
            }
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

            if (current.type == EventType.MouseUp && current.button == 0)
            {
                paintStrokeRegionId = -1;
                paintUndoRecorded = false;
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

            if (paintTool != MapGenDraftPaintTool.Select
                && current.button == 0
                && (current.type == EventType.MouseDown || current.type == EventType.MouseDrag))
            {
                PaintDraftCell(coord, cell);
                current.Use();
                Repaint();
                return;
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

        private void PaintDraftCell(Vector2Int coord, MapGenMockupCell currentCell)
        {
            if (draft == null)
            {
                return;
            }

            draft.EnsureCellArray();
            if (coord.x < 0 || coord.y < 0 || coord.x >= draft.Width || coord.y >= draft.Height)
            {
                return;
            }

            var state = StateForPaintTool(paintTool);
            var regionState = IsPaintRegionState(state);
            if (regionState && paintStrokeRegionId < 0)
            {
                paintStrokeRegionId = currentCell.State == state && currentCell.RegionId >= 0
                    ? currentCell.RegionId
                    : NextRegionId(draft);
            }

            var nextCell = BuildPaintCell(state, regionState ? paintStrokeRegionId : -1);
            var index = (coord.y * draft.Width) + coord.x;
            if (index < 0 || index >= draft.Cells.Length || CellsMatchForPaint(draft.Cells[index], nextCell))
            {
                return;
            }

            if (!paintUndoRecorded)
            {
                Undo.RecordObject(draft, "Paint MapGen Draft Cell");
                paintUndoRecorded = true;
            }

            draft.Cells[index] = nextCell;
            draft.LastGeneratedSignature = draft.ComputeSignature();
            draft.LastGeneratedSourceSignature = draft.CurrentSourceSignature;
            draft.ClearAcceptance();
            selectedCell = coord;
            hasSelectedCell = true;
            selectedRegionId = nextCell.RegionId;
            EditorUtility.SetDirty(draft);
            SaveWindowState();
            SetLastOperationResult($"Painted {state} at {coord.x},{coord.y}. Save Draft when ready.");
        }

        private static MapGenCellState StateForPaintTool(MapGenDraftPaintTool tool)
        {
            return tool switch
            {
                MapGenDraftPaintTool.Room => MapGenCellState.Room,
                MapGenDraftPaintTool.Corridor => MapGenCellState.Corridor,
                MapGenDraftPaintTool.Blocked => MapGenCellState.Blocked,
                MapGenDraftPaintTool.Reserved => MapGenCellState.Reserved,
                _ => MapGenCellState.Empty
            };
        }

        private static bool IsPaintRegionState(MapGenCellState state)
        {
            return state == MapGenCellState.Room || state == MapGenCellState.Corridor || state == MapGenCellState.Connector;
        }

        private static int NextRegionId(MapGenMockupDraftAsset sourceDraft)
        {
            var maxRegionId = -1;
            foreach (var cell in sourceDraft.Cells ?? System.Array.Empty<MapGenMockupCell>())
            {
                if (cell.RegionId > maxRegionId)
                {
                    maxRegionId = cell.RegionId;
                }
            }

            return maxRegionId + 1;
        }

        private static MapGenMockupCell BuildPaintCell(MapGenCellState state, int regionId)
        {
            if (state == MapGenCellState.Empty)
            {
                return MapGenMockupCell.Empty;
            }

            return new MapGenMockupCell
            {
                State = state,
                RegionId = regionId,
                RoomCategory = MapGenRoomCategory.Main,
                SocketKind = state == MapGenCellState.Blocked ? MapGenSocketKind.Blocked : MapGenSocketKind.None,
                SocketId = string.Empty,
                SocketWidth = state == MapGenCellState.Blocked ? 1 : 0,
                PropChannel = string.Empty,
                PropWeight = 1,
                SourceTemplateId = string.Empty,
                SourceShapeId = string.Empty
            };
        }

        private static bool CellsMatchForPaint(MapGenMockupCell a, MapGenMockupCell b)
        {
            return a.State == b.State
                && a.RegionId == b.RegionId
                && a.RoomCategory == b.RoomCategory
                && a.SocketKind == b.SocketKind
                && a.SocketWidth == b.SocketWidth;
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
                EditorGUILayout.LabelField("선택 리전 / Selected Region", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("종류 / Type", DescribeRegionType(roomCount, corridorCount, connectorCount, blockedCount, reservedCount));
                EditorGUILayout.LabelField("크기 / Cells", cellCount.ToString());
                EditorGUILayout.LabelField("잠금 / Lock", regionOverride.Locked ? "잠김 / Locked" : "열림 / Editable");

                using (new EditorGUI.DisabledScope(regionOverride.Locked))
                {
                    if (GUILayout.Button("다른 형태로 교체 / Replace Shape"))
                    {
                        RegenerateSelectedRegion("Replaced shape");
                    }
                }

                if (GUILayout.Button(regionOverride.Locked ? "잠금 해제 / Unlock" : "잠금 / Lock"))
                {
                    Undo.RecordObject(draft, "Toggle MapGen Region Lock");
                    draft.SetRegionLocked(selectedRegionId, !regionOverride.Locked);
                    EditorUtility.SetDirty(draft);
                    SetLastOperationResult($"{(regionOverride.Locked ? "Unlocked" : "Locked")} region {selectedRegionId}.");
                }

                DrawFoldoutSection("고급 리전 편집 / Advanced Region Edit", ref showRegionAdvancedEditFoldout, () =>
                {
                    EditorGUILayout.LabelField("Region Id", selectedRegionId.ToString());
                    EditorGUILayout.LabelField(
                        "State Counts",
                        $"Room {roomCount}, Corridor {corridorCount}, Connector {connectorCount}, Blocked {blockedCount}, Reserved {reservedCount}");
                    EditorGUILayout.LabelField("Adjacent Links / 인접 링크", adjacentLinkCount.ToString());
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
                        SetLastOperationResult($"Changed region {selectedRegionId} category to {newCategory}. Save the draft before scene output.");
                    }

                    if (GUILayout.Button("Clear Region Override"))
                    {
                        Undo.RecordObject(draft, "Clear MapGen Region Override");
                        draft.ClearRegionOverride(selectedRegionId);
                        EditorUtility.SetDirty(draft);
                        SetLastOperationResult($"Cleared region {selectedRegionId} override metadata.");
                    }

                    using (new EditorGUI.DisabledScope(regionOverride.Locked || corridorCount + connectorCount <= 0))
                    {
                        if (GUILayout.Button("Reroute Connectors / 연결 재경로"))
                        {
                            RegenerateSelectedRegion("Rerouted connectors");
                        }
                    }

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
                });
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
            var report = draft.RegenerateRegionFromDraft(selectedRegionId);
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
                return "Blocked/reserved cells stamp blocker or reserved output. / 차단·예약 셀은 blocker 또는 reserved 출력 생성";
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
            SetLastOperationResult($"{actionLabel} region {selectedRegionId}. Save the draft before scene output.");
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
