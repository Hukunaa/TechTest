using UnityEngine;
using Game;
using Hero;

namespace Utils
{
    public class CameraFollower : MonoBehaviour
    {
        [SerializeField] private float _maxZ = 10f;
        [SerializeField] private float _minZ = 10f;

        [SerializeField] private HeroController _hero;
        [SerializeField] private float _offset = 0f;
        [SerializeField] private float _smoothTime = 0.3f;
        private Vector3 _cameraInitialPosition;
        
        void Start()
        {
            _cameraInitialPosition = transform.position;
        }

        void LateUpdate()
        {
            if(_hero != null)
            {
                Vector3 targetPosition = _cameraInitialPosition;
                targetPosition.z = Mathf.Clamp(_hero.transform.position.z + _offset, _minZ, _maxZ);
                transform.position = Vector3.Lerp(transform.position, targetPosition, _smoothTime * Time.deltaTime);
            }
            else
            {
                //If the hero reference is not set, try to find it in the scene (e.g. if the hero was just spawned).
                if (GameManager.Instance.HeroController != null)
                {
                    _hero = GameManager.Instance.HeroController;
                }
            }
        }
    }
}
