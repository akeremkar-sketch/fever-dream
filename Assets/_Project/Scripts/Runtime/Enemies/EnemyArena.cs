using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>
    /// Fight area for the enemies below it in the hierarchy. They only engage while the player is inside the box
    /// and never walk out of it. Resize the trigger box to fit the arena.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EnemyArena : MonoBehaviour
    {
        private BoxCollider area;

        private void Awake()
        {
            area = GetComponent<BoxCollider>();
            area.isTrigger = true;
        }

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        public bool Contains(Vector3 worldPoint)
        {
            if (area == null)
            {
                area = GetComponent<BoxCollider>();
            }
            Vector3 local = transform.InverseTransformPoint(worldPoint) - area.center;
            Vector3 half = area.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
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
