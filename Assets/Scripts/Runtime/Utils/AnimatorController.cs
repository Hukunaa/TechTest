using UnityEngine;

namespace Utils
{
    public class AnimatorController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private ParticleSystem _movementParticles;

        [SerializeField] private string _idleStateName = "pc_fight_idle";
        [SerializeField] private string _runStateName = "Run";
        [SerializeField] private string _attackStateName = "Weak";
        [SerializeField] private string _takeHitStateName = "damage";
        [SerializeField] private string _deathStateName = "death";
        [SerializeField] private float _locomotionBlendTime = 0.1f;
        [SerializeField] private float _actionBlendTime = 0.05f;
        [SerializeField] private float _baseRunAnimationSpeed = 1f;
        
        void Awake()
        {
            if (_animator == null)
                Debug.LogError("AnimatorController: Animator reference is missing.", this);
        }

        public void PlayRun(float speedMultiplier = 1f)
        {
            if (_animator == null) return;

            if(_movementParticles != null)
                _movementParticles.Play();

            _animator.speed = speedMultiplier * _baseRunAnimationSpeed;
            CrossFade(_runStateName, _locomotionBlendTime / speedMultiplier);
        }

        public void PlayIdle()
        {
            if (_animator == null) return;

            _animator.speed = 1f;

            if(_movementParticles != null)
                _movementParticles.Stop();

            CrossFade(_idleStateName, _locomotionBlendTime);
        }
        public void PlayAttack(float weaponAnimationSpeed = 1f)
        {
            if (_animator == null) return;

            if(_movementParticles != null)
                _movementParticles.Stop();

            _animator.speed = weaponAnimationSpeed;
            CrossFade(_attackStateName, _actionBlendTime / weaponAnimationSpeed);
        }

        public void PlayTakeHit()
        {
            if (_animator == null) return;

            _animator.speed = 1f;
            CrossFade(_takeHitStateName, _actionBlendTime);
        }

        public void PlayDeath()
        {
            if (_animator == null) return;
            
            if(_movementParticles != null)
                _movementParticles.Stop();

            _animator.speed = 1f;
            CrossFade(_deathStateName, _actionBlendTime);
        }

        public float GetCurrentStateLength()
        {
            if (_animator == null) return 0f;
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.length;
        }

        void CrossFade(string stateName, float blendTime)
        {
            _animator.CrossFadeInFixedTime(stateName, blendTime, 0);
        }
    }
}
