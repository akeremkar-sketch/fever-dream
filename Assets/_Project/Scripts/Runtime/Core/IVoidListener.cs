namespace FeverDream.Core
{
    /// <summary>Anything that should react when it falls into a <see cref="KillVolume"/>.</summary>
    public interface IVoidListener
    {
        void OnFellIntoVoid();
    }
}
