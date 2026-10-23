using UnityEngine;

namespace FeverDream.Player
{
    [CreateAssetMenu(menuName = "Fever Dream/Player/Camera Settings", fileName = "SO_PlayerCamera")]
    public sealed class PlayerCameraSettings : ScriptableObject
    {
        [Header("Look")]
        [Tooltip("Degrees per pixel of mouse movement.")]
        [Min(0f)] public float mouseSensitivity = 0.1f;
        [Tooltip("Degrees per second at full stick deflection.")]
        [Min(0f)] public float gamepadSensitivity = 200f;
        [Range(10f, 89f)] public float pitchLimit = 85f;

        [Header("Field of View")]
        [Range(50f, 110f)] public float baseFieldOfView = 80f;
        public float sprintFovBoost = 6f;
        public float slideFovBoost = 10f;
        public float wallRunFovBoost = 8f;
        public float grappleFovBoost = 14f;
        [Min(0f)] public float fovSharpness = 8f;

        [Header("Motion")]
        public float wallRunTilt = 10f;
        [Min(0f)] public float tiltSharpness = 10f;
        [Min(0f)] public float eyeHeightSharpness = 14f;
        [Tooltip("The eye always stays this far below the top of the capsule, so a crouch or slide never shows the inside of a ceiling.")]
        [Min(0f)] public float ceilingMargin = 0.15f;
    }
}
