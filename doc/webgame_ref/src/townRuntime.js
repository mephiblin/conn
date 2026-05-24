import { createAuthoredRectangularMap } from "./mapGeneration.js";

export const TOWN_FLOOR_ID = 0;
export const TOWN_MAP_ID = "town_serpent_gate";

const TOWN_SIZE = { width: 12, height: 12 };
const TOWN_START = { floor: TOWN_FLOOR_ID, x: 6, y: 10, facing: "north" };

const TOWN_NPC_PATCHES = Object.freeze({
  npc_innkeeper: [
    { type: "trade", label: "여관에 묵는다", vendorId: "vendor_inn", log: "여관에서 피와 모래를 씻어냈다." },
    { type: "talk", label: "마을 소문을 듣는다", note: "피와 모래 냄새가 아직 갑옷에 남아 있다고 여관 주인이 중얼거린다." },
  ],
  npc_trainer: [
    { type: "trade", label: "훈련을 받는다", vendorId: "vendor_trainer", log: "훈련소에서 전투 감각을 갈고닦았다." },
    { type: "talk", label: "전열 훈련을 묻는다", note: "훈련소 교관은 무거운 무기를 들기 전에 발과 호흡을 먼저 맞추라고 말한다." },
  ],
  npc_smith: [
    { type: "trade", label: "무기를 손본다", vendorId: "vendor_smith", log: "청동 날을 새로 세웠다." },
    { type: "talk", label: "사원 청동을 묻는다", note: "대장장이는 사원 아래 청동은 오래 묵을수록 더 잔인하게 울린다고 경고한다." },
  ],
  npc_apothecary: [
    { type: "trade", label: "약재를 산다", vendorId: "vendor_apothecary", log: "약재와 붕대를 챙겼다." },
    { type: "talk", label: "독 대비를 묻는다", note: "약재상은 검은 물 자국을 본 날에는 해독제를 몸에서 떼지 말라고 한다." },
  ],
  npc_bulletin_board: [
    { type: "quest_board", label: "의뢰를 확인한다" },
    { type: "talk", label: "원정 공고를 읽는다", note: "게시판에는 촌장이 남긴 의뢰서, 귀환 밧줄 위치, 최근 실종자 기록이 빼곡히 붙어 있다." },
  ],
  npc_gatekeeper: [
    { type: "quest_gate", label: "문으로 들어간다" },
    { type: "talk", label: "입구 상태를 묻는다", note: "문은 수주한 의뢰 전표의 인장이 있어야 열린다." },
  ],
});

const WALKABLE_KEYS = new Set([
  "5,10", "6,10", "7,10",
  "4,9", "5,9", "6,9", "7,9", "8,9",
  "3,8", "4,8", "5,8", "6,8", "7,8", "8,8", "9,8",
  "2,7", "3,7", "4,7", "5,7", "6,7", "7,7", "8,7", "9,7", "10,7",
  "2,6", "3,6", "4,6", "5,6", "6,6", "7,6", "8,6", "9,6", "10,6",
  "2,5", "3,5", "4,5", "5,5", "6,5", "7,5", "8,5", "9,5", "10,5",
  "2,4", "3,4", "4,4", "5,4", "6,4", "7,4", "8,4", "9,4", "10,4",
  "2,3", "3,3", "4,3", "5,3", "6,3", "7,3", "8,3", "9,3", "10,3",
  "3,2", "4,2", "5,2", "6,2", "7,2", "8,2", "9,2",
  "5,1", "6,1", "7,1",
]);

function buildTownPlacements() {
  return [
    {
      id: "town_innkeeper",
      kind: "npc",
      position: { floor: TOWN_FLOOR_ID, x: 2, y: 3 },
      npcId: "npc_innkeeper",
      refId: "npc_innkeeper",
      note: "붉은 천막 아래에서 여관 주인이 모래 묻은 물통과 침상을 정리한다.",
    },
    {
      id: "town_trainer",
      kind: "npc",
      position: { floor: TOWN_FLOOR_ID, x: 4, y: 2 },
      npcId: "npc_trainer",
      refId: "npc_trainer",
      note: "훈련소 마당에서 나무 검과 방패가 벽에 기대어 있다.",
    },
    {
      id: "town_smith",
      kind: "npc",
      position: { floor: TOWN_FLOOR_ID, x: 9, y: 3 },
      npcId: "npc_smith",
      refId: "npc_smith",
      note: "대장간 화로가 청동빛 불꽃을 튀기며 골목을 달군다.",
    },
    {
      id: "town_apothecary",
      kind: "npc",
      position: { floor: TOWN_FLOOR_ID, x: 10, y: 5 },
      npcId: "npc_apothecary",
      refId: "npc_apothecary",
      note: "약재상 앞 선반에 붕대와 해독제가 가지런히 걸려 있다.",
    },
    {
      id: "town_skill_merchant",
      kind: "npc",
      position: { floor: TOWN_FLOOR_ID, x: 9, y: 7 },
      npcId: "npc_skill_merchant",
      refId: "npc_skill_merchant",
      note: "기술 상인이 가죽 카드집과 청동 주사위를 좌판에 펼쳐 놓았다.",
    },
    {
      id: "town_scholar",
      kind: "npc",
      position: { floor: TOWN_FLOOR_ID, x: 3, y: 6 },
      npcId: "npc_scholar",
      refId: "npc_scholar",
      note: "학자가 사원 입구와 이어지는 검은 자국을 탁본 위에 다시 덧그린다.",
    },
    {
      id: "town_bulletin_board",
      kind: "npc",
      position: { floor: TOWN_FLOOR_ID, x: 6, y: 1 },
      npcId: "npc_bulletin_board",
      refId: "npc_bulletin_board",
      note: "원정 게시판에 사원 입구 지도와 최근 귀환 기록, 실종자 전표가 겹겹이 꽂혀 있다.",
    },
    {
      id: "town_gatekeeper",
      kind: "npc",
      position: { floor: TOWN_FLOOR_ID, x: 8, y: 2 },
      npcId: "npc_gatekeeper",
      refId: "npc_gatekeeper",
      note: "청동 문이 사원 안쪽의 어둠을 막고 있다. 수주한 의뢰 전표를 문틀에 대면 입구가 열린다.",
    },
  ];
}

function buildTownLights() {
  return [
    { id: "town_gate_light", type: "point", x: 6, y: 2, height: 2.2, color: "#e1b26c", intensity: 0.84, range: 5.8 },
    { id: "town_plaza_light", type: "point", x: 6, y: 6, height: 2, color: "#7fd1c3", intensity: 0.52, range: 7.2 },
    { id: "town_inn_light", type: "point", x: 2, y: 3, height: 1.9, color: "#f0b46d", intensity: 0.74, range: 4.8 },
    { id: "town_smith_light", type: "point", x: 9, y: 3, height: 2.1, color: "#d98f5a", intensity: 0.82, range: 5.4 },
    { id: "town_scholar_light", type: "point", x: 3, y: 6, height: 1.9, color: "#8cb8d6", intensity: 0.58, range: 4.8 },
    { id: "town_skill_merchant_light", type: "point", x: 9, y: 7, height: 1.9, color: "#d6c17c", intensity: 0.6, range: 4.8 },
  ];
}

export function createTownMap() {
  return createAuthoredRectangularMap({
    id: TOWN_MAP_ID,
    name: "카라쉬 외곽 정착지",
    theme: "town_outpost",
    floor: TOWN_FLOOR_ID,
    width: TOWN_SIZE.width,
    height: TOWN_SIZE.height,
    start: TOWN_START,
    walkableKeys: WALKABLE_KEYS,
    placements: buildTownPlacements(),
    lights: buildTownLights(),
    generation: {
      seed: "town_authored_v1",
      profileId: "town_authored_rectangular",
      algorithm: "authored_rectangular_town",
      moduleCount: 6,
    },
    tags: ["town", "safe_hub"],
    defaultWalkableTags: ["town", "safe"],
    defaultBlockedTags: ["town_blocked", "structure"],
    defaultFloorTextureId: "floor_sandstone_01",
    defaultCeilingTextureId: "ceiling_stone_01",
    defaultWallTextureId: "wall_buried_temple_01",
  });
}

export function ensureTownNpcServices(npcDefinitions = {}) {
  for (const [npcId, services] of Object.entries(TOWN_NPC_PATCHES)) {
    const npc = npcDefinitions[npcId];
    if (!npc) continue;
    const existing = Array.isArray(npc.services) ? npc.services : [];
    const byKey = new Set(existing.map((service) => `${service.type}:${service.vendorId || service.targetFloor || service.label || ""}`));
    const merged = [...existing];
    for (const service of services) {
      const serviceKey = `${service.type}:${service.vendorId || service.targetFloor || service.label || ""}`;
      if (!byKey.has(serviceKey)) {
        merged.push(JSON.parse(JSON.stringify(service)));
        byKey.add(serviceKey);
      }
    }
    npc.services = merged;
  }
  return npcDefinitions;
}

export function ensureTownFloorMaps(floorMaps = {}) {
  if (floorMaps[TOWN_FLOOR_ID]) return floorMaps;
  return {
    [TOWN_FLOOR_ID]: createTownMap(),
    ...floorMaps,
  };
}

export function activateTownState(state, options = {}) {
  const nextFloorMaps = ensureTownFloorMaps(state.floorMaps || {});
  state.floorMaps = nextFloorMaps;
  state.mode = "town";
  state.map = nextFloorMaps[TOWN_FLOOR_ID];
  state.player = {
    ...state.player,
    floor: TOWN_FLOOR_ID,
    x: options.x ?? state.map.start.x,
    y: options.y ?? state.map.start.y,
    facing: options.facing || state.map.start.facing,
  };
  state.visitedByFloor = state.visitedByFloor && typeof state.visitedByFloor === "object"
    ? state.visitedByFloor
    : {};
  if (!state.visitedByFloor[TOWN_FLOOR_ID]) state.visitedByFloor[TOWN_FLOOR_ID] = new Set();
  state.visited = state.visitedByFloor[TOWN_FLOOR_ID];
  state.visited.add(`${Math.floor(state.player.x + 0.5)},${Math.floor(state.player.y + 0.5)}`);
  state.visitedByFloor[TOWN_FLOOR_ID] = state.visited;
  return state;
}
