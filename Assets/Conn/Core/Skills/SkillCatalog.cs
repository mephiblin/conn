namespace Conn.Core.Skills
{
    public static class SkillCatalog
    {
        public const string SlashId = "skill_slash";
        public const string GuardId = "skill_guard";
        public const string FocusStrikeId = "skill_focus_strike";
        public const string MendId = "skill_mend";

        private static readonly SkillDefinition[] Skills =
        {
            new SkillDefinition(SlashId, "Slash", SkillEffectKind.Attack, 0, 1, 1),
            new SkillDefinition(GuardId, "Guard", SkillEffectKind.Guard, 5, 2, 2),
            new SkillDefinition(FocusStrikeId, "Focus Strike", SkillEffectKind.Attack, 8, 4, 2, "bleed"),
            new SkillDefinition(MendId, "Mend", SkillEffectKind.Heal, 7, 3, 3)
        };

        public static SkillDefinition[] All => Skills;

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
