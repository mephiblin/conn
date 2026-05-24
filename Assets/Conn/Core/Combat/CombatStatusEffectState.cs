namespace Conn.Core.Combat
{
    [System.Serializable]
    public sealed class CombatStatusEffectState
    {
        public CombatStatusEffectKind Kind;
        public int RemainingTurns;
        public int TickDamage;

        public string DisplayName => Kind.ToString();
    }
}
