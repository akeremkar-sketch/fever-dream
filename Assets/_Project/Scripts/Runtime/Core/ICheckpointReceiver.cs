namespace FeverDream.Core
{
    /// <summary>Anything that stores the last <see cref="Checkpoint"/> it passed through.</summary>
    public interface ICheckpointReceiver
    {
        void SetCheckpoint(Checkpoint checkpoint);
    }
}
