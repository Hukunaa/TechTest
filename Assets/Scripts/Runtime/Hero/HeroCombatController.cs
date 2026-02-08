using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using ScriptableObjects;
using Game;
using Enemy;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Hero
{
    [RequireComponent(typeof(HeroController))]
    [RequireComponent(typeof(AnimatorController))]
    public class HeroCombatController : MonoBehaviour
    {
        [SerializeField] private List<Weapon> _weapons = new List<Weapon>();
        [SerializeField] private Transform _weaponSlot;

        private Level _level;
        public HeroCombatState _currentState = HeroCombatState.Idle;
        private HeroController _heroController;
        private AnimatorController _heroAnimator;
        private Weapon _currentWeapon;
        private GameObject _weaponInstance;
        private TrailRenderer _weaponTrailInstance;
        private Coroutine _attackRoutine;
        private int _currentWeaponIndex = -1;
        private float _baseSpeed;
        private float _baseMaxSpeed;
        private bool _isAttacking;
        private bool _canAttack;
        private float _attackDisableCooldown;

        private AsyncOperationHandle<GameObject> _weaponInstanceHandle;
        private bool _hasWeaponHandle;
        private Coroutine _weaponSpawnRoutine;

        public Weapon CurrentWeapon => _currentWeapon;
        public event Action<Weapon> WeaponEquipped;
        public bool IsAttacking => _isAttacking;
        public event Action<HeroCombatState> OnHeroStateChanged;
        public HeroCombatState HeroCurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    OnHeroStateChanged?.Invoke(_currentState);
                }
            }
        }

        // Cache references and capture baseline movement values so weapon modifiers can be applied consistently.
        void Awake()
        {
            _heroController = GetComponent<HeroController>();
            _heroAnimator = GetComponentInParent<AnimatorController>();
            _isAttacking = false;

            if (_heroController != null)
            {
                _baseSpeed = _heroController.Speed;
                _baseMaxSpeed = _heroController.MaxSpeed;
            }
        }

        // Equip the initial weapon and hook animation state reactions to combat state changes.
        void Start()
        {
            if (_weapons != null && _weapons.Count > 0)
                EquipWeaponIndex(0);

            OnHeroStateChanged += OnStateChanged;
        }

        // Drive the combat loop: attempt auto-attacks and keep the combat state aligned with movement/alive status.
        void Update()
        {
            TryAutoAttack();
            CheckCharacterState();
        }

        // Keep the combat state in sync with movement and life state so animations and gameplay behave correctly.
        void CheckCharacterState()
        {
            if (IsMoving() && _heroController.IsAlive())
            {
                if (HeroCurrentState != HeroCombatState.Moving)
                    HeroCurrentState = HeroCombatState.Moving;
            }
            else if (!_heroController.IsAlive())
            {
                if (HeroCurrentState != HeroCombatState.Dead)
                    HeroCurrentState = HeroCombatState.Dead;
            }
        }

        // Core auto-attack decision: respect cooldown gates, require idle (not moving), find the closest target in range, then start the attack sequence.
        private void TryAutoAttack()
        {
            if (!_canAttack)
            {
                _attackDisableCooldown -= Time.deltaTime;
                if (_attackDisableCooldown <= 0f)
                {
                    _canAttack = true;
                    _attackDisableCooldown = 0f;

                    if (_heroController.IsAlive())
                        HeroCurrentState = HeroCombatState.Idle;
                }
                else
                    return;
            }

            if (_currentWeapon == null) return;
            if (!_heroController.IsEnabled) return;
            if (!_heroController.IsAlive()) return;
            if (_isAttacking) return;
            if (IsMoving()) return;

            if (_level == null)
            {
                SetCurrentLevel(GameManager.Instance.CurrentLevel);
                if (_level == null)
                    return;
            }

            EnemyController target = FindClosestEnemyInRange(_currentWeapon.AttackRange);
            if (target == null)
            {
                HeroCurrentState = HeroCombatState.Idle;
                return;
            }

            _heroController.SetLookTarget(target.transform);
            _attackRoutine = StartCoroutine(AttackRoutine(_currentWeapon, target));
        }

        // Provide the current level reference so target selection can query the enemies list.
        public void SetCurrentLevel(Level level)
        {
            _level = level;
        }

        public void EquipWeaponIndex(int index)
        {
            if (_weapons == null || _weapons.Count == 0) return;
            index = Mathf.Clamp(index, 0, _weapons.Count - 1);
            EquipWeaponInternal(_weapons[index], index);
        }

        public void EquipWeapon(Weapon weapon)
        {
            if (weapon == null || _weapons == null || _weapons.Count == 0) return;
            int index = _weapons.IndexOf(weapon);
            if (index < 0) index = 0;
            EquipWeaponInternal(weapon, index);
        }

        public void NextWeapon()
        {
            if (_weapons == null || _weapons.Count == 0) return;
            int next = (_currentWeaponIndex + 1) % _weapons.Count;
            EquipWeaponIndex(next);
        }

        public void PreviousWeapon()
        {
            if (_weapons == null || _weapons.Count == 0) return;
            int prev = _currentWeaponIndex <= 0 ? _weapons.Count - 1 : _currentWeaponIndex - 1;
            EquipWeaponIndex(prev);
        }

        // Equip flow: cancel any ongoing attack/spawn, release the previous weapon instance, spawn the new one via Addressables, apply movement modifier.
        private void EquipWeaponInternal(Weapon weapon, int index)
        {
            if (weapon == null) return;
            if (weapon == _currentWeapon) return;

            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            _isAttacking = false;

            _currentWeapon = weapon;
            _currentWeaponIndex = index;

            if (_weaponSpawnRoutine != null)
            {
                StopCoroutine(_weaponSpawnRoutine);
                _weaponSpawnRoutine = null;
            }

            ReleaseWeaponInstance();

            if (_weaponSlot != null && weapon.Prefab != null)
            {
                _weaponSpawnRoutine = StartCoroutine(SpawnWeaponInstanceRoutine(weapon));
            }

            ApplyMovementModifier();
            WeaponEquipped?.Invoke(_currentWeapon);
        }

        // Spawn the weapon model through Addressables, parent it to the weapon slot, and cache the TrailRenderer for hit feedback.
        private IEnumerator SpawnWeaponInstanceRoutine(Weapon weapon)
        {
            if (weapon == null || weapon.Prefab == null) yield break;
            if (!weapon.Prefab.RuntimeKeyIsValid()) yield break;
            if (_weaponSlot == null) yield break;

            _weaponInstanceHandle = weapon.Prefab.InstantiateAsync(_weaponSlot, false);
            _hasWeaponHandle = true;

            yield return _weaponInstanceHandle;

            if (_weaponInstanceHandle.Status != AsyncOperationStatus.Succeeded || _weaponInstanceHandle.Result == null)
            {
                ReleaseWeaponInstance();
                yield break;
            }

            _weaponInstance = _weaponInstanceHandle.Result;
            _weaponTrailInstance = _weaponInstance.GetComponentInChildren<TrailRenderer>();

            if (_weaponTrailInstance != null)
                _weaponTrailInstance.enabled = false;

            _weaponInstance.transform.localPosition = Vector3.zero;
            _weaponInstance.transform.localRotation = Quaternion.identity;
        }

        // Release the currently spawned weapon instance back to Addressables and clear cached references.
        private void ReleaseWeaponInstance()
        {
            _weaponInstance = null;
            _weaponTrailInstance = null;

            if (_hasWeaponHandle && _weaponInstanceHandle.IsValid())
                Addressables.ReleaseInstance(_weaponInstanceHandle);

            _hasWeaponHandle = false;
        }

        // Apply movement speed multipliers from the current weapon while preserving the original base values.
        private void ApplyMovementModifier()
        {
            if (_heroController == null || _currentWeapon == null) return;

            float m = Mathf.Clamp(_currentWeapon.MovementSpeedModifier, 0.5f, 1.5f);
            _heroController.Speed = _baseSpeed * m;
            _heroController.MaxSpeed = _baseMaxSpeed * m;
        }

        // Attack sequence: play attack animation, wait for the weapon hit timing, then apply damage if the target is still valid/in range.
        private IEnumerator AttackRoutine(Weapon weapon, EnemyController initialTarget)
        {
            _isAttacking = true;
            HeroCurrentState = HeroCombatState.Attacking;

            if (_heroAnimator != null)
                _heroAnimator.PlayAttack(weapon.AttackAnimationSpeed);

            if (_weaponTrailInstance != null)
            {
                _weaponTrailInstance.enabled = true;
            }

            float delay = weapon.HitDelay / weapon.AttackAnimationSpeed;
            delay = Mathf.Clamp(delay, 0f, 10f);

            float t = 0f;
            while (t < delay)
            {
                if (IsMoving() || !_heroController.IsAlive())
                {
                    CancelAttack();
                    yield break;
                }

                t += Time.deltaTime;
                yield return null;
            }

            EnemyController target = initialTarget;

            if (target == null || !target.gameObject.activeInHierarchy || !IsEnemyInRange(target, weapon.AttackRange))
                target = FindClosestEnemyInRange(weapon.AttackRange);

            if (target != null)
                target.TakeHit(weapon.Damage);

            float currentAnimLength = _heroAnimator.GetCurrentStateLength();
            float animationRemainingTime = currentAnimLength - delay;

            float t1 = 0f;
            while (t1 < animationRemainingTime)
            {
                if (IsMoving() || !_heroController.IsAlive())
                {
                    CancelAttack();
                    yield break;
                }

                t1 += Time.deltaTime;
                yield return null;
            }

            _attackRoutine = null;
            _isAttacking = false;
        }

        // Cancel the current attack cleanly (trail off, coroutine stop) and optionally gate the next attack via a cooldown delay.
        public void CancelAttack(float preventNextAttackDelay = 0f)
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            if (_weaponTrailInstance != null)
            {
                _weaponTrailInstance.enabled = false;
            }

            if (preventNextAttackDelay > 0f)
            {
                _canAttack = false;
                _attackDisableCooldown = preventNextAttackDelay;
            }

            _isAttacking = false;
        }

        private bool IsMoving()
        {
            if (_heroController == null)
                Debug.LogError("HeroActions: HeroController reference is missing.", this);

            return _heroController.IsMoving;
        }

        // Targeting: scan level enemies and pick the closest living enemy within the weapon's range (XZ plane).
        private EnemyController FindClosestEnemyInRange(float range)
        {
            if (_level == null) return null;

            float radius = Mathf.Max(0f, range);
            float radiusSquared = radius * radius;

            Vector3 originFlat = transform.position;
            originFlat.y = 0f;

            var enemies = _level.Enemies;
            if (enemies == null || enemies.Count == 0) return null;

            EnemyController controller = null;
            float bestMatch = float.PositiveInfinity;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;
                if (!enemy.gameObject.activeInHierarchy) continue;
                if (enemy.CurrentHealth <= 0f) continue;

                Vector3 enemyPosFlat = enemy.transform.position;
                enemyPosFlat.y = 0f;

                Vector3 distance = enemyPosFlat - originFlat;
                float distanceSquared = distance.sqrMagnitude;
                if (distanceSquared > radiusSquared) continue;

                if (distanceSquared < bestMatch)
                {
                    bestMatch = distanceSquared;
                    controller = enemy;
                }
            }

            return controller;
        }

        private bool IsEnemyInRange(EnemyController enemy, float range)
        {
            if (enemy == null) return false;

            float r = Mathf.Max(0f, range);
            Vector3 distance = enemy.transform.position - transform.position;
            distance.y = 0f;
            return distance.sqrMagnitude <= r * r;
        }

        // Translate combat state changes into animator calls (attack is handled inside the attack routine for timing/speed control).
        void OnStateChanged(HeroCombatState newState)
        {
            switch (newState)
            {
                case HeroCombatState.Idle:
                    _heroAnimator.PlayIdle();
                    break;
                case HeroCombatState.Moving:
                    _heroAnimator.PlayRun(CurrentWeapon.MovementSpeedModifier);
                    break;
                case HeroCombatState.Attacking:
                    break;
                case HeroCombatState.Dead:
                    break;
            }
        }

        // Reset the hero combat state back to Idle (useful for level restarts or respawns).
        public void Reset()
        {
            HeroCurrentState = HeroCombatState.Idle;
        }

        // Cleanup: stop routines, release addressable weapon instance, and unsubscribe to avoid leaks/dangling delegates.
        void OnDestroy()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            if (_weaponSpawnRoutine != null)
            {
                StopCoroutine(_weaponSpawnRoutine);
                _weaponSpawnRoutine = null;
            }

            ReleaseWeaponInstance();

            OnHeroStateChanged -= OnStateChanged;
            WeaponEquipped = null;
        }
    }
}
