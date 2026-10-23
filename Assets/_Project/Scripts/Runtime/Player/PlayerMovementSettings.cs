using UnityEngine;

namespace FeverDream.Player
{
    [CreateAssetMenu(menuName = "Fever Dream/Player/Movement Settings", fileName = "SO_PlayerMovement")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [Header("Collision")]
        [Tooltip("Geometry the player can stand on, run along and climb.")]
        public LayerMask environmentMask = 1 << 8;
        [Min(0.5f)] public float standHeight = 1.8f;
        [Min(0.5f)] public float crouchHeight = 1.0f;
        [Min(0.1f)] public float standEyeHeight = 1.65f;
        [Min(0.1f)] public float crouchEyeHeight = 0.85f;

        [Header("Ground")]
        [Min(0f)] public float walkSpeed = 6.5f;
        [Min(0f)] public float sprintSpeed = 10.5f;
        [Min(0f)] public float crouchSpeed = 3.5f;
        [Min(0f)] public float groundAcceleration = 70f;
        [Min(0f)] public float groundStickSpeed = 2f;

        [Header("Air")]
        [Min(0f)] public float jumpHeight = 1.4f;
        public float gravity = -30f;
        [Min(0f)] public float terminalFallSpeed = 50f;
        [Min(0f)] public float airAcceleration = 22f;
        [Min(0f)] public float coyoteTime = 0.12f;
        [Min(0f)] public float jumpBufferTime = 0.12f;

        [Header("Slide")]
        [Tooltip("Minimum horizontal speed to start a slide instead of crouching.")]
        [Min(0f)] public float slideMinSpeed = 7.5f;
        [Min(0f)] public float slideBoost = 3f;
        [Min(0f)] public float slideFriction = 7f;
        [Min(0f)] public float slideExitSpeed = 4f;
        [Min(0f)] public float slideMinDuration = 0.25f;
        [Min(0f)] public float slideMaxDuration = 1.1f;

        [Header("Wall Run")]
        [Min(0f)] public float wallCheckDistance = 0.8f;
        [Min(0f)] public float wallRunMinSpeed = 5f;
        [Min(0f)] public float wallRunSpeed = 11f;
        [Tooltip("Required clearance below the feet before a wall run can start.")]
        [Min(0f)] public float wallRunMinHeight = 0.9f;
        public float wallRunGravity = -2.5f;
        [Tooltip("Upward speed is clamped into this range when a wall run starts, so running off an edge still carries over a gap.")]
        [Min(0f)] public float wallRunMinEntryUpSpeed = 2f;
        [Min(0f)] public float wallRunEntryUpSpeed = 2.5f;
        [Min(0f)] public float wallRunMaxDuration = 1.8f;
        [Min(0f)] public float wallStickSpeed = 2f;
        [Min(0f)] public float wallRunCooldown = 0.35f;

        [Header("Wall Jump")]
        [Min(0f)] public float wallJumpUpSpeed = 10f;
        [Min(0f)] public float wallJumpSideSpeed = 6.5f;
        [Range(0f, 1.5f)] public float wallJumpMomentumKeep = 0.9f;

        [Header("Ledge Climb (mantle)")]
        [Min(0f)] public float mantleReach = 0.7f;
        [Tooltip("Ledge height above the feet that can be climbed.")]
        [Min(0f)] public float mantleMinHeight = 0.9f;
        [Min(0f)] public float mantleMaxHeight = 2.4f;
        [Min(0.05f)] public float mantleDuration = 0.35f;
        [Min(0f)] public float mantleExitSpeed = 5f;

        [Header("Grapple (Zincirli Kama)")]
        [Min(0f)] public float grappleRange = 28f;
        [Min(0f)] public float grappleMinDistance = 3f;
        [Tooltip("Max angle between the view direction and a grapple point to target it.")]
        [Range(1f, 45f)] public float grappleAimAngle = 14f;
        [Min(0f)] public float grapplePullSpeed = 21f;
        [Min(0f)] public float grappleAcceleration = 70f;
        [Min(0f)] public float grappleReleaseDistance = 2.2f;
        [Min(0f)] public float grappleMaxDuration = 2.2f;
        [Min(0f)] public float grappleExitUpBoost = 4f;
        [Range(0f, 1.5f)] public float grappleExitVelocityKeep = 0.65f;

        [Header("Combat Assist")]
        [Tooltip("Upward speed applied on each aerial hit so air combos can continue.")]
        [Min(0f)] public float airAttackHoverSpeed = 1.5f;
    }
}
