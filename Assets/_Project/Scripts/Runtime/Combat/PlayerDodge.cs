using System;
using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;

namespace FeverDream.Combat
{
    /// <summary>
    /// Short dash with invulnerability. Evading a hit within the first moments of the dash is a
    /// "son an kaçınma" (perfect dodge): slow motion and an Aura bonus.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerContext))]
    public sealed class PlayerDodge : MonoBehaviour, IEvadeProvider
    {
        [SerializeField, Min(0f)] private float dashSpeed = 17f;
        [SerializeField, Min(0f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float cooldown = 0.45f;
        [SerializeField, Min(0f)] private float invulnerability = 0.3f;

        [Header("Perfect Dodge")]
        [SerializeField, Min(0f)] private float perfectWindow = 0.15f;
        [SerializeField, Range(0.05f, 1f)] private float perfectSlowMotionScale = 0.3f;
        [Tooltip("Real-time seconds.")]
        [SerializeField, Min(0f)] private float perfectSlowMotionDuration = 0.7f;

        private PlayerContext context;
        private float lastDodgeTime = float.NegativeInfinity;
        private float dashEndTime;
        private Vector3 dashDirection;
        private bool perfectUsed;

        public event Action Dodged;
        public event Action PerfectDodged;

        public bool IsDashing => Time.time < dashEndTime;

        private void Awake()
        {
            context = GetComponent<PlayerContext>();
        }

        private void Update()
        {
            if (context.Health.IsDead)
            {
                return;
            }

            var state = context.Movement.State;
            bool canDodge = state != MovementState.Mantling && state != MovementState.Grappling;
            if (context.Input.DodgePressed && canDodge && Time.time >= lastDodgeTime + cooldown)
            {
                Begin();
            }
            if (IsDashing)
            {
                context.Movement.SetHorizontalVelocity(dashDirection * dashSpeed);
            }
        }

        public bool TryEvade()
        {
            float sinceDodge = Time.time - lastDodgeTime;
            if (sinceDodge > invulnerability)
            {
                return false;
            }

            if (sinceDodge <= perfectWindow && !perfectUsed)
            {
                perfectUsed = true;
                TimeEffects.SlowMotion(perfectSlowMotionScale, perfectSlowMotionDuration);
                context.Combo.RegisterPerfectDodge();
                PerfectDodged?.Invoke();
            }
            return true;
        }

        private void Begin()
        {
            Vector2 move = context.Input.Move;
            Vector3 direction = transform.right * move.x + transform.forward * move.y;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = -new Vector3(transform.forward.x, 0f, transform.forward.z);
            }

            dashDirection = direction.normalized;
            lastDodgeTime = Time.time;
            dashEndTime = Time.time + dashDuration;
            perfectUsed = false;
            if (!context.Movement.IsGrounded)
            {
                context.Movement.SetVerticalVelocity(Mathf.Max(context.Movement.Velocity.y, 0f));
            }
            Dodged?.Invoke();
        }
    }
}
