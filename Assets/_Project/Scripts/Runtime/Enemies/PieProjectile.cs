using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>
    /// Clown pie. Lives on the Projectile layer, which only collides with the player and the environment,
    /// so pies never hit other enemies. Tsunagi can cut it out of the air. Its flight values come from the
    /// thrower's <see cref="EnemySettings"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class PieProjectile : MonoBehaviour, IDamageable
    {
        private Rigidbody body;
        private GameObject owner;
        private float damage;
        private float knockback;
        private float gravity;
        private bool spent;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
        }

        public void Launch(Vector3 velocity, GameObject source, float hitDamage, float hitKnockback, float flightGravity, float lifetime)
        {
            owner = source;
            damage = hitDamage;
            knockback = hitKnockback;
            gravity = flightGravity;
            body.linearVelocity = velocity;
            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            body.linearVelocity += Vector3.up * (gravity * Time.fixedDeltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Checkpoints, kill volumes and the goal are triggers; pies fly through them.
            if (spent || other.isTrigger || (owner != null && other.transform.IsChildOf(owner.transform)))
            {
                return;
            }

            var target = other.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                Vector3 direction = body.linearVelocity;
                direction.y = 0f;
                target.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Point = transform.position,
                    Direction = direction.normalized,
                    Knockback = knockback,
                    Source = owner,
                });
            }
            Splat();
        }

        public DamageResult TakeDamage(in DamageInfo info)
        {
            if (spent)
            {
                return DamageResult.Ignored;
            }
            Splat();
            // Cutting a pie protects the player but is not a combo hit (no Aura farming on projectiles).
            return DamageResult.Deflected;
        }

        private void Splat()
        {
            spent = true;
            Destroy(gameObject);
        }
    }
}
