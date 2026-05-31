using Conn.Core.Equipment;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Core.Skills;
using Conn.Runtime.Combat;
using Conn.Runtime.Content;
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
        private static Vector2 overlayScroll;
        private static GameSceneId? lastSceneId;
        private const float MaxOverlayWidth = 360f;
        private const float MinOverlayWidth = 240f;
        private const float OverlayMargin = 16f;
        private static readonly string[] CharacterPortraitIds =
        {
            "portrait_vanguard",
            "portrait_duelist",
            "portrait_arcanist"
        };
        private static readonly string[] CharacterPortraitNames =
        {
            "Vanguard",
            "Duelist",
            "Arcanist"
        };
        private static readonly string[] StarterWeaponIds =
        {
            EquipmentCatalog.RustySwordId,
            EquipmentCatalog.GreatAxeId
        };

        public GameSceneId SceneId
        {
            get => sceneId;
            set => sceneId = value;
        }

        private void OnGUI()
        {
            if (RuntimeUiSettings.UseCanvasUi && !RuntimeUiSettings.UseLegacyImguiOverlay)
            {
                return;
            }

            ResetTransientUiOnSceneChange();
            if (GameSession.Instance == null)
            {
                return;
            }

            var overlayRect = OverlayAreaRect(Screen.width, Screen.height);
            GUILayout.BeginArea(overlayRect, GUI.skin.box);
            overlayScroll = GUILayout.BeginScrollView(
                overlayScroll,
                false,
                true,
                GUILayout.Width(Mathf.Max(1f, overlayRect.width - 8f)),
                GUILayout.Height(Mathf.Max(1f, overlayRect.height - 12f)));
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
            GUILayout.Label($"Loadout: {session.Equipment.WeaponGrip} ({session.Equipment.DiceCount} dice / {session.Equipment.DefenseBonus} def)");
            GUILayout.Label($"Weapon: {EquipmentName(session.Equipment.EquippedWeaponId)}");
            GUILayout.Label($"Shield: {EquipmentName(session.Equipment.EquippedShieldId)}");
            GUILayout.Label($"Armor: H {EquipmentName(session.Equipment.EquippedHeadId)} / C {EquipmentName(session.Equipment.EquippedChestId)}");
            GUILayout.Label($"Armor: A {EquipmentName(session.Equipment.EquippedArmsId)} / L {EquipmentName(session.Equipment.EquippedLegsId)} / F {EquipmentName(session.Equipment.EquippedFeetId)}");
            GUILayout.Label($"Q consumable / R loadout / T face {session.Skills.NextEditFaceIndex + 1}");
            GUILayout.Label($"Items: {session.Inventory.ItemIds.Count}");
            GUILayout.Label($"Consumables: {ConsumableRuntimeService.OwnedConsumableIds(session).Length}");
            GUILayout.Label($"Skills: {session.Skills.OwnedCount}/{session.Skills.EquippedCount}");
            DrawEquippedSkillFaces(session);
            GUILayout.Space(8);

            DrawConsumableControls(session);
            DrawCharacterPanelToggle(session);

            if (sceneId == GameSceneId.Title)
            {
                if (session.Mode == GameMode.CharacterCreation)
                {
                    GUILayout.Label("Character Creation");
                    GUILayout.Label($"Name: {session.Character.CharacterName}");
                    DrawCharacterCreationSelectorControls(session);
                    if (GUILayout.Button("Create Character"))
                    {
                        GameSession.Instance.StartNewGame(new CharacterCreationOptions
                        {
                            CharacterName = session.Character.CharacterName,
                            SelectedPortraitIndex = session.Character.SelectedPortraitIndex,
                            SelectedPortraitId = session.Character.SelectedPortraitId,
                            Strength = session.Character.Strength,
                            Dexterity = session.Character.Dexterity,
                            Vitality = session.Character.Vitality,
                            Energy = session.Character.Energy,
                            StarterWeaponId = session.Character.StarterWeaponId
                        });
                        SceneFlowService.Load(GameSceneId.Town);
                    }

                    if (GUILayout.Button("Back"))
                    {
                        session.Mode = GameMode.Title;
                    }
                }
                else
                {
                    if (GUILayout.Button("New Game"))
                    {
                        GameSession.Instance.BeginCharacterCreation();
                    }

                    if (GUILayout.Button("Continue"))
                    {
                        var gameSession = GameSession.Instance;
                        if (!gameSession.TryContinue())
                        {
                            gameSession.BeginCharacterCreation();
                        }

                        SceneFlowService.Load(SaveRuntimeService.SceneForLoadedState(gameSession.State));
                    }
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
                EndingControls(session);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public static Rect OverlayAreaRect(int screenWidth, int screenHeight)
        {
            var availableWidth = Mathf.Max(1f, screenWidth - OverlayMargin * 2f);
            var availableHeight = Mathf.Max(1f, screenHeight - OverlayMargin * 2f);
            var width = Mathf.Clamp(availableWidth, Mathf.Min(MinOverlayWidth, availableWidth), MaxOverlayWidth);
            return new Rect(OverlayMargin, OverlayMargin, width, availableHeight);
        }

        private void ResetTransientUiOnSceneChange()
        {
            if (lastSceneId == sceneId)
            {
                return;
            }

            characterPanelOpen = false;
            overlayScroll = Vector2.zero;
            lastSceneId = sceneId;
        }

        private static void EndingControls(GameSessionState session)
        {
            GUILayout.Label("Run ended.");
            GUILayout.Label(session.Player.IsDead ? "Result: death" : "Result: ending");
            GUILayout.Label("Continue returns here until a New Game overwrites the save.");
            if (GUILayout.Button("New Game"))
            {
                GameSession.Instance.StartNewGame();
                SceneFlowService.Load(GameSceneId.Town);
            }

            if (GUILayout.Button("Back To Title"))
            {
                SceneFlowService.Load(GameSceneId.Title);
            }
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
            GUILayout.Label($"Defense: {session.Equipment.DefenseBonus} (armor {session.Equipment.ArmorValue})");
            GUILayout.Label($"Weapon: {EquipmentName(session.Equipment.EquippedWeaponId)}");
            GUILayout.Label($"Shield: {EquipmentName(session.Equipment.EquippedShieldId)}");
            GUILayout.Label("Armor");
            GUILayout.Label($"Head: {EquipmentName(session.Equipment.EquippedHeadId)}");
            GUILayout.Label($"Chest: {EquipmentName(session.Equipment.EquippedChestId)}");
            GUILayout.Label($"Arms: {EquipmentName(session.Equipment.EquippedArmsId)}");
            GUILayout.Label($"Legs: {EquipmentName(session.Equipment.EquippedLegsId)}");
            GUILayout.Label($"Feet: {EquipmentName(session.Equipment.EquippedFeetId)}");
            GUILayout.Space(4);
            GUILayout.Label("Equipment Inventory");
            for (var i = 0; i < session.Inventory.ItemIds.Count; i++)
            {
                var itemId = session.Inventory.ItemIds[i];
                var item = RuntimeContentDatabase.FindEquipment(itemId);
                if (item == null)
                {
                    continue;
                }

                var equipped = session.Equipment.IsEquipped(itemId);
                GUILayout.Label(ChapterOneUxText.EquipmentStatus(session, itemId));
                GUILayout.Label(session.Equipment.ComparisonLineFor(itemId));
                GUI.enabled = !equipped;
                if (GUILayout.Button(equipped ? $"Equipped | {item.DisplayName}" : $"Equip | {item.DisplayName}"))
                {
                    EquipmentRuntimeService.TryEquip(session, itemId);
                }

                GUI.enabled = true;
            }

            GUILayout.Label("Consumables");
            var consumableIds = ConsumableRuntimeService.OwnedConsumableIds(session);
            if (consumableIds.Length == 0)
            {
                GUILayout.Label("No consumables");
            }

            for (var i = 0; i < consumableIds.Length; i++)
            {
                GUILayout.Label(ChapterOneUxText.ConsumableStatus(session, consumableIds[i]));
            }

            DrawConsumableControls(session);

            GUILayout.Label("Skill Faces");
            var diceCount = session.Equipment.DiceCount;
            for (var i = 0; i < diceCount; i++)
            {
                var skillId = i < session.Skills.EquippedSkillIds.Count
                    ? session.Skills.EquippedSkillIds[i]
                    : string.Empty;
                var skill = RuntimeContentDatabase.FindSkill(skillId);
                var label = skill != null ? skill.DisplayName : "기본공격";
                if (GUILayout.Button($"Cycle Face {i + 1}: {label}"))
                {
                    SkillRuntimeService.CycleEquippedFace(session, i);
                }
            }

            GUILayout.Label("Owned Skills");
            for (var i = 0; i < session.Skills.OwnedSkillIds.Count; i++)
            {
                var skill = RuntimeContentDatabase.FindSkill(session.Skills.OwnedSkillIds[i]);
                if (skill != null)
                {
                    GUILayout.Label(ChapterOneUxText.SkillStatus(session, skill.SkillId));
                }
            }

            GUILayout.Space(8);
        }

        private static void DrawConsumableControls(GameSessionState session)
        {
            var consumableId = ConsumableRuntimeService.FirstOwnedConsumableId(session);
            var consumable = RuntimeContentDatabase.FindConsumable(consumableId);
            GUI.enabled = consumable != null && session.Player.Hp < session.Player.MaxHp;
            if (GUILayout.Button(consumable != null ? $"Use {consumable.DisplayName}" : "Use Consumable"))
            {
                ConsumableRuntimeService.Use(session, consumableId);
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
                var skill = RuntimeContentDatabase.FindSkill(skillId);
                GUILayout.Label(skill != null
                    ? $"Face {i + 1}: {skill.DisplayName} / {skill.EffectKind} +{skill.Power}"
                    : $"Face {i + 1}: 기본공격 / Attack +0");
            }
        }

        private static string EquipmentName(string itemId)
        {
            var item = RuntimeContentDatabase.FindEquipment(itemId);
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
            var stockItemIds = EquipmentShopRuntimeService.BlacksmithStockItemIds();
            for (var i = 0; i < stockItemIds.Length; i++)
            {
                var item = RuntimeContentDatabase.FindEquipment(stockItemIds[i]);
                if (item == null || item.BuyPrice <= 0)
                {
                    continue;
                }

                var owned = session.Inventory.HasItem(item.ItemId);
                GUILayout.Label(ChapterOneUxText.EquipmentBuyStatus(session, item.ItemId));
                GUI.enabled = !owned && EquipmentShopRuntimeService.CanBuy(session, item.ItemId);
                if (GUILayout.Button(owned
                    ? $"Owned | {item.DisplayName}"
                    : $"Buy & Equip | {item.DisplayName} ({item.BuyPrice}g)"))
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
                var item = RuntimeContentDatabase.FindEquipment(itemId);
                if (item == null || !EquipmentShopRuntimeService.CanSell(session, itemId))
                {
                    continue;
                }

                anySellable = true;
                GUILayout.Label(ChapterOneUxText.EquipmentStatus(session, itemId));
                if (GUILayout.Button($"Sell | {item.DisplayName} ({item.SellPrice}g)"))
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
            var stockSkillIds = SkillShopRuntimeService.SkillMerchantStock(session);
            GUILayout.Label($"Stock refresh #{session.Shop.SkillMerchantRefreshIndex}");
            for (var i = 0; i < stockSkillIds.Length; i++)
            {
                var skill = RuntimeContentDatabase.FindSkill(stockSkillIds[i]);
                if (skill == null || skill.BuyPrice <= 0)
                {
                    continue;
                }

                GUI.enabled = SkillShopRuntimeService.CanBuy(session, skill.SkillId);
                GUILayout.Label(ChapterOneUxText.SkillBuyStatus(session, skill.SkillId));
                if (GUILayout.Button($"Buy & Equip | {skill.DisplayName} ({skill.BuyPrice}g)"))
                {
                    SkillShopRuntimeService.BuyAndEquip(session, skill.SkillId);
                }

                GUI.enabled = true;
            }

            if (GUILayout.Button("Refresh Skill Stock"))
            {
                SkillShopRuntimeService.RefreshSkillMerchantStock(session);
            }

            GUILayout.Label("Sell Loose Skills");
            var anySellable = false;
            for (var i = 0; i < session.Skills.OwnedSkillIds.Count; i++)
            {
                var skill = RuntimeContentDatabase.FindSkill(session.Skills.OwnedSkillIds[i]);
                if (skill == null)
                {
                    continue;
                }

                if (!SkillShopRuntimeService.CanSellLoose(session, skill.SkillId))
                {
                    continue;
                }

                anySellable = true;
                GUILayout.Label(ChapterOneUxText.SkillStatus(session, skill.SkillId));
                if (GUILayout.Button($"Sell Loose | {skill.DisplayName} ({skill.SellPrice}g)"))
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
            GUILayout.Label($"Player {CombatRuntimeService.DescribeCombatantStatuses(session.Combat.Player)}");
            GUILayout.Label($"Enemy HP: {session.Combat.Enemy.Hp}/{session.Combat.Enemy.MaxHp}");
            GUILayout.Label($"Enemy {CombatRuntimeService.DescribeCombatantStatuses(session.Combat.Enemy)}");
            GUILayout.Label(CombatRuntimeService.DescribeEnemySlots(session.Combat));
            GUILayout.Label($"Selected: {session.Combat.SelectedDiceCount}/3 / Cooldown shown per reel result");
            GUILayout.Label($"Log: {session.Combat.LastMessage}");

            GUI.enabled = CombatRuntimeService.CanStopReels(session);
            if (GUILayout.Button("STOP Reels"))
            {
                CombatRuntimeService.StopReels(session);
            }
            GUI.enabled = true;

            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                var face = session.Combat.DiceFaces[i];
                if (GUILayout.Button(CombatRuntimeService.DescribeDiceFace(face)))
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

        private static void DrawCharacterCreationSelectorControls(GameSessionState session)
        {
            var portraitIndex = ClampIndex(session.Character.SelectedPortraitIndex, CharacterPortraitIds.Length);
            if (session.Character.SelectedPortraitId != CharacterPortraitIds[portraitIndex])
            {
                ApplyCharacterPortrait(session, portraitIndex);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(42f)))
            {
                ApplyCharacterPortrait(session, portraitIndex - 1);
            }

            GUILayout.Label($"Portrait: {CharacterPortraitNames[portraitIndex]} ({portraitIndex + 1}/{CharacterPortraitIds.Length})");
            if (GUILayout.Button(">", GUILayout.Width(42f)))
            {
                ApplyCharacterPortrait(session, portraitIndex + 1);
            }

            GUILayout.EndHorizontal();

            var starterIndex = StarterWeaponIndex(session.Character.StarterWeaponId);
            if (session.Character.StarterWeaponId != StarterWeaponIds[starterIndex])
            {
                ApplyStarterWeapon(session, starterIndex);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(42f)))
            {
                ApplyStarterWeapon(session, starterIndex - 1);
            }

            GUILayout.Label($"Starter: {EquipmentName(StarterWeaponIds[starterIndex])} ({starterIndex + 1}/{StarterWeaponIds.Length})");
            if (GUILayout.Button(">", GUILayout.Width(42f)))
            {
                ApplyStarterWeapon(session, starterIndex + 1);
            }

            GUILayout.EndHorizontal();
        }

        private static void ApplyCharacterPortrait(GameSessionState session, int index)
        {
            var portraitIndex = ClampIndex(index, CharacterPortraitIds.Length);
            session.Character.SelectedPortraitIndex = portraitIndex;
            session.Character.SelectedPortraitId = CharacterPortraitIds[portraitIndex];
            session.Character.Strength = portraitIndex switch
            {
                0 => 30,
                1 => 18,
                2 => 12,
                _ => 20
            };
            session.Character.Dexterity = portraitIndex switch
            {
                0 => 18,
                1 => 30,
                2 => 18,
                _ => 20
            };
            session.Character.Vitality = portraitIndex switch
            {
                0 => 26,
                1 => 20,
                2 => 22,
                _ => 20
            };
            session.Character.Energy = portraitIndex switch
            {
                0 => 10,
                1 => 14,
                2 => 30,
                _ => 20
            };
        }

        private static void ApplyStarterWeapon(GameSessionState session, int index)
        {
            session.Character.StarterWeaponId = StarterWeaponIds[ClampIndex(index, StarterWeaponIds.Length)];
        }

        private static int StarterWeaponIndex(string weaponId)
        {
            for (var i = 0; i < StarterWeaponIds.Length; i++)
            {
                if (StarterWeaponIds[i] == weaponId)
                {
                    return i;
                }
            }

            return 0;
        }

        private static int ClampIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            var wrapped = index % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private static void ReturnToTown(GameSessionState session)
        {
            QuestRuntimeService.ReturnToTown(session);
        }
    }
}
