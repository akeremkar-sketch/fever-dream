using System;
using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;

namespace FeverDream.Combat
{
    /// <summary>
    /// Short dash with invulnerability. Evading a hit within the first moments of the dash is a
    /// "son an kaçınma" (perfect dodge): slow motion and an Aura bonus. Tuning lives in <see cref="PlayerCombatSettings"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerContext))]
    public sealed class PlayerDodge : MonoBehaviour, IEvadeProvider
    {
        [SerializeField] private PlayerCombatSettings settings;

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
            if (settings == null)
            {
                Debug.LogError($"{name}: PlayerDodge has no PlayerCombatSettings assigned.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (context.Health.IsDead)
            {
                return;
            }

            var state = context.Movement.State;
            bool canDodge = state != MovementState.Mantling && state != MovementState.Grappling;
            if (context.Input.DodgePressed && canDodge && Time.time >= lastDodgeTime + settings.dodgeCooldown)
            {
                Begin();
            }
            if (IsDashing)
            {
                context.Movement.SetHorizontalVelocity(dashDirection * settings.dashSpeed);
            }
        }

        public bool TryEvade()
        {
            if (!enabled)
            {
                return false;
            }
            float sinceDodge = Time.time - lastDodgeTime;
            if (sinceDodge > settings.dodgeInvulnerability)
            {
                return false;
            }

            if (sinceDodge <= settings.perfectWindow && !perfectUsed)
            {
                perfectUsed = true;
                TimeEffects.SlowMotion(settings.perfectSlowMotionScale, settings.perfectSlowMotionDuration);
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
            context.Movement.CancelLunge();
            lastDodgeTime = Time.time;
            dashEndTime = Time.time + settings.dashDuration;
            perfectUsed = false;
            if (!context.Movement.IsGrounded)
            {
                context.Movement.SetVerticalVelocity(Mathf.Max(context.Movement.Velocity.y, 0f));
            }
            Dodged?.Invoke();
        }
    }
}
