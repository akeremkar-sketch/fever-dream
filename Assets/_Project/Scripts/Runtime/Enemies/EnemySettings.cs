using UnityEngine;

namespace FeverDream.Enemies
{
    [CreateAssetMenu(menuName = "Fever Dream/Enemies/Enemy Settings", fileName = "SO_Enemy")]
    public sealed class EnemySettings : ScriptableObject
    {
        public string displayName = "Düşman";

        [Header("Health")]
        [Min(1f)] public float maxHealth = 40f;

        [Header("Movement")]
        [Min(0f)] public float moveSpeed = 3.5f;
        [Min(0f)] public float acceleration = 20f;
        [Tooltip("How fast knockback slides to a stop.")]
        [Min(0f)] public float groundFriction = 14f;
        [Min(0f)] public float turnSpeed = 540f;
        [Min(0f)] public float detectionRange = 22f;
        public float gravity = -25f;

        [Header("Attack")]
        [Min(0f)] public float attackRange = 2.2f;
        [Tooltip("Extra reach in which a dodging player still counts as evading the attack (perfect dodge).")]
        [Min(0f)] public float dodgeLeniency = 2.5f;
        [Tooltip("Telegraph time before the hit lands.")]
        [Min(0f)] public float attackWindup = 0.75f;
        [Min(0f)] public float attackRecovery = 0.6f;
        [Min(0f)] public float attackCooldown = 1.5f;
        [Min(0f)] public float attackDamage = 12f;
        [Min(0f)] public float attackKnockback = 7f;

        [Header("Hit Reactions")]
        [Min(0f)] public float staggerDuration = 0.35f;
        [Range(0f, 1f)] public float knockbackResistance;
        [Range(0f, 1f)] public float launchResistance;
        [Tooltip("Upward speed when hit in the air (juggle).")]
        [Min(0f)] public float juggleUpSpeed = 2.5f;
        [Min(0f)] public float airHoldDuration = 0.6f;
        [Range(0f, 1f)] public float airHoldGravityScale = 0.1f;

        [Header("Ranged")]
        [Min(0f)] public float preferredDistance = 11f;
        [Min(0f)] public float projectileSpeed = 15f;
    }
}
