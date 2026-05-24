using Conn.Runtime.World;
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
            if (focused == null)
            {
                return;
            }

            var label = focused.CanInteract ? $"E - {focused.Prompt}" : focused.Prompt;
            GUI.Label(new Rect(Screen.width * 0.5f - 180f, Screen.height - 96f, 360f, 32f), label);
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
