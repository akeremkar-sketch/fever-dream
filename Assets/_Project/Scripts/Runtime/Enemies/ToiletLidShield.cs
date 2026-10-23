using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>
    /// Klozet kapaklı kalkan. Blocks light attacks from the front with a toilet lid.
    /// Heavy attacks from the front (launcher, finisher, slam) break the guard for a while;
    /// light hits from behind or from the air pass. Tuning lives in <see cref="EnemySettings"/>.
    /// </summary>
    public sealed class ToiletLidShield : EnemyBase
    {
        private const AttackFlags GuardBreakers = AttackFlags.Heavy | AttackFlags.Launcher | AttackFlags.Finisher | AttackFlags.Slam;

        [SerializeField] private Transform lid;
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
            if (!GuardUp)
            {
                return false;
            }

            Vector3 towardAttacker = -Flat(info.Direction).normalized;
            if (Vector3.Dot(transform.forward, towardAttacker) <= Settings.guardFrontArc)
            {
                return false;
            }
            // Heavy attacks are checked before the aerial bypass: the air slam is always aerial and must still break the lid.
            if ((info.Flags & GuardBreakers) != 0)
            {
                guardBrokenUntil = Time.time + Settings.guardBreakDuration;
                return false;
            }
            return !info.Has(AttackFlags.Aerial);
        }
    }
}
