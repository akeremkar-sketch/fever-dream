namespace FeverDream.Core
{
    public enum DamageResult
    {
        Ignored,
        Blocked,
        Hit,
        Killed,
        /// <summary>A projectile was cut out of the air. It is destroyed but does not count as a combo hit.</summary>
        Deflected,
    }
}
