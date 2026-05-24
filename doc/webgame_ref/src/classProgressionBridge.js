export function createClassProgressionBridge(deps = {}) {
  const {
    classes = [],
    getState = () => ({}),
    normalizeHeroState = (hero) => hero,
  } = deps;

  function classDefinition(hero) {
    return classes[hero.classIndex] || classes[0];
  }

  function nextClassMilestone(hero) {
    const milestones = classDefinition(hero)?.progression?.milestones || [];
    return milestones[hero.trainingLevel || 0] || null;
  }

  function nextClassMilestoneText(hero) {
    const milestone = nextClassMilestone(hero);
    if (!milestone) return "훈련 완료";
    return `다음 ${milestone.label} · XP ${milestone.xpCost}`;
  }

  function syncHeroClassDefinition(hero) {
    const classDef = classes[hero.classIndex] || classes[0];
    hero.cls = classDef.cls;
    hero.skillId = classDef.skillId;
    hero.category = classDef.category;
    hero.prof = hero.prof && typeof hero.prof === "object" ? hero.prof : {};
    if (hero.prof[classDef.category] == null) hero.prof[classDef.category] = 0;
    return hero;
  }

  function syncPartyClassDefinitions() {
    const state = getState();
    state.party = state.party.map((hero, index) => syncHeroClassDefinition(normalizeHeroState(hero, index)));
  }

  function classMilestonesJson(classDef) {
    return JSON.stringify(classDef?.progression?.milestones || [], null, 2);
  }

  function classDefinitionValidationIssues(index, classDef) {
    const issues = [];
    if (!classDef || typeof classDef !== "object") {
      issues.push(`class[${index}] 정의가 object가 아니다.`);
      return issues;
    }
    if (!classDef.cls) issues.push(`class[${index}] cls가 비어 있다.`);
    if (!classDef.category) issues.push(`class[${index}] category가 비어 있다.`);
    for (const stat of ["hp", "atk", "def"]) {
      if (typeof classDef[stat] !== "number") issues.push(`class[${index}] ${stat}는 number여야 한다.`);
    }
    if (!Array.isArray(classDef?.progression?.milestones)) issues.push(`class[${index}] progression.milestones는 array여야 한다.`);
    for (const [milestoneIndex, milestone] of (classDef?.progression?.milestones || []).entries()) {
      if (!milestone?.label) issues.push(`class[${index}] milestone[${milestoneIndex}] label이 비어 있다.`);
      if (typeof milestone?.xpCost !== "number") issues.push(`class[${index}] milestone[${milestoneIndex}] xpCost는 number여야 한다.`);
      for (const stat of ["hpGain", "atkGain", "defGain", "profGain"]) {
        if (milestone?.[stat] != null && typeof milestone[stat] !== "number") {
          issues.push(`class[${index}] milestone[${milestoneIndex}] ${stat}는 number여야 한다.`);
        }
      }
    }
    return issues;
  }

  function validateClassDefinitionsTable(definitions) {
    if (!Array.isArray(definitions)) throw new Error("classDefinitions 검증 실패: array가 아니다.");
    const issues = definitions.flatMap((classDef, index) => classDefinitionValidationIssues(index, classDef));
    if (issues.length) throw new Error(`classDefinitions 검증 실패: ${issues[0]}`);
  }

  return {
    classDefinition,
    nextClassMilestone,
    nextClassMilestoneText,
    syncHeroClassDefinition,
    syncPartyClassDefinitions,
    classMilestonesJson,
    classDefinitionValidationIssues,
    validateClassDefinitionsTable,
  };
}
