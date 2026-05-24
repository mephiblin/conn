using Conn.Core.Combat;
using Conn.Core.Equipment;
using Conn.Core.Inventory;

namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class GameSessionState
    {
        public GameMode Mode = GameMode.Title;
        public int Gold;
        public QuestRuntimeState Quest = new QuestRuntimeState();
        public PlayerPoseSnapshot PreEncounterSnapshot = new PlayerPoseSnapshot();
        public PlayerEquipmentState Equipment = new PlayerEquipmentState();
        public InventoryState Inventory = new InventoryState();
        public CombatSessionState Combat = new CombatSessionState();

        public void StartNewGame()
        {
            Mode = GameMode.Town;
            Gold = 25;
            Quest.Clear();
            PreEncounterSnapshot.Clear();
            Inventory.Clear();
            Inventory.AddItem(EquipmentCatalog.RustySwordId);
            Equipment.EquippedWeaponId = EquipmentCatalog.RustySwordId;
            Equipment.EquippedShieldId = string.Empty;
            Combat.Clear();
        }
    }
}
