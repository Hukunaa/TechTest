using UnityEngine;
using Hero;
using Game;
using ScriptableObjects;

namespace UI
{
    public class WeaponSelector : MonoBehaviour
    {
        [SerializeField] private WeaponButton[] _weaponButtons;

        private HeroCombatController _heroActionsController;

        void Start()
        {
            foreach (var button in _weaponButtons)
            {
                button.OnWeaponSelected += OnWeaponSelected;
            }
        }
        
        void Update()
        {
            //Wait for the Hero to be spawned and then initialize the weapon selection based on the currently equipped weapon at the start of the game.
            if (_heroActionsController == null && GameManager.Instance.HeroController != null)
            {
                _heroActionsController = GameManager.Instance.HeroController.GetComponent<HeroCombatController>();
                if (_heroActionsController != null)
                {
                    UpdateWeaponSelection(_heroActionsController.CurrentWeapon);
                }
            }
        }

        void OnWeaponSelected(WeaponButton button, Weapon weapon)
        {
            if (_heroActionsController != null)
            {
                _heroActionsController.EquipWeapon(weapon);
                UpdateWeaponSelection(weapon);
            }
        }

        public void UpdateWeaponSelection(Weapon equippedWeapon)
        {
            if(equippedWeapon == null)
            {
                Debug.LogWarning("WeaponSelector: Equipped weapon is null.", this);
                return;
            }

            foreach (var button in _weaponButtons)
            {
                button.SetEquipped(button.Weapon == equippedWeapon);
            }
        }

        void OnDestroy()
        {
            foreach (var button in _weaponButtons)
            {
                button.OnWeaponSelected -= OnWeaponSelected;
            }
        }
    }
}