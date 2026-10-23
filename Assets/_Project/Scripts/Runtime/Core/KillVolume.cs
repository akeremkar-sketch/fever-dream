using UnityEngine;

namespace FeverDream.Core
{
    /// <summary>Trigger below the level. Sends anything that enters it back via <see cref="IVoidListener"/>.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class KillVolume : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var listener = other.GetComponentInParent<IVoidListener>();
            listener?.OnFellIntoVoid();
        }
    }
}
