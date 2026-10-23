using UnityEngine;

namespace FeverDream.Core
{
    /// <summary>Trigger that becomes the respawn point once the player passes through it.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [Tooltip("Where the player respawns. Uses this transform when empty.")]
        [SerializeField] private Transform spawnPoint;

        public Vector3 SpawnPosition => SpawnTransform.position;
        public Quaternion SpawnRotation => Quaternion.Euler(0f, SpawnTransform.eulerAngles.y, 0f);

        private Transform SpawnTransform => spawnPoint != null ? spawnPoint : transform;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var receiver = other.GetComponentInParent<ICheckpointReceiver>();
            receiver?.SetCheckpoint(this);
        }
    }
}
