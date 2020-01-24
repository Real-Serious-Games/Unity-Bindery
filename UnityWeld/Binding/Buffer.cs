using System.Collections.Generic;
using UnityEngine;

namespace UnityWeld.Binding
{
    public static class Buffer
    {
        private static readonly List<Component> ComponentsBufferInternal = new List<Component>(500);
        private static readonly List<MonoBehaviour> BehavioursBufferInternal = new List<MonoBehaviour>(500);
        private static readonly List<Template> TemplatesBufferInternal = new List<Template>(500);
        private static readonly List<AbstractMemberBinding> AbstractMemberBindingBufferInternal = new List<AbstractMemberBinding>(500);

        public static List<Component> Components
        {
            get
            {
                ComponentsBufferInternal.Clear();
                return ComponentsBufferInternal;
            }
        }

        public static List<MonoBehaviour> Behaviours
        {
            get
            {
                BehavioursBufferInternal.Clear();
                return BehavioursBufferInternal;
            }
        }

        public static List<Template> Templates
        {
            get
            {
                TemplatesBufferInternal.Clear();
                return TemplatesBufferInternal;
            }
        }

        public static List<AbstractMemberBinding> AbstractMemberBindings
        {
            get
            {
                AbstractMemberBindingBufferInternal.Clear();
                return AbstractMemberBindingBufferInternal;
            }
        }
    }
}