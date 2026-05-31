using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Runtime.Scenes;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class GateInteractable : MonoBehaviour, IWorldInteractable
    {
        public string Prompt => GameSession.Instance.State.Quest.HasActiveQuest
            ? "Enter dungeon"
            : "Quest required";

        public bool CanInteract => CanEnterDungeon(GameSession.Instance.State);

        public static bool CanEnterDungeon(GameSessionState session)
        {
            return session != null && session.Quest.HasActiveQuest;
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                RuntimeNoticeService.Set(GameSession.Instance.State, ChapterOneUxText.GateBlockedNotice());
                return;
            }

            RuntimeNoticeService.Set(GameSession.Instance.State, ChapterOneUxText.GateAllowedNotice(GameSession.Instance.State));
            SceneFlowService.Load(GameSceneId.Dungeon);
        }
    }
}
