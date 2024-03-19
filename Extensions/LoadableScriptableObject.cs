using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codebase.Extension
{
    public abstract class LoadableScriptableObject<T> : SerializedScriptableObject where T : SerializedScriptableObject
    {
        private const string ResourcePath = "ScriptableObjects/";

        private static T _cachedInstance = null;
        private static int _referenceCount = 0;
        private static bool _isNotReleasable = false;
        
        public static T GetInstance(bool canBeReleased = true)
        {
            _isNotReleasable = !canBeReleased;
            _referenceCount++;

            if (_cachedInstance == null)
            {
                _cachedInstance = Resources.Load<T>(ResourcePath + typeof(T).Name);

                if (_cachedInstance == null)
                    throw new FileLoadException($"Can not load <SO> with type:[{typeof(T).Name}] " +
                                                $"by resource path:[{ResourcePath + typeof(T).Name}]");
            }
            
            return _cachedInstance;
        }
        
        public static T ReleaseInstance()
        {
            if (_isNotReleasable)
                return null;
            
            _referenceCount--;

            if (_referenceCount <= 0 && _cachedInstance != null)
                Resources.UnloadAsset(_cachedInstance);

            return null;
        }
        
        public static void ClearAllReferences()
        {
            if (_isNotReleasable)
                return;
            
            _referenceCount = 0;

            if (_cachedInstance != null)
                Resources.UnloadAsset(_cachedInstance);
        }

        public T Release()
        {
            if (_isNotReleasable)
                return null;
                
            _referenceCount--;

            if (_referenceCount <= 0 && _cachedInstance != null)
                Resources.UnloadAsset(_cachedInstance);

            return null;
        }
    }
}