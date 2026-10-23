using UnityEngine;

namespace FeverDream.Player
{
    /// <summary>Player-side combat tuning: dodge, perfect dodge, taking damage and respawning.</summary>
    [CreateAssetMenu(menuName = "Fever Dream/Player/Combat Settings", fileName = "SO_PlayerCombat")]
    public sealed class PlayerCombatSettings : ScriptableObject
    {
        [Header("Dodge")]
        [Min(0f)] public float dashSpeed = 17f;
        [Min(0f)] public float dashDuration = 0.16f;
        [Min(0f)] public float dodgeCooldown = 0.45f;
        [Min(0f)] public float dodgeInvulnerability = 0.3f;

        [Header("Perfect Dodge (son an kaçınma)")]
        [Tooltip("A hit evaded this soon after the dodge starts counts as a perfect dodge.")]
        [Min(0f)] public float perfectWindow = 0.15f;
        [Range(0.05f, 1f)] public float perfectSlowMotionScale = 0.3f;
        [Tooltip("Real-time seconds.")]
        [Min(0f)] public float perfectSlowMotionDuration = 0.7f;

        [Header("Taking Damage")]
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float hurtInvulnerability = 0.6f;
        [Min(0f)] public float knockbackUpRatio = 0.3f;
        [Min(0f)] public float hurtCameraShake = 1.6f;
        [Min(0f)] public float hurtCameraShakeDuration = 0.25f;

        [Header("Respawn")]
        [Tooltip("Real-time seconds between dying and respawning at the last checkpoint.")]
        [Min(0f)] public float deathRespawnDelay = 1.2f;
    }
}
