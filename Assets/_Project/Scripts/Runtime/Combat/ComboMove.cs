using System;
using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Combat
{
    /// <summary>One attack of a Tsunagi form. Timing: startup (wind-up) → hit → recovery → chain window.</summary>
    [Serializable]
    public sealed class ComboMove
    {
        [Tooltip("Unique id. Used to count different moves for the Aura variety multiplier.")]
        public string id = "move";
        public string displayName = "Kesik";
        public SwingType swing = SwingType.SlashRight;
        public AttackFlags flags = AttackFlags.None;

        [Header("Damage")]
        [Min(0f)] public float damage = 10f;
        [Min(0f)] public float knockback = 3f;
        [Tooltip("Upward speed given to the target. Above zero launches it into the air.")]
        [Min(0f)] public float launchForce;

        [Header("Timing (seconds)")]
        [Min(0f)] public float startup = 0.08f;
        [Min(0f)] public float recovery = 0.22f;
        [Tooltip("Time after recovery in which the next input continues the chain.")]
        [Min(0f)] public float chainWindow = 0.45f;

        [Header("Reach")]
        [Min(0f)] public float range = 2.6f;
        [Min(0f)] public float radius = 0.9f;
        [Tooltip("Forward step in metres when the move starts on the ground (approximate). Air moves never lunge.")]
        [Min(0f)] public float lunge = 0.4f;

        [Header("Feel")]
        [Min(0f)] public float hitStop = 0.05f;
        [Min(0f)] public float cameraShake = 0.6f;

        [Header("Aura")]
        [Min(0f)] public float auraValue = 10f;
    }
}
