using FeverDream.Core;
using FeverDream.Player;
using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>
    /// Turta atan palyaço. Keeps its distance, strafes and lobs pies. Forces the player to close the gap
    /// with parkur and the grapple. Weak point: low health, and pies can be cut out of the air.
    /// </summary>
    public sealed class ClownThrower : EnemyBase
    {
        [SerializeField] private PieProjectile projectilePrefab;
        [SerializeField] private Transform throwPoint;

        private float strafeSign = 1f;
        private float nextStrafeSwitch;

        protected override void TickChase(PlayerContext target)
        {
            Vector3 targetPosition = target.transform.position;
            Vector3 toTarget = Flat(targetPosition - transform.position);
            float distance = toTarget.magnitude;
            Vector3 direction = distance > 0.01f ? toTarget / distance : transform.forward;
            Face(targetPosition);

            if (Time.time >= nextStrafeSwitch)
            {
                strafeSign = Random.value < 0.5f ? -1f : 1f;
                nextStrafeSwitch = Time.time + Settings.strafeSwitchInterval;
            }

            float preferred = Settings.preferredDistance;
            Vector3 intent;
            if (distance < preferred - 2f)
            {
                intent = -direction;
            }
            else if (distance > preferred + 3f)
            {
                intent = direction;
            }
            else
            {
                intent = Vector3.Cross(Vector3.up, direction) * (strafeSign * 0.6f);
            }
            MoveIntent = intent * Settings.moveSpeed;

            if (distance <= Settings.attackRange && Time.time >= NextAttackTime && HasLineOfSight(target))
            {
                TryBeginWindup();
            }
        }

        protected override void PerformAttack(PlayerContext target)
        {
            if (projectilePrefab == null)
            {
                return;
            }

            Vector3 origin = throwPoint != null ? throwPoint.position : transform.position + Vector3.up * 1.6f;
            Vector3 aim = target.transform.position + Vector3.up * 1.2f;
            float travelTime = Vector3.Distance(origin, aim) / Settings.projectileSpeed;
            // Lead only the horizontal motion: vertical speed (ground stick, jumps) would send pies into the floor or overhead.
            Vector3 lead = target.Movement.Velocity;
            lead.y = 0f;
            aim += lead * (travelTime * Settings.aimLeadFactor);

            Vector3 velocity = (aim - origin).normalized * Settings.projectileSpeed;
            velocity.y += 0.5f * -projectilePrefab.Gravity * travelTime;

            var pie = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(velocity), RuntimeHierarchy.GetGroup("Projectiles"));
            pie.Launch(velocity, gameObject, Settings.attackDamage, Settings.attackKnockback);
        }
    }
}
