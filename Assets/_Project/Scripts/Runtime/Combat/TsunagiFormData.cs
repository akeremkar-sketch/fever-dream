using UnityEngine;

namespace FeverDream.Combat
{
    /// <summary>Move set of one Tsunagi form (Katana for the prototype).</summary>
    [CreateAssetMenu(menuName = "Fever Dream/Combat/Tsunagi Form", fileName = "SO_TsunagiForm")]
    public sealed class TsunagiFormData : ScriptableObject
    {
        public string formName = "Katana";

        [Header("Ground")]
        [Tooltip("Light attack chain on the ground. Needs at least one move.")]
        public ComboMove[] groundChain = new ComboMove[0];
        [Tooltip("Heavy attack on the ground: launches the target into the air.")]
        public ComboMove launcher = new ComboMove();
        [Tooltip("Heavy attack after a full light chain.")]
        public ComboMove finisher = new ComboMove();
        [Min(1)] public int finisherAfterLightHits = 3;

        [Header("Air")]
        [Tooltip("Light attack chain in the air. Each landed hit keeps the player and the target floating.")]
        public ComboMove[] airChain = new ComboMove[0];
        [Tooltip("Heavy attack in the air: dives down and hits around the player.")]
        public ComboMove airSlam = new ComboMove();
        [Min(0f)] public float slamDiveSpeed = 22f;

        [Header("Feel")]
        [Tooltip("Time scale during hit stop. Each move sets its own hit stop duration.")]
        [Range(0.01f, 1f)] public float hitStopTimeScale = 0.05f;
        [Tooltip("Seconds the camera shakes on a landed hit. Each move sets its own shake strength.")]
        [Min(0f)] public float cameraShakeDuration = 0.12f;
        [Tooltip("Seconds over which a move's ground lunge distance is covered.")]
        [Min(0.01f)] public float lungeDuration = 0.08f;

        [Header("Input")]
        [Min(0f)] public float inputBufferTime = 0.25f;

        private void OnValidate()
        {
            if (groundChain == null || groundChain.Length == 0)
            {
                Debug.LogWarning($"{name}: the ground chain is empty, light attacks on the ground will use the launcher.", this);
            }
        }
    }
}
