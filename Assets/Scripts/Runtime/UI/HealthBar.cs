
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game;

namespace UI
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Image _barFillImage;
        [SerializeField] private TMP_Text _barText;
        [SerializeField] private LivingUnit _livingUnit;
        [SerializeField] private Vector3 _offset = Vector3.zero;

        private Transform _parentTransform;

        void Awake()
        {
            _parentTransform = transform.parent;
            transform.SetParent(null);

            if(_livingUnit != null)
                _livingUnit.OnTakeDamage += OnTakeDamage;

            //initialize the health bar display
            OnTakeDamage(0);
        }

        void OnDestroy()
        {
            if(_livingUnit != null)
                _livingUnit.OnTakeDamage -= OnTakeDamage;
        }

        void OnTakeDamage(int damage)
        {
            if (_livingUnit != null && _barFillImage != null && _barText != null)
            {
                float healthPercent = _livingUnit.CurrentHealth / _livingUnit.MaxHealth;
                _barFillImage.fillAmount = healthPercent;
                _barText.text = _livingUnit.CurrentHealth.ToString("0");
                transform.position = _parentTransform.position + _offset;
            }
            else
            {
                if(gameObject.activeSelf)
                    gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (_livingUnit != null && _barFillImage != null && _barText != null)
            {
                float healthPercent = _livingUnit.CurrentHealth / _livingUnit.MaxHealth;
                _barFillImage.fillAmount = healthPercent;
                _barText.text = _livingUnit.CurrentHealth.ToString("0");
                transform.position = _parentTransform.position + _offset;
            }
            else
            {
                if(gameObject.activeSelf)
                    gameObject.SetActive(false);
            }
        }
    }
}