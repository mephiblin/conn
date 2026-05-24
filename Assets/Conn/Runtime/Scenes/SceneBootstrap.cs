using Conn.Core.Scenes;
using Conn.Runtime.Combat;
using Conn.Runtime.Session;
using UnityEngine;

namespace Conn.Runtime.Scenes
{
    public sealed class SceneBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSceneId sceneId;

        public GameSceneId SceneId
        {
            get => sceneId;
            set => sceneId = value;
        }

        private void Awake()
        {
            var session = GameSession.Instance;
            session.State.Mode = SceneFlowService.ToMode(sceneId);
            if (sceneId == GameSceneId.Combat)
            {
                CombatRuntimeService.StartTestCombat(session.State);
            }
        }
    }
}
