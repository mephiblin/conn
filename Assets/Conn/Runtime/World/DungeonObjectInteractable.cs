using Conn.Core.Maps;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class DungeonObjectInteractable : MonoBehaviour, IWorldInteractable
    {
        private string placementId = string.Empty;
        private string runtimeReferenceId = string.Empty;
        private string stateKey = string.Empty;
        private RoomChunkObjectKind kind;

        public string Prompt
        {
            get
            {
                var opened = IsOpened();
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
            stateKey = DungeonObjectRuntimeService.StateKeyFor(placementId, runtimeReferenceId);
            kind = placement?.Kind ?? RoomChunkObjectKind.Decor;
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

            DungeonObjectRuntimeService.TryResolveLoot(session, stateKey, placementId, kind, out _);
        }

        private bool IsOpened()
        {
            var session = GameSession.Instance != null ? GameSession.Instance.State : null;
            return session != null && DungeonObjectRuntimeService.IsOpened(session, stateKey);
        }
    }
}
