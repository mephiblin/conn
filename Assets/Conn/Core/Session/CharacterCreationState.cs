namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class CharacterCreationState
    {
        public string CharacterName = "Adventurer";
        public int SelectedPortraitIndex;
        public string SelectedPortraitId = "portrait_0";
        public int Strength = 5;
        public int Dexterity = 5;
        public int Vitality = 5;
        public int Energy = 5;
        public string StarterWeaponId = string.Empty;
        public string StarterSkillId = string.Empty;

        public void ResetToDefaults(string starterWeaponId, string starterSkillId = "")
        {
            CharacterName = "Adventurer";
            SelectedPortraitIndex = 0;
            SelectedPortraitId = "portrait_0";
            Strength = 5;
            Dexterity = 5;
            Vitality = 5;
            Energy = 5;
            StarterWeaponId = starterWeaponId ?? string.Empty;
            StarterSkillId = starterSkillId ?? string.Empty;
        }

        public void Apply(CharacterCreationOptions options, string fallbackStarterWeaponId, string fallbackStarterSkillId = "")
        {
            if (options == null)
            {
                ResetToDefaults(fallbackStarterWeaponId, fallbackStarterSkillId);
                return;
            }

            CharacterName = string.IsNullOrWhiteSpace(options.CharacterName) ? "Adventurer" : options.CharacterName.Trim();
            SelectedPortraitIndex = options.SelectedPortraitIndex < 0 ? 0 : options.SelectedPortraitIndex;
            SelectedPortraitId = string.IsNullOrWhiteSpace(options.SelectedPortraitId)
                ? $"portrait_{SelectedPortraitIndex}"
                : options.SelectedPortraitId.Trim();
            Strength = options.Strength;
            Dexterity = options.Dexterity;
            Vitality = options.Vitality;
            Energy = options.Energy;
            StarterWeaponId = string.IsNullOrWhiteSpace(options.StarterWeaponId)
                ? fallbackStarterWeaponId ?? string.Empty
                : options.StarterWeaponId.Trim();
            StarterSkillId = string.IsNullOrWhiteSpace(options.StarterSkillId)
                ? fallbackStarterSkillId ?? string.Empty
                : options.StarterSkillId.Trim();
        }
    }
}
