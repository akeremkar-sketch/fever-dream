namespace FeverDream.Combat
{
    /// <summary>
    /// Which procedural swing the first-person weapon plays for a move. Values are stored by number in
    /// SO_Tsunagi_* assets: give new swings a new number, never renumber or reuse an existing one.
    /// </summary>
    public enum SwingType
    {
        SlashRight = 0,
        SlashLeft = 1,
        Overhead = 2,
        Uppercut = 3,
        Thrust = 4,
        Slam = 5,
        Spin = 6,
    }
}
