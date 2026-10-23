namespace FeverDream.AlterEgo
{
    /// <summary>
    /// Moments the pigeon alter ego comments on. Values are stored by number in SO_AlterEgo assets:
    /// give new events a new number, never renumber or reuse an existing one.
    /// </summary>
    public enum AlterEgoEvent
    {
        LevelStart = 0,
        ComboSmall = 1,
        ComboBig = 2,
        ComboHuge = 3,
        ComboBroken = 4,
        FellIntoVoid = 5,
        PlayerDied = 6,
        PerfectDodge = 7,
        EnemyKilled = 8,
        CheckpointReached = 9,
        LevelComplete = 10,
    }
}
