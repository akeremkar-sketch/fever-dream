using UnityEngine;

namespace FeverDream.Player
{
    /// <summary>
    /// First-person look and camera feel: yaw turns the player, pitch tilts the camera root, plus FOV kicks,
    /// wall-run tilt, crouch height and shake. Runs before movement so movement always uses this frame's yaw.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerMovement))]
    public sealed class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Camera viewCamera;

        [Header("Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
        [SerializeField, Min(0f)] private float gamepadSensitivity = 200f;
        [SerializeField, Range(10f, 89f)] private float pitchLimit = 85f;

        [Header("Field of View")]
        [SerializeField, Range(50f, 110f)] private float baseFieldOfView = 80f;
        [SerializeField] private float sprintFovBoost = 6f;
        [SerializeField] private float slideFovBoost = 10f;
        [SerializeField] private float wallRunFovBoost = 8f;
        [SerializeField] private float grappleFovBoost = 14f;
        [SerializeField, Min(0f)] private float fovSharpness = 8f;

        [Header("Motion")]
        [SerializeField] private float wallRunTilt = 10f;
        [SerializeField, Min(0f)] private float tiltSharpness = 10f;
        [SerializeField, Min(0f)] private float eyeHeightSharpness = 14f;

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
        }

        private void OnEnable()
        {
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (input.PausePressed)
            {
                SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
            }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                ApplyLook();
            }
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
            delta *= input.LookFromMouse ? mouseSensitivity : gamepadSensitivity * Time.unscaledDeltaTime;
            transform.Rotate(0f, delta.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
        }

        private void UpdateFeel(float dt)
        {
            float targetFov = baseFieldOfView;
            switch (movement.State)
            {
                case MovementState.Sliding: targetFov += slideFovBoost; break;
                case MovementState.WallRunning: targetFov += wallRunFovBoost; break;
                case MovementState.Grappling: targetFov += grappleFovBoost; break;
                case MovementState.Grounded when movement.IsSprinting: targetFov += sprintFovBoost; break;
            }
            viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, targetFov, Sharpness(fovSharpness, dt));

            roll = Mathf.Lerp(roll, movement.WallSide * wallRunTilt, Sharpness(tiltSharpness, dt));

            Vector3 rootPosition = cameraRoot.localPosition;
            rootPosition.y = Mathf.Lerp(rootPosition.y, movement.EyeHeight, Sharpness(eyeHeightSharpness, dt));
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
