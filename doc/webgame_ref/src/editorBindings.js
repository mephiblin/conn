import { bindEditorContentInteractions } from "./editorContentBindingsBridge.js";
import { bindEditorEventInteractions } from "./editorEventBindingsBridge.js";
import { bindEditorNpcInteractions } from "./editorNpcBindingsBridge.js";
import { bindEditorPointerInteraction } from "./editorPointerInteraction.js";

const EDITOR_CONTENT_TABS = new Set(["map", "monster", "skill", "item", "event", "npc"]);

function bindEditorContentTabs(deps = {}) {
  const {
    state,
    documentObject,
    render = () => {},
  } = deps;
  documentObject?.querySelectorAll?.("[data-editor-content-tab]")?.forEach((button) => {
    button.onclick = () => {
      const tab = button.getAttribute("data-editor-content-tab") || "map";
      state.editorContentTab = EDITOR_CONTENT_TABS.has(tab) ? tab : "map";
      render();
    };
  });
}

export function bindEditorWorkspaceInteractions(deps = {}) {
  bindEditorContentTabs(deps);
  if (
    deps?.state?.editorWorkspaceMode === "generator_workbench"
    || deps?.documentObject?.getElementById?.("workbenchProfileSelect")
  ) return;
  bindEditorPointerInteraction(deps);
  bindEditorEventInteractions(deps);
  bindEditorNpcInteractions(deps);
  bindEditorContentInteractions(deps);
}
