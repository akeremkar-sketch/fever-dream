using FeverDream.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FeverDream.Player
{
    /// <summary>
    /// Single place that reads the IA_Player actions. Gameplay code asks this component, never the devices.
    /// While the game is paused, and on the frame it resumes, gameplay input reads as idle; Pause and ToggleHelp still work.
    /// </summary>
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

        public Vector2 Move => GameplayBlocked ? Vector2.zero : move.ReadValue<Vector2>();
        public Vector2 Look => GameplayBlocked ? Vector2.zero : look.ReadValue<Vector2>();
        public bool LookFromMouse => look.activeControl == null || look.activeControl.device is Pointer;

        public bool JumpPressed => !GameplayBlocked && jump.WasPressedThisFrame();
        public bool SprintHeld => !GameplayBlocked && sprint.IsPressed();
        public bool SlidePressed => !GameplayBlocked && slide.WasPressedThisFrame();
        public bool SlideHeld => !GameplayBlocked && slide.IsPressed();
        public bool LightAttackPressed => !GameplayBlocked && lightAttack.WasPressedThisFrame();
        public bool HeavyAttackPressed => !GameplayBlocked && heavyAttack.WasPressedThisFrame();
        public bool DodgePressed => !GameplayBlocked && dodge.WasPressedThisFrame();
        public bool GrapplePressed => !GameplayBlocked && grapple.WasPressedThisFrame();
        public bool PausePressed => pause.WasPressedThisFrame();
        public bool ToggleHelpPressed => toggleHelp.WasPressedThisFrame();

        private static bool GameplayBlocked => GamePause.IsPaused || Time.frameCount <= GamePause.ResumedFrame;

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
