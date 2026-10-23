using System;
using UnityEngine;

namespace FeverDream.Core
{
    [Flags]
    public enum AttackFlags
    {
        None = 0,
        Heavy = 1 << 0,
        Launcher = 1 << 1,
        Aerial = 1 << 2,
        Finisher = 1 << 3,
        Slam = 1 << 4,
    }

    public enum DamageResult
    {
        Ignored,
        Blocked,
        Hit,
        Killed,
    }

    public struct DamageInfo
    {
        public float Amount;
        public Vector3 Point;
        /// <summary>Direction the hit travels, from the attacker towards the target.</summary>
        public Vector3 Direction;
        public float Knockback;
        public float LaunchForce;
        public AttackFlags Flags;
        public GameObject Source;

        public readonly bool Has(AttackFlags flag) => (Flags & flag) != 0;
    }

    public interface IDamageable
    {
        DamageResult TakeDamage(in DamageInfo info);
    }
}
