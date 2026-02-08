using System;
using UnityEngine;

namespace Game
{
    public class LivingUnit : MonoBehaviour
    {
        [SerializeField] protected float _speed = 6f;
        [SerializeField] protected float _acceleration = 30f;
        [SerializeField] protected float _maxSpeed = 8f;
        [SerializeField] protected float _maxHealth = 10f;

        protected bool _isEnabled = false;
        protected float _currentHealth;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        public bool IsMoving = false;
        public bool IsEnabled => _isEnabled;

        public event Action<int> OnTakeDamage;
        public event Action<LivingUnit> OnDead;

        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Max(0f, value);
        }

        public float MaxSpeed
        {
            get => _maxSpeed;
            set => _maxSpeed = Mathf.Max(0f, value);
        }

        // Initialize runtime health state when the unit is created.
        protected virtual void Awake()
        {
            _currentHealth = _maxHealth;
        }

        // Apply damage, raise damage events, and trigger death when health reaches zero.
        public virtual void TakeHit(float damage)
        {
            if (damage <= 0f) return;
            if (!IsAlive()) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            OnTakeDamage?.Invoke((int)damage);

            if (!IsAlive())
                Die();
        }

        // Notify listeners that this unit has died. Subclasses can extend this for VFX/animations.
        protected virtual void Die()
        {
            OnDead?.Invoke(this);
        }

        // Rotate on the XZ plane to face a given direction.
        protected void RotateTowards(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0f) return;

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        // Disable gameplay logic for this unit (movement/attacks should early-out based on IsEnabled).
        public virtual void Disable()
        {
            _isEnabled = false;
        }

        // Enable gameplay logic for this unit.
        public virtual void Enable()
        {
            _isEnabled = true;
        }

        // Simple alive check based on current health.
        public bool IsAlive()
        {
            return _currentHealth > 0f;
        }
    }
}
