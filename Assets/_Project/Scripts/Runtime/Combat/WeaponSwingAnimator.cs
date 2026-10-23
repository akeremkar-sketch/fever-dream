using System;
using FeverDream.Player;
using UnityEngine;

namespace FeverDream.Combat
{
    /// <summary>
    /// Procedural first-person swings for the placeholder Tsunagi: wind-up during startup, a fast sweep at the hit
    /// frame, then settle back. Uses scaled time so hit stop freezes the blade mid-swing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponSwingAnimator : MonoBehaviour
    {
        private readonly struct Pose
        {
            public readonly Vector3 Position;
            public readonly Vector3 Euler;

            public Pose(Vector3 position, Vector3 euler)
            {
                Position = position;
                Euler = euler;
            }
        }

        [SerializeField] private TsunagiWeapon weapon;
        [SerializeField] private TrailRenderer trail;

        [Header("Idle")]
        [SerializeField] private Vector3 idlePosition = new Vector3(0.32f, -0.32f, 0.5f);
        [SerializeField] private Vector3 idleEuler = new Vector3(-20f, -12f, 0f);
        [SerializeField, Min(0f)] private float idleSharpness = 14f;
        [SerializeField, Min(0f)] private float bobAmount = 0.015f;
        [SerializeField, Min(0f)] private float bobFrequency = 9f;

        [Header("Swing")]
        [SerializeField, Min(0.01f)] private float sweepDuration = 0.08f;

        private PlayerMovement movement;
        private ComboMove move;
        private float elapsed;
        private bool playing;
        private float bobPhase;

        private void Awake()
        {
            if (weapon == null)
            {
                weapon = GetComponentInParent<TsunagiWeapon>();
            }
            movement = GetComponentInParent<PlayerMovement>();
            transform.localPosition = idlePosition;
            transform.localRotation = Quaternion.Euler(idleEuler);
            SetTrail(false);
        }

        private void OnEnable()
        {
            weapon.MoveStarted += Play;
        }

        private void OnDisable()
        {
            weapon.MoveStarted -= Play;
        }

        private void Play(ComboMove next)
        {
            move = next;
            elapsed = 0f;
            playing = true;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 restPosition = idlePosition + Bob(dt);
            if (!playing)
            {
                float blend = 1f - Mathf.Exp(-idleSharpness * dt);
                transform.localPosition = Vector3.Lerp(transform.localPosition, restPosition, blend);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(idleEuler), blend);
                SetTrail(false);
                return;
            }

            elapsed += dt;
            GetSwing(move.swing, out var from, out var to);
            float windup = Mathf.Max(move.startup, 0.0001f);
            float sweep = Mathf.Min(sweepDuration, Mathf.Max(move.recovery, 0.0001f));
            float settle = Mathf.Max(move.recovery - sweep, 0.0001f);

            Vector3 position;
            Vector3 euler;
            if (elapsed < windup)
            {
                float t = Smooth(elapsed / windup);
                position = Vector3.Lerp(idlePosition, from.Position, t);
                euler = Vector3.Lerp(idleEuler, from.Euler, t);
                SetTrail(false);
            }
            else if (elapsed < windup + sweep)
            {
                float t = 1f - Mathf.Pow(1f - (elapsed - windup) / sweep, 3f);
                position = Vector3.Lerp(from.Position, to.Position, t);
                euler = Vector3.Lerp(from.Euler, to.Euler, t);
                SetTrail(true);
            }
            else if (elapsed < windup + sweep + settle)
            {
                float t = Smooth((elapsed - windup - sweep) / settle);
                position = Vector3.Lerp(to.Position, restPosition, t);
                euler = Vector3.Lerp(to.Euler, idleEuler, t);
                SetTrail(t < 0.3f);
            }
            else
            {
                playing = false;
                return;
            }

            transform.localPosition = position;
            transform.localRotation = Quaternion.Euler(euler);
        }

        private Vector3 Bob(float dt)
        {
            if (movement == null || !movement.IsGrounded || movement.HorizontalSpeed < 1f)
            {
                return Vector3.zero;
            }
            bobPhase += dt * bobFrequency * Mathf.Clamp(movement.HorizontalSpeed / 6.5f, 0.5f, 2f);
            return new Vector3(Mathf.Cos(bobPhase) * bobAmount, Mathf.Abs(Mathf.Sin(bobPhase)) * bobAmount, 0f);
        }

        private void SetTrail(bool emitting)
        {
            if (trail != null)
            {
                trail.emitting = emitting;
            }
        }

        private static void GetSwing(SwingType swing, out Pose from, out Pose to)
        {
            switch (swing)
            {
                case SwingType.SlashRight:
                    from = new Pose(new Vector3(0.5f, -0.05f, 0.45f), new Vector3(0f, 75f, -90f));
                    to = new Pose(new Vector3(-0.45f, -0.2f, 0.5f), new Vector3(10f, -80f, -90f));
                    break;
                case SwingType.SlashLeft:
                    from = new Pose(new Vector3(-0.5f, -0.05f, 0.45f), new Vector3(0f, -75f, 90f));
                    to = new Pose(new Vector3(0.45f, -0.2f, 0.5f), new Vector3(10f, 80f, 90f));
                    break;
                case SwingType.Overhead:
                    from = new Pose(new Vector3(0.15f, 0.3f, 0.35f), new Vector3(-75f, 0f, 0f));
                    to = new Pose(new Vector3(0.05f, -0.45f, 0.55f), new Vector3(60f, 0f, 0f));
                    break;
                case SwingType.Uppercut:
                    from = new Pose(new Vector3(0.2f, -0.55f, 0.5f), new Vector3(60f, -10f, 0f));
                    to = new Pose(new Vector3(0.15f, 0.3f, 0.4f), new Vector3(-75f, -10f, 0f));
                    break;
                case SwingType.Thrust:
                    from = new Pose(new Vector3(0.3f, -0.25f, 0.1f), new Vector3(-5f, -5f, 0f));
                    to = new Pose(new Vector3(0.12f, -0.15f, 0.95f), Vector3.zero);
                    break;
                case SwingType.Slam:
                    from = new Pose(new Vector3(0.1f, 0.35f, 0.3f), new Vector3(-85f, 0f, 0f));
                    to = new Pose(new Vector3(0f, -0.5f, 0.5f), new Vector3(80f, 0f, 0f));
                    break;
                case SwingType.Spin:
                    from = new Pose(new Vector3(0.5f, -0.1f, 0.4f), new Vector3(0f, 100f, -90f));
                    to = new Pose(new Vector3(0.3f, -0.1f, 0.4f), new Vector3(0f, -260f, -90f));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(swing), swing, null);
            }
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
