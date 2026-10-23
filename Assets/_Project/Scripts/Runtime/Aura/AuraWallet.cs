using System;
using UnityEngine;

namespace FeverDream.Aura
{
    /// <summary>Total Aura Points, the game's currency. Spending at checkpoints comes after the prototype.</summary>
    [DisallowMultipleComponent]
    public sealed class AuraWallet : MonoBehaviour
    {
        [SerializeField] private AuraSettings settings;

        /// <summary>New total and the amount added.</summary>
        public event Action<int, int> Changed;

        public int Total { get; private set; }

        public AuraTier Tier => settings == null ? AuraTier.Calm
            : Total >= settings.panicThreshold ? AuraTier.Panic
            : Total >= settings.uneasyThreshold ? AuraTier.Uneasy
            : AuraTier.Calm;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            Total += amount;
            Changed?.Invoke(Total, amount);
        }
    }
}
