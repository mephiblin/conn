using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;
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
            GUILayout.BeginArea(new Rect(16, 16, width, 360), GUI.skin.box);
            GUILayout.Label($"Scene: {sceneId}");
            var session = GameSession.Instance.State;
            GUILayout.Label($"Mode: {session.Mode}");
            GUILayout.Label($"Gold: {session.Gold}");
            GUILayout.Space(8);

            if (sceneId == GameSceneId.Title)
            {
                if (GUILayout.Button("New Game"))
                {
                    GameSession.Instance.StartNewGame();
                    SceneFlowService.Load(GameSceneId.Town);
                }

                if (GUILayout.Button("Continue"))
                {
                    SceneFlowService.Load(GameSceneId.Town);
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

        private static void TownControls(GameSessionState session)
        {
            GUILayout.Label(session.Quest.HasActiveQuest
                ? $"Quest: {session.Quest.ActiveQuestId}"
                : "Quest: none");

            if (GUILayout.Button("Accept Test Hunt"))
            {
                QuestRuntimeService.AcceptTestHunt(session);
            }

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

            if (GUILayout.Button("Simulate Monster Contact"))
            {
                SceneFlowService.Load(GameSceneId.Combat);
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
            if (GUILayout.Button("Win Combat"))
            {
                QuestRuntimeService.CompleteTarget(session);
                SceneFlowService.Load(GameSceneId.Dungeon);
            }

            if (GUILayout.Button("Die"))
            {
                SceneFlowService.Load(GameSceneId.Ending);
            }
        }

        private static void ReturnToTown(GameSessionState session)
        {
            QuestRuntimeService.ReturnToTown(session);
        }
    }
}
