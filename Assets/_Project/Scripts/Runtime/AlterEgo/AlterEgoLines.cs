using UnityEngine;

namespace FeverDream.AlterEgo
{
    [CreateAssetMenu(menuName = "Fever Dream/Alter Ego/Lines", fileName = "SO_AlterEgo")]
    public sealed class AlterEgoLines : ScriptableObject
    {
        public string speakerName = "Güvercin";
        [Min(0.5f)] public float lineDuration = 3.5f;
        [Tooltip("Minimum seconds between two lines (interrupting lines ignore it).")]
        [Min(0f)] public float cooldown = 2.5f;
        [Tooltip("Aura Points a finished combo needs to count as big / huge.")]
        [Min(0)] public int bigComboPoints = 150;
        [Min(0)] public int hugeComboPoints = 400;
        public AlterEgoLineSet[] lineSets = new AlterEgoLineSet[0];

        public AlterEgoLineSet Find(AlterEgoEvent trigger)
        {
            foreach (var set in lineSets)
            {
                if (set != null && set.trigger == trigger)
                {
                    return set;
                }
            }
            return null;
        }
    }
}
