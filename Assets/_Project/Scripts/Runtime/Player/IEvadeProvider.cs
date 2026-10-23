namespace FeverDream.Player
{
    /// <summary>Lets an evasive ability (the dodge) cancel incoming damage. Returns true when the hit was evaded.</summary>
    public interface IEvadeProvider
    {
        bool TryEvade();
    }
}
