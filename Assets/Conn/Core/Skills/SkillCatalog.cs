namespace Conn.Core.Skills
{
    public static class SkillCatalog
    {
        public const string SlashId = "skill_slash";
        public const string GuardId = "skill_guard";
        public const string FocusStrikeId = "skill_focus_strike";

        private static readonly SkillDefinition[] Skills =
        {
            new SkillDefinition(SlashId, "Slash", 0, 1, 1),
            new SkillDefinition(GuardId, "Guard", 5, 2, 0),
            new SkillDefinition(FocusStrikeId, "Focus Strike", 8, 4, 2)
        };

        public static SkillDefinition Find(string skillId)
        {
            for (var i = 0; i < Skills.Length; i++)
            {
                if (Skills[i].SkillId == skillId)
                {
                    return Skills[i];
                }
            }

            return null;
        }
    }
}
