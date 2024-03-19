using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.App.Enums;
using Codebase.Extension;
using UnityEngine;

namespace Codebase.App.Weapon
{
    [CreateAssetMenu(fileName = nameof(WeaponSetup), menuName = "App/Weapon/" + nameof(WeaponSetup))]
    public class WeaponSetup : LoadableScriptableObject<WeaponSetup>
    {
        [SerializeField] private List<WeaponEntity> _weaponEntities;

        public WeaponEntity Get(WeaponName weaponName)
        {
            WeaponEntity entity = _weaponEntities.FirstOrDefault(we => we.WeaponStats.WeaponName == weaponName);

            if (entity == default)
                throw new ArgumentException($"Can not get weaponEntity with type:[{weaponName}]");

            return entity;
        }
    }
}