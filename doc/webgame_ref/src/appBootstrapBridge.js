import { createAppShellBridges } from "./appShellBridges.js";
import { bindGlobalUiControls, bootstrapPlayerRuntimeControls, registerAppDebugHarness } from "./bootstrapUi.js";
import { createProductModeBridge } from "./productModeBridge.js";

export function bootstrapAppRuntime(deps = {}) {
  const {
    shellBridgeDeps = {},
    productModeBridgeDeps = {},
    debugHarnessDeps = {},
    playerRuntimeDeps = {},
  } = deps;

  const shell = createAppShellBridges(shellBridgeDeps);
  const productMode = createProductModeBridge(productModeBridgeDeps);

  registerAppDebugHarness(debugHarnessDeps);

  const playerRuntimeControls = bootstrapPlayerRuntimeControls({
    ...playerRuntimeDeps,
    bindGlobalUiControls,
    setProductEntry: productMode.setProductEntry,
    setMode: productMode.setMode,
    saveGame: shell.saveGame,
    loadGame: shell.loadGame,
  });

  return {
    ...shell,
    ...productMode,
    playerRuntimeControls,
  };
}
