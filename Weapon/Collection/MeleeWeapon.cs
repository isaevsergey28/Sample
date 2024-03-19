using Codebase.App.Animation;
using Codebase.App.Attack;
using Codebase.App.Entity.Shield;
using Codebase.App.Enums;
using Codebase.Extension;
using Lean.Pool;
using UnityEngine;

namespace Codebase.App.Weapon.Collection
{
    public class MeleeWeapon : WeaponEntity
    {
        [SerializeField] private ParticleSystem _slashVfxPrefab;

        public override WeaponEntity Shot(GameObject receiver)
        {
            IntoInShootExecution();

            DamageReceiver = receiver;

            Vector3 position = DamageReceiver.transform.position;
            
            Vector3 pos = Vector3.Lerp(Carrier.transform.position, position, 0.3f);
            pos.y += 0.5f;

            Vector3 targetPos = position;
            targetPos.y += 0.5f;
            
            ParticleSystem particle = LeanPool.Spawn(_slashVfxPrefab, pos, 
                _slashVfxPrefab.transform.rotation);
            
            particle.transform.LookAt(targetPos);
            particle.Play();
            
            if (DamageReceiver != null && DamageReceiver.TryGetComponent(out IDamageReceiver damageReceiver) &&
                CheckForObstacles(receiver))
            {
                PlayShotSound();
                
                damageReceiver.MakeDamage(_weaponStats.Damage, DamageSender.PLAYER,0);
                damageReceiver.Push((DamageReceiver.transform.position - Carrier.transform.position).normalized * WeaponStats.PushForce);
            }
            
            ExitFromShootExecution();

            return this;
        }


        private bool CheckForObstacles(GameObject target)
        {
            Vector3 position = Carrier.transform.position;
            Transform targetTransform = target.transform;
            Vector3 targetPosition = targetTransform.position;
            RaycastHit[] hits = new RaycastHit[5];

            Ray ray = new(position, targetPosition - position);
            int raycastHitSize = Physics.RaycastNonAlloc(ray, hits, position.Length(targetPosition));

            for (int i = 0; i < raycastHitSize; i++)
            {
                if (hits[i].collider.TryGetComponent(out ShieldEntity _))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
