using UnityEngine;

namespace FeverDream.Core
{
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
}
