namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class CharacterCreationOptions
    {
        public string CharacterName = "Adventurer";
        public int SelectedPortraitIndex;
        public string SelectedPortraitId = "portrait_0";
        public int Strength = 5;
        public int Dexterity = 5;
        public int Vitality = 5;
        public int Energy = 5;
        public string StarterWeaponId = string.Empty;
    }
}
