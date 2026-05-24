export function createEditorEventPanelBridge() {
  return function buildEditorEventPanelBody(deps = {}) {
    const {
      eventDef = null,
      eventTool = "",
      eventDefId = "",
      compatiblePresets = [],
      eventValidationSnapshot = null,
      validationSummaryText = () => "",
      eventProjectReviewBundle = { totals: {}, issueLines: [], events: [] },
      currentBundleExportJson = "",
      previousBundleArchiveDiff = null,
      previousBundleArchive = null,
      eventExportArchiveLine = () => "",
      eventBundleJsonDiffText = "",
      eventBundleStructuralCompare = null,
      eventBundleCompareOptions = [],
      selectedEventBundleCompareEventId = "",
      selectedEventBundleCompareRow = null,
      selectedEventBundleComparePrevious = null,
      selectedEventBundleCompareCurrent = null,
      selectedEventBundleVisualDiffRows = [],
      selectedEventBundleComparePatch = null,
      eventBundlePatchDraftValue = "",
      selectedEventBundleResolvedPreview = null,
      filteredEventBundlePatchHistory = [],
      eventBundlePatchHistory = [],
      filteredEventBundlePatchArchive = [],
      eventBundlePatchArchive = [],
      eventBundlePatchArchiveQuery = "",
      selectedEventBundlePatchArchiveEntryId = "",
      selectedEventBundlePatchArchiveEntry = null,
      eventBundlePatchArchiveLine = () => "",
      eventBundleFocusOptions = [],
      selectedEventBundleFocusPath = "",
      focusedEventBundlePreviousValue = null,
      focusedEventBundleCurrentValue = null,
      focusedEventBundleResolvedValue = null,
      renderEditorEventExportArchiveBody = () => "",
      eventExportArchiveDeps = {},
      renderEditorEventGraphBody = () => "",
      selectedEventStepDef = null,
      selectedEventStepDefState = null,
      eventGraphPreview = [],
      currentGraphExportJson = "",
      previousGraphArchiveDiff = null,
      previousGraphArchive = null,
      eventGraphJsonDiffText = "",
      eventGraphSummaryDiff = null,
      activeEditorEventTest = null,
      activeEditorEventInteraction = null,
      classes = [],
      resourceKeys = [],
      partyStatKeys = [],
      eventEffectTypes = [],
      eventPlacementKind = "",
      allowedInteractionsForPlacementKind = [],
      linkedPlacements = [],
      linkedIssues = [],
      effectJson = () => "[]",
      eventStepsJson = () => "[]",
      renderEventEffectFields = () => "",
      escapeHtml = (value) => String(value ?? ""),
      buildDiffBadgeSpec = () => ({}),
      buildDiffCountScaleLabel = () => "",
      renderDiffBadgeHtml = () => "",
      eventEditorToPlacementKind = {},
      eventTriggerTypes = [],
    } = deps;

    if (!eventDef) {
      return `<div class="preset-inspector"><div class="muted">선택된 event preset을 찾지 못했다.</div></div>`;
    }

    return `
      <div class="preset-inspector">
        <div class="preset-field">
          <label for="eventInspectorToolSelect">Event object type</label>
          <select id="eventInspectorToolSelect">${Object.keys(eventEditorToPlacementKind).map((tool) => `<option value="${tool}" ${eventTool === tool ? "selected" : ""}>${eventEditorToPlacementKind[tool]}</option>`).join("")}</select>
        </div>
        <div class="preset-field">
          <label for="eventDefinitionSelect">Event preset</label>
          <select id="eventDefinitionSelect">${compatiblePresets.map(([id, def]) => `<option value="${id}" ${eventDefId === id ? "selected" : ""}>${id} · ${def.name}</option>`).join("")}</select>
        </div>
        <div class="preset-field">
          <label for="eventPresetIdInput">Event preset ID</label>
          <input id="eventPresetIdInput" value="${eventDefId}" />
        </div>
        <div class="preset-field">
          <label for="eventPresetNameInput">Event preset name</label>
          <input id="eventPresetNameInput" value="${eventDef.name || ""}" />
        </div>
        <div class="preset-field">
          <label for="eventTypeInput">Event type</label>
          <input id="eventTypeInput" value="${eventDef.type || ""}" />
        </div>
        <div class="preset-field">
          <label>Event validation</label>
          <div class="validation-report">
            <div class="validation-line is-${eventValidationSnapshot?.summary.error ? "error" : "info"}"><strong>event</strong> ${eventValidationSnapshot ? validationSummaryText({ summary: eventValidationSnapshot.summary }) : "error 0 · warning 0 · info 0"}</div>
            ${(eventValidationSnapshot?.issues || []).slice(0, 6).map((issue) => `<div class="validation-line is-${issue.severity}"><strong>${escapeHtml(issue.scope)}</strong> ${escapeHtml(issue.message)}</div>`).join("") || `<div class="validation-line is-info"><strong>ok</strong> 현재 event validation issue 없음</div>`}
          </div>
        </div>
        <div class="preset-field">
          <label for="eventInteractionSelect">Event interaction</label>
          <select id="eventInteractionSelect">${eventTriggerTypes.map((interaction) => `<option value="${interaction}" ${(eventDef.interaction || "interact") === interaction ? "selected" : ""}>${interaction}</option>`).join("")}</select>
        </div>
        <div class="preset-field">
          <label>Project event review bundle</label>
          <div class="validation-report">
            <div class="validation-line is-${eventProjectReviewBundle.totals.error ? "error" : "info"}"><strong>project</strong> event ${eventProjectReviewBundle.totals.eventCount} · error ${eventProjectReviewBundle.totals.error} · warning ${eventProjectReviewBundle.totals.warning} · placement ${eventProjectReviewBundle.totals.linkedPlacementCount}</div>
            <div class="validation-line is-${eventProjectReviewBundle.totals.danglingDefaultTargets || eventProjectReviewBundle.totals.danglingBranchTargets || eventProjectReviewBundle.totals.danglingChoiceTargets ? "error" : "info"}"><strong>graph</strong> default ${eventProjectReviewBundle.totals.danglingDefaultTargets} · branch ${eventProjectReviewBundle.totals.danglingBranchTargets} · choice ${eventProjectReviewBundle.totals.danglingChoiceTargets}</div>
            <div class="validation-line is-${eventProjectReviewBundle.totals.npcHandoffIssues ? "warning" : "info"}"><strong>handoff</strong> npc issue ${eventProjectReviewBundle.totals.npcHandoffIssues}</div>
            ${eventProjectReviewBundle.issueLines.slice(0, 8).map((issue) => `<div class="validation-line is-${issue.severity}"><strong>${escapeHtml(issue.eventId)}</strong> ${escapeHtml(issue.message)}</div>`).join("") || `<div class="validation-line is-info"><strong>ok</strong> project-level graph review issue 없음</div>`}
          </div>
        </div>
        <div class="preset-field">
          <label for="eventProjectReviewBundleInput">Project review bundle JSON</label>
          <div class="preset-toolbar">
            <button id="copyEventProjectReviewBundleBtn">bundle 복사</button>
            <button id="downloadEventProjectReviewBundleBtn">bundle 다운로드</button>
          </div>
          <textarea id="eventProjectReviewBundleInput" rows="10" spellcheck="false" readonly>${escapeHtml(currentBundleExportJson)}</textarea>
        </div>
        <div class="preset-field">
          <label>Previous bundle summary diff</label>
          <div class="validation-report">
            ${previousBundleArchiveDiff
              ? `<div class="validation-line is-info"><strong>delta</strong> step ${previousBundleArchiveDiff.stepDelta >= 0 ? "+" : ""}${previousBundleArchiveDiff.stepDelta} · branch ${previousBundleArchiveDiff.branchDelta >= 0 ? "+" : ""}${previousBundleArchiveDiff.branchDelta} · choice ${previousBundleArchiveDiff.choiceDelta >= 0 ? "+" : ""}${previousBundleArchiveDiff.choiceDelta}</div>
                 <div class="validation-line is-muted"><strong>prev</strong> ${escapeHtml(eventExportArchiveLine(previousBundleArchive))}</div>`
              : `<div class="validation-line is-info"><strong>info</strong> 비교할 이전 bundle archive 없음</div>`}
          </div>
        </div>
        <div class="preset-field">
          <label for="eventBundleJsonDiffInput">Bundle JSON diff</label>
          <div class="preset-toolbar">
            <button id="downloadEventBundleDiffBtn" ${eventBundleJsonDiffText ? "" : "disabled"}>bundle diff 다운로드</button>
          </div>
          <textarea id="eventBundleJsonDiffInput" rows="12" spellcheck="false" readonly>${escapeHtml(eventBundleJsonDiffText || "비교 가능한 이전 bundle archive payload가 없다.")}</textarea>
        </div>
        <div class="preset-field">
          <label>Bundle structural compare</label>
          <div class="validation-report">
            ${eventBundleStructuralCompare
              ? `
                <div class="validation-line is-info"><strong>compare</strong> added ${eventBundleStructuralCompare.added.length} · removed ${eventBundleStructuralCompare.removed.length} · changed ${eventBundleStructuralCompare.changed.length} · unchanged ${eventBundleStructuralCompare.unchanged.length}</div>
                ${eventBundleStructuralCompare.added.slice(0, 4).map((eventId) => `<div class="validation-line is-info"><strong>added</strong> ${escapeHtml(eventId)}</div>`).join("")}
                ${eventBundleStructuralCompare.removed.slice(0, 4).map((eventId) => `<div class="validation-line is-warning"><strong>removed</strong> ${escapeHtml(eventId)}</div>`).join("")}
                ${eventBundleStructuralCompare.changed.slice(0, 6).map((row) => `<div class="validation-line is-info"><strong>${escapeHtml(row.eventId)}</strong>${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(row.diffs.length)))} ${escapeHtml(row.diffs.join(" · "))}</div>`).join("") || `<div class="validation-line is-info"><strong>ok</strong> changed row 없음</div>`}
              `
              : `<div class="validation-line is-info"><strong>info</strong> 비교 가능한 이전 bundle archive payload 없음</div>`}
          </div>
          ${eventBundleCompareOptions.length ? `
            <div class="preset-stack">
              <label for="eventBundleCompareEventSelect">Compare event row</label>
              <select id="eventBundleCompareEventSelect">
                ${eventBundleCompareOptions.map((row) => {
                  const badge = row.status === "changed"
                    ? `[changed ${Array.isArray(row.detail?.split?.(" · ")) ? row.detail.split(" · ").length : 1}]`
                    : row.status === "added"
                      ? "[added]"
                      : "[removed]";
                  return `<option value="${row.eventId}" ${row.eventId === selectedEventBundleCompareEventId ? "selected" : ""}>${escapeHtml(row.eventId)} · ${row.status}${row.detail && row.detail !== row.status ? ` · ${escapeHtml(row.detail)}` : ""} ${badge}</option>`;
                }).join("")}
              </select>
              <div class="muted">${selectedEventBundleCompareRow ? `${selectedEventBundleCompareRow.status}${renderDiffBadgeHtml(buildDiffBadgeSpec(selectedEventBundleCompareRow.status === "changed" ? buildDiffCountScaleLabel((selectedEventBundleCompareRow.detail || "").split(" · ").filter(Boolean).length || 1) : selectedEventBundleCompareRow.status))} · ${escapeHtml(selectedEventBundleCompareRow.detail || selectedEventBundleCompareRow.status)}` : "선택 row 없음"}</div>
              <label for="eventBundleComparePreviousJson">Previous row</label>
              <textarea id="eventBundleComparePreviousJson" rows="8" spellcheck="false" readonly>${escapeHtml(JSON.stringify(selectedEventBundleComparePrevious || {}, null, 2))}</textarea>
              <label for="eventBundleCompareCurrentJson">Current row</label>
              <textarea id="eventBundleCompareCurrentJson" rows="8" spellcheck="false" readonly>${escapeHtml(JSON.stringify(selectedEventBundleCompareCurrent || {}, null, 2))}</textarea>
              <label>Visual diff highlight</label>
              <div class="validation-report">
                <div class="validation-line is-info"><strong>paths</strong>${renderDiffBadgeHtml(buildDiffBadgeSpec(buildDiffCountScaleLabel(selectedEventBundleVisualDiffRows.length)))} changed path ${selectedEventBundleVisualDiffRows.length}</div>
                ${selectedEventBundleVisualDiffRows.length
                  ? selectedEventBundleVisualDiffRows.slice(0, 12).map((entry) => `
                    <div class="validation-line is-${entry.status === "removed" ? "warning" : "info"}">
                      <strong>${escapeHtml(entry.path)}</strong>
                      prev ${escapeHtml(entry.previousText)} -> curr ${escapeHtml(entry.currentText)}
                    </div>
                  `).join("")
                  : `<div class="validation-line is-info"><strong>info</strong> highlight할 diff row가 없다.</div>`}
                ${selectedEventBundleVisualDiffRows.length > 12 ? `<div class="validation-line is-muted"><strong>more</strong> ${selectedEventBundleVisualDiffRows.length - 12}개 path는 patch export에서 계속 볼 수 있다.</div>` : ""}
              </div>
              <label for="eventBundleComparePatchJson">Patch export</label>
              <div class="preset-toolbar">
                <button id="downloadEventBundlePatchBtn" ${selectedEventBundleComparePatch ? "" : "disabled"}>patch 다운로드</button>
                <button id="resetEventBundlePatchDraftBtn" ${selectedEventBundleComparePatch ? "" : "disabled"}>patch 초안 초기화</button>
              </div>
              <textarea id="eventBundleComparePatchJson" rows="8" spellcheck="false">${escapeHtml(eventBundlePatchDraftValue)}</textarea>
              <label for="eventBundleResolvedPreviewJson">Resolved row preview</label>
              <div class="preset-toolbar">
                <button id="applyResolvedEventBundleRowBtn" ${selectedEventBundleResolvedPreview && selectedEventBundleCompareEventId ? "" : "disabled"}>resolved row 적용</button>
              </div>
              <textarea id="eventBundleResolvedPreviewJson" rows="8" spellcheck="false" readonly>${escapeHtml(JSON.stringify(selectedEventBundleResolvedPreview, null, 2))}</textarea>
              <div class="validation-report">
                <div class="validation-line is-info"><strong>search</strong> history ${filteredEventBundlePatchHistory.length}/${eventBundlePatchHistory.length} · archive ${filteredEventBundlePatchArchive.length}/${eventBundlePatchArchive.length}</div>
              </div>
              <div class="preset-field">
                <label for="eventBundlePatchArchiveQueryInput">Patch archive search</label>
                <input id="eventBundlePatchArchiveQueryInput" value="${escapeHtml(eventBundlePatchArchiveQuery)}" placeholder="action, eventId, label 검색" />
              </div>
              <div class="validation-report">
                <div class="validation-line is-info"><strong>history</strong> ${filteredEventBundlePatchHistory.length}개 recent patch action</div>
                ${filteredEventBundlePatchHistory.slice(0, 6).map((entry) => `<div class="validation-line is-info"><strong>${escapeHtml(entry.action || "patch")}</strong> ${escapeHtml(entry.eventId || selectedEventBundleCompareEventId || "event")} · ${escapeHtml(entry.label || "")} · ${escapeHtml(entry.actedAt || "")}</div>`).join("") || `<div class="validation-line is-info"><strong>info</strong> 검색 결과 recent patch action이 없다.</div>`}
              </div>
              <div class="validation-report">
                <div class="validation-line is-info"><strong>archive</strong> ${filteredEventBundlePatchArchive.length}개 persistent patch action</div>
                ${filteredEventBundlePatchArchive.slice(0, 6).map((entry) => `<div class="validation-line is-info"><strong>${escapeHtml(entry.action || "patch")}</strong> ${escapeHtml(entry.eventId || selectedEventBundleCompareEventId || "event")} · ${escapeHtml(entry.label || "")} · ${escapeHtml(entry.archivedAt || "")}</div>`).join("") || `<div class="validation-line is-info"><strong>info</strong> 검색 결과 patch archive가 없다.</div>`}
              </div>
              ${filteredEventBundlePatchArchive.length ? `
                <div class="preset-stack">
                  <label for="eventBundlePatchArchiveEntrySelect">Archive restore target</label>
                  <select id="eventBundlePatchArchiveEntrySelect">
                    ${filteredEventBundlePatchArchive.map((entry) => `<option value="${entry.id}" ${entry.id === selectedEventBundlePatchArchiveEntryId ? "selected" : ""}>${escapeHtml(eventBundlePatchArchiveLine(entry))}</option>`).join("")}
                  </select>
                  <div class="preset-toolbar">
                    <button id="restoreEventBundlePatchArchiveBtn" ${selectedEventBundlePatchArchiveEntry?.patchDraft || selectedEventBundlePatchArchiveEntry?.payload ? "" : "disabled"}>archive 복원</button>
                  </div>
                  <label for="eventBundlePatchArchivePreviewJson">Archive patch preview</label>
                  <textarea id="eventBundlePatchArchivePreviewJson" rows="6" spellcheck="false" readonly>${escapeHtml(selectedEventBundlePatchArchiveEntry?.patchDraft || JSON.stringify(selectedEventBundlePatchArchiveEntry?.payload || {}, null, 2))}</textarea>
                </div>
              ` : ""}
              ${eventBundleFocusOptions.length ? `
                <label for="eventBundleFocusPathSelect">Focused path</label>
                <select id="eventBundleFocusPathSelect">
                  ${eventBundleFocusOptions.map((path) => `<option value="${escapeHtml(path)}" ${path === selectedEventBundleFocusPath ? "selected" : ""}>${escapeHtml(path)}</option>`).join("")}
                </select>
                <div class="preset-toolbar">
                  <button id="copyEventBundleFocusedValueBtn" ${selectedEventBundleFocusPath ? "" : "disabled"}>focused value 복사</button>
                  <button id="copyEventBundleFocusedPreviousBtn" ${selectedEventBundleFocusPath ? "" : "disabled"}>previous 복사</button>
                  <button id="copyEventBundleFocusedCurrentBtn" ${selectedEventBundleFocusPath ? "" : "disabled"}>current 복사</button>
                  <button id="copyEventBundleFocusedResolvedBtn" ${selectedEventBundleFocusPath ? "" : "disabled"}>resolved 복사</button>
                </div>
                <label for="eventBundleFocusedPreviousJson">Focused previous</label>
                <textarea id="eventBundleFocusedPreviousJson" rows="4" spellcheck="false" readonly>${escapeHtml(JSON.stringify(focusedEventBundlePreviousValue, null, 2))}</textarea>
                <label for="eventBundleFocusedCurrentJson">Focused current</label>
                <textarea id="eventBundleFocusedCurrentJson" rows="4" spellcheck="false" readonly>${escapeHtml(JSON.stringify(focusedEventBundleCurrentValue, null, 2))}</textarea>
                <label for="eventBundleFocusedResolvedJson">Focused resolved</label>
                <textarea id="eventBundleFocusedResolvedJson" rows="4" spellcheck="false" readonly>${escapeHtml(JSON.stringify(focusedEventBundleResolvedValue, null, 2))}</textarea>
              ` : ""}
            </div>
          ` : ""}
        </div>
        ${renderEditorEventExportArchiveBody(eventExportArchiveDeps)}
        <div class="preset-field">
          <label>Project review rows</label>
          <div class="preset-stack">
            ${eventProjectReviewBundle.events.slice(0, 6).map((row) => `<div class="muted"><strong>${escapeHtml(row.eventId)}</strong> · step ${row.summaryDiff.stepCount} · branch ${row.summaryDiff.branchCount} · choice ${row.summaryDiff.choiceCount} · placement ${row.linkedPlacementCount} · ${validationSummaryText({ summary: row.validationSummary })}</div>`).join("")}
          </div>
        </div>
        <div class="preset-toolbar">
          <button id="duplicateEventPresetBtn">프리셋 복제</button>
          <button id="newEventPresetBtn">현재 도구용 새 프리셋</button>
        </div>
        <div class="preset-field">
          <label>Graph template quick-apply</label>
          <div class="preset-toolbar">
            <button id="applyAltarChoiceTemplateBtn">altar choice</button>
            <button id="applyTrapResolutionTemplateBtn">trap resolution</button>
            <button id="applyNpcHandoffTemplateBtn">npc handoff</button>
            <button id="applyBossGateTemplateBtn">boss gate</button>
          </div>
        </div>
        <div class="preset-field">
          <label for="eventUsageModeSelect">Usage mode</label>
          <select id="eventUsageModeSelect">
            ${["repeat", "uses", "cooldown"].map((mode) => `<option value="${mode}" ${eventDef.usage?.mode === mode ? "selected" : ""}>${mode}</option>`).join("")}
          </select>
        </div>
        <div class="preset-field">
          <label for="eventUsesRemainingInput">Uses remaining</label>
          <input id="eventUsesRemainingInput" type="number" min="0" value="${eventDef.usage?.usesRemaining ?? 0}" />
        </div>
        <div class="preset-field">
          <label for="eventCooldownStepsInput">Cooldown steps</label>
          <input id="eventCooldownStepsInput" type="number" min="0" value="${eventDef.usage?.cooldownSteps ?? 0}" />
        </div>
        ${eventTool === "trap" ? `
          <div class="preset-field">
            <label for="eventDetectionCheckInput">Detection check</label>
            <input id="eventDetectionCheckInput" value="${eventDef.detection?.check || ""}" />
          </div>
          <div class="preset-field">
            <label for="eventDetectionDifficultyInput">Detection difficulty</label>
            <input id="eventDetectionDifficultyInput" type="number" min="0" value="${eventDef.detection?.difficulty ?? 0}" />
          </div>
          <div class="preset-field">
            <label for="eventDisarmCheckInput">Disarm check</label>
            <input id="eventDisarmCheckInput" value="${eventDef.disarm?.check || ""}" />
          </div>
          <div class="preset-field">
            <label for="eventDisarmDifficultyInput">Disarm difficulty</label>
            <input id="eventDisarmDifficultyInput" type="number" min="0" value="${eventDef.disarm?.difficulty ?? 0}" />
          </div>
        ` : ""}
        <div class="preset-field">
          <label for="eventEffectsJsonInput">Effects JSON</label>
          <textarea id="eventEffectsJsonInput" rows="10" spellcheck="false">${escapeHtml(effectJson(eventDef))}</textarea>
        </div>
        <div class="preset-field">
          <label>Root effects</label>
          <div class="preset-stack">
            ${(eventDef.effects || []).map((effect, index) => `
              <div class="preset-stack">
                <div class="preset-toolbar">
                  <select data-event-root-effect-kind="${index}">
                    ${eventEffectTypes.map((kind) => `<option value="${kind}" ${effect.kind === kind ? "selected" : ""}>${kind}</option>`).join("")}
                  </select>
                  <button data-remove-event-root-effect="${index}">삭제</button>
                </div>
                ${renderEventEffectFields(effect, index, "event-root-effect")}
              </div>
            `).join("") || `<div class="muted">root effect 없음</div>`}
            <button id="addEventRootEffectBtn">root effect 추가</button>
          </div>
        </div>
        <div class="preset-field">
          <label for="eventEntryStepIdInput">Entry step ID</label>
          <input id="eventEntryStepIdInput" value="${eventDef.entryStepId || ""}" placeholder="optional · default first step" />
        </div>
        <div class="preset-field">
          <label for="eventStepsJsonInput">Step graph JSON</label>
          <textarea id="eventStepsJsonInput" rows="12" spellcheck="false">${escapeHtml(eventStepsJson(eventDef))}</textarea>
        </div>
        ${renderEditorEventGraphBody({
          eventDef,
          selectedEventStepDef,
          selectedEventStepDefState,
          eventGraphPreview,
          currentGraphExportJson,
          previousGraphArchiveDiff,
          previousGraphArchive,
          eventExportArchiveLine,
          eventGraphJsonDiffText,
          eventGraphSummaryDiff,
          activeEditorEventTest,
          activeEditorEventInteraction,
          eventValidationSnapshot,
          classes,
          resourceKeys,
          partyStatKeys,
          eventEffectTypes,
          eventPlacementKind,
          allowedInteractionsForPlacementKind,
          linkedPlacements,
          linkedIssues,
          renderEventEffectFields,
          escapeHtml,
        })}
      </div>
    `;
  };
}
