using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codebase.App.Entity;
using Codebase.App.Entity.Player;
using Codebase.App.Enums;
using Codebase.App.Projectiles;
using Codebase.App.RouletteStore;
using Codebase.App.ScriptableObjects.Weapons;
using Codebase.App.ServiceLocators;
using Codebase.App.Services;
using Codebase.App.Tag;
using Codebase.Extension;
using Codebase.Instruments.Attribute;
using Cysharp.Threading.Tasks;
using Lean.Pool;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using SoundPlayer = Codebase.App.Audio.SoundPlayer;

namespace Codebase.App.Weapon
{
    public abstract class WeaponEntity : AbstractEntity
    {
        public event Action<float> RechargeStarted; 
        public event Action RechargeFinished; 
        public event Action ReloadStarted;
        public event Action ReloadFinished;
        public event Action ShootStarted;
        public event Action ShootFinished;

        public WeaponType Type => CurrentWeaponType;
        public WeaponStats WeaponStats => _weaponStats;
        public bool AvailableToShot => RechargeProgress.Value >= 1f && ReloadProgress.Value >= 1f && InShootExecution == false;
        public bool IsWeaponRecharging => IsRecharging;
        public bool IsWeaponImproved => IsImproved;
        
        private const TagType ObstacleTag = TagType.LOCATION_STATIC_OBJECT;
        public IReadOnlyReactiveProperty<float> RechargeProgress => _rechargeProgress;
        private IReadOnlyReactiveProperty<float> ReloadProgress => _reloadProgress;
        
        [SerializeField] protected WeaponStats _weaponStats;
        [SerializeField] protected ParticleSystem _shotParticlesPrefab;
        
        [HideIf(nameof(CurrentWeaponType), WeaponType.MELEE), SerializeField] protected bool _ContainsShellParent;
        [ShowIf(nameof(_ContainsShellParent)), SerializeField] protected ParticleSystem ShellPrefab;
        [ShowIf(nameof(_ContainsShellParent))] [SerializeField] protected Transform _shellParent;

        [SerializeField] protected bool _ContainsProjectilesParent;
        [ShowIf(nameof(_ContainsProjectilesParent))] [SerializeField] protected BaseProjectile ProjectilePrefab;
        [ShowIf(nameof(_ContainsProjectilesParent))] [SerializeField] protected Transform _projectileParent;

        private readonly ReactiveProperty<float> _rechargeProgress = new(1f);
        private readonly ReactiveProperty<float> _reloadProgress = new(1f);

        protected Transform SelfTransform;
        protected AbstractEntity Carrier;
        protected GameObject DamageReceiver;
        protected SoundPlayer SoundPlayer;
        protected List<TagType> Targets;
        protected WeaponType CurrentWeaponType;
        protected int CurrentAmmo;
        protected bool IsInitialized;
        protected bool InShootExecution;
        protected bool IsImproved;
        protected bool IsRecharging;

        public virtual WeaponEntity Initialize(AbstractEntity carrier, List<TagType> targets)
        {
            if(IsInitialized)
                return this;

            Carrier = carrier;
            SelfTransform = transform;
            
            IsInitialized = true;
            
            Targets = new List<TagType>(targets)
            {
                ObstacleTag
            };
            SoundPlayer = ServiceLocator.AudioInstaller.SoundPlayer;
            
            CurrentWeaponType = _weaponStats.WeaponName.GetAttribute<WeaponAttribute>().WeaponType;
            CurrentAmmo = _weaponStats.ProjectilesAmount;

            return this;
        }

        public void ApplyExternalDamageScaler(float scale = 1f) => _weaponStats.DamageScaler = scale;

        public abstract WeaponEntity Shot(GameObject receiver);
        
        public void ExitFromShootExecution()
        {
            InShootExecution = false;
            
            ShootFinished?.Invoke();
        }

        public void ImproveWeapon(WeaponImproveType improveType)
        {
            IsImproved = true;
            
            ServiceLocator.ProgressServices.MissionProgressService.SetImprovedWeaponName(_weaponStats.WeaponName, improveType);

            WeaponImprovementConfig weaponImprovementConfig = WeaponImprovementConfig.GetInstance();

            int value = weaponImprovementConfig.GetValueByImprovementType(improveType);
            
            switch (improveType)
            {
                case WeaponImproveType.NONE:
                    throw new ArgumentOutOfRangeException($"INTERACTIVE_OBJECTS TYPE OF IMPROVEMENT");
                case WeaponImproveType.SHOT_UPGRADE:
                    _weaponStats.UpgadeLevelForShotCount += value;
                    break;
                case WeaponImproveType.RICOCHET_UPGRADE:
                    _weaponStats.CountOfPossibleRicochet += value;
                    break;
            }

            weaponImprovementConfig.Release();
        }
        
        protected void PlayShotSound()
        {
            SoundPlayer.PlaySoundByType(WeaponStats.ShotSoundType, sourceParent: SelfTransform);
        }
        
        protected void DecreaseCurrentAmount() => CurrentAmmo--;

        protected void TryLoadNewAmmo()
        {
            DecreaseCurrentAmount();
            
            if(CurrentAmmo <= 0) Reload();
            else Recharge();
        }
        
        protected virtual async void CreateProjectile()
        {
            IntoInShootExecution();

            for (int i = 0; i < _weaponStats.UpgadeLevelForShotCount; i++)
            {
                for (int j = 0; j < _weaponStats.ProjectilesByShot; j++)
                {
                    if (InShootExecution == false)
                        return;

                    PlayShotSound();

                    await CreateProjectile(DamageReceiver.gameObject.transform.position);

                    SpawnShotEffects();

                    if (_weaponStats.ProjectilesByShot > 1)
                        await UniTask.Delay(TimeSpan.FromSeconds(_weaponStats.ProjectilesByShotDelayBetweenShots));
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
            }

            TryLoadNewAmmo();
            ExitFromShootExecution();
        }

        protected void SpawnShotEffects()
        {
            if (_shotParticlesPrefab && _projectileParent)
            {
                ParticleSystem shotParticles = LeanPool.Spawn(_shotParticlesPrefab, _projectileParent.position, Quaternion.LookRotation(-_projectileParent.transform.forward, Vector3.up), _projectileParent);
                shotParticles.Play();
            }
                
            if (_ContainsShellParent && _shellParent)
            {
                ParticleSystem shellParticle = 
                    LeanPool.Spawn(ShellPrefab, _shellParent.position, Quaternion.LookRotation(_shellParent.forward));
                shellParticle.Play();
            }
        }

        protected void IntoInShootExecution()
        {
            InShootExecution = true;
            
            ShootStarted?.Invoke();
        }

        
        protected virtual void OnReloadComplete()
        {
            CurrentAmmo = _weaponStats.ProjectilesAmount;
            
            ReloadFinished?.Invoke();
        }

        protected virtual void OnRechargeComplete()
        {
            IsRecharging = false;
            
            RechargeFinished?.Invoke();
        }
        
        private Task<BaseProjectile> CreateProjectile(Vector3 targetPosition)
        {
            BaseProjectile projectile = LeanPool.Spawn(ProjectilePrefab);

            projectile
                .With(p => 
                    p.Initialize(Carrier,_weaponStats, _projectileParent, targetPosition, Targets, () => LeanPool.Despawn(p)));
            
            return Task.FromResult(projectile);
        }

        private void Recharge()
        {
            IsRecharging = true;

            float rechargeTime = _weaponStats.WeaponRecharge;

            if (Carrier is PlayerEntity)
                rechargeTime.ModifyByPassiveSkill(AbilityType.ATTACK_SPEED_BOOSTER);
            
            RX
                .DoValue(_rechargeProgress.Value = 0f, 1f, rechargeTime, OnRechargeComplete)
                .Subscribe(value =>
                {
                    _rechargeProgress.Value = value;
                });

            RechargeStarted?.Invoke(rechargeTime);
        }

        private void Reload()
        {
            RX
                .DoValue(_reloadProgress.Value = 0f, 1f, _weaponStats.WeaponReload, OnReloadComplete)
                .Subscribe(value =>
                {
                    _reloadProgress.Value = value;
                });
            
            ReloadStarted?.Invoke();
        }
    }
}