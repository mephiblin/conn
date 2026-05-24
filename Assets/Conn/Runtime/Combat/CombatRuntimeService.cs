using Conn.Core.Scenes;
using Conn.Core.Session;
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
            session.Combat.Player.Setup("player", "Player", 20);
            session.Combat.Enemy.Setup(session.Quest.TargetMonsterId, "Test Monster", 12);
            session.Combat.LastMessage = $"Combat started. Dice: {session.Combat.PlayerDiceCount}";
        }

        public static void PlayerAttack(GameSessionState session)
        {
            EnsureCombat(session);
            var damage = session.Combat.PlayerDiceCount + 1 + session.Skills.EquippedPower(session.Combat.PlayerDiceCount);
            session.Combat.Enemy.Damage(damage);
            session.Combat.LastMessage = $"Player deals {damage}.";

            if (session.Combat.Enemy.IsDead)
            {
                Win(session);
                return;
            }

            EnemyAttack(session);
            session.Combat.Round++;
        }

        public static void Die(GameSessionState session)
        {
            session.Combat.Clear();
            SceneFlowService.Load(GameSceneId.Ending);
        }

        private static void EnemyAttack(GameSessionState session)
        {
            var damage = 4 - session.Combat.PlayerDefenseBonus;
            if (damage < 1)
            {
                damage = 1;
            }

            session.Combat.Player.Damage(damage);
            session.Combat.LastMessage += $" Enemy deals {damage}.";
            if (session.Combat.Player.IsDead)
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
    }
}
