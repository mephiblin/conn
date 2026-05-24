using Conn.Core.Quests;
using Conn.Core.Session;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.World
{
    public static class TownServiceRuntimeService
    {
        public static bool Rest(GameSessionState session, int cost)
        {
            if (!Pay(session, cost, "Inn"))
            {
                return false;
            }

            session.Player.HealToFull();
            SaveIfPlaying();
            Debug.Log("Inn: Rested at the inn.");
            return true;
        }

        public static bool Train(GameSessionState session, int cost)
        {
            if (!Pay(session, cost, "Trainer"))
            {
                return false;
            }

            session.Player.MaxHp += 2;
            session.Player.HealToFull();
            SaveIfPlaying();
            Debug.Log("Trainer: Max HP increased.");
            return true;
        }

        public static string ScholarHint(GameSessionState session)
        {
            if (session.Quest.HasActiveQuest)
            {
                return $"Scholar: current target is {session.Quest.TargetMonsterId}.";
            }

            var offer = QuestCatalog.BoardOffer(session.Quest.BoardOfferIndex);
            return offer != null
                ? $"Scholar: board offer is {offer.DisplayName}."
                : "Scholar: no quest offer is available.";
        }

        private static bool Pay(GameSessionState session, int cost, string serviceName)
        {
            if (cost > 0 && session.Gold < cost)
            {
                Debug.Log($"{serviceName}: not enough gold.");
                return false;
            }

            session.Gold -= cost;
            return true;
        }

        private static void SaveIfPlaying()
        {
            if (Application.isPlaying)
            {
                GameSession.Instance.SaveGame();
            }
        }
    }
}
