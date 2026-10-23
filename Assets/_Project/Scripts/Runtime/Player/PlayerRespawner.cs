using System;
using System.Collections;
using FeverDream.Core;
using UnityEngine;

namespace FeverDream.Player
{
    public enum RespawnReason
    {
        Died,
        FellIntoVoid,
    }

    /// <summary>Remembers the last checkpoint and brings the player back after death or a fall into the void.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerContext))]
    public sealed class PlayerRespawner : MonoBehaviour, ICheckpointReceiver, IVoidListener
    {
        [SerializeField, Min(0f)] private float deathRespawnDelay = 1.2f;

        private PlayerContext context;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private bool respawning;

        /// <summary>Raised the moment the player dies or falls, before the respawn.</summary>
        public event Action<RespawnReason> Downed;
        public event Action<RespawnReason> Respawned;
        public event Action<Checkpoint> CheckpointReached;

        public Checkpoint CurrentCheckpoint { get; private set; }

        private void Awake()
        {
            context = GetComponent<PlayerContext>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
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
            if (checkpoint == null || checkpoint == CurrentCheckpoint)
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
            Downed?.Invoke(RespawnReason.Died);
            yield return new WaitForSecondsRealtime(deathRespawnDelay);
            context.Health.Restore();
            Respawn(RespawnReason.Died);
        }

        private void Respawn(RespawnReason reason)
        {
            context.Movement.TeleportTo(spawnPosition, spawnRotation);
            context.Camera.ResetView();
            respawning = false;
            Respawned?.Invoke(reason);
        }
    }
}
