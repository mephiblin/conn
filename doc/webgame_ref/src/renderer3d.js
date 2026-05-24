import * as THREE_MODULE from "https://cdn.jsdelivr.net/npm/three@0.179.1/build/three.module.js";

const GENERATED_NORMAL_TEXTURES = new Map();

const DIRS = ["north", "east", "south", "west"];
const DIR_VECTORS = {
  north: { x: 0, y: -1 },
  east: { x: 1, y: 0 },
  south: { x: 0, y: 1 },
  west: { x: -1, y: 0 },
};

const FACING_YAW = {
  north: 0,
  east: -Math.PI / 2,
  south: Math.PI,
  west: Math.PI / 2,
};

const DEFAULT_OPTIONS = {
  cellSize: 4,
  wallHeight: 3.2,
  wallThickness: 0.25,
  floorThickness: 0.14,
  cameraHeight: 1.52,
  cameraPitch: -0.02,
  fov: 64,
  near: 0.05,
  far: 250,
  enableCeiling: true,
  showPlacementMarkers: true,
  pixelRatioScale: 1,
  lodProfile: "default",
  backgroundColor: 0x0d0b09,
  fogColor: 0x15110d,
  fogNear: 10,
  fogFar: 72,
  cameraSmoothing: 0.1,
};

const LOD_PROFILES = {
  low: { ceiling: false, placementMarkers: true, placementLights: false, heroMaterialDetail: false },
  default: { ceiling: true, placementMarkers: true, placementLights: true, heroMaterialDetail: false },
  high: { ceiling: true, placementMarkers: true, placementLights: true, heroMaterialDetail: true },
};

const PLACEMENT_STYLE = {
  stairs: { color: 0x6384aa, shape: "stairs" },
  entry_marker: { color: 0x4f6f8e, shape: "glyph" },
  encounter: { color: 0xa53f33, shape: "pillar" },
  monster: { color: 0xa53f33, shape: "pillar" },
  trap: { color: 0xb8892b, shape: "spike" },
  item: { color: 0x6b9a67, shape: "pillar" },
  event: { color: 0x7e6398, shape: "pillar" },
  event_trigger: { color: 0x7e6398, shape: "glyph" },
  shrine: { color: 0x73bdb1, shape: "glyph" },
  rest_site: { color: 0x9a8bd2, shape: "glyph" },
  camp: { color: 0xc98f56, shape: "camp" },
  npc: { color: 0x5f8aa6, shape: "npc" },
};

const SURFACE_MATERIALS = {
  floor_sandstone_01: { color: 0x7b654a, roughness: 0.94, metalness: 0.02 },
  floor_obsidian_01: { color: 0x45414a, roughness: 0.72, metalness: 0.12 },
  floor_moss_01: { color: 0x596a4d, roughness: 0.98, metalness: 0.01 },
  floor_bloodstone_01: { color: 0x7b4d43, roughness: 0.88, metalness: 0.04 },
  ceiling_stone_01: { color: 0x42382d, roughness: 0.94, metalness: 0.02 },
  ceiling_vault_01: { color: 0x5d4f45, roughness: 0.9, metalness: 0.03 },
  ceiling_soot_01: { color: 0x332f2b, roughness: 0.98, metalness: 0.01 },
  ceiling_gold_01: { color: 0x75613b, roughness: 0.72, metalness: 0.12 },
  wall_buried_temple_01: { color: 0x5a4937, roughness: 0.94, metalness: 0.04 },
  wall_black_brick_01: { color: 0x3b373d, roughness: 0.84, metalness: 0.08 },
  wall_mossy_01: { color: 0x516447, roughness: 0.97, metalness: 0.02 },
  wall_sacred_relief_01: { color: 0x756147, roughness: 0.78, metalness: 0.08 },
  door_bronze_01: { color: 0xb58b4f, roughness: 0.62, metalness: 0.2 },
};

export function hasThree() {
  return !!THREE_MODULE;
}

export function createDungeonRenderer3D(host, options = {}) {
  return new DungeonRenderer3D(host, options);
}

export class DungeonRenderer3D {
  constructor(host, options = {}) {
    this.host = resolveHost(host);
    this.options = { ...DEFAULT_OPTIONS, ...options };
    this.THREE = null;
    this.renderer = null;
    this.scene = null;
    this.camera = null;
    this.cameraTorch = null;
    this.root = null;
    this.markerGroup = null;
    this.frame = 0;
    this.animationTime = 0;
    this.currentMap = null;
    this.sceneSignature = "";
    this.currentPlayer = null;
    this.currentCameraLook = { yaw: 0, pitch: 0 };
    this.currentCameraPose = null;
    this.targetCameraPose = null;
    this.worldOffset = { x: 0, z: 0 };
    this.resizeObserver = null;
    this.windowResizeHandler = null;
    this.overlay = null;
    this.initialized = false;
  }

  init() {
    if (this.initialized) return true;
    if (!hasThree()) {
      this.mountFallback("Three.js module을 불러오지 못했다.");
      return false;
    }

    this.THREE = THREE_MODULE;
    this.clearHost();
    this.buildRenderer();
    this.buildScene();
    this.bindResize();
    this.resize();
    this.animate();
    this.initialized = true;
    return true;
  }

  rebuildScene(map, overrides = {}) {
    this.options = { ...this.options, ...overrides };
    const previousMapId = this.currentMap?.id || "";
    const nextMapId = map?.id || "";
    const nextSceneSignature = buildSceneSignature(map, this.options);
    if (previousMapId && previousMapId !== nextMapId) {
      this.currentCameraPose = null;
      this.targetCameraPose = null;
    }
    this.currentMap = map || null;
    this.sceneSignature = nextSceneSignature;
    if (!this.init()) return false;

    this.disposeRoot();
    if (!map) {
      this.renderFrame();
      return true;
    }

    this.root = new this.THREE.Group();
    this.root.name = `dungeon:${map.id || "map"}`;
    this.scene.add(this.root);
    this.worldOffset = computeWorldOffset(map, this.options.cellSize);
    this.buildMapGeometry(map);
    if (this.currentPlayer) this.updatePlayerPose(this.currentPlayer, this.currentCameraLook);
    this.renderFrame();
    return true;
  }

  rebuild(map, overrides = {}) {
    return this.rebuildScene(map, overrides);
  }

  updatePlayerPose(player, cameraLook = this.currentCameraLook) {
    this.currentPlayer = player || null;
    this.currentCameraLook = {
      yaw: cameraLook?.yaw || 0,
      pitch: cameraLook?.pitch || 0,
    };
    if (!this.init() || !player) return false;

    const pose = this.computeCameraPose(player, this.currentCameraLook);
    this.targetCameraPose = pose;
    if (!this.currentCameraPose) {
      this.currentCameraPose = { ...pose };
      this.applyCameraPose(this.currentCameraPose);
    }
    this.renderFrame();
    return true;
  }

  updatePlayer(player, cameraLook = this.currentCameraLook) {
    return this.updatePlayerPose(player, cameraLook);
  }

  sync(map, player, overrides = {}) {
    const nextOptions = { ...this.options, ...overrides };
    const nextSceneSignature = buildSceneSignature(map, nextOptions);
    if (!this.currentMap || this.currentMap !== map || this.sceneSignature !== nextSceneSignature) {
      if (!this.rebuildScene(map, overrides)) return false;
    } else {
      this.options = nextOptions;
    }
    return this.updatePlayerPose(player, overrides.cameraLook || this.currentCameraLook);
  }

  resize() {
    if (!this.renderer || !this.camera) return false;
    const width = Math.max(1, this.host.clientWidth || this.renderer.domElement.clientWidth || 900);
    const height = Math.max(1, this.host.clientHeight || this.renderer.domElement.clientHeight || 540);
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2) * this.options.pixelRatioScale);
    this.renderer.setSize(width, height, false);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    this.renderFrame();
    return true;
  }

  gridToWorld(x, y) {
    return {
      x: this.worldOffset.x + (x + 0.5) * this.options.cellSize,
      z: this.worldOffset.z + (y + 0.5) * this.options.cellSize,
    };
  }

  computeCameraPose(player, cameraLook = { yaw: 0, pitch: 0 }) {
    const world = this.gridToWorld(player.x, player.y);
    const yaw = (FACING_YAW[player.facing] ?? 0) + (cameraLook.yaw || 0);
    const pitch = this.options.cameraPitch + (cameraLook.pitch || 0);

    return {
      x: world.x,
      z: world.z,
      yaw,
      pitch,
    };
  }

  dispose() {
    if (this.resizeObserver) this.resizeObserver.disconnect();
    this.resizeObserver = null;
    if (this.windowResizeHandler) window.removeEventListener("resize", this.windowResizeHandler);
    this.windowResizeHandler = null;
    if (this.frame) window.cancelAnimationFrame(this.frame);
    this.frame = 0;

    this.disposeRoot();

    if (this.cameraTorch) {
      this.cameraTorch.geometry.dispose();
      this.cameraTorch.material.dispose();
      this.cameraTorch = null;
    }

    if (this.renderer) {
      this.renderer.dispose();
      if (this.renderer.domElement.parentNode === this.host && !(this.host instanceof HTMLCanvasElement)) {
        this.host.removeChild(this.renderer.domElement);
      }
    }

    if (this.overlay?.parentNode === this.host) this.host.removeChild(this.overlay);

    this.renderer = null;
    this.scene = null;
    this.camera = null;
    this.root = null;
    this.markerGroup = null;
    this.overlay = null;
    this.initialized = false;
  }

  buildRenderer() {
    const useCanvas = this.host instanceof HTMLCanvasElement;
    this.renderer = new this.THREE.WebGLRenderer({
      antialias: true,
      alpha: false,
      canvas: useCanvas ? this.host : undefined,
    });
    this.renderer.outputColorSpace = this.THREE.SRGBColorSpace;
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = this.THREE.PCFSoftShadowMap;
    this.renderer.domElement.style.display = "block";
    this.renderer.domElement.style.width = "100%";
    this.renderer.domElement.style.height = "100%";
    if (!useCanvas) this.host.appendChild(this.renderer.domElement);
  }

  buildScene() {
    this.scene = new this.THREE.Scene();
    this.scene.background = new this.THREE.Color(this.options.backgroundColor);
    this.scene.fog = new this.THREE.Fog(this.options.fogColor, this.options.fogNear, this.options.fogFar);

    this.camera = new this.THREE.PerspectiveCamera(
      this.options.fov,
      1,
      this.options.near,
      this.options.far
    );
    this.camera.rotation.order = "YXZ";

    const sky = new this.THREE.HemisphereLight(0xd4c8b2, 0x140f0c, 0.95);
    this.scene.add(sky);

    const fill = new this.THREE.DirectionalLight(0xf3d3aa, 0.52);
    fill.position.set(9, 16, 5);
    fill.castShadow = true;
    fill.shadow.mapSize.set(1024, 1024);
    fill.shadow.camera.near = 0.5;
    fill.shadow.camera.far = 60;
    this.scene.add(fill);

    const torchLight = new this.THREE.PointLight(0xf5c17f, 1.7, 22, 2);
    torchLight.position.set(0, 0.15, 0);
    this.camera.add(torchLight);

    this.cameraTorch = new this.THREE.Mesh(
      new this.THREE.SphereGeometry(0.03, 6, 6),
      new this.THREE.MeshBasicMaterial({ color: 0xf5c17f })
    );
    this.cameraTorch.visible = false;
    this.camera.add(this.cameraTorch);
    this.scene.add(this.camera);
  }

  bindResize() {
    if (typeof ResizeObserver !== "undefined") {
      this.resizeObserver = new ResizeObserver(() => this.resize());
      this.resizeObserver.observe(this.host);
      return;
    }

    this.windowResizeHandler = () => this.resize();
    window.addEventListener("resize", this.windowResizeHandler);
  }

  animate() {
    if (!this.renderer || !this.scene || !this.camera) return;
    this.frame = window.requestAnimationFrame((time) => {
      this.animationTime = time * 0.001;
      this.stepCameraPose();
      this.animateMarkers();
      this.animate();
    });
    this.renderer.render(this.scene, this.camera);
  }

  renderFrame() {
    if (!this.renderer || !this.scene || !this.camera) return;
    this.animateMarkers();
    this.renderer.render(this.scene, this.camera);
  }

  applyCameraPose(pose) {
    if (!this.camera || !pose) return false;
    this.camera.position.set(pose.x, this.options.cameraHeight, pose.z);
    this.camera.rotation.set(pose.pitch, pose.yaw, 0);
    return true;
  }

  stepCameraPose() {
    if (!this.targetCameraPose || !this.camera) return false;
    if (!this.currentCameraPose) this.currentCameraPose = { ...this.targetCameraPose };
    const amount = clamp01(this.options.cameraSmoothing);
    this.currentCameraPose.x = lerp(this.currentCameraPose.x, this.targetCameraPose.x, amount);
    this.currentCameraPose.z = lerp(this.currentCameraPose.z, this.targetCameraPose.z, amount);
    this.currentCameraPose.pitch = this.targetCameraPose.pitch;
    this.currentCameraPose.yaw = this.targetCameraPose.yaw;
    if (Math.abs(this.currentCameraPose.x - this.targetCameraPose.x) < 0.002) this.currentCameraPose.x = this.targetCameraPose.x;
    if (Math.abs(this.currentCameraPose.z - this.targetCameraPose.z) < 0.002) this.currentCameraPose.z = this.targetCameraPose.z;
    return this.applyCameraPose(this.currentCameraPose);
  }

  buildMapGeometry(map) {
    const walkableCells = map.cells.filter((cell) => cell.walkable);
    const seenEdges = new Set();

    const floorGeometry = new this.THREE.BoxGeometry(
      this.options.cellSize,
      this.options.floorThickness,
      this.options.cellSize
    );

    for (const cell of walkableCells) {
      const center = this.gridToWorld(cell.x, cell.y);

      const floor = new this.THREE.Mesh(
        floorGeometry,
        surfaceMaterial(
          this.THREE,
          this.options.materialManifest,
          this.options.lodProfile,
          cell.floorMaterialId || cell.floorTexture,
          "floor_sandstone_01"
        )
      );
      if (cell.tileRole === "junction" || cell.tileRole === "intersection") floor.scale.set(0.98, 1, 0.98);
      else if (cell.tileRole === "end_cap") floor.scale.set(0.94, 1, 0.94);
      floor.position.set(center.x, -this.options.floorThickness / 2, center.z);
      floor.receiveShadow = true;
      floor.userData = { kind: "floor", cell };
      this.root.add(floor);

      if (this.options.enableCeiling && allowsCeiling(this.options.lodProfile)) {
        const ceiling = new this.THREE.Mesh(
          floorGeometry,
          surfaceMaterial(
            this.THREE,
            this.options.materialManifest,
            this.options.lodProfile,
            cell.ceilingMaterialId || cell.ceilingTexture,
            "ceiling_stone_01"
          )
        );
        ceiling.position.set(center.x, this.options.wallHeight + this.options.floorThickness / 2, center.z);
        ceiling.receiveShadow = true;
        ceiling.userData = { kind: "ceiling", cell };
        this.root.add(ceiling);
      }

      for (const dir of DIRS) {
        const wall = cell.walls?.[dir];
        if (!wall) continue;
        const edgeKey = canonicalEdgeKey(cell.x, cell.y, dir);
        if (seenEdges.has(edgeKey)) continue;
        seenEdges.add(edgeKey);
        this.root.add(this.createWallMesh(cell, dir, wall));
      }
    }

    if (this.options.showPlacementMarkers && allowsPlacementMarkers(this.options.lodProfile)) {
      this.markerGroup = this.buildPlacementMarkers(map);
      this.root.add(this.markerGroup);
    }
    const decorGroup = this.buildDecorMarkers(map);
    if (decorGroup) this.root.add(decorGroup);
    const authoredLights = this.buildAuthoredLights(map);
    if (authoredLights) this.root.add(authoredLights);
  }

  createWallMesh(cell, dir, wall) {
    const isDoor = wall.type === "door" || wall.type === "secret";
    const isOpen = isDoor && wall.blocksMovement === false && wall.blocksSight === false;
    const longSide = this.options.cellSize + this.options.wallThickness;
    const geometry = dir === "north" || dir === "south"
      ? new this.THREE.BoxGeometry(longSide, this.options.wallHeight, this.options.wallThickness)
      : new this.THREE.BoxGeometry(this.options.wallThickness, this.options.wallHeight, longSide);
    const materialDef = resolveMaterialDef(this.options.materialManifest, wall.materialId || wall.texture, wall.texture, this.options.lodProfile);
    const material = new this.THREE.MeshStandardMaterial({
      color: colorForWall(wall, this.options.materialManifest, this.options.lodProfile),
      roughness: materialDef.roughness ?? (isDoor ? 0.62 : 0.94),
      metalness: materialDef.metalness ?? (isDoor ? 0.2 : 0.04),
      normalMap: resolveNormalMap(this.THREE, materialDef),
      emissive: resolveEmissiveColor(materialDef, wall),
      emissiveIntensity: resolveEmissiveIntensity(materialDef, wall),
    });

    const mesh = new this.THREE.Mesh(geometry, material);
    const position = edgeWorldPosition(cell.x, cell.y, dir, this.options.cellSize, this.worldOffset);
    mesh.position.set(position.x, this.options.wallHeight / 2, position.z);
    mesh.castShadow = true;
    mesh.receiveShadow = true;

    if (isOpen) {
      if (dir === "north" || dir === "south") {
        mesh.scale.x = 0.12;
        mesh.position.x += dir === "north" ? -this.options.cellSize * 0.42 : this.options.cellSize * 0.42;
      } else {
        mesh.scale.z = 0.12;
        mesh.position.z += dir === "west" ? -this.options.cellSize * 0.42 : this.options.cellSize * 0.42;
      }
    }

    if (wall.variant === "pillar") {
      if (dir === "north" || dir === "south") mesh.scale.x = 0.42;
      else mesh.scale.z = 0.42;
    } else if (wall.variant === "corner") {
      if (dir === "north" || dir === "south") mesh.scale.x = 0.7;
      else mesh.scale.z = 0.7;
    } else if (wall.variant === "end_cap") {
      if (dir === "north" || dir === "south") mesh.scale.x = 0.55;
      else mesh.scale.z = 0.55;
      material.color.setHex(adjustColor(colorForWall(wall, this.options.materialManifest, this.options.lodProfile), 10));
    } else if (wall.variant === "t_junction") {
      material.color.setHex(adjustColor(colorForWall(wall, this.options.materialManifest, this.options.lodProfile), 6));
    } else if (wall.variant === "doorframe") {
      material.color.setHex(adjustColor(colorForWall(wall, this.options.materialManifest, this.options.lodProfile), 14));
    }

    mesh.userData = { kind: isDoor ? "door" : "wall", cell, dir, wall };
    return mesh;
  }

  buildPlacementMarkers(map) {
    const group = new this.THREE.Group();
    group.name = "placements";

    for (const placement of map.placements || []) {
      if (placement.done) continue;
      const style = PLACEMENT_STYLE[placement.kind];
      if (!style) continue;

      const marker = createPlacementMesh(this.THREE, style);
      const world = this.gridToWorld(placement.position.x, placement.position.y);
      marker.position.set(world.x, style.shape === "stairs" ? 0.32 : style.shape === "npc" ? 0.98 : 0.72, world.z);
      marker.castShadow = true;
      marker.receiveShadow = true;
      marker.userData = {
        placement,
        baseY: marker.position.y,
        phase: (placement.position.x * 0.61 + placement.position.y * 0.43) % (Math.PI * 2),
      };
      group.add(marker);
      const light = allowsPlacementLights(this.options.lodProfile)
        ? createPlacementLight(this.THREE, placement, style)
        : null;
      if (light) {
        light.position.set(world.x, style.shape === "npc" ? 1.65 : 1.1, world.z);
        group.add(light);
      }
    }

    return group;
  }

  buildDecorMarkers(map) {
    const decor = Array.isArray(map.decor) ? map.decor : [];
    if (!decor.length) return null;
    const group = new this.THREE.Group();
    group.name = "decor";
    for (const entry of decor) {
      const center = this.gridToWorld(entry.x, entry.y);
      let geometry = null;
      const color = normalizeColorValue(entry.color) ?? 0x8a6747;
      if (entry.kind === "torch") geometry = new this.THREE.CylinderGeometry(0.06, 0.08, 0.9, 6);
      else if (entry.kind === "barrel") geometry = new this.THREE.CylinderGeometry(0.22, 0.24, 0.5, 10);
      else if (entry.kind === "crate") geometry = new this.THREE.BoxGeometry(0.42, 0.42, 0.42);
      else if (entry.kind === "bones") geometry = new this.THREE.SphereGeometry(0.16, 8, 8);
      else if (entry.kind === "altar") geometry = new this.THREE.BoxGeometry(0.6, 0.35, 0.6);
      if (!geometry) continue;
      const mesh = new this.THREE.Mesh(
        geometry,
        new this.THREE.MeshStandardMaterial({
          color,
          roughness: 0.82,
          metalness: 0.04,
          emissive: entry.kind === "torch" ? color : 0x000000,
          emissiveIntensity: entry.kind === "torch" ? 0.18 : 0,
        })
      );
      mesh.position.set(center.x, entry.kind === "torch" ? 0.45 : 0.2, center.z);
      mesh.castShadow = true;
      mesh.receiveShadow = true;
      mesh.userData = { kind: "decor", decor: entry };
      group.add(mesh);
    }
    return group;
  }

  buildAuthoredLights(map) {
    const lights = Array.isArray(map.lights) ? map.lights : [];
    if (!lights.length) return null;
    const group = new this.THREE.Group();
    group.name = "authored-lights";
    for (const light of lights) {
      if (light.type !== "point") continue;
      const point = new this.THREE.PointLight(
        normalizeColorValue(light.color) ?? 0xf0b46d,
        light.intensity ?? 0.72,
        light.range ?? 8,
        2
      );
      const world = this.gridToWorld(light.x, light.y);
      point.position.set(world.x, light.height ?? 1.8, world.z);
      point.userData = { kind: "authored-light", id: light.id };
      group.add(point);
    }
    return group.children.length ? group : null;
  }

  animateMarkers() {
    if (!this.markerGroup) return;
    for (const child of this.markerGroup.children) {
      const baseY = child.userData.baseY ?? child.position.y;
      const phase = child.userData.phase ?? 0;
      child.position.y = baseY + Math.sin(this.animationTime * 1.5 + phase) * 0.08;
      child.rotation.y = this.animationTime * 0.55 + phase;
    }
  }

  disposeRoot() {
    if (!this.root || !this.scene) return;
    this.scene.remove(this.root);
    disposeObject3D(this.root);
    this.root = null;
    this.markerGroup = null;
  }

  clearHost() {
    if (this.host instanceof HTMLCanvasElement) return;
    this.host.innerHTML = "";
  }

  mountFallback(text) {
    this.clearHost();
    if (this.host instanceof HTMLCanvasElement) return;
    const overlay = document.createElement("div");
    overlay.textContent = text;
    overlay.style.display = "grid";
    overlay.style.placeItems = "center";
    overlay.style.width = "100%";
    overlay.style.height = "100%";
    overlay.style.minHeight = "240px";
    overlay.style.padding = "16px";
    overlay.style.background = "linear-gradient(180deg, #17130f, #0d0a08)";
    overlay.style.color = "#eee2d0";
    overlay.style.font = "14px/1.5 sans-serif";
    overlay.style.textAlign = "center";
    this.host.appendChild(overlay);
    this.overlay = overlay;
  }
}

function resolveHost(host) {
  if (typeof host === "string") {
    const element = document.querySelector(host);
    if (!element) throw new Error(`renderer3d: host not found for selector "${host}"`);
    return element;
  }
  if (!host || typeof host !== "object" || typeof host.appendChild !== "function") {
    throw new Error("renderer3d: host must be a selector or DOM element");
  }
  return host;
}

function computeWorldOffset(map, cellSize) {
  return {
    x: -(map.size.width * cellSize) / 2,
    z: -(map.size.height * cellSize) / 2,
  };
}

function edgeWorldPosition(x, y, dir, cellSize, offset) {
  const centerX = offset.x + (x + 0.5) * cellSize;
  const centerZ = offset.z + (y + 0.5) * cellSize;
  const half = cellSize / 2;
  if (dir === "north") return { x: centerX, z: centerZ - half };
  if (dir === "south") return { x: centerX, z: centerZ + half };
  if (dir === "east") return { x: centerX + half, z: centerZ };
  return { x: centerX - half, z: centerZ };
}

function canonicalEdgeKey(x, y, dir) {
  const step = DIR_VECTORS[dir];
  const nx = x + step.x;
  const ny = y + step.y;
  return `${Math.min(x, nx)},${Math.min(y, ny)}:${Math.max(x, nx)},${Math.max(y, ny)}`;
}

function oppositeDir(dir) {
  if (dir === "north") return "south";
  if (dir === "south") return "north";
  if (dir === "east") return "west";
  return "east";
}

function wallBlocksView(wall) {
  return !!wall && wall.blocksSight !== false;
}

function normalizeColorValue(value) {
  if (typeof value === "number") return value;
  if (typeof value === "string") return Number.parseInt(value.replace("#", "0x"), 16);
  return null;
}

function resolveMaterialDef(materialManifest, materialId, fallbackId, lodProfile = "default") {
  const manifestDef = materialManifest?.[materialId] || materialManifest?.[fallbackId];
  if (manifestDef) return applyMaterialLod(manifestDef, lodProfile);
  return SURFACE_MATERIALS[materialId] || SURFACE_MATERIALS[fallbackId] || { color: 0x665544, roughness: 0.9, metalness: 0.04 };
}

function surfaceMaterial(THREE, materialManifest, lodProfile, materialId, fallbackId) {
  const def = resolveMaterialDef(materialManifest, materialId, fallbackId, lodProfile);
  return new THREE.MeshStandardMaterial({
    color: normalizeColorValue(def.baseColor) ?? normalizeColorValue(def.fallbackColor) ?? def.color ?? 0x665544,
    roughness: def.roughness ?? 0.9,
    metalness: def.metalness ?? 0.04,
    normalMap: resolveNormalMap(THREE, def),
    emissive: resolveEmissiveColor(def),
    emissiveIntensity: resolveEmissiveIntensity(def),
  });
}

function resolveNormalMap(THREE, materialDef = {}) {
  const normalMapId = materialDef.normalMap;
  if (typeof normalMapId !== "string" || !normalMapId) return null;
  if (normalMapId.startsWith("generated://")) return generatedNormalTexture(THREE, normalMapId);
  return null;
}

function resolveEmissiveColor(materialDef = {}, wall = null) {
  const explicit = normalizeColorValue(materialDef.emissive);
  if (explicit != null) return explicit;
  if (materialDef.lightingHint === "interactive") {
    return normalizeColorValue(materialDef.baseColor) ?? normalizeColorValue(materialDef.fallbackColor) ?? 0x2a2010;
  }
  if (materialDef.lightingHint === "guiding") {
    return normalizeColorValue(materialDef.baseColor) ?? 0x1f2a2a;
  }
  if (wall?.type === "secret") return 0x1a2417;
  return 0x000000;
}

function resolveEmissiveIntensity(materialDef = {}, wall = null) {
  if (typeof materialDef.emissiveIntensity === "number" && materialDef.emissiveIntensity > 0) return materialDef.emissiveIntensity;
  if (materialDef.lightingHint === "interactive") return 0.08;
  if (materialDef.lightingHint === "guiding") return 0.12;
  if (wall?.type === "secret") return 0.25;
  return 0;
}

function colorForWall(wall, materialManifest, lodProfile = "default") {
  const materialDef = resolveMaterialDef(materialManifest, wall?.materialId || wall?.texture, wall?.texture, lodProfile);
  const materialColor = normalizeColorValue(materialDef.baseColor) ?? normalizeColorValue(materialDef.fallbackColor) ?? materialDef.color;
  if (materialColor != null) return materialColor;
  if (wall.type === "secret") return wall.locked ? 0x587153 : 0x6c8965;
  if (wall.type === "door") return wall.locked ? 0x95672b : 0xb58b4f;
  return 0x594937;
}

function adjustColor(hex, amount = 0) {
  const r = Math.max(0, Math.min(255, ((hex >> 16) & 0xff) + amount));
  const g = Math.max(0, Math.min(255, ((hex >> 8) & 0xff) + amount));
  const b = Math.max(0, Math.min(255, (hex & 0xff) + amount));
  return (r << 16) | (g << 8) | b;
}

function createPlacementMesh(THREE, style) {
  if (style.shape === "stairs") {
    return new THREE.Mesh(
      new THREE.CylinderGeometry(0.55, 0.9, 0.52, 6),
      new THREE.MeshStandardMaterial({
        color: style.color,
        roughness: 0.42,
        metalness: 0.12,
        emissive: style.color,
        emissiveIntensity: 0.14,
      })
    );
  }

  if (style.shape === "spike") {
    return new THREE.Mesh(
      new THREE.ConeGeometry(0.4, 1.2, 4),
      new THREE.MeshStandardMaterial({
        color: style.color,
        roughness: 0.56,
        metalness: 0.1,
        emissive: style.color,
        emissiveIntensity: 0.1,
      })
    );
  }

  if (style.shape === "npc") {
    const group = new THREE.Group();
    const robe = new THREE.Mesh(
      new THREE.CylinderGeometry(0.24, 0.4, 1.2, 10),
      new THREE.MeshStandardMaterial({
        color: style.color,
        roughness: 0.7,
        metalness: 0.08,
        emissive: style.color,
        emissiveIntensity: 0.08,
      })
    );
    robe.position.y = 0.62;
    const head = new THREE.Mesh(
      new THREE.SphereGeometry(0.19, 10, 10),
      new THREE.MeshStandardMaterial({
        color: 0xd5c0a0,
        roughness: 0.82,
        metalness: 0.02,
      })
    );
    head.position.y = 1.32;
    const halo = new THREE.Mesh(
      new THREE.TorusGeometry(0.3, 0.035, 8, 20),
      new THREE.MeshStandardMaterial({
        color: 0xd2a44b,
        roughness: 0.42,
        metalness: 0.24,
        emissive: 0xd2a44b,
        emissiveIntensity: 0.18,
      })
    );
    halo.position.y = 1.08;
    halo.rotation.x = Math.PI / 2;
    group.add(robe, head, halo);
    return group;
  }

  if (style.shape === "glyph") {
    return new THREE.Mesh(
      new THREE.OctahedronGeometry(0.36, 0),
      new THREE.MeshStandardMaterial({
        color: style.color,
        roughness: 0.36,
        metalness: 0.2,
        emissive: style.color,
        emissiveIntensity: 0.2,
      })
    );
  }

  if (style.shape === "camp") {
    const group = new THREE.Group();
    const fire = new THREE.Mesh(
      new THREE.OctahedronGeometry(0.28, 0),
      new THREE.MeshStandardMaterial({
        color: 0xf0a35c,
        roughness: 0.22,
        metalness: 0.08,
        emissive: 0xf0a35c,
        emissiveIntensity: 0.42,
      })
    );
    fire.position.y = 0.68;
    const base = new THREE.Mesh(
      new THREE.CylinderGeometry(0.34, 0.42, 0.18, 8),
      new THREE.MeshStandardMaterial({
        color: 0x6d4c2f,
        roughness: 0.86,
        metalness: 0.03,
      })
    );
    base.position.y = 0.12;
    group.add(fire, base);
    return group;
  }

  return new THREE.Mesh(
    new THREE.CylinderGeometry(0.3, 0.36, 1.45, 10),
    new THREE.MeshStandardMaterial({
      color: style.color,
      roughness: 0.58,
      metalness: 0.12,
      emissive: style.color,
      emissiveIntensity: 0.08,
    })
  );
}

function createPlacementLight(THREE, placement, style) {
  if (placement.kind === "shrine") return new THREE.PointLight(0x7fdcd0, 0.9, 10, 2);
  if (placement.kind === "camp" || placement.kind === "rest_site") return new THREE.PointLight(0xf0b46d, 0.78, 9, 2);
  if (placement.kind === "trap") return new THREE.PointLight(0xb8892b, 0.52, 6, 2);
  if (placement.kind === "event_trigger") return new THREE.PointLight(style.color, 0.42, 7, 2);
  if (placement.kind === "npc") return new THREE.PointLight(0xc7d7e4, 0.28, 5, 2);
  if (placement.kind === "stairs") return new THREE.PointLight(style.color, 0.36, 6, 2);
  return null;
}

function generatedNormalTexture(THREE, id) {
  if (GENERATED_NORMAL_TEXTURES.has(id)) return GENERATED_NORMAL_TEXTURES.get(id);
  const key = id.replace("generated://", "");
  const texture = buildGeneratedNormalTexture(THREE, key);
  GENERATED_NORMAL_TEXTURES.set(id, texture);
  return texture;
}

function buildGeneratedNormalTexture(THREE, key) {
  const size = 4;
  const data = new Uint8Array(size * size * 4);
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const index = (y * size + x) * 4;
      const sample = generatedNormalSample(key, x, y, size);
      data[index] = sample[0];
      data[index + 1] = sample[1];
      data[index + 2] = sample[2];
      data[index + 3] = 255;
    }
  }
  const texture = new THREE.DataTexture(data, size, size, THREE.RGBAFormat);
  texture.needsUpdate = true;
  texture.wrapS = THREE.RepeatWrapping;
  texture.wrapT = THREE.RepeatWrapping;
  texture.magFilter = THREE.LinearFilter;
  texture.minFilter = THREE.LinearMipmapLinearFilter;
  return texture;
}

function generatedNormalSample(key, x, y, size) {
  const flat = [128, 128, 255];
  if (key === "obsidian_tiles") return (x + y) % 2 === 0 ? [118, 138, 248] : [138, 118, 248];
  if (key === "hammered_gold") return x % 2 === 0 ? [144, 120, 246] : [112, 136, 246];
  if (key === "sacred_relief") return y < size / 2 ? [132, 112, 246] : [124, 144, 246];
  if (key === "bronze_door") return x === 1 || x === 2 ? [146, 128, 244] : [118, 128, 248];
  return flat;
}

function profileSettings(lodProfile = "default") {
  return LOD_PROFILES[lodProfile] || LOD_PROFILES.default;
}

function allowsCeiling(lodProfile) {
  return profileSettings(lodProfile).ceiling;
}

function allowsPlacementMarkers(lodProfile) {
  return profileSettings(lodProfile).placementMarkers;
}

function allowsPlacementLights(lodProfile) {
  return profileSettings(lodProfile).placementLights;
}

function buildSceneSignature(map, options = {}) {
  if (!map) return "empty";
  const doorSignature = Object.entries(map.doors || {})
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([id, door]) => [
      id,
      door?.type || "",
      door?.open ? 1 : 0,
      door?.locked ? 1 : 0,
      door?.keyId || "",
    ].join(":"))
    .join(",");
  const placementSignature = (map.placements || [])
    .map((placement) => [
      placement.id || "",
      placement.kind || "",
      placement.position?.x ?? "",
      placement.position?.y ?? "",
      placement.done ? 1 : 0,
    ].join(":"))
    .join(",");
  const lightSignature = (map.lights || [])
    .map((light) => [
      light.x,
      light.y,
      light.color || "",
      light.intensity || "",
      light.radius || "",
    ].join(":"))
    .join(",");
  const materialManifest = options.materialManifest || {};
  const materialCount = [
    Object.keys(materialManifest.materials || {}).length,
    Object.keys(materialManifest.battleBackgrounds || {}).length,
  ].join(":");
  return [
    map.id || "",
    map.width || 0,
    map.height || 0,
    map.floor || "",
    map.generation?.seed || "",
    options.lodProfile || "default",
    options.enableCeiling === false ? 0 : 1,
    options.showPlacementMarkers === false ? 0 : 1,
    materialCount,
    doorSignature,
    placementSignature,
    lightSignature,
  ].join("|");
}

function applyMaterialLod(materialDef = {}, lodProfile = "default") {
  const settings = profileSettings(lodProfile);
  if (materialDef.lod === "hero" && !settings.heroMaterialDetail) {
    return {
      ...materialDef,
      metalness: 0.02,
      roughness: Math.max(0.86, materialDef.roughness ?? 0.86),
      emissiveIntensity: Math.min(materialDef.emissiveIntensity ?? 0, 0.06),
    };
  }
  return materialDef;
}

function clamp01(value) {
  return Math.max(0, Math.min(1, Number(value) || 0));
}

function lerp(from, to, amount) {
  return from + (to - from) * amount;
}

function disposeObject3D(root) {
  root.traverse((node) => {
    if (!node.isMesh) return;
    if (node.geometry) node.geometry.dispose();
    if (Array.isArray(node.material)) {
      node.material.forEach((material) => material?.dispose?.());
      return;
    }
    node.material?.dispose?.();
  });
}
