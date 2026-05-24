import { blocksMovement, collectPlacementsAt, getCell } from "./runtimeCore.js";
import { DIRS, VEC } from "./mapGeneration.js";

export function createMapCompiler(deps = {}) {
  const {
    classes = [],
    monsters = {},
    items = {},
    encounters = {},
    eventDefinitions = {},
    skills = {},
    npcs = {},
    vendors = {},
    lootTables = {},
    materialManifest = {},
    rarityDefinitions = {},
    affixDefinitions = {},
    affixPoolDefinitions = {},
    contentBuildDataFiles = [],
    roomTypes = [],
    cellTags = [],
    battleBackgrounds = [],
    themeBattleBackgrounds = {},
    placementKinds = new Set(),
    legacyPlacementKinds = new Set(),
    eventObjectPlacementKinds = new Set(),
    movementBlockingPlacementKinds = new Set(),
    eventEffectTypes = new Set(),
    eventTriggerTypes = new Set(),
    resourceKeys = new Set(),
    companionStateKeys = new Set(),
    partyStatKeys = new Set(),
    validMaterialLightingHints = new Set(),
    validMaterialLods = new Set(),
    generatedNormalMapKeys = new Set(),
    legacyMonsterToEncounter = {},
    legacyEventToTrigger = {},
    editorOnlyBoundaryKeys = new Set(),
    validateEventDefinitionsTable = () => {},
    validateClassDefinitionsTable = () => {},
    validateItemDefinitionsTable = () => {},
    validateVendorDefinitionsTable = () => {},
    validateLootTableDefinitionsTable = () => {},
    validateRarityDefinitionsTable = () => {},
    validateAffixDefinitionsTable = () => {},
    validateAffixPoolDefinitionsTable = () => {},
    validateNpcDefinitionsTable = () => {},
    validateQuestSeedReferenceIntegrity = () => {},
  } = deps;

  function normalizeTextureId(id, list, fallback) {
    return list.includes(id) ? id : fallback;
  }

  function collectBoundaryIssues(value, context, issues = [], seen = new WeakSet()) {
    if (!value || typeof value !== "object") return issues;
    if (seen.has(value)) return issues;
    seen.add(value);
    for (const [key, nested] of Object.entries(value)) {
      const path = `${context}.${key}`;
      if (editorOnlyBoundaryKeys.has(key)) issues.push({ severity: "error", message: `${path}는 runtime boundary에 들어가면 안 된다.`, code: "runtime_editor_boundary_leak" });
      collectBoundaryIssues(nested, path, issues, seen);
    }
    return issues;
  }

  function withBoundaryIssues(report, issues) {
    if (!issues.length) return report;
    const mergedIssues = [...(report?.issues || []), ...issues];
    return {
      ...(report || {}),
      summary: summarizeIssues(mergedIssues),
      issues: mergedIssues,
    };
  }

  function ensureBoundaryClean(value, context, report = null) {
    const issues = collectBoundaryIssues(value, context);
    if (report) return withBoundaryIssues(report, issues);
    if (issues.length) {
      const first = issues[0];
      throw new Error(`${context} 경계 검증 실패: ${first.message}`);
    }
    return report;
  }

  function summarizeIssues(issues = []) {
    return {
      error: issues.filter((issue) => issue.severity === "error").length,
      warning: issues.filter((issue) => issue.severity === "warning").length,
      info: issues.filter((issue) => issue.severity === "info").length,
    };
  }

  function isValidHexColor(value) {
    return typeof value === "string" && /^#([0-9a-fA-F]{6})$/.test(value.trim());
  }

  function wallKey(x, y, dir) {
    return `${x},${y},${dir}`;
  }

  function opposite(dir) {
    return DIRS[(DIRS.indexOf(dir) + 2) % 4];
  }

  function oppositeDoor(map, x, y, dir) {
    const v = VEC[dir];
    const key = wallKey(x + v.x, y + v.y, opposite(dir));
    return map.doors[key];
  }

  function roomCells(map, roomId) {
    return map.cells.filter((cell) => cell.roomId === roomId);
  }

  function roomPlacements(map, roomId) {
    return map.placements.filter((placement) => {
      if (!placement.position) return false;
      return getCell(map, placement.position.x, placement.position.y)?.roomId === roomId;
    });
  }

  function roomTagSet(cells) {
    return new Set(cells.flatMap((cell) => cell.tags || []));
  }

  function battleBackgroundFallback(map, cell) {
    if (cell?.battleBackgroundId) {
      return { source: "cell", id: cell.battleBackgroundId };
    }
    const tagFallback = (cell?.tags || []).find((tag) => tag.startsWith("battle_bg_") && battleBackgrounds.includes(tag));
    if (tagFallback) return { source: "tag", id: tagFallback };
    const themeFallback = themeBattleBackgrounds[map.theme];
    if (themeFallback) return { source: "theme", id: themeFallback };
    return { source: "default", id: "" };
  }

  function allowedInteractionsForPlacementKind(kind) {
    if (kind === "trap") return ["onEnter"];
    if (kind === "event_trigger") return ["interact", "onEnter", "onExit"];
    if (kind === "rest_site") return ["interact", "onRest"];
    if (kind === "camp") return ["interact", "onCamp"];
    return ["interact"];
  }

  function resolvePlacementEvent(placement) {
    const eventId = placement?.interaction?.eventId || placement?.refId || "";
    const baseEvent = eventDefinitions[eventId];
    if (!baseEvent) return null;
    const resolved = JSON.parse(JSON.stringify(baseEvent));
    const overrides = placement?.eventOverrides || {};
    if (overrides.usage) resolved.usage = { ...(resolved.usage || {}), ...overrides.usage };
    if (overrides.detection) resolved.detection = { ...(resolved.detection || {}), ...overrides.detection };
    if (overrides.disarm) resolved.disarm = { ...(resolved.disarm || {}), ...overrides.disarm };
    return resolved;
  }

  function createEventValidationEntry(scope, message, extra = {}) {
    return { severity: extra.severity || "error", scope, message, ...extra };
  }

  function eventEffectValidationEntries(ownerLabel, effects, scope = "event") {
    const issues = [];
    if (effects != null && !Array.isArray(effects)) {
      issues.push(createEventValidationEntry(scope, `${ownerLabel} effects는 array여야 한다.`));
      return issues;
    }
    for (const [index, effect] of (effects || []).entries()) {
      if (!eventEffectTypes.has(effect?.kind)) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] kind가 registry에 없다: ${effect?.kind || "(empty)"}`, { effectIndex: index }));
      if (effect?.kind === "log" && !(effect.message || effect.text)) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] log에는 message/text가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "set_flag" && !effect.flag) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] set_flag에는 flag가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "set_quest_seed_state" && !effect.questSeedId) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] set_quest_seed_state에는 questSeedId가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "set_quest_seed_state" && effect.status != null && typeof effect.status !== "string") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] set_quest_seed_state status는 string이어야 한다.`, { effectIndex: index }));
      if (effect?.kind === "open_npc_service" && !effect.npcPlacementId) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] open_npc_service에는 npcPlacementId가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "open_npc_service" && effect.npcPlacementId != null && typeof effect.npcPlacementId !== "string") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] open_npc_service npcPlacementId는 string이어야 한다.`, { effectIndex: index }));
      if (effect?.kind === "open_npc_service" && effect.serviceIndex != null && typeof effect.serviceIndex !== "number") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] open_npc_service serviceIndex는 number여야 한다.`, { effectIndex: index }));
      if ((effect?.kind === "damage_front" || effect?.kind === "heal_party" || effect?.kind === "consume_resource") && typeof effect.amount !== "number") {
        issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] ${effect.kind}에는 number amount가 필요하다.`, { effectIndex: index }));
      }
      if (effect?.kind === "damage_front" && effect.minHp != null && typeof effect.minHp !== "number") {
        issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] damage_front minHp는 number여야 한다.`, { effectIndex: index }));
      }
      if ((effect?.kind === "grant_xp_party" || effect?.kind === "restore_resource") && typeof effect.amount !== "number") {
        issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] ${effect.kind}에는 number amount가 필요하다.`, { effectIndex: index }));
      }
      if (effect?.kind === "consume_resource" && !effect.resource) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] consume_resource에는 resource가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "consume_resource" && effect.resource && !resourceKeys.has(effect.resource)) {
        issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] consume_resource resource가 지원되지 않는다: ${effect.resource}`, { effectIndex: index }));
      }
      if (effect?.kind === "restore_resource" && !effect.resource) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] restore_resource에는 resource가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "restore_resource" && effect.resource && !resourceKeys.has(effect.resource)) {
        issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] restore_resource resource가 지원되지 않는다: ${effect.resource}`, { effectIndex: index }));
      }
      if (effect?.kind === "grant_item" && !effect.itemId) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] grant_item에는 itemId가 필요하다.`, { effectIndex: index }));
      if (effect?.kind === "grant_item" && effect.itemId && !items[effect.itemId]) issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] grant_item itemId가 지원되지 않는다: ${effect.itemId}`, { effectIndex: index }));
      if (effect?.kind === "grant_item" && effect.quantity != null && typeof effect.quantity !== "number") issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] grant_item quantity는 number여야 한다.`, { effectIndex: index }));
      if ((effect?.kind === "cure_status_party" || effect?.kind === "add_status_front") && !effect.status) {
        issues.push(createEventValidationEntry(scope, `${ownerLabel} effect[${index}] ${effect.kind}에는 status가 필요하다.`, { effectIndex: index }));
      }
    }
    return issues;
  }

  function eventDefinitionValidationIssues(eventId, event) {
    const issues = [];
    if (!event || typeof event !== "object") {
      issues.push(createEventValidationEntry("event", `${eventId} event 정의가 object가 아니다.`));
      return issues.map((issue) => issue.message);
    }
    if (!event.type) issues.push(createEventValidationEntry("event", `${eventId} event.type이 비어 있다.`));
    if (event.interaction && !eventTriggerTypes.has(event.interaction)) {
      issues.push(createEventValidationEntry("event", `${eventId} interaction이 지원되지 않는다: ${event.interaction}`));
    }
    issues.push(...eventEffectValidationEntries(eventId, event.effects || [], "event:root"));
    return issues.map((issue) => issue.message);
  }

  function collectEventEffects(event) {
    const effects = [...(event?.effects || [])];
    for (const step of event?.steps || []) {
      effects.push(...(step?.effects || []));
      for (const choice of step?.choices || []) effects.push(...(choice?.effects || []));
    }
    return effects;
  }

  function validationSummaryText(report) {
    return `error ${report.summary.error} · warning ${report.summary.warning} · info ${report.summary.info}`;
  }

  function reachable(map, sx, sy, allowUnlockableDoors = false) {
    const seen = new Set();
    const q = [[sx, sy]];
    seen.add(`${sx},${sy}`);
    while (q.length) {
      const [x, y] = q.shift();
      for (const dir of DIRS) {
        const v = VEC[dir];
        if (blocksMovement(map, x, y, dir, VEC, { allowUnlockableDoors })) continue;
        const n = getCell(map, x + v.x, y + v.y);
        const key = `${x + v.x},${y + v.y}`;
        if (n?.walkable && !seen.has(key)) {
          seen.add(key);
          q.push([n.x, n.y]);
        }
      }
    }
    return seen;
  }

  function normalizeRequiredPlacementContract(map) {
    if (!map.generation || typeof map.generation !== "object") map.generation = {};
    if (!Array.isArray(map.generation.requiredNpcPlacementIds)) map.generation.requiredNpcPlacementIds = [];
    if (!Array.isArray(map.generation.requiredEventPlacementIds)) map.generation.requiredEventPlacementIds = [];
    map.generation.requiredNpcPlacementIds = [...new Set(map.generation.requiredNpcPlacementIds.filter((id) => typeof id === "string" && id.trim()))];
    map.generation.requiredEventPlacementIds = [...new Set(map.generation.requiredEventPlacementIds.filter((id) => typeof id === "string" && id.trim()))];
    return map.generation;
  }

  function requiredNpcPlacementIds(map) {
    return normalizeRequiredPlacementContract(map).requiredNpcPlacementIds;
  }

  function requiredEventPlacementIds(map) {
    return normalizeRequiredPlacementContract(map).requiredEventPlacementIds;
  }

  function isRequiredNpcPlacement(map, placementId) {
    return requiredNpcPlacementIds(map).includes(placementId);
  }

  function isRequiredEventPlacement(map, placementId) {
    return requiredEventPlacementIds(map).includes(placementId);
  }

  function toggleRequiredPlacementContract(map, placementId, category) {
    const generation = normalizeRequiredPlacementContract(map);
    const field = category === "npc" ? "requiredNpcPlacementIds" : "requiredEventPlacementIds";
    if (generation[field].includes(placementId)) generation[field] = generation[field].filter((id) => id !== placementId);
    else generation[field].push(placementId);
  }

  function placementStateKey(map, placement) {
    return placement.stateKey || `${map.id || "map"}:${placement.id}`;
  }

  function normalizePlacement(map, placement) {
    const next = JSON.parse(JSON.stringify(placement || {}));
    if (next.kind === "monster") {
      const refId = legacyMonsterToEncounter[next.monsterId] || "";
      return {
        ...next,
        kind: "encounter",
        refType: "encounter",
        refId,
        stateKey: placementStateKey(map, next),
        legacyKind: "monster",
        legacyMonsterId: next.monsterId || null,
      };
    }
    if (next.kind === "event") {
      const eventId = next.eventId || legacyEventToTrigger[next.id] || "";
      return {
        ...next,
        kind: "event_trigger",
        refType: "event",
        refId: eventId,
        interaction: {
          type: next.interaction?.type || "interact",
          eventId,
        },
        stateKey: placementStateKey(map, next),
        legacyKind: "event",
      };
    }
    if (next.kind === "encounter") {
      next.refType = "encounter";
      if (!next.refId && next.monsterId) next.refId = legacyMonsterToEncounter[next.monsterId] || "";
      next.stateKey = placementStateKey(map, next);
    }
    if (next.kind === "event_trigger") {
      const eventId = next.interaction?.eventId || next.refId || "";
      next.refType = "event";
      next.refId = eventId;
      next.interaction = { type: next.interaction?.type || "interact", eventId };
      if (next.eventOverrides && typeof next.eventOverrides === "object") next.eventOverrides = JSON.parse(JSON.stringify(next.eventOverrides));
      next.stateKey = placementStateKey(map, next);
    }
    if (next.kind === "trap") {
      const effectMap = {
        poison: "event_trap_poison_dart",
        bleed: "event_trap_bleed_blade",
        curse: "event_trap_curse_rune",
      };
      const eventId = next.interaction?.eventId || next.refId || effectMap[next.effect] || "";
      next.refType = "event";
      next.refId = eventId;
      next.interaction = { type: next.interaction?.type || "onEnter", eventId };
      if (next.eventOverrides && typeof next.eventOverrides === "object") next.eventOverrides = JSON.parse(JSON.stringify(next.eventOverrides));
      next.stateKey = placementStateKey(map, next);
    }
    if (next.kind === "shrine" || next.kind === "rest_site" || next.kind === "camp") {
      const eventId = next.interaction?.eventId || next.refId || "";
      next.refType = "event";
      next.refId = eventId;
      next.interaction = { type: next.interaction?.type || "interact", eventId };
      if (next.eventOverrides && typeof next.eventOverrides === "object") next.eventOverrides = JSON.parse(JSON.stringify(next.eventOverrides));
      next.stateKey = placementStateKey(map, next);
    }
    if (next.kind === "npc") {
      next.refType = "npc";
      next.npcId = next.npcId || next.refId || "";
      next.refId = next.npcId;
      next.stateKey = placementStateKey(map, next);
    }
    return next;
  }

  function serializePlacement(placement) {
    const {
      done,
      legacyKind,
      legacyMonsterId,
      monsterId,
      eventId,
      eventRuntime,
      ...rest
    } = placement;
    return JSON.parse(JSON.stringify(rest));
  }

  function validateMap(map) {
    const issues = [];
    const addIssue = (severity, message, code) => {
      const key = `${severity}:${message}`;
      if (!issues.some((issue) => `${issue.severity}:${issue.message}` === key)) issues.push({ severity, message, code });
    };
    try {
      validateEventDefinitionsTable(eventDefinitions);
    } catch (error) {
      addIssue("error", error.message.replace(/^eventDefinitions 검증 실패:\s*/, ""), "event_definition_invalid");
    }
    const start = getCell(map, map.start.x, map.start.y);
    if (!start || !start.walkable) addIssue("error", "시작 지점이 이동 가능한 칸이 아니다.", "start_not_walkable");
    if (!map.placements.some((p) => p.kind === "stairs")) addIssue("error", "계단 배치가 없다.", "missing_stairs");
    for (let x = 0; x < map.size.width; x++) {
      if (getCell(map, x, 0)?.walkable || getCell(map, x, map.size.height - 1)?.walkable) addIssue("error", "외곽 상하 벽이 열려 있다.", "open_outer_wall_y");
    }
    for (let y = 0; y < map.size.height; y++) {
      if (getCell(map, 0, y)?.walkable || getCell(map, map.size.width - 1, y)?.walkable) addIssue("error", "외곽 좌우 벽이 열려 있다.", "open_outer_wall_x");
    }
    for (const room of map.rooms || []) {
      if (!room.id) addIssue("error", "room ID가 비어 있다.", "missing_room_id");
      if (!roomTypes.includes(room.roomType)) addIssue("error", `${room.id}의 roomType이 registry에 없다: ${room.roomType}`, "unknown_room_type");
    }
    for (const cell of map.cells) {
      for (const tag of cell.tags || []) {
        if (tag === "buried_temple" || tag === "stone") continue;
        if (tag.startsWith("battle_bg_")) continue;
        if (!cellTags.includes(tag)) addIssue("warning", `${cell.x},${cell.y} 셀의 태그가 registry에 없다: ${tag}`, "unknown_cell_tag");
      }
      if (cell.roomId && !(map.rooms || []).some((room) => room.id === cell.roomId)) addIssue("error", `${cell.x},${cell.y} 셀이 알 수 없는 roomId를 참조한다: ${cell.roomId}`, "unknown_room_id");
      if (cell.battleBackgroundId && !battleBackgrounds.includes(cell.battleBackgroundId)) {
        addIssue("warning", `${cell.x},${cell.y} 셀의 battleBackgroundId가 알려진 목록에 없다: ${cell.battleBackgroundId}`, "unknown_battle_background");
      }
    }
    const ids = new Set();
    for (const p of map.placements) {
      if (!p.id || ids.has(p.id)) addIssue("error", `배치 ID가 없거나 중복된다: ${p.id || "(empty)"}`, "duplicate_placement_id");
      ids.add(p.id);
      if (!placementKinds.has(p.kind)) addIssue("error", `${p.id}의 배치 종류가 registry에 없다: ${p.kind}`, "unknown_placement_kind");
      if (p.legacyKind || legacyPlacementKinds.has(p.kind)) {
        const targetKind = (p.kind === "encounter" || p.legacyKind === "monster") ? "encounter" : "event_trigger";
        const targetRef = p.refId || p.interaction?.eventId || p.monsterId || "(missing ref)";
        addIssue("warning", `${p.id}는 레거시 ${p.legacyKind || p.kind} 배치에서 마이그레이션됐다. ${targetKind}:${targetRef} 기준으로 다시 저장해 정리해야 한다.`, "legacy_placement_kind");
      }
      if (!p.position || p.position.x < 0 || p.position.y < 0 || p.position.x >= map.size.width || p.position.y >= map.size.height) {
        addIssue("error", `${p.id} 위치가 맵 범위를 벗어난다.`, "placement_out_of_bounds");
        continue;
      }
      const cell = getCell(map, p.position.x, p.position.y);
      if (!cell?.walkable) addIssue("error", `${p.id} 위치가 이동 가능한 칸이 아니다.`, "placement_not_walkable");
      if (p.kind === "monster" && !monsters[p.monsterId]) addIssue("error", `${p.id}가 알 수 없는 몬스터를 참조한다: ${p.monsterId}`, "unknown_monster_ref");
      if (p.kind === "encounter") {
        if (p.refType && p.refType !== "encounter") addIssue("error", `${p.id} encounter 배치의 refType이 잘못됐다: ${p.refType}`, "invalid_encounter_ref_type");
        if (!encounters[p.refId]) addIssue("error", `${p.id}가 알 수 없는 encounter를 참조한다: ${p.refId}`, "unknown_encounter_ref");
      }
      if (p.kind === "npc") {
        const npcId = p.npcId || p.refId;
        if (p.refType && p.refType !== "npc") addIssue("warning", `${p.id} npc 배치의 refType이 비표준이다: ${p.refType}`, "invalid_npc_ref_type");
        if (!npcId || !npcs[npcId]) addIssue("error", `${p.id}가 알 수 없는 npc를 참조한다: ${npcId || "(empty)"}`, "unknown_npc_ref");
        if (!(p.note || "").trim() && !(cell?.tags || []).includes("npc_anchor")) {
          addIssue("info", `${p.id} npc 배치는 npc_anchor 태그나 placement note가 있으면 의도가 더 분명해진다.`, "npc_placement_missing_anchor_context");
        }
      }
      if (eventObjectPlacementKinds.has(p.kind)) {
        const eventId = p.interaction?.eventId || p.refId;
        if (!eventId || !eventDefinitions[eventId]) addIssue("error", `${p.id}가 알 수 없는 ${p.kind} event를 참조한다: ${eventId || "(empty)"}`, "unknown_event_ref");
        if (!p.interaction?.type) addIssue("warning", `${p.id} ${p.kind} 배치에 interaction.type이 없다.`, "missing_event_interaction_type");
        const event = resolvePlacementEvent(p);
        if (p.kind === "trap") {
          if (!event?.detection) addIssue("warning", `${p.id} trap event에 detection 정의가 없다.`, "trap_missing_detection");
          if (!event?.disarm) addIssue("warning", `${p.id} trap event에 disarm 정의가 없다.`, "trap_missing_disarm");
        }
        if ((p.kind === "shrine" || p.kind === "rest_site" || p.kind === "camp") && !event?.usage) {
          addIssue("warning", `${p.id} ${p.kind} event에 uses/cooldown 정책이 없다.`, "event_missing_usage_policy");
        }
        if (event?.usage?.mode === "cooldown" && Number(event.usage.cooldownSteps || 0) <= 0) {
          addIssue("warning", `${p.id} ${p.kind} event의 cooldownSteps가 0 이하라서 재사용 정책이 비어 있다.`, "event_invalid_cooldown_policy");
        }
        if (!allowedInteractionsForPlacementKind(p.kind).includes(event?.interaction || "interact")) {
          addIssue("error", `${p.id} ${p.kind} 배치가 ${eventId}(${event?.interaction || "interact"})와 호환되지 않는다.`, "event_interaction_mismatch");
        }
        for (const [effectIndex, effect] of collectEventEffects(event).entries()) {
          if (effect?.kind === "open_npc_service") {
            const targetPlacement = map.placements.find((entry) => entry.id === effect.npcPlacementId && entry.kind === "npc");
            if (!targetPlacement) addIssue("error", `${p.id} effect[${effectIndex}] open_npc_service target npc placement가 없다: ${effect.npcPlacementId || "(empty)"}`, "event_npc_handoff_target_missing");
            else {
              const targetNpc = npcs[targetPlacement.npcId || targetPlacement.refId];
              const services = Array.isArray(targetNpc?.services) ? targetNpc.services : [];
              if (effect.serviceIndex != null && (Number(effect.serviceIndex) < 0 || Number(effect.serviceIndex) >= services.length)) {
                addIssue("warning", `${p.id} effect[${effectIndex}] open_npc_service serviceIndex가 target NPC service 범위를 벗어난다: ${effect.serviceIndex}`, "event_npc_handoff_service_index");
              }
            }
          }
        }
        for (const message of eventDefinitionValidationIssues(eventId, event)) {
          addIssue("warning", `${p.id} ${message}`, "event_definition_issue");
        }
      }
      if (p.kind === "item" && !items[p.itemId]) addIssue("error", `${p.id}가 알 수 없는 아이템을 참조한다: ${p.itemId}`, "unknown_item_ref");
      if (p.kind === "trap" && !p.interaction?.eventId && !p.effect) addIssue("warning", `${p.id} 함정에 eventId/effect가 없다.`, "trap_missing_effect");
      if ((p.kind === "rest_site" || p.kind === "camp") && !((cell?.tags || []).includes("camp_allowed") || (cell?.tags || []).includes("safe"))) {
        addIssue("warning", `${p.id} ${p.kind} 배치는 camp_allowed 또는 safe 태그가 있는 칸에 두는 것이 좋다.`, "rest_site_missing_safe_tag");
      }
      if (p.kind === "stairs" && !p.targetFloor && !p.final) addIssue("info", `${p.id} 계단은 현재 층 내부 메시지만 표시한다.`, "stairs_without_target");
    }
    for (const room of map.rooms || []) {
      const cells = roomCells(map, room.id);
      const placements = roomPlacements(map, room.id);
      const tags = roomTagSet(cells);
      const encounterPlacements = placements.filter((placement) => placement.kind === "encounter");
      const hasEncounter = encounterPlacements.length > 0;
      const hasTrap = placements.some((placement) => placement.kind === "trap");
      const hasStairs = placements.some((placement) => placement.kind === "stairs");
      const hasEvent = placements.some((placement) => placement.kind === "event_trigger" || placement.kind === "shrine" || placement.kind === "rest_site" || placement.kind === "camp");
      const hasItemReward = placements.some((placement) => placement.kind === "item");
      if (!cells.length) addIssue("warning", `${room.id} room에는 연결된 셀이 없다.`, "room_without_cells");
      if (room.roomType === "combat_room" && !hasEncounter && !hasEvent) {
        addIssue("warning", `${room.id} combat_room에는 encounter 또는 event_trigger가 필요하다.`, "combat_room_missing_activity");
      }
      if (room.roomType === "trap_room") {
        if (!hasTrap) addIssue("error", `${room.id} trap_room에는 함정 placement가 필요하다.`, "trap_room_missing_trap");
        if (!hasItemReward && !hasEvent) addIssue("warning", `${room.id} trap_room에는 보상 item 또는 event trigger가 필요하다.`, "trap_room_missing_reward");
      }
      if (room.roomType === "shrine_room" && !hasEvent) {
        addIssue("error", `${room.id} shrine_room에는 shrine/event_trigger 계열 placement가 필요하다.`, "shrine_room_missing_event");
      }
      if (room.roomType === "camp_room" && !tags.has("camp_allowed") && !tags.has("safe") && !placements.some((placement) => placement.kind === "rest_site" || placement.kind === "camp")) {
        addIssue("error", `${room.id} camp_room에는 camp_allowed/safe 태그 또는 rest_site/camp placement가 필요하다.`, "camp_room_missing_rest_anchor");
      }
      if (room.roomType === "safe_room") {
        if (!tags.has("safe") && !tags.has("save_allowed")) addIssue("warning", `${room.id} safe_room에는 safe 또는 save_allowed 태그가 필요하다.`, "safe_room_missing_safe_tag");
        if (hasEncounter) addIssue("error", `${room.id} safe_room에는 encounter를 둘 수 없다.`, "safe_room_has_encounter");
      }
      if (room.roomType === "boss_room") {
        if (!hasEncounter) addIssue("error", `${room.id} boss_room에는 boss encounter가 필요하다.`, "boss_room_missing_encounter");
        if (!tags.has("boss_anchor")) addIssue("warning", `${room.id} boss_room에는 boss_anchor 태그가 권장된다.`, "boss_room_missing_anchor");
      }
      if (room.roomType === "transition_room" && !hasStairs) {
        addIssue("error", `${room.id} transition_room에는 stairs placement가 필요하다.`, "transition_room_missing_stairs");
      }
      for (const placement of encounterPlacements) {
        const cell = getCell(map, placement.position.x, placement.position.y);
        const fallback = battleBackgroundFallback(map, cell);
        if (fallback.source === "default") {
          addIssue("warning", `${placement.id} encounter는 cell/tag/theme 전투 배경이 없어 기본 배경에 의존한다.`, "encounter_default_battle_background");
        } else if (fallback.source === "theme") {
          addIssue("info", `${placement.id} encounter는 map theme 전투 배경 fallback을 사용한다: ${fallback.id}`, "encounter_theme_battle_background");
        } else if (fallback.source === "tag") {
          addIssue("info", `${placement.id} encounter는 cell tag 전투 배경 fallback을 사용한다: ${fallback.id}`, "encounter_tag_battle_background");
        }
      }
    }
    for (const [key, door] of Object.entries(map.doors || {})) {
      const [x, y, dir] = key.split(",");
      if (!DIRS.includes(dir) || !getCell(map, Number(x), Number(y))) addIssue("error", `문 좌표가 유효하지 않다: ${key}`, "invalid_door_key");
      if (door.locked && door.keyId && !items[door.keyId]) addIssue("error", `잠긴 문이 알 수 없는 열쇠를 참조한다: ${door.keyId}`, "unknown_key_ref");
    }
    const reach = reachable(map, map.start.x, map.start.y, true);
    for (const p of map.placements.filter((p) => p.position && ["stairs", "monster", "event", "encounter", "event_trigger", "item", "trap", "shrine", "rest_site", "camp"].includes(p.kind))) {
      if (!reach.has(`${p.position.x},${p.position.y}`)) addIssue("error", `${p.id} 위치에 도달할 수 없다.`, "placement_unreachable");
    }
    addIssue("info", `도달 가능한 칸 ${reach.size}개 / 전체 칸 ${map.cells.length}개`, "reachable_cells");
    return {
      generatedAt: new Date().toISOString(),
      mapId: map.id,
      summary: summarizeIssues(issues),
      issues,
    };
  }

  function hasValidationErrors(report) {
    return report.summary.error > 0;
  }

  function compileMapForRuntime(map, report = validateMap(map)) {
    if (hasValidationErrors(report)) return { ok: false, report, compiledMap: null };
    const normalizedPlacements = map.placements.map((placement) => serializePlacement(normalizePlacement(map, placement)));
    const runtimeMap = {
      schemaVersion: map.schemaVersion || 1,
      id: map.id,
      name: map.name,
      theme: map.theme,
      size: { width: map.size.width, height: map.size.height, floors: map.size.floors || 1 },
      start: { ...map.start },
      cells: map.cells.map((cell) => ({
        x: cell.x,
        y: cell.y,
        walkable: Boolean(cell.walkable),
        roomId: cell.roomId || null,
        tags: [...(cell.tags || [])],
        tileRole: cell.tileRole || "",
        floorVariant: cell.floorVariant || "",
        decorTags: [...(cell.decorTags || [])],
        floorMaterialId: cell.floorMaterialId || cell.floorTexture,
        ceilingMaterialId: cell.ceilingMaterialId || cell.ceilingTexture,
        wallMaterialId: cell.wallMaterialId || cell.wallTexture,
        floorTexture: cell.floorTexture,
        ceilingTexture: cell.ceilingTexture,
        wallTexture: cell.wallTexture,
        battleBackgroundId: cell.battleBackgroundId || null,
        walls: JSON.parse(JSON.stringify(cell.walls || {})),
      })),
      rooms: JSON.parse(JSON.stringify(map.rooms || [])),
      doors: JSON.parse(JSON.stringify(map.doors || {})),
      placements: normalizedPlacements,
      lights: JSON.parse(JSON.stringify(map.lights || [])),
      decor: JSON.parse(JSON.stringify(map.decor || [])),
      generation: JSON.parse(JSON.stringify(map.generation || {})),
      tags: [...(map.tags || [])],
    };
    return {
      ok: true,
      report,
      compiledMap: {
        schemaVersion: 1,
        kind: "compiledMap",
        compiledAt: new Date().toISOString(),
        sourceMapId: map.id,
        reportSummary: report.summary,
        map: runtimeMap,
      },
    };
  }

  function cloneEditorMap(map) {
    const normalizedPlacements = map.placements.map((placement) => serializePlacement(normalizePlacement(map, placement)));
    return {
      schemaVersion: map.schemaVersion || 1,
      id: map.id,
      name: map.name,
      theme: map.theme,
      size: { ...map.size },
      start: { ...map.start },
      cells: map.cells.map((cell) => ({
        x: cell.x,
        y: cell.y,
        walkable: Boolean(cell.walkable),
        roomId: cell.roomId || null,
        tags: [...(cell.tags || [])],
        tileRole: cell.tileRole || "",
        floorVariant: cell.floorVariant || "",
        decorTags: [...(cell.decorTags || [])],
        floorMaterialId: cell.floorMaterialId || cell.floorTexture,
        ceilingMaterialId: cell.ceilingMaterialId || cell.ceilingTexture,
        wallMaterialId: cell.wallMaterialId || cell.wallTexture,
        floorTexture: cell.floorTexture,
        ceilingTexture: cell.ceilingTexture,
        wallTexture: cell.wallTexture,
        battleBackgroundId: cell.battleBackgroundId || null,
        walls: JSON.parse(JSON.stringify(cell.walls || {})),
      })),
      rooms: JSON.parse(JSON.stringify(map.rooms || [])),
      doors: JSON.parse(JSON.stringify(map.doors || {})),
      placements: normalizedPlacements,
      lights: JSON.parse(JSON.stringify(map.lights || [])),
      decor: JSON.parse(JSON.stringify(map.decor || [])),
      generation: JSON.parse(JSON.stringify(map.generation || {})),
      tags: [...(map.tags || [])],
    };
  }

  function buildCompiledMapForRuntime(map, report = validateMap(map)) {
    const result = compileMapForRuntime(map, report);
    if (!result.ok) return result;
    const boundaryReport = ensureBoundaryClean(result.compiledMap, "compiledMap", result.report);
    if (hasValidationErrors(boundaryReport)) return { ok: false, report: boundaryReport, compiledMap: null };
    return { ...result, report: boundaryReport };
  }

  function compileProjectForRuntime(floorMaps) {
    const compiledMaps = {};
    const failures = [];
    for (const [floor, map] of Object.entries(floorMaps || {})) {
      const result = buildCompiledMapForRuntime(map);
      if (!result.ok) failures.push({ floor: Number(floor), mapId: map.id, report: result.report });
      else compiledMaps[floor] = result.compiledMap;
    }
    return {
      ok: failures.length === 0,
      compiledMaps,
      failures,
    };
  }

  function sortedUniqueStrings(values) {
    return [...new Set((values || []).filter((value) => typeof value === "string" && value.trim()))].sort();
  }

  function buildContentValidationReport() {
    const issues = [];
    const addIssue = (severity, message, code) => {
      const key = `${severity}:${code}:${message}`;
      if (!issues.some((issue) => `${issue.severity}:${issue.code}:${issue.message}` === key)) issues.push({ severity, message, code });
    };
    const validators = [
      { code: "event_definitions_invalid", run: () => validateEventDefinitionsTable(eventDefinitions) },
      { code: "class_definitions_invalid", run: () => validateClassDefinitionsTable(classes) },
      { code: "item_definitions_invalid", run: () => validateItemDefinitionsTable(items) },
      { code: "vendor_definitions_invalid", run: () => validateVendorDefinitionsTable(vendors) },
      { code: "loot_table_definitions_invalid", run: () => validateLootTableDefinitionsTable(lootTables) },
      { code: "rarity_definitions_invalid", run: () => validateRarityDefinitionsTable(rarityDefinitions) },
      { code: "affix_definitions_invalid", run: () => validateAffixDefinitionsTable(affixDefinitions, rarityDefinitions) },
      { code: "affix_pool_definitions_invalid", run: () => validateAffixPoolDefinitionsTable(affixPoolDefinitions, affixDefinitions) },
      { code: "npc_definitions_invalid", run: () => validateNpcDefinitionsTable(npcs) },
      { code: "quest_seed_reference_invalid", run: () => validateQuestSeedReferenceIntegrity(npcs, eventDefinitions) },
    ];
    validators.forEach(({ code, run }) => {
      try {
        run();
      } catch (error) {
        addIssue("error", error.message, code);
      }
    });
    return {
      generatedAt: new Date().toISOString(),
      scope: "content",
      summary: summarizeIssues(issues),
      issues,
    };
  }

  function buildAssetValidationReport(floorMaps) {
    const issues = [];
    const addIssue = (severity, message, code, extra = {}) => {
      const key = `${severity}:${code}:${message}`;
      if (!issues.some((issue) => `${issue.severity}:${issue.code}:${issue.message}` === key)) {
        issues.push({ severity, message, code, ...extra });
      }
    };
    const referencedMaterialIds = new Set();
    const referencedBattleBackgroundIds = new Set();
    const metadataCounters = {
      invalidMaterialMetadata: 0,
      unusedMaterialCandidates: 0,
      unusedBattleBackgroundCandidates: 0,
    };
    Object.entries(materialManifest || {}).forEach(([materialId, materialDef]) => {
      if (!materialDef || typeof materialDef !== "object" || Array.isArray(materialDef)) {
        metadataCounters.invalidMaterialMetadata += 1;
        addIssue("error", `material ${materialId} 정의는 object여야 한다.`, "material_definition_invalid");
        return;
      }
      if (!isValidHexColor(materialDef.baseColor || "")) {
        metadataCounters.invalidMaterialMetadata += 1;
        addIssue("error", `material ${materialId} baseColor는 #RRGGBB 형식이어야 한다.`, "material_base_color_invalid");
      }
      if (!isValidHexColor(materialDef.fallbackColor || "")) {
        metadataCounters.invalidMaterialMetadata += 1;
        addIssue("warning", `material ${materialId} fallbackColor는 #RRGGBB 형식이어야 한다.`, "material_fallback_color_invalid");
      }
      if (materialDef.lightingHint != null && materialDef.lightingHint !== "" && !validMaterialLightingHints.has(materialDef.lightingHint)) {
        metadataCounters.invalidMaterialMetadata += 1;
        addIssue("error", `material ${materialId} lightingHint가 registry에 없다: ${materialDef.lightingHint}`, "material_lighting_hint_invalid");
      }
      if (materialDef.lod != null && materialDef.lod !== "" && !validMaterialLods.has(materialDef.lod)) {
        metadataCounters.invalidMaterialMetadata += 1;
        addIssue("error", `material ${materialId} lod가 registry에 없다: ${materialDef.lod}`, "material_lod_invalid");
      }
      const normalMapId = materialDef?.normalMap;
      if (normalMapId == null || normalMapId === "") return;
      if (typeof normalMapId !== "string") {
        addIssue("error", `material ${materialId} normalMap은 string이어야 한다.`, "material_normal_map_invalid_type");
        return;
      }
      if (!normalMapId.startsWith("generated://")) {
        addIssue("warning", `material ${materialId} normalMap은 아직 generated:// preset만 지원한다: ${normalMapId}`, "material_normal_map_unsupported");
        return;
      }
      const presetKey = normalMapId.replace("generated://", "");
      if (!generatedNormalMapKeys.has(presetKey)) {
        addIssue("error", `material ${materialId}가 알 수 없는 generated normalMap preset을 참조한다: ${normalMapId}`, "material_normal_map_missing_preset");
      }
    });
    Object.entries(floorMaps || {}).forEach(([floor, map]) => {
      if (map.theme && themeBattleBackgrounds[map.theme]) referencedBattleBackgroundIds.add(themeBattleBackgrounds[map.theme]);
      (map.cells || []).forEach((cell) => {
        if (cell.battleBackgroundId) referencedBattleBackgroundIds.add(cell.battleBackgroundId);
        if (cell.floorMaterialId) referencedMaterialIds.add(cell.floorMaterialId);
        if (cell.ceilingMaterialId) referencedMaterialIds.add(cell.ceilingMaterialId);
        if (cell.wallMaterialId) referencedMaterialIds.add(cell.wallMaterialId);
        if (cell.battleBackgroundId && !battleBackgrounds.includes(cell.battleBackgroundId)) {
          addIssue("error", `층 ${floor} · ${map.id}: ${cell.x},${cell.y} 셀이 알 수 없는 battleBackgroundId를 참조한다: ${cell.battleBackgroundId}`, "asset_missing_battle_background", { floor: Number(floor), mapId: map.id });
        }
        [["floor", cell.floorMaterialId], ["ceiling", cell.ceilingMaterialId], ["wall", cell.wallMaterialId]].forEach(([surface, materialId]) => {
          if (!materialId) return;
          if (!materialManifest?.[materialId]) {
            addIssue("error", `층 ${floor} · ${map.id}: ${cell.x},${cell.y} ${surface} material이 materialManifest에 없다: ${materialId}`, "asset_missing_material_id", { floor: Number(floor), mapId: map.id });
          }
        });
      });
      if (map.theme && themeBattleBackgrounds[map.theme] && !battleBackgrounds.includes(themeBattleBackgrounds[map.theme])) {
        addIssue("error", `층 ${floor} · ${map.id}: theme ${map.theme}의 전투 배경 fallback이 registry에 없다: ${themeBattleBackgrounds[map.theme]}`, "asset_missing_theme_battle_background", { floor: Number(floor), mapId: map.id });
      }
    });
    Object.keys(materialManifest || {}).sort().forEach((materialId) => {
      if (referencedMaterialIds.has(materialId)) return;
      metadataCounters.unusedMaterialCandidates += 1;
      addIssue("info", `material ${materialId}는 현재 floor cell에서 참조되지 않는다.`, "unused_material_candidate");
    });
    battleBackgrounds.filter(Boolean).sort().forEach((backgroundId) => {
      if (referencedBattleBackgroundIds.has(backgroundId)) return;
      metadataCounters.unusedBattleBackgroundCandidates += 1;
      addIssue("info", `battle background ${backgroundId}는 현재 floor/theme에서 참조되지 않는다.`, "unused_battle_background_candidate");
    });
    return {
      generatedAt: new Date().toISOString(),
      scope: "asset",
      summary: summarizeIssues(issues),
      metadataSummary: {
        ...metadataCounters,
        referencedMaterialIds: [...referencedMaterialIds].sort(),
        referencedBattleBackgroundIds: [...referencedBattleBackgroundIds].sort(),
        materialIds: Object.keys(materialManifest || {}).sort(),
        battleBackgroundIds: battleBackgrounds.filter(Boolean).slice().sort(),
      },
      issues,
    };
  }

  function encounterContainsMonster(encounterId, monsterId) {
    const encounter = encounters[encounterId];
    if (!encounter || !monsterId) return false;
    return (encounter.enemies || []).some((enemy) => enemy?.monsterId === monsterId);
  }

  function buildProjectProgressionValidationReport(floorMaps) {
    const issues = [];
    const addIssue = (severity, message, code, extra = {}) => {
      const key = `${severity}:${code}:${message}`;
      if (!issues.some((issue) => `${issue.severity}:${issue.code}:${issue.message}` === key)) {
        issues.push({ severity, message, code, ...extra });
      }
    };
    const floorNumbers = Object.keys(floorMaps || {}).map(Number).filter(Number.isFinite).sort((a, b) => a - b);
    if (!floorNumbers.length) addIssue("error", "project에 floor map이 없다.", "project_missing_floor_maps");
    if (floorNumbers.length && floorNumbers[0] !== 1) {
      addIssue("error", `첫 floor는 1이어야 한다. 현재 시작 floor는 ${floorNumbers[0]}이다.`, "project_missing_floor_1");
    }
    floorNumbers.forEach((floor, index) => {
      if (index > 0 && floor !== floorNumbers[index - 1] + 1) {
        addIssue("error", `floor index가 연속되지 않는다: ${floorNumbers[index - 1]} 다음에 ${floor}가 온다.`, "project_floor_gap");
      }
    });
    const highestFloor = floorNumbers[floorNumbers.length - 1] || 0;
    floorNumbers.forEach((floor) => {
      const map = floorMaps[floor];
      if (!map) return;
      if (Number(map.start?.floor || floor) !== floor) {
        addIssue("error", `층 ${floor} · ${map.id}: map.start.floor가 floor key와 다르다 (${map.start?.floor || "(empty)"}).`, "project_start_floor_mismatch", { floor, mapId: map.id });
      }
      const stairsPlacements = (map.placements || []).filter((placement) => placement.kind === "stairs");
      const outboundStairs = stairsPlacements.filter((placement) => Number.isFinite(Number(placement.targetFloor)));
      const finalStairs = stairsPlacements.filter((placement) => placement.final);
      if (floor < highestFloor && !outboundStairs.length) {
        addIssue("error", `층 ${floor} · ${map.id}: 다음 floor로 가는 stairs.targetFloor가 없다.`, "project_missing_outbound_stairs", { floor, mapId: map.id });
      }
      if (floor === highestFloor && !finalStairs.length) {
        addIssue("error", `층 ${floor} · ${map.id}: 최고 floor에는 final stairs가 필요하다.`, "project_missing_final_stairs", { floor, mapId: map.id });
      }
      outboundStairs.forEach((placement) => {
        const targetFloor = Number(placement.targetFloor);
        if (!floorMaps[targetFloor]) {
          addIssue("error", `층 ${floor} · ${map.id}: ${placement.id}가 없는 targetFloor를 참조한다: ${targetFloor}`, "project_missing_target_floor", { floor, mapId: map.id });
        }
        if (targetFloor <= floor) {
          addIssue("warning", `층 ${floor} · ${map.id}: ${placement.id} targetFloor ${targetFloor}는 현재 floor보다 높지 않다.`, "project_nonascending_target_floor", { floor, mapId: map.id });
        }
        if (placement.requiredBoss) {
          const hasBossEncounter = (map.placements || []).some((candidate) => candidate.kind === "encounter" && encounterContainsMonster(candidate.refId, placement.requiredBoss));
          if (!hasBossEncounter) {
            addIssue("error", `층 ${floor} · ${map.id}: ${placement.id} requiredBoss ${placement.requiredBoss}를 가진 encounter가 현재 floor에 없다.`, "project_missing_required_boss_encounter", { floor, mapId: map.id });
          }
        }
      });
      finalStairs.forEach((placement) => {
        if (placement.targetFloor) {
          addIssue("warning", `층 ${floor} · ${map.id}: final stairs ${placement.id}는 targetFloor 없이 종료 전용으로 두는 것이 좋다.`, "project_final_stairs_with_target_floor", { floor, mapId: map.id });
        }
        if (placement.requiredBoss) {
          const hasBossEncounter = (map.placements || []).some((candidate) => candidate.kind === "encounter" && encounterContainsMonster(candidate.refId, placement.requiredBoss));
          if (!hasBossEncounter) {
            addIssue("error", `층 ${floor} · ${map.id}: final stairs ${placement.id} requiredBoss ${placement.requiredBoss}를 가진 encounter가 현재 floor에 없다.`, "project_missing_final_required_boss_encounter", { floor, mapId: map.id });
          }
        }
      });
    });
    return { generatedAt: new Date().toISOString(), scope: "progression", summary: summarizeIssues(issues), issues };
  }

  function buildProjectKeyLockValidationReport(floorMaps) {
    const issues = [];
    const addIssue = (severity, message, code, extra = {}) => {
      const key = `${severity}:${code}:${message}`;
      if (!issues.some((issue) => `${issue.severity}:${issue.code}:${issue.message}` === key)) {
        issues.push({ severity, message, code, ...extra });
      }
    };
    Object.entries(floorMaps || {}).forEach(([floor, map]) => {
      const lockedDoors = Object.entries(map.doors || {}).filter(([, door]) => door?.locked && door?.keyId);
      if (!lockedDoors.length) return;
      const reachBeforeUnlock = reachable(map, map.start.x, map.start.y, false);
      lockedDoors.forEach(([doorKey, door]) => {
        const keyPlacements = (map.placements || []).filter((placement) => placement.kind === "item" && placement.itemId === door.keyId);
        if (!keyPlacements.length) {
          addIssue("error", `층 ${floor} · ${map.id}: 잠긴 문 ${doorKey}의 keyId ${door.keyId}에 대응하는 item placement가 없다.`, "project_missing_key_item_placement", { floor: Number(floor), mapId: map.id });
          return;
        }
        const reachableKeyPlacements = keyPlacements.filter((placement) => reachBeforeUnlock.has(`${placement.position?.x},${placement.position?.y}`));
        if (!reachableKeyPlacements.length) {
          addIssue("error", `층 ${floor} · ${map.id}: 잠긴 문 ${doorKey}의 열쇠 ${door.keyId}가 잠금 해제 전 도달 가능한 구역에 없다.`, "project_unreachable_key_before_unlock", { floor: Number(floor), mapId: map.id });
        }
      });
    });
    return { generatedAt: new Date().toISOString(), scope: "key_lock", summary: summarizeIssues(issues), issues };
  }

  function resolveRequiredTarget(map, targetId) {
    if (targetId === "start") {
      return { id: "start", label: "start", x: map.start?.x, y: map.start?.y, type: "start" };
    }
    const placement = (map.placements || []).find((entry) => entry.id === targetId);
    if (!placement?.position) return null;
    return { id: placement.id, label: `${placement.kind}:${placement.id}`, x: placement.position.x, y: placement.position.y, type: placement.kind };
  }

  function buildRequiredTargetIds(map) {
    const targetIds = new Set(map.generation?.lockedPoints || []);
    requiredNpcPlacementIds(map).forEach((id) => targetIds.add(id));
    requiredEventPlacementIds(map).forEach((id) => targetIds.add(id));
    (map.placements || []).filter((placement) => placement.kind === "stairs").forEach((placement) => targetIds.add(placement.id));
    return [...targetIds];
  }

  function buildRequiredPlacementContractReport(floorMaps) {
    const issues = [];
    const addIssue = (severity, message, code, extra = {}) => {
      const key = `${severity}:${code}:${message}`;
      if (!issues.some((issue) => `${issue.severity}:${issue.code}:${issue.message}` === key)) {
        issues.push({ severity, message, code, ...extra });
      }
    };
    Object.entries(floorMaps || {}).forEach(([floor, map]) => {
      requiredNpcPlacementIds(map).forEach((placementId) => {
        const placement = (map.placements || []).find((entry) => entry.id === placementId);
        if (!placement) {
          addIssue("error", `층 ${floor} · ${map.id}: requiredNpcPlacementIds가 없는 placement를 참조한다: ${placementId}`, "project_missing_required_npc_placement", { floor: Number(floor), mapId: map.id });
          return;
        }
        if (placement.kind !== "npc") {
          addIssue("error", `층 ${floor} · ${map.id}: requiredNpcPlacementIds ${placementId}의 kind가 npc가 아니다: ${placement.kind}`, "project_required_npc_kind_mismatch", { floor: Number(floor), mapId: map.id });
        }
      });
      requiredEventPlacementIds(map).forEach((placementId) => {
        const placement = (map.placements || []).find((entry) => entry.id === placementId);
        if (!placement) {
          addIssue("error", `층 ${floor} · ${map.id}: requiredEventPlacementIds가 없는 placement를 참조한다: ${placementId}`, "project_missing_required_event_placement", { floor: Number(floor), mapId: map.id });
          return;
        }
        if (!(eventObjectPlacementKinds.has(placement.kind) || placement.kind === "event_trigger")) {
          addIssue("error", `층 ${floor} · ${map.id}: requiredEventPlacementIds ${placementId}의 kind가 event 계열이 아니다: ${placement.kind}`, "project_required_event_kind_mismatch", { floor: Number(floor), mapId: map.id });
        }
      });
    });
    return { generatedAt: new Date().toISOString(), scope: "required_contract", summary: summarizeIssues(issues), issues };
  }

  function buildRequiredContentReachabilityReport(floorMaps) {
    const issues = [];
    const addIssue = (severity, message, code, extra = {}) => {
      const key = `${severity}:${code}:${message}`;
      if (!issues.some((issue) => `${issue.severity}:${issue.code}:${issue.message}` === key)) {
        issues.push({ severity, message, code, ...extra });
      }
    };
    Object.entries(floorMaps || {}).forEach(([floor, map]) => {
      const reachableCells = reachable(map, map.start.x, map.start.y, true);
      buildRequiredTargetIds(map).forEach((targetId) => {
        const target = resolveRequiredTarget(map, targetId);
        if (!target || !Number.isFinite(target.x) || !Number.isFinite(target.y)) {
          addIssue("error", `층 ${floor} · ${map.id}: required target ${targetId}를 해석할 수 없다.`, "project_missing_required_target", { floor: Number(floor), mapId: map.id });
          return;
        }
        if (!reachableCells.has(`${target.x},${target.y}`)) {
          addIssue("error", `층 ${floor} · ${map.id}: required target ${target.label} 위치 ${target.x},${target.y}에 도달할 수 없다.`, "project_unreachable_required_target", { floor: Number(floor), mapId: map.id });
        }
      });
    });
    return { generatedAt: new Date().toISOString(), scope: "required_content", summary: summarizeIssues(issues), issues };
  }

  function buildProjectValidationReport(floorMaps) {
    const contentReport = buildContentValidationReport();
    const assetReport = buildAssetValidationReport(floorMaps);
    const progressionReport = buildProjectProgressionValidationReport(floorMaps);
    const keyLockReport = buildProjectKeyLockValidationReport(floorMaps);
    const requiredPlacementContractReport = buildRequiredPlacementContractReport(floorMaps);
    const requiredContentReport = buildRequiredContentReachabilityReport(floorMaps);
    const issues = [
      ...contentReport.issues,
      ...assetReport.issues,
      ...progressionReport.issues,
      ...keyLockReport.issues,
      ...requiredPlacementContractReport.issues,
      ...requiredContentReport.issues,
    ];
    for (const [floor, map] of Object.entries(floorMaps || {})) {
      const report = validateMap(map);
      for (const issue of report.issues) {
        issues.push({
          severity: issue.severity,
          code: issue.code,
          floor: Number(floor),
          mapId: map.id,
          message: `층 ${floor} · ${map.id}: ${issue.message}`,
        });
      }
    }
    return { generatedAt: new Date().toISOString(), scope: "project", summary: summarizeIssues(issues), issues };
  }

  function mergeProjectCompileFailures(report, failures) {
    const issues = [...(report?.issues || [])];
    for (const failure of failures || []) {
      issues.push({
        severity: "error",
        code: "compiled_map_build_failed",
        floor: failure.floor,
        mapId: failure.mapId,
        message: `층 ${failure.floor} · ${failure.mapId}: compiledMap build 실패 (${validationSummaryText(failure.report)})`,
      });
    }
    return { generatedAt: new Date().toISOString(), scope: "project", summary: summarizeIssues(issues), issues };
  }

  function buildContentReferenceSummary(floorMaps, compiledProject) {
    const referencedEncounterIds = [];
    const referencedEventDefinitionIds = [];
    const referencedNpcDefinitionIds = [];
    const referencedItemIds = [];
    const referencedVendorIds = [];
    const referencedLootTableIds = [];
    const referencedBattleBackgroundIds = [];
    const referencedMaterialIds = [];
    const referencedNormalMapIds = [];
    const roomTypeValues = [];
    const cellTagValues = [];
    const placementKindValues = [];
    const mapIds = [];
    Object.values(floorMaps || {}).forEach((map) => {
      mapIds.push(map.id);
      (map.rooms || []).forEach((room) => roomTypeValues.push(room.roomType || ""));
      if (map.theme && themeBattleBackgrounds[map.theme]) referencedBattleBackgroundIds.push(themeBattleBackgrounds[map.theme]);
      (map.cells || []).forEach((cell) => {
        (cell.tags || []).forEach((tag) => cellTagValues.push(tag));
        if (cell.battleBackgroundId) referencedBattleBackgroundIds.push(cell.battleBackgroundId);
        if (cell.floorMaterialId) referencedMaterialIds.push(cell.floorMaterialId);
        if (cell.ceilingMaterialId) referencedMaterialIds.push(cell.ceilingMaterialId);
        if (cell.wallMaterialId) referencedMaterialIds.push(cell.wallMaterialId);
      });
      (map.placements || []).forEach((placement) => {
        placementKindValues.push(placement.kind || "");
        if (placement.kind === "encounter" && placement.refId) referencedEncounterIds.push(placement.refId);
        if (eventObjectPlacementKinds.has(placement.kind)) {
          const eventId = placement.interaction?.eventId || placement.refId;
          if (eventId) referencedEventDefinitionIds.push(eventId);
        }
        if (placement.kind === "npc") {
          const npcId = placement.npcId || placement.refId;
          if (npcId) referencedNpcDefinitionIds.push(npcId);
        }
        if (placement.kind === "item" && placement.itemId) referencedItemIds.push(placement.itemId);
      });
    });
    Object.values(npcs).forEach((npc) => {
      (npc.services || []).forEach((service) => {
        if (service.vendorId) referencedVendorIds.push(service.vendorId);
        if (service.encounterId) referencedEncounterIds.push(service.encounterId);
      });
    });
    Object.values(lootTables).forEach((table) => {
      if (!table || typeof table !== "object" || Array.isArray(table)) return;
      (table.guaranteed || []).forEach((entry) => entry.itemId && referencedItemIds.push(entry.itemId));
      (table.tierEntries || []).forEach((entry) => entry.itemId && referencedItemIds.push(entry.itemId));
      (table.bonusRolls || []).forEach((entry) => entry.tableId && referencedLootTableIds.push(entry.tableId));
    });
    (lootTables.combatRewardProfiles?.default || []).forEach((profile) => {
      if (profile.tableId) referencedLootTableIds.push(profile.tableId);
    });
    sortedUniqueStrings(referencedMaterialIds).forEach((materialId) => {
      const normalMapId = materialManifest?.[materialId]?.normalMap;
      if (normalMapId) referencedNormalMapIds.push(normalMapId);
    });
    return {
      mapIds: sortedUniqueStrings(mapIds),
      compiledMapIds: sortedUniqueStrings(Object.values(compiledProject?.compiledMaps || {}).map((compiledMap) => compiledMap?.map?.id || "")),
      roomTypes: sortedUniqueStrings(roomTypeValues),
      cellTags: sortedUniqueStrings(cellTagValues),
      placementKinds: sortedUniqueStrings(placementKindValues),
      contentRefs: {
        classDefinitionIds: classes.map((entry) => entry.id).filter(Boolean).sort(),
        monsterIds: Object.keys(monsters).sort(),
        itemDefinitionIds: Object.keys(items).sort(),
        encounterIds: Object.keys(encounters).sort(),
        eventDefinitionIds: Object.keys(eventDefinitions).sort(),
        skillIds: Object.keys(skills).sort(),
        npcDefinitionIds: Object.keys(npcs).sort(),
        vendorDefinitionIds: Object.keys(vendors).sort(),
        lootTableIds: Object.keys(lootTables).filter((id) => id !== "combatRewardProfiles").sort(),
        rarityIds: Object.keys(rarityDefinitions).sort(),
        affixIds: Object.keys(affixDefinitions).sort(),
        affixPoolIds: Object.keys(affixPoolDefinitions).sort(),
        materialIds: Object.keys(materialManifest || {}).sort(),
      },
      referencedIds: {
        encounterIds: sortedUniqueStrings(referencedEncounterIds),
        eventDefinitionIds: sortedUniqueStrings(referencedEventDefinitionIds),
        npcDefinitionIds: sortedUniqueStrings(referencedNpcDefinitionIds),
        itemDefinitionIds: sortedUniqueStrings(referencedItemIds),
        vendorDefinitionIds: sortedUniqueStrings(referencedVendorIds),
        lootTableIds: sortedUniqueStrings(referencedLootTableIds),
      },
      assetRefs: {
        battleBackgroundIds: sortedUniqueStrings(referencedBattleBackgroundIds),
        materialIds: sortedUniqueStrings(referencedMaterialIds),
        normalMapIds: sortedUniqueStrings(referencedNormalMapIds),
      },
    };
  }

  function buildContentBuildManifest(floorMaps) {
    const validationReport = buildProjectValidationReport(floorMaps);
    const assetValidationReport = buildAssetValidationReport(floorMaps);
    const progressionValidationReport = buildProjectProgressionValidationReport(floorMaps);
    const keyLockValidationReport = buildProjectKeyLockValidationReport(floorMaps);
    const requiredPlacementContractReport = buildRequiredPlacementContractReport(floorMaps);
    const requiredContentValidationReport = buildRequiredContentReachabilityReport(floorMaps);
    if (hasValidationErrors(validationReport)) {
      return { ok: false, report: validationReport, manifest: null, compiledProject: null };
    }
    const compiledProject = compileProjectForRuntime(floorMaps);
    const compileAwareReport = compiledProject.ok ? validationReport : mergeProjectCompileFailures(validationReport, compiledProject.failures);
    if (!compiledProject.ok) {
      return { ok: false, report: compileAwareReport, manifest: null, compiledProject };
    }
    const manifest = {
      schemaVersion: 1,
      kind: "contentBuildManifest",
      projectId: "project_serpent_temple",
      projectName: "Serpent Temple",
      generatedAt: new Date().toISOString(),
      validationSummary: compileAwareReport.summary,
      assetValidationSummary: assetValidationReport.summary,
      assetMetadataSummary: assetValidationReport.metadataSummary,
      progressionValidationSummary: progressionValidationReport.summary,
      keyLockValidationSummary: keyLockValidationReport.summary,
      requiredPlacementContractSummary: requiredPlacementContractReport.summary,
      requiredContentValidationSummary: requiredContentValidationReport.summary,
      dataFiles: contentBuildDataFiles.map((entry) => ({ ...entry })),
      compiledMaps: Object.entries(compiledProject.compiledMaps).map(([floor, compiledMap]) => ({
        floor: Number(floor),
        mapId: compiledMap.map.id,
        name: compiledMap.map.name,
        sourceMapId: compiledMap.sourceMapId,
      })),
      referenceSummary: buildContentReferenceSummary(floorMaps, compiledProject),
    };
    ensureBoundaryClean(manifest, "contentBuildManifest", compileAwareReport);
    return { ok: true, report: compileAwareReport, manifest, compiledProject };
  }

  return {
    validateMap,
    hasValidationErrors,
    normalizeRequiredPlacementContract,
    requiredNpcPlacementIds,
    requiredEventPlacementIds,
    isRequiredNpcPlacement,
    isRequiredEventPlacement,
    toggleRequiredPlacementContract,
    normalizePlacement,
    serializePlacement,
    compileMapForRuntime,
    cloneEditorMap,
    buildCompiledMapForRuntime,
    compileProjectForRuntime,
    buildContentValidationReport,
    buildAssetValidationReport,
    buildProjectProgressionValidationReport,
    buildProjectKeyLockValidationReport,
    buildRequiredPlacementContractReport,
    buildRequiredContentReachabilityReport,
    buildProjectValidationReport,
    mergeProjectCompileFailures,
    buildContentReferenceSummary,
    buildContentBuildManifest,
    reachable,
    ensureBoundaryClean,
    validationSummaryText,
    resolvePlacementEvent,
    collectEventEffects,
  };
}
