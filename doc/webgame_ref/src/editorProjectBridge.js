export function createEditorProjectBridge(deps = {}) {
  const {
    buildEditorProjectModule = () => null,
    applyEditorProjectModule = () => null,
    saveEditorProjectModule = () => null,
    loadEditorProjectModule = () => null,
    normalizeMapMetadataModule = (map) => map,
    normalizeMapLightModule = (light) => light,
    createEditorProjectDependencies = () => ({}),
    createMapMetadataDependencies = () => ({}),
  } = deps;

  return {
    buildEditorProject() {
      return buildEditorProjectModule(createEditorProjectDependencies());
    },
    applyEditorProject(project) {
      return applyEditorProjectModule(project, createEditorProjectDependencies());
    },
    saveEditorProject() {
      return saveEditorProjectModule(createEditorProjectDependencies());
    },
    loadEditorProject() {
      return loadEditorProjectModule(createEditorProjectDependencies());
    },
    normalizeMapMetadata(map) {
      return normalizeMapMetadataModule(map, createMapMetadataDependencies());
    },
    normalizeMapLight(light) {
      return normalizeMapLightModule(light);
    },
  };
}
