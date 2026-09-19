using System;
using UnityEngine;
using HORDAX.Core;

namespace HORDAX.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        private float baseMaxHealth;

        public event Action<float> Damaged;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float Normalized => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;

        private void Awake()
        {
            baseMaxHealth = Mathf.Max(1f, maxHealth);
            maxHealth = baseMaxHealth;
            CurrentHealth = maxHealth;
        }

        public void SetPermanentHealthMultiplier(float multiplier)
        {
            maxHealth = Mathf.Max(1f, baseMaxHealth * Mathf.Max(0.01f, multiplier));
            CurrentHealth = maxHealth;
        }

        public void Damage(float amount)
        {
            if (amount <= 0f || CurrentHealth <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            Damaged?.Invoke(amount);

            if (CurrentHealth <= 0f && GameManager.Instance != null)
                GameManager.Instance.Lose();
        }

        public void Heal(float amount)
        {
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + Mathf.Max(0f, amount));
        }
    }
}
