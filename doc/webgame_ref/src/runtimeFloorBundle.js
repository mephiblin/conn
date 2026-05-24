export function createInitialRuntimeFloorBundle({
  presetCatalog,
  createRuntimeFloorMaps,
  compileProjectForRuntime,
  buildRuntimeSessionFloorMaps,
}) {
  const authoredFloorMaps = createRuntimeFloorMaps(presetCatalog);
  const compiledProject = compileProjectForRuntime(authoredFloorMaps);
  if (compiledProject.ok) {
    return {
      floorMaps: buildRuntimeSessionFloorMaps(compiledProject.compiledMaps),
      source: "compiled_authored_floor_bundle",
      compileFailures: [],
    };
  }
  return {
    floorMaps: authoredFloorMaps,
    source: "generated_runtime_fallback",
    compileFailures: JSON.parse(JSON.stringify(compiledProject.failures || [])),
  };
}
