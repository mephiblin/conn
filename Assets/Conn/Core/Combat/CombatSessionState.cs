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
            Player.Setup(string.Empty, string.Empty, 0);
            Enemy.Setup(string.Empty, string.Empty, 0);
            DiceFaces.Clear();
            LastMessage = string.Empty;
        }
    }
}
