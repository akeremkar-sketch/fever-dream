using System.Collections.Generic;
using UnityEngine;

namespace FeverDream.Player
{
    /// <summary>Anchor the Zincirli Kama can hook onto. Active points register themselves for targeting.</summary>
    [DisallowMultipleComponent]
    public sealed class GrapplePoint : MonoBehaviour
    {
        private static readonly List<GrapplePoint> active = new List<GrapplePoint>();

        public static IReadOnlyList<GrapplePoint> Active => active;

        private void OnEnable()
        {
            active.Add(this);
        }

        private void OnDisable()
        {
            active.Remove(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            active.Clear();
        }
    }
}
