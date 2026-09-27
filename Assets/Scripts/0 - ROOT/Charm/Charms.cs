using System;
using UnityEngine;

namespace Quark
{
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
