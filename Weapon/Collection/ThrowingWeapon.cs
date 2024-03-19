using System.Collections.Generic;
using Codebase.App.Entity;
using Codebase.App.Enums;
using Codebase.App.Projectiles;
using Codebase.App.Tag;
using Codebase.Extension;
using Lean.Pool;
using UnityEngine;

namespace Codebase.App.Weapon.Collection
{
    public class ThrowingWeapon : WeaponEntity
    {
        [SerializeField] private MeshRenderer _disappearingMesh;
        private LayerMask _ricochetsLayerMask;
        
        public override WeaponEntity Initialize(AbstractEntity carrier, List<TagType> targets)
        {
            base.Initialize(carrier, targets);
            _ricochetsLayerMask = LayerExtension.ConvertToLayerMask(_weaponStats.RicochetTriggerTags);
            return this;
        }

        public override WeaponEntity Shot(GameObject receiver)
        {
            DamageReceiver = receiver;

            if (DamageReceiver == null)
                return this;
            
            CreateProjectile();
            DecreaseCurrentAmount();
            TryLoadNewAmmo();
            ChangeVisibilityDisappearingMesh(false);

            return this;
        }

        protected override void CreateProjectile()
        {
            PlayShotSound();

            LeanPool
                .Spawn(ProjectilePrefab)
                .With(pj => pj.Initialize(Carrier,
                    _weaponStats,
                    _projectileParent,
                    DamageReceiver.gameObject.transform.position,
                    Targets,
                    () => LeanPool.Despawn(pj))).With(x =>
                {
                    if (_weaponStats.CountOfPossibleRicochet > 0 && x is ThrowableObjectProjectile proj)
                    {
                        proj.SetRicochetLayerMask(_ricochetsLayerMask);
                    }
                });
        }

        protected override void OnReloadComplete()
        {
            ChangeVisibilityDisappearingMesh(true);
            
            base.OnReloadComplete();
        }

        protected override void OnRechargeComplete()
        {
            ChangeVisibilityDisappearingMesh(true);
            
            base.OnRechargeComplete();
        }

        private void ChangeVisibilityDisappearingMesh(bool status)
        {
            if (_disappearingMesh)
                _disappearingMesh.enabled = status;
        }
    }
}
