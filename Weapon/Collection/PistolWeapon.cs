using UnityEngine;

namespace Codebase.App.Weapon.Collection
{
    public class PistolWeapon : WeaponEntity
    {
        public override WeaponEntity Shot(GameObject receiver)
        {
            DamageReceiver = receiver;

            if (DamageReceiver == null)
                return this;
            
            CreateProjectile();
            
            return this;
        }
    }
}
