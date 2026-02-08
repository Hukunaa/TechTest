using DG.Tweening;
using UnityEngine;
using UI;
using Game;
using Interfaces;
using Utils;
using Enemy;

namespace Hero
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(HeroCombatController))]
    public class HeroController : LivingUnit, IControllable
    {
        [SerializeField] private AnimatorController _animatorController;

        private HeroCombatController _heroWeaponController;
        private JoystickController _joystickController;
        private Rigidbody _rigidbody;
        private Transform _lookTarget;
        private Vector2 _lastInputDirection = Vector2.zero;

        // Cache core components and ensure physics state matches the enabled state at boot.
        protected override void Awake()
        {
            base.Awake();
            _rigidbody = GetComponent<Rigidbody>();
            _heroWeaponController = GetComponent<HeroCombatController>();
            _rigidbody.isKinematic = !_isEnabled;

            if (_rigidbody == null)
                Debug.LogWarning("MoveableEntity: Rigidbody reference is missing.", this);
        }

        // Subscribe to game state changes and locate the joystick input source.
        void Start()
        {
            GameManager.Instance.StateChanged += OnGameStateChanged;
            _joystickController = FindObjectOfType<JoystickController>();

            if (_joystickController == null)
                Debug.LogWarning("HeroMovementController: No JoystickController found in the scene.", this);
        }

        // Unsubscribe on destroy to avoid dangling listeners.
        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnGameStateChanged;
        }

        // React to game state transitions by enabling/disabling the hero and resetting when entering Idle.
        void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Idle:
                    ResetPositionToSpawn();
                    ResetCharacter();
                    Disable();
                    break;
                case GameState.Pause:
                case GameState.RoundComplete:
                    Disable();
                    break;
                case GameState.Dead:
                    Disable();
                    break;
                case GameState.Running:
                    Enable();
                    break;
            }
        }

        // Reset the hero runtime state for a fresh run (health + combat state).
        public void ResetCharacter()
        {
            _currentHealth = _maxHealth;
            _heroWeaponController.Reset();
        }

        // Place the hero at the spawn point and clear rotation.
        public void ResetPositionToSpawn()
        {
            transform.position = new Vector3(0f, 0.0f, 3f);
            transform.rotation = Quaternion.identity;
        }

        // Read joystick input and cache the last movement direction for Update/FixedUpdate usage.
        public void ProcessInput()
        {
            if (_joystickController == null) return;
            if (!IsAlive()) return;

            Vector2 direction = _joystickController.InputDirection;
            direction.Normalize();
            _lastInputDirection = direction;
        }

        // Handle facing logic: face movement direction while moving, otherwise face the current look target (enemy) if set.
        void Update()
        {
            ProcessInput();

            Vector3 horizontal = new Vector3(_lastInputDirection.x, 0f, _lastInputDirection.y);
            float speed = horizontal.magnitude;

            IsMoving = speed > 0f && _isEnabled;

            if (IsMoving)
            {
                RotateTowards(-horizontal);
                _lookTarget = null;
            }
            else if (_lookTarget != null && _isEnabled)
            {
                Vector3 flatToTarget = -(_lookTarget.position - transform.position);
                flatToTarget.y = 0f;
                RotateTowards(flatToTarget);
            }
        }

        // Apply acceleration-based movement using Rigidbody velocity while respecting max speed.
        void FixedUpdate()
        {
            if (!_isEnabled || !IsAlive()) return;

            Vector3 input = new Vector3(_lastInputDirection.x, 0f, _lastInputDirection.y);

            Vector3 v3 = _rigidbody.velocity;
            Vector3 horizontal = new Vector3(v3.x, 0f, v3.z);

            Vector3 desired = input * _speed;
            horizontal = Vector3.MoveTowards(horizontal, desired, _acceleration * Time.fixedDeltaTime);

            float max = Mathf.Max(0f, _maxSpeed);
            if (horizontal.sqrMagnitude > max * max)
                horizontal = horizontal.normalized * max;

            _rigidbody.velocity = new Vector3(horizontal.x, v3.y, horizontal.z);
        }

        // On hit: apply damage, cancel attacks for immediate feedback, then play hit feedback (shake + animation).
        public override void TakeHit(float damage)
        {
            base.TakeHit(damage);

            if (!IsAlive()) return;

            _heroWeaponController.CancelAttack(1f);

            Camera.main.GetComponent<CameraShake>().Shake(0.2f, 0.1f, 5f, true);
            _animatorController.PlayTakeHit();
        }

        // On death: stop attacking, play death animation, then trigger game over after a short delay.
        protected override void Die()
        {
            base.Die();

            _heroWeaponController.CancelAttack();
            _animatorController.PlayDeath();

            DOVirtual.DelayedCall(1.5f, () => GameManager.Instance.LoseLevel()).SetLink(gameObject);
        }

        // Provide a target the hero should face while idle (used by auto-attack to face the chosen enemy).
        public void SetLookTarget(Transform target)
        {
            _lookTarget = target;
        }

        // Disable hero control by turning off gameplay and freezing physics motion.
        public override void Disable()
        {
            base.Disable();
            _rigidbody.isKinematic = true;
        }

        // Enable hero control by turning gameplay back on and re-enabling physics motion.
        public override void Enable()
        {
            base.Enable();
            _rigidbody.isKinematic = false;
        }
    }
}
