using Conn.Core.Combat;
using Conn.Core.Equipment;
using Conn.Core.Inventory;
using Conn.Core.Skills;
using Conn.Core.World;
using System;

namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class GameSessionState
    {
        public static Func<string> StarterEquipmentIdResolver = () => EquipmentCatalog.RustySwordId;
        public static Func<string> StarterSkillIdResolver = () => SkillCatalog.SlashId;

        public GameMode Mode = GameMode.Title;
        public int Gold;
        public string LastNotice = string.Empty;
        public PlayerRuntimeState Player = new PlayerRuntimeState();
        public QuestRuntimeState Quest = new QuestRuntimeState();
        public PlayerPoseSnapshot PreEncounterSnapshot = new PlayerPoseSnapshot();
        public PlayerEquipmentState Equipment = new PlayerEquipmentState();
        public InventoryState Inventory = new InventoryState();
        public SkillInventoryState Skills = new SkillInventoryState();
        public CombatSessionState Combat = new CombatSessionState();
        public WorldRuntimeState World = new WorldRuntimeState();
        public ShopRuntimeState Shop = new ShopRuntimeState();

        public void StartNewGame()
        {
            Mode = GameMode.Town;
            Gold = 25;
            LastNotice = string.Empty;
            Player.Reset();
            Quest.Clear();
            Quest.ClearLastReward();
            Quest.BoardOfferIndex = 0;
            Quest.BoardRerollCount = 0;
            PreEncounterSnapshot.Clear();
            var starterEquipmentId = StarterEquipmentIdResolver();
            var starterSkillId = StarterSkillIdResolver();
            Inventory.Clear();
            Inventory.AddItem(starterEquipmentId);
            Equipment.EquippedWeaponId = starterEquipmentId;
            Equipment.EquippedShieldId = string.Empty;
            Equipment.EquippedHeadId = string.Empty;
            Equipment.EquippedChestId = string.Empty;
            Equipment.EquippedArmsId = string.Empty;
            Equipment.EquippedLegsId = string.Empty;
            Equipment.EquippedFeetId = string.Empty;
            Skills.Clear();
            Skills.AddSkill(starterSkillId);
            Skills.EquipFirstOpenFace(starterSkillId, Equipment.DiceCount);
            Combat.Clear();
            World.Clear();
            if (Shop == null)
            {
                Shop = new ShopRuntimeState();
            }

            Shop.Reset();
        }
    }
}
