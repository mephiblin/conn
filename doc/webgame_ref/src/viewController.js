import { createDungeonRenderer3D } from "./renderer3d.js";

export function createViewController(host, options = {}) {
  const {
    yawSensitivity = 0.0018,
    pitchSensitivity = 0.0012,
    minLookPitch = -0.55,
    maxLookPitch = 0.42,
    getMode = () => "dungeon",
    interactiveModes = ["dungeon"],
    canCapturePointer = () => true,
    enabled = true,
  } = options;
  const renderer = createDungeonRenderer3D(host);
  const interactiveModeSet = new Set(interactiveModes);
  const inputHandlers = {};
  const pointerLook = {
    enabled,
    active: false,
    pointerId: null,
    lastX: 0,
    lastY: 0,
    lookYaw: 0,
    lookPitch: 0,
    locked: false,
    hovered: false,
  };

  function currentCameraLook() {
    return {
      yaw: pointerLook.lookYaw,
      pitch: pointerLook.lookPitch,
    };
  }

  function applyMouseLookDelta(deltaX, deltaY) {
    pointerLook.lookYaw = normalizeRadians(pointerLook.lookYaw - deltaX * yawSensitivity);
    pointerLook.lookPitch = clamp(pointerLook.lookPitch - deltaY * pitchSensitivity, minLookPitch, maxLookPitch);
    if (renderer.currentPlayer && interactiveModeSet.has(getMode())) renderer.updatePlayerPose(renderer.currentPlayer, currentCameraLook());
  }

  function syncCursor() {
    host.style.cursor = interactiveModeSet.has(getMode()) && pointerLook.enabled && canCapturePointer()
      ? (pointerLook.locked ? "none" : "crosshair")
      : "default";
  }

  function releasePointerLook() {
    if (document.pointerLockElement === host && typeof document.exitPointerLock === "function") {
      document.exitPointerLock();
    }
    pointerLook.active = false;
    pointerLook.pointerId = null;
    pointerLook.lastX = 0;
    pointerLook.lastY = 0;
    pointerLook.locked = false;
    pointerLook.hovered = false;
    syncCursor();
  }

  function setDragLookEnabled(force) {
    const next = typeof force === "boolean" ? force : !pointerLook.enabled;
    if (pointerLook.enabled === next) return next;
    pointerLook.enabled = next;
    if (!next) releasePointerLook();
    syncCursor();
    return next;
  }

  function setupInput() {
    syncCursor();
    if (host.dataset.inputReady === "true") return;
    host.dataset.inputReady = "true";
    host.tabIndex = 0;
    host.setAttribute("role", "application");
    inputHandlers.contextmenu = (event) => event.preventDefault();
    inputHandlers.click = (event) => {
      if (!interactiveModeSet.has(getMode()) || !pointerLook.enabled || !canCapturePointer() || event.button !== 0) return;
      host.focus();
      if (document.pointerLockElement !== host && typeof host.requestPointerLock === "function") {
        host.requestPointerLock();
      }
      syncCursor();
      event.preventDefault();
    };
    inputHandlers.mouseenter = () => {
      pointerLook.hovered = true;
      syncCursor();
    };
    inputHandlers.mouseleave = () => {
      pointerLook.hovered = false;
      syncCursor();
    };
    inputHandlers.hostMousemove = (event) => {
      if (document.pointerLockElement === host || !interactiveModeSet.has(getMode()) || !pointerLook.enabled || !canCapturePointer() || !pointerLook.hovered) return;
      applyMouseLookDelta(event.movementX, event.movementY);
      event.preventDefault();
    };
    inputHandlers.pointerlockchange = () => {
      pointerLook.locked = document.pointerLockElement === host;
      pointerLook.active = pointerLook.locked;
      syncCursor();
    };
    inputHandlers.documentMousemove = (event) => {
      if (document.pointerLockElement !== host || !interactiveModeSet.has(getMode()) || !pointerLook.enabled || !canCapturePointer()) return;
      applyMouseLookDelta(event.movementX, event.movementY);
      event.preventDefault();
    };
    host.addEventListener("contextmenu", inputHandlers.contextmenu);
    host.addEventListener("click", inputHandlers.click);
    host.addEventListener("mouseenter", inputHandlers.mouseenter);
    host.addEventListener("mouseleave", inputHandlers.mouseleave);
    host.addEventListener("mousemove", inputHandlers.hostMousemove);
    document.addEventListener("pointerlockchange", inputHandlers.pointerlockchange);
    document.addEventListener("mousemove", inputHandlers.documentMousemove);
  }

  function teardownInput() {
    if (host.dataset.inputReady !== "true") return;
    host.dataset.inputReady = "false";
    host.removeEventListener("contextmenu", inputHandlers.contextmenu);
    host.removeEventListener("click", inputHandlers.click);
    host.removeEventListener("mouseenter", inputHandlers.mouseenter);
    host.removeEventListener("mouseleave", inputHandlers.mouseleave);
    host.removeEventListener("mousemove", inputHandlers.hostMousemove);
    document.removeEventListener("pointerlockchange", inputHandlers.pointerlockchange);
    document.removeEventListener("mousemove", inputHandlers.documentMousemove);
  }

  return {
    sync(map, player, overrides = {}) {
      setupInput();
      if (pointerLook.locked && !canCapturePointer()) releasePointerLook();
      renderer.sync(map, player, {
        ...overrides,
        cameraLook: currentCameraLook(),
      });
      syncCursor();
    },
    releasePointerLook,
    setDragLookEnabled,
    getPointerLookState() {
      return { ...pointerLook };
    },
    setPointerLook(nextLook = {}) {
      if (Number.isFinite(nextLook.yaw)) pointerLook.lookYaw = normalizeRadians(nextLook.yaw);
      if (Number.isFinite(nextLook.pitch)) pointerLook.lookPitch = clamp(nextLook.pitch, minLookPitch, maxLookPitch);
      if (renderer.currentPlayer && interactiveModeSet.has(getMode())) renderer.updatePlayerPose(renderer.currentPlayer, currentCameraLook());
      return { ...pointerLook };
    },
    dispose() {
      releasePointerLook();
      teardownInput();
      renderer.dispose?.();
    },
    host,
  };
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function normalizeRadians(value) {
  const circle = Math.PI * 2;
  return ((((value + Math.PI) % circle) + circle) % circle) - Math.PI;
}
