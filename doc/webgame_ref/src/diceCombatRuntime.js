const DEFAULT_DICE_COUNT = 5;
const DEFAULT_SELECT_LIMIT = 3;
const EMPTY_FACE_FALLBACK_SKILL_ID = "fallback_basic_attack";
const DEFAULT_FACE_TEMPLATES = [
  ["attack_basic", "attack_basic", "attack_power", "guard_wall", "signature", "attack_power"],
  ["attack_basic", "field_patch", "attack_basic", "signature", "guard_wall", "attack_power"],
  ["guard_wall", "guard_wall", "attack_basic", "field_patch", "signature", "fortify"],
  ["attack_basic", "attack_power", "signature", "attack_power", "attack_basic", "attack_power"],
  ["field_patch", "attack_basic", "guard_wall", "signature", "attack_power", "signature"],
];
const DEFAULT_SKILLS = {
  attack_basic: {
    name: "베기",
    kind: "attack",
    targetMode: "enemy",
    effect: 0,
    formula: "die_as_effect",
    tags: ["attack"],
    priority: 10,
    cooldown: 1,
  },
  [EMPTY_FACE_FALLBACK_SKILL_ID]: {
    name: "기본공격",
    kind: "attack",
    targetMode: "enemy",
    effect: 0,
    formula: "die_as_effect",
    tags: ["attack", "fallback"],
    priority: 6,
    cooldown: 2,
    description: "장착되지 않은 면은 주사위 눈만큼 피해를 준다.",
  },
  attack_power: {
    name: "강타",
    kind: "attack",
    targetMode: "enemy",
    effect: 2,
    formula: "die_plus_effect",
    tags: ["attack"],
    priority: 16,
    cooldown: 2,
  },
  guard_wall: {
    name: "방어 태세",
    kind: "defend",
    targetMode: "self",
    effect: 1,
    formula: "die_plus_effect",
    tags: ["guard"],
    priority: 9,
    cooldown: 2,
  },
  fortify: {
    name: "철벽",
    kind: "defend",
    targetMode: "self",
    effect: 1,
    formula: "die_times_effect",
    tags: ["guard"],
    priority: 13,
    cooldown: 4,
  },
  field_patch: {
    name: "응급처치",
    kind: "heal",
    targetMode: "ally",
    effect: 1,
    formula: "die_plus_effect",
    tags: ["heal"],
    priority: 8,
    cooldown: 3,
  },
};

function defaultSkillDescription(definition = {}) {
  const effect = Math.max(0, Number(definition.effect || 0));
  const formula = definition.formula || "die_as_effect";
  const kind = definition.kind || "attack";
  const targetMode = definition.targetMode || "enemy";
  let suffix = "효과를 준다.";
  if (kind === "attack" || kind === "skill") suffix = "피해를 준다.";
  else if (kind === "guard" || kind === "defend") suffix = "방어를 올린다.";
  else if (kind === "buff") suffix = "강화를 얻는다.";
  else if (kind === "debuff") suffix = "약화를 부여한다.";
  else if (kind === "lifesteal") suffix = "피해를 주고 흡수한다.";
  else if (kind === "summon") suffix = "소환 준비를 한다.";
  else if (kind === "heal" || targetMode === "ally") suffix = "회복한다.";
  else if (kind === "support" && targetMode === "enemy") suffix = "약점 노출을 부여한다.";
  if (formula === "die_times_effect") return `주사위 눈 x ${effect} ${suffix}`;
  if (formula === "die_plus_effect") return `주사위 눈 + ${effect} ${suffix}`;
  if (formula === "die_minus_effect") return `주사위 눈 - ${effect} ${suffix}`;
  if (formula === "die_divide_effect") return `주사위 눈 / ${Math.max(1, effect)} ${suffix}`;
  if (formula === "die_equals_effect") return `고정 ${effect} ${suffix}`;
  return `주사위 눈만큼 ${suffix}`;
}

function clampPositiveInteger(value, fallback) {
  const number = Number(value);
  return Number.isInteger(number) && number > 0 ? number : fallback;
}

function cooldownKeyForFace(dieId = "", faceIndex = -1) {
  if (!dieId || !Number.isInteger(Number(faceIndex)) || Number(faceIndex) < 0) return "";
  return `${dieId}::${Number(faceIndex)}`;
}

export function createDiceCombatRuntime(deps = {}) {
  const rand = deps.rand || ((min, max) => min + Math.floor(Math.random() * (max - min + 1)));
  const skillName = deps.skillName || ((skillId) => skillId || "기술");
  const skills = deps.skills || {};

  function resolveEffectiveSkillId(hero, skillId) {
    const normalizedSkillId = typeof skillId === "string" ? skillId.trim() : "";
    if (!normalizedSkillId) return EMPTY_FACE_FALLBACK_SKILL_ID;
    if (normalizedSkillId === "signature") return hero?.skillId || "signature";
    return normalizedSkillId;
  }

  function skillDefinition(hero, skillId) {
    const effectiveSkillId = resolveEffectiveSkillId(hero, skillId);
    const base = skills[effectiveSkillId] || DEFAULT_SKILLS[effectiveSkillId] || {};
    const definition = {
      id: effectiveSkillId || hero?.skillId || "skill_unknown",
      name: base.name || skillName(effectiveSkillId || hero?.skillId),
      kind: base.kind || (effectiveSkillId === hero?.skillId ? "skill" : "attack"),
      targetMode: base.targetMode || "enemy",
      effect: Number(base.effect || 0),
      formula: base.formula || "die_as_effect",
      tags: Array.isArray(base.tags) ? [...base.tags] : [],
      description: base.description || "",
      status: base.status || "",
      duration: Math.max(0, Number(base.duration || 0)),
      deferred: Boolean(base.deferred),
      priority: Number(base.priority || 0),
      cooldown: Math.min(6, Math.max(1, Number(base.cooldown || 1))),
      isSignature: Boolean(effectiveSkillId) && effectiveSkillId === hero?.skillId,
    };
    if (!definition.description) definition.description = defaultSkillDescription(definition);
    return definition;
  }

  function evaluateFormula(formula, dieValue, effect) {
    if (formula === "die_times_effect") return dieValue * effect;
    if (formula === "die_plus_effect") return dieValue + effect;
    if (formula === "die_minus_effect") return dieValue - effect;
    if (formula === "die_divide_effect") return Math.floor(dieValue / Math.max(1, effect));
    if (formula === "die_equals_effect") return effect;
    return dieValue;
  }

  function normalizeHeroLoadout(hero = {}) {
    const source = hero.diceLoadout || {};
    const dice = Array.isArray(source.dice) ? source.dice : [];
    const diceCount = clampPositiveInteger(source.diceCount, Math.max(DEFAULT_DICE_COUNT, dice.length || 0));
    const selectLimit = Math.min(clampPositiveInteger(source.selectLimit, DEFAULT_SELECT_LIMIT), diceCount);
    const normalizedDice = Array.from({ length: diceCount }, (_, dieIndex) => {
      const die = dice[dieIndex] || {};
      const faces = Array.isArray(die.faces) ? die.faces : [];
      const template = DEFAULT_FACE_TEMPLATES[dieIndex] || DEFAULT_FACE_TEMPLATES[DEFAULT_FACE_TEMPLATES.length - 1];
      return {
        id: die.id || `${hero.id || "hero"}_die_${dieIndex + 1}`,
        faces: Array.from({ length: 6 }, (_, faceIndex) => {
          const templateSkillId = template[faceIndex] === "signature" ? (hero.skillId || hero.knownSkillIds?.[0] || "attack_basic") : template[faceIndex];
          const fallbackSkillId = hero.skillId || hero.knownSkillIds?.[0] || templateSkillId;
          return {
            value: clampPositiveInteger(faces[faceIndex]?.value, faceIndex + 1),
            skillId: typeof faces[faceIndex]?.skillId === "string"
              ? faces[faceIndex].skillId
              : (templateSkillId || fallbackSkillId || ""),
          };
        }),
      };
    });
    const normalized = { diceCount, selectLimit, dice: normalizedDice };
    hero.diceLoadout = normalized;
    return normalized;
  }

  function createRoll(hero, die, dieIndex) {
    const faceIndex = rand(0, Math.max(0, die.faces.length - 1));
    const face = die.faces[faceIndex];
    const skill = skillDefinition(hero, face.skillId);
    const previewFaces = Array.isArray(die.faces) && die.faces.length
      ? die.faces.map((candidateFace, candidateIndex) => {
          const candidateSkill = skillDefinition(hero, candidateFace?.skillId);
          return {
            value: clampPositiveInteger(candidateFace?.value, candidateIndex + 1),
            skillId: resolveEffectiveSkillId(hero, candidateFace?.skillId),
            skillName: candidateSkill.name,
            kind: candidateSkill.kind,
            targetMode: candidateSkill.targetMode,
          };
        })
      : [{
          value: clampPositiveInteger(face?.value, faceIndex + 1),
          skillId: resolveEffectiveSkillId(hero, face?.skillId),
          skillName: skill.name,
          kind: skill.kind,
          targetMode: skill.targetMode,
        }];
    return {
      id: `${die.id}_roll_${faceIndex + 1}`,
      dieId: die.id,
      dieIndex,
      faceIndex,
      cooldownKey: cooldownKeyForFace(die.id, faceIndex),
      value: clampPositiveInteger(face?.value, faceIndex + 1),
      skillId: resolveEffectiveSkillId(hero, face?.skillId),
      skillName: skill.name,
      kind: skill.kind,
      targetMode: skill.targetMode,
      effect: skill.effect,
      formula: skill.formula,
      tags: skill.tags,
      description: skill.description,
      status: skill.status,
      duration: skill.duration,
      deferred: skill.deferred,
      priority: skill.priority,
      cooldown: skill.cooldown,
      isSignature: skill.isSignature,
      reelPreviewFaces: previewFaces,
    };
  }

  function normalizeSkillCooldowns(hero = {}) {
    const source = hero.skillCooldowns && typeof hero.skillCooldowns === "object" ? hero.skillCooldowns : {};
    hero.skillCooldowns = Object.fromEntries(
      Object.entries(source)
        .map(([skillId, turns]) => [skillId, Math.max(0, Math.floor(Number(turns) || 0))])
        .filter(([, turns]) => turns > 0)
    );
    return hero.skillCooldowns;
  }

  function skillCooldownRemaining(hero = {}, cooldownKey = "") {
    const cooldowns = normalizeSkillCooldowns(hero);
    return Math.max(0, Math.floor(Number(cooldowns[cooldownKey]) || 0));
  }

  function tickHeroCooldowns(hero = {}) {
    const cooldowns = normalizeSkillCooldowns(hero);
    Object.keys(cooldowns).forEach((skillId) => {
      const nextTurns = Math.max(0, cooldowns[skillId] - 1);
      if (nextTurns > 0) cooldowns[skillId] = nextTurns;
      else delete cooldowns[skillId];
    });
    return cooldowns;
  }

  function applySkillCooldown(hero = {}, cooldownKey = "", amount = 0) {
    if (!hero || !cooldownKey) return 0;
    const turns = Math.max(0, Math.min(6, Math.floor(Number(amount) || 0)));
    const cooldowns = normalizeSkillCooldowns(hero);
    if (turns > 0) cooldowns[cooldownKey] = turns;
    else delete cooldowns[cooldownKey];
    return Math.max(0, Number(cooldowns[cooldownKey] || 0));
  }

  function normalizeTimedEffects(target = {}) {
    const source = target.timedEffects && typeof target.timedEffects === "object" ? target.timedEffects : {};
    target.timedEffects = Object.fromEntries(
      Object.entries(source)
        .map(([key, effect]) => {
          if (!effect || typeof effect !== "object") return [key, null];
          const turns = Math.max(0, Math.floor(Number(effect.turns || 0)));
          if (!turns) return [key, null];
          return [key, {
            kind: effect.kind || key,
            amount: Math.max(0, Math.floor(Number(effect.amount || 0))),
            turns,
          }];
        })
        .filter(([, effect]) => effect)
    );
    return target.timedEffects;
  }

  function addTimedEffect(target = {}, key = "", data = {}) {
    if (!target || !key) return null;
    const effects = normalizeTimedEffects(target);
    const current = effects[key] || { kind: key, amount: 0, turns: 0 };
    effects[key] = {
      kind: data.kind || current.kind || key,
      amount: Math.max(Number(current.amount || 0), Math.max(1, Number(data.amount || 1))),
      turns: Math.max(Number(current.turns || 0), Math.max(1, Number(data.turns || 1))),
    };
    return effects[key];
  }

  function timedEffectAmount(target = {}, key = "") {
    const effect = normalizeTimedEffects(target)[key];
    return Math.max(0, Number(effect?.amount || 0));
  }

  function tickTimedEffects(target = {}) {
    const effects = normalizeTimedEffects(target);
    Object.keys(effects).forEach((key) => {
      const nextTurns = Math.max(0, Number(effects[key].turns || 0) - 1);
      if (nextTurns > 0) effects[key].turns = nextTurns;
      else delete effects[key];
    });
    return effects;
  }

  function beginHeroTurn(combat, hero, options = {}) {
    if (!combat || !hero) return null;
    const force = options.force === true;
    const tickCooldowns = options.tickCooldowns === true;
    if (!force && combat.diceState?.heroId === hero.id && Array.isArray(combat.diceState.rolls)) return combat.diceState;
    if (tickCooldowns) tickHeroCooldowns(hero);
    if (tickCooldowns) tickTimedEffects(hero);
    const loadout = normalizeHeroLoadout(hero);
    combat.diceState = {
      heroId: hero.id,
      phase: "spinning",
      selectLimit: loadout.selectLimit,
      diceCount: loadout.diceCount,
      rolls: loadout.dice.map((die, dieIndex) => ({
        ...createRoll(hero, die, dieIndex),
        spinState: "spinning",
        stoppedAt: 0,
      })),
      spinningRollIds: [],
      selectedRollIds: [],
      selectionOrder: [],
      selectionCursor: 0,
      stopCursor: 0,
      intent: null,
      targetRequired: false,
      pendingTargetId: "",
    };
    combat.diceState.spinningRollIds = combat.diceState.rolls.map((roll) => roll.id);
    return combat.diceState;
  }

  function setIntent(combat, intent) {
    if (!combat?.diceState) return null;
    combat.diceState.intent = intent || null;
    return combat.diceState;
  }

  function clearSelection(combat) {
    if (!combat?.diceState) return null;
    combat.diceState.phase = combat.diceState.rolls.every((roll) => roll.spinState === "stopped") ? "select" : "spinning";
    combat.diceState.selectedRollIds = [];
    combat.diceState.selectionOrder = [];
    combat.diceState.selectionCursor = 0;
    combat.diceState.rolls.forEach((roll) => {
      delete roll.selectedAt;
    });
    combat.diceState.targetRequired = false;
    combat.diceState.pendingTargetId = "";
    return combat.diceState;
  }

  function toggleRollSelection(combat, rollId) {
    if (!combat?.diceState || !rollId) return false;
    if (combat.diceState.phase !== "select") return false;
    const roll = combat.diceState.rolls.find((entry) => entry.id === rollId);
    if (!roll) return false;
    const alreadySelected = combat.diceState.selectedRollIds.includes(rollId);
    if (alreadySelected) {
      combat.diceState.selectedRollIds = combat.diceState.selectedRollIds.filter((entry) => entry !== rollId);
      combat.diceState.selectionOrder = combat.diceState.selectionOrder.filter((entry) => entry !== rollId);
      delete roll.selectedAt;
      combat.diceState.targetRequired = false;
      combat.diceState.pendingTargetId = "";
      if (combat.diceState.phase !== "select") combat.diceState.phase = "select";
      return true;
    }
    if (combat.diceState.selectedRollIds.length >= combat.diceState.selectLimit) return false;
    combat.diceState.selectionCursor += 1;
    roll.selectedAt = combat.diceState.selectionCursor;
    combat.diceState.selectedRollIds.push(rollId);
    combat.diceState.selectionOrder = [...combat.diceState.selectedRollIds]
      .map((id) => combat.diceState.rolls.find((entry) => entry.id === id))
      .filter(Boolean)
      .sort((left, right) => Number(left.selectedAt || 0) - Number(right.selectedAt || 0))
      .map((entry) => entry.id);
    return true;
  }

  function selectedRolls(combat) {
    if (!combat?.diceState) return [];
    const orderedIds = combat.diceState.selectionOrder.length
      ? combat.diceState.selectionOrder
      : [...combat.diceState.selectedRollIds]
          .map((id) => combat.diceState.rolls.find((entry) => entry.id === id))
          .filter(Boolean)
          .sort((left, right) => Number(left.selectedAt || 0) - Number(right.selectedAt || 0))
          .map((entry) => entry.id);
    return orderedIds
      .map((rollId) => combat.diceState.rolls.find((entry) => entry.id === rollId))
      .filter(Boolean);
  }

  function targetNeededForSelection(combat) {
    return selectedRolls(combat).some((roll) => roll.targetMode === "enemy");
  }

  function evaluateRollMagnitude(roll) {
    return Math.max(0, evaluateFormula(roll.formula, roll.value, roll.effect));
  }

  function rollScore(hero, roll, intent) {
    const magnitude = evaluateRollMagnitude(roll);
    const priority = Number(roll.priority || 0);
    const offenseBias = Number(hero?.atk || 0);
    const defenseBias = Number(hero?.def || 0);
    if (intent === "defend") {
      if (roll.kind === "defend") return magnitude + priority + defenseBias + 12;
      if (roll.kind === "heal") return magnitude + priority + 6;
      return magnitude + priority - 8;
    }
    if (intent === "skill") {
      if (roll.isSignature || roll.kind === "skill") return magnitude + priority + offenseBias + 18;
      if (roll.kind === "attack") return magnitude + priority + offenseBias + 8;
      if (roll.kind === "heal" || roll.kind === "defend") return magnitude + priority + 2;
      return magnitude + priority;
    }
    if (roll.kind === "attack" || roll.kind === "skill") return magnitude + priority + offenseBias + 10;
    if (roll.kind === "heal") return magnitude + priority + 4;
    if (roll.kind === "defend") return magnitude + priority + defenseBias;
    return magnitude + priority;
  }

  function autoSelectForIntent(combat, hero, intent, options = {}) {
    if (!combat?.diceState || combat.diceState.phase !== "select") return [];
    if (options.reset !== false) clearSelection(combat);
    const ranked = [...combat.diceState.rolls].sort((left, right) => rollScore(hero, right, intent) - rollScore(hero, left, intent));
    const picks = [];
    for (const roll of ranked) {
      if (picks.length >= combat.diceState.selectLimit) break;
      if (toggleRollSelection(combat, roll.id)) picks.push(roll.id);
    }
    return picks;
  }

  function lowestHpLivingHero(party = [], preferredHero = null) {
    const living = party.filter((hero) => Number(hero?.hp || 0) > 0);
    if (!living.length) return preferredHero;
    return [...living].sort((left, right) => (left.hp / Math.max(1, left.maxHp)) - (right.hp / Math.max(1, right.maxHp)))[0];
  }

  function resolveSelectedRolls(args = {}) {
    const {
      combat,
      hero,
      party = [],
      enemies = [],
      targetId = "",
    } = args;
    const rolls = selectedRolls(combat);
    if (!combat?.diceState || combat.diceState.phase !== "select" || !hero || !rolls.length) {
      return { applied: false, targetRequired: false, logs: [], usedSignature: false };
    }
    let targetEnemy = enemies.find((enemy) => enemy.id === targetId && enemy.hp > 0) || enemies.find((enemy) => enemy.hp > 0) || null;
    const logs = [];
    let usedSignature = false;

    if (rolls.some((roll) => roll.targetMode === "enemy") && !targetEnemy) {
      combat.diceState.phase = "target";
      combat.diceState.targetRequired = true;
      return { applied: false, targetRequired: true, logs, usedSignature: false };
    }

    rolls.forEach((roll) => {
      const remainingCooldown = skillCooldownRemaining(hero, roll.cooldownKey);
      if (remainingCooldown > 0) {
        logs.push(`${hero.name}의 ${roll.skillName}은 아직 쿨타임 ${remainingCooldown}턴이 남아 사용할 수 없다.`);
        return;
      }
      const amount = evaluateRollMagnitude(roll);
      if (roll.targetMode === "enemy" && (!targetEnemy || targetEnemy.hp <= 0)) {
        targetEnemy = enemies.find((enemy) => enemy.hp > 0) || null;
      }
      if (roll.kind === "summon") {
        logs.push(`${hero.name}이 ${roll.skillName}을 준비했지만 소환 로직은 아직 보류 상태다.`);
      } else if (roll.targetMode === "enemy" && targetEnemy) {
        if (roll.kind === "support") {
          targetEnemy.exposed = Number(targetEnemy.exposed || 0) + amount;
          logs.push(`${hero.name}의 ${roll.skillName}이 ${targetEnemy.name}의 약점을 ${amount}만큼 드러냈다.`);
        } else if (roll.kind === "debuff") {
          const key = roll.status || "weakened";
          const duration = roll.duration || 2;
          const applied = addTimedEffect(targetEnemy, key, { kind: "debuff", amount, turns: duration });
          if (key === "stunned") targetEnemy.stunnedTurns = Math.max(Number(targetEnemy.stunnedTurns || 0), duration);
          targetEnemy.exposed = Number(targetEnemy.exposed || 0) + Math.max(1, Math.floor(amount / 2));
          logs.push(`${hero.name}의 ${roll.skillName}이 ${targetEnemy.name}에게 ${key} ${applied.amount} (${applied.turns}턴)을 부여했다.`);
        } else {
          const offenseBias = roll.kind === "skill" ? 1 : 0;
          const heroBuff = timedEffectAmount(hero, "empowered");
          const enemyWeakened = timedEffectAmount(targetEnemy, "weakened");
          const exposureBonus = Number(targetEnemy.exposed || 0);
          const defense = Math.max(0, Number(targetEnemy.def || 0) - exposureBonus);
          const dealt = Math.max(1, amount + Number(hero.atk || 0) + offenseBias + heroBuff + enemyWeakened - defense + rand(0, 1));
          targetEnemy.hp = Math.max(0, targetEnemy.hp - dealt);
          logs.push(`${hero.name}의 ${roll.skillName}(${roll.value})이 ${targetEnemy.name}에게 ${dealt} 피해를 입혔다.`);
          if (roll.kind === "lifesteal") {
            const beforeHp = Number(hero.hp || 0);
            const drain = Math.max(1, Math.ceil(dealt / 2));
            hero.hp = Math.min(Number(hero.maxHp || beforeHp), beforeHp + drain);
            const healed = hero.hp - beforeHp;
            if (healed > 0) logs.push(`${hero.name}이 ${roll.skillName}로 HP ${healed}를 흡수했다.`);
          }
          if (targetEnemy.hp <= 0) logs.push(`${targetEnemy.name}이 쓰러졌다.`);
        }
      } else if (roll.kind === "guard" || roll.kind === "defend") {
        hero.defend = true;
        hero.guard = Number(hero.guard || 0) + Math.max(1, amount);
        logs.push(`${hero.name}이 ${roll.skillName}로 방어 ${Math.max(1, amount)}를 준비했다.`);
      } else if (roll.kind === "buff") {
        const key = roll.status || "empowered";
        const duration = roll.duration || 2;
        const targetHero = roll.targetMode === "ally" || roll.targetMode === "party" ? (lowestHpLivingHero(party, hero) || hero) : hero;
        const applied = addTimedEffect(targetHero, key, { kind: "buff", amount, turns: duration });
        logs.push(`${hero.name}이 ${targetHero.name}에게 ${roll.skillName}로 ${key} ${applied.amount} (${applied.turns}턴)을 부여했다.`);
      } else {
        const targetHero = roll.targetMode === "self" ? hero : (lowestHpLivingHero(party, hero) || hero);
        const beforeHp = Number(targetHero.hp || 0);
        targetHero.hp = Math.min(Number(targetHero.maxHp || beforeHp), beforeHp + Math.max(1, amount));
        const healed = targetHero.hp - beforeHp;
        if (healed > 0) logs.push(`${hero.name}이 ${targetHero.name}에게 ${roll.skillName}로 HP ${healed} 회복을 부여했다.`);
        else logs.push(`${hero.name}이 ${roll.skillName}를 썼지만 회복이 필요하지 않았다.`);
      }
      if (roll.cooldown > 0) applySkillCooldown(hero, roll.cooldownKey, roll.cooldown);
      if (roll.isSignature) usedSignature = true;
    });

    combat.diceState.phase = "resolved";
    combat.diceState.targetRequired = false;
    combat.diceState.pendingTargetId = targetEnemy?.id || "";
    return {
      applied: true,
      targetRequired: false,
      logs,
      usedSignature,
    };
  }

  function describeRoll(hero, roll) {
    const definition = skillDefinition(hero, roll.skillId);
    return `${roll.value} · ${definition.name}`;
  }

  function stopNextRoll(combat) {
    if (!combat?.diceState) return null;
    const spinningRoll = combat.diceState.rolls.find((roll) => roll.spinState !== "stopped");
    if (!spinningRoll) {
      combat.diceState.phase = "select";
      return {
        stoppedRoll: null,
        allStopped: true,
        remainingSpinning: 0,
        diceState: combat.diceState,
      };
    }
    combat.diceState.stopCursor += 1;
    spinningRoll.spinState = "stopped";
    spinningRoll.stoppedAt = combat.diceState.stopCursor;
    combat.diceState.spinningRollIds = combat.diceState.rolls
      .filter((roll) => roll.spinState !== "stopped")
      .map((roll) => roll.id);
    const remainingSpinning = combat.diceState.spinningRollIds.length;
    const allStopped = remainingSpinning === 0;
    combat.diceState.phase = allStopped ? "select" : "spinning";
    return {
      stoppedRoll: spinningRoll,
      allStopped,
      remainingSpinning,
      diceState: combat.diceState,
    };
  }

  function stopAllRolls(combat) {
    if (!combat?.diceState) return null;
    const stoppedRolls = [];
    let result = null;
    do {
      result = stopNextRoll(combat);
      if (result?.stoppedRoll) stoppedRolls.push(result.stoppedRoll);
    } while (result && !result.allStopped);
    return {
      stoppedRolls,
      allStopped: Boolean(result?.allStopped),
      diceState: combat.diceState,
    };
  }

  return {
    DEFAULT_DICE_COUNT,
    DEFAULT_SELECT_LIMIT,
    skillDefinition,
    normalizeHeroLoadout,
    beginHeroTurn,
    setIntent,
    clearSelection,
    toggleRollSelection,
    autoSelectForIntent,
    selectedRolls,
    targetNeededForSelection,
    resolveSelectedRolls,
    describeRoll,
    cooldownKeyForFace,
    stopNextRoll,
    stopAllRolls,
    normalizeSkillCooldowns,
    skillCooldownRemaining,
    tickHeroCooldowns,
    applySkillCooldown,
    normalizeTimedEffects,
    addTimedEffect,
    tickTimedEffects,
  };
}
