using System.Collections.Generic;
using Codebase.App.Entity;
using Codebase.App.Tag;

using UnityEngine;

namespace Codebase.App.Weapon.Collection
{
    public class AutomaticRiffleWeapon : WeaponEntity
    {
        public override WeaponEntity Initialize(AbstractEntity carrier, List<TagType> targets)
        {
            base.Initialize(carrier, targets);

            return this;
        }

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
