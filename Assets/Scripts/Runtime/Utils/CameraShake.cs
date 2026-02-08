using UnityEngine;

namespace Utils
{
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private Vector3 _baseLocalPos;
        private float _timeLeft;
        private float _duration;
        private float _amplitude;
        private float _frequency;
        private float _seed;
        private bool _isShaking;

        void Awake()
        {
            if (_target == null) _target = transform;
            _baseLocalPos = _target.localPosition;
            _seed = Random.value * 1000f;
        }

        void LateUpdate()
        {
            if (!_isShaking)
            {
                _baseLocalPos = _target.localPosition;
                return;
            }

            _timeLeft -= Time.unscaledDeltaTime;
            if (_timeLeft <= 0f)
            {
                StopShake();
                return;
            }

            float normalizedLeft = _duration > 0f ? Mathf.Clamp01(_timeLeft / _duration) : 0f;
            float fade = _fadeCurve != null ? Mathf.Clamp01(_fadeCurve.Evaluate(1f - normalizedLeft)) : normalizedLeft;

            float t = Time.unscaledTime * _frequency;

            float x = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(_seed + 11.17f, t) - 0.5f) * 2f;
            float z = (Mathf.PerlinNoise(_seed + 23.73f, t) - 0.5f) * 2f;

            _target.localPosition = _baseLocalPos + new Vector3(x, y, z) * (_amplitude * fade);
        }

        public void Shake(float duration, float amplitude, float frequency, bool overrideExisting = false)
        {
            if (_target == null) return;

            duration = Mathf.Max(0f, duration);
            amplitude = Mathf.Max(0f, amplitude);
            frequency = Mathf.Max(0f, frequency);

            if (duration <= 0f || amplitude <= 0f || frequency <= 0f)
            {
                StopShake();
                return;
            }

            if (!_isShaking)
                _baseLocalPos = _target.localPosition;
            else
                _target.localPosition = _baseLocalPos;

            if (overrideExisting)
            {
                _duration = duration;
                _timeLeft = duration;
                _amplitude = amplitude;
                _frequency = frequency;
                _seed = Random.value * 1000f;
            }
            else
            {
                float previousLeft = _timeLeft;
                _duration = Mathf.Max(_duration, duration);
                _timeLeft = Mathf.Max(previousLeft, duration);
                _amplitude = Mathf.Max(_amplitude, amplitude);
                _frequency = Mathf.Max(_frequency, frequency);
            }

            _isShaking = true;
        }

        public void StopShake()
        {
            _isShaking = false;
            _timeLeft = 0f;

            if (_target != null)
                _target.localPosition = _baseLocalPos;
        }
    }
}
