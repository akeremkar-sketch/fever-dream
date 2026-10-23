using System;
using UnityEngine;

namespace FeverDream.Core
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        public event Action<float> Changed;
        public event Action Died;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public float Normalized => Current / maxHealth;
        public bool IsDead => Current <= 0f;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void SetMax(float value, bool refill = true)
        {
            maxHealth = Mathf.Max(1f, value);
            if (refill)
            {
                Current = maxHealth;
            }
            Changed?.Invoke(Current);
        }

        /// <summary>Applies damage and returns true when this hit was the killing blow.</summary>
        public bool ApplyDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return false;
            }

            Current = Mathf.Max(0f, Current - amount);
            Changed?.Invoke(Current);
            if (!IsDead)
            {
                return false;
            }

            Died?.Invoke();
            return true;
        }

        public void Restore()
        {
            Current = maxHealth;
            Changed?.Invoke(Current);
        }
    }
}
