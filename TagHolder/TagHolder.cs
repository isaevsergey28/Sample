using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Codebase.App.Tag
{
    public class TagHolder : MonoBehaviour
    {
        public GameObject Parent => _containsCustomParent ? _customParent : gameObject;
        public List<TagType> Tags => _tags;

        [SerializeField] private bool _containsCustomParent = false;

        [ShowIf("@_containsCustomParent == true"), SerializeField]
        private GameObject _customParent;

        [SerializeField] private List<TagType> _tags;

        public void Replace(TagType oldType, TagType newType)
        {
            if (_tags.Contains(oldType) == false)
                return;

            int oldTypeIndex = _tags.ToList().IndexOf(oldType);
            _tags[oldTypeIndex] = newType;
        }

        public bool IsActualTag(List<TagType> tagType)
        {
            if (tagType == null)
                return false;
            
            for (int i = 0; i < tagType.Count; i++)
            {
                if (_tags.Contains(tagType[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}