using System;
using UnityEngine;

namespace FeverDream.AlterEgo
{
    /// <summary>Lines for one event, split by how worn down the alter ego is (see <see cref="Aura.AuraTier"/>).</summary>
    [Serializable]
    public sealed class AlterEgoLineSet
    {
        public AlterEgoEvent trigger;
        [Range(0f, 1f)] public float chance = 1f;
        [Tooltip("Speak even while another line is on cooldown.")]
        public bool interrupt;
        [TextArea(1, 3)] public string[] calm = new string[0];
        [TextArea(1, 3)] public string[] uneasy = new string[0];
        [TextArea(1, 3)] public string[] panic = new string[0];
    }
}
