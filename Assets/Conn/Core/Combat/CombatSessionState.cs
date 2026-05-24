using System.Collections.Generic;

namespace Conn.Core.Combat
{
    [System.Serializable]
    public sealed class CombatSessionState
    {
        public bool Active;
        public int Round;
        public int PlayerDiceCount;
        public int PlayerDefenseBonus;
        public string FieldMonsterStateKey = string.Empty;
        public string EncounterId = string.Empty;
        public string MonsterId = string.Empty;
        public string EnemyActionName = string.Empty;
        public int EnemyAttackPower;
        public int XpReward;
        public CombatantState Player = new CombatantState();
        public CombatantState Enemy = new CombatantState();
        public List<DiceFaceState> DiceFaces = new List<DiceFaceState>();
        public string LastMessage = string.Empty;

        public int SelectedDiceCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < DiceFaces.Count; i++)
                {
                    if (DiceFaces[i].Selected)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Clear()
        {
            Active = false;
            Round = 0;
            PlayerDiceCount = 0;
            PlayerDefenseBonus = 0;
            FieldMonsterStateKey = string.Empty;
            EncounterId = string.Empty;
            MonsterId = string.Empty;
            EnemyActionName = string.Empty;
            EnemyAttackPower = 0;
            XpReward = 0;
            Player.Setup(string.Empty, string.Empty, 0);
            Enemy.Setup(string.Empty, string.Empty, 0);
            DiceFaces.Clear();
            LastMessage = string.Empty;
        }
    }
}
