using System.Text;
using Conn.Core.Combat;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Content;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;
using Conn.Runtime.World;
using UnityEngine;

namespace Conn.Runtime.Combat
{
    public static class CombatRuntimeService
    {
        public static void StartTestCombat(GameSessionState session)
        {
            session.Combat.Active = true;
            session.Combat.Round = 1;
            session.Combat.PlayerDiceCount = session.Equipment.DiceCount;
            session.Combat.PlayerDefenseBonus = session.Equipment.DefenseBonus;
            var handoff = FieldMonsterRuntimeService.FindCombatHandoff(session);
            session.Combat.FieldMonsterStateKey = handoff != null ? handoff.StateKey : string.Empty;
            var encounter = ResolveEncounter(session, handoff);
            var monsterId = encounter != null ? encounter.MonsterId : ResolveMonsterId(session, handoff);
            var monster = RuntimeContentDatabase.FindMonster(monsterId);
            session.Skills.ResizeEquippedFaces(session.Combat.PlayerDiceCount);
            session.Combat.Player.Setup("player", "Player", session.Player.MaxHp, session.Player.Hp);
            session.Combat.EncounterId = encounter != null ? encounter.EncounterId : string.Empty;
            session.Combat.EncounterPattern = ResolveEncounterPattern(encounter);
            session.Combat.EncounterRewardId = encounter != null ? encounter.RewardId : string.Empty;
            session.Combat.MonsterId = monster != null ? monster.MonsterId : monsterId;
            session.Combat.EnemySpecies = monster != null ? monster.Species : string.Empty;
            session.Combat.EnemyActionName = ResolveEnemyActionName(monster);
            session.Combat.EnemyAttackPower = monster != null && monster.EnemyActionPower > 0 ? monster.EnemyActionPower : 4;
            session.Combat.XpReward = ResolveXpReward(encounter, monster);
            session.Combat.Enemy.Setup(
                session.Combat.MonsterId,
                monster != null ? monster.DisplayName : "Unknown Monster",
                monster != null ? monster.MaxHp : 12);
            BuildEnemySlots(session, encounter, monster);
            BuildDiceFaces(session);
            BeginReelSpin(session, $"전투 시작. 릴 {session.Combat.PlayerDiceCount}개가 동시에 회전한다.");
        }

        public static void ToggleDieSelection(GameSessionState session, int dieIndex)
        {
            EnsureCombat(session);
            if (dieIndex < 0 || dieIndex >= session.Combat.DiceFaces.Count)
            {
                return;
            }

            var face = session.Combat.DiceFaces[dieIndex];
            if (session.Combat.ReelSpinActive || !face.ReelStopped)
            {
                session.Combat.LastMessage = "릴 전체가 아직 회전 중이다. STOP으로 모든 결과를 확정한 뒤 선택한다.";
                return;
            }

            if (face.IsCoolingDown)
            {
                session.Combat.LastMessage = $"Die {dieIndex + 1} is cooling down.";
                return;
            }

            if (!face.Selected && session.Combat.SelectedDiceCount >= 3)
            {
                session.Combat.LastMessage = "Select up to 3 dice.";
                return;
            }

            face.Selected = !face.Selected;
        }

        public static void ResolveSelectedDice(GameSessionState session)
        {
            EnsureCombat(session);
            if (session.Combat.ReelSpinActive)
            {
                session.Combat.LastMessage = "릴 전체가 회전 중이다. STOP으로 모든 릴을 함께 멈춘 뒤 선택한다.";
                return;
            }

            var selected = session.Combat.SelectedDiceCount;
            if (selected == 0)
            {
                ResolveEmptySelectionTurn(session);
                return;
            }

            var attack = 0;
            var guard = 0;
            var healing = 0;
            var appliedBleed = false;
            var selectedFaces = new StringBuilder();
            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                var face = session.Combat.DiceFaces[i];
                if (face.Selected)
                {
                    if (selectedFaces.Length > 0)
                    {
                        selectedFaces.Append(", ");
                    }

                    var adjustedPower = AdjustPowerForEnemySpecies(face, session.Combat.EnemySpecies);
                    var resolvedAmount = face.RolledValue + adjustedPower;
                    selectedFaces.Append($"Die {face.Index + 1} {face.RolledValue} {face.DisplayName} ({face.EffectKind} +{face.Power}");
                    if (adjustedPower != face.Power)
                    {
                        selectedFaces.Append($" -> +{adjustedPower} vs {session.Combat.EnemySpecies}");
                    }

                    selectedFaces.Append(')');
                    if (face.EffectKind == SkillEffectKind.Guard)
                    {
                        guard += resolvedAmount;
                    }
                    else if (face.EffectKind == SkillEffectKind.Heal)
                    {
                        healing += resolvedAmount;
                    }
                    else if (face.EffectKind == SkillEffectKind.Guard || face.EffectKind == SkillEffectKind.Support || face.EffectKind == SkillEffectKind.Buff)
                    {
                        guard += resolvedAmount;
                    }
                    else if (face.EffectKind == SkillEffectKind.Debuff)
                    {
                        attack += resolvedAmount;
                        appliedBleed = adjustedPower > 0;
                    }
                    else if (face.EffectKind == SkillEffectKind.Lifesteal)
                    {
                        attack += resolvedAmount;
                        healing += resolvedAmount > 0 ? resolvedAmount : 1;
                    }
                    else
                    {
                        attack += resolvedAmount;
                        if (HasSpecialEffect(face, "bleed"))
                        {
                            attack += 1;
                            appliedBleed = true;
                        }
                    }

                    face.Selected = false;
                    face.Cooldown = 2;
                }
            }

            if (healing > 0)
            {
                session.Player.Heal(healing);
                session.Combat.Player.Hp = session.Player.Hp;
            }

            session.Combat.Enemy.Damage(attack);
            session.Combat.LastMessage = $"Resolved {selected} face(s): {selectedFaces}. Result: {attack} damage, {guard} guard, {healing} heal.";
            if (appliedBleed)
            {
                session.Combat.Enemy.AddOrRefreshStatus(CombatStatusEffectKind.Bleed, 2, 1);
                session.Combat.LastMessage += " Focus Strike effect: applied Bleed (1 damage for 2 turns).";
            }

            if (session.Combat.Enemy.IsDead)
            {
                Win(session);
                return;
            }

            EnemyAttack(session, guard);
            if (!session.Combat.Active)
            {
                return;
            }

            TickStatuses(session);
            if (!session.Combat.Active)
            {
                return;
            }

            ApplyEnemySpeciesTurnRegen(session);
            if (!session.Combat.Active)
            {
                return;
            }

            session.Combat.Round++;
            TickCooldowns(session);
            BeginReelSpin(session, "다음 라운드 시작. 릴 전체가 다시 회전한다.");
        }

        public static void ResolveEmptySelectionTurn(GameSessionState session)
        {
            EnsureCombat(session);
            if (session.Combat.ReelSpinActive)
            {
                session.Combat.LastMessage = "릴 전체가 회전 중이다. STOP으로 결과를 확정한 뒤에만 턴을 넘길 수 있다.";
                return;
            }

            ClearDiceSelection(session);
            session.Combat.LastMessage = "No dice selected. Advanced turn.";
            EnemyAttack(session, 0);
            if (!session.Combat.Active)
            {
                return;
            }

            TickStatuses(session);
            if (!session.Combat.Active)
            {
                return;
            }

            session.Combat.Round++;
            TickCooldowns(session);
            BeginReelSpin(session, "선택 없이 턴이 넘어갔다. 다음 라운드에서 릴 전체가 다시 회전한다.");
        }

        public static void PlayerAttack(GameSessionState session)
        {
            ResolveSelectedDice(session);
        }

        public static void Die(GameSessionState session)
        {
            session.Combat.Clear();
            session.Mode = GameMode.Ending;
            RuntimeNoticeService.Set(session, "Defeat: You died.");
            if (Application.isPlaying)
            {
                GameSession.Instance.SaveGame();
                SceneFlowService.Load(GameSceneId.Ending);
            }
        }

        public static string DescribeDiceFace(DiceFaceState face)
        {
            if (face == null)
            {
                return "Die: missing";
            }

            var state = face.IsCoolingDown
                ? $"cooldown {face.Cooldown}"
                : face.Selected ? "selected" : "ready";
            var special = HasSpecialEffect(face, "bleed") ? " / effect Bleed" : string.Empty;
            return $"Die {face.Index + 1}: {face.RolledValue} {face.DisplayName} / {face.EffectKind} +{face.Power} / {state}{special}";
        }

        public static bool CanStopReels(GameSessionState session)
        {
            return session != null
                && session.Combat != null
                && session.Combat.Active
                && session.Combat.ReelSpinActive;
        }

        public static void StopReels(GameSessionState session)
        {
            EnsureCombat(session);
            if (!CanStopReels(session))
            {
                session.Combat.LastMessage = "멈출 릴이 없다.";
                return;
            }

            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                var face = session.Combat.DiceFaces[i];
                ApplyStoppedRoll(session, face);
            }

            session.Combat.ReelSpinActive = false;
            session.Combat.ReelStopCount = session.Combat.DiceFaces.Count;
            session.Combat.LastMessage = $"모든 릴이 동시에 멈췄다. {session.Combat.SelectedDiceCount}/3 선택 후 Attack을 실행한다.";
        }

        public static string DescribeCombatantStatuses(CombatantState combatant)
        {
            if (combatant == null || combatant.StatusEffects == null || combatant.StatusEffects.Count == 0)
            {
                return "Status: none";
            }

            var builder = new StringBuilder("Status: ");
            for (var i = 0; i < combatant.StatusEffects.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                var status = combatant.StatusEffects[i];
                builder.Append($"{status.DisplayName} {status.RemainingTurns} turn(s), {status.TickDamage} damage");
            }

            return builder.ToString();
        }

        public static string DescribeEnemySlots(CombatSessionState combat)
        {
            if (combat == null || combat.EnemySlots == null || combat.EnemySlots.Count == 0)
            {
                return "Enemy slots: single primary";
            }

            var builder = new StringBuilder("Enemy slots: ");
            for (var i = 0; i < combat.EnemySlots.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(combat.EnemySlots[i].Describe());
            }

            return builder.ToString();
        }

        public static void Flee(GameSessionState session)
        {
            var stateKey = session.Combat.FieldMonsterStateKey;
            if (!string.IsNullOrWhiteSpace(stateKey))
            {
                FieldMonsterRuntimeService.MarkReturnToAnchor(session, stateKey);
            }

            session.Combat.Clear();
            session.Mode = GameMode.Dungeon;
            if (Application.isPlaying)
            {
                GameSession.Instance.SaveGame();
                SceneFlowService.Load(GameSceneId.Dungeon);
            }
        }

        private static void EnemyAttack(GameSessionState session, int guard)
        {
            var attackPower = session.Combat.EnemyAttackPower > 0 ? session.Combat.EnemyAttackPower : 4;
            var damage = attackPower - session.Combat.PlayerDefenseBonus - guard;
            if (damage < 1)
            {
                damage = 1;
            }

            session.Combat.Player.Damage(damage);
            session.Player.Damage(damage);
            var blocked = attackPower - damage;
            if (blocked < 0)
            {
                blocked = 0;
            }

            var actionName = string.IsNullOrWhiteSpace(session.Combat.EnemyActionName)
                ? "Attack"
                : session.Combat.EnemyActionName;
            var enemyName = string.IsNullOrWhiteSpace(session.Combat.Enemy.DisplayName)
                ? "Enemy"
                : session.Combat.Enemy.DisplayName;
            session.Combat.LastMessage += $" {enemyName} uses {actionName} for {damage} damage ({attackPower} power, {blocked} blocked).";
            if (Application.isPlaying)
            {
                GameSession.Instance.SaveGame();
            }

            if (session.Player.IsDead)
            {
                Die(session);
            }
        }

        private static void TickStatuses(GameSessionState session)
        {
            TickStatusEffects(session, session.Combat.Enemy, null);
            if (session.Combat.Enemy.IsDead)
            {
                Win(session);
                return;
            }

            TickStatusEffects(session, session.Combat.Player, session.Player);
            if (session.Player.IsDead)
            {
                Die(session);
            }
        }

        private static void TickStatusEffects(GameSessionState session, CombatantState combatant, Conn.Core.Session.PlayerRuntimeState persistentPlayer)
        {
            if (combatant.StatusEffects == null)
            {
                return;
            }

            for (var i = combatant.StatusEffects.Count - 1; i >= 0; i--)
            {
                var status = combatant.StatusEffects[i];
                if (status.RemainingTurns <= 0)
                {
                    combatant.StatusEffects.RemoveAt(i);
                    continue;
                }

                var damage = status.TickDamage > 0 ? status.TickDamage : 0;
                if (damage > 0)
                {
                    combatant.Damage(damage);
                    if (persistentPlayer != null)
                    {
                        persistentPlayer.Damage(damage);
                    }

                    var targetName = string.IsNullOrWhiteSpace(combatant.DisplayName) ? "Combatant" : combatant.DisplayName;
                    session.Combat.LastMessage += $" {targetName} suffers {damage} {status.DisplayName} damage.";
                }

                status.RemainingTurns--;
                if (status.RemainingTurns <= 0)
                {
                    combatant.StatusEffects.RemoveAt(i);
                    session.Combat.LastMessage += $" {status.DisplayName} ended.";
                }
            }
        }

        private static void Win(GameSessionState session)
        {
            var xpReward = session.Combat.XpReward;
            session.Combat.LastMessage = string.IsNullOrWhiteSpace(session.Combat.LastMessage)
                ? $"Victory: Enemy defeated. Gained {xpReward} XP."
                : session.Combat.LastMessage + $" Victory: Enemy defeated. Gained {xpReward} XP.";
            session.Combat.Active = false;
            if (xpReward > 0)
            {
                session.Player.GainXp(xpReward);
            }

            RuntimeNoticeService.Set(session, $"Victory: Enemy defeated. Gained {xpReward} XP.");
            var stateKey = string.IsNullOrWhiteSpace(session.Combat.FieldMonsterStateKey)
                ? "field_monster_test_guard"
                : session.Combat.FieldMonsterStateKey;
            QuestRuntimeService.CompleteTarget(session, stateKey);
            if (Application.isPlaying)
            {
                SceneFlowService.Load(GameSceneId.Dungeon);
            }
        }

        private static void EnsureCombat(GameSessionState session)
        {
            if (!session.Combat.Active)
            {
                StartTestCombat(session);
            }
        }

        private static EncounterDefinition ResolveEncounter(GameSessionState session, Conn.Core.World.FieldMonsterState handoff)
        {
            var encounter = handoff != null ? RuntimeContentDatabase.FindEncounter(handoff.EncounterId) : null;
            if (encounter != null)
            {
                return encounter;
            }

            if (!string.IsNullOrWhiteSpace(session.Quest.TargetEncounterId))
            {
                encounter = RuntimeContentDatabase.FindEncounter(session.Quest.TargetEncounterId);
            }

            return encounter ?? RuntimeContentDatabase.FindEncounterForMonster(ResolveMonsterId(session, handoff));
        }

        private static string ResolveMonsterId(GameSessionState session, Conn.Core.World.FieldMonsterState handoff)
        {
            if (handoff != null && !string.IsNullOrWhiteSpace(handoff.MonsterId))
            {
                return handoff.MonsterId;
            }

            return string.IsNullOrWhiteSpace(session.Quest.TargetMonsterId)
                ? MonsterCatalog.TestGuardId
                : session.Quest.TargetMonsterId;
        }

        private static int ResolveXpReward(EncounterDefinition encounter, MonsterDefinition monster)
        {
            if (encounter != null && encounter.XpReward > 0)
            {
                return encounter.XpReward;
            }

            return monster != null ? monster.XpReward : 0;
        }

        private static string ResolveEncounterPattern(EncounterDefinition encounter)
        {
            if (encounter != null && !string.IsNullOrWhiteSpace(encounter.Pattern))
            {
                return encounter.Pattern;
            }

            return "single_primary";
        }

        private static string ResolveEnemyActionName(MonsterDefinition monster)
        {
            if (monster != null && !string.IsNullOrWhiteSpace(monster.EnemyActionName))
            {
                return monster.EnemyActionName;
            }

            return "Attack";
        }

        private static void BuildEnemySlots(GameSessionState session, EncounterDefinition encounter, MonsterDefinition primaryMonster)
        {
            session.Combat.EnemySlots.Clear();
            if (encounter != null && encounter.EnemySlots != null && encounter.EnemySlots.Length > 0)
            {
                for (var i = 0; i < encounter.EnemySlots.Length; i++)
                {
                    var slot = encounter.EnemySlots[i];
                    var slotMonster = RuntimeContentDatabase.FindMonster(slot.MonsterId);
                    session.Combat.EnemySlots.Add(new EncounterEnemySlotState
                    {
                        SlotId = string.IsNullOrWhiteSpace(slot.SlotId) ? $"slot_{i}" : slot.SlotId,
                        MonsterId = slot.MonsterId,
                        DisplayName = slotMonster != null ? slotMonster.DisplayName : slot.MonsterId,
                        Count = slot.Count <= 0 ? 1 : slot.Count,
                        Primary = slot.Primary || slot.MonsterId == session.Combat.MonsterId
                    });
                }

                return;
            }

            session.Combat.EnemySlots.Add(new EncounterEnemySlotState
            {
                SlotId = "primary",
                MonsterId = session.Combat.MonsterId,
                DisplayName = primaryMonster != null ? primaryMonster.DisplayName : session.Combat.Enemy.DisplayName,
                Count = 1,
                Primary = true
            });
        }

        private static void BuildDiceFaces(GameSessionState session)
        {
            session.Combat.DiceFaces.Clear();
            for (var i = 0; i < session.Combat.PlayerDiceCount; i++)
            {
                session.Combat.DiceFaces.Add(new DiceFaceState
                {
                    Index = i,
                    SkillId = string.Empty,
                    DisplayName = "릴 회전",
                    RolledValue = 1,
                    EffectKind = SkillEffectKind.Attack,
                    SpecialEffectId = string.Empty,
                    Power = 0,
                    Selected = false,
                    Cooldown = 0,
                    ReelStopped = false,
                    ReelSkillIds = BuildReelSkillPool(session, i),
                    ReelStopIndex = 0
                });
            }
        }

        private static string[] BuildReelSkillPool(GameSessionState session, int dieIndex)
        {
            var pool = new System.Collections.Generic.List<string>();
            var owned = session != null && session.Skills != null
                ? session.Skills.OwnedSkillIds
                : null;
            if (owned != null)
            {
                for (var i = 0; i < owned.Count; i++)
                {
                    var skillId = owned[i];
                    if (string.IsNullOrWhiteSpace(skillId) || pool.Contains(skillId))
                    {
                        continue;
                    }

                    pool.Add(skillId);
                }
            }

            if (pool.Count == 0)
            {
                var fallbackSkillId = dieIndex < session.Skills.EquippedSkillIds.Count
                    ? session.Skills.EquippedSkillIds[dieIndex]
                    : string.Empty;
                pool.Add(fallbackSkillId);
            }

            if (pool.Count == 0)
            {
                pool.Add(string.Empty);
            }

            return pool.ToArray();
        }

        private static void BeginReelSpin(GameSessionState session, string message)
        {
            ClearDiceSelection(session);
            session.Combat.ReelSpinActive = true;
            session.Combat.ReelStopCount = 0;
            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                var face = session.Combat.DiceFaces[i];
                face.ReelStopped = false;
                face.RolledValue = 1;
                face.SkillId = string.Empty;
                face.DisplayName = "릴 회전";
                face.EffectKind = SkillEffectKind.Attack;
                face.SpecialEffectId = string.Empty;
                face.Power = 0;
                face.ReelStopIndex = 0;
                face.ReelSkillIds = BuildReelSkillPool(session, i);
            }

            session.Combat.LastMessage = message;
        }

        private static void ApplyStoppedRoll(GameSessionState session, DiceFaceState face)
        {
            face.ReelStopped = true;
            face.Selected = false;
            face.RolledValue = Random.Range(1, 7);
            face.ReelStopIndex = face.ReelSkillIds != null && face.ReelSkillIds.Length > 0
                ? Random.Range(0, face.ReelSkillIds.Length)
                : 0;
            var skillId = face.ReelSkillIds != null && face.ReelSkillIds.Length > 0
                ? face.ReelSkillIds[face.ReelStopIndex]
                : string.Empty;
            var skill = RuntimeContentDatabase.FindSkill(skillId);
            face.SkillId = skillId;
            face.DisplayName = skill != null ? skill.DisplayName : "기본공격";
            face.EffectKind = skill != null ? skill.EffectKind : SkillEffectKind.Attack;
            face.SpecialEffectId = skill != null ? skill.SpecialEffectId : string.Empty;
            face.Power = skill != null ? skill.Power : 0;
        }

        private static bool HasSpecialEffect(DiceFaceState face, string effectId)
        {
            return face != null && string.Equals(face.SpecialEffectId, effectId, System.StringComparison.OrdinalIgnoreCase);
        }

        private static int AdjustPowerForEnemySpecies(DiceFaceState face, string enemySpecies)
        {
            if (face == null || string.IsNullOrWhiteSpace(face.SkillId) || string.IsNullOrWhiteSpace(enemySpecies))
            {
                return face != null ? face.Power : 0;
            }

            var skill = RuntimeContentDatabase.FindSkill(face.SkillId);
            return skill != null ? skill.AdjustPowerForSpecies(enemySpecies, face.Power) : face.Power;
        }

        private static void ApplyEnemySpeciesTurnRegen(GameSessionState session)
        {
            if (session?.Combat == null || !session.Combat.Active || session.Combat.Enemy.IsDead)
            {
                return;
            }

            var profile = RuntimeContentDatabase.FindMonsterSpeciesProfile(session.Combat.EnemySpecies);
            if (profile == null || profile.TurnRegenHp <= 0)
            {
                return;
            }

            session.Combat.Enemy.Heal(profile.TurnRegenHp);
            session.Combat.LastMessage += $" {session.Combat.Enemy.DisplayName} regenerates {profile.TurnRegenHp} HP.";
        }

        private static void TickCooldowns(GameSessionState session)
        {
            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                var face = session.Combat.DiceFaces[i];
                if (face.Cooldown > 0)
                {
                    face.Cooldown--;
                }
            }
        }

        private static void ClearDiceSelection(GameSessionState session)
        {
            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                session.Combat.DiceFaces[i].Selected = false;
            }
        }
    }
}
