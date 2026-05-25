using Conn.Runtime.World;
using Conn.UI.Runtime;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Conn.Rendering.Interaction
{
    public sealed class PlayerWorldInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float range = 4.0f;

        private IWorldInteractable focused;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            focused = FindFocusedInteractable();
            if (focused != null && IsInteractPressed())
            {
                focused.Interact();
            }
        }

        private void OnGUI()
        {
            if (RuntimeUiSettings.UseCanvasUi && !RuntimeUiSettings.UseLegacyImguiOverlay)
            {
                return;
            }

            if (focused == null)
            {
                return;
            }

            var label = focused.CanInteract ? $"E - {focused.Prompt}" : focused.Prompt;
            GUI.Label(PromptRect(Screen.width, Screen.height), label);
        }

        public static Rect PromptRect(int screenWidth, int screenHeight)
        {
            const float margin = 16f;
            var width = Mathf.Min(360f, Mathf.Max(1f, screenWidth - margin * 2f));
            var x = Mathf.Clamp(screenWidth * 0.5f - width * 0.5f, margin, Mathf.Max(margin, screenWidth - margin - width));
            var y = Mathf.Clamp(screenHeight - 96f, margin, Mathf.Max(margin, screenHeight - margin - 32f));
            return new Rect(x, y, width, 32f);
        }

        private IWorldInteractable FindFocusedInteractable()
        {
            if (playerCamera == null)
            {
                return null;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, range))
            {
                return null;
            }

            return hit.collider.GetComponentInParent<IWorldInteractable>();
        }

        private static bool IsInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }
    }
}
