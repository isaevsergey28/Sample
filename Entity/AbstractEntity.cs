using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Codebase.App.Entity
{
    public abstract class AbstractEntity : MonoBehaviour
    {
        protected bool IsPaused;
        protected bool Initialized;
        
        public virtual AbstractEntityComponents GetComponents() => new DefaultEntityComponents();
    }
    
    public abstract class AbstractEntityComponents
    {
        protected Dictionary<string, object> InnerComponents = new();
        public abstract AbstractEntityComponents Initialize(AbstractEntity abstractEntity);
        public abstract AbstractEntityComponents Declare(AbstractEntity abstractEntity);
        public abstract T Select<T>(string id = "") where T : class;
    }
    
    public class DefaultEntityComponents : AbstractEntityComponents
    {
        public override AbstractEntityComponents Initialize(AbstractEntity abstractEntity) => this;

        public override AbstractEntityComponents Declare(AbstractEntity abstractEntity) => this;

        [CanBeNull]
        public override T Select<T>(string id = "")
        {
            return null;
        }
    }
}