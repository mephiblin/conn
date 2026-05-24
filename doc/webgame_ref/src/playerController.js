const DEFAULT_TARGET = typeof window !== "undefined" ? window : null;
const HELD_MOVEMENT_ACTIONS = new Set(["forward", "backward", "strafeLeft", "strafeRight", "forwardLeft", "forwardRight"]);

export const KEY_BINDINGS = Object.freeze({
  ArrowUp: "forward",
  ArrowDown: "backward",
  ArrowLeft: "turnLeft",
  ArrowRight: "turnRight",
  Enter: "interact",
  " ": "interact",
  Spacebar: "interact",
  Escape: "escape",
  w: "forward",
  s: "backward",
  a: "strafeLeft",
  d: "strafeRight",
  f: "interact",
  i: "inventory",
  r: "rest",
  m: "toggleDragLook",
});

export const KEY_CODE_BINDINGS = Object.freeze({
  ArrowUp: "forward",
  ArrowDown: "backward",
  ArrowLeft: "turnLeft",
  ArrowRight: "turnRight",
  Enter: "interact",
  Space: "interact",
  Escape: "escape",
  KeyW: "forward",
  KeyS: "backward",
  KeyA: "strafeLeft",
  KeyD: "strafeRight",
  KeyF: "interact",
  KeyI: "inventory",
  KeyR: "rest",
  KeyM: "toggleDragLook",
});

export function actionFromKeyboardEvent(event) {
  if (!event || event.altKey || event.ctrlKey || event.metaKey) return null;

  const key = typeof event.key === "string" ? event.key : "";
  const code = typeof event.code === "string" ? event.code : "";
  return KEY_CODE_BINDINGS[code]
    || KEY_BINDINGS[key]
    || KEY_BINDINGS[key.toLowerCase()]
    || null;
}

export function createPlayerController({
  target = DEFAULT_TARGET,
  isEditableTarget = () => false,
  onAction = () => {},
  onMoveInput = () => {},
} = {}) {
  const activeMovementKeys = new Map();
  let movementFrame = 0;
  let lastFrameTime = 0;

  function movementIntent() {
    let forward = 0;
    let strafe = 0;
    for (const { action } of activeMovementKeys.values()) {
      if (action === "forward") forward += 1;
      else if (action === "backward") forward -= 1;
      else if (action === "strafeRight") strafe += 1;
      else if (action === "strafeLeft") strafe -= 1;
      else if (action === "forwardRight") {
        forward += 1;
        strafe += 1;
      } else if (action === "forwardLeft") {
        forward += 1;
        strafe -= 1;
      }
    }
    return {
      forward: clampAxis(forward),
      strafe: clampAxis(strafe),
    };
  }

  function stopMovementLoop() {
    if (!movementFrame) return;
    cancelFrame(movementFrame);
    movementFrame = 0;
    lastFrameTime = 0;
  }

  function stepMovementFrame(time) {
    movementFrame = 0;
    const intent = movementIntent();
    if (!intent.forward && !intent.strafe) {
      lastFrameTime = 0;
      onMoveInput({ forward: 0, strafe: 0, dt: 0 });
      return;
    }
    const previous = lastFrameTime || time;
    const dt = Math.max(0.001, Math.min(0.05, (time - previous) / 1000));
    lastFrameTime = time;
    onMoveInput({ ...intent, dt });
    scheduleMovementLoop();
  }

  function scheduleMovementLoop() {
    if (movementFrame) return;
    movementFrame = requestFrame(stepMovementFrame);
  }

  function handleKeydown(event) {
    if (isEditableTarget(event.target)) return;

    const action = actionFromKeyboardEvent(event);
    if (!action) return;

    event.preventDefault();
    if (HELD_MOVEMENT_ACTIONS.has(action)) {
      const keyId = event.code || event.key || action;
      if (activeMovementKeys.has(keyId)) return;
      activeMovementKeys.set(keyId, { action, startedAt: performanceNow() });
      scheduleMovementLoop();
      return;
    }

    onAction(action, event);
  }

  function handleKeyup(event) {
    const action = actionFromKeyboardEvent(event);
    if (!action || !HELD_MOVEMENT_ACTIONS.has(action)) return;

    const keyId = event.code || event.key || action;
    activeMovementKeys.delete(keyId);
    if (!activeMovementKeys.size) {
      stopMovementLoop();
      onMoveInput({ forward: 0, strafe: 0, dt: 0 });
    }
  }

  function handleBlur() {
    activeMovementKeys.clear();
    stopMovementLoop();
    onMoveInput({ forward: 0, strafe: 0, dt: 0 });
  }

  target?.addEventListener?.("keydown", handleKeydown);
  target?.addEventListener?.("keyup", handleKeyup);
  target?.addEventListener?.("blur", handleBlur);

  return {
    destroy() {
      target?.removeEventListener?.("keydown", handleKeydown);
      target?.removeEventListener?.("keyup", handleKeyup);
      target?.removeEventListener?.("blur", handleBlur);
      handleBlur();
    },
  };
}

function performanceNow() {
  return typeof performance !== "undefined" && typeof performance.now === "function"
    ? performance.now()
    : Date.now();
}

function requestFrame(callback) {
  if (typeof window !== "undefined" && typeof window.requestAnimationFrame === "function") {
    return window.requestAnimationFrame(callback);
  }
  return setTimeout(() => callback(performanceNow()), 16);
}

function cancelFrame(handle) {
  if (typeof window !== "undefined" && typeof window.cancelAnimationFrame === "function" && typeof handle === "number") {
    window.cancelAnimationFrame(handle);
    return;
  }
  clearTimeout(handle);
}

function clampAxis(value) {
  if (value > 0) return 1;
  if (value < 0) return -1;
  return 0;
}
