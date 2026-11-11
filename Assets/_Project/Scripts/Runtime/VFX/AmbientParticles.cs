using UnityEngine;

namespace FeverDream.VFX
{
    /// <summary>
    /// Keeps a world-space ambient particle cloud around an emitter that travels with the player. The particles stay
    /// where they were born, so a teleport (respawn, checkpoint) would leave the whole cloud behind and the new place
    /// empty until it slowly refills. After a jump this restarts the system with the cloud already grown.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class AmbientParticles : MonoBehaviour
    {
        [Tooltip("A move longer than this in one frame counts as a teleport. Keep it well above the fastest movement per frame.")]
        [SerializeField, Min(1f)] private float teleportDistance = 20f;

        private ParticleSystem particles;
        private Vector3 lastPosition;

        private void Awake()
        {
            particles = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            if ((position - lastPosition).sqrMagnitude > teleportDistance * teleportDistance)
            {
                Refill();
            }
            lastPosition = position;
        }

        private void Refill()
        {
            // Simulate with restart clears the old cloud and fast-forwards one full loop, like prewarm on the first frame.
            particles.Simulate(particles.main.duration, true, true);
            particles.Play(true);
        }
    }
}
