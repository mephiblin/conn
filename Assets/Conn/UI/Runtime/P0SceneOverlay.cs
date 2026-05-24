using Conn.Core.Equipment;
using Conn.Core.Items;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Combat;
using Conn.Runtime.Inventory;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;
using Conn.Runtime.Equipment;
using Conn.Runtime.Skills;
using Conn.Runtime.World;
using UnityEngine;

namespace Conn.UI.Runtime
{
    public sealed class P0SceneOverlay : MonoBehaviour
    {
        [SerializeField] private GameSceneId sceneId;
        private static bool characterPanelOpen;

        public GameSceneId SceneId
        {
            get => sceneId;
            set => sceneId = value;
        }

        private void OnGUI()
        {
            const int width = 360;
            GUILayout.BeginArea(new Rect(16, 16, width, Screen.height - 32), GUI.skin.box);
            GUILayout.Label($"Scene: {sceneId}");
            var session = GameSession.Instance.State;
            GUILayout.Label($"Mode: {session.Mode}");
            if (!string.IsNullOrWhiteSpace(session.LastNotice))
            {
                GUILayout.Label($"Notice: {session.LastNotice}");
            }

            GUILayout.Label($"Gold: {session.Gold}");
            GUILayout.Label($"XP: {session.Player.Xp}");
            GUILayout.Label($"HP: {session.Player.Hp}/{session.Player.MaxHp}");
            GUILayout.Label("Trainer: +2 Max HP / 5 XP");
            GUILayout.Label($"Loadout: {session.Equipment.WeaponGrip} ({session.Equipment.DiceCount} dice)");
            GUILayout.Label($"Weapon: {EquipmentName(session.Equipment.EquippedWeaponId)}");
            GUILayout.Label($"Shield: {EquipmentName(session.Equipment.EquippedShieldId)}");
            GUILayout.Label($"Q potion / R loadout / T face {session.Skills.NextEditFaceIndex + 1}");
            GUILayout.Label($"Items: {session.Inventory.ItemIds.Count}");
            GUILayout.Label($"Potions: {ConsumableRuntimeService.Count(session, ConsumableCatalog.MinorPotionId)}");
            GUILayout.Label($"Skills: {session.Skills.OwnedCount}/{session.Skills.EquippedCount}");
            DrawEquippedSkillFaces(session);
            GUILayout.Space(8);

            DrawConsumableControls(session);
            DrawCharacterPanelToggle(session);

            if (sceneId == GameSceneId.Title)
            {
                if (GUILayout.Button("New Game"))
                {
                    GameSession.Instance.StartNewGame();
                    SceneFlowService.Load(GameSceneId.Town);
                }

                if (GUILayout.Button("Continue"))
                {
                    var gameSession = GameSession.Instance;
                    if (!gameSession.TryContinue())
                    {
                        gameSession.StartNewGame();
                    }

                    SceneFlowService.Load(SaveRuntimeService.SceneForLoadedState(gameSession.State));
                }
            }
            else if (sceneId == GameSceneId.Town)
            {
                TownControls(session);
                QuestBoardControls(session);
                TownShopControls(session);
            }
            else if (sceneId == GameSceneId.Dungeon)
            {
                DungeonControls(session);
            }
            else if (sceneId == GameSceneId.Combat)
            {
                CombatControls(session);
            }
            else if (sceneId == GameSceneId.Ending)
            {
                if (GUILayout.Button("Back To Title"))
                {
                    SceneFlowService.Load(GameSceneId.Title);
                }
            }

            GUILayout.EndArea();
        }

        private static void DrawCharacterPanelToggle(GameSessionState session)
        {
            if (GUILayout.Button(characterPanelOpen ? "Close Character" : "Character"))
            {
                characterPanelOpen = !characterPanelOpen;
            }

            if (!characterPanelOpen)
            {
                return;
            }

            GUILayout.Space(8);
            GUILayout.Label("Equipment");
            for (var i = 0; i < session.Inventory.ItemIds.Count; i++)
            {
                var itemId = session.Inventory.ItemIds[i];
                var item = EquipmentCatalog.Find(itemId);
                if (item == null)
                {
                    continue;
                }

                var equipped = session.Equipment.IsEquipped(itemId);
                GUI.enabled = !equipped;
                if (GUILayout.Button(equipped ? $"Equipped: {item.DisplayName}" : $"Equip {item.DisplayName}"))
                {
                    EquipmentRuntimeService.TryEquip(session, itemId);
                }

                GUI.enabled = true;
            }

            GUILayout.Label("Skill Faces");
            var diceCount = session.Equipment.DiceCount;
            for (var i = 0; i < diceCount; i++)
            {
                var skillId = i < session.Skills.EquippedSkillIds.Count
                    ? session.Skills.EquippedSkillIds[i]
                    : string.Empty;
                var skill = SkillCatalog.Find(skillId);
                var label = skill != null ? skill.DisplayName : "Strike";
                if (GUILayout.Button($"Cycle Face {i + 1}: {label}"))
                {
                    SkillRuntimeService.CycleEquippedFace(session, i);
                }
            }

            GUILayout.Label("Owned Skills");
            for (var i = 0; i < SkillCatalog.All.Length; i++)
            {
                var skill = SkillCatalog.All[i];
                var owned = session.Skills.CountOwned(skill.SkillId);
                if (owned > 0)
                {
                    GUILayout.Label($"{skill.DisplayName}: {owned}");
                }
            }

            GUILayout.Space(8);
        }

        private static void DrawConsumableControls(GameSessionState session)
        {
            var potionCount = ConsumableRuntimeService.Count(session, ConsumableCatalog.MinorPotionId);
            GUI.enabled = potionCount > 0 && session.Player.Hp < session.Player.MaxHp;
            if (GUILayout.Button("Use Potion"))
            {
                ConsumableRuntimeService.Use(session, ConsumableCatalog.MinorPotionId);
            }

            GUI.enabled = true;
            GUILayout.Space(8);
        }

        private static void DrawEquippedSkillFaces(GameSessionState session)
        {
            var diceCount = session.Equipment.DiceCount;
            for (var i = 0; i < diceCount; i++)
            {
                var skillId = i < session.Skills.EquippedSkillIds.Count
                    ? session.Skills.EquippedSkillIds[i]
                    : string.Empty;
                var skill = SkillCatalog.Find(skillId);
                GUILayout.Label(skill != null
                    ? $"Face {i + 1}: {skill.DisplayName} / {skill.EffectKind} +{skill.Power}"
                    : $"Face {i + 1}: Strike / Attack +0");
            }
        }

        private static string EquipmentName(string itemId)
        {
            var item = EquipmentCatalog.Find(itemId);
            return item != null ? item.DisplayName : "None";
        }

        private static void TownControls(GameSessionState session)
        {
            GUILayout.Label(session.Quest.HasActiveQuest
                ? $"Quest: {session.Quest.ActiveQuestTitle}"
                : "Quest: none");
            if (!string.IsNullOrWhiteSpace(session.Quest.LastCompletedQuestTitle))
            {
                GUILayout.Label($"Last reward: {session.Quest.LastCompletedQuestTitle} +{session.Quest.LastGoldReward}g");
            }

            var offer = QuestRuntimeService.CurrentBoardOffer(session);
            GUILayout.Label(offer != null
                ? $"Board: {offer.DisplayName} ({offer.GoldReward}g)"
                : "Board: no quest");
            GUILayout.Label("Use E on Quest Board and Gate.");

            if (GUILayout.Button("Back To Title"))
            {
                SceneFlowService.Load(GameSceneId.Title);
            }
        }

        private static void QuestBoardControls(GameSessionState session)
        {
            if (!TownQuestBoardPanelState.IsOpen)
            {
                return;
            }

            GUILayout.Space(8);
            GUILayout.Label("Quest Board");
            if (session.Quest.HasActiveQuest)
            {
                GUILayout.Label($"Active: {session.Quest.ActiveQuestTitle}");
                GUILayout.Label($"Target: {session.Quest.TargetMonsterId}");
                GUILayout.Label($"Reward: {session.Quest.GoldReward}g");
                GUILayout.Label("Only one quest can be active.");
            }
            else
            {
                var offer = QuestRuntimeService.CurrentBoardOffer(session);
                if (offer == null)
                {
                    GUILayout.Label("No quest available.");
                }
                else
                {
                    GUILayout.Label($"Offer: {offer.DisplayName}");
                    GUILayout.Label($"Target: {offer.TargetMonsterId}");
                    GUILayout.Label($"Reward: {offer.GoldReward}g");
                    GUILayout.Label($"Board rolls: {session.Quest.BoardRerollCount}");
                    if (GUILayout.Button("Accept Quest"))
                    {
                        QuestRuntimeService.AcceptCurrentBoardOffer(session);
                        RuntimeNoticeService.Set(session, $"Accepted quest: {offer.QuestId}");
                        TownQuestBoardPanelState.Close();
                    }

                    if (GUILayout.Button("Reroll Board"))
                    {
                        QuestRuntimeService.RerollBoard(session);
                        RuntimeNoticeService.Set(session, "Quest board rerolled.");
                    }
                }
            }

            if (GUILayout.Button("Close Board"))
            {
                TownQuestBoardPanelState.Close();
            }
        }

        private static void TownShopControls(GameSessionState session)
        {
            if (TownShopPanelState.Current == TownShopPanelKind.None)
            {
                return;
            }

            GUILayout.Space(8);
            if (TownShopPanelState.Current == TownShopPanelKind.Blacksmith)
            {
                DrawBlacksmithPanel(session);
            }
            else if (TownShopPanelState.Current == TownShopPanelKind.SkillMerchant)
            {
                DrawSkillMerchantPanel(session);
            }

            if (GUILayout.Button("Close Shop"))
            {
                TownShopPanelState.Close();
            }
        }

        private static void DrawBlacksmithPanel(GameSessionState session)
        {
            GUILayout.Label("Blacksmith");
            for (var i = 0; i < EquipmentCatalog.All.Length; i++)
            {
                var item = EquipmentCatalog.All[i];
                if (item.BuyPrice <= 0)
                {
                    continue;
                }

                var owned = session.Inventory.HasItem(item.ItemId);
                GUI.enabled = !owned && EquipmentShopRuntimeService.CanBuy(session, item.ItemId);
                if (GUILayout.Button(owned
                    ? $"Owned: {item.DisplayName}"
                    : $"Buy & Equip {item.DisplayName} ({item.BuyPrice}g)"))
                {
                    EquipmentShopRuntimeService.BuyAndEquip(session, item.ItemId);
                }

                GUI.enabled = true;
            }

            GUILayout.Label("Sell");
            var anySellable = false;
            for (var i = 0; i < session.Inventory.ItemIds.Count; i++)
            {
                var itemId = session.Inventory.ItemIds[i];
                var item = EquipmentCatalog.Find(itemId);
                if (item == null || !EquipmentShopRuntimeService.CanSell(session, itemId))
                {
                    continue;
                }

                anySellable = true;
                if (GUILayout.Button($"Sell {item.DisplayName} ({item.SellPrice}g)"))
                {
                    EquipmentShopRuntimeService.Sell(session, itemId);
                    break;
                }
            }

            if (!anySellable)
            {
                GUILayout.Label("No unequipped equipment to sell.");
            }

            if (GUILayout.Button("Switch Owned Loadout"))
            {
                EquipmentRuntimeService.ToggleOwnedLoadout(session);
            }
        }

        private static void DrawSkillMerchantPanel(GameSessionState session)
        {
            GUILayout.Label("Skill Merchant");
            for (var i = 0; i < SkillCatalog.All.Length; i++)
            {
                var skill = SkillCatalog.All[i];
                if (skill.BuyPrice <= 0)
                {
                    continue;
                }

                GUI.enabled = SkillShopRuntimeService.CanBuy(session, skill.SkillId);
                if (GUILayout.Button($"Buy & Equip {skill.DisplayName} ({skill.BuyPrice}g)"))
                {
                    SkillShopRuntimeService.BuyAndEquip(session, skill.SkillId);
                }

                GUI.enabled = true;
            }

            GUILayout.Label("Sell Loose Skills");
            var anySellable = false;
            for (var i = 0; i < SkillCatalog.All.Length; i++)
            {
                var skill = SkillCatalog.All[i];
                if (!SkillShopRuntimeService.CanSellLoose(session, skill.SkillId))
                {
                    continue;
                }

                anySellable = true;
                if (GUILayout.Button($"Sell {skill.DisplayName} ({skill.SellPrice}g)"))
                {
                    SkillShopRuntimeService.SellLoose(session, skill.SkillId);
                }
            }

            if (!anySellable)
            {
                GUILayout.Label("No loose skill cards to sell.");
            }
        }

        private static void DungeonControls(GameSessionState session)
        {
            GUILayout.Label(session.Quest.HasActiveQuest
                ? $"Quest: {session.Quest.ActiveQuestTitle}"
                : "Quest: none");
            GUILayout.Label($"Target: {session.Quest.TargetMonsterId}");
            GUILayout.Label($"Expedition: {FieldMonsterRuntimeService.ExpeditionStatus(session)}");
            GUILayout.Label($"Return: {(session.Quest.ReturnAvailable ? "available" : "locked")}");
            GUILayout.Label($"Snapshot: {(session.PreEncounterSnapshot.Valid ? "saved" : "none")}");
            if (session.Quest.ReturnAvailable && !session.Quest.ReturnPromptSeen)
            {
                GUILayout.Space(8);
                GUILayout.Label("Quest complete");
                if (GUILayout.Button("Return Now"))
                {
                    ReturnToTown(session);
                    return;
                }

                if (GUILayout.Button("Keep Exploring"))
                {
                    QuestRuntimeService.KeepExploring(session);
                }

                GUILayout.Space(8);
            }

            GUI.enabled = session.Quest.ReturnAvailable;
            if (GUILayout.Button("Return To Town"))
            {
                ReturnToTown(session);
            }
            GUI.enabled = true;
        }

        private static void CombatControls(GameSessionState session)
        {
            if (!session.Combat.Active)
            {
                CombatRuntimeService.StartTestCombat(session);
            }

            GUILayout.Label($"Round: {session.Combat.Round}");
            GUILayout.Label($"Player HP: {session.Combat.Player.Hp}/{session.Combat.Player.MaxHp}");
            GUILayout.Label($"Enemy HP: {session.Combat.Enemy.Hp}/{session.Combat.Enemy.MaxHp}");
            GUILayout.Label($"Selected: {session.Combat.SelectedDiceCount}/3");
            GUILayout.Label(session.Combat.LastMessage);

            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                var face = session.Combat.DiceFaces[i];
                var prefix = face.IsCoolingDown ? "[CD] " : face.Selected ? "[x] " : "[ ] ";
                if (GUILayout.Button(prefix + face.Label))
                {
                    CombatRuntimeService.ToggleDieSelection(session, i);
                }
            }

            if (GUILayout.Button("Resolve Selected Dice"))
            {
                CombatRuntimeService.ResolveSelectedDice(session);
            }

            if (GUILayout.Button("Flee"))
            {
                CombatRuntimeService.Flee(session);
            }
        }

        private static void ReturnToTown(GameSessionState session)
        {
            QuestRuntimeService.ReturnToTown(session);
        }
    }
}
