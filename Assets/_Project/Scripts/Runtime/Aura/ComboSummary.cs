namespace FeverDream.Aura
{
    /// <summary>Result of a combo that ended on its own (not broken).</summary>
    public readonly struct ComboSummary
    {
        public readonly int Hits;
        public readonly int UniqueMoves;
        public readonly float Multiplier;
        /// <summary>Aura Points paid out. Zero when the combo was too short.</summary>
        public readonly int Points;

        public ComboSummary(int hits, int uniqueMoves, float multiplier, int points)
        {
            Hits = hits;
            UniqueMoves = uniqueMoves;
            Multiplier = multiplier;
            Points = points;
        }
    }
}
