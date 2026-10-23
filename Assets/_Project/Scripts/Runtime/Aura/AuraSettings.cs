using UnityEngine;

namespace FeverDream.Aura
{
    [CreateAssetMenu(menuName = "Fever Dream/Aura/Aura Settings", fileName = "SO_Aura")]
    public sealed class AuraSettings : ScriptableObject
    {
        [Header("Combo")]
        [Tooltip("Seconds without a landed hit before the combo ends and pays out.")]
        [Min(0.1f)] public float comboTimeout = 2.5f;
        [Tooltip("Combos shorter than this pay nothing, unless they include a perfect dodge.")]
        [Min(1)] public int minHitsToAward = 2;

        [Header("Bonus Points")]
        [Min(0f)] public float aerialHitBonus = 4f;
        [Min(0f)] public float launcherBonus = 10f;
        [Min(0f)] public float finisherBonus = 60f;
        [Min(0f)] public float slamBonus = 15f;
        [Min(0f)] public float killBonus = 25f;
        [Min(0f)] public float perfectDodgeBonus = 40f;

        [Header("Variety Multiplier")]
        [Tooltip("Added to the multiplier for every different move used in the combo after the first.")]
        [Min(0f)] public float varietyPerUniqueMove = 0.2f;
        [Min(1f)] public float maxMultiplier = 3f;

        [Header("Alter Ego Tiers (total Aura)")]
        [Min(0)] public int uneasyThreshold = 600;
        [Min(0)] public int panicThreshold = 1500;
    }
}
