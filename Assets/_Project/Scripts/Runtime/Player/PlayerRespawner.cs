using System;
using System.Collections;
using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Player
{
    /// <summary>
    /// Remembers the last checkpoint and brings the player back after death or a fall into the void.
    /// While dead the player has no control and cannot trigger checkpoints.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerContext))]
    public sealed class PlayerRespawner : MonoBehaviour, ICheckpointReceiver, IVoidListener
    {
        [SerializeField] private PlayerCombatSettings settings;

        private PlayerContext context;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private bool respawning;

        /// <summary>Raised the moment the player dies or falls, before the respawn.</summary>
        public event Action<RespawnReason> Downed;
        public event Action<RespawnReason> Respawned;
        public event Action<Checkpoint> CheckpointReached;

        public Checkpoint CurrentCheckpoint { get; private set; }
        public bool IsRespawning => respawning;

        private void Awake()
        {
            context = GetComponent<PlayerContext>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            if (settings == null)
            {
                Debug.LogError($"{name}: PlayerRespawner has no PlayerCombatSettings assigned.", this);
            }
        }

        private void OnEnable()
        {
            context.Health.Died += HandleDied;
        }

        private void OnDisable()
        {
            context.Health.Died -= HandleDied;
        }

        public void SetCheckpoint(Checkpoint checkpoint)
        {
            if (respawning || checkpoint == null || checkpoint == CurrentCheckpoint)
            {
                return;
            }
            CurrentCheckpoint = checkpoint;
            spawnPosition = checkpoint.SpawnPosition;
            spawnRotation = checkpoint.SpawnRotation;
            CheckpointReached?.Invoke(checkpoint);
        }

        public void OnFellIntoVoid()
        {
            if (respawning)
            {
                return;
            }
            Downed?.Invoke(RespawnReason.FellIntoVoid);
            Respawn(RespawnReason.FellIntoVoid);
        }

        private void HandleDied()
        {
            if (!respawning)
            {
                StartCoroutine(RespawnAfterDeath());
            }
        }

        private IEnumerator RespawnAfterDeath()
        {
            respawning = true;
            context.Input.enabled = false;
            Downed?.Invoke(RespawnReason.Died);
            yield return new WaitForSecondsRealtime(settings != null ? settings.deathRespawnDelay : 1f);
            context.Health.Restore();
            Respawn(RespawnReason.Died);
        }

        private void Respawn(RespawnReason reason)
        {
            context.Movement.TeleportTo(spawnPosition, spawnRotation);
            context.Camera.ResetView();
            context.Input.enabled = true;
            respawning = false;
            Respawned?.Invoke(reason);
        }
    }
}
