namespace Conn.Core.Combat
{
    [System.Serializable]
    public sealed class CombatSessionState
    {
        public bool Active;
        public int Round;
        public int PlayerDiceCount;
        public int PlayerDefenseBonus;
        public CombatantState Player = new CombatantState();
        public CombatantState Enemy = new CombatantState();
        public string LastMessage = string.Empty;

        public void Clear()
        {
            Active = false;
            Round = 0;
            PlayerDiceCount = 0;
            PlayerDefenseBonus = 0;
            Player.Setup(string.Empty, string.Empty, 0);
            Enemy.Setup(string.Empty, string.Empty, 0);
            LastMessage = string.Empty;
        }
    }
}
