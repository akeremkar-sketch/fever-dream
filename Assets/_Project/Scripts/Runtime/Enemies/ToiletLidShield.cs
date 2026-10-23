using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>
    /// Klozet kapaklı kalkan. Blocks light attacks from the front with a toilet lid.
    /// Heavy attacks (launcher, finisher, slam) break the guard for a while; hits from behind or from the air pass.
    /// </summary>
    public sealed class ToiletLidShield : EnemyBase
    {
        private const AttackFlags GuardBreakers = AttackFlags.Heavy | AttackFlags.Launcher | AttackFlags.Finisher | AttackFlags.Slam;

        [SerializeField] private Transform lid;
        [SerializeField, Min(0f)] private float guardBreakDuration = 2.5f;
        [Tooltip("Dot product threshold for a hit to count as coming from the front.")]
        [SerializeField, Range(-1f, 1f)] private float frontArc = 0.3f;
        [SerializeField] private Vector3 lidGuardEuler = Vector3.zero;
        [SerializeField] private Vector3 lidBrokenEuler = new Vector3(-110f, 0f, 0f);

        private float guardBrokenUntil;

        public bool GuardUp => !IsDead && Time.time >= guardBrokenUntil;

        protected override void Update()
        {
            base.Update();
            if (lid != null)
            {
                var target = Quaternion.Euler(GuardUp ? lidGuardEuler : lidBrokenEuler);
                lid.localRotation = Quaternion.Slerp(lid.localRotation, target, 1f - Mathf.Exp(-15f * Time.deltaTime));
            }
        }

        protected override bool IsBlocking(in DamageInfo info)
        {
            if (!GuardUp || info.Has(AttackFlags.Aerial))
            {
                return false;
            }

            Vector3 towardAttacker = -Flat(info.Direction).normalized;
            if (Vector3.Dot(transform.forward, towardAttacker) <= frontArc)
            {
                return false;
            }
            if ((info.Flags & GuardBreakers) != 0)
            {
                guardBrokenUntil = Time.time + guardBreakDuration;
                return false;
            }
            return true;
        }
    }
}
