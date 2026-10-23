using System;
using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Player
{
    /// <summary>
    /// Root of the player prefab and the one known access point to its systems
    /// (<see cref="Current"/>), so enemies, UI and the alter ego never have to search the scene.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerContext : MonoBehaviour
    {
        public static PlayerContext Current { get; private set; }
        public static event Action<PlayerContext> Registered;

        private PlayerInputReader input;
        private PlayerMovement movement;
        private PlayerCamera playerCamera;
        private Health health;
        private PlayerDamageReceiver damageReceiver;
        private PlayerRespawner respawner;

        public PlayerInputReader Input => Resolve(ref input);
        public PlayerMovement Movement => Resolve(ref movement);
        public PlayerCamera Camera => Resolve(ref playerCamera);
        public Health Health => Resolve(ref health);
        public PlayerDamageReceiver DamageReceiver => Resolve(ref damageReceiver);
        public PlayerRespawner Respawner => Resolve(ref respawner);

        private void Awake()
        {
            Current = this;
            Registered?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        private T Resolve<T>(ref T cached) where T : Component
        {
            if (cached == null)
            {
                cached = GetComponentInChildren<T>(true);
            }
            return cached;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Registered = null;
        }
    }
}
