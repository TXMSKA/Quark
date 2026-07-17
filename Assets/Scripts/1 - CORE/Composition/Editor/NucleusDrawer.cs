using System;
using UnityEditor;
using UnityEngine;

namespace Quark
{
    [CustomPropertyDrawer(typeof(Nucleus<>))]
    sealed class NucleusDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property.FindPropertyRelative("addons"), label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            EditorGUI.PropertyField(position, property.FindPropertyRelative("addons"), new GUIContent("Addons"), true);
    }

    [CustomPropertyDrawer(typeof(Addon), true)]
    sealed class AddonDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            property.managedReferenceValue == null
                ? EditorGUIUtility.singleLineHeight
                : EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var current = property.managedReferenceValue;

            if (current != null)
                EditorGUI.PropertyField(position, property, new GUIContent(current.GetType().Name), true);
            else
                EditorGUI.LabelField(position, label);

            var header = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                                  position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            if (EditorGUI.DropdownButton(header, new GUIContent(current == null ? "Select Addon…" : current.GetType().Name), FocusType.Keyboard))
                ShowMenu(property);
        }

        static void ShowMenu(SerializedProperty property)
        {
            var prop = property.Copy();
            var menu = new GenericMenu();
            var host = property.serializedObject.targetObject.GetType();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(Addon)))
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition) continue;
                var ownerType = AddonOwnerType(type);
                if (ownerType == null || !ownerType.IsAssignableFrom(host)) continue;
                menu.AddItem(new GUIContent(type.Name), false, () => Assign(prop, Activator.CreateInstance(type)));
            }

            if (menu.GetItemCount() == 0) menu.AddDisabledItem(new GUIContent("No addons available"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("None"), false, () => Assign(prop, null));
            menu.ShowAsContext();
        }

        static Type AddonOwnerType(Type type)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Addon<>))
                    return t.GetGenericArguments()[0];
            }
            return null;
        }

        static void Assign(SerializedProperty property, object value)
        {
            property.managedReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
