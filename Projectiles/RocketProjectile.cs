using System;
using System.Collections.Generic;
using Codebase.App.Destruction;
using Codebase.App.Entity;
using Codebase.App.Enums;
using Codebase.App.ProjectileTimer;
using Codebase.App.ScriptableObjects.Weapons;
using Codebase.App.Tag;
using DG.Tweening;
using UniRx;
using UnityEngine;

namespace Codebase.App.Projectiles
{
    public class RocketProjectile : BaseProjectile
    {
        [SerializeField] protected TrailRenderer _trail;
        [SerializeField] protected Rigidbody _selfRigidbody;
        [SerializeField] protected ProjectileTimerController _projectileTimerController;
        [SerializeField] private bool _isProjectileWithYAxisMultiplier;

        private IDisposable _movementDisposable;
        private IDisposable _explosionDisposable;
        private Tween _rotationTween;
        
        private Vector3 _startingPosition;
        private Vector3 _targetPosition;

        private Collider _selfCollider;
        private ExplosionHandler _explosionHandler;
        private ExplosionStats _explosionStats;
        private float _journeyLength;
        private float _startTime;
        private float _speed;

        public override BaseProjectile Initialize(AbstractEntity weaponCarrier, WeaponStats weaponStats,
            Transform projectileParent,
            Vector3 destination, List<TagType> targetTags, Action achieveCallback, bool alreadyNormalized)
        {
            base.Initialize(weaponCarrier, weaponStats, projectileParent, destination, targetTags, achieveCallback, alreadyNormalized);
            
            _selfCollider = GetComponent<Collider>();
            _explosionHandler = new ExplosionHandler();
            
            _explosionStats = new ExplosionStats(WeaponStats.ProjectileRadiusDamage, WeaponStats.RicochetTriggerTags,
                WeaponStats.Damage, WeaponStats.InstantExplosion, WeaponStats.ExplosionForce);
            
            _explosionHandler.Initialize(Transform, _explosionStats, _selfCollider, _explosionStats.InstantExplosion ? null : _projectileTimerController);
            
            if(_trail) 
                _trail.Clear();

            return this;
        }

        protected override void OnTriggerEnter(Collider other) { }

        private void OnCollisionEnter(UnityEngine.Collision collision)
        {
            if (WeaponCarrier!=null && collision.transform == WeaponCarrier.transform)
            {
                return;
            }
            
            if (collision.collider.gameObject.layer == LayerMask.NameToLayer(ConstantTag.ObstacleObject)
                || _explosionStats.InstantExplosion &&  (~collision.collider.gameObject.layer & LayerExtension.ConvertToLayerMask(TargetTags)) != 0)
            {
                OnFlightFinish();
            }
        }

        protected override void OnInitialize() => ThrowToTarget();

        private void ThrowToTarget()
        {
            _startingPosition = Transform.position;
            _targetPosition = TargetPos;
            
            _journeyLength = Vector3.Distance(_startingPosition, _targetPosition);
            _startTime = Time.time;
            _speed = WeaponStats.ProjectileSpeed;

            _movementDisposable = Observable.EveryUpdate().Subscribe(ProjectileMovement);
        }
        
        private void ProjectileMovement(long tick)
        {
            float distanceCovered = (Time.time - _startTime) * _speed;
            float fractionOfJourney = distanceCovered / _journeyLength;

            Vector3 currentPos = Vector3.Lerp(_startingPosition, _targetPosition, fractionOfJourney);
            float yOffset = WeaponStats.GrenadeThrowingHeigh * 4.0f * (fractionOfJourney - fractionOfJourney * fractionOfJourney);

            if (fractionOfJourney < 1)
            {
                Vector3 newPos = currentPos;
                
                if (_isProjectileWithYAxisMultiplier)
                    newPos += Vector3.up * yOffset;
                
                _selfRigidbody.MovePosition(newPos);
                return;
            }

            OnFlightFinish();
        }
        
        private void OnFlightFinish()
        {
            _movementDisposable?.Dispose();
            RotationDisposable?.Dispose();

            _explosionHandler.Exploded += OnExploded;
            
            _explosionDisposable = Observable.EveryUpdate().Where(_ => ExplodeCondition()).Subscribe(_ =>
                {
                    _explosionHandler.Explode();
                    
                    _explosionDisposable?.Dispose();
                });
        }

        private void OnExploded()
        {
            _explosionHandler.Exploded -= OnExploded;

            base.OnFlightEnded();
            AchieveCallback?.Invoke();
        }

        private bool ExplodeCondition()
        {
            if(_selfRigidbody == null)
                _explosionDisposable?.Dispose();
        
            return _selfRigidbody.velocity.magnitude < 0.2f;
        }

        protected override void OnDestroy()
        {
            _explosionHandler?.Dispose();
            base.OnDestroy();
        }
    }
}