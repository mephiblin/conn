using Conn.Core.Scenes;
using Conn.Runtime.Combat;
using Conn.Core.Content;
using Conn.Core.Maps;
using Conn.MapGenV2.Core;
using Conn.Runtime.Content;
using Conn.Runtime.Maps;
using Conn.Runtime.Session;
using Conn.Runtime.World;
using UnityEngine;

namespace Conn.Runtime.Scenes
{
    public sealed class SceneBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSceneId sceneId;
        [SerializeField] private ContentDatabaseDefinition contentDatabase;
        [SerializeField] private CompiledMapAsset[] compiledMaps = System.Array.Empty<CompiledMapAsset>();
        [SerializeField] private MapGenBakedMapAsset[] mapGenV2BakedMaps = System.Array.Empty<MapGenBakedMapAsset>();
        [SerializeField] private RuntimeMapGenerationBundleAsset[] runtimeMapGenerationBundles = System.Array.Empty<RuntimeMapGenerationBundleAsset>();

        public GameSceneId SceneId
        {
            get => sceneId;
            set => sceneId = value;
        }

        public ContentDatabaseDefinition ContentDatabase
        {
            get => contentDatabase;
            set => contentDatabase = value;
        }

        public CompiledMapAsset[] CompiledMaps
        {
            get => compiledMaps;
            set => compiledMaps = value;
        }

        public MapGenBakedMapAsset[] MapGenV2BakedMaps
        {
            get => mapGenV2BakedMaps;
            set => mapGenV2BakedMaps = value;
        }

        public RuntimeMapGenerationBundleAsset[] RuntimeMapGenerationBundles
        {
            get => runtimeMapGenerationBundles;
            set => runtimeMapGenerationBundles = value;
        }

        private void Awake()
        {
            RuntimeContentDatabase.SetActive(contentDatabase);
            CompiledMapDungeonRuntimeService.SetCompiledMapAssets(compiledMaps);
            CompiledMapDungeonRuntimeService.SetMapGenV2BakedMaps(mapGenV2BakedMaps);
            CompiledMapDungeonRuntimeService.SetRuntimeMapGenerationBundles(runtimeMapGenerationBundles);
            var session = GameSession.Instance;
            session.State.Mode = SceneFlowService.ToMode(sceneId);
            if (sceneId == GameSceneId.Combat)
            {
                CombatRuntimeService.StartTestCombat(session.State);
            }
            else if (sceneId == GameSceneId.Dungeon)
            {
                var compiledMap = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session.State);
                var mapCells = DungeonMapActorSpawner.SpawnFromCompiledMap(compiledMap);
                var placedPlayer = DungeonMapActorSpawner.PlacePlayerAtStart(compiledMap);
                CompiledMapDungeonRuntimeService.RegisterQuestTargetFieldMonster(session.State, compiledMap);
                var fieldMonsters = FieldMonsterActorSpawner.SpawnFromCompiledMap(session.State, compiledMap);
                var dungeonObjects = DungeonObjectActorSpawner.SpawnFromCompiledMap(compiledMap);
                DungeonVisualDebugOverlay.SpawnForCompiledMap(compiledMap);
                Debug.Log(
                    $"Dungeon map loaded: {compiledMap?.MapId} profile={compiledMap?.ProfileId} quest={session.State.Quest.ActiveQuestId} cells={mapCells} monsters={fieldMonsters} objects={dungeonObjects} playerPlaced={placedPlayer}");
            }
        }
    }
}
