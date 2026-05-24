using Conn.Core.Scenes;
using Conn.Core.Quests;
using Conn.Core.Session;
using Conn.Runtime.Scenes;
using Conn.Runtime.World;
using UnityEngine;

namespace Conn.Runtime.Session
{
    public static class QuestRuntimeService
    {
        public static void AcceptTestHunt(GameSessionState session)
        {
            AcceptQuest(session, QuestCatalog.TestHuntId);
        }

        public static QuestDefinition CurrentBoardOffer(GameSessionState session)
        {
            return QuestCatalog.BoardOffer(session.Quest.BoardOfferIndex);
        }

        public static void AcceptCurrentBoardOffer(GameSessionState session)
        {
            var offer = CurrentBoardOffer(session);
            if (offer != null)
            {
                AcceptQuest(session, offer);
            }
        }

        public static void RerollBoard(GameSessionState session)
        {
            session.Quest.BoardOfferIndex++;
            session.Quest.BoardRerollCount++;
        }

        public static void AcceptQuest(GameSessionState session, string questId)
        {
            var definition = QuestCatalog.Find(questId);
            if (definition != null)
            {
                AcceptQuest(session, definition);
            }
        }

        public static void AcceptQuest(GameSessionState session, QuestDefinition definition)
        {
            AcceptQuest(session, definition.QuestId, definition.DisplayName, definition.TargetMonsterId, definition.GoldReward);
        }

        public static void AcceptQuest(GameSessionState session, string questId, string targetMonsterId, int goldReward)
        {
            AcceptQuest(session, questId, questId, targetMonsterId, goldReward);
        }

        public static void AcceptQuest(GameSessionState session, string questId, string title, string targetMonsterId, int goldReward)
        {
            session.Quest.ClearLastReward();
            session.Quest.ActiveQuestId = questId;
            session.Quest.ActiveQuestTitle = title;
            session.Quest.TargetMonsterId = targetMonsterId;
            session.Quest.GoldReward = goldReward;
            session.Quest.TargetDefeated = false;
            session.Quest.ReturnAvailable = false;
            session.Quest.ReturnPromptSeen = false;
            session.PreEncounterSnapshot.Clear();
            FieldMonsterRuntimeService.ClearForNewQuest(session);
            SaveIfPlaying();
        }

        public static void CompleteTarget(GameSessionState session)
        {
            CompleteTarget(session, "field_monster_test_guard");
        }

        public static void CompleteTarget(GameSessionState session, string fieldMonsterStateKey)
        {
            session.Quest.TargetDefeated = true;
            session.Quest.ReturnAvailable = true;
            session.Quest.ReturnPromptSeen = false;
            FieldMonsterRuntimeService.MarkDefeated(session, fieldMonsterStateKey);
            SaveIfPlaying();
        }

        public static void KeepExploring(GameSessionState session)
        {
            if (!session.Quest.ReturnAvailable)
            {
                return;
            }

            session.Quest.ReturnPromptSeen = true;
            SaveIfPlaying();
        }

        public static void ReturnToTown(GameSessionState session)
        {
            CompleteReturn(session);
            SceneFlowService.Load(GameSceneId.Town);
        }

        public static void CompleteReturn(GameSessionState session)
        {
            var completedTitle = session.Quest.ActiveQuestTitle;
            var goldReward = session.Quest.GoldReward;
            session.Gold += goldReward;
            session.Quest.Clear();
            session.Quest.LastCompletedQuestTitle = completedTitle;
            session.Quest.LastGoldReward = goldReward;
            RerollBoard(session);
            session.PreEncounterSnapshot.Clear();
            SaveIfPlaying();
        }

        private static void SaveIfPlaying()
        {
            if (Application.isPlaying)
            {
                GameSession.Instance.SaveGame();
            }
        }

        public static void CapturePreEncounter(GameSessionState session, Transform player)
        {
            session.PreEncounterSnapshot.Capture(
                player.position.x,
                player.position.y,
                player.position.z,
                player.eulerAngles.y);
        }

        public static bool TryApplyPreEncounterSnapshot(GameSessionState session, Transform player)
        {
            var snapshot = session.PreEncounterSnapshot;
            if (!snapshot.Valid)
            {
                return false;
            }

            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.SetPositionAndRotation(
                new Vector3(snapshot.X, snapshot.Y, snapshot.Z),
                Quaternion.Euler(0f, snapshot.Yaw, 0f));

            if (controller != null)
            {
                controller.enabled = true;
            }

            return true;
        }
    }
}
