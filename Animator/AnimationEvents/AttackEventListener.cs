using System;
using UnityEngine;

namespace Codebase.App.Animation
{
    public class AttackEventListener : MonoBehaviour
    {
        public event Action AttackAnimationStarted; 
        public event Action AttackAnimationTriggerPassed; 
        public event Action AttackAnimationFinished;

        private void AttackAnimationStartedCallback() => AttackAnimationStarted?.Invoke();

        private void AttackAnimationTriggerPassedCallback() => AttackAnimationTriggerPassed?.Invoke();

        private void AttackAnimationFinishedCallback() => AttackAnimationFinished?.Invoke();
    }
}