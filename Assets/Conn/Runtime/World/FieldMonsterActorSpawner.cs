using Conn.Core.Maps;
using Conn.Core.Session;
using Conn.Runtime.Maps;
using UnityEngine;

namespace Conn.Runtime.World
{
    public static class FieldMonsterActorSpawner
    {
        public const string RootName = "Compiled Field Monsters";

        public static int SpawnFromCompiledMap(GameSessionState session, CompiledMap compiledMap, Transform parent = null)
        {
            if (session == null || compiledMap == null || !session.Quest.HasActiveQuest)
            {
                return 0;
            }

            var root = parent != null ? parent : EnsureRoot();
            var spawned = 0;
            if (compiledMap.EncounterPlacements == null || compiledMap.EncounterPlacements.Count == 0)
            {
                return SpawnFromMapPlacements(session, compiledMap, root);
            }

            for (var i = 0; i < compiledMap.EncounterPlacements.Count; i++)
            {
                var encounterPlacement = compiledMap.EncounterPlacements[i];
                if (encounterPlacement == null || string.IsNullOrWhiteSpace(encounterPlacement.MapPlacementId))
                {
                    continue;
                }

                var placement = FindPlacement(compiledMap, encounterPlacement.MapPlacementId);
                if (placement == null || !ShouldSpawnActor(placement.Kind, encounterPlacement))
                {
                    continue;
                }

                var stateKey = string.IsNullOrWhiteSpace(encounterPlacement.StateKey)
                    ? CompiledMapDungeonRuntimeService.StateKeyFor(compiledMap, placement)
                    : encounterPlacement.StateKey;
                var useQuestTarget = encounterPlacement.RequiredForQuest || placement.Kind == MapPlacementKind.QuestTarget;
                var encounterId = useQuestTarget
                    ? CompiledMapDungeonRuntimeService.ResolveQuestEncounterId(session, encounterPlacement)
                    : string.IsNullOrWhiteSpace(encounterPlacement.EncounterId)
                        ? session.Quest.TargetEncounterId
                        : encounterPlacement.EncounterId;
                var monsterId = useQuestTarget
                    ? CompiledMapDungeonRuntimeService.ResolveQuestMonsterId(session, encounterPlacement)
                    : string.IsNullOrWhiteSpace(encounterPlacement.PrimaryMonsterId)
                        ? session.Quest.TargetMonsterId
                        : encounterPlacement.PrimaryMonsterId;

                FieldMonsterRuntimeService.RegisterAt(session, stateKey, placement.Id, encounterId, monsterId, placement.X, placement.Y);
                CreateActor(root, compiledMap, placement, stateKey, encounterId, monsterId);
                spawned++;
            }

            return spawned;
        }

        private static int SpawnFromMapPlacements(GameSessionState session, CompiledMap compiledMap, Transform root)
        {
            var spawned = 0;
            for (var i = 0; i < compiledMap.Placements.Count; i++)
            {
                var placement = compiledMap.Placements[i];
                if (!ShouldSpawnActor(placement.Kind, null))
                {
                    continue;
                }

                var stateKey = CompiledMapDungeonRuntimeService.StateKeyFor(compiledMap, placement);
                var encounterId = session.Quest.TargetEncounterId;
                var monsterId = session.Quest.TargetMonsterId;
                FieldMonsterRuntimeService.RegisterAt(session, stateKey, placement.Id, encounterId, monsterId, placement.X, placement.Y);
                CreateActor(root, compiledMap, placement, stateKey, encounterId, monsterId);
                spawned++;
            }

            return spawned;
        }

        private static Transform EnsureRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                return existing.transform;
            }

            return new GameObject(RootName).transform;
        }

        private static void CreateActor(Transform root, CompiledMap compiledMap, MapPlacement placement, string stateKey, string encounterId, string monsterId)
        {
            var actor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            actor.name = $"Field Monster - {placement.Id}";
            actor.transform.SetParent(root, false);
            actor.transform.position = WorldPosition(compiledMap, placement);
            actor.transform.localScale = new Vector3(0.8f, 1.4f, 0.8f);
            actor.tag = "Untagged";

            var collider = actor.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            var contact = actor.AddComponent<FieldMonsterContact>();
            contact.Configure(stateKey, placement.Id, encounterId, monsterId);
            var controller = actor.AddComponent<FieldMonsterActorController>();
            controller.Configure(stateKey, actor.transform.position);
            controller.SetPlayerTarget(FindPlayerTarget());
        }

        private static Transform FindPlayerTarget()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform : null;
        }

        private static Vector3 WorldPosition(CompiledMap compiledMap, MapPlacement placement)
        {
            return DungeonMapActorSpawner.WorldPosition(compiledMap, placement.X, placement.Y, 1.4f);
        }

        private static bool ShouldSpawnActor(MapPlacementKind kind, CompiledEncounterPlacement encounterPlacement)
        {
            return kind == MapPlacementKind.Monster
                || kind == MapPlacementKind.QuestTarget
                || kind == MapPlacementKind.Boss
                || (encounterPlacement != null && encounterPlacement.RequiredForQuest);
        }

        private static MapPlacement FindPlacement(CompiledMap compiledMap, string placementId)
        {
            for (var i = 0; i < compiledMap.Placements.Count; i++)
            {
                if (compiledMap.Placements[i].Id == placementId)
                {
                    return compiledMap.Placements[i];
                }
            }

            return null;
        }
    }
}
