using Conn.Core.Scenes;
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

        public bool CanInteract => GameSession.Instance.State.Quest.HasActiveQuest;

        public void Interact()
        {
            if (!CanInteract)
            {
                Debug.Log("The gate is closed. Accept a quest first.");
                return;
            }

            SceneFlowService.Load(GameSceneId.Dungeon);
        }
    }
}
