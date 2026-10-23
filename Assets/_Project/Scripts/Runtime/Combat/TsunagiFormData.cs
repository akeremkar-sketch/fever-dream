using UnityEngine;

namespace FeverDream.Combat
{
    /// <summary>Move set of one Tsunagi form (Katana for the prototype).</summary>
    [CreateAssetMenu(menuName = "Fever Dream/Combat/Tsunagi Form", fileName = "SO_TsunagiForm")]
    public sealed class TsunagiFormData : ScriptableObject
    {
        public string formName = "Katana";

        [Header("Ground")]
        [Tooltip("Light attack chain on the ground.")]
        public ComboMove[] groundChain = new ComboMove[0];
        [Tooltip("Heavy attack on the ground: launches the target into the air.")]
        public ComboMove launcher = new ComboMove();
        [Tooltip("Heavy attack after a full light chain.")]
        public ComboMove finisher = new ComboMove();
        [Min(1)] public int finisherAfterLightHits = 3;

        [Header("Air")]
        [Tooltip("Light attack chain in the air. Each hit keeps the player and the target floating.")]
        public ComboMove[] airChain = new ComboMove[0];
        [Tooltip("Heavy attack in the air: dives down and hits around the player.")]
        public ComboMove airSlam = new ComboMove();
        [Min(0f)] public float slamDiveSpeed = 22f;

        [Header("Input")]
        [Min(0f)] public float inputBufferTime = 0.25f;
    }
}
