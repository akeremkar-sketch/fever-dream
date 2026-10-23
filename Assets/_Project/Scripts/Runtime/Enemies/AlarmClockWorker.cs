namespace FeverDream.Enemies
{
    /// <summary>
    /// Alarm saatli ofis çalışanı. Basic melee enemy whose head rings before every attack.
    /// Weak point: hitting it while it rings (the wind-up) stuns it for much longer
    /// (<see cref="EnemySettings.windupInterruptStaggerMultiplier"/>).
    /// </summary>
    public sealed class AlarmClockWorker : EnemyBase
    {
    }
}
