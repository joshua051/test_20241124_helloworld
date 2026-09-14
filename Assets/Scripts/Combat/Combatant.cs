using System;
using UnityEngine;

namespace IronSand.Combat
{
    public abstract class Combatant : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float health = 100f;
        [SerializeField, Min(0f)] private float hitStunSeconds = 0.2f;

        private float hitStunRemaining;
        private float invulnerableRemaining;

        public event Action<Combatant> Died;
        public event Action<float, float> HealthChanged;

        public float MaxHealth => maxHealth;
        public float Health => health;
        public bool IsDead { get; private set; }
        public bool IsStunned => hitStunRemaining > 0f;
        public bool IsInvulnerable => invulnerableRemaining > 0f;

        protected virtual void Awake()
        {
            health = Mathf.Clamp(health, 0f, maxHealth);
        }

        protected virtual void Update()
        {
            hitStunRemaining = Mathf.Max(0f, hitStunRemaining - Time.deltaTime);
            invulnerableRemaining = Mathf.Max(0f, invulnerableRemaining - Time.deltaTime);
        }

        public virtual bool ApplyDamage(float amount, Vector3 knockback, float stunMultiplier = 1f)
        {
            if (IsDead || IsInvulnerable || amount <= 0f)
            {
                return false;
            }

            health = Mathf.Max(0f, health - amount);
            hitStunRemaining = Mathf.Max(hitStunRemaining, hitStunSeconds * Mathf.Max(0f, stunMultiplier));
            OnDamaged(knockback);
            HealthChanged?.Invoke(health, maxHealth);

            if (health <= 0f)
            {
                IsDead = true;
                OnDeath();
                Died?.Invoke(this);
            }

            return true;
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            health = Mathf.Min(maxHealth, health + amount);
            HealthChanged?.Invoke(health, maxHealth);
        }

        public void GrantInvulnerability(float seconds)
        {
            invulnerableRemaining = Mathf.Max(invulnerableRemaining, Mathf.Max(0f, seconds));
        }

        protected abstract void OnDamaged(Vector3 knockback);
        protected abstract void OnDeath();
    }
}
