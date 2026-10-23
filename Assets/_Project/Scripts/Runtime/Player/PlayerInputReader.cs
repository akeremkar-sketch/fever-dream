using UnityEngine;
using UnityEngine.InputSystem;

namespace FeverDream.Player
{
    /// <summary>Single place that reads the IA_Player actions. Gameplay code asks this component, never the devices.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMapName = "Player";

        private InputAction move;
        private InputAction look;
        private InputAction jump;
        private InputAction sprint;
        private InputAction slide;
        private InputAction lightAttack;
        private InputAction heavyAttack;
        private InputAction dodge;
        private InputAction grapple;
        private InputAction pause;
        private InputAction toggleHelp;

        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 Look => look.ReadValue<Vector2>();
        public bool LookFromMouse => look.activeControl == null || look.activeControl.device is Pointer;

        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool SprintHeld => sprint.IsPressed();
        public bool SlidePressed => slide.WasPressedThisFrame();
        public bool SlideHeld => slide.IsPressed();
        public bool LightAttackPressed => lightAttack.WasPressedThisFrame();
        public bool HeavyAttackPressed => heavyAttack.WasPressedThisFrame();
        public bool DodgePressed => dodge.WasPressedThisFrame();
        public bool GrapplePressed => grapple.WasPressedThisFrame();
        public bool PausePressed => pause.WasPressedThisFrame();
        public bool ToggleHelpPressed => toggleHelp.WasPressedThisFrame();

        private void Awake()
        {
            var map = actions.FindActionMap(actionMapName, true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            jump = map.FindAction("Jump", true);
            sprint = map.FindAction("Sprint", true);
            slide = map.FindAction("Slide", true);
            lightAttack = map.FindAction("LightAttack", true);
            heavyAttack = map.FindAction("HeavyAttack", true);
            dodge = map.FindAction("Dodge", true);
            grapple = map.FindAction("Grapple", true);
            pause = map.FindAction("Pause", true);
            toggleHelp = map.FindAction("ToggleHelp", true);
        }

        private void OnEnable()
        {
            actions.FindActionMap(actionMapName, true).Enable();
        }

        private void OnDisable()
        {
            actions.FindActionMap(actionMapName, true).Disable();
        }
    }
}
