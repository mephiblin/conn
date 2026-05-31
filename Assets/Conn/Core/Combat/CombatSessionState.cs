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
        public string EncounterPattern = string.Empty;
        public string EncounterRewardId = string.Empty;
        public string MonsterId = string.Empty;
        public string EnemySpecies = string.Empty;
        public string EnemyActionName = string.Empty;
        public int EnemyAttackPower;
        public int XpReward;
        public CombatantState Player = new CombatantState();
        public CombatantState Enemy = new CombatantState();
        public List<EncounterEnemySlotState> EnemySlots = new List<EncounterEnemySlotState>();
        public List<DiceFaceState> DiceFaces = new List<DiceFaceState>();
        public List<DiceResultCooldownState> DiceResultCooldowns = new List<DiceResultCooldownState>();
        public string LastMessage = string.Empty;
        public bool ReelSpinActive;
        public int ReelStopCount;

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
            EncounterPattern = string.Empty;
            EncounterRewardId = string.Empty;
            MonsterId = string.Empty;
            EnemySpecies = string.Empty;
            EnemyActionName = string.Empty;
            EnemyAttackPower = 0;
            XpReward = 0;
            Player.Setup(string.Empty, string.Empty, 0);
            Enemy.Setup(string.Empty, string.Empty, 0);
            Player.ClearStatusEffects();
            Enemy.ClearStatusEffects();
            EnemySlots.Clear();
            DiceFaces.Clear();
            DiceResultCooldowns.Clear();
            LastMessage = string.Empty;
            ReelSpinActive = false;
            ReelStopCount = 0;
        }
    }

    [System.Serializable]
    public sealed class EncounterEnemySlotState
    {
        public string SlotId = string.Empty;
        public string MonsterId = string.Empty;
        public string DisplayName = string.Empty;
        public int Count;
        public bool Primary;

        public string Describe()
        {
            var name = string.IsNullOrWhiteSpace(DisplayName) ? MonsterId : DisplayName;
            var role = Primary ? "primary" : "support";
            return $"{SlotId}: {name} x{Count} ({role})";
        }
    }

    [System.Serializable]
    public sealed class DiceResultCooldownState
    {
        public string ResultKey = string.Empty;
        public int RemainingTurns;
    }
}
