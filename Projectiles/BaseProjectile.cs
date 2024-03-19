using System;
using System.Collections.Generic;
using ACS.Core;
using Codebase.App.Audio;
using Codebase.App.Entity;
using Codebase.App.ScriptableObjects.Weapons;
using Codebase.App.ServiceLocators;
using Codebase.App.Signals;
using Codebase.App.Tag;
using Lean.Pool;
using UniRx;
using UnityEngine;

namespace Codebase.App.Projectiles
{
     public abstract class BaseProjectile : AbstractEntity
    {
        [SerializeField] protected ParticleSystem ExplosionEffect;
        [SerializeField] protected bool ExplosionEffectOnTargetCenter = false;
        [SerializeField] protected SoundType SoundType;

        protected Transform Transform;
        protected IDisposable Disposable;
        protected IDisposable RotationDisposable;
        protected WeaponStats WeaponStats;
        protected SoundPlayer SoundPlayer;
        protected AbstractEntity WeaponCarrier;
        
        protected List<TagType> TargetTags;
        protected Vector3 TargetPos;
        protected Vector3 StartPosition;
        protected Vector3 ProjectileMovementStep;
        
        protected Action AchieveCallback;

        protected bool IsDeactivated;
        protected bool FlightCompleted;

        private void Start()
        {
            Core.Instance.SignalBusService.Subscribe<LevelClearingSignal>(OnLevelChanged);
        }

        public virtual BaseProjectile Initialize(AbstractEntity weaponCarrier, WeaponStats weaponStats, Transform projectileParent, Vector3 destination, 
            List<TagType> targetTags, Action achieveCallback = null, bool alreadyNormalized = false)
        {
            if (projectileParent == null || transform == null)
                return this;
            
            FlightCompleted = false;
            
            WeaponCarrier = weaponCarrier;
            Transform = transform;
            WeaponStats = weaponStats;
            TargetTags = targetTags;
            TargetPos = destination;
            Transform.position = projectileParent.position;
            AchieveCallback = achieveCallback;
            SoundPlayer ??= ServiceLocator.AudioInstaller.SoundPlayer;
            Vector3 selfTransformPosition = Transform.position;
            TargetPos = new Vector3(destination.x, selfTransformPosition.y, destination.z);
            Transform.rotation = Quaternion.LookRotation(TargetPos - selfTransformPosition);
            StartPosition = selfTransformPosition;
            
            if(alreadyNormalized == false)
                ProjectileMovementStep = (TargetPos - selfTransformPosition).normalized * (Time.fixedDeltaTime * weaponStats.ProjectileSpeed);
            else 
                ProjectileMovementStep = destination * (Time.fixedDeltaTime * weaponStats.ProjectileSpeed);
            
            OnInitialize();
            
            return this;
        }

        public void Deactivate()
        {
            IsDeactivated = true;
            OnFlightEnded();
        }
        
        protected virtual void OnInitialize() => 
            Disposable = Observable.EveryFixedUpdate().Subscribe(ApplyProjectileTranslation);

        protected virtual void ApplyProjectileTranslation(long tick)
        {
            if (Transform == null)
            {
                Disposable?.Dispose();
                return;
            }
            
            Transform.position += ProjectileMovementStep;
        }

        protected virtual void OnFlightEnded(bool withCollision = false, Collider other = null)
        {
            FlightCompleted = true;

            Disposable?.Dispose();
            RotationDisposable?.Dispose();

            IsDeactivated = false;
                
            SoundPlayer.PlaySoundByType(SoundType);

            if (ExplosionEffect != null)
            {
                ParticleSystem explosion =
                    LeanPool.Spawn(ExplosionEffect, ExplosionEffectOnTargetCenter ? TargetPos:Transform.position, ExplosionEffect.transform.rotation);
                explosion.Play();
                
                SoundPlayer.PlaySoundByType(SoundType, sourceParent: explosion.transform);
            }
        }
        
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (FlightCompleted)
                return;
            
            OnFlightEnded(true, other);
        }

        protected virtual void OnDestroy()
        {
            Core.Instance.SignalBusService.Unsubscribe<LevelClearingSignal>(OnLevelChanged);
        }
        
        private void OnLevelChanged(LevelClearingSignal obj)
        {
            if (gameObject.activeSelf == false)
                return;
            
            Disposable?.Dispose();
            RotationDisposable?.Dispose();
            
            LeanPool.Despawn(this);
        }
    }
}