using UnityEngine;

namespace Codebase.App.Attack
{
    public interface IDamageReceiver
    { 
        bool IsAlive { get; }
        void MakeDamage(long damageCount, DamageSender sender, float delayTime = 0, bool checkNoise = true);
        void Push(Vector3 force, bool zeroY = true);
    }

    public enum DamageSender
    {
        INTERACTIVE_OBJECTS,
        PLAYER,
        ENEMY
    }
}