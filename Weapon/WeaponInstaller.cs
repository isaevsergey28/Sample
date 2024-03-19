using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.App.Animation;
using Codebase.App.Attack.Indicator;
using Codebase.App.Component;
using Codebase.App.Entity;
using Codebase.App.Enums;
using Codebase.App.RouletteStore;
using Codebase.App.ServiceLocators;
using Codebase.App.Tag;
using Codebase.App.Vision;
using Codebase.Extension;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

namespace Codebase.App.Weapon
{
    public class WeaponInstaller : EntityComponent
    {
        public event Action ShootStarted;
        public event Action ShootFinished;
        public event Action WeaponChanged;
        public WeaponEntity WeaponEntity => _weaponEntity;
        public bool ObtainedFromRoulette => ObtainedFromRoulette;
        
        [SerializeField] private Transform _weaponParent;
        [SerializeField] private Transform _shieldParent;
        [SerializeField] private WeaponRechargeView _rechargeView;
        
        [SerializeField] private bool _recalcualteVisionLength;
        [ShowIf(nameof(_recalcualteVisionLength)), SerializeField] private SegmentVision _segmentVision;
        [ShowIf(nameof(_recalcualteVisionLength)), SerializeField] private VisionCone _visionCone;
        
        private IDisposable _rechargeDisposable;
        private AbstractEntity _entity;
        private WeaponEntity _weaponEntity;
        private EntityAnimator _entityAnimator;
        private List<TagType> _targetTags;
        private WeaponType _handledWeaponType;
        
        public override void Initialize(AbstractEntity entity)
        {
            _entity = entity;
            
            if (_entity is not ITagContainable tagContainer)
                throw new ArgumentException("Can not convert entity to ITagContainable");

            _entityAnimator = _entity.GetComponents().Select<EntityAnimator>();
            _targetTags = tagContainer.GetTags().Tags;
        }

        public void InstallWeapon(WeaponName weaponName)
        {
            ClearCurrentWeapon();
            
            WeaponSetup setup = WeaponSetup.GetInstance();
            setup
                .With(sp => _weaponEntity = sp.Get(weaponName))
                .With(sp => sp.Release());
            
            _weaponEntity
                .With(we => _weaponEntity = Instantiate(_weaponEntity, _weaponParent))
                .With(we => _weaponEntity.Initialize(_entity, _targetTags));

            if(_weaponEntity.WeaponStats.IsWeaponContainsShield)
                _shieldParent.gameObject.SetActive(true);
            
            ReplaceWeaponData(_weaponEntity);

            _entityAnimator.SetCurrentHandAnimationSetup(_weaponEntity.Type);
            _entityAnimator.SetHandAnimation(AnimationStateType.IDLE);

            if (_recalcualteVisionLength == false)
                return;

            _segmentVision.ChangeRadius(_weaponEntity.WeaponStats.WeaponTriggerRadius);

            if (_visionCone != null)
                _visionCone.ChangeRadius(_weaponEntity.WeaponStats.WeaponTriggerRadius);

            if (_weaponEntity.WeaponStats.LinkWithRechargeView && _rechargeView != null)
            {
                UnsubscribeToRecharging();
                SubscribeToRecharging();
            }

            WeaponChanged?.Invoke();
        }

        private void SubscribeToRecharging()
        {
            _weaponEntity.RechargeStarted += OnRechargeStart;
            _weaponEntity.RechargeFinished += OnRechargeFinished;
        }
        
        private void UnsubscribeToRecharging()
        {
            _weaponEntity.RechargeStarted -= OnRechargeStart;
            _weaponEntity.RechargeFinished -= OnRechargeFinished;
        }

        public void TryRemoveLinkedRechargeView()
        {
            _rechargeDisposable?.Dispose();

            if (_weaponEntity != null) 
                UnsubscribeToRecharging();

            if (_rechargeView != null)
                _rechargeView.HideView(true);
        }

        private void OnRechargeStart(float duration)
        {
            _rechargeView.ShowView(duration);

            //_rechargeDisposable?.Dispose();
            //_rechargeDisposable = _weaponEntity.RechargeProgress.Subscribe(value => _attackIndicator.ChangeRechargeValue(value));
        }

        private void OnRechargeFinished()
        {
            _rechargeView.HideView();
            
            //_rechargeDisposable?.Dispose();
            //_attackIndicator.ChangeRechargeValue(0f);
        }

        public void ClearCurrentWeapon()
        {
            if (_weaponEntity != null) 
                Destroy(_weaponEntity.gameObject);
        }

        private void ReplaceWeaponData(WeaponEntity entity)
        {
            _weaponEntity = entity;
            _weaponEntity.gameObject.SetActive(true);

            _weaponEntity.ShootStarted += ShootStarted;
            _weaponEntity.ShootFinished += ShootFinished;
        }

        private void OnDestroy()
        {
            if(_weaponEntity == null)
                return;
            
            _weaponEntity.RechargeStarted -= OnRechargeStart;
            _weaponEntity.RechargeFinished -= OnRechargeFinished;
        }

        [Button] public void InstallMelee(WeaponName weaponName) => InstallWeapon(weaponName);
    }
}
