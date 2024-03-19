using System;
using System.Collections.Generic;
using Codebase.App.Destruction;
using Codebase.App.Entity;
using Codebase.App.Enums;
using Codebase.App.ProjectileTimer;
using Codebase.App.ScriptableObjects.Weapons;
using Codebase.App.Tag;
using Codebase.Extension;
using DG.Tweening;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Codebase.App.Projectiles
{
    public class ThrowableGrenadeProjectile : BaseProjectile
    {
        [SerializeField] protected TrailRenderer _trail;
        [SerializeField] protected Rigidbody _selfRigidbody;
        [SerializeField] protected ProjectileTimerController _projectileTimerController;
        [SerializeField] protected SpriteRenderer _explosionZone;
        [SerializeField] private bool _isProjectileWithYAxisMultiplier;

        private IDisposable _movementDisposable;
        private IDisposable _explosionDisposable;
        private IDisposable _explosionZoneDisposable;
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
            
            if(_explosionZone) 
                _explosionZone.gameObject.SetActive(false);

            if(_trail) 
                _trail.Clear();

            return this;
        }

        private void OnCollisionEnter(UnityEngine.Collision collision)
        {
            if (collision.collider.gameObject.layer == LayerMask.NameToLayer(ConstantTag.ObstacleObject)
                || _explosionStats.InstantExplosion &&  (~collision.collider.gameObject.layer & LayerExtension.ConvertToLayerMask(TargetTags)) != 0)
            {
                OnFlightFinish();
            }
        }

        protected override void OnInitialize() => ThrowToTarget();

        protected override void OnTriggerEnter(Collider other) { }

        private void ThrowToTarget()
        {
            _startingPosition = Transform.position;
            _targetPosition = TargetPos;

            Vector2 randomPoint = Random.insideUnitCircle;
            _targetPosition = new Vector3(_targetPosition.x + randomPoint.x, _targetPosition.y,
                _targetPosition.z + randomPoint.y);
            
            _journeyLength = Vector3.Distance(_startingPosition, _targetPosition);
            _startTime = Time.time;
            _speed = _journeyLength;

            _movementDisposable = Observable.EveryUpdate().Subscribe(GrenadeMovement);
            
            if(WeaponStats.HasRotation) 
            {
                RotationDisposable = Observable
                    .EveryUpdate()
                    .Subscribe(l => 
                        _selfRigidbody.AddTorque(new Vector3(WeaponStats.RotationSpeed, -WeaponStats.RotationSpeed, 0)));
            }
        }
        
        private void GrenadeMovement(long tick)
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
                
                if(WeaponStats.HasRotation == false)
                    _selfRigidbody.rotation = Quaternion.LookRotation(newPos - _selfRigidbody.position);
                
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
                if (_explosionZone)
                {
                    _explosionZone.gameObject.With(g => g.SetActive(true)).
                        With(g => g.transform.localScale = new Vector3(_explosionStats.ExplosionRadius * 13.6f, _explosionStats.ExplosionRadius * 13.6f, 1));
                    
                    _explosionZoneDisposable =  Observable.EveryUpdate().Subscribe(_ => 
                    {
                        if(_explosionZone != null && _explosionZone.transform != null)
                            _explosionZone.transform.forward = Vector3.up;
                    });
                }

                _explosionHandler.Explode();

                _explosionDisposable?.Dispose();
            });
        }

        private void OnExploded()
        {
            _explosionHandler.Exploded -= OnExploded;

            _explosionZoneDisposable?.Dispose();
            
            base.OnFlightEnded();
            AchieveCallback?.Invoke();
        }

        private bool ExplodeCondition()
        {
            if (_selfRigidbody == null)
            {
                _explosionDisposable?.Dispose();
                return false;
            }
            
            return _selfRigidbody.velocity.magnitude < 0.2f;
        }

        protected override void OnDestroy()
        {
            _explosionDisposable?.Dispose();
            _explosionZoneDisposable?.Dispose();
            _explosionHandler?.Dispose();
            base.OnDestroy();
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (transform == null || _explosionStats == null) 
                return;
            
            Gizmos.DrawSphere(transform.position, _explosionStats.ExplosionRadius);
        }
        #endif
    }
}