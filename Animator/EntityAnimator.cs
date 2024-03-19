using System;
using System.Collections.Generic;
using System.Linq;
using Codebase.App.Component;
using Codebase.App.Entity;
using Codebase.App.Enums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codebase.App.Animation
{
    public class EntityAnimator : EntityComponent
    {
        [SerializeField] protected Animator _animator;
        [SerializeField] private List<AnimationSetup> _animationsSetups = new();

        private AnimationSetup _handAnimationSetup;
        private WeaponType _handledWeaponType;
        private AnimationName _currentAnimationName;
        
        private static readonly int MovementValue = Animator.StringToHash("MovementValue");
        private static readonly int IsShooting = Animator.StringToHash("IsShooting");

        private float _movementValue;
        
        public override void Initialize(AbstractEntity entity) { }

        public void SetCurrentHandAnimationSetup(WeaponType weaponType)
        {
            _handledWeaponType = weaponType;

            _handAnimationSetup = _animationsSetups.FirstOrDefault(stp => stp.WeaponType == _handledWeaponType);

            if (_handAnimationSetup == default)
                throw new ArgumentException($"Can not get setup for weaponType:[{weaponType}]");
        }

        public void SetHandAnimation(AnimationStateType stateType)
        {
            AnimationName animName = _handAnimationSetup.SubAnimationSetups
                .First(stp => stp.AnimationStateType == stateType).AnimationName;

            if (_currentAnimationName == animName && stateType != AnimationStateType.ATTACK)
                return;
            
            _animator.StopPlayback();
            _animator.CrossFadeInFixedTime(animName.ToString(), fixedTransitionDuration: 0.2f, 0 ,0);
            
            _currentAnimationName = animName;
        }
        
        public void ChangeShootingState(bool state)
        {
            if(_animator == null)
                return;
            
            _animator.SetBool(IsShooting, state);
        }
        
        public void SetMovementAnimation(float value)
        {
            if(_animator == null || Math.Abs(_movementValue - value) < 0.001f)
                return;

            _movementValue = value;
            
            _animator.SetFloat(MovementValue, _movementValue);
        }
        
        public void SetAnimatorActive(bool value)
        {
            if(_animator == null)
                return;
            
            _animator.enabled = value;
        }

        [Serializable]
        public class AnimationSetup
        {
            public WeaponType WeaponType;
            public List<SubAnimationSetup> SubAnimationSetups;
            
            [Button]
            private void SetupAnimationName()
            {
                SubAnimationSetups.Clear();
                
                foreach (AnimationStateType animState in Enum.GetValues(typeof(AnimationStateType)))
                {
                    if(animState == AnimationStateType.NONE)
                        continue;

                    SubAnimationSetup newSetup = new()
                    {
                        AnimationStateType = animState
                    };

                    foreach (AnimationName animName in Enum.GetValues(typeof(AnimationName)))
                    {
                        if(animState + "_" + WeaponType == animName.ToString())
                        {
                            newSetup.AnimationName = animName;
                        }
                    }
                    
                    SubAnimationSetups.Add(newSetup);
                }
            }
        }

        [Serializable]
        public class SubAnimationSetup
        {
            public AnimationStateType AnimationStateType;
            public AnimationName AnimationName;
        }
    }
}
