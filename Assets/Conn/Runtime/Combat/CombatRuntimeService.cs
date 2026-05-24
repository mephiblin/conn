using Conn.Core.Combat;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Core.Skills;
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
            var monster = MonsterCatalog.Find(monsterId);
            session.Skills.ResizeEquippedFaces(session.Combat.PlayerDiceCount);
            session.Combat.Player.Setup("player", "Player", session.Player.MaxHp, session.Player.Hp);
            session.Combat.EncounterId = encounter != null ? encounter.EncounterId : string.Empty;
            session.Combat.MonsterId = monster != null ? monster.MonsterId : monsterId;
            session.Combat.EnemyActionName = ResolveEnemyActionName(monster);
            session.Combat.EnemyAttackPower = monster != null && monster.EnemyActionPower > 0 ? monster.EnemyActionPower : 4;
            session.Combat.XpReward = ResolveXpReward(encounter, monster);
            session.Combat.Enemy.Setup(
                session.Combat.MonsterId,
                monster != null ? monster.DisplayName : "Unknown Monster",
                monster != null ? monster.MaxHp : 12);
            BuildDiceFaces(session);
            session.Combat.LastMessage = $"Combat started. Dice: {session.Combat.PlayerDiceCount}";
        }

        public static void ToggleDieSelection(GameSessionState session, int dieIndex)
        {
            EnsureCombat(session);
            if (dieIndex < 0 || dieIndex >= session.Combat.DiceFaces.Count)
            {
                return;
            }

            var face = session.Combat.DiceFaces[dieIndex];
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
            var selected = session.Combat.SelectedDiceCount;
            if (selected == 0)
            {
                session.Combat.LastMessage = "Select at least 1 die.";
                return;
            }

            var attack = 0;
            var guard = 0;
            var healing = 0;
            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                var face = session.Combat.DiceFaces[i];
                if (face.Selected)
                {
                    if (face.EffectKind == SkillEffectKind.Guard)
                    {
                        guard += face.Power;
                    }
                    else if (face.EffectKind == SkillEffectKind.Heal)
                    {
                        healing += face.Power;
                    }
                    else
                    {
                        attack += 1 + face.Power;
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
            session.Combat.LastMessage = $"Resolved {selected}: {attack} damage, {guard} guard, {healing} heal.";

            if (session.Combat.Enemy.IsDead)
            {
                Win(session);
                return;
            }

            EnemyAttack(session, guard);
            session.Combat.Round++;
            TickCooldowns(session);
        }

        public static void PlayerAttack(GameSessionState session)
        {
            ResolveSelectedDice(session);
        }

        public static void Die(GameSessionState session)
        {
            session.Combat.Clear();
            session.Mode = GameMode.Ending;
            RuntimeNoticeService.Set(session, "You died.");
            if (Application.isPlaying)
            {
                GameSession.Instance.SaveGame();
                SceneFlowService.Load(GameSceneId.Ending);
            }
        }

        public static void Flee(GameSessionState session)
        {
            var stateKey = session.Combat.FieldMonsterStateKey;
            if (!string.IsNullOrWhiteSpace(stateKey))
            {
                FieldMonsterRuntimeService.MarkIdle(session, stateKey);
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

        private static void Win(GameSessionState session)
        {
            var xpReward = session.Combat.XpReward;
            session.Combat.LastMessage = "Enemy defeated.";
            session.Combat.Active = false;
            if (xpReward > 0)
            {
                session.Player.GainXp(xpReward);
            }

            RuntimeNoticeService.Set(session, $"Enemy defeated. Gained {xpReward} XP.");
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
            var encounter = handoff != null ? EncounterCatalog.Find(handoff.EncounterId) : null;
            return encounter ?? EncounterCatalog.FindForMonster(ResolveMonsterId(session, handoff));
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

        private static string ResolveEnemyActionName(MonsterDefinition monster)
        {
            if (monster != null && !string.IsNullOrWhiteSpace(monster.EnemyActionName))
            {
                return monster.EnemyActionName;
            }

            return "Attack";
        }

        private static void BuildDiceFaces(GameSessionState session)
        {
            session.Combat.DiceFaces.Clear();
            for (var i = 0; i < session.Combat.PlayerDiceCount; i++)
            {
                var skillId = i < session.Skills.EquippedSkillIds.Count
                    ? session.Skills.EquippedSkillIds[i]
                    : string.Empty;
                var skill = SkillCatalog.Find(skillId);
                var displayName = skill != null ? skill.DisplayName : "Strike";
                session.Combat.DiceFaces.Add(new DiceFaceState
                {
                    Index = i,
                    SkillId = skillId,
                    DisplayName = displayName,
                    EffectKind = skill != null ? skill.EffectKind : SkillEffectKind.Attack,
                    Power = skill != null ? skill.Power : 0,
                    Selected = false,
                    Cooldown = 0
                });
            }
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
    }
}
