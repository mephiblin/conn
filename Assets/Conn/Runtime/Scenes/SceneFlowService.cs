using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Runtime.Session;
using Conn.Runtime.World;
using UnityEngine.SceneManagement;

namespace Conn.Runtime.Scenes
{
    public static class SceneFlowService
    {
        public static string SceneName(GameSceneId sceneId)
        {
            return sceneId.ToString();
        }

        public static void Load(GameSceneId sceneId)
        {
            GameSession.Instance.State.Mode = ToMode(sceneId);
            TownQuestBoardPanelState.Close();
            TownShopPanelState.Close();
            RuntimeCursorService.ClearManualRelease();
            RuntimeCursorService.Apply(sceneId, GameSession.Instance.State, characterPanelOpen: false);

            SceneManager.LoadScene(SceneName(sceneId));
        }

        public static GameMode ToMode(GameSceneId sceneId)
        {
            return sceneId switch
            {
                GameSceneId.Title => GameMode.Title,
                GameSceneId.Town => GameMode.Town,
                GameSceneId.Dungeon => GameMode.Dungeon,
                GameSceneId.Combat => GameMode.Combat,
                GameSceneId.Ending => GameMode.Ending,
                _ => GameMode.None
            };
        }
    }
}
