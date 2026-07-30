using System;
using UnityEngine;

namespace Quark
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ButtonAttribute : Attribute
    {
        public string Label { get; }
        public ButtonAttribute(string label = null) => Label = label;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReadOnlyAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinMaxAttribute : PropertyAttribute
    {
        public float Min { get; }
        public float Max { get; }
        public MinMaxAttribute(float min, float max) { Min = min; Max = max; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShowIfAttribute : PropertyAttribute
    {
        public string Member { get; }
        public bool Invert { get; }
        public ShowIfAttribute(string member, bool invert = false) { Member = member; Invert = invert; }
    }
}
