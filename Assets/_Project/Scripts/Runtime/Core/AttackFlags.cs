using System;

namespace FeverDream.Core
{
    [Flags]
    public enum AttackFlags
    {
        None = 0,
        Heavy = 1 << 0,
        Launcher = 1 << 1,
        Aerial = 1 << 2,
        Finisher = 1 << 3,
        Slam = 1 << 4,
    }
}
