using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>
    /// Fight area for the enemies below it in the hierarchy. They engage when the player enters the box, keep fighting
    /// while the player stays within <see cref="DisengageMargin"/> of it, and never walk out of it. It also limits how
    /// many of its enemies may attack at once. Resize the trigger box to fit the arena.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EnemyArena : MonoBehaviour
    {
        [Tooltip("How many enemies of this arena may wind up or recover from an attack at the same time.")]
        [SerializeField, Min(1)] private int maxSimultaneousAttackers = 2;
        [Tooltip("Enemies keep fighting while the player is this close to the box (no free hits from a doorway).")]
        [SerializeField, Min(0f)] private float disengageMargin = 4f;

        private BoxCollider area;
        private int activeAttackers;

        public float DisengageMargin => disengageMargin;

        private void Awake()
        {
            area = GetComponent<BoxCollider>();
            area.isTrigger = true;
        }

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        public bool Contains(Vector3 worldPoint, float padding = 0f) => OutsideDistance(worldPoint) <= padding;

        /// <summary>A move is allowed when it ends inside, or brings an enemy that was knocked out closer to the box.</summary>
        public bool AllowsStep(Vector3 from, Vector3 to)
        {
            float target = OutsideDistance(to);
            return target <= 0f || target < OutsideDistance(from);
        }

        /// <summary>Distance from the point to the box in its local space; zero inside.</summary>
        public float OutsideDistance(Vector3 worldPoint)
        {
            if (area == null)
            {
                area = GetComponent<BoxCollider>();
            }
            Vector3 local = transform.InverseTransformPoint(worldPoint) - area.center;
            Vector3 half = area.size * 0.5f;
            Vector3 excess = new Vector3(
                Mathf.Max(0f, Mathf.Abs(local.x) - half.x),
                Mathf.Max(0f, Mathf.Abs(local.y) - half.y),
                Mathf.Max(0f, Mathf.Abs(local.z) - half.z));
            return excess.magnitude;
        }

        public Vector3 Center => transform.TransformPoint(GetComponent<BoxCollider>().center);

        public bool TryReserveAttacker()
        {
            if (activeAttackers >= maxSimultaneousAttackers)
            {
                return false;
            }
            activeAttackers++;
            return true;
        }

        public void ReleaseAttacker()
        {
            activeAttackers = Mathf.Max(0, activeAttackers - 1);
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
