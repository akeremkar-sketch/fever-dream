using System;
using UnityEngine;

namespace FeverDream.Core
{
    /// <summary>End of the level: the dream toilet. Fires once when the player reaches it.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class LevelGoal : MonoBehaviour
    {
        public static event Action<LevelGoal> Reached;

        [SerializeField] private string playerTag = "Player";

        public bool IsReached { get; private set; }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsReached || !other.CompareTag(playerTag))
            {
                return;
            }
            // A player carried in by knockback while dead does not finish the level.
            var health = other.GetComponentInParent<Health>();
            if (health != null && health.IsDead)
            {
                return;
            }
            IsReached = true;
            Reached?.Invoke(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Reached = null;
        }
    }
}
