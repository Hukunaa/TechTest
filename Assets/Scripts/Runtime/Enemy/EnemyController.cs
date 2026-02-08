using System.Collections;
using DG.Tweening;
using UnityEngine;
using Hero;
using Game;
using Utils;
using ScriptableObjects;

namespace Enemy
{
    [RequireComponent(typeof(AnimatorController))]
    public class EnemyController : LivingUnit
    {
        [SerializeField] private Weapon _weapon;
        [SerializeField] private float _attackSpeed = 1f;

        private Coroutine _attackRoutine;
        private bool _isAttacking;
        private AnimatorController _animatorController;
        private float _attackCooldown;

        // Cache references and reset runtime state when the enemy is created.
        protected override void Awake()
        {
            base.Awake();
            _currentHealth = _maxHealth;
            _animatorController = GetComponent<AnimatorController>();
            _isAttacking = false;
        }

        void OnEnable()
        {
            _currentHealth = _maxHealth;
        }

        // Apply incoming damage, and only play a hit reaction if we're not in the middle of an attack.
        public override void TakeHit(float damage)
        {
            base.TakeHit(damage);

            if (!IsAlive()) return;

            if (!_isAttacking)
                _animatorController.PlayTakeHit();
        }

        // On death: add impact feedback (camera shake + death anim) and remove the enemy with a tweened scale-out.
        protected override void Die()
        {
            base.Die();
            Camera.main.GetComponent<CameraShake>().Shake(0.4f, 0.2f, 10f, true);
            _animatorController.PlayDeath();

            transform.DOKill();
            transform.DOScale(Vector3.zero, 0.4f).SetDelay(1.2f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject)).SetLink(gameObject);
        }

        // Each frame: face the hero (if alive) and attempt to attack when conditions are met.
        void Update()
        {
            if (GameManager.Instance.HeroController != null && IsAlive())
                LookAtPlayer(GameManager.Instance.HeroController.transform);

            TryToAttack();
        }

        // Rotate on the XZ plane to face the hero so attacks look directed and readable.
        public void LookAtPlayer(Transform playerTransform)
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            directionToPlayer.y = 0f;
            if (directionToPlayer.sqrMagnitude > 0f)
                RotateTowards(directionToPlayer);
        }

        // Gate attacking using cooldown + game state + range checks, then start the timed attack routine.
        public void TryToAttack()
        {
            if (_isAttacking) return;
            if (_weapon == null) return;
            if (!IsAlive()) return;
            if (GameManager.Instance.State != GameState.Running) return;

            if (_attackCooldown > 0f)
            {
                _attackCooldown -= Time.deltaTime;
                return;
            }

            HeroController target = GameManager.Instance.HeroController;
            if (target == null) return;

            if (!IsTargetInRange(target.transform, _weapon.AttackRange) || !target.IsAlive())
                return;

            _attackRoutine = StartCoroutine(Attack(_weapon));
            _attackCooldown = Mathf.Max(0f, 1f / _attackSpeed);
        }

        // Play attack animation, wait for the weapon hit timing, then apply damage if the hero is still valid and in range.
        public IEnumerator Attack(Weapon weapon)
        {
            _isAttacking = true;

            if (_animatorController != null)
                _animatorController.PlayAttack();

            float delay = weapon.HitDelay / weapon.AttackAnimationSpeed;
            delay = Mathf.Clamp(delay, 0f, 10f);

            float t = 0f;
            while (t < delay)
            {
                if (!IsAlive())
                {
                    CancelAttack();
                    yield break;
                }

                t += Time.deltaTime;
                yield return null;
            }

            HeroController target = GameManager.Instance.HeroController;

            if (target == null || !target.IsAlive() || !IsTargetInRange(target.transform, _weapon.AttackRange))
            {
                CancelAttack();
                yield break;
            }

            target.TakeHit(weapon.Damage);

            _attackRoutine = null;
            _isAttacking = false;
        }

        // Ensure the attack routine is stopped cleanly and the enemy can attempt a new attack later.
        private void CancelAttack()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            _isAttacking = false;
        }

        // Range check on the XZ plane to ignore height differences.
        bool IsTargetInRange(Transform target, float range)
        {
            if (target == null) return false;

            float r = Mathf.Max(0f, range);

            Vector3 a = transform.position;
            Vector3 b = target.position;
            a.y = 0f;
            b.y = 0f;

            return (b - a).sqrMagnitude <= r * r;
        }
    }
}
