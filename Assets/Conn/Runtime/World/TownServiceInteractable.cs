using Conn.Core.Items;
using Conn.Runtime.Inventory;
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
                    TownServiceKind.Inn => cost > 0 ? $"{serviceName}: Rest ({cost}g)" : $"{serviceName}: Rest",
                    TownServiceKind.Trainer => cost > 0 ? $"{serviceName}: Train Max HP ({cost} XP)" : $"{serviceName}: Train Max HP",
                    TownServiceKind.Apothecary => $"{serviceName}: Buy {ConsumableCatalog.Find(ConsumableCatalog.MinorPotionId)?.DisplayName} ({cost}g)",
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

        public void Interact()
        {
            var session = GameSession.Instance.State;
            if (serviceKind == TownServiceKind.Inn)
            {
                TownServiceRuntimeService.Rest(session, cost);
                return;
            }

            if (serviceKind == TownServiceKind.Trainer)
            {
                TownServiceRuntimeService.Train(session, cost);
                return;
            }

            if (serviceKind == TownServiceKind.Apothecary)
            {
                ConsumableRuntimeService.Buy(session, ConsumableCatalog.MinorPotionId);
                return;
            }

            if (serviceKind == TownServiceKind.Scholar)
            {
                RuntimeNoticeService.Set(session, TownServiceRuntimeService.ScholarHint(session));
                return;
            }

            RuntimeNoticeService.Set(session, $"{serviceName}: service is not implemented yet.");
        }
    }
}
