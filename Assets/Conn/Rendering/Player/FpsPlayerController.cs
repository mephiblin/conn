using Conn.Core.Items;
using Conn.Runtime.Equipment;
using Conn.Runtime.Inventory;
using Conn.Runtime.Session;
using Conn.Runtime.Skills;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Conn.Rendering.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FpsPlayerController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float lookSensitivity = 0.28f;
        [SerializeField] private float gravity = -18.0f;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            Look();
            Move();
            UsePotionShortcut();
            ToggleLoadoutShortcut();
            CycleSkillFaceShortcut();
        }

        private void Look()
        {
            if (playerCamera == null)
            {
                return;
            }

            var lookInput = ReadLookInput() * lookSensitivity;
            var mouseX = lookInput.x;
            var mouseY = lookInput.y;
            transform.Rotate(Vector3.up, mouseX);
            pitch = Mathf.Clamp(pitch - mouseY, -80f, 80f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void Move()
        {
            var moveInput = ReadMoveInput();
            var input = new Vector3(moveInput.x, 0f, moveInput.y);
            input = Vector3.ClampMagnitude(input, 1f);
            var worldMove = transform.TransformDirection(input) * moveSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            worldMove.y = verticalVelocity;
            controller.Move(worldMove * Time.deltaTime);
        }

        private static Vector2 ReadLookInput()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
            return Vector2.zero;
#endif
        }

        private static Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var x = ReadAxis(keyboard.aKey, keyboard.dKey);
            var y = ReadAxis(keyboard.sKey, keyboard.wKey);
            return new Vector2(x, y);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }

        private static void UsePotionShortcut()
        {
            if (!ReadUsePotionPressed())
            {
                return;
            }

            var session = GameSession.Instance.State;
            if (session.Player.Hp >= session.Player.MaxHp)
            {
                return;
            }

            ConsumableRuntimeService.Use(session, ConsumableCatalog.MinorPotionId);
        }

        private static bool ReadUsePotionPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Q);
#else
            return false;
#endif
        }

        private static void ToggleLoadoutShortcut()
        {
            if (!ReadToggleLoadoutPressed())
            {
                return;
            }

            EquipmentRuntimeService.ToggleOwnedLoadout(GameSession.Instance.State);
        }

        private static bool ReadToggleLoadoutPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }

        private static void CycleSkillFaceShortcut()
        {
            if (ReadCycleSkillFacePressed())
            {
                SkillRuntimeService.CycleNextEditFace(GameSession.Instance.State);
            }
        }

        private static bool ReadCycleSkillFacePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.T);
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }
#endif
    }
}
