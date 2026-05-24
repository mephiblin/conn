export function createEditorContentPanelBridge(deps = {}) {
  const {
    questDefinitions = {},
    monsters = {},
    skills = {},
    mapProfiles = [],
    items = {},
    vendors = {},
    lootTables = {},
    rarityDefinitions = {},
    affixDefinitions = {},
    affixPoolDefinitions = {},
    renderEditorItemBasePanel = () => "",
    renderEditorQuestDefinitionPanel = () => "",
    renderEditorMonsterDefinitionPanel = () => "",
    renderEditorSkillDefinitionPanel = () => "",
    renderEditorVendorInventoryPanel = () => "",
    renderEditorLootTablePanel = () => "",
    renderEditorAffixRarityPanel = () => "",
    renderEditorSampleItemPanel = () => "",
    escapeHtml = (value) => String(value ?? ""),
    questDefinitionsJson = () => "",
    questGeneratorSelection = () => ({ archetype: "boss_hunt", mapKind: "twisted_temple", floor: 1, targetMonsterId: "", candidates: [], mapOptions: [] }),
    mapKindLabel = (value) => String(value || ""),
    monsterDefinitionsJson = () => "",
    skillDefinitionsJson = () => "",
    itemDefinitionsJson = () => "",
    itemDefinitionOptionListHtml = () => "",
    rarityDefinitionOptionListHtml = () => "",
    affixPoolOptionListHtml = () => "",
    vendorsJson = () => "",
    vendorInventoryEntryItemId = () => "",
    vendorInventorySummary = () => "",
    lootTableDefinitionIds = () => [],
    lootTablesJson = () => "",
    rarityDefinitionsJson = () => "",
    affixDefinitionsJson = () => "",
    affixPoolDefinitionsJson = () => "",
    sampleItemPreviewJson = () => "",
    activeRarityDefinitionId = () => "",
    activeAffixPoolId = () => "",
  } = deps;

  return function buildEditorContentPanels({
    questDefId,
    questDef,
    itemDefId,
    itemDef,
    monsterDefId,
    monsterDef,
    skillDefId,
    skillDef,
    vendorDefId,
    vendorDef,
    selectedVendorRotation,
    selectedVendorRotationState,
    lootTableId,
    lootTableDef,
    selectedLootTier,
    selectedLootTierState,
    selectedLootBonus,
    selectedLootBonusState,
    selectedCombatRewardProfile,
    selectedCombatRewardProfileState,
    rarityDefId,
    rarityDef,
    affixDefId,
    affixDef,
    affixPoolId,
    affixPoolDef,
    sampleItemPreview,
  } = {}) {
    const questGenerator = questGeneratorSelection(questDef);
    const questPanelMarkup = renderEditorQuestDefinitionPanel({
      subtitle: questDef ? `${questDefId} · ${questDef.name || "unnamed"}` : "quest 없음",
      bodyMarkup: questDef ? `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addQuestDefinitionBtn">quest 추가</button>
                <button id="addGeneratedQuestDefinitionBtn">테이블 생성 quest 추가</button>
                <button id="duplicateQuestDefinitionBtn">선택 quest 복제</button>
                <button id="removeQuestDefinitionBtn">선택 quest 삭제</button>
              </div>
              <div class="preset-field">
                <label for="questDefinitionSelect">Selected quest</label>
                <select id="questDefinitionSelect">${Object.entries(questDefinitions).map(([id, quest]) => `<option value="${id}" ${id === questDefId ? "selected" : ""}>${id} · ${escapeHtml(quest.name || id)}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="questDefinitionNameInput">Quest name</label>
                <input id="questDefinitionNameInput" value="${escapeHtml(questDef.name || "")}" />
              </div>
              <div class="preset-field">
                <label for="questDefinitionDescriptionInput">Description</label>
                <textarea id="questDefinitionDescriptionInput" rows="3" spellcheck="false">${escapeHtml(questDef.description || "")}</textarea>
              </div>
              <div class="preset-toolbar">
                <label>Map kind <select id="questDefinitionMapKindInput">${questGenerator.mapOptions.map((option) => `<option value="${escapeHtml(option.mapKind)}" ${option.mapKind === (questDef.mapKind || "twisted_temple") ? "selected" : ""}>${escapeHtml(option.mapKindName)} · ${escapeHtml(option.mapKind)}</option>`).join("")}</select></label>
                <label>Start floor <input id="questDefinitionStartFloorInput" type="number" min="1" value="${Math.max(1, Number(questDef.startFloor || 1))}" /></label>
              </div>
              <div class="preset-field">
                <label>Quest generator</label>
                <div class="preset-toolbar">
                  <label>Archetype
                    <select id="questDefinitionGeneratorArchetypeSelect">
                      <option value="boss_hunt" ${questGenerator.archetype === "boss_hunt" ? "selected" : ""}>boss_hunt</option>
                      <option value="subjugation" ${questGenerator.archetype === "subjugation" ? "selected" : ""}>subjugation</option>
                    </select>
                  </label>
                  <label>Target
                    <select id="questDefinitionGeneratorTargetSelect">
                      ${questGenerator.candidates.map((entry) => `<option value="${escapeHtml(entry.monsterId)}" ${entry.monsterId === questGenerator.targetMonsterId ? "selected" : ""}>${escapeHtml(entry.monster.name || entry.monsterId)} · ${escapeHtml(entry.monsterId)}</option>`).join("")}
                    </select>
                  </label>
                  <button id="generateQuestDefinitionBtn">현재 quest 재생성</button>
                </div>
                <div class="muted">${questGenerator.candidates.length ? escapeHtml(questGenerator.candidates.find((entry) => entry.monsterId === questGenerator.targetMonsterId)?.summary || "") : `${escapeHtml(mapKindLabel(questGenerator.mapKind))} ${questGenerator.floor}층에서 조건에 맞는 몬스터가 없다.`}</div>
              </div>
              <div class="preset-field">
                <label for="questDefinitionConditionKindInput">Condition kind</label>
                <select id="questDefinitionConditionKindInput">
                  <option value="bosses_defeated" ${(questDef.conditions?.kind || "bosses_defeated") === "bosses_defeated" ? "selected" : ""}>bosses_defeated</option>
                  <option value="specific_monsters_defeated" ${questDef.conditions?.kind === "specific_monsters_defeated" ? "selected" : ""}>specific_monsters_defeated</option>
                </select>
              </div>
              <div class="preset-field">
                <label for="questDefinitionConditionSummaryInput">Condition summary</label>
                <input id="questDefinitionConditionSummaryInput" value="${escapeHtml(questDef.conditions?.summary || "")}" />
              </div>
              <div class="preset-toolbar">
                <label>Required count <input id="questDefinitionBossCountInput" type="number" min="1" value="${Math.max(1, Number(questDef.conditions?.requiredCount || questDef.conditions?.bossesDefeatedAtLeast || 1))}" /></label>
                <label>Gold <input id="questDefinitionGoldRewardInput" type="number" min="0" value="${Math.max(0, Number(questDef.rewards?.gold || 0))}" /></label>
                <label>XP <input id="questDefinitionXpRewardInput" type="number" min="0" value="${Math.max(0, Number(questDef.rewards?.xp || 0))}" /></label>
              </div>
              <div class="preset-field">
                <label for="questDefinitionTargetMonsterIdsInput">Target monsters</label>
                <input id="questDefinitionTargetMonsterIdsInput" value="${escapeHtml((questDef.conditions?.targetMonsterIds || []).join(", "))}" placeholder="blind_priest, black_water_beast" />
              </div>
              <div class="preset-field">
                <label for="questDefinitionRewardFlagInput">Reward flag</label>
                <input id="questDefinitionRewardFlagInput" value="${escapeHtml(questDef.rewards?.flag || "")}" />
              </div>
              <div class="preset-field">
                <label for="questDefinitionRewardItemsInput">Reward items</label>
                <input id="questDefinitionRewardItemsInput" value="${escapeHtml((questDef.rewards?.items || []).map((entry) => `${entry.itemId}:${entry.quantity || 1}`).join(", "))}" placeholder="bandage:1, antivenom:2" />
              </div>
              <div class="preset-field">
                <label for="questDefinitionsJsonInput">Quests JSON</label>
                <textarea id="questDefinitionsJsonInput" rows="8" spellcheck="false">${escapeHtml(questDefinitionsJson())}</textarea>
              </div>
              <div class="muted">map ${escapeHtml(mapKindLabel(questDef.mapKind || "-"))} · floor ${Number(questDef.startFloor || 1)} · condition ${(questDef.conditions?.kind || "bosses_defeated")} · reward gold ${Number(questDef.rewards?.gold || 0)} xp ${Number(questDef.rewards?.xp || 0)}</div>
            </div>
          ` : `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addQuestDefinitionBtn">quest 추가</button>
                <button id="addGeneratedQuestDefinitionBtn">테이블 생성 quest 추가</button>
              </div>
              <div class="muted">quest definition이 비어 있다. 수동 추가 또는 테이블 생성으로 첫 quest를 만든다.</div>
            </div>
          `,
    });

    const monsterPanelMarkup = renderEditorMonsterDefinitionPanel({
      subtitle: monsterDef ? `${monsterDefId} · ${monsterDef.name || "unnamed"}` : "monster 없음",
      bodyMarkup: monsterDef ? `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addMonsterDefinitionBtn">monster 추가</button>
                <button id="duplicateMonsterDefinitionBtn">선택 monster 복제</button>
                <button id="removeMonsterDefinitionBtn">선택 monster 삭제</button>
              </div>
              <div class="preset-field">
                <label for="monsterDefinitionSelect">Selected monster</label>
                <select id="monsterDefinitionSelect">${Object.entries(monsters).map(([id, monster]) => `<option value="${id}" ${id === monsterDefId ? "selected" : ""}>${id} · ${escapeHtml(monster.name || id)}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="monsterDefinitionNameInput">Monster name</label>
                <input id="monsterDefinitionNameInput" value="${escapeHtml(monsterDef.name || "")}" />
              </div>
              <div class="preset-toolbar">
                <label>HP <input id="monsterDefinitionHpInput" type="number" min="1" value="${Math.max(1, Number(monsterDef.hp || 1))}" /></label>
                <label>ATK <input id="monsterDefinitionAtkInput" type="number" min="1" value="${Math.max(1, Number(monsterDef.atk || 1))}" /></label>
                <label>DEF <input id="monsterDefinitionDefInput" type="number" min="0" value="${Math.max(0, Number(monsterDef.def || 0))}" /></label>
                <label>XP <input id="monsterDefinitionXpInput" type="number" min="0" value="${Math.max(0, Number(monsterDef.xp || 0))}" /></label>
              </div>
              <div class="preset-toolbar">
                <label>ATK min <input id="monsterDefinitionAtkMinInput" type="number" min="0" value="${monsterDef.atkMin ?? ""}" placeholder="unset" /></label>
                <label>ATK max <input id="monsterDefinitionAtkMaxInput" type="number" min="0" value="${monsterDef.atkMax ?? ""}" placeholder="unset" /></label>
                <label>Weight <input id="monsterDefinitionSpawnWeightInput" type="number" min="1" value="${Math.max(1, Number(monsterDef.spawn?.weight || 1))}" /></label>
              </div>
              <div class="preset-field">
                <label for="monsterDefinitionAiInput">AI tag</label>
                <input id="monsterDefinitionAiInput" value="${escapeHtml(monsterDef.ai || "")}" />
              </div>
              <div class="preset-field">
                <label for="monsterDefinitionMapKindsInput">Map kinds</label>
                <input id="monsterDefinitionMapKindsInput" value="${escapeHtml((monsterDef.spawn?.mapKinds || []).join(", "))}" placeholder="twisted_temple, coral_coast, ruins" />
              </div>
              <div class="preset-field">
                <label for="monsterDefinitionRolesInput">Spawn roles</label>
                <input id="monsterDefinitionRolesInput" value="${escapeHtml((monsterDef.spawn?.roles || []).join(", "))}" placeholder="start, key, guard, boss" />
              </div>
              <div class="preset-toolbar">
                <label>Min floor <input id="monsterDefinitionMinFloorInput" type="number" min="1" value="${Math.max(1, Number(monsterDef.spawn?.minFloor || 1))}" /></label>
                <label>Max floor <input id="monsterDefinitionMaxFloorInput" type="number" min="1" value="${monsterDef.spawn?.maxFloor ?? ""}" placeholder="unset" /></label>
              </div>
              <div class="preset-toolbar">
                <label>HP/floor <input id="monsterDefinitionHpScaleInput" type="number" min="0" step="0.01" value="${Number(monsterDef.scaling?.hpPerFloor || 0)}" /></label>
                <label>ATK/floor <input id="monsterDefinitionAtkScaleInput" type="number" min="0" step="0.01" value="${Number(monsterDef.scaling?.atkPerFloor || 0)}" /></label>
                <label><input id="monsterDefinitionBossInput" type="checkbox" ${monsterDef.boss ? "checked" : ""} /> boss</label>
              </div>
              <div class="preset-field">
                <label for="monsterDefinitionsJsonInput">Monsters JSON</label>
                <textarea id="monsterDefinitionsJsonInput" rows="8" spellcheck="false">${escapeHtml(monsterDefinitionsJson())}</textarea>
              </div>
              <div class="muted">spawn ${(monsterDef.spawn?.mapKinds || []).join(", ") || "all"} · floor ${monsterDef.spawn?.minFloor || 1}-${monsterDef.spawn?.maxFloor || "all"} · hp +${Math.round(Number(monsterDef.scaling?.hpPerFloor || 0) * 100)}%/floor · atk +${Math.round(Number(monsterDef.scaling?.atkPerFloor || 0) * 100)}%/floor</div>
            </div>
          ` : `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addMonsterDefinitionBtn">monster 추가</button>
              </div>
              <div class="muted">monster definition이 비어 있다. 버튼으로 첫 monster를 추가한다.</div>
            </div>
          `,
    });

    const skillFormulaOptions = [
      ["die_as_effect", "눈 = 효과"],
      ["die_plus_effect", "눈 + 효과"],
      ["die_minus_effect", "눈 - 효과"],
      ["die_times_effect", "눈 x 효과"],
      ["die_divide_effect", "눈 / 효과"],
      ["die_equals_effect", "고정 효과"],
    ];
    const skillPanelMarkup = renderEditorSkillDefinitionPanel({
      subtitle: skillDef ? `${skillDefId} · ${skillDef.name || "unnamed"}` : "skill 없음",
      bodyMarkup: skillDef ? `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addAttackSkillDefinitionBtn">attack 추가</button>
                <button id="addHealSkillDefinitionBtn">heal 추가</button>
                <button id="addBuffSkillDefinitionBtn">buff 추가</button>
                <button id="addDebuffSkillDefinitionBtn">debuff 추가</button>
                <button id="addLifestealSkillDefinitionBtn">lifesteal 추가</button>
                <button id="duplicateSkillDefinitionBtn">선택 skill 복제</button>
                <button id="removeSkillDefinitionBtn">선택 skill 삭제</button>
              </div>
              <div class="preset-field">
                <label for="skillDefinitionSelect">Selected skill</label>
                <select id="skillDefinitionSelect">${Object.entries(skills).map(([id, skill]) => `<option value="${id}" ${id === skillDefId ? "selected" : ""}>${id} · ${escapeHtml(skill.name || id)}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="skillDefinitionNameInput">Skill name</label>
                <input id="skillDefinitionNameInput" value="${escapeHtml(skillDef.name || "")}" />
              </div>
              <div class="preset-toolbar">
                <label>Kind
                  <select id="skillDefinitionKindSelect">
                    ${["attack", "guard", "heal", "support", "buff", "debuff", "lifesteal", "summon"].map((kind) => `<option value="${kind}" ${skillDef.kind === kind ? "selected" : ""}>${kind}</option>`).join("")}
                  </select>
                </label>
                <label>Target
                  <select id="skillDefinitionTargetModeSelect">
                    ${["enemy", "ally", "self", "all_enemies", "party"].map((mode) => `<option value="${mode}" ${(skillDef.targetMode || "enemy") === mode ? "selected" : ""}>${mode}</option>`).join("")}
                  </select>
                </label>
                <label>Formula
                  <select id="skillDefinitionFormulaSelect">
                    ${skillFormulaOptions.map(([formula, label]) => `<option value="${formula}" ${(skillDef.formula || "die_as_effect") === formula ? "selected" : ""}>${label}</option>`).join("")}
                  </select>
                </label>
              </div>
              <div class="preset-toolbar">
                <label>Effect <input id="skillDefinitionEffectInput" type="number" step="1" value="${Number(skillDef.effect || 0)}" /></label>
                <label>Cooldown <input id="skillDefinitionCooldownInput" type="number" min="0" max="6" step="1" value="${Math.max(0, Number(skillDef.cooldown || 0))}" /></label>
                <label>Duration <input id="skillDefinitionDurationInput" type="number" min="0" step="1" value="${Number(skillDef.duration || 0)}" /></label>
              </div>
              <div class="preset-toolbar">
                <label>Buy <input id="skillDefinitionBuyPriceInput" type="number" min="0" step="1" value="${Math.max(0, Number(skillDef.buyPrice || 0))}" /></label>
                <label>Sell <input id="skillDefinitionSellPriceInput" type="number" min="0" step="1" value="${Math.max(0, Number(skillDef.sellPrice || 0))}" /></label>
                <label>Status <input id="skillDefinitionStatusInput" value="${escapeHtml(skillDef.status || "")}" placeholder="stunned, slowed, weakened" /></label>
              </div>
              <div class="preset-field">
                <label for="skillDefinitionCatalogIdsInput">Catalog IDs</label>
                <input id="skillDefinitionCatalogIdsInput" value="${escapeHtml((skillDef.catalogIds || []).join(", "))}" placeholder="merchant_basic, trainer_skill_rotation" />
              </div>
              <div class="preset-field">
                <label for="skillDefinitionTagsInput">Tags</label>
                <input id="skillDefinitionTagsInput" value="${escapeHtml((skillDef.tags || []).join(", "))}" placeholder="melee, storm, debuff" />
              </div>
              <div class="preset-field">
                <label for="skillDefinitionDescriptionInput">Description</label>
                <textarea id="skillDefinitionDescriptionInput" rows="3" spellcheck="false">${escapeHtml(skillDef.description || "")}</textarea>
              </div>
              <label class="preset-field"><span><input id="skillDefinitionDeferredInput" type="checkbox" ${skillDef.deferred ? "checked" : ""} /> deferred logic</span></label>
              <div class="preset-field">
                <label for="skillDefinitionsJsonInput">Skills JSON</label>
                <textarea id="skillDefinitionsJsonInput" rows="8" spellcheck="false">${escapeHtml(skillDefinitionsJson())}</textarea>
              </div>
              <div class="muted">formula ${(skillDef.formula || "die_as_effect")} · kind ${(skillDef.kind || "attack")} · cooldown ${Number(skillDef.cooldown || 0)} · catalog ${(skillDef.catalogIds || []).join(", ") || "none"}</div>
            </div>
          ` : `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addAttackSkillDefinitionBtn">attack 추가</button>
                <button id="addHealSkillDefinitionBtn">heal 추가</button>
                <button id="addBuffSkillDefinitionBtn">buff 추가</button>
              </div>
              <div class="muted">skill definition이 비어 있다. 버튼으로 첫 skill을 추가한다.</div>
            </div>
          `,
    });

    const itemPanelMarkup = renderEditorItemBasePanel({
      subtitle: itemDef ? `${itemDefId} · ${itemDef.name || "unnamed"}` : "item 없음",
      bodyMarkup: itemDef ? `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addConsumableItemBtn">consumable 추가</button>
                <button id="addArtifactItemBtn">artifact 추가</button>
                <button id="addQuestItemBtn">quest 추가</button>
                <button id="duplicateItemDefinitionBtn">선택 item 복제</button>
                <button id="removeItemDefinitionBtn">선택 item 삭제</button>
              </div>
              <div class="preset-field">
                <label for="itemDefinitionSelect">Selected item</label>
                <select id="itemDefinitionSelect">${Object.entries(items).map(([id, item]) => `<option value="${id}" ${id === itemDefId ? "selected" : ""}>${id} · ${escapeHtml(item.name || id)}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="itemDefinitionNameInput">Item name</label>
                <input id="itemDefinitionNameInput" value="${escapeHtml(itemDef.name || "")}" />
              </div>
              <div class="preset-field">
                <label for="itemDefinitionKindSelect">Item kind</label>
                <select id="itemDefinitionKindSelect">
                  ${["consumable", "key", "artifact", "quest", "equipment"].map((kind) => `<option value="${kind}" ${itemDef.kind === kind ? "selected" : ""}>${kind}</option>`).join("")}
                </select>
              </div>
              <div class="preset-field">
                <label for="itemDefinitionsJsonInput">Items JSON</label>
                <textarea id="itemDefinitionsJsonInput" rows="8" spellcheck="false">${escapeHtml(itemDefinitionsJson())}</textarea>
              </div>
              ${itemDef.kind === "consumable" ? `
                <div class="preset-field">
                  <label for="itemDefinitionHealInput">Heal</label>
                  <input id="itemDefinitionHealInput" type="number" step="1" value="${Number(itemDef.heal || 0)}" />
                </div>
                <div class="preset-field">
                  <label for="itemDefinitionCureInput">Cure status</label>
                  <input id="itemDefinitionCureInput" value="${escapeHtml(itemDef.cure || "")}" />
                </div>
              ` : ""}
              ${itemDef.kind === "artifact" || itemDef.kind === "equipment" ? `
                <div class="preset-field">
                  <label for="itemDefinitionAttackInput">Attack</label>
                  <input id="itemDefinitionAttackInput" type="number" step="1" value="${Number(itemDef.attack || 0)}" />
                </div>
                <div class="preset-field">
                  <label for="itemDefinitionDefenseInput">Defense</label>
                  <input id="itemDefinitionDefenseInput" type="number" step="1" value="${Number(itemDef.defense || 0)}" />
                </div>
                <div class="preset-field">
                  <label for="itemDefinitionCurseInput">Curse</label>
                  <input id="itemDefinitionCurseInput" type="number" step="1" value="${Number(itemDef.curse || 0)}" />
                </div>
              ` : ""}
              ${itemDef.kind === "equipment" ? `
                <div class="preset-field">
                  <label for="itemDefinitionSlotInput">Equipment slot</label>
                  <input id="itemDefinitionSlotInput" value="${escapeHtml(itemDef.slot || "weapon")}" />
                </div>
              ` : ""}
              <div class="preset-field">
                <label for="itemDefinitionRarityInput">Rarity tag</label>
                <input id="itemDefinitionRarityInput" value="${escapeHtml(itemDef.rarity || "")}" placeholder="common / rare / relic" />
              </div>
              <div class="muted">kind ${itemDef.kind || "unknown"}${itemDef.slot ? ` · slot ${itemDef.slot}` : ""}${itemDef.rarity ? ` · rarity ${itemDef.rarity}` : ""}</div>
            </div>
          ` : `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addConsumableItemBtn">consumable 추가</button>
                <button id="addArtifactItemBtn">artifact 추가</button>
                <button id="addQuestItemBtn">quest 추가</button>
              </div>
              <div class="muted">item definition이 비어 있다. 버튼으로 첫 item을 추가한다.</div>
            </div>
          `,
    });

    const vendorPanelMarkup = renderEditorVendorInventoryPanel({
      subtitle: vendorDef ? `${vendorDefId} · ${vendorDef.summary || vendorDef.serviceType}` : "vendor 없음",
      bodyMarkup: vendorDef ? `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addSellBundleVendorBtn">sell_bundle 추가</button>
                <button id="addTrainerVendorBtn">train_party 추가</button>
                <button id="duplicateVendorDefinitionBtn">선택 vendor 복제</button>
                <button id="removeVendorDefinitionBtn">선택 vendor 삭제</button>
              </div>
              <div class="preset-field">
                <label for="vendorDefinitionSelect">Selected vendor</label>
                <select id="vendorDefinitionSelect">${Object.entries(vendors).map(([id, vendor]) => `<option value="${id}" ${id === vendorDefId ? "selected" : ""}>${id} · ${escapeHtml(vendor.summary || vendor.serviceType || id)}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="vendorDefinitionServiceTypeSelect">Service type</label>
                <select id="vendorDefinitionServiceTypeSelect">
                  ${["sell_bundle", "heal_party", "buff_frontline", "train_party"].map((type) => `<option value="${type}" ${vendorDef.serviceType === type ? "selected" : ""}>${type}</option>`).join("")}
                </select>
              </div>
              <div class="preset-field">
                <label for="vendorDefinitionSummaryInput">Summary</label>
                <textarea id="vendorDefinitionSummaryInput" rows="3" spellcheck="false">${escapeHtml(vendorDef.summary || "")}</textarea>
              </div>
              <div class="preset-field">
                <label for="vendorDefinitionGoldCostInput">Base gold cost</label>
                <input id="vendorDefinitionGoldCostInput" type="number" min="0" value="${Math.max(0, Number(vendorDef.cost?.gold || 0))}" />
              </div>
              <div class="preset-field">
                <label for="vendorDefinitionsJsonInput">Vendors JSON</label>
                <textarea id="vendorDefinitionsJsonInput" rows="8" spellcheck="false">${escapeHtml(vendorsJson())}</textarea>
              </div>
              <div class="preset-field">
                <label>Base inventory fields</label>
                <div class="preset-stack">
                  ${(vendorDef.inventory || []).map((entry, index) => `
                    <div class="preset-stack">
                      <div class="preset-toolbar">
                        <select data-vendor-base-item-id="${index}">
                          ${itemDefinitionOptionListHtml(vendorInventoryEntryItemId(entry))}
                        </select>
                        <label><input type="checkbox" data-vendor-base-generated="${index}" ${entry?.generated ? "checked" : ""} /> generated</label>
                        <button data-remove-vendor-base-item="${index}">삭제</button>
                      </div>
                      ${entry?.generated ? `
                        <div class="preset-toolbar">
                          <select data-vendor-base-rarity-id="${index}">
                            ${rarityDefinitionOptionListHtml(entry.rarityId || activeRarityDefinitionId())}
                          </select>
                          <select data-vendor-base-affix-pool-id="${index}">
                            ${affixPoolOptionListHtml(entry.affixPoolId || activeAffixPoolId())}
                          </select>
                        </div>
                      ` : ""}
                    </div>
                  `).join("") || `<div class="muted">base inventory 없음</div>`}
                  <button id="addVendorBaseItemBtn">base item 추가</button>
                </div>
              </div>
              <div class="preset-toolbar">
                <button id="addVendorRotationBtn">rotation 추가</button>
                ${selectedVendorRotation ? `<button id="removeVendorRotationBtn">선택 rotation 삭제</button>` : ""}
              </div>
              ${selectedVendorRotation ? `
                <div class="preset-field">
                  <label for="vendorRotationSelect">Selected rotation</label>
                  <select id="vendorRotationSelect">${(vendorDef.rotation || []).map((rotation, index) => `<option value="${index}" ${index === selectedVendorRotationState.index ? "selected" : ""}>${index} · ${escapeHtml(rotation.summary || `rotation_${index}`)}</option>`).join("")}</select>
                </div>
                <div class="preset-field">
                  <label for="vendorRotationSummaryInput">Rotation summary</label>
                  <textarea id="vendorRotationSummaryInput" rows="3" spellcheck="false">${escapeHtml(selectedVendorRotation.summary || "")}</textarea>
                </div>
                <div class="preset-field">
                  <label for="vendorRotationMinFloorInput">Min floor</label>
                  <input id="vendorRotationMinFloorInput" type="number" min="0" value="${Number(selectedVendorRotation.when?.minFloor || 0)}" />
                </div>
                <div class="preset-field">
                  <label for="vendorRotationMaxFloorInput">Max floor</label>
                  <input id="vendorRotationMaxFloorInput" type="number" min="0" value="${selectedVendorRotation.when?.maxFloor ?? ""}" placeholder="unset" />
                </div>
                <div class="preset-field">
                  <label for="vendorRotationBossesInput">Bosses defeated at least</label>
                  <input id="vendorRotationBossesInput" type="number" min="0" value="${Number(selectedVendorRotation.when?.bossesDefeatedAtLeast || 0)}" />
                </div>
                <div class="preset-field">
                  <label for="vendorRotationGoldCostInput">Rotation gold cost</label>
                  <input id="vendorRotationGoldCostInput" type="number" min="0" value="${Math.max(0, Number(selectedVendorRotation.cost?.gold || 0))}" />
                </div>
                <div class="preset-field">
                  <label>Rotation inventory fields</label>
                  <div class="preset-stack">
                    ${(selectedVendorRotation.inventory || []).map((entry, index) => `
                      <div class="preset-stack">
                        <div class="preset-toolbar">
                          <select data-vendor-rotation-item-id="${index}">
                            ${itemDefinitionOptionListHtml(vendorInventoryEntryItemId(entry))}
                          </select>
                          <label><input type="checkbox" data-vendor-rotation-generated="${index}" ${entry?.generated ? "checked" : ""} /> generated</label>
                          <button data-remove-vendor-rotation-item="${index}">삭제</button>
                        </div>
                        ${entry?.generated ? `
                          <div class="preset-toolbar">
                            <select data-vendor-rotation-rarity-id="${index}">
                              ${rarityDefinitionOptionListHtml(entry.rarityId || activeRarityDefinitionId())}
                            </select>
                            <select data-vendor-rotation-affix-pool-id="${index}">
                              ${affixPoolOptionListHtml(entry.affixPoolId || activeAffixPoolId())}
                            </select>
                          </div>
                        ` : ""}
                      </div>
                    `).join("") || `<div class="muted">rotation inventory 없음</div>`}
                    <button id="addVendorRotationItemBtn">rotation item 추가</button>
                  </div>
                </div>
              ` : `<div class="muted">rotation 없음. 버튼으로 첫 rotation을 추가한다.</div>`}
              <div class="muted">base inventory ${vendorInventorySummary(vendorDef.inventory || []) || "없음"}${selectedVendorRotation ? ` · rotation inventory ${vendorInventorySummary(selectedVendorRotation.inventory || []) || "없음"}` : ""}</div>
            </div>
          ` : `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addSellBundleVendorBtn">sell_bundle 추가</button>
                <button id="addTrainerVendorBtn">train_party 추가</button>
              </div>
              <div class="muted">vendor definition이 비어 있다. 버튼으로 첫 vendor를 추가한다.</div>
            </div>
          `,
    });

    const lootPanelMarkup = renderEditorLootTablePanel({
      subtitle: lootTableDef ? `${lootTableId} · rolls ${lootTableDef.rolls ?? 1}` : "loot table 없음",
      bodyMarkup: lootTableDef ? `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addLootTableBtn">loot table 추가</button>
                <button id="duplicateLootTableBtn">선택 table 복제</button>
                <button id="removeLootTableBtn">선택 table 삭제</button>
              </div>
              <div class="preset-field">
                <label for="lootTableDefinitionSelect">Selected loot table</label>
                <select id="lootTableDefinitionSelect">${lootTableDefinitionIds().map((id) => `<option value="${id}" ${id === lootTableId ? "selected" : ""}>${id}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="lootTableRollsInput">Rolls</label>
                <input id="lootTableRollsInput" type="number" min="0" value="${Math.max(0, Number(lootTableDef.rolls || 0))}" />
              </div>
              <div class="preset-field">
                <label for="lootTablesJsonInput">Loot Tables JSON</label>
                <textarea id="lootTablesJsonInput" rows="8" spellcheck="false">${escapeHtml(lootTablesJson())}</textarea>
              </div>
              <div class="preset-field">
                <label>Guaranteed fields</label>
                <div class="preset-stack">
                  ${(lootTableDef.guaranteed || []).map((entry, index) => `
                    <div class="preset-stack">
                      <div class="preset-toolbar">
                        <select data-loot-guaranteed-item-id="${index}">
                          ${itemDefinitionOptionListHtml(entry?.itemId || "")}
                        </select>
                        <input data-loot-guaranteed-quantity="${index}" type="number" min="1" value="${Math.max(1, Number(entry?.quantity || 1))}" />
                        <label><input type="checkbox" data-loot-guaranteed-generated="${index}" ${entry?.generated ? "checked" : ""} /> generated</label>
                        <button data-remove-loot-guaranteed="${index}">삭제</button>
                      </div>
                      ${entry?.generated ? `
                        <div class="preset-toolbar">
                          <select data-loot-guaranteed-rarity-id="${index}">
                            ${rarityDefinitionOptionListHtml(entry.rarityId || activeRarityDefinitionId())}
                          </select>
                          <select data-loot-guaranteed-affix-pool-id="${index}">
                            ${affixPoolOptionListHtml(entry.affixPoolId || activeAffixPoolId())}
                          </select>
                        </div>
                      ` : ""}
                    </div>
                  `).join("") || `<div class="muted">guaranteed 없음</div>`}
                  <button id="addLootGuaranteedBtn">guaranteed 추가</button>
                </div>
              </div>
              <div class="preset-toolbar">
                <button id="addLootTierBtn">tier 추가</button>
                ${selectedLootTier ? `<button id="removeLootTierBtn">선택 tier 삭제</button>` : ""}
              </div>
              ${selectedLootTier ? `
                <div class="preset-field">
                  <label for="lootTierSelect">Selected tier</label>
                  <select id="lootTierSelect">${(lootTableDef.tierEntries || []).map((tier, index) => `<option value="${index}" ${index === selectedLootTierState.index ? "selected" : ""}>${index} · weight ${Number(tier.weight || 0)}</option>`).join("")}</select>
                </div>
                <div class="preset-field">
                  <label for="lootTierWeightInput">Tier weight</label>
                  <input id="lootTierWeightInput" type="number" min="0" value="${Math.max(0, Number(selectedLootTier.weight || 0))}" />
                </div>
                <div class="preset-field">
                  <label for="lootTierMinFloorInput">Tier min floor</label>
                  <input id="lootTierMinFloorInput" type="number" min="0" value="${selectedLootTier.when?.floorAtLeast ?? ""}" placeholder="unset" />
                </div>
                <div class="preset-field">
                  <label>Tier entries</label>
                  <div class="preset-stack">
                    ${(selectedLootTier.entries || []).map((entry, index) => `
                      <div class="preset-stack">
                        <div class="preset-toolbar">
                          <select data-loot-tier-item-id="${index}">
                            ${itemDefinitionOptionListHtml(entry?.itemId || "")}
                          </select>
                          <input data-loot-tier-item-quantity="${index}" type="number" min="1" value="${Math.max(1, Number(entry?.quantity || 1))}" />
                          <input data-loot-tier-item-weight="${index}" type="number" min="0" value="${Math.max(0, Number(entry?.weight || 0))}" />
                          <label><input type="checkbox" data-loot-tier-generated="${index}" ${entry?.generated ? "checked" : ""} /> generated</label>
                          <button data-remove-loot-tier-item="${index}">삭제</button>
                        </div>
                        ${entry?.generated ? `
                          <div class="preset-toolbar">
                            <select data-loot-tier-rarity-id="${index}">
                              ${rarityDefinitionOptionListHtml(entry.rarityId || activeRarityDefinitionId())}
                            </select>
                            <select data-loot-tier-affix-pool-id="${index}">
                              ${affixPoolOptionListHtml(entry.affixPoolId || activeAffixPoolId())}
                            </select>
                          </div>
                        ` : ""}
                      </div>
                    `).join("") || `<div class="muted">tier entry 없음</div>`}
                    <button id="addLootTierItemBtn">tier item 추가</button>
                  </div>
                </div>
              ` : `<div class="muted">tier가 없다. 버튼으로 첫 tier를 추가한다.</div>`}
              <div class="preset-toolbar">
                <button id="addLootBonusBtn">bonus 추가</button>
                ${selectedLootBonus ? `<button id="removeLootBonusBtn">선택 bonus 삭제</button>` : ""}
              </div>
              ${selectedLootBonus ? `
                <div class="preset-field">
                  <label for="lootBonusSelect">Selected bonus</label>
                  <select id="lootBonusSelect">${(lootTableDef.bonusRolls || []).map((bonus, index) => `<option value="${index}" ${index === selectedLootBonusState.index ? "selected" : ""}>${index} · chance ${Number(bonus.chance || 0)}</option>`).join("")}</select>
                </div>
                <div class="preset-field">
                  <label for="lootBonusChanceInput">Bonus chance</label>
                  <input id="lootBonusChanceInput" type="number" min="0" max="1" step="0.05" value="${Math.max(0, Number(selectedLootBonus.chance || 0))}" />
                </div>
                <div class="preset-field">
                  <label>Bonus entries</label>
                  <div class="preset-stack">
                    ${(selectedLootBonus.entries || []).map((entry, index) => `
                      <div class="preset-stack">
                        <div class="preset-toolbar">
                          <select data-loot-bonus-item-id="${index}">
                            ${itemDefinitionOptionListHtml(entry?.itemId || "")}
                          </select>
                          <input data-loot-bonus-item-quantity="${index}" type="number" min="1" value="${Math.max(1, Number(entry?.quantity || 1))}" />
                          <input data-loot-bonus-item-weight="${index}" type="number" min="0" value="${Math.max(0, Number(entry?.weight || 0))}" />
                          <label><input type="checkbox" data-loot-bonus-generated="${index}" ${entry?.generated ? "checked" : ""} /> generated</label>
                          <button data-remove-loot-bonus-item="${index}">삭제</button>
                        </div>
                        ${entry?.generated ? `
                          <div class="preset-toolbar">
                            <select data-loot-bonus-rarity-id="${index}">
                              ${rarityDefinitionOptionListHtml(entry.rarityId || activeRarityDefinitionId())}
                            </select>
                            <select data-loot-bonus-affix-pool-id="${index}">
                              ${affixPoolOptionListHtml(entry.affixPoolId || activeAffixPoolId())}
                            </select>
                          </div>
                        ` : ""}
                      </div>
                    `).join("") || `<div class="muted">bonus entry 없음</div>`}
                    <button id="addLootBonusItemBtn">bonus item 추가</button>
                  </div>
                </div>
              ` : `<div class="muted">bonus가 없다. 버튼으로 첫 bonus를 추가한다.</div>`}
              <div class="preset-toolbar">
                <button id="addCombatRewardProfileBtn">combat reward profile 추가</button>
                ${selectedCombatRewardProfile ? `<button id="removeCombatRewardProfileBtn">선택 profile 삭제</button>` : ""}
              </div>
              ${selectedCombatRewardProfile ? `
                <div class="preset-field">
                  <label for="combatRewardProfileSelect">Selected reward profile</label>
                  <select id="combatRewardProfileSelect">${(lootTables.combatRewardProfiles?.default || []).map((profile, index) => `<option value="${index}" ${index === selectedCombatRewardProfileState.index ? "selected" : ""}>${index} · ${profile.tableId}</option>`).join("")}</select>
                </div>
                <div class="preset-field">
                  <label for="combatRewardProfileTableSelect">Profile table</label>
                  <select id="combatRewardProfileTableSelect">${lootTableDefinitionIds().map((id) => `<option value="${id}" ${id === selectedCombatRewardProfile.tableId ? "selected" : ""}>${id}</option>`).join("")}</select>
                </div>
                <div class="preset-field">
                  <label for="combatRewardProfileMinXpInput">Profile min XP</label>
                  <input id="combatRewardProfileMinXpInput" type="number" min="0" value="${Number(selectedCombatRewardProfile.when?.minXp || 0)}" />
                </div>
                <div class="preset-field">
                  <label for="combatRewardProfileBossSelect">Boss only</label>
                  <select id="combatRewardProfileBossSelect">
                    <option value="" ${selectedCombatRewardProfile.when?.boss ? "" : "selected"}>(unset)</option>
                    <option value="true" ${selectedCombatRewardProfile.when?.boss ? "selected" : ""}>true</option>
                  </select>
                </div>
              ` : `<div class="muted">combat reward profile이 없다. 버튼으로 첫 profile을 추가한다.</div>`}
            </div>
          ` : `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addLootTableBtn">loot table 추가</button>
              </div>
              <div class="muted">loot table이 비어 있다. 버튼으로 첫 table을 추가한다.</div>
            </div>
          `,
    });

    const affixPanelMarkup = renderEditorAffixRarityPanel({
      subtitle: `${rarityDef ? `${rarityDefId} · ${rarityDef.label}` : "rarity 없음"}${affixDef ? ` · ${affixDefId}` : ""}`,
      bodyMarkup: `
            <div class="preset-inspector">
              <div class="preset-toolbar">
                <button id="addRarityDefinitionBtn">rarity 추가</button>
                <button id="duplicateRarityDefinitionBtn" ${rarityDef ? "" : "disabled"}>rarity 복제</button>
                <button id="removeRarityDefinitionBtn" ${rarityDef ? "" : "disabled"}>rarity 삭제</button>
              </div>
              ${rarityDef ? `
                <div class="preset-field">
                  <label for="rarityDefinitionSelect">Selected rarity</label>
                  <select id="rarityDefinitionSelect">${Object.entries(rarityDefinitions).map(([id, entry]) => `<option value="${id}" ${id === rarityDefId ? "selected" : ""}>${id} · ${escapeHtml(entry.label || id)}</option>`).join("")}</select>
                </div>
                <div class="preset-field">
                  <label for="rarityDefinitionLabelInput">Rarity label</label>
                  <input id="rarityDefinitionLabelInput" value="${escapeHtml(rarityDef.label || "")}" />
                </div>
                <div class="preset-field">
                  <label for="rarityDefinitionWeightInput">Rarity weight</label>
                  <input id="rarityDefinitionWeightInput" type="number" min="0" value="${Math.max(0, Number(rarityDef.weight || 0))}" />
                </div>
                <div class="preset-field">
                  <label for="rarityDefinitionValueMultiplierInput">Value multiplier</label>
                  <input id="rarityDefinitionValueMultiplierInput" type="number" min="0" step="0.1" value="${Math.max(0, Number(rarityDef.valueMultiplier || 0))}" />
                </div>
                <div class="preset-field">
                  <label for="rarityDefinitionAffixCountInput">Affix count</label>
                  <input id="rarityDefinitionAffixCountInput" type="number" min="0" value="${Math.max(0, Number(rarityDef.affixCount || 0))}" />
                </div>
              ` : `<div class="muted">rarity definition이 없다.</div>`}
              <div class="preset-field">
                <label for="rarityDefinitionsJsonInput">Rarity JSON</label>
                <textarea id="rarityDefinitionsJsonInput" rows="6" spellcheck="false">${escapeHtml(rarityDefinitionsJson())}</textarea>
              </div>
              <div class="preset-toolbar">
                <button id="addPrefixAffixBtn">prefix affix 추가</button>
                <button id="addSuffixAffixBtn">suffix affix 추가</button>
                <button id="duplicateAffixDefinitionBtn" ${affixDef ? "" : "disabled"}>affix 복제</button>
                <button id="removeAffixDefinitionBtn" ${affixDef ? "" : "disabled"}>affix 삭제</button>
              </div>
              ${affixDef ? `
                <div class="preset-field">
                  <label for="affixDefinitionSelect">Selected affix</label>
                  <select id="affixDefinitionSelect">${Object.entries(affixDefinitions).map(([id, entry]) => `<option value="${id}" ${id === affixDefId ? "selected" : ""}>${id} · ${escapeHtml(entry.label || id)}</option>`).join("")}</select>
                </div>
                <div class="preset-field">
                  <label for="affixDefinitionLabelInput">Affix label</label>
                  <input id="affixDefinitionLabelInput" value="${escapeHtml(affixDef.label || "")}" />
                </div>
                <div class="preset-field">
                  <label for="affixDefinitionSlotSelect">Affix slot</label>
                  <select id="affixDefinitionSlotSelect">
                    ${["prefix", "suffix"].map((slot) => `<option value="${slot}" ${affixDef.slot === slot ? "selected" : ""}>${slot}</option>`).join("")}
                  </select>
                </div>
                <div class="preset-field">
                  <label for="affixDefinitionStatSelect">Target stat</label>
                  <select id="affixDefinitionStatSelect">
                    ${["attack", "defense", "heal", "cure", "gold"].map((stat) => `<option value="${stat}" ${affixDef.stat === stat ? "selected" : ""}>${stat}</option>`).join("")}
                  </select>
                </div>
                <div class="preset-field">
                  <label for="affixDefinitionAmountInput">Affix amount</label>
                  <input id="affixDefinitionAmountInput" type="number" step="1" value="${Number(affixDef.amount || 0)}" />
                </div>
                <div class="preset-field">
                  <label for="affixDefinitionValueInput">Affix value text</label>
                  <input id="affixDefinitionValueInput" value="${escapeHtml(affixDef.value || "")}" placeholder="cure 같은 text stat용" />
                </div>
                <div class="preset-field">
                  <label for="affixDefinitionRaritySelect">Affix rarity</label>
                  <select id="affixDefinitionRaritySelect">${Object.entries(rarityDefinitions).map(([id, entry]) => `<option value="${id}" ${affixDef.rarity === id ? "selected" : ""}>${id} · ${escapeHtml(entry.label || id)}</option>`).join("")}</select>
                </div>
              ` : `<div class="muted">affix definition이 없다.</div>`}
              <div class="preset-field">
                <label for="affixDefinitionsJsonInput">Affix JSON</label>
                <textarea id="affixDefinitionsJsonInput" rows="6" spellcheck="false">${escapeHtml(affixDefinitionsJson())}</textarea>
              </div>
              <div class="preset-toolbar">
                <button id="addAffixPoolBtn">affix pool 추가</button>
                <button id="duplicateAffixPoolBtn" ${affixPoolDef ? "" : "disabled"}>pool 복제</button>
                <button id="removeAffixPoolBtn" ${affixPoolDef ? "" : "disabled"}>pool 삭제</button>
              </div>
              ${affixPoolDef ? `
                <div class="preset-field">
                  <label for="affixPoolDefinitionSelect">Selected affix pool</label>
                  <select id="affixPoolDefinitionSelect">${Object.entries(affixPoolDefinitions).map(([id, entry]) => `<option value="${id}" ${id === affixPoolId ? "selected" : ""}>${id} · ${escapeHtml(entry.label || id)}</option>`).join("")}</select>
                </div>
                <div class="preset-field">
                  <label for="affixPoolLabelInput">Pool label</label>
                  <input id="affixPoolLabelInput" value="${escapeHtml(affixPoolDef.label || "")}" />
                </div>
                <div class="preset-field">
                  <label for="affixPoolItemKindsInput">Item kinds</label>
                  <input id="affixPoolItemKindsInput" value="${escapeHtml((affixPoolDef.itemKinds || []).join(", "))}" placeholder="equipment, consumable" />
                </div>
                <div class="preset-field">
                  <label>Pool affix fields</label>
                  <div class="preset-stack">
                    ${(affixPoolDef.affixIds || []).map((affixId, index) => `
                      <div class="preset-toolbar">
                        <select data-affix-pool-affix-id="${index}">
                          ${Object.entries(affixDefinitions).map(([id, entry]) => `<option value="${id}" ${id === affixId ? "selected" : ""}>${id} · ${escapeHtml(entry.label || id)}</option>`).join("")}
                        </select>
                        <button data-remove-affix-pool-affix="${index}">삭제</button>
                      </div>
                    `).join("") || `<div class="muted">pool affix 없음</div>`}
                    <button id="addAffixPoolAffixBtn">pool affix 추가</button>
                  </div>
                </div>
              ` : `<div class="muted">affix pool definition이 없다.</div>`}
              <div class="preset-field">
                <label for="affixPoolDefinitionsJsonInput">Affix Pool JSON</label>
                <textarea id="affixPoolDefinitionsJsonInput" rows="6" spellcheck="false">${escapeHtml(affixPoolDefinitionsJson())}</textarea>
              </div>
            </div>
          `,
    });

    const sampleItemPanelMarkup = renderEditorSampleItemPanel({
      subtitle: sampleItemPreview ? `${escapeHtml(sampleItemPreview.name || sampleItemPreview.baseItemId)} · ${escapeHtml(sampleItemPreview.rarityLabel || sampleItemPreview.rarityId)}` : "preview 없음",
      bodyMarkup: `
            <div class="preset-inspector">
              <div class="preset-field">
                <label for="sampleItemBaseSelect">Sample base item</label>
                <select id="sampleItemBaseSelect">${Object.entries(items).map(([id, item]) => `<option value="${id}" ${id === itemDefId ? "selected" : ""}>${id} · ${escapeHtml(item.name || id)}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="sampleItemRaritySelect">Sample rarity</label>
                <select id="sampleItemRaritySelect">${Object.entries(rarityDefinitions).map(([id, rarity]) => `<option value="${id}" ${id === rarityDefId ? "selected" : ""}>${id} · ${escapeHtml(rarity.label || id)}</option>`).join("")}</select>
              </div>
              <div class="preset-field">
                <label for="sampleItemAffixPoolSelect">Sample affix pool</label>
                <select id="sampleItemAffixPoolSelect">${Object.entries(affixPoolDefinitions).map(([id, pool]) => `<option value="${id}" ${id === affixPoolId ? "selected" : ""}>${id} · ${escapeHtml(pool.label || id)}</option>`).join("")}</select>
              </div>
              <div class="preset-toolbar">
                <button id="generateSampleItemBtn">샘플 생성</button>
                <button id="pushSampleItemToInventoryBtn" ${sampleItemPreview ? "" : "disabled"}>가방에 추가</button>
                <button id="clearSampleItemBtn" ${sampleItemPreview ? "" : "disabled"}>샘플 비우기</button>
              </div>
              ${sampleItemPreview ? `
                <div class="muted">${escapeHtml(sampleItemPreview.name)} · ${escapeHtml(sampleItemPreview.rarityLabel || sampleItemPreview.rarityId)} · affix ${sampleItemPreview.affixes.length}개</div>
                <div class="muted">ATK ${Number(sampleItemPreview.stats?.attack || 0)} · DEF ${Number(sampleItemPreview.stats?.defense || 0)} · HEAL ${Number(sampleItemPreview.stats?.heal || 0)}${sampleItemPreview.stats?.cure ? ` · CURE ${escapeHtml(sampleItemPreview.stats.cure)}` : ""} · VALUE ${Number(sampleItemPreview.valueEstimate || 0)}</div>
                <div class="muted">${(sampleItemPreview.affixes || []).map((entry) => `${escapeHtml(entry.slot || "?")}: ${escapeHtml(entry.label || entry.id)}`).join("<br />") || "affix 없음"}</div>
                <div class="preset-field">
                  <label for="sampleItemPreviewJsonInput">Sample preview JSON</label>
                  <textarea id="sampleItemPreviewJsonInput" rows="10" spellcheck="false" readonly>${escapeHtml(sampleItemPreviewJson(sampleItemPreview))}</textarea>
                </div>
              ` : `<div class="muted">현재 선택된 item/rarity/pool 조합으로 샘플 generated item preview를 만들 수 있다.</div>`}
            </div>
          `,
    });

    return {
      itemPanelMarkup,
      questPanelMarkup,
      vendorPanelMarkup,
      lootPanelMarkup,
      affixPanelMarkup,
      sampleItemPanelMarkup,
      monsterPanelMarkup,
      skillPanelMarkup,
    };
  };
}
