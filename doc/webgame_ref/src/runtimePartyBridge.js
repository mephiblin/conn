import {
  buildDefaultKnownSkillIds,
  createDefaultDiceLoadout,
  createDefaultSkillInventory,
  normalizeHeroDiceProfile,
} from "./diceSkillLoadout.js";

export function createRuntimePartyBridge(deps = {}) {
  const {
    getState = () => ({}),
    classes = [],
    npcs = {},
    partyModelLimits = { maxMembers: 2 },
    normalizeInventoryEntry = (value) => value,
    vendorOffer = () => null,
    focusedNpcClassIndices = () => [],
    focusedNpcClassNames = () => [],
    activeNpcQuestHook = () => null,
    activeNpcQuestSeed = () => null,
    nextClassMilestone = () => null,
    addLog = () => {},
    setMode = () => {},
    grantVendorInventoryEntry = () => {},
    vendorInventoryEntryLabel = () => "",
  } = deps;

  function buildTownPlaces() {
    const state = getState();
    const innkeeper = npcs.npc_innkeeper;
    const trainer = npcs.npc_trainer;
    const smith = npcs.npc_smith;
    const apothecary = npcs.npc_apothecary;
    const scholar = npcs.npc_scholar;
    const bulletinBoard = npcs.npc_bulletin_board;
    const gatekeeper = npcs.npc_gatekeeper;
    const innOffer = vendorOffer("vendor_inn");
    const trainerOffer = vendorOffer("vendor_trainer");
    const smithOffer = vendorOffer("vendor_smith");
    const apothecaryOffer = vendorOffer("vendor_apothecary");
    const trainerFocusNames = focusedNpcClassNames(trainer);
    const trainerText = [
      trainerOffer?.summary || trainer.description,
      trainer?.progressionHooks?.note || "",
      trainerFocusNames.length ? `집중 클래스: ${trainerFocusNames.join(", ")}` : "",
    ].filter(Boolean).join(" · ");
    const scholarHook = activeNpcQuestHook(scholar);
    const scholarSeed = activeNpcQuestSeed(scholar);
    const scholarText = [
      scholar.description,
      scholarHook?.note || "",
      scholarSeed?.title ? `seed: ${scholarSeed.title}` : "",
    ].filter(Boolean).join(" · ");
    return [
      [innkeeper.name, innOffer?.summary || innkeeper.description, () => {
        if (state.resources.gold >= (innOffer?.cost?.gold || 0)) {
          state.resources.gold -= innOffer.cost.gold;
          state.party.forEach((p) => { p.hp = p.maxHp; });
          addLog(innkeeper.log);
        }
      }],
      [trainer.name, trainerText, () => {
        const trained = [];
        const focusedClasses = focusedNpcClassIndices(trainer);
        state.party.forEach((p) => {
          if (focusedClasses.length && !focusedClasses.includes(p.classIndex)) return;
          const milestone = nextClassMilestone(p);
          if (!milestone || p.xp < (milestone.xpCost || 0)) return;
          p.xp -= milestone.xpCost || 0;
          p.maxHp += milestone.hpGain || 0;
          p.hp = Math.min(p.maxHp, p.hp + (milestone.hpGain || 0));
          p.atk += milestone.atkGain || 0;
          p.def += milestone.defGain || 0;
          p.prof[p.category] = (p.prof[p.category] || 0) + (milestone.profGain || 0);
          if (milestone.passiveUnlock) p.passive = true;
          p.trainingLevel = (p.trainingLevel || 0) + 1;
          trained.push(`${p.name}:${milestone.label}`);
        });
        addLog(trained.length ? `${trainer.log} (${trained.join(", ")})` : "훈련할 준비가 된 인원이 없다.");
      }],
      [smith.name, smithOffer?.summary || smith.description, () => {
        if (state.resources.gold >= (smithOffer?.cost?.gold || 0)) {
          state.resources.gold -= smithOffer.cost.gold;
          state.party.slice(0, 2).forEach((p) => { p.atk += smithOffer?.rewards?.frontlineAtkGain || 0; });
          addLog(`${smith.log} (+${smithOffer?.rewards?.frontlineAtkGain || 0} 공격)`);
        }
      }],
      [apothecary.name, apothecaryOffer?.summary || apothecary.description, () => {
        if (state.resources.gold >= (apothecaryOffer?.cost?.gold || 0)) {
          state.resources.gold -= apothecaryOffer.cost.gold;
          (apothecaryOffer.inventory || []).forEach((entry) => grantVendorInventoryEntry(entry));
          addLog(`${apothecary.log} (${(apothecaryOffer.inventory || []).map((entry) => vendorInventoryEntryLabel(entry)).join(", ")})`);
        }
      }],
      [scholar.name, scholarText, () => addLog([scholar.log, scholarHook?.note, scholarSeed?.note].filter(Boolean).join(" "))],
      [bulletinBoard?.name || "게시판", bulletinBoard?.description || "원정 공고를 확인하고 던전으로 향한다.", () => setMode("dungeon")],
      [gatekeeper.name, gatekeeper.description, () => addLog(gatekeeper.log || gatekeeper.description)],
    ];
  }

  function makeHero(slot, classIndex, name) {
    const c = classes[classIndex] || classes[0];
    const hero = {
      id: `hero_${slot}`,
      name: name || ["코르", "사디아", "타렉", "나부"][slot] || `용병 ${slot + 1}`,
      classIndex,
      row: slot === 0 ? "전열" : "후열",
      ...c,
      maxHp: c.hp,
      hp: c.hp,
      status: [],
      xp: 0,
      prof: { [c.category]: 0 },
      trainingLevel: 0,
      passive: false,
      defend: false,
    };
    hero.knownSkillIds = buildDefaultKnownSkillIds(hero);
    hero.diceLoadout = createDefaultDiceLoadout(hero);
    hero.skillInventory = createDefaultSkillInventory(hero);
    hero.skillInventoryTable = hero.skillInventory;
    hero.skillCooldowns = {};
    return hero;
  }

  function normalizeHeroState(hero, slot = 0) {
    const c = classes[hero.classIndex] || classes[0];
    hero.id = hero.id || `hero_${slot}`;
    hero.row = hero.row || (slot === 0 ? "전열" : "후열");
    hero.cls = c.cls;
    hero.skillId = c.skillId;
    hero.category = c.category;
    hero.status = Array.isArray(hero.status) ? hero.status : [];
    hero.prof = hero.prof && typeof hero.prof === "object" ? hero.prof : { [c.category]: 0 };
    if (hero.prof[c.category] == null) hero.prof[c.category] = 0;
    hero.trainingLevel = Number(hero.trainingLevel || 0);
    hero.passive = Boolean(hero.passive);
    hero.defend = Boolean(hero.defend);
    hero.equipment = hero.equipment && typeof hero.equipment === "object"
      ? Object.fromEntries(
          Object.entries(hero.equipment)
            .map(([key, value]) => [key, normalizeInventoryEntry(value)])
            .filter(([, value]) => value)
        )
      : {};
    if (typeof hero.maxHp !== "number") hero.maxHp = c.hp;
    if (typeof hero.hp !== "number") hero.hp = hero.maxHp;
    hero.skillCooldowns = hero.skillCooldowns && typeof hero.skillCooldowns === "object"
      ? Object.fromEntries(
          Object.entries(hero.skillCooldowns)
            .map(([skillId, turns]) => [skillId, Math.max(0, Math.floor(Number(turns) || 0))])
            .filter(([, turns]) => turns > 0)
        )
      : {};
    if (hero.skillInventoryTable && !hero.skillInventory) hero.skillInventory = hero.skillInventoryTable;
    normalizeHeroDiceProfile(hero, {
      heroId: hero.id,
      defaultKnownSkillIds: buildDefaultKnownSkillIds(hero),
    });
    return hero;
  }

  function createCompanionRecord(hero, seed = {}) {
    return {
      npcId: seed.npcId || null,
      recruited: true,
      joinedParty: Boolean(seed.joinedParty),
      hero: JSON.parse(JSON.stringify(hero)),
      placementStateKey: seed.placementStateKey || null,
    };
  }

  function normalizePartyModel(party = [], companion = null) {
    const normalizedParty = Array.isArray(party)
      ? party.map((hero, slot) => normalizeHeroState(JSON.parse(JSON.stringify(hero)), slot))
      : [];
    const protagonistSource = normalizedParty.find((hero) => !hero.isCompanion) || normalizedParty[0] || makeHero(0, 0, "코난");
    const protagonist = normalizeHeroState(protagonistSource, 0);
    protagonist.isCompanion = false;
    protagonist.row = "전열";

    let companionRecord = companion ? JSON.parse(JSON.stringify(companion)) : null;
    let companionHero = companionRecord?.hero ? normalizeHeroState(companionRecord.hero, 1) : null;

    if (!companionHero) {
      const fallbackCompanion = normalizedParty.find((hero) => hero.id !== protagonist.id);
      if (fallbackCompanion) {
        companionHero = normalizeHeroState(fallbackCompanion, 1);
        companionRecord = createCompanionRecord(companionHero, {
          joinedParty: true,
        });
      }
    }

    if (companionHero) {
      companionHero.isCompanion = true;
      companionHero.row = "후열";
      companionRecord = createCompanionRecord(companionHero, {
        ...companionRecord,
        joinedParty: Boolean(companionRecord?.joinedParty),
      });
    } else {
      companionRecord = null;
    }

    const nextParty = [protagonist];
    if (companionRecord?.recruited && companionRecord.joinedParty && companionHero) nextParty.push(companionHero);
    return {
      party: nextParty.slice(0, partyModelLimits.maxMembers),
      companion: companionRecord,
      trimmedCount: Math.max(0, normalizedParty.length - nextParty.length),
    };
  }

  function buildCompanionHero(profile = {}, fallbackName = "동료") {
    const classIndex = Number.isInteger(profile.classIndex) ? profile.classIndex : 2;
    const hero = makeHero(1, classIndex, profile.name || fallbackName);
    hero.id = profile.id || `companion_${Date.now()}`;
    hero.isCompanion = true;
    hero.note = profile.note || "";
    hero.row = "후열";
    return normalizeHeroState(hero, 1);
  }

  return {
    buildTownPlaces,
    makeHero,
    normalizeHeroState,
    createCompanionRecord,
    normalizePartyModel,
    buildCompanionHero,
  };
}
