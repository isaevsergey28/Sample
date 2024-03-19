using System;
using System.Collections.Generic;
using ACS.Core;
using ACS.SignalBus.SignalBus;
using Codebase.App.Signals;
using Codebase.Extension;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

namespace Codebase.App.Vision
{
    public class VisionCone : MonoBehaviour
    {
        public Material Material => _material;

        [SerializeField] private bool _showGizmo = false;

        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Material _visionConeMaterial;
        [SerializeField] private float _visionRange;
        [SerializeField] private float _visionAngle;
        [SerializeField] private bool _ignoreOverriding = true;

        [SerializeField] private LayerMask _visionObstructingLayer;

        private IDisposable _updateDisposable;
        
        private Mesh _visionConeMesh;
        private MeshFilter _meshFilter;
        private Material _material;
        private Transform _selfTransform;
        private ISignalBusService _signalBusService;
        
        private Vector3[] _vertices;
        private RaycastHit _raycastHit;

        private int[] _triangles;
        private bool _isDestroyed = false;
        private float _currentAngle;
        private float _angleInc;
        private float _defaultVisionAngle;
        private int _visionConeResolution = 150;
        private bool _initialized;
        
        private readonly List<VisualConeRaycastData> _visualConeRaycastData = new();

#if UNITY_EDITOR
        [Button]
        private void DebugDraw()
        {
            if (Application.isPlaying)
                return;
            
            RecalculateValues();
            DrawVisionCone();
        }
#endif
        
        public void Init()
        {
            _initialized = true;
            
            RecalculateValues();

            _meshRenderer.material = _visionConeMaterial;
            _material = _meshRenderer.material;

            _signalBusService = Core.Instance.SignalBusService;

            _updateDisposable = RX.LoopedTimer(0.1f, 0.05f, TryUpdate);

            _signalBusService.Subscribe<LevelClearingSignal>(OnLevelReloading);
        }

        public void ChangeAngle(float angle)
        {
            if(_ignoreOverriding)
                return;
            
            _visionAngle = angle;
        }

        public void ChangeRadius(float radius)
        {
            if(_ignoreOverriding)
                return;
            
            _visionRange = radius * 1.05f;
        }

        private void RecalculateValues()
        {
            _selfTransform = transform;

            _meshRenderer.material = _visionConeMaterial;
            
            if (_meshFilter == null)
                _meshFilter = _selfTransform.TryGetComponent(out MeshFilter meshFilter)
                    ? meshFilter
                    : _selfTransform.AddComponent<MeshFilter>();

            _visionConeMesh = new Mesh();
            _defaultVisionAngle = _visionAngle * Mathf.Deg2Rad;
            _currentAngle = -_defaultVisionAngle / 2;
            _angleInc = _defaultVisionAngle / (_visionConeResolution - 1);

            _triangles = new int[(_visionConeResolution - 1) * 3];
            _vertices = new Vector3[_visionConeResolution + 1];

            _vertices[0] = Vector3.zero;

            _visualConeRaycastData.Clear();
            
            for (int i = 0; i < _visionConeResolution; i++)
            {
                _visualConeRaycastData.Add(new VisualConeRaycastData()
                {
                    Sin = Mathf.Sin(_currentAngle),
                    Cos = Mathf.Cos(_currentAngle)
                });

                _currentAngle += _angleInc;
            }
            
            for (int i = 0, j = 0; i < _triangles.Length; i += 3, j++)
            {
                _triangles[i] = 0;
                _triangles[i + 1] = j + 1;
                _triangles[i + 2] = j + 2;
            }
        }
        
        private void TryUpdate()
        {
            if(_isDestroyed || _vertices == null || _vertices.Length <= 0) 
                return;
            
            DrawVisionCone();
        }

        private void DrawVisionCone()
        {
            _currentAngle = -_defaultVisionAngle * 0.5f;
            _vertices[0] = Vector3.zero;

            for (int i = 0; i < _visionConeResolution; i++)
            {
                Vector3 raycastDirection = (_selfTransform.forward * _visualConeRaycastData[i].Cos) + (_selfTransform.right * _visualConeRaycastData[i].Sin);
                Vector3 forward = (Vector3.forward * _visualConeRaycastData[i].Cos) + (Vector3.right * _visualConeRaycastData[i].Sin);
                
                _vertices[i + 1] =
                    Physics.SphereCast(transform.position, 0f, raycastDirection, out _raycastHit, _visionRange,
                        _visionObstructingLayer)
                        ? forward * _raycastHit.distance
                        : forward * _visionRange;
                
                _currentAngle += _angleInc;
            }

            _visionConeMesh.Clear();
            _visionConeMesh.vertices = _vertices;
            _visionConeMesh.triangles = _triangles;
            _meshFilter.sharedMesh = _visionConeMesh;
        }

        private void OnLevelReloading(LevelClearingSignal signal)
        {
            _signalBusService.Unsubscribe<LevelClearingSignal>(OnLevelReloading);

            _isDestroyed = true;
        }

        private void OnDestroy()
        {
            _updateDisposable?.Dispose();
            
            if (_initialized == false)
                return;
            
            _signalBusService.Unsubscribe<LevelClearingSignal>(OnLevelReloading);
        }

        private class VisualConeRaycastData
        {
            public float Sin;
            public float Cos;
        }
    }
}
