using Conn.Core.Combat;
using Conn.Core.Equipment;
using Conn.Core.Inventory;
using Conn.Core.Skills;
using Conn.Core.World;

namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class GameSessionState
    {
        public GameMode Mode = GameMode.Title;
        public int Gold;
        public PlayerRuntimeState Player = new PlayerRuntimeState();
        public QuestRuntimeState Quest = new QuestRuntimeState();
        public PlayerPoseSnapshot PreEncounterSnapshot = new PlayerPoseSnapshot();
        public PlayerEquipmentState Equipment = new PlayerEquipmentState();
        public InventoryState Inventory = new InventoryState();
        public SkillInventoryState Skills = new SkillInventoryState();
        public CombatSessionState Combat = new CombatSessionState();
        public WorldRuntimeState World = new WorldRuntimeState();

        public void StartNewGame()
        {
            Mode = GameMode.Town;
            Gold = 25;
            Player.Reset();
            Quest.Clear();
            Quest.BoardOfferIndex = 0;
            Quest.BoardRerollCount = 0;
            PreEncounterSnapshot.Clear();
            Inventory.Clear();
            Inventory.AddItem(EquipmentCatalog.RustySwordId);
            Equipment.EquippedWeaponId = EquipmentCatalog.RustySwordId;
            Equipment.EquippedShieldId = string.Empty;
            Skills.Clear();
            Skills.AddSkill(SkillCatalog.SlashId);
            Skills.EquipFirstOpenFace(SkillCatalog.SlashId, Equipment.DiceCount);
            Combat.Clear();
            World.Clear();
        }
    }
}
