export function createEditorWorkspacePanelBridge(deps = {}) {
  const {
    renderEditorSurfaceBrushPanel = () => "",
    renderEditorRangeBrushPanel = () => "",
    renderEditorDensityHistogramPanel = () => "",
    renderEditorPlacementRecommendationPanel = () => "",
    renderEditorSelectedBlockPanel = () => "",
    renderEditorValidationPanel = () => "",
    renderEditorContentBuildDashboardPanel = () => "",
    renderEditorPresetLibraryPanel = () => "",
    createPreviewGrid = () => null,
    previewGridMarkup = () => "",
    validationSummaryText = () => "",
    validationBlockerMarkup = () => "",
    densityHistogramMarkup = () => "",
    textureSwatchColor = () => "#000",
    escapeHtml = (value) => String(value ?? ""),
  } = deps;

  return function buildEditorWorkspacePanels(args = {}) {
    const {
      state,
      activeRoom,
      previewRect,
      selectedCount,
      selectedRect,
      densityOverlay,
      densityHistogram,
      recommendationRoomId,
      recommendationRoomSummary,
      placementRecommendations = [],
      preset = null,
      compiled = { ok: false },
      validationReport = null,
      projectValidationReport = null,
      validationIssueMarkup = () => "",
      projectDashboard = null,
      floorTextureIds = [],
      ceilingTextureIds = [],
      wallTextureIds = [],
      battleBackgrounds = [],
      cellTags = [],
      roomTypes = [],
      isRangeBrushTool = () => false,
      activeBrushRangeStart = () => null,
      currentMapSeed = () => 0,
      customPresets = [],
      selectedPreset = null,
    } = args;

    const recommendationPanelMarkup = renderEditorPlacementRecommendationPanel({
      subtitle: `${state.selectedRoomType} · ${state.selectedCellTag}`,
      roomSummary: `${recommendationRoomId ? `room ${recommendationRoomId}` : "cursor room 없음"} · encounter ${recommendationRoomSummary.encounter} · trap ${recommendationRoomSummary.trap} · npc ${recommendationRoomSummary.npc} · recovery ${recommendationRoomSummary.recovery} · event ${recommendationRoomSummary.event}`,
      bodyMarkup: placementRecommendations.length ? `
        <div class="preset-toolbar">
          <button id="autoPlaceRecommendationsBtn" ${recommendationRoomId ? "" : "disabled"}>추천 자동 배치</button>
        </div>
        <div class="preset-toolbar">
          ${placementRecommendations.map((entry) => `<button data-recommend-tool="${entry.tool}">${escapeHtml(entry.label)} ${entry.score}</button>`).join("")}
        </div>
        <div class="preset-stack">
          ${placementRecommendations.map((entry) => `<div class="muted"><strong>${escapeHtml(entry.label)}</strong> · score ${entry.score} · ${entry.source} · ${escapeHtml(entry.notes.join(" · "))}</div>`).join("")}
        </div>
      ` : `<div class="muted">현재 room type/tag 조합에 연결된 recommendation rule이 없다.</div>`,
    });

    const toolPanelsMarkup = `
      ${renderEditorSurfaceBrushPanel({
        bodyMarkup: `
          <div class="preset-inspector">
            <div class="preset-field">
              <label for="floorTextureSelect">Floor texture</label>
              <select id="floorTextureSelect">${floorTextureIds.map((id) => `<option value="${id}" ${state.selectedFloorTextureId === id ? "selected" : ""}>${id}</option>`).join("")}</select>
            </div>
            <div class="preset-field">
              <label for="ceilingTextureSelect">Ceiling texture</label>
              <select id="ceilingTextureSelect">${ceilingTextureIds.map((id) => `<option value="${id}" ${state.selectedCeilingTextureId === id ? "selected" : ""}>${id}</option>`).join("")}</select>
            </div>
            <div class="preset-field">
              <label for="wallTextureSelect">Wall texture</label>
              <select id="wallTextureSelect">${wallTextureIds.map((id) => `<option value="${id}" ${state.selectedWallTextureId === id ? "selected" : ""}>${id}</option>`).join("")}</select>
            </div>
            <div class="texture-swatch-grid">
              <div class="texture-swatch-card"><span class="texture-swatch" style="background:${textureSwatchColor(state.selectedFloorTextureId)}"></span><strong>floor</strong><div class="muted">${state.selectedFloorTextureId}</div></div>
              <div class="texture-swatch-card"><span class="texture-swatch" style="background:${textureSwatchColor(state.selectedCeilingTextureId)}"></span><strong>ceiling</strong><div class="muted">${state.selectedCeilingTextureId}</div></div>
              <div class="texture-swatch-card"><span class="texture-swatch" style="background:${textureSwatchColor(state.selectedWallTextureId)}"></span><strong>wall</strong><div class="muted">${state.selectedWallTextureId}</div></div>
            </div>
          </div>
        `,
      })}
      ${renderEditorRangeBrushPanel({
        cellTagOptions: cellTags.map((tag) => `<option value="${tag}" ${state.selectedCellTag === tag ? "selected" : ""}>${tag}</option>`).join(""),
        battleBackgroundOptions: battleBackgrounds.map((id) => `<option value="${id}" ${state.selectedBattleBackgroundId === id ? "selected" : ""}>${id || "(clear)"}</option>`).join(""),
        roomTypeOptions: roomTypes.map((type) => `<option value="${type}" ${state.selectedRoomType === type ? "selected" : ""}>${type}</option>`).join(""),
        roomIdValue: state.activeRoomId || "",
        metadataSelectionMode: state.metadataSelectionMode,
        lassoSelectionAction: state.lassoSelectionAction,
        densityOverlayMode: state.densityOverlayMode,
        rangeModeSummary: state.metadataSelectionMode === "lasso"
          ? `lasso ${state.lassoSelectionAction} · 드래그로 자유 선택을 만들고 선택 적용으로 메타데이터를 반영한다.`
          : state.editorTool === "room" && state.roomRangeStart
            ? `room range 시작 ${state.roomRangeStart.x},${state.roomRangeStart.y} · 현재 preview ${previewRect ? `${previewRect.x},${previewRect.y} ${previewRect.width}x${previewRect.height}` : "없음"}`
            : state.editorTool === "cellTag" && state.metadataRangeStart
              ? `cellTag range 시작 ${state.metadataRangeStart.x},${state.metadataRangeStart.y} · 현재 preview ${previewRect ? `${previewRect.x},${previewRect.y} ${previewRect.width}x${previewRect.height}` : "없음"}`
              : state.editorTool === "battleBg" && state.metadataRangeStart
                ? `battleBg range 시작 ${state.metadataRangeStart.x},${state.metadataRangeStart.y} · 현재 preview ${previewRect ? `${previewRect.x},${previewRect.y} ${previewRect.width}x${previewRect.height}` : "없음"}`
                : state.editorTool === "texture" && state.metadataRangeStart
                  ? `texture range 시작 ${state.metadataRangeStart.x},${state.metadataRangeStart.y} · 현재 preview ${previewRect ? `${previewRect.x},${previewRect.y} ${previewRect.width}x${previewRect.height}` : "없음"}`
                  : "texture/room/cellTag/battleBg 브러시는 클릭-드래그 또는 시작 셀/끝 셀 순서로 직사각형 범위를 지정한다.",
        activeRoomSummary: activeRoom
          ? `현재 room ${activeRoom.id} · bounds ${activeRoom.bounds.x},${activeRoom.bounds.y} ${activeRoom.bounds.width}x${activeRoom.bounds.height}`
          : "현재 선택 room bounds 없음",
        selectedCountSummary: selectedCount
          ? `현재 selection ${selectedCount}칸 · bounds ${selectedRect ? `${selectedRect.x},${selectedRect.y} ${selectedRect.width}x${selectedRect.height}` : "없음"}`
          : "현재 selection 없음",
        densityOverlaySummary: densityOverlay.mode === "none"
          ? "density overlay 비활성화"
          : `${densityOverlay.mode} density · hot cell ${densityOverlay.hotCells} · total ${densityOverlay.totalPlacements} · max ${densityOverlay.maxCount}`,
        applyDisabled: !(selectedCount && isRangeBrushTool()),
        selectionDisabled: !isRangeBrushTool(),
        clearDisabled: !(activeBrushRangeStart() || selectedCount),
      })}
      ${renderEditorDensityHistogramPanel({
        subtitle: densityHistogram.mode === "none" ? "overlay off" : densityHistogram.mode,
        bodyMarkup: densityHistogram.mode === "none"
          ? `<div class="muted">Density overlay를 켜면 global / room / tag 분포를 같이 본다.</div>`
          : `
            ${densityHistogramMarkup(densityHistogram.global, "global")}
            <div class="preset-field">
              <label>Heatmap bands</label>
              <div class="preset-stack">
                <div class="muted">low ${densityHistogram.bandSummary.low}칸 · medium ${densityHistogram.bandSummary.medium}칸 · high ${densityHistogram.bandSummary.high}칸 · peak ${densityHistogram.bandSummary.peak}칸</div>
                <div class="muted">max ${densityOverlay.maxCount || 0} 기준으로 상대 intensity를 band로 나눈다.</div>
              </div>
            </div>
            ${densityHistogramMarkup(densityHistogram.room, recommendationRoomId ? `room ${recommendationRoomId}` : "room")}
            ${densityHistogramMarkup(densityHistogram.tag, `tag ${state.selectedCellTag}`)}
            <div class="preset-field">
              <label>Top hot cells</label>
              <div class="preset-stack">
                ${densityHistogram.topCells.length
                  ? densityHistogram.topCells.map((entry) => `<div class="muted">${entry.x},${entry.y} · count ${entry.count} · room ${escapeHtml(entry.roomId || "-")} · tag ${escapeHtml(entry.tags.join(", ") || "-")}</div>`).join("")
                  : `<div class="muted">hot cell 없음</div>`}
              </div>
            </div>
          `,
      })}
      ${recommendationPanelMarkup}
      ${renderEditorSelectedBlockPanel({
        subtitle: preset ? `${preset.name} · rot ${state.presetRotation * 90}°` : "선택 없음",
        bodyMarkup: `
          <div class="preset-canvas preset-preview-grid">${preset ? previewGridMarkup(createPreviewGrid(preset, { width: 7, height: 7, rotation: state.presetRotation }), "preset-preview-cell") : ""}</div>
          <div class="preset-toolbar">
            <button id="rotatePresetBtn">회전</button>
            <button id="loadPresetToDraftBtn" ${preset ? "" : "disabled"}>초안으로 복사</button>
          </div>
        `,
      })}
      <button id="generateBtn">Legacy 절차 생성</button>
      <button id="testBtn">테스트 플레이</button>
      <div class="preset-toolbar">
        <button id="saveProjectBtn">프로젝트 저장</button>
        <button id="loadProjectBtn">프로젝트 불러오기</button>
      </div>
      <button id="exportBtn">맵 JSON 내보내기</button>
      <button id="exportProjectBtn">프로젝트 JSON 내보내기</button>
      <button id="compileBtn" ${compiled.ok ? "" : "disabled"}>compiledMap 내보내기</button>
      <button id="exportManifestBtn">contentBuildManifest 내보내기</button>
      <button id="importBtn">JSON 불러오기</button>
      <button id="importProjectBtn">프로젝트 JSON 불러오기</button>
      ${renderEditorValidationPanel({
        title: "검증 리포트",
        subtitle: validationSummaryText(validationReport),
        blockerMarkup: validationBlockerMarkup("active floor", validationReport),
        reportMarkup: validationIssueMarkup(validationReport),
      })}
      ${renderEditorValidationPanel({
        title: "프로젝트 검증",
        subtitle: validationSummaryText(projectValidationReport),
        blockerMarkup: validationBlockerMarkup("project", projectValidationReport),
        reportMarkup: validationIssueMarkup(projectValidationReport),
      })}
      ${renderEditorContentBuildDashboardPanel({
        subtitle: projectDashboard.manifestReady ? "manifest ready" : "build blocked",
        bodyMarkup: `
          <div class="muted">전체 floor ${projectDashboard.floorEntries.length}개 · compiled ok ${Object.keys(projectDashboard.compiledProject.compiledMaps || {}).length} · compile failure ${projectDashboard.compiledProject.failures.length}</div>
          <div class="validation-line is-${compiled.ok ? "info" : "error"}"><strong>active floor compiledMap</strong> ${compiled.ok ? "export 가능" : "export blocked"}</div>
          ${compiled.ok ? "" : validationBlockerMarkup("active floor blocker", compiled.report || validationReport)}
          <div class="validation-line is-${projectDashboard.compiledProject.ok ? "info" : "error"}"><strong>test play</strong> ${projectDashboard.compiledProject.ok ? "시작 가능" : `blocked · 먼저 층 ${projectDashboard.firstCompileFailure?.floor || "?"} 확인`}</div>
          ${projectDashboard.compiledProject.ok ? "" : validationBlockerMarkup("test play blocker", projectDashboard.firstCompileFailure?.report)}
          <div class="validation-line is-${projectDashboard.manifestReady ? "info" : "error"}"><strong>contentBuildManifest</strong> ${projectDashboard.manifestReady ? "export 가능" : "export blocked"}</div>
          <div class="muted">project ${validationSummaryText(projectDashboard.projectValidationReport)}</div>
          <div class="muted">asset ${validationSummaryText(projectDashboard.assetValidationReport)} · progression ${validationSummaryText(projectDashboard.progressionValidationReport)}</div>
          <div class="muted">asset metadata · invalid ${projectDashboard.assetValidationReport.metadataSummary.invalidMaterialMetadata} · unused material ${projectDashboard.assetValidationReport.metadataSummary.unusedMaterialCandidates} · unused bg ${projectDashboard.assetValidationReport.metadataSummary.unusedBattleBackgroundCandidates}</div>
          <div class="muted">key/lock ${validationSummaryText(projectDashboard.keyLockValidationReport)} · required contract ${validationSummaryText(projectDashboard.requiredPlacementContractReport)}</div>
          <div class="muted">required target ${validationSummaryText(projectDashboard.requiredContentValidationReport)}</div>
          ${projectDashboard.manifestReady ? "" : validationBlockerMarkup("manifest blocker", projectDashboard.manifestBlockerReport)}
          <div class="preset-stack">
            ${projectDashboard.assetValidationReport.metadataSummary.materialIds
              .filter((materialId) => !projectDashboard.assetValidationReport.metadataSummary.referencedMaterialIds.includes(materialId))
              .slice(0, 5)
              .map((materialId) => `<div class="muted">unused material 후보 · ${escapeHtml(materialId)}</div>`).join("")}
            ${projectDashboard.assetValidationReport.metadataSummary.battleBackgroundIds
              .filter((backgroundId) => !projectDashboard.assetValidationReport.metadataSummary.referencedBattleBackgroundIds.includes(backgroundId))
              .slice(0, 5)
              .map((backgroundId) => `<div class="muted">unused battle bg 후보 · ${escapeHtml(backgroundId)}</div>`).join("")}
          </div>
          <div class="preset-stack">
            ${projectDashboard.floorEntries.map((entry) => `
              <div class="validation-line is-${entry.compileOk ? "info" : "error"}">
                <strong>F${entry.floor}</strong>
                ${escapeHtml(entry.mapId)} · map ${validationSummaryText(entry.report)} · compile ${entry.compileOk ? "ok" : validationSummaryText(entry.compileFailureReport || entry.report)}
              </div>
              ${entry.compileOk ? "" : validationBlockerMarkup(`F${entry.floor} first blocker`, entry.compileFailureReport || entry.report)}
            `).join("")}
          </div>
          ${projectDashboard.manifestReady
            ? `<div class="validation-line is-info"><strong>info</strong> contentBuildManifest를 바로 내보낼 수 있다.</div>`
            : `<div class="validation-line is-error"><strong>error</strong> 현재 blocker를 정리해야 contentBuildManifest를 내보낼 수 있다.</div>`}
        `,
      })}
    `;

    const presetLibraryMarkup = renderEditorPresetLibraryPanel({
      listMarkup: state.presetCatalog.map((entry) => {
        const preview = createPreviewGrid(entry, { width: 7, height: 7, rotation: 0 });
        return `
          <div class="preset-card ${state.selectedPresetId === entry.id ? "is-selected" : ""}">
            <div class="preset-card-head">
              <div class="preset-name">${entry.name}</div>
              <span class="preset-tag">${entry.kind}</span>
            </div>
            <div class="preset-canvas preset-preview-grid">${previewGridMarkup(preview, "preset-preview-cell")}</div>
            <div class="preset-meta">${entry.tags?.join(", ") || "untagged"}</div>
            <div class="row">
              <button data-preset-select="${entry.id}">선택</button>
              <label class="muted"><input type="checkbox" data-preset-active="${entry.id}" ${state.generationPresetIds.includes(entry.id) ? "checked" : ""} /> 생성</label>
            </div>
          </div>
        `;
      }).join(""),
    });

    return {
      toolPanelsMarkup,
      presetLibraryMarkup,
    };
  };
}
