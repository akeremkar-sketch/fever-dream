using System;
using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Player
{
    /// <summary>Entry point for every hit on the player: evasion, short invulnerability, health, knockback and feedback.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerContext))]
    public sealed class PlayerDamageReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerCombatSettings settings;

        private PlayerContext context;
        private IEvadeProvider evade;
        private float invulnerableUntil;

        public event Action<DamageInfo> Damaged;

        private void Awake()
        {
            context = GetComponent<PlayerContext>();
            evade = GetComponent<IEvadeProvider>();
        }

        public DamageResult TakeDamage(in DamageInfo info)
        {
            if (context.Health.IsDead || Time.time < invulnerableUntil)
            {
                return DamageResult.Ignored;
            }
            if (evade != null && evade.TryEvade())
            {
                return DamageResult.Ignored;
            }

            invulnerableUntil = Time.time + settings.hurtInvulnerability;
            bool killed = context.Health.ApplyDamage(info.Amount);

            Vector3 push = new Vector3(info.Direction.x, 0f, info.Direction.z).normalized * info.Knockback;
            context.Movement.AddImpulse(push + Vector3.up * (info.Knockback * settings.knockbackUpRatio));
            context.Camera.AddShake(settings.hurtCameraShake, 0.25f);
            Damaged?.Invoke(info);
            return killed ? DamageResult.Killed : DamageResult.Hit;
        }
    }
}
