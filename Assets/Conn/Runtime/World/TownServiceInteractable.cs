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
                    TownServiceKind.Apothecary => cost > 0 ? $"{serviceName}: Buy supplies ({cost}g)" : $"{serviceName}: Buy supplies",
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
                PayForService(session, "Rested at the inn.");
                return;
            }

            if (serviceKind == TownServiceKind.Apothecary)
            {
                PayForService(session, "Bought basic supplies.");
                return;
            }

            Debug.Log($"{serviceName}: service is not implemented yet.");
        }

        private void PayForService(Core.Session.GameSessionState session, string successMessage)
        {
            if (cost > 0 && session.Gold < cost)
            {
                Debug.Log($"{serviceName}: not enough gold.");
                return;
            }

            session.Gold -= cost;
            GameSession.Instance.SaveGame();
            Debug.Log($"{serviceName}: {successMessage}");
        }
    }
}
