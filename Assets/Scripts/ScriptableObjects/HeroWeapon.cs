using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ScriptableObjects
{
    [CreateAssetMenu(menuName = "Hero/Weapon", fileName = "HeroWeapon")]
    public class Weapon : ScriptableObject
    {
        public string WeaponName;
        [Range(0.5f, 1.5f)] public float MovementSpeedModifier = 1f;
        public float AttackRange = 1.5f;
        public float HitDelay = 0.15f;
        public float AttackAnimationSpeed = 1f;
        public float Damage = 10f;
        public Sprite Icon;
        public AssetReferenceGameObject Prefab;

        void OnValidate()
        {
            MovementSpeedModifier = Mathf.Clamp(MovementSpeedModifier, 0.5f, 1.5f);
        }
    }
}
