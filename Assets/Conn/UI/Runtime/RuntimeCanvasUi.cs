using Conn.Core.Equipment;
using Conn.Core.Quests;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Runtime.Combat;
using Conn.Runtime.Content;
using Conn.Runtime.Equipment;
using Conn.Runtime.Inventory;
using Conn.Runtime.Maps;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;
using Conn.Runtime.Skills;
using Conn.Runtime.World;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Conn.UI.Runtime
{
    public sealed class RuntimeCanvasUi : MonoBehaviour
    {
        [SerializeField] private GameSceneId sceneId;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Sprite blacksmithBackgroundSprite;
        [SerializeField] private Sprite skillMerchantBackgroundSprite;
        [SerializeField] private Sprite innBackgroundSprite;
        [SerializeField] private Sprite apothecaryBackgroundSprite;
        [SerializeField] private Sprite scholarBackgroundSprite;
        private const float RefreshIntervalSeconds = 0.15f;
        private bool characterOpen;
        private float nextRefreshTime;
        private string lastRenderKey = string.Empty;
        private string selectedSkillId = string.Empty;
        private int selectedQuestBoardOffset;

        public GameSceneId SceneId
        {
            get => sceneId;
            set => sceneId = value;
        }

        public Canvas Canvas => canvas;

        public void ConfigureNpcBackgroundSprites(
            Sprite blacksmith,
            Sprite skillMerchant,
            Sprite inn,
            Sprite apothecary,
            Sprite scholar)
        {
            blacksmithBackgroundSprite = blacksmith;
            skillMerchantBackgroundSprite = skillMerchant;
            innBackgroundSprite = inn;
            apothecaryBackgroundSprite = apothecary;
            scholarBackgroundSprite = scholar;
        }

        private void Awake()
        {
            RuntimeCanvasUiBuilder.EnsureRuntimeCanvas(gameObject, sceneId);
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (!RuntimeUiSettings.UseCanvasUi)
            {
                if (canvas != null)
                {
                    canvas.enabled = false;
                }

                RuntimeCursorService.Apply(sceneId, GameSession.Instance != null ? GameSession.Instance.State : null, characterOpen);
                return;
            }

            if (canvas != null)
            {
                canvas.enabled = true;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                RuntimeCursorService.ToggleManualRelease();
            }

            if (Time.unscaledTime >= nextRefreshTime && !IsPointerPressActive())
            {
                Refresh();
                nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
            }

            RuntimeCursorService.Apply(sceneId, GameSession.Instance != null ? GameSession.Instance.State : null, characterOpen);
        }

        private void OnDisable()
        {
            RuntimeCursorService.Release();
        }

        public void Bind(Canvas runtimeCanvas)
        {
            canvas = runtimeCanvas;
        }

        public void Refresh()
        {
            if (canvas == null || GameSession.Instance == null)
            {
                return;
            }

            var session = GameSession.Instance.State;
            var renderKey = BuildRenderKey(session);
            if (renderKey == lastRenderKey)
            {
                return;
            }

            lastRenderKey = renderKey;
            HideScenePanels();
            if (sceneId == GameSceneId.Dungeon)
            {
                DrawCommon(session);
            }

            if (sceneId == GameSceneId.Title)
            {
                DrawTitle(session);
            }
            else if (sceneId == GameSceneId.Town)
            {
                DrawTown(session);
            }
            else if (sceneId == GameSceneId.Dungeon)
            {
                DrawDungeon(session);
            }
            else if (sceneId == GameSceneId.Combat)
            {
                DrawCombat(session);
            }
            else if (sceneId == GameSceneId.Ending)
            {
                DrawEnding(session);
            }
        }

        private string BuildRenderKey(GameSessionState session)
        {
            var key = new StringBuilder(256);
            key.Append(sceneId)
                .Append('|').Append(session.Mode)
                .Append('|').Append(session.Gold)
                .Append('|').Append(session.Player.Xp)
                .Append('|').Append(session.Player.Hp)
                .Append('/').Append(session.Player.MaxHp)
                .Append('|').Append(session.LastNotice)
                .Append('|').Append(characterOpen)
                .Append('|').Append(RuntimeCursorService.ManualReleaseActive)
                .Append('|').Append(TownQuestBoardPanelState.IsOpen)
                .Append('|').Append(TownShopPanelState.Current)
                .Append('|').Append(TownNpcInteractionState.IsOpen)
                .Append('|').Append(TownNpcInteractionState.Kind)
                .Append('|').Append(TownNpcInteractionState.NpcName)
                .Append('|').Append(TownNpcInteractionState.Dialogue)
                .Append('|').Append(TownNpcInteractionState.Cost)
                .Append('|').Append(TownNpcInteractionState.ItemId)
                .Append('|').Append(selectedSkillId)
                .Append('|').Append(selectedQuestBoardOffset)
                .Append('|').Append(session.Quest.HasActiveQuest)
                .Append('|').Append(session.Quest.ActiveQuestId)
                .Append('|').Append(session.Quest.BoardOfferIndex)
                .Append('|').Append(session.Quest.BoardRerollCount)
                .Append('|').Append(session.Shop != null ? session.Shop.SkillMerchantRefreshIndex : 0);

            AppendList(key, session.Inventory.ItemIds);
            AppendList(key, session.Skills.OwnedSkillIds);
            AppendList(key, session.Skills.EquippedSkillIds);
            if (session.Combat != null)
            {
                key.Append('|').Append(session.Combat.Active)
                    .Append('|').Append(session.Combat.Enemy.Hp)
                    .Append('|').Append(session.Combat.LastMessage)
                    .Append('|').Append(session.Combat.SelectedDiceCount)
                    .Append('|').Append(session.Combat.ReelSpinActive)
                    .Append('|').Append(session.Combat.ReelStopCount);
                if (session.Combat.ReelSpinActive)
                {
                    key.Append('|').Append(Mathf.FloorToInt(Time.unscaledTime * 8f));
                }
            }

            return key.ToString();
        }

        private static void AppendList(StringBuilder key, System.Collections.Generic.IEnumerable<string> values)
        {
            key.Append('|');
            if (values == null)
            {
                return;
            }

            foreach (var value in values)
            {
                key.Append(value).Append(',');
            }
        }

        private void HideScenePanels()
        {
            SetGroup("RuntimeHud", false);
            SetGroup("RuntimeInteractionPrompt", false);
            SetGroup("RuntimePrimaryPanel", false);
            SetGroup("RuntimeSecondaryPanel", false);
            SetGroup("RuntimeBottomPanel", false);

            foreach (var name in RuntimeCanvasUiBuilder.TitlePanelNames)
            {
                SetGroup(name, false);
            }

            foreach (var name in RuntimeCanvasUiBuilder.TownPanelNames)
            {
                SetGroup(name, false);
            }

            foreach (var name in RuntimeCanvasUiBuilder.DungeonPanelNames)
            {
                SetGroup(name, false);
            }

            foreach (var name in RuntimeCanvasUiBuilder.CombatPanelNames)
            {
                SetGroup(name, false);
            }

            foreach (var name in RuntimeCanvasUiBuilder.EndingPanelNames)
            {
                SetGroup(name, false);
            }
        }

        private void DrawCommon(GameSessionState session)
        {
            var hud = Panel("RuntimeHud");
            BuildPanel(hud, $"{sceneId}  |  Gold {session.Gold}  XP {session.Player.Xp}  HP {session.Player.Hp}/{session.Player.MaxHp}", false);
        }

        private void DrawTitle(GameSessionState session)
        {
            var root = Panel("TitleRoot");
            BuildPanel(root, "Conn", false);
            var newGameButton = AddTitleButton(root, "New Game", () =>
            {
                GameSession.Instance.StartNewGame();
                SceneFlowService.Load(GameSceneId.Town);
            });
            AddTitleButton(root, "Continue", () =>
            {
                var gameSession = GameSession.Instance;
                if (!gameSession.TryContinue())
                {
                    gameSession.StartNewGame();
                }

                SceneFlowService.Load(SaveRuntimeService.SceneForLoadedState(gameSession.State));
            });

            HidePanel("TitleButtons");
            SelectDefaultTitleButton(root, newGameButton.gameObject);
        }

        private void DrawTown(GameSessionState session)
        {
            var hud = Panel("TownHud");
            BuildPanel(hud, "Karash Outpost", false);
            AddText(hud, $"Gold {session.Gold}  XP {session.Player.Xp}  HP {session.Player.Hp}/{session.Player.MaxHp}");
            AddText(hud, session.Quest.HasActiveQuest ? $"Quest: {session.Quest.ActiveQuestTitle}" : "Quest: none");
            AddText(hud, RuntimeCursorService.ManualReleaseActive ? "Cursor: free (Esc)" : "Cursor: locked (Esc)");

            var quickActions = Panel("TownQuickActionsPanel");
            BuildPanel(quickActions, string.Empty, false);
            AddSquareButton(quickActions, characterOpen ? "Bag -" : "Bag", () => characterOpen = !characterOpen);
            AddSquareButton(quickActions, "Title", () =>
            {
                RuntimeCursorService.ClearManualRelease();
                SceneFlowService.Load(GameSceneId.Title);
            });

            if (TownNpcInteractionState.IsOpen && TownNpcInteractionState.Kind == TownNpcInteractionKind.QuestBoard)
            {
                HidePanel("TownHud");
                HidePanel("TownQuickActionsPanel");
                HidePanel("TownInteractionPrompt");
                HidePanel("TownNoticePanel");
                HidePanel("TownCharacterInventoryPanel");
                DrawTownNpcBackdrop();
                HidePanel("TownNpcInteractionPanel");
                HidePanel("TownNpcStandingCgPanel");
                DrawTownQuestBoard(session);
                HidePanel("TownShopPanel");
            }
            else if (TownNpcInteractionState.IsOpen)
            {
                HidePanel("TownHud");
                HidePanel("TownQuickActionsPanel");
                HidePanel("TownInteractionPrompt");
                HidePanel("TownNoticePanel");
                HidePanel("TownCharacterInventoryPanel");
                DrawTownNpcBackdrop();
                DrawTownNpcInteraction(session);
                HidePanel("TownQuestBoardPanel");
                HidePanel("TownShopPanel");
            }
            else
            {
                DrawInteractionPrompt("TownInteractionPrompt");
                HidePanel("TownNpcBackdropPanel");
                HidePanel("TownNpcInteractionPanel");
                HidePanel("TownNpcStandingCgPanel");
                DrawTownQuestBoard(session);
                DrawTownShop(session);
                DrawTownNotice(session);
                DrawCharacterInventory(session);
            }
        }

        private void DrawTownNpcBackdrop()
        {
            var backdrop = Panel("TownNpcBackdropPanel");
            BuildPanel(backdrop, string.Empty, false);
            ApplyNpcBackdropImage(backdrop, TownNpcInteractionState.Kind);
        }

        private void ApplyNpcBackdropImage(RectTransform backdrop, TownNpcInteractionKind kind)
        {
            if (backdrop == null)
            {
                return;
            }

            var image = backdrop.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.sprite = NpcBackdropSpriteFor(kind);
            image.color = image.sprite != null ? Color.white : PanelBackgroundColor(backdrop.name);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private Sprite NpcBackdropSpriteFor(TownNpcInteractionKind kind)
        {
            return kind switch
            {
                TownNpcInteractionKind.Blacksmith => blacksmithBackgroundSprite,
                TownNpcInteractionKind.SkillMerchant => skillMerchantBackgroundSprite,
                TownNpcInteractionKind.Inn => innBackgroundSprite,
                TownNpcInteractionKind.Apothecary => apothecaryBackgroundSprite,
                TownNpcInteractionKind.Scholar => scholarBackgroundSprite,
                _ => null
            };
        }

        private void DrawTownNpcInteraction(GameSessionState session)
        {
            var npcName = FirstNonEmpty(TownNpcInteractionState.NpcName, string.Empty, "Town NPC");
            var dialogue = FirstNonEmpty(TownNpcInteractionState.Dialogue, string.Empty, "Welcome. What do you need?");

            OrderTownNpcPanels();

            var interaction = Panel("TownNpcInteractionPanel");
            BuildPanel(interaction, npcName, true);
            AddText(interaction, dialogue, 16, FontStyle.Italic);
            AddText(interaction, " ");
            AddText(interaction, $"Gold {session.Gold}  XP {session.Player.Xp}  HP {session.Player.Hp}/{session.Player.MaxHp}");
            if (TownNpcInteractionState.Kind != TownNpcInteractionKind.None)
            {
                AddText(interaction, $"Interaction: {TownNpcInteractionState.Kind}");
            }

            if (TownNpcInteractionState.Cost > 0)
            {
                AddText(interaction, $"Cost: {TownNpcInteractionState.Cost}");
            }

            if (!string.IsNullOrWhiteSpace(TownNpcInteractionState.ItemId))
            {
                AddText(interaction, $"Item: {TownNpcInteractionState.ItemId}");
            }

            AddNpcInteractionActions(interaction, session);
            AddButton(interaction, "Close", TownNpcInteractionState.Close);

            var standingCg = Panel("TownNpcStandingCgPanel");
            BuildPanel(standingCg, string.Empty, false);
            AddText(standingCg, "Standing CG Placeholder", 26, FontStyle.Bold);
            AddText(standingCg, npcName);
        }

        private void OrderTownNpcPanels()
        {
            canvas.transform.Find("TownNpcBackdropPanel")?.SetAsLastSibling();
            canvas.transform.Find("TownNpcStandingCgPanel")?.SetAsLastSibling();
            canvas.transform.Find("TownNpcInteractionPanel")?.SetAsLastSibling();
        }

        private void AddNpcInteractionActions(Transform panel, GameSessionState session)
        {
            if (TownNpcInteractionState.Kind == TownNpcInteractionKind.Inn)
            {
                AddButton(panel, "Rest", () => TownServiceRuntimeService.Rest(session, TownNpcInteractionState.Cost));
                return;
            }

            if (TownNpcInteractionState.Kind == TownNpcInteractionKind.Trainer)
            {
                AddButton(panel, "Train Max HP", () => TownServiceRuntimeService.Train(session, TownNpcInteractionState.Cost));
                return;
            }

            if (TownNpcInteractionState.Kind == TownNpcInteractionKind.Apothecary)
            {
                AddButton(panel, "Buy Item", () => ConsumableRuntimeService.Buy(session, TownNpcInteractionState.ItemId));
                return;
            }

            if (TownNpcInteractionState.Kind == TownNpcInteractionKind.QuestBoard)
            {
                DrawTownQuestBoardContent(panel, session);
                return;
            }

            if (TownNpcInteractionState.Kind == TownNpcInteractionKind.Blacksmith || TownNpcInteractionState.Kind == TownNpcInteractionKind.SkillMerchant)
            {
                var shopKind = TownNpcInteractionState.Kind == TownNpcInteractionKind.Blacksmith
                    ? TownShopPanelKind.Blacksmith
                    : TownShopPanelKind.SkillMerchant;
                DrawTownShopContent(panel, session, shopKind);
            }
        }

        private void DrawTownQuestBoard(GameSessionState session)
        {
            if (!TownQuestBoardPanelState.IsOpen)
            {
                HidePanel("TownQuestBoardPanel");
                return;
            }

            var panel = Panel("TownQuestBoardPanel");
            ApplyQuestBoardPanelRect(panel);
            BuildPanel(panel, "Quest Board", true);
            DrawTownQuestBoardContent(panel, session);
        }

        private void DrawTownQuestBoardContent(Transform panel, GameSessionState session)
        {
            var body = AddHorizontalGroup(panel, 12f);
            var listColumn = AddQuestBoardColumn(body, "QuestList", 430f, 0.72f, new Color(0.025f, 0.026f, 0.025f, 0.88f));
            var detailColumn = AddQuestBoardColumn(body, "QuestDetail", 720f, 1.2f, new Color(0.87f, 0.82f, 0.72f, 0.96f));

            AddQuestBoardHeader(listColumn, "Available Side Quests");
            if (session.Quest.HasActiveQuest)
            {
                AddQuestListRow(listColumn, session.Quest.ActiveQuestTitle, $"{session.Quest.GoldReward}g", true, true, () => { });
                AddText(listColumn, "Only one quest can be active.", 13);
                DrawActiveQuestDetail(detailColumn, session);
            }
            else
            {
                var offers = BuildQuestBoardOffers(session);
                if (offers.Count == 0)
                {
                    AddText(listColumn, "No quest available.");
                    DrawEmptyQuestDetail(detailColumn);
                }
                else
                {
                    if (selectedQuestBoardOffset < 0 || selectedQuestBoardOffset >= offers.Count)
                    {
                        selectedQuestBoardOffset = 0;
                    }

                    for (var i = 0; i < offers.Count; i++)
                    {
                        var offset = i;
                        var offer = offers[i];
                        AddQuestListRow(
                            listColumn,
                            offer.DisplayName,
                            $"{offer.GoldReward}g",
                            i == selectedQuestBoardOffset,
                            false,
                            () =>
                            {
                                selectedQuestBoardOffset = offset;
                                lastRenderKey = string.Empty;
                                Refresh();
                            });
                    }

                    AddText(listColumn, $"Board rolls: {session.Quest.BoardRerollCount}", 13);
                    AddButton(listColumn, "Reroll Board", () =>
                    {
                        selectedQuestBoardOffset = 0;
                        QuestRuntimeService.RerollBoard(session);
                        RuntimeNoticeService.Set(session, "Quest board rerolled.");
                    });

                    DrawQuestOfferDetail(detailColumn, session, offers[selectedQuestBoardOffset]);
                }
            }

            AddButton(listColumn, "Close Board", TownQuestBoardPanelState.Close);
        }

        private static void ApplyQuestBoardPanelRect(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            panel.anchorMin = new Vector2(0.08f, 0.12f);
            panel.anchorMax = new Vector2(0.94f, 0.84f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
        }

        private static System.Collections.Generic.List<QuestDefinition> BuildQuestBoardOffers(GameSessionState session)
        {
            var offers = new System.Collections.Generic.List<QuestDefinition>();
            var seen = new System.Collections.Generic.HashSet<string>();
            const int desiredCount = 8;
            const int maxProbeCount = 24;

            for (var i = 0; i < maxProbeCount && offers.Count < desiredCount; i++)
            {
                var offer = RuntimeContentDatabase.BoardQuestAt(session.Quest.BoardOfferIndex + i);
                if (offer == null || string.IsNullOrWhiteSpace(offer.QuestId) || !seen.Add(offer.QuestId))
                {
                    continue;
                }

                offers.Add(offer);
            }

            return offers;
        }

        private RectTransform AddQuestBoardColumn(Transform parent, string name, float preferredWidth, float flexibleWidth, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            var image = obj.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.padding = name == "QuestDetail"
                ? new RectOffset(24, 24, 20, 20)
                : new RectOffset(14, 14, 14, 14);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var element = obj.AddComponent<LayoutElement>();
            element.minWidth = Mathf.Min(preferredWidth, 280f);
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            element.minHeight = 560f;
            return rect;
        }

        private void AddQuestBoardHeader(Transform parent, string text)
        {
            var label = AddQuestText(parent, text, 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            var layout = label.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = 44f;
            }
        }

        private Button AddQuestListRow(Transform parent, string title, string reward, bool selected, bool completed, UnityEngine.Events.UnityAction action)
        {
            var obj = new GameObject("QuestListRow");
            obj.transform.SetParent(ContentParent(parent), false);
            var image = obj.AddComponent<Image>();
            image.color = selected
                ? new Color(0.58f, 0.42f, 0.18f, 0.94f)
                : new Color(0.06f, 0.065f, 0.06f, 0.64f);
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = new Color(0.68f, 0.48f, 0.2f, 0.96f);
            colors.pressedColor = new Color(0.82f, 0.52f, 0.18f, 0.98f);
            button.colors = colors;

            var layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = 46f;
            var row = obj.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 10, 4, 4);
            row.spacing = 8f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var titleText = AddQuestText(obj.transform, title, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            var titleLayout = titleText.GetComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;
            titleLayout.minHeight = 34f;
            var rewardText = AddQuestText(obj.transform, completed ? "Done" : reward, 16, FontStyle.Bold, new Color(1f, 0.82f, 0.38f, 1f), TextAnchor.MiddleRight);
            var rewardLayout = rewardText.GetComponent<LayoutElement>();
            rewardLayout.minWidth = 72f;
            rewardLayout.preferredWidth = 86f;
            return button;
        }

        private void DrawQuestOfferDetail(Transform parent, GameSessionState session, QuestDefinition quest)
        {
            AddQuestText(parent, quest.DisplayName, 31, FontStyle.Bold, Color.black, TextAnchor.MiddleCenter);
            AddQuestSeparator(parent);
            AddQuestDetailRow(parent, "Objective", "Required", "Owned");
            AddQuestDetailRow(parent, quest.TargetMonsterId, "1", "-");
            AddQuestSeparator(parent);
            AddQuestSectionTitle(parent, "Reward");
            AddQuestText(parent, $"{quest.GoldReward}g", 22, FontStyle.Bold, new Color(0.14f, 0.09f, 0.02f, 1f), TextAnchor.MiddleLeft);
            AddQuestSeparator(parent);
            AddQuestDetailRow(parent, "Client", "Quest Board", string.Empty);
            AddQuestDetailRow(parent, "Encounter", quest.TargetEncounterId, string.Empty);
            AddQuestDetailRow(parent, "Map", quest.MapProfileId, string.Empty);
            AddQuestText(parent, "Complete the listed hunt objective, then return from the dungeon to claim the reward.", 19, FontStyle.Normal, new Color(0.12f, 0.1f, 0.08f, 1f), TextAnchor.UpperLeft);

            var actions = AddHorizontalGroup(parent, 10f);
            var questId = quest.QuestId;
            AddButton(actions, "Accept", () =>
            {
                QuestRuntimeService.AcceptQuest(session, questId);
                RuntimeNoticeService.Set(session, $"Accepted quest: {questId}");
                TownQuestBoardPanelState.Close();
            });
            AddButton(actions, "Cancel", TownQuestBoardPanelState.Close);
        }

        private void DrawActiveQuestDetail(Transform parent, GameSessionState session)
        {
            AddQuestText(parent, session.Quest.ActiveQuestTitle, 31, FontStyle.Bold, Color.black, TextAnchor.MiddleCenter);
            AddQuestSeparator(parent);
            AddQuestDetailRow(parent, "Objective", "Required", "State");
            AddQuestDetailRow(parent, session.Quest.TargetMonsterId, "1", session.Quest.TargetDefeated ? "Done" : "Open");
            AddQuestSeparator(parent);
            AddQuestSectionTitle(parent, "Reward");
            AddQuestText(parent, $"{session.Quest.GoldReward}g", 22, FontStyle.Bold, new Color(0.14f, 0.09f, 0.02f, 1f), TextAnchor.MiddleLeft);
            AddQuestSeparator(parent);
            AddQuestDetailRow(parent, "Encounter", session.Quest.TargetEncounterId, string.Empty);
            AddQuestDetailRow(parent, "Map", session.Quest.MapProfileId, string.Empty);
            AddQuestText(parent, session.Quest.ReturnAvailable ? "The quest target is complete. Return from the dungeon to collect the reward." : "Enter the dungeon and defeat the target to unlock return.", 19, FontStyle.Normal, new Color(0.12f, 0.1f, 0.08f, 1f), TextAnchor.UpperLeft);
            AddButton(parent, "Cancel", TownQuestBoardPanelState.Close);
        }

        private void DrawEmptyQuestDetail(Transform parent)
        {
            AddQuestText(parent, "No Quest Available", 31, FontStyle.Bold, Color.black, TextAnchor.MiddleCenter);
            AddQuestSeparator(parent);
            AddQuestText(parent, "The board has no valid hunt contracts right now.", 20, FontStyle.Normal, new Color(0.12f, 0.1f, 0.08f, 1f), TextAnchor.UpperLeft);
            AddButton(parent, "Cancel", TownQuestBoardPanelState.Close);
        }

        private void AddQuestSectionTitle(Transform parent, string title)
        {
            var text = AddQuestText(parent, title, 20, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            text.color = new Color(0.38f, 0.34f, 0.26f, 1f);
        }

        private void AddQuestDetailRow(Transform parent, string left, string middle, string right)
        {
            var row = AddHorizontalGroup(parent, 8f);
            AddQuestCell(row, left, 1.2f, TextAnchor.MiddleLeft);
            AddQuestCell(row, middle, 0.6f, TextAnchor.MiddleCenter);
            if (!string.IsNullOrWhiteSpace(right))
            {
                AddQuestCell(row, right, 0.6f, TextAnchor.MiddleCenter);
            }
        }

        private void AddQuestCell(Transform parent, string text, float flexibleWidth, TextAnchor alignment)
        {
            var cell = AddQuestText(parent, text, 18, FontStyle.Bold, new Color(0.12f, 0.1f, 0.08f, 1f), alignment);
            var layout = cell.GetComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.minHeight = 32f;
        }

        private void AddQuestSeparator(Transform parent)
        {
            var obj = new GameObject("QuestSeparator");
            obj.transform.SetParent(ContentParent(parent), false);
            var image = obj.AddComponent<Image>();
            image.color = new Color(0.24f, 0.22f, 0.18f, 0.42f);
            image.raycastTarget = false;
            var layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = 2f;
        }

        private Text AddQuestText(Transform parent, string text, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            var obj = new GameObject("QuestText");
            obj.transform.SetParent(ContentParent(parent), false);
            var textComponent = obj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = size;
            textComponent.fontStyle = style;
            textComponent.color = color;
            textComponent.alignment = alignment;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.raycastTarget = false;
            var layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(26f, size + 10f);
            return textComponent;
        }

        private void DrawTownShop(GameSessionState session)
        {
            if (TownShopPanelState.Current == TownShopPanelKind.None)
            {
                HidePanel("TownShopPanel");
                return;
            }

            var panel = Panel("TownShopPanel");
            BuildPanel(panel, "Town Shop", true);
            DrawTownShopContent(panel, session, TownShopPanelState.Current);
        }

        private void DrawTownShopContent(Transform panel, GameSessionState session, TownShopPanelKind shopKind)
        {
            if (shopKind == TownShopPanelKind.Blacksmith)
            {
                AddText(panel, "Blacksmith");
                var stock = EquipmentShopRuntimeService.BlacksmithStockItemIds();
                for (var i = 0; i < stock.Length; i++)
                {
                    var item = RuntimeContentDatabase.FindEquipment(stock[i]);
                    if (item == null || item.BuyPrice <= 0)
                    {
                        continue;
                    }

                    var itemId = item.ItemId;
                    AddText(panel, ChapterOneUxText.EquipmentBuyStatus(session, itemId));
                    AddButton(panel, $"Buy & Equip: {item.DisplayName}", () => EquipmentShopRuntimeService.BuyAndEquip(session, itemId), EquipmentShopRuntimeService.CanBuy(session, itemId));
                }

                AddText(panel, "Sell");
                for (var i = 0; i < session.Inventory.ItemIds.Count; i++)
                {
                    var itemId = session.Inventory.ItemIds[i];
                    var item = RuntimeContentDatabase.FindEquipment(itemId);
                    if (item == null)
                    {
                        continue;
                    }

                    AddText(panel, ChapterOneUxText.EquipmentStatus(session, itemId));
                    AddButton(panel, $"Sell: {item.DisplayName}", () => EquipmentShopRuntimeService.Sell(session, itemId), EquipmentShopRuntimeService.CanSell(session, itemId));
                }

                AddButton(panel, "Switch Owned Loadout", () => EquipmentRuntimeService.ToggleOwnedLoadout(session));
                AddButton(panel, "Close Shop", TownShopPanelState.Close);
            }
            else if (shopKind == TownShopPanelKind.SkillMerchant)
            {
                AddText(panel, $"Skill Merchant  |  Refresh #{session.Shop.SkillMerchantRefreshIndex}");
                var stock = SkillShopRuntimeService.SkillMerchantStock(session);
                for (var i = 0; i < stock.Length; i++)
                {
                    var skill = RuntimeContentDatabase.FindSkill(stock[i]);
                    if (skill == null || skill.BuyPrice <= 0)
                    {
                        continue;
                    }

                    var skillId = skill.SkillId;
                    AddText(panel, ChapterOneUxText.SkillBuyStatus(session, skillId));
                    AddButton(panel, $"Buy & Equip: {skill.DisplayName}", () => SkillShopRuntimeService.BuyAndEquip(session, skillId), SkillShopRuntimeService.CanBuy(session, skillId));
                }

                AddButton(panel, "Refresh Skill Stock", () => SkillShopRuntimeService.RefreshSkillMerchantStock(session));
                AddText(panel, "Sell Loose Skills");
                for (var i = 0; i < session.Skills.OwnedSkillIds.Count; i++)
                {
                    var skill = RuntimeContentDatabase.FindSkill(session.Skills.OwnedSkillIds[i]);
                    if (skill == null)
                    {
                        continue;
                    }

                    var skillId = skill.SkillId;
                    AddText(panel, ChapterOneUxText.SkillStatus(session, skillId));
                    AddButton(panel, $"Sell Loose: {skill.DisplayName}", () => SkillShopRuntimeService.SellLoose(session, skillId), SkillShopRuntimeService.CanSellLoose(session, skillId));
                }

                AddButton(panel, "Close Shop", TownShopPanelState.Close);
            }
        }

        private void DrawCharacterInventory(GameSessionState session)
        {
            if (!characterOpen)
            {
                HidePanel("TownCharacterInventoryPanel");
                return;
            }

            var panel = Panel("TownCharacterInventoryPanel");
            BuildPanel(panel, "Character / Inventory", true);
            AddText(panel, $"Loadout: {session.Equipment.WeaponGrip}  Dice {session.Equipment.DiceCount}  Def {session.Equipment.DefenseBonus}");
            AddText(panel, $"Weapon: {EquipmentName(session.Equipment.EquippedWeaponId)}");
            AddText(panel, $"Shield: {EquipmentName(session.Equipment.EquippedShieldId)}");
            AddText(panel, $"Armor: H {EquipmentName(session.Equipment.EquippedHeadId)} / C {EquipmentName(session.Equipment.EquippedChestId)}");
            AddText(panel, $"Armor: A {EquipmentName(session.Equipment.EquippedArmsId)} / L {EquipmentName(session.Equipment.EquippedLegsId)} / F {EquipmentName(session.Equipment.EquippedFeetId)}");
            AddText(panel, "Equipment");
            for (var i = 0; i < session.Inventory.ItemIds.Count; i++)
            {
                var itemId = session.Inventory.ItemIds[i];
                var item = RuntimeContentDatabase.FindEquipment(itemId);
                if (item == null)
                {
                    continue;
                }

                AddText(panel, ChapterOneUxText.EquipmentStatus(session, itemId));
                AddText(panel, session.Equipment.ComparisonLineFor(itemId));
                AddButton(panel, $"Equip: {item.DisplayName}", () => EquipmentRuntimeService.TryEquip(session, itemId), !session.Equipment.IsEquipped(itemId));
            }

            AddText(panel, "Consumables");
            var consumableIds = ConsumableRuntimeService.OwnedConsumableIds(session);
            if (consumableIds.Length == 0)
            {
                AddText(panel, "No consumables");
            }

            for (var i = 0; i < consumableIds.Length; i++)
            {
                var itemId = consumableIds[i];
                var item = RuntimeContentDatabase.FindConsumable(itemId);
                if (item == null)
                {
                    continue;
                }

                AddText(panel, ChapterOneUxText.ConsumableStatus(session, itemId));
                AddButton(panel, $"Use: {item.DisplayName}", () => ConsumableRuntimeService.Use(session, itemId), session.Player.Hp < session.Player.MaxHp);
            }

            AddText(panel, "Skill Inventory");
            if (session.Skills.OwnedSkillIds.Count == 0)
            {
                AddText(panel, "No owned skills.");
            }

            var renderedSkillIds = new System.Collections.Generic.HashSet<string>();
            for (var i = 0; i < session.Skills.OwnedSkillIds.Count; i++)
            {
                var skillId = session.Skills.OwnedSkillIds[i];
                if (!renderedSkillIds.Add(skillId))
                {
                    continue;
                }

                var skill = RuntimeContentDatabase.FindSkill(skillId);
                if (skill == null)
                {
                    continue;
                }

                var selected = skillId == selectedSkillId ? "Selected | " : string.Empty;
                var owned = session.Skills.CountOwned(skillId);
                var equipped = session.Skills.CountEquipped(skillId);
                var skillButton = AddButton(
                    panel,
                    $"{selected}{skill.DisplayName} | {skill.EffectKind} +{skill.Power} | 보유 {owned} 장착 {equipped}",
                    () => selectedSkillId = skillId);
                skillButton.gameObject.AddComponent<SkillFaceDragDrop>().ConfigureDragSource(skillId);
            }

            AddText(panel, string.IsNullOrWhiteSpace(selectedSkillId) ? "Selected skill: none" : $"Selected skill: {SkillName(selectedSkillId)}");
            AddText(panel, "Dice Faces");
            for (var i = 0; i < session.Equipment.DiceCount; i++)
            {
                var faceIndex = i;
                var skillId = i < session.Skills.EquippedSkillIds.Count ? session.Skills.EquippedSkillIds[i] : string.Empty;
                var skill = RuntimeContentDatabase.FindSkill(skillId);
                var faceButton = AddButton(
                    panel,
                    $"Face {i + 1}: {(skill != null ? skill.DisplayName : "기본공격")}",
                    () =>
                    {
                        if (string.IsNullOrWhiteSpace(selectedSkillId))
                        {
                            SkillRuntimeService.CycleEquippedFace(session, faceIndex);
                        }
                        else
                        {
                            SkillRuntimeService.EquipSkillToFace(session, selectedSkillId, faceIndex);
                        }
                    });
                faceButton.gameObject.AddComponent<SkillFaceDragDrop>().ConfigureDropTarget(faceIndex);
            }
        }

        private void DrawTownNotice(GameSessionState session)
        {
            var panel = Panel("TownNoticePanel");
            BuildPanel(panel, "Notice", true);
            AddText(panel, string.IsNullOrWhiteSpace(session.LastNotice) ? "No notice." : session.LastNotice);
            if (!string.IsNullOrWhiteSpace(session.Quest.LastCompletedQuestTitle))
            {
                AddText(panel, $"Last reward: {session.Quest.LastCompletedQuestTitle} +{session.Quest.LastGoldReward}g");
            }
        }

        private void DrawDungeon(GameSessionState session)
        {
            var hud = Panel("DungeonHud");
            BuildPanel(hud, "Dungeon", false);
            AddText(hud, session.Quest.HasActiveQuest ? $"Quest: {session.Quest.ActiveQuestTitle}" : "Quest: none");
            AddText(hud, $"Target: {session.Quest.TargetMonsterId}");
            AddText(hud, $"Expedition: {FieldMonsterRuntimeService.ExpeditionStatus(session)}");
            AddText(hud, $"Return: {(session.Quest.ReturnAvailable ? "available" : "locked")}");
            DrawInteractionPrompt("DungeonInteractionPrompt");

            var returns = Panel("DungeonReturnPanel");
            BuildPanel(returns, "Return", true);
            if (session.Quest.ReturnAvailable && !session.Quest.ReturnPromptSeen)
            {
                AddText(returns, "Quest complete");
                AddButton(returns, "Return Now", () => QuestRuntimeService.ReturnToTown(session));
                AddButton(returns, "Keep Exploring", () => QuestRuntimeService.KeepExploring(session));
            }
            else
            {
                AddText(returns, session.Quest.ReturnAvailable ? "Return available" : "Defeat the target to return.");
            }

            AddButton(returns, "Return To Town", () => QuestRuntimeService.ReturnToTown(session), session.Quest.ReturnAvailable);

            var readout = Panel("DungeonPlacementReadout");
            BuildPanel(readout, "CompiledMap Readout", true);
            var compiledMap = CompiledMapDungeonRuntimeService.CurrentCompiledMap;
            AddText(readout, $"Snapshot: {(session.PreEncounterSnapshot.Valid ? "saved" : "none")}");
            AddText(readout, $"Field monsters active: {FieldMonsterRuntimeService.CountActive(session)}");
            AddText(readout, $"Field monsters defeated: {FieldMonsterRuntimeService.CountDefeated(session)}");
            AddText(readout, $"Baked cells: {CompiledMapDungeonRuntimeService.CountBakedCells(compiledMap)}");
            AddText(readout, $"Baked objects: {CompiledMapDungeonRuntimeService.CountBakedObjects(compiledMap)}");
            AddText(readout, $"Interactive objects: {CompiledMapDungeonRuntimeService.CountInteractiveObjects(compiledMap)}");
            AddText(readout, "Placements: start / quest target / exit / monster / loot");
        }

        private void DrawCombat(GameSessionState session)
        {
            if (!session.Combat.Active)
            {
                CombatRuntimeService.StartTestCombat(session);
            }

            var enemy = Panel("CombatEnemyStagePanel");
            BuildPanel(enemy, session.Combat.Enemy.DisplayName, true);
            AddText(enemy, $"Enemy: {session.Combat.Enemy.DisplayName}");
            AddText(enemy, $"HP {session.Combat.Enemy.Hp}/{session.Combat.Enemy.MaxHp}");
            AddText(enemy, $"Will attack for: {session.Combat.EnemyAttackPower} dmg");
            AddText(enemy, CombatRuntimeService.DescribeCombatantStatuses(session.Combat.Enemy));

            var command = Panel("CombatCommandPanel");
            BuildPanel(command, $"Round {session.Combat.Round}", true);
            AddText(command, session.Combat.LastMessage);
            AddText(
                command,
                session.Combat.ReelSpinActive
                    ? $"SPINNING {session.Combat.DiceFaces.Count} REELS"
                    : $"READY {session.Combat.SelectedDiceCount}/3");
            var commandRow = AddHorizontalGroup(command, 10f);
            AddButton(commandRow, "STOP", () => CombatRuntimeService.StopReels(session), CombatRuntimeService.CanStopReels(session));
            AddButton(commandRow, "Attack", () => CombatRuntimeService.ResolveSelectedDice(session), !session.Combat.ReelSpinActive);
            AddButton(commandRow, "Flee", () => CombatRuntimeService.Flee(session));

            var status = Panel("CombatStatusPanel");
            BuildPanel(status, "Player", true);
            AddText(status, $"HP {session.Combat.Player.Hp}/{session.Combat.Player.MaxHp}");
            AddText(status, $"Defense {session.Combat.PlayerDefenseBonus}");
            AddText(status, CombatRuntimeService.DescribeCombatantStatuses(session.Combat.Player));

            var dice = Panel("CombatDicePanel");
            BuildPanel(dice, "Skill Roulette", true);
            AddText(
                dice,
                session.Combat.ReelSpinActive
                    ? "모든 릴이 동시에 회전 중이다. STOP을 누르면 슬롯머신처럼 전체 결과가 한 번에 확정된다."
                    : "멈춘 결과 중 최대 3개를 선택해 공격을 만든다.");
            AddText(dice, $"선택 {session.Combat.SelectedDiceCount}/3 · 릴 {session.Combat.DiceFaces.Count}개");
            var reelTray = AddHorizontalGroup(dice, 14f);
            for (var i = 0; i < session.Combat.DiceFaces.Count; i++)
            {
                AddCombatReelCard(reelTray, session, session.Combat.DiceFaces[i]);
            }

            HidePanel("CombatLogPanel");
        }

        private void DrawEnding(GameSessionState session)
        {
            var result = Panel("EndingResultPanel");
            BuildPanel(result, "Run ended", false);
            AddText(result, session.Player.IsDead ? "Result: death" : "Result: ending");
            AddText(result, string.IsNullOrWhiteSpace(session.LastNotice) ? "Continue returns here until New Game overwrites the save." : session.LastNotice);
            AddButton(result, "Back To Title", () => SceneFlowService.Load(GameSceneId.Title));
            AddButton(result, "New Game", () =>
            {
                GameSession.Instance.StartNewGame();
                SceneFlowService.Load(GameSceneId.Town);
            });

            HidePanel("EndingButtons");
        }

        private void DrawInteractionPrompt(string panelName)
        {
            var panel = Panel(panelName);
            BuildPanel(panel, "Interaction", true);
            var focused = FindFocusedInteractable();
            AddText(panel, focused == null ? "Look at an NPC, gate, board, exit, or monster." : (focused.CanInteract ? $"E - {focused.Prompt}" : focused.Prompt));
        }

        private IWorldInteractable FindFocusedInteractable()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            var ray = new Ray(camera.transform.position, camera.transform.forward);
            return Physics.Raycast(ray, out var hit, 4f) ? hit.collider.GetComponentInParent<IWorldInteractable>() : null;
        }

        private RectTransform Panel(string name)
        {
            var panel = canvas.transform.Find(name) as RectTransform;
            if (panel == null)
            {
                RuntimeCanvasUiBuilder.EnsureRuntimeCanvas(gameObject, sceneId);
                panel = canvas.transform.Find(name) as RectTransform;
            }

            if (panel == null)
            {
                return null;
            }

            SetGroup(name, true);
            return panel;
        }

        private void BuildPanel(RectTransform panel, string title, bool scroll)
        {
            if (panel == null)
            {
                return;
            }

            Clear(panel);
            var image = panel.GetComponent<Image>();
            if (image == null)
            {
                image = panel.gameObject.AddComponent<Image>();
            }

            image.color = PanelBackgroundColor(panel.name);
            image.raycastTarget = panel.name != "TitleRoot"
                && panel.name != "TownNpcStandingCgPanel";
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var scrollRect = panel.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.enabled = false;
                scrollRect.viewport = null;
                scrollRect.content = null;
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                AddTextRaw(panel, title, panel.name == "TitleRoot" ? 44 : 20, FontStyle.Bold);
            }
        }

        private void AddText(Transform parent, string text, int size = 15, FontStyle style = FontStyle.Normal)
        {
            AddTextRaw(ContentParent(parent), text, size, style);
        }

        private void AddTextRaw(Transform parent, string text, int size = 15, FontStyle style = FontStyle.Normal)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var textComponent = obj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = size;
            textComponent.fontStyle = style;
            textComponent.color = Color.white;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.raycastTarget = false;
            var layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(22f, size + 8f);
        }

        private Button AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action, bool interactable = true)
        {
            var obj = new GameObject("Button");
            obj.transform.SetParent(ContentParent(parent), false);
            var image = obj.AddComponent<Image>();
            image.color = interactable ? new Color(0.18f, 0.24f, 0.32f, 0.96f) : new Color(0.12f, 0.12f, 0.14f, 0.72f);
            var button = obj.AddComponent<Button>();
            button.interactable = interactable;
            button.onClick.AddListener(action);
            var layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = 34f;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 2f);
            rect.offsetMax = new Vector2(-8f, -2f);
            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = interactable ? Color.white : new Color(0.72f, 0.72f, 0.72f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
            return button;
        }

        private RectTransform AddHorizontalGroup(Transform parent, float spacing)
        {
            var obj = new GameObject("Row");
            obj.transform.SetParent(ContentParent(parent), false);
            var rect = obj.AddComponent<RectTransform>();
            var layout = obj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            obj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        private void AddCombatReelCard(Transform parent, GameSessionState session, Conn.Core.Combat.DiceFaceState face)
        {
            var obj = new GameObject($"Reel{face.Index + 1}");
            obj.transform.SetParent(parent, false);
            var image = obj.AddComponent<Image>();
            image.color = face.Selected
                ? new Color(0.52f, 0.34f, 0.16f, 0.92f)
                : new Color(0.16f, 0.19f, 0.27f, 0.96f);
            var button = obj.AddComponent<Button>();
            button.interactable = face.ReelStopped && !session.Combat.ReelSpinActive && !face.IsCoolingDown;
            var faceIndex = face.Index;
            button.onClick.AddListener(() => CombatRuntimeService.ToggleDieSelection(session, faceIndex));

            var layout = obj.AddComponent<LayoutElement>();
            layout.minWidth = 150f;
            layout.preferredWidth = 170f;
            layout.minHeight = 242f;

            var vertical = obj.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(12, 12, 12, 12);
            vertical.spacing = 8f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandHeight = false;

            AddTextRaw(obj.transform, $"R{face.Index + 1}", 18, FontStyle.Bold);
            if (face.IsCoolingDown)
            {
                AddTextRaw(obj.transform, $"Cooldown {face.Cooldown}", 12, FontStyle.Bold);
            }
            else if (face.Selected)
            {
                AddTextRaw(obj.transform, "Selected", 12, FontStyle.Bold);
            }
            else
            {
                AddTextRaw(obj.transform, face.ReelStopped ? "Locked" : "Spinning", 12, FontStyle.Bold);
            }

            var window = new GameObject("Window");
            window.transform.SetParent(obj.transform, false);
            var windowImage = window.AddComponent<Image>();
            windowImage.color = new Color(0.87f, 0.88f, 0.93f, 0.96f);
            var windowLayout = window.AddComponent<LayoutElement>();
            windowLayout.minHeight = 132f;
            var windowVertical = window.AddComponent<VerticalLayoutGroup>();
            windowVertical.padding = new RectOffset(8, 8, 8, 8);
            windowVertical.spacing = 4f;
            windowVertical.childControlWidth = true;
            windowVertical.childControlHeight = true;
            windowVertical.childForceExpandHeight = true;

            var centerIndex = ResolveVisibleReelCenterIndex(face);
            for (var offset = -1; offset <= 1; offset++)
            {
                var cellSkillId = ResolveReelSkillId(face, centerIndex + offset);
                var skill = RuntimeContentDatabase.FindSkill(cellSkillId);
                var cell = new GameObject("Cell");
                cell.transform.SetParent(window.transform, false);
                var cellImage = cell.AddComponent<Image>();
                var isFocus = offset == 0;
                cellImage.color = isFocus
                    ? new Color(0.95f, 0.87f, 0.72f, 0.95f)
                    : new Color(0.79f, 0.82f, 0.9f, 0.62f);
                var cellLayout = cell.AddComponent<LayoutElement>();
                cellLayout.minHeight = 34f;
                var cellVertical = cell.AddComponent<VerticalLayoutGroup>();
                cellVertical.padding = new RectOffset(6, 6, 4, 4);
                cellVertical.spacing = 1f;
                cellVertical.childAlignment = TextAnchor.MiddleCenter;
                cellVertical.childControlWidth = true;
                cellVertical.childControlHeight = true;
                cellVertical.childForceExpandHeight = true;

                var rolledValue = isFocus && face.ReelStopped ? face.RolledValue : ResolvePreviewValue(face, offset);
                AddTextRaw(cell.transform, rolledValue.ToString(), 18, FontStyle.Bold);
                AddTextRaw(cell.transform, skill != null ? skill.DisplayName : "기본공격", 11, FontStyle.Normal);
            }

            AddTextRaw(
                obj.transform,
                face.ReelStopped
                    ? $"{face.RolledValue} · {face.DisplayName} · {face.EffectKind} +{face.Power}"
                    : "STOP으로 결과 고정",
                12,
                FontStyle.Normal);
        }

        private static int ResolveVisibleReelCenterIndex(Conn.Core.Combat.DiceFaceState face)
        {
            var length = face.ReelSkillIds != null && face.ReelSkillIds.Length > 0 ? face.ReelSkillIds.Length : 1;
            if (face.ReelStopped)
            {
                return Mathf.Abs(face.ReelStopIndex) % length;
            }

            return Mathf.FloorToInt(Time.unscaledTime * 8f + face.Index * 1.7f) % length;
        }

        private static string ResolveReelSkillId(Conn.Core.Combat.DiceFaceState face, int rawIndex)
        {
            if (face.ReelSkillIds == null || face.ReelSkillIds.Length == 0)
            {
                return string.Empty;
            }

            var length = face.ReelSkillIds.Length;
            var index = rawIndex % length;
            if (index < 0)
            {
                index += length;
            }

            return face.ReelSkillIds[index];
        }

        private static int ResolvePreviewValue(Conn.Core.Combat.DiceFaceState face, int offset)
        {
            var value = Mathf.Abs(Mathf.FloorToInt(Time.unscaledTime * 9f) + face.Index * 2 + offset) % 6;
            return value + 1;
        }

        private Button AddSquareButton(Transform parent, string label, UnityEngine.Events.UnityAction action, bool interactable = true)
        {
            var button = AddButton(parent, label, action, interactable);
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minWidth = 58f;
                layout.preferredWidth = 58f;
                layout.minHeight = 58f;
                layout.preferredHeight = 58f;
            }

            return button;
        }

        private Button AddTitleButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var obj = new GameObject("TitleButton");
            obj.transform.SetParent(ContentParent(parent), false);
            var image = obj.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            var button = obj.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 0.08f, 0.18f, 0.18f);
            colors.pressedColor = new Color(1f, 0.08f, 0.18f, 0.28f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(action);

            var layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = 54f;

            var markerObj = new GameObject("Marker");
            markerObj.transform.SetParent(obj.transform, false);
            var markerRect = markerObj.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0f, 0f);
            markerRect.anchorMax = new Vector2(0f, 1f);
            markerRect.pivot = new Vector2(0f, 0.5f);
            markerRect.sizeDelta = new Vector2(36f, 0f);
            markerRect.anchoredPosition = Vector2.zero;

            var marker = markerObj.AddComponent<Text>();
            marker.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            marker.fontSize = 30;
            marker.fontStyle = FontStyle.Bold;
            marker.alignment = TextAnchor.MiddleLeft;
            marker.raycastTarget = false;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(42f, 0f);
            rect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
            obj.AddComponent<TitleMenuButtonVisual>().Bind(text, marker);
            return button;
        }

        private static void SelectDefaultTitleButton(Transform root, GameObject fallback)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || fallback == null)
            {
                return;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(root))
            {
                return;
            }

            eventSystem.SetSelectedGameObject(fallback);
        }

        private static bool IsPointerPressActive()
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return true;
            }

            return false;
        }

        private static Transform ContentParent(Transform parent)
        {
            var content = parent.Find("ScrollViewport/ScrollContent");
            return content != null ? content : parent;
        }

        private static void CreateScrollContent(RectTransform panel)
        {
            var viewportObject = new GameObject("ScrollViewport");
            viewportObject.transform.SetParent(panel, false);
            var viewport = viewportObject.AddComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportObject.AddComponent<RectMask2D>();
            var viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.08f);
            viewportImage.raycastTarget = false;
            var viewportLayout = viewportObject.AddComponent<LayoutElement>();
            viewportLayout.flexibleHeight = 1f;
            viewportLayout.minHeight = 64f;

            var contentObject = new GameObject("ScrollContent");
            contentObject.transform.SetParent(viewportObject.transform, false);
            var content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 6f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            var fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = panel.GetComponent<ScrollRect>();
            if (scroll == null)
            {
                scroll = panel.gameObject.AddComponent<ScrollRect>();
            }

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.enabled = true;
        }

        private void HidePanel(string name)
        {
            SetGroup(name, false);
        }

        private void SetGroup(string name, bool visible)
        {
            if (canvas == null)
            {
                return;
            }

            var child = canvas.transform.Find(name);
            if (child == null)
            {
                return;
            }

            var group = child.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = child.gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = visible ? 1f : 0f;
            var passivePanel = name == "TownNoticePanel"
                || name == "TownNpcStandingCgPanel";
            group.interactable = visible && !passivePanel;
            group.blocksRaycasts = visible && !passivePanel;
        }

        private static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private static string EquipmentName(string itemId)
        {
            var item = RuntimeContentDatabase.FindEquipment(itemId);
            return item != null ? item.DisplayName : "None";
        }

        private static string SkillName(string skillId)
        {
            var skill = RuntimeContentDatabase.FindSkill(skillId);
            return skill != null ? skill.DisplayName : "Unknown";
        }

        private static Color PanelBackgroundColor(string panelName)
        {
            if (panelName == "TitleRoot")
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            if (panelName == "TownNpcBackdropPanel")
            {
                return new Color(0.015f, 0.012f, 0.01f, 0.96f);
            }

            if (panelName == "TownNpcStandingCgPanel")
            {
                return new Color(0.08f, 0.08f, 0.1f, 0.76f);
            }

            return new Color(0.04f, 0.05f, 0.07f, 0.84f);
        }

        private static string FirstNonEmpty(string first, string second, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }

            return !string.IsNullOrWhiteSpace(second) ? second : fallback;
        }
    }
}
