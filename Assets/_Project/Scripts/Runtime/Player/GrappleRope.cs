using UnityEngine;

namespace FeverDream.Player
{
    /// <summary>Draws the Zincirli Kama chain while the player is grappling.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class GrappleRope : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [Tooltip("Where the chain leaves the hand. Uses this transform when empty.")]
        [SerializeField] private Transform origin;

        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.enabled = false;
            if (movement == null)
            {
                movement = GetComponentInParent<PlayerMovement>();
            }
        }

        private void LateUpdate()
        {
            var target = movement.ActiveGrapple;
            line.enabled = target != null;
            if (target == null)
            {
                return;
            }
            line.SetPosition(0, (origin != null ? origin : transform).position);
            line.SetPosition(1, target.transform.position);
        }
    }
}
