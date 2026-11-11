using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Player
{
    /// <summary>
    /// First-person look and camera feel: yaw turns the player, pitch tilts the camera root, plus FOV kicks,
    /// wall-run tilt, crouch height and shake. Look runs before movement so movement always uses this frame's yaw;
    /// the feel runs in LateUpdate, after movement.
    /// Tuning lives in <see cref="PlayerCameraSettings"/>.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement))]
    public sealed class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private PlayerCameraSettings settings;
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Camera viewCamera;

        private PlayerInputReader input;
        private PlayerMovement movement;
        private float pitch;
        private float roll;
        private float shakeAmplitude;
        private float shakeDuration;
        private float shakeTimer;

        public Camera ViewCamera => viewCamera;
        public Transform CameraRoot => cameraRoot;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            movement = GetComponent<PlayerMovement>();
            if (settings == null)
            {
                Debug.LogError($"{name}: PlayerCamera has no PlayerCameraSettings assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            GamePause.Changed += HandlePauseChanged;
            SetCursorLocked(!GamePause.IsPaused);
        }

        private void OnDisable()
        {
            GamePause.Changed -= HandlePauseChanged;
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (!GamePause.IsPaused && Cursor.lockState == CursorLockMode.Locked)
            {
                ApplyLook();
            }
        }

        private static void HandlePauseChanged(bool paused) => SetCursorLocked(!paused);

        private void LateUpdate()
        {
            // After movement, so a slide or crouch that shrank the capsule this frame is already respected.
            UpdateFeel(Time.unscaledDeltaTime);
        }

        public void AddShake(float amplitude, float duration)
        {
            float current = shakeDuration > 0f ? shakeAmplitude * (shakeTimer / shakeDuration) : 0f;
            if (amplitude < current || duration <= 0f)
            {
                return;
            }
            shakeAmplitude = amplitude;
            shakeDuration = duration;
            shakeTimer = duration;
        }

        public void ResetView()
        {
            pitch = 0f;
            roll = 0f;
        }

        private void ApplyLook()
        {
            Vector2 delta = input.Look;
            delta *= input.LookFromMouse ? settings.mouseSensitivity : settings.gamepadSensitivity * Time.unscaledDeltaTime;
            transform.Rotate(0f, delta.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y, -settings.pitchLimit, settings.pitchLimit);
        }

        private void UpdateFeel(float dt)
        {
            float targetFov = settings.baseFieldOfView;
            switch (movement.State)
            {
                case MovementState.Sliding: targetFov += settings.slideFovBoost; break;
                case MovementState.WallRunning: targetFov += settings.wallRunFovBoost; break;
                case MovementState.Grappling: targetFov += settings.grappleFovBoost; break;
                case MovementState.Grounded when movement.IsSprinting: targetFov += settings.sprintFovBoost; break;
            }
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, targetFov, Sharpness(settings.fovSharpness, dt));

            roll = Mathf.Lerp(roll, movement.WallSide * settings.wallRunTilt, Sharpness(settings.tiltSharpness, dt));

            // Ease towards the eye height, but never above the capsule: the capsule shrinks instantly when sliding.
            Vector3 rootPosition = cameraRoot.localPosition;
            rootPosition.y = Mathf.Lerp(rootPosition.y, movement.EyeHeight, Sharpness(settings.eyeHeightSharpness, dt));
            rootPosition.y = Mathf.Min(rootPosition.y, movement.CapsuleHeight - settings.ceilingMargin);
            cameraRoot.localPosition = rootPosition;

            Vector2 shake = Vector2.zero;
            if (shakeTimer > 0f)
            {
                shakeTimer -= dt;
                float strength = shakeAmplitude * Mathf.Clamp01(shakeTimer / shakeDuration);
                shake = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * strength;
            }
            cameraRoot.localRotation = Quaternion.Euler(pitch + shake.x, shake.y, roll);
        }

        private static float Sharpness(float sharpness, float dt) => 1f - Mathf.Exp(-sharpness * dt);

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
