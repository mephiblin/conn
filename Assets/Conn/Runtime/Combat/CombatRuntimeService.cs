using Conn.Core.Combat;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;

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
            session.Skills.ResizeEquippedFaces(session.Combat.PlayerDiceCount);
            session.Combat.Player.Setup("player", "Player", session.Player.MaxHp, session.Player.Hp);
            session.Combat.Enemy.Setup(session.Quest.TargetMonsterId, "Test Monster", 12);
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

            var damage = 0;
            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                var face = session.Combat.DiceFaces[i];
                if (face.Selected)
                {
                    damage += 1 + face.Power;
                    face.Selected = false;
                    face.Cooldown = 2;
                }
            }

            session.Combat.Enemy.Damage(damage);
            session.Combat.LastMessage = $"Resolved {selected} dice for {damage} damage.";

            if (session.Combat.Enemy.IsDead)
            {
                Win(session);
                return;
            }

            EnemyAttack(session);
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
            SceneFlowService.Load(GameSceneId.Ending);
            GameSession.Instance.SaveGame();
        }

        private static void EnemyAttack(GameSessionState session)
        {
            var damage = 4 - session.Combat.PlayerDefenseBonus;
            if (damage < 1)
            {
                damage = 1;
            }

            session.Combat.Player.Damage(damage);
            session.Player.Damage(damage);
            session.Combat.LastMessage += $" Enemy deals {damage}.";
            GameSession.Instance.SaveGame();
            if (session.Player.IsDead)
            {
                Die(session);
            }
        }

        private static void Win(GameSessionState session)
        {
            session.Combat.LastMessage = "Enemy defeated.";
            session.Combat.Active = false;
            QuestRuntimeService.CompleteTarget(session);
            SceneFlowService.Load(GameSceneId.Dungeon);
        }

        private static void EnsureCombat(GameSessionState session)
        {
            if (!session.Combat.Active)
            {
                StartTestCombat(session);
            }
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
                session.Combat.DiceFaces.Add(new DiceFaceState
                {
                    Index = i,
                    SkillId = skillId,
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
