export function createEventRuntimeBridge(deps = {}) {
  const {
    getState = () => ({}),
    ensureEventRuntime = () => {
      throw new Error("eventRuntimeBridge dependency missing: ensureEventRuntime");
    },
    closeInteraction = () => {},
  } = deps;

  function currentEditorEventTestSession() {
    const state = getState();
    return state.editor?.eventTestSession && typeof state.editor.eventTestSession === "object"
      ? state.editor.eventTestSession
      : null;
  }

  function stopEditorEventTestSession() {
    const state = getState();
    state.editor.eventTestSession = null;
    if (state.interaction?.testSessionId) closeInteraction();
  }

  function openEventChoiceInteraction(...args) {
    return ensureEventRuntime().openEventChoiceInteraction(...args);
  }

  function continueEventFlow(...args) {
    return ensureEventRuntime().continueEventFlow(...args);
  }

  function resolveEventChoice(...args) {
    return ensureEventRuntime().resolveEventChoice(...args);
  }

  function startEditorEventTestSession(...args) {
    return ensureEventRuntime().startEditorEventTestSession(...args);
  }

  function applyEventEffects(...args) {
    return ensureEventRuntime().applyEventEffects(...args);
  }

  function runTypedEventEffects(...args) {
    return ensureEventRuntime().runTypedEventEffects(...args);
  }

  function canDetectTrap(...args) {
    return ensureEventRuntime().canDetectTrap(...args);
  }

  function canDisarmTrap(...args) {
    return ensureEventRuntime().canDisarmTrap(...args);
  }

  function ensureEventRuntimeState(...args) {
    return ensureEventRuntime().ensureEventRuntimeState(...args);
  }

  function eventUsageState(...args) {
    return ensureEventRuntime().eventUsageState(...args);
  }

  function spendEventUsage(...args) {
    return ensureEventRuntime().spendEventUsage(...args);
  }

  function advanceWorldTurn(...args) {
    return ensureEventRuntime().advanceWorldTurn(...args);
  }

  return {
    currentEditorEventTestSession,
    stopEditorEventTestSession,
    openEventChoiceInteraction,
    continueEventFlow,
    resolveEventChoice,
    startEditorEventTestSession,
    applyEventEffects,
    runTypedEventEffects,
    canDetectTrap,
    canDisarmTrap,
    ensureEventRuntimeState,
    eventUsageState,
    spendEventUsage,
    advanceWorldTurn,
  };
}
