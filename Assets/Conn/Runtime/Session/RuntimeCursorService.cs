using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Runtime.World;
using UnityEngine;

namespace Conn.Runtime.Session
{
    public static class RuntimeCursorService
    {
        public static bool PointerUiActive { get; private set; }
        public static bool ManualReleaseActive { get; private set; }

        public static void Apply(GameSceneId sceneId, GameSessionState session, bool characterPanelOpen)
        {
            var shouldShow = ShouldShowCursor(sceneId, session, characterPanelOpen);
            shouldShow |= ManualReleaseActive;
            PointerUiActive = shouldShow;
            Cursor.visible = shouldShow;
            Cursor.lockState = shouldShow ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public static void ToggleManualRelease()
        {
            ManualReleaseActive = !ManualReleaseActive;
        }

        public static void ClearManualRelease()
        {
            ManualReleaseActive = false;
        }

        public static void Release()
        {
            PointerUiActive = false;
            ManualReleaseActive = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private static bool ShouldShowCursor(GameSceneId sceneId, GameSessionState session, bool characterPanelOpen)
        {
            if (!RuntimeUiSettings.UseCanvasUi || RuntimeUiSettings.UseLegacyImguiOverlay)
            {
                return true;
            }

            if (sceneId == GameSceneId.Title || sceneId == GameSceneId.Combat || sceneId == GameSceneId.Ending)
            {
                return true;
            }

            if (sceneId == GameSceneId.Town)
            {
                return TownNpcInteractionState.IsOpen
                    || TownQuestBoardPanelState.IsOpen
                    || TownShopPanelState.Current != TownShopPanelKind.None
                    || characterPanelOpen;
            }

            if (sceneId == GameSceneId.Dungeon && session != null)
            {
                return session.Quest.ReturnAvailable && !session.Quest.ReturnPromptSeen;
            }

            return false;
        }
    }
}
