using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Runtime.Combat;
using Conn.Runtime.Inventory;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;
using Conn.Core.Items;
using UnityEngine;

namespace Conn.UI.Runtime
{
    public sealed class P0SceneOverlay : MonoBehaviour
    {
        [SerializeField] private GameSceneId sceneId;

        public GameSceneId SceneId
        {
            get => sceneId;
            set => sceneId = value;
        }

        private void OnGUI()
        {
            const int width = 260;
            GUILayout.BeginArea(new Rect(16, 16, width, 520), GUI.skin.box);
            GUILayout.Label($"Scene: {sceneId}");
            var session = GameSession.Instance.State;
            GUILayout.Label($"Mode: {session.Mode}");
            GUILayout.Label($"Gold: {session.Gold}");
            GUILayout.Label($"HP: {session.Player.Hp}/{session.Player.MaxHp}");
            GUILayout.Label($"Weapon: {session.Equipment.WeaponGrip} ({session.Equipment.DiceCount} dice)");
            GUILayout.Label($"Items: {session.Inventory.ItemIds.Count}");
            GUILayout.Label($"Potions: {ConsumableRuntimeService.Count(session, ConsumableCatalog.MinorPotionId)}");
            GUILayout.Label($"Skills: {session.Skills.OwnedCount}/{session.Skills.EquippedCount}");
            GUILayout.Space(8);

            DrawConsumableControls(session);

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

        private static void TownControls(GameSessionState session)
        {
            GUILayout.Label(session.Quest.HasActiveQuest
                ? $"Quest: {session.Quest.ActiveQuestTitle}"
                : "Quest: none");

            GUI.enabled = !session.Quest.HasActiveQuest;
            var offer = QuestRuntimeService.CurrentBoardOffer(session);
            if (GUILayout.Button(offer != null ? $"Accept {offer.DisplayName}" : "No Quest Available"))
            {
                QuestRuntimeService.AcceptCurrentBoardOffer(session);
            }
            GUI.enabled = true;

            GUI.enabled = session.Quest.HasActiveQuest;
            if (GUILayout.Button("Enter Dungeon"))
            {
                SceneFlowService.Load(GameSceneId.Dungeon);
            }
            GUI.enabled = true;

            if (GUILayout.Button("Back To Title"))
            {
                SceneFlowService.Load(GameSceneId.Title);
            }
        }

        private static void DungeonControls(GameSessionState session)
        {
            GUILayout.Label(session.Quest.TargetDefeated ? "Target defeated" : "Find visible monster");
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
                    session.Quest.ReturnPromptSeen = true;
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
