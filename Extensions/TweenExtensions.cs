using System;
using DG.Tweening;
using UnityEngine;

namespace Codebase.Extension
{
    public static class TweenExtensions
    {
        public static void KillTween(this Component component)
        {
            component.DOPause();
            component.DOKill();
        }
        
        public static void KillTween(this Material material)
        {
            material.DOPause();
            material.DOKill();
        }
    }
}