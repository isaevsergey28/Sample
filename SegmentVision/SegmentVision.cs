using System;
using Codebase.App.Enums;
using Codebase.App.Tag;
using Codebase.Extension;
using UniRx;
using UnityEngine;

namespace Codebase.App.Vision
{
    public class SegmentVision : MonoBehaviour
    {
        public event Action<GameObject> SawTarget;
        public event Action LostTarget;
        public event Action<GameObject> SawSomething;
        
        public GameObject Target => _target;

        [SerializeField] private bool _showGizmo = false;
        [SerializeField] private bool _ignoreOverriding = true;
        
        [SerializeField] private float _maxAngle = 5f;
        [SerializeField] private float _maxRadius = 10f;
        [SerializeField] private LayerMask _raycastLayerMask;

        private IDisposable _updateDisposable;
        
        private TagCollection _targetTags;
        private GameObject _target;
        private int _layerMask = 0;
        private bool _visionInitialized;
        
        public void Init()
        {
            _visionInitialized = true;

            _updateDisposable = RX.LoopedTimer(0.1f, 0.1f, CheckFieldOfView);
        }
        
        public void SetTargetTags(TagCollection tagCollection)
        {
            _targetTags = tagCollection;
            _layerMask = LayerExtension.ConvertToLayerMask(_targetTags.Tags);

            _targetTags.Tags.ToObservable().ObserveEveryValueChanged(v => v).Subscribe(_ => OnListChanged());
        }

        private void OnListChanged() => _layerMask = LayerExtension.ConvertToLayerMask(_targetTags.Tags);

        public void ChangeAngle(float angle)
        {
            if (_ignoreOverriding)
                return;
            
            _maxAngle = angle;
        }

        public void ChangeRadius(float radius)
        {
            if (_ignoreOverriding)
                return;
            
            _maxRadius = radius;
        }

        public void SetVisionActive(bool value)
        {
            enabled = value;
        }
        
        private void CheckFieldOfView()
        {
            if (_visionInitialized == false)
                return;
            
            Transform checkingObject = transform;
            Vector3 checkingObjectPosition = checkingObject.position;

            Collider[] overlaps = new Collider[10];
            int count = Physics.OverlapSphereNonAlloc(checkingObjectPosition, _maxRadius, overlaps, _layerMask);

            for (int i = 0; i < count; i++)
            {
                if (overlaps[i] != null && overlaps[i].TryGetComponent(out TagHolder holder))
                {
                    if (holder.IsActualTag(_targetTags.Tags))
                    {
                        Vector3 directionBetween = (overlaps[i].transform.position - checkingObjectPosition).normalized;
                        directionBetween.y *= 0;

                        float angle = Vector3.Angle(checkingObject.forward, directionBetween);

                        if (angle <= _maxAngle)
                        {
                            Ray ray = new Ray(checkingObjectPosition, overlaps[i].bounds.center- checkingObjectPosition);
                            Physics.Raycast(ray, out RaycastHit hit, _maxRadius, _raycastLayerMask);
                           
                            if (hit.transform == overlaps[i].transform)
                            {
                                if (_target != holder.Parent)
                                {
                                    _target = holder.Parent;

                                    SawTarget?.Invoke(_target);
                                }
                                else
                                {
                                    SawSomething?.Invoke(_target);
                                }
                                
                                return;
                            }
                        }
                    }
                }
            }

            _target = null;
            
            LostTarget?.Invoke();
        }

        private void OnDestroy() => _updateDisposable?.Dispose();

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if(_showGizmo == false)
                return;

            Vector3 fovLine1 = Quaternion.AngleAxis(_maxAngle, transform.up) * transform.forward * _maxRadius;
            Vector3 fovLine2 = Quaternion.AngleAxis(-_maxAngle, transform.up) * transform.forward * _maxRadius;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, fovLine1);
            Gizmos.DrawRay(transform.position, fovLine2);

            if(!Application.isPlaying || _target == null)
                return;
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, (_target.transform.position - transform.position).normalized * _maxRadius);
        }
#endif
    }
}