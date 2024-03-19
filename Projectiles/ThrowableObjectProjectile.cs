using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.App.Attack;
using Codebase.App.Entity;
using Codebase.App.Entity.Player;
using Codebase.App.ScriptableObjects.Weapons;
using Codebase.App.Tag;
using Codebase.Extension;
using Lean.Pool;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

namespace Codebase.App.Projectiles
{
    public class ThrowableObjectProjectile : BaseProjectile
    {
        [SerializeField] protected TrailRenderer _trail;
        [SerializeField] protected bool _hasConstantTrailEffect;
        [ShowIf(nameof(_hasConstantTrailEffect)), SerializeField] protected ParticleSystem _constantTrailPrefab;
        private ParticleSystem _constantTrail;
        
        private readonly List<Collider> _markedColliders = new();
        private readonly List<TagType> _obstacleTag = new() {  TagType.LOCATION_STATIC_OBJECT };
        
        private int _possibleRicochets;
        
        private readonly Collider[] _initialRicochetOverlaps = new Collider[30];
        private LayerMask _ricochetLayerMask;
        
        public override BaseProjectile Initialize(AbstractEntity weaponCarrier, WeaponStats weaponStats,
            Transform projectileParent,
            Vector3 destination, List<TagType> targetTags, Action achieveCallback, bool alreadyNormalized)
        {
            base.Initialize(weaponCarrier, weaponStats, projectileParent, destination, targetTags, achieveCallback, alreadyNormalized);
            
            _possibleRicochets = WeaponStats.CountOfPossibleRicochet;

            _markedColliders.Clear();
            
            if(_trail) 
                _trail.Clear();

            SetupMovementToTarget(TargetPos);

            if (_hasConstantTrailEffect)
            {
                _constantTrail = LeanPool.Spawn(_constantTrailPrefab, transform.position, Quaternion.identity, transform);
                _constantTrail.Play();
            }
            return this;
        }

        public void SetRicochetLayerMask(LayerMask layerMask)
        {
            _ricochetLayerMask = layerMask;
        }

        private void SetupMovementToTarget(Vector3 destination)
        {
            StartPosition = Transform.position;
            TargetPos =  destination;
            ProjectileMovementStep = (TargetPos - StartPosition).normalized * (Time.fixedDeltaTime * WeaponStats.ProjectileSpeed);
            
            Vector3 direction = destination - StartPosition;
            Vector3 forwardAngle = new Vector3(0,-Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg - 90f, 0);

            Transform.eulerAngles = forwardAngle;
        }
        
        protected override void OnInitialize()
        {
            Disposable = Observable.EveryFixedUpdate().Subscribe(ApplyProjectileTranslation);
            
            if(WeaponStats.HasRotation)
                RotationDisposable = Observable
                    .EveryFixedUpdate()
                    .Subscribe(l => 
                        Transform.Rotate(0, -WeaponStats.RotationSpeed, 0, Space.Self));
        }

        protected override void ApplyProjectileTranslation(long tick) => Transform.position += ProjectileMovementStep;

        protected override void OnFlightEnded(bool withCollision = false, Collider other = null)
        {
            if (IsDeactivated)
            {
                FinishFlight();
                return;
            }

            if (other == null || other.TryGetComponent(out TagHolder tagHolder) == false)
                return;
            
            if (tagHolder.IsActualTag(TargetTags) == false 
                && (_possibleRicochets<WeaponStats.CountOfPossibleRicochet && tagHolder.IsActualTag(WeaponStats.RicochetTriggerTags))==false)
                return;

            if (TryMakeDamage(other) == false)
            {
                FinishFlight();
                return;
            }

            if (TryRicochet(other) == false) 
                FinishFlight();

            void FinishFlight()
            {
                if (_hasConstantTrailEffect)
                {
                    _constantTrail.transform.parent = null;
                    _constantTrail.Stop();
                }
                base.OnFlightEnded(withCollision, other);
                AchieveCallback?.Invoke();
            }
        }

        private bool TryMakeDamage(Collider other)
        {
            if (other.TryGetComponent(out IDamageReceiver damageReceiver))
            {
                DamageSender sender = WeaponCarrier is PlayerEntity ? DamageSender.PLAYER : DamageSender.ENEMY;
                
                damageReceiver.MakeDamage((long) (WeaponStats.Damage * WeaponStats.DamageScaler), sender);
                damageReceiver.Push((other.transform.position - StartPosition).normalized * WeaponStats.PushForce);
                return true;
            }
            
            return false;
        }

        private bool TryRicochet(Collider currentCollider)
        {
            if (_possibleRicochets <= 0)
                return false;
            
            Vector3 selfPosition = Transform.position;
            Array.Clear(_initialRicochetOverlaps,0,_initialRicochetOverlaps.Length-1);

            Physics.OverlapSphereNonAlloc(selfPosition, WeaponStats.PossibleRicochetRadius, _initialRicochetOverlaps, layerMask:_ricochetLayerMask);

            List<Collider> sortedOverlaps = _initialRicochetOverlaps.Where(x=>x!=null&&x!=currentCollider)
                .OrderBy(x =>
                {
                    TagHolder tagHolder = x.GetComponent<TagHolder>();
                    if (tagHolder != null)
                    {
                        int index = WeaponStats.RicochetTriggerTags.FindIndex(tag => tagHolder.Tags.Contains(tag));
                        return index >= 0 ? index : int.MaxValue;
                    }

                    return int.MaxValue;
                })
                .ThenBy(x => Vector3.Distance(x.transform.position, selfPosition))
                .ToList();
            
            for (int i = 0; i < sortedOverlaps.Count; i++)
            {
                Collider target = sortedOverlaps[i];
                
                if(target == null || _markedColliders.Contains(target))
                    continue;

                if (target.TryGetComponent(out TagHolder holder) == false)
                    continue;

                if (holder.IsActualTag(WeaponStats.RicochetTriggerTags) == false)
                    continue;
                
                Ray ray = new Ray(selfPosition, target.transform.position - selfPosition);
                RaycastHit[] hits = new RaycastHit[5];
                Physics.RaycastNonAlloc(ray, hits, selfPosition.Length(target.transform.position));
                
                bool notContainsObstacle = hits.Any(x=>x.transform!=null&& x.transform.TryGetComponent(out TagHolder hitTagHolder) && hitTagHolder.IsActualTag(_obstacleTag))==false;
                
                if (notContainsObstacle)
                {
                    _possibleRicochets--;

                    _markedColliders.Add(target);
                    
                    SetupMovementToTarget(target.bounds.center);
                    
                    return true;
                }
            }

            return false;
        }
    }
}
