using Conn.Core.Scenes;
using Conn.Runtime.Combat;
using Conn.Core.Content;
using Conn.Runtime.Content;
using Conn.Runtime.Maps;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.Scenes
{
    public sealed class SceneBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSceneId sceneId;
        [SerializeField] private ContentDatabaseDefinition contentDatabase;

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

        private void Awake()
        {
            RuntimeContentDatabase.SetActive(contentDatabase);
            var session = GameSession.Instance;
            session.State.Mode = SceneFlowService.ToMode(sceneId);
            if (sceneId == GameSceneId.Combat)
            {
                CombatRuntimeService.StartTestCombat(session.State);
            }
            else if (sceneId == GameSceneId.Dungeon)
            {
                var compiledMap = CompiledMapDungeonRuntimeService.BuildQuestCompiledMap(session.State);
                CompiledMapDungeonRuntimeService.RegisterQuestTargetFieldMonster(session.State, compiledMap);
            }
        }
    }
}
