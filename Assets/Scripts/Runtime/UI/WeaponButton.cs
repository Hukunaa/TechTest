using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ScriptableObjects;

namespace UI
{
    public class WeaponButton : MonoBehaviour
    {
        [SerializeField] private Weapon _weapon;
        [SerializeField] private Image _weaponIcon;
        [SerializeField] private Image _weaponIconBackground;
        [SerializeField] private Color _equippedColor = Color.white;
        [SerializeField] private Color _unequippedColor = Color.gray;

        public Weapon Weapon => _weapon;
        public event Action<WeaponButton,Weapon> OnWeaponSelected;
        
        void Start()
        {
            if (_weaponIcon != null && _weapon != null)
            {
                _weaponIcon.sprite = _weapon.Icon;
            }
        }

        public void OnButtonPressed()
        {
            if (_weapon != null)
            {
                OnWeaponSelected?.Invoke(this, _weapon);
            }
        }

        public void SetEquipped(bool isEquipped)
        {
            if (_weaponIconBackground != null)
            {
                _weaponIconBackground.color = isEquipped ? _equippedColor : _unequippedColor;
            }

            float newScale = isEquipped ? 1.1f : 1f;
            _weaponIconBackground.rectTransform.DOKill();
            _weaponIconBackground.rectTransform.DOScale(newScale, 0.1f).SetEase(Ease.OutBack);
        }
    }
}
