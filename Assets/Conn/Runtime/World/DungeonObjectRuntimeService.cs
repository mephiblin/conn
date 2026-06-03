using Conn.Core.Maps;
using Conn.Core.Session;
using Conn.Core.World;
using Conn.Runtime.Session;

namespace Conn.Runtime.World
{
    public static class DungeonObjectRuntimeService
    {
        public static string StateKeyFor(string placementId, string runtimeReferenceId)
        {
            if (!string.IsNullOrWhiteSpace(runtimeReferenceId))
            {
                return $"dungeon_object:{runtimeReferenceId}";
            }

            return $"dungeon_object:{placementId}";
        }

        public static bool IsOpened(GameSessionState session, string stateKey)
        {
            var state = session?.World?.FindDungeonObject(stateKey);
            return state != null && state.Opened;
        }

        public static DungeonObjectState Register(GameSessionState session, string stateKey, string placementId)
        {
            return session.World.GetOrCreateDungeonObject(stateKey, placementId);
        }

        public static bool TryResolveLoot(
            GameSessionState session,
            string stateKey,
            string placementId,
            RoomChunkObjectKind kind,
            out string notice)
        {
            notice = string.Empty;
            if (session == null || !CanLoot(kind))
            {
                return false;
            }

            var state = Register(session, stateKey, placementId);
            if (state.Opened)
            {
                notice = kind == RoomChunkObjectKind.Chest ? "Chest already opened." : "Barrel already broken.";
                RuntimeNoticeService.Set(session, notice);
                return false;
            }

            var baseGold = BaseGold(kind);
            var bonusGold = ObjectiveClearBonusGold(session, kind);
            var riskDamage = ObjectiveClearRiskDamage(session, kind);
            var totalGold = baseGold + bonusGold;

            state.Opened = true;
            state.GoldCollected = totalGold;
            state.RiskDamageTaken = riskDamage;
            session.Gold += totalGold;
            if (riskDamage > 0)
            {
                session.Player.Damage(riskDamage);
            }

            notice = LootNotice(kind, placementId, totalGold, bonusGold, riskDamage);
            RuntimeNoticeService.Set(session, notice);
            return true;
        }

        public static string ExpeditionLootStatus(GameSessionState session)
        {
            if (session?.World?.DungeonObjects == null || session.World.DungeonObjects.Count == 0)
            {
                return "Loot: none opened";
            }

            var opened = 0;
            var gold = 0;
            var risk = 0;
            for (var i = 0; i < session.World.DungeonObjects.Count; i++)
            {
                var state = session.World.DungeonObjects[i];
                if (!state.Opened)
                {
                    continue;
                }

                opened++;
                gold += state.GoldCollected;
                risk += state.RiskDamageTaken;
            }

            return opened > 0 ? $"Loot: {opened} opened, +{gold}g, {risk} risk damage" : "Loot: none opened";
        }

        private static bool CanLoot(RoomChunkObjectKind kind)
        {
            return kind == RoomChunkObjectKind.Chest || kind == RoomChunkObjectKind.Barrel;
        }

        private static int BaseGold(RoomChunkObjectKind kind)
        {
            return kind == RoomChunkObjectKind.Chest ? 10 : kind == RoomChunkObjectKind.Barrel ? 3 : 0;
        }

        private static int ObjectiveClearBonusGold(GameSessionState session, RoomChunkObjectKind kind)
        {
            if (session.Quest == null || !session.Quest.ReturnAvailable)
            {
                return 0;
            }

            return kind == RoomChunkObjectKind.Chest ? 5 : 2;
        }

        private static int ObjectiveClearRiskDamage(GameSessionState session, RoomChunkObjectKind kind)
        {
            if (session.Quest == null || !session.Quest.ReturnAvailable)
            {
                return 0;
            }

            return kind == RoomChunkObjectKind.Chest ? 2 : 1;
        }

        private static string LootNotice(RoomChunkObjectKind kind, string placementId, int totalGold, int bonusGold, int riskDamage)
        {
            var action = kind == RoomChunkObjectKind.Chest ? $"Opened chest {placementId}" : $"Broke barrel {placementId}";
            var notice = $"{action}. Found {totalGold}g.";
            if (bonusGold > 0 || riskDamage > 0)
            {
                notice += $" Lingering danger: +{bonusGold}g bonus, took {riskDamage} damage.";
            }

            return notice;
        }
    }
}
