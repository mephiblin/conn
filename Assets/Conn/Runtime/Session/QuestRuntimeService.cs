using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Runtime.Scenes;
using UnityEngine;

namespace Conn.Runtime.Session
{
    public static class QuestRuntimeService
    {
        public static void AcceptTestHunt(GameSessionState session)
        {
            AcceptQuest(session, "quest_test_hunt", "monster_test_guard", 10);
        }

        public static void AcceptQuest(GameSessionState session, string questId, string targetMonsterId, int goldReward)
        {
            session.Quest.ActiveQuestId = questId;
            session.Quest.TargetMonsterId = targetMonsterId;
            session.Quest.GoldReward = goldReward;
            session.Quest.TargetDefeated = false;
            session.Quest.ReturnAvailable = false;
            session.Quest.ReturnPromptSeen = false;
            session.PreEncounterSnapshot.Clear();
            GameSession.Instance.SaveGame();
        }

        public static void CompleteTarget(GameSessionState session)
        {
            session.Quest.TargetDefeated = true;
            session.Quest.ReturnAvailable = true;
            session.Quest.ReturnPromptSeen = false;
            GameSession.Instance.SaveGame();
        }

        public static void ReturnToTown(GameSessionState session)
        {
            session.Gold += session.Quest.GoldReward;
            session.Quest.Clear();
            session.PreEncounterSnapshot.Clear();
            GameSession.Instance.SaveGame();
            SceneFlowService.Load(GameSceneId.Town);
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
