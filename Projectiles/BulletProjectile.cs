using Codebase.App.Attack;
using Codebase.App.Entity.Player;
using Codebase.App.Enums;
using Codebase.App.Tag;
using UnityEngine;
using UnityEngine.Profiling;

namespace Codebase.App.Projectiles
{
    class BulletProjectile : BaseProjectile
    {  
        [SerializeField] protected TrailRenderer _trail;

        
        protected override void OnInitialize()
        {
            if(_trail) 
                _trail.Clear();
            
            base.OnInitialize();
        }
        
        protected override void OnFlightEnded(bool withCollision = false, Collider other = null)
        {
            if (IsDeactivated || other != null && other.gameObject.layer == LayerExtension.GetCorrespondingLayerData(TagType.OBSTACLE_OBJECT).Index || FlightCompleted)
            {
                base.OnFlightEnded(withCollision, other);
                AchieveCallback?.Invoke();

                return;
            }

            if (other != null && other.TryGetComponent(out TagHolder tagHolder))
            {
                if (tagHolder.IsActualTag(TargetTags))
                {
                    base.OnFlightEnded(withCollision, other);

                    if (other.TryGetComponent(out IDamageReceiver damageReceiver))
                    {
                        DamageSender sender = WeaponCarrier is PlayerEntity ? DamageSender.PLAYER : 
                            DamageSender.ENEMY;

                        damageReceiver.MakeDamage((long)(WeaponStats.Damage * WeaponStats.DamageScaler), sender);
                        damageReceiver.Push((other.transform.position - StartPosition).normalized * WeaponStats.PushForce);
                    }   
                    
                    AchieveCallback?.Invoke();
                }
            }
        }
    }
}