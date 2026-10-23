using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>
    /// Clown pie. Lives on the Projectile layer, which only collides with the player and the environment,
    /// so pies never hit other enemies. Tsunagi can cut it out of the air.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class PieProjectile : MonoBehaviour, IDamageable
    {
        [SerializeField] private float gravity = -5f;
        [SerializeField, Min(0.1f)] private float lifetime = 6f;

        private Rigidbody body;
        private GameObject owner;
        private float damage;
        private float knockback;
        private bool spent;

        public float Gravity => gravity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
        }

        public void Launch(Vector3 velocity, GameObject source, float hitDamage, float hitKnockback)
        {
            owner = source;
            damage = hitDamage;
            knockback = hitKnockback;
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
