using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Quark
{
    // Global skin: hides the script row, routes addon lists through AddonList, draws [Button] pills.
    // Any editor registered for a more specific type still wins.
    [CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
    sealed class CharmEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null) { base.OnInspectorGUI(); return; }

            serializedObject.Update();
            var fields = new List<SerializedProperty>();
            var lists = new List<SerializedProperty>();
            var it = serializedObject.GetIterator();
            for (var enter = true; it.NextVisible(enter); enter = false)
            {
                if (it.propertyPath == "m_Script") continue;
                (IsNucleus(it) || AddonList.Is(target, it) ? lists : fields).Add(it.Copy());
            }

            Box(fields, lists);
            serializedObject.ApplyModifiedProperties();
            Buttons();
        }

        bool IsNucleus(SerializedProperty p)
        {
            var info = Reflect.Info(target.GetType(), p.name);
            return info != null && typeof(Nucleus).IsAssignableFrom(info.FieldType);
        }

        SerializedProperty Addons(SerializedProperty p) => IsNucleus(p) ? p.FindPropertyRelative("addons") : p;

        // One surface for the whole component: fields first, addon lists last.
        void Box(List<SerializedProperty> fields, List<SerializedProperty> lists)
        {
            if (fields.Count == 0 && lists.Count == 0) return;

            var h = 6f;
            foreach (var f in fields) h += EditorGUI.GetPropertyHeight(f, true) + 2f;
            if (fields.Count > 0 && lists.Count > 0) h += 4f;
            foreach (var l in lists) h += AddonList.Height(Addons(l));
            h += 2f;

            var r = GUILayoutUtility.GetRect(0f, h, GUILayout.ExpandWidth(true));
            Charm.Fill(r, Charm.Surface, Charm.Radius);
            Charm.Outline(r, Charm.Line, Charm.Radius);

            var old = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Max(110f, (r.width - 20f) * 0.4f);
            var y = r.y + 6f;
            foreach (var f in fields)
            {
                var fh = EditorGUI.GetPropertyHeight(f, true);
                EditorGUI.PropertyField(new Rect(r.x + 10f, y, r.width - 18f, fh), f, true);
                y += fh + 2f;
            }
            if (fields.Count > 0 && lists.Count > 0) y += 4f;
            foreach (var l in lists)
            {
                var addons = Addons(l);
                var lh = AddonList.Height(addons);
                AddonList.Draw(new Rect(r.x + 10f, y, r.width - 18f, lh), addons, target.GetType(), 1);
                y += lh;
            }
            EditorGUIUtility.labelWidth = old;
            GUILayout.Space(4f);
        }

        void Buttons()
        {
            foreach (var method in Methods())
            {
                GUILayout.Space(6f);
                var label = method.GetCustomAttribute<ButtonAttribute>().Label ?? ObjectNames.NicifyVariableName(method.Name);
                if (!Charm.Pill(GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true)), label)) continue;
                foreach (var t in targets)
                {
                    Undo.RecordObject(t, method.Name);
                    method.Invoke(t, null);
                    EditorUtility.SetDirty(t);
                }
            }
        }

        IEnumerable<MethodInfo> Methods() =>
            target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetParameters().Length == 0 && m.IsDefined(typeof(ButtonAttribute), true));
    }
}
