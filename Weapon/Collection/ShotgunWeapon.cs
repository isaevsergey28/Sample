using System.Collections.Generic;
using System.Threading.Tasks;
using Codebase.App.Entity;
using Codebase.App.Projectiles;
using Codebase.App.Tag;
using Codebase.Extension;
using Cysharp.Threading.Tasks;
using Lean.Pool;
using UnityEngine;

namespace Codebase.App.Weapon.Collection
{
    public class ShotgunWeapon : WeaponEntity
    {
        
        public override WeaponEntity Initialize(AbstractEntity carrier, List<TagType> targets)
        {
            base.Initialize(carrier, targets);
            
            return this;
        }

        public override WeaponEntity Shot(GameObject receiver)
        {
            DamageReceiver = receiver;

            if (DamageReceiver != null)
            {
                CreateProjectile();
                DecreaseCurrentAmount();
                TryLoadNewAmmo();
            }
            
            return this;
        }

        protected override async void CreateProjectile() => await CreateProjectiles();

        private async UniTask CreateProjectiles()
        {
            PlayShotSound();

            IntoInShootExecution();
            
            float startSegmentAngle = -(_weaponStats.ShotAngle / 2);
            float oneSegmentAngle = _weaponStats.ShotAngle / (_weaponStats.ProjectilesByShot - 1);
            Vector3 sourceDirection = (DamageReceiver.gameObject.transform.position - _projectileParent.position).normalized;
            
            for (int i = 0; i < _weaponStats.ProjectilesByShot; i++)
            {
                Vector3 shootDirection = Quaternion.Euler(0, startSegmentAngle + (oneSegmentAngle * i), 0) * sourceDirection;

                await CreateProjectile(shootDirection);
            }
            
            SpawnShotEffects();
            
            ExitFromShootExecution();
        }
        
        private Task<BaseProjectile> CreateProjectile(Vector3 direction)
        {
            BaseProjectile projectile = LeanPool.Spawn(ProjectilePrefab);

            projectile.With(p => p.Initialize(Carrier,
                _weaponStats, 
                _projectileParent, 
                new Vector3(direction.x, 0, direction.z), 
                Targets, 
                () => LeanPool.Despawn(p), 
                alreadyNormalized: true));

            return Task.FromResult(projectile);
        }
    }
}
