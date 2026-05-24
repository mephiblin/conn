export function createEditorValidationBridge(deps = {}) {
  const {
    escapeHtml = (value) => String(value ?? ""),
    buildProjectValidationReport = () => ({}),
    buildAssetValidationReport = () => ({}),
    buildProjectProgressionValidationReport = () => ({}),
    buildProjectKeyLockValidationReport = () => ({}),
    buildRequiredPlacementContractReport = () => ({}),
    buildRequiredContentReachabilityReport = () => ({}),
    compileProjectForRuntime = () => ({ ok: false, failures: [], compiledMaps: {} }),
    hasValidationErrors = () => false,
    validateMap = () => ({}),
    cellCoordKey = () => "",
    getCell = () => null,
    placementKindForTool = () => "",
    densityOverlayModes = new Set(),
    roomRecommendationRules = {},
    cellTagRecommendationRules = {},
    placementToolButtons = new Set(),
  } = deps;

  function validationSummaryText(report) {
    return `error ${report.summary.error} · warning ${report.summary.warning} · info ${report.summary.info}`;
  }

  function firstValidationIssue(report, severity = "error") {
    const issues = Array.isArray(report?.issues) ? report.issues : [];
    if (!severity) return issues[0] || null;
    return issues.find((issue) => issue.severity === severity) || issues[0] || null;
  }

  function validationIssueRepairHint(issue) {
    const message = issue?.message || "";
    if (!message) return "검증 리포트의 첫 error를 먼저 수정한 뒤 다시 빌드한다.";
    if (/required|필수|required target|도달|reach/i.test(message)) {
      return "필수 NPC/event/stairs 대상이 start에서 도달 가능한 walkable cell에 있는지 확인한다.";
    }
    if (/key|lock|locked|열쇠|잠금/i.test(message)) {
      return "잠긴 문보다 먼저 도달 가능한 위치에 대응 key item placement를 배치한다.";
    }
    if (/start|stairs|계단/i.test(message)) {
      return "start와 stairs placement의 floor/targetFloor, room 연결, 주변 walkable cell을 확인한다.";
    }
    if (/room|cell|placement|배치/i.test(message)) {
      return "최근 brush/placement 편집으로 roomId 또는 cellTag 계약이 깨졌는지 되돌리거나 재배치한다.";
    }
    if (/asset|material|texture|background/i.test(message)) {
      return "material/battle background 참조가 content registry와 asset manifest에 존재하는지 확인한다.";
    }
    return "해당 issue가 가리키는 map/content 필드를 수정한 뒤 compiledMap과 manifest를 다시 내보낸다.";
  }

  function validationBlockerMarkup(label, report) {
    const issue = firstValidationIssue(report);
    if (!issue) return `<div class="validation-line is-info"><strong>${escapeHtml(label)}</strong> blocker 없음</div>`;
    return `
      <div class="validation-line is-${issue.severity === "error" ? "error" : "warning"}">
        <strong>${escapeHtml(label)}</strong> ${escapeHtml(issue.message || "상세 메시지 없음")}
      </div>
      <div class="muted">repair hint · ${escapeHtml(validationIssueRepairHint(issue))}</div>
    `;
  }

  function buildProjectDashboardSnapshot(floorMaps) {
    const projectValidationReport = buildProjectValidationReport(floorMaps);
    const assetValidationReport = buildAssetValidationReport(floorMaps);
    const progressionValidationReport = buildProjectProgressionValidationReport(floorMaps);
    const keyLockValidationReport = buildProjectKeyLockValidationReport(floorMaps);
    const requiredPlacementContractReport = buildRequiredPlacementContractReport(floorMaps);
    const requiredContentValidationReport = buildRequiredContentReachabilityReport(floorMaps);
    const compiledProject = compileProjectForRuntime(floorMaps);
    const manifestReady = !hasValidationErrors(projectValidationReport) && compiledProject.ok;
    const firstCompileFailure = compiledProject.failures[0] || null;
    const manifestBlockerReport = hasValidationErrors(projectValidationReport)
      ? projectValidationReport
      : (firstCompileFailure?.report || projectValidationReport);
    return {
      projectValidationReport,
      assetValidationReport,
      progressionValidationReport,
      keyLockValidationReport,
      requiredPlacementContractReport,
      requiredContentValidationReport,
      compiledProject,
      manifestReady,
      firstCompileFailure,
      manifestBlockerReport,
      floorEntries: Object.entries(floorMaps || {})
        .map(([floor, map]) => {
          const report = validateMap(map);
          const compileFailure = compiledProject.failures.find((entry) => Number(entry.floor) === Number(floor));
          return {
            floor: Number(floor),
            mapId: map.id,
            report,
            compileOk: !compileFailure,
            compileFailureReport: compileFailure?.report || null,
          };
        })
        .sort((a, b) => a.floor - b.floor),
    };
  }

  function placementMatchesDensityMode(placement, mode) {
    if (!placement || mode === "none") return false;
    if (mode === "encounter") return placement.kind === "encounter";
    if (mode === "trap") return placement.kind === "trap";
    if (mode === "reward") return placement.kind === "item";
    if (mode === "recovery") return placement.kind === "shrine" || placement.kind === "rest_site" || placement.kind === "camp";
    if (mode === "camp") return placement.kind === "camp" || placement.kind === "rest_site";
    if (mode === "npc") return placement.kind === "npc";
    if (mode === "event") return placement.kind === "event_trigger" || placement.kind === "shrine" || placement.kind === "rest_site" || placement.kind === "camp";
    return false;
  }

  function buildDensityOverlaySnapshot(map, mode) {
    if (!map || !densityOverlayModes.has(mode) || mode === "none") {
      return { mode: "none", counts: {}, hotCells: 0, maxCount: 0, totalPlacements: 0 };
    }
    const counts = {};
    let hotCells = 0;
    let maxCount = 0;
    let totalPlacements = 0;
    (map.placements || []).forEach((placement) => {
      if (!placementMatchesDensityMode(placement, mode) || !placement.position) return;
      const key = cellCoordKey(placement.position.x, placement.position.y);
      counts[key] = (counts[key] || 0) + 1;
      totalPlacements += 1;
    });
    Object.values(counts).forEach((count) => {
      if (count > 0) hotCells += 1;
      if (count > maxCount) maxCount = count;
    });
    return { mode, counts, hotCells, maxCount, totalPlacements };
  }

  function buildDensityBucketEntries(counts = []) {
    const buckets = new Map();
    counts.forEach((count) => {
      if (count <= 0) return;
      buckets.set(count, (buckets.get(count) || 0) + 1);
    });
    return [...buckets.entries()]
      .sort((a, b) => b[0] - a[0])
      .map(([count, cells]) => ({ count, cells }));
  }

  function classifyDensityBand(count, maxCount) {
    if (!count || !maxCount) return "none";
    if (count >= maxCount) return "peak";
    const ratio = count / maxCount;
    if (ratio >= 0.75) return "high";
    if (ratio >= 0.4) return "medium";
    return "low";
  }

  function buildDensityBandSummary(counts = {}, maxCount = 0) {
    const summary = { low: 0, medium: 0, high: 0, peak: 0 };
    Object.values(counts).forEach((count) => {
      const band = classifyDensityBand(Number(count || 0), maxCount);
      if (band !== "none") summary[band] += 1;
    });
    return summary;
  }

  function buildDensityHistogramSection(cells = [], densityCounts = {}) {
    const counts = cells.map((cell) => Number(densityCounts[cellCoordKey(cell.x, cell.y)] || 0));
    const hotCounts = counts.filter((count) => count > 0);
    return {
      totalCells: cells.length,
      hotCells: hotCounts.length,
      totalPlacements: hotCounts.reduce((sum, count) => sum + count, 0),
      maxCount: hotCounts.length ? Math.max(...hotCounts) : 0,
      buckets: buildDensityBucketEntries(hotCounts),
    };
  }

  function buildDensityHistogramSnapshot(map, densityOverlay, roomId, cellTag) {
    if (!map || !densityOverlay || densityOverlay.mode === "none") {
      return {
        mode: "none",
        global: buildDensityHistogramSection([], {}),
        room: buildDensityHistogramSection([], {}),
        tag: buildDensityHistogramSection([], {}),
        topCells: [],
      };
    }
    const counts = densityOverlay.counts || {};
    const cells = map.cells || [];
    const roomCells = roomId ? cells.filter((cell) => cell.roomId === roomId) : [];
    const tagCells = cellTag ? cells.filter((cell) => (cell.tags || []).includes(cellTag)) : [];
    const topCells = cells
      .map((cell) => ({
        x: cell.x,
        y: cell.y,
        roomId: cell.roomId || "",
        tags: cell.tags || [],
        count: Number(counts[cellCoordKey(cell.x, cell.y)] || 0),
      }))
      .filter((entry) => entry.count > 0)
      .sort((a, b) => b.count - a.count || a.y - b.y || a.x - b.x)
      .slice(0, 5);
    return {
      mode: densityOverlay.mode,
      global: buildDensityHistogramSection(cells, counts),
      room: buildDensityHistogramSection(roomCells, counts),
      tag: buildDensityHistogramSection(tagCells, counts),
      bandSummary: buildDensityBandSummary(counts, densityOverlay.maxCount),
      topCells,
    };
  }

  function densityHistogramMarkup(section, label) {
    if (!section.totalCells) return `<div class="muted">${escapeHtml(label)} 대상 없음</div>`;
    return `
      <div class="muted"><strong>${escapeHtml(label)}</strong> · cell ${section.totalCells} · hot ${section.hotCells} · total ${section.totalPlacements} · max ${section.maxCount}</div>
      ${section.buckets.length ? `
        <div class="preset-stack">
          ${section.buckets.map((bucket) => `<div class="muted">${bucket.count}개 배치 칸 · ${bucket.cells}칸</div>`).join("")}
        </div>
      ` : `<div class="muted">${escapeHtml(label)} hot cell 없음</div>`}
    `;
  }

  function toolLabelForRecommendation(tool) {
    if (tool === "eventTrigger") return "eventTrigger";
    if (tool === "restSite") return "restSite";
    return tool;
  }

  function densityModeForRecommendationTool(tool) {
    if (tool === "encounter") return "encounter";
    if (tool === "trap") return "trap";
    if (tool === "npc") return "npc";
    if (tool === "camp") return "camp";
    if (tool === "shrine" || tool === "restSite") return "recovery";
    if (tool === "eventTrigger") return "event";
    if (tool === "stairs") return "event";
    return "none";
  }

  function buildRoomPlacementSummary(map, roomId) {
    const placements = (map?.placements || []).filter((placement) => {
      if (!placement.position || !roomId) return false;
      return getCell(map, placement.position.x, placement.position.y)?.roomId === roomId;
    });
    return {
      total: placements.length,
      encounter: placements.filter((placement) => placement.kind === "encounter").length,
      trap: placements.filter((placement) => placement.kind === "trap").length,
      npc: placements.filter((placement) => placement.kind === "npc").length,
      event: placements.filter((placement) => placement.kind === "event_trigger" || placement.kind === "shrine" || placement.kind === "rest_site" || placement.kind === "camp").length,
      recovery: placements.filter((placement) => placement.kind === "shrine" || placement.kind === "rest_site" || placement.kind === "camp").length,
      camp: placements.filter((placement) => placement.kind === "camp" || placement.kind === "rest_site").length,
      stairs: placements.filter((placement) => placement.kind === "stairs").length,
      reward: placements.filter((placement) => placement.kind === "item").length,
    };
  }

  function buildPlacementRecommendations(map, roomType, cellTag, densityOverlay, roomSummary) {
    const entries = [];
    const seen = new Set();
    const addEntries = (sourceEntries = [], source) => {
      sourceEntries.forEach((entry) => {
        if (!entry?.tool || !placementToolButtons.has(entry.tool) || seen.has(entry.tool)) return;
        seen.add(entry.tool);
        entries.push({
          tool: entry.tool,
          label: toolLabelForRecommendation(entry.tool),
          source,
          reason: entry.reason,
        });
      });
    };
    addEntries(roomRecommendationRules[roomType] || [], "room");
    addEntries(cellTagRecommendationRules[cellTag] || [], "tag");
    return entries.map((entry) => {
      let score = entry.source === "room" ? 6 : 4;
      const notes = [entry.reason];
      const densityMode = densityModeForRecommendationTool(entry.tool);
      const kindKey = densityMode === "recovery" ? "recovery" : densityMode === "event" ? "event" : densityMode;
      const roomCount = Number(roomSummary?.[kindKey] || 0);
      if (roomCount === 0) {
        score += 4;
        notes.push("현재 room에 아직 없음");
      } else {
        score -= Math.min(3, roomCount * 2);
        notes.push(`현재 room ${roomCount}개`);
      }
      if (densityOverlay?.mode !== "none" && densityOverlay?.mode === densityMode) {
        score += densityOverlay.totalPlacements === 0 ? 2 : -Math.min(2, densityOverlay.maxCount);
        notes.push(`density ${densityOverlay.mode}`);
      }
      if (roomType === "boss_room" && entry.tool === "encounter") score += 3;
      if (roomType === "transition_room" && entry.tool === "stairs") score += 3;
      if (roomType === "npc_room" && entry.tool === "npc") score += 3;
      if (roomType === "safe_room" && (entry.tool === "camp" || entry.tool === "restSite")) score += 2;
      return {
        ...entry,
        score,
        notes,
        kind: placementKindForTool(entry.tool),
      };
    }).sort((a, b) => b.score - a.score || a.label.localeCompare(b.label));
  }

  return {
    validationSummaryText,
    firstValidationIssue,
    validationIssueRepairHint,
    validationBlockerMarkup,
    buildProjectDashboardSnapshot,
    buildDensityOverlaySnapshot,
    classifyDensityBand,
    buildDensityHistogramSnapshot,
    densityHistogramMarkup,
    toolLabelForRecommendation,
    densityModeForRecommendationTool,
    buildRoomPlacementSummary,
    buildPlacementRecommendations,
  };
}
