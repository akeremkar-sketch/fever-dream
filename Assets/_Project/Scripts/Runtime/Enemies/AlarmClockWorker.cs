using UnityEngine;

namespace FeverDream.Enemies
{
    /// <summary>
    /// Alarm saatli ofis çalışanı. Basic melee enemy whose head rings before every attack.
    /// Weak point: hitting it while it rings (the wind-up) stuns it for much longer.
    /// </summary>
    public sealed class AlarmClockWorker : EnemyBase
    {
        [SerializeField, Min(1f)] private float ringingInterruptStagger = 3f;

        protected override float WindupInterruptStaggerMultiplier => ringingInterruptStagger;
    }
}
