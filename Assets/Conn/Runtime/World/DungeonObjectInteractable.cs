using Conn.Core.Maps;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class DungeonObjectInteractable : MonoBehaviour, IWorldInteractable
    {
        private string placementId = string.Empty;
        private string runtimeReferenceId = string.Empty;
        private RoomChunkObjectKind kind;
        private bool opened;
        private int goldReward;

        public string Prompt
        {
            get
            {
                if (kind == RoomChunkObjectKind.Chest)
                {
                    return opened ? "Chest is empty" : "Open Chest";
                }

                if (kind == RoomChunkObjectKind.Barrel)
                {
                    return opened ? "Barrel is broken" : "Break Barrel";
                }

                return kind == RoomChunkObjectKind.Torch ? "Inspect Torch" : $"Inspect {kind}";
            }
        }

        public bool CanInteract => kind == RoomChunkObjectKind.Chest || kind == RoomChunkObjectKind.Barrel || kind == RoomChunkObjectKind.Torch;

        public void Configure(CompiledMapObjectPlacement placement)
        {
            placementId = placement?.PlacementId ?? string.Empty;
            runtimeReferenceId = placement?.RuntimeReferenceId ?? string.Empty;
            kind = placement?.Kind ?? RoomChunkObjectKind.Decor;
            opened = false;
            goldReward = kind == RoomChunkObjectKind.Chest ? 10 : kind == RoomChunkObjectKind.Barrel ? 3 : 0;
        }

        public void Interact()
        {
            var session = GameSession.Instance.State;
            if (session == null || !CanInteract)
            {
                return;
            }

            if (kind == RoomChunkObjectKind.Torch)
            {
                RuntimeNoticeService.Set(session, $"Torch {placementId} flickers. Ref {runtimeReferenceId}.");
                return;
            }

            if (opened)
            {
                RuntimeNoticeService.Set(session, kind == RoomChunkObjectKind.Chest ? "Chest already opened." : "Barrel already broken.");
                return;
            }

            opened = true;
            session.Gold += goldReward;
            RuntimeNoticeService.Set(session, kind == RoomChunkObjectKind.Chest
                ? $"Opened chest {placementId}. Found {goldReward}g."
                : $"Broke barrel {placementId}. Found {goldReward}g.");
        }
    }
}
