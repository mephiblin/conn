using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public sealed class TownServiceInteractable : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private string serviceName = "Town Service";
        [SerializeField] private TownServiceKind serviceKind;
        [SerializeField] private int cost;

        public string Prompt
        {
            get
            {
                return serviceKind switch
                {
                    TownServiceKind.Inn => EffectiveCost > 0 ? $"{serviceName}: Rest ({EffectiveCost}g)" : $"{serviceName}: Rest",
                    TownServiceKind.Trainer => EffectiveCost > 0 ? $"{serviceName}: Train Max HP ({EffectiveCost} XP)" : $"{serviceName}: Train Max HP",
                    TownServiceKind.Apothecary => $"{serviceName}: Buy {ApothecaryItemName} ({EffectiveCost}g)",
                    TownServiceKind.Scholar => $"{serviceName}: Ask about quest",
                    _ => $"{serviceName}: Talk"
                };
            }
        }

        public bool CanInteract => true;

        public string ServiceName
        {
            get => serviceName;
            set => serviceName = value;
        }

        public TownServiceKind ServiceKind
        {
            get => serviceKind;
            set => serviceKind = value;
        }

        public int Cost
        {
            get => cost;
            set => cost = value;
        }

        private int EffectiveCost => TownServiceRuntimeService.CostFor(serviceKind, cost);

        public void Interact()
        {
            var session = GameSession.Instance.State;
            var itemId = serviceKind == TownServiceKind.Apothecary
                ? TownServiceRuntimeService.FirstConsumableStockIdFor(serviceKind)
                : string.Empty;
            var dialogue = DialogueFor(session);
            TownNpcInteractionState.Open(NpcKindFor(serviceKind), serviceName, dialogue, serviceKind, EffectiveCost, itemId);
            RuntimeNoticeService.Set(session, dialogue);
        }

        private string ApothecaryItemName
        {
            get
            {
                var itemId = TownServiceRuntimeService.FirstConsumableStockIdFor(serviceKind);
                var item = Conn.Runtime.Content.RuntimeContentDatabase.FindConsumable(itemId);
                return item != null ? item.DisplayName : itemId;
            }
        }

        private TownNpcInteractionKind NpcKindFor(TownServiceKind kind)
        {
            return kind switch
            {
                TownServiceKind.Inn => TownNpcInteractionKind.Inn,
                TownServiceKind.Trainer => TownNpcInteractionKind.Trainer,
                TownServiceKind.Apothecary => TownNpcInteractionKind.Apothecary,
                TownServiceKind.Scholar => TownNpcInteractionKind.Scholar,
                _ => TownNpcInteractionKind.None
            };
        }

        private string DialogueFor(Conn.Core.Session.GameSessionState session)
        {
            return serviceKind switch
            {
                TownServiceKind.Inn => EffectiveCost > 0
                    ? $"{serviceName}: rest here for {EffectiveCost}g."
                    : $"{serviceName}: rest here.",
                TownServiceKind.Trainer => EffectiveCost > 0
                    ? $"{serviceName}: train Max HP for {EffectiveCost} XP."
                    : $"{serviceName}: train Max HP.",
                TownServiceKind.Apothecary => $"{serviceName}: buy {ApothecaryItemName} for {EffectiveCost}g.",
                TownServiceKind.Scholar => TownServiceRuntimeService.ScholarHint(session),
                _ => $"{serviceName}: service is not implemented yet."
            };
        }
    }
}
