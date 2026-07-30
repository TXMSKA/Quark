using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Quark
{
    // Restyles native [Header] everywhere: accent caps + hairline.
    [CustomPropertyDrawer(typeof(HeaderAttribute))]
    sealed class HeaderCharm : DecoratorDrawer
    {
        public override float GetHeight() => 26f;

        public override void OnGUI(Rect position)
        {
            var r = EditorGUI.IndentedRect(position);
            var text = ((HeaderAttribute)attribute).header.ToUpperInvariant();
            var row = new Rect(r.x, r.yMax - 18f, r.width, 16f);
            GUI.Label(row, text, Charm.MiniAccent);
            var w = Charm.MiniAccent.CalcSize(new GUIContent(text)).x;
            Charm.Rule(row.x + w + 8f, row.center.y, row.width - w - 8f);
        }
    }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    sealed class ReadOnlyCharm : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.PropertyField(position, property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(MinMaxAttribute))]
    sealed class MinMaxCharm : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var attr = (MinMaxAttribute)attribute;
            var value = property.vector2Value;
            var rect = EditorGUI.PrefixLabel(position, label);
            var left = new Rect(rect.x, rect.y, 40f, rect.height);
            var right = new Rect(rect.xMax - 40f, rect.y, 40f, rect.height);
            var slider = new Rect(left.xMax + 6f, rect.y, rect.width - 92f, rect.height);
            value.x = EditorGUI.FloatField(left, value.x);
            value.y = EditorGUI.FloatField(right, value.y);
            EditorGUI.MinMaxSlider(slider, ref value.x, ref value.y, attr.Min, attr.Max);
            property.vector2Value = value;
        }
    }

    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    sealed class ShowIfCharm : PropertyDrawer
    {
        bool Visible(SerializedProperty property)
        {
            var attr = (ShowIfAttribute)attribute;
            return Reflect.Show(Reflect.Host(property), attr.Member, attr.Invert);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            Visible(property) ? EditorGUI.GetPropertyHeight(property, label, true) : -EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (Visible(property))
                EditorGUI.PropertyField(position, property, label, true);
        }
    }

    // Resolves the object instance declaring a property, through nested classes, lists and [SerializeReference].
    static class Reflect
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static object Host(SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            var parts = property.propertyPath.Replace(".Array.data[", "[").Split('.');
            for (var i = 0; i < parts.Length - 1 && obj != null; i++)
            {
                var part = parts[i];
                var bracket = part.IndexOf('[');
                if (bracket < 0) { obj = Field(obj, part); continue; }
                var index = int.Parse(part[(bracket + 1)..^1]);
                obj = Field(obj, part[..bracket]) is IList list && index < list.Count ? list[index] : null;
            }
            return obj;
        }

        public static bool Show(object host, string member, bool invert)
        {
            for (var t = host?.GetType(); t != null; t = t.BaseType)
            {
                if (t.GetField(member, Flags) is { } f && f.FieldType == typeof(bool)) return (bool)f.GetValue(host) != invert;
                if (t.GetProperty(member, Flags) is { } p && p.PropertyType == typeof(bool) && p.CanRead) return (bool)p.GetValue(host) != invert;
                if (t.GetMethod(member, Flags, null, Type.EmptyTypes, null) is { } m && m.ReturnType == typeof(bool)) return (bool)m.Invoke(host, null) != invert;
            }
            return true;
        }

        public static FieldInfo Info(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
                if (t.GetField(name, Flags) is { } f) return f;
            return null;
        }

        static object Field(object obj, string name) => Info(obj.GetType(), name)?.GetValue(obj);
    }
}
