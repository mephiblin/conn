using System.Collections.Generic;

namespace Conn.Core.Combat
{
    [System.Serializable]
    public sealed class CombatantState
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public int MaxHp;
        public int Hp;
        public List<CombatStatusEffectState> StatusEffects = new List<CombatStatusEffectState>();

        public bool IsDead => Hp <= 0;

        public void Setup(string id, string displayName, int maxHp)
        {
            Id = id;
            DisplayName = displayName;
            MaxHp = maxHp;
            Hp = maxHp;
            EnsureStatusEffects();
            StatusEffects.Clear();
        }

        public void Setup(string id, string displayName, int maxHp, int hp)
        {
            Id = id;
            DisplayName = displayName;
            MaxHp = maxHp;
            Hp = hp;
            EnsureStatusEffects();
            StatusEffects.Clear();
        }

        public void Damage(int amount)
        {
            Hp -= amount;
            if (Hp < 0)
            {
                Hp = 0;
            }
        }

        public void AddOrRefreshStatus(CombatStatusEffectKind kind, int duration, int tickDamage)
        {
            EnsureStatusEffects();
            for (var i = 0; i < StatusEffects.Count; i++)
            {
                var status = StatusEffects[i];
                if (status.Kind == kind)
                {
                    if (status.RemainingTurns < duration)
                    {
                        status.RemainingTurns = duration;
                    }

                    if (status.TickDamage < tickDamage)
                    {
                        status.TickDamage = tickDamage;
                    }

                    return;
                }
            }

            StatusEffects.Add(new CombatStatusEffectState
            {
                Kind = kind,
                RemainingTurns = duration,
                TickDamage = tickDamage
            });
        }

        public void ClearStatusEffects()
        {
            EnsureStatusEffects();
            StatusEffects.Clear();
        }

        private void EnsureStatusEffects()
        {
            if (StatusEffects == null)
            {
                StatusEffects = new List<CombatStatusEffectState>();
            }
        }
    }
}
