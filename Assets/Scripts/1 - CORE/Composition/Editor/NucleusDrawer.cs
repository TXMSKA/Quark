using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Quark
{
    [CustomPropertyDrawer(typeof(Nucleus))]
    class NucleusDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            AddonList.Height(property.FindPropertyRelative("addons"));

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            AddonList.Draw(position, property.FindPropertyRelative("addons"), property.serializedObject.targetObject.GetType());
    }

    [CustomPropertyDrawer(typeof(Nucleus<>))]
    sealed class NucleusGenericDrawer : NucleusDrawer { }

    // Charm-styled renderer for any [SerializeReference] List<Addon>, at any nesting depth.
    static class AddonList
    {
        const float Head = 22f, Row = 24f, Gap = 4f;
        const string EnabledField = "<Enabled>k__BackingField";

        // Detected via reflection: a [SerializeReference] list/array whose element type is Addon-derived.
        public static bool Is(object host, SerializedProperty p)
        {
            if (host == null || !p.isArray) return false;
            var info = Reflect.Info(host.GetType(), p.name);
            if (info == null || !info.IsDefined(typeof(SerializeReference), false)) return false;
            var type = info.FieldType;
            var element = type.IsArray ? type.GetElementType()
                : type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? type.GetGenericArguments()[0]
                : null;
            return element != null && typeof(Addon).IsAssignableFrom(element);
        }

        // The addon array behind a child: a direct [SerializeReference] list, or one wrapped in a Nucleus.
        public static SerializedProperty Inner(object host, SerializedProperty p)
        {
            if (p.isArray) return Is(host, p) ? p : null;
            if (host == null || p.propertyType != SerializedPropertyType.Generic) return null;
            var info = Reflect.Info(host.GetType(), p.name);
            return info != null && typeof(Nucleus).IsAssignableFrom(info.FieldType) ? p.FindPropertyRelative("addons") : null;
        }

        public static void Layout(SerializedProperty list, Type host) =>
            Draw(GUILayoutUtility.GetRect(0f, Height(list), GUILayout.ExpandWidth(true)), list, host);

        public static float Height(SerializedProperty list)
        {
            var h = Head + Gap;
            if (list.serializedObject.isEditingMultipleObjects || list.arraySize == 0) return h + 18f;
            for (var i = 0; i < list.arraySize; i++) h += Card(list.GetArrayElementAtIndex(i)) + Gap;
            return h;
        }

        public static void Draw(Rect rect, SerializedProperty list, Type host, int depth = 0)
        {
            var head = new Rect(rect.x, rect.y, rect.width, Head);
            GUI.Label(head, "ADDONS", Charm.MiniDim);
            var w = Charm.MiniDim.CalcSize(new GUIContent("ADDONS")).x;

            var count = list.arraySize.ToString();
            var cw = Charm.Chip.CalcSize(new GUIContent(count)).x + 8f;
            var chip = new Rect(head.x + w + 6f, head.y + (Head - 13f) * 0.5f, cw, 13f);
            Charm.Fill(chip, Charm.Surface2, 6.5f);
            GUI.Label(chip, count, Charm.Chip);

            var y = rect.y + Head + Gap;
            if (list.serializedObject.isEditingMultipleObjects)
            {
                GUI.Label(new Rect(rect.x + 2f, y, rect.width, 16f), "Multi-edit not supported", Charm.Hint);
                return;
            }

            var pill = new Rect(head.xMax - 54f, head.y + (Head - 16f) * 0.5f, 54f, 16f);
            Charm.Rule(chip.xMax + 8f, head.center.y, pill.x - chip.xMax - 16f);
            if (Charm.Pill(pill, "+ Add")) TypeMenu(list, -1, host);

            if (list.arraySize == 0)
            {
                GUI.Label(new Rect(rect.x + 2f, y, rect.width, 16f), "No addons", Charm.Hint);
                return;
            }

            for (var i = 0; i < list.arraySize; i++)
            {
                var h = Card(list.GetArrayElementAtIndex(i));
                DrawCard(new Rect(rect.x, y, rect.width, h), list, i, host, depth);
                y += h + Gap;
            }
        }

        static float Card(SerializedProperty element)
        {
            var body = element.managedReferenceValue == null || !element.isExpanded ? 0f : Body(element);
            return body > 0f ? Row + 4f + body + 4f : Row;
        }

        static float Body(SerializedProperty element)
        {
            var h = 0f;
            var value = element.managedReferenceValue;
            foreach (var child in Children(element))
            {
                if (child.name == EnabledField) continue;
                var list = Inner(value, child);
                h += (list != null ? Height(list) : EditorGUI.GetPropertyHeight(child, true)) + 2f;
            }
            return h;
        }

        static void DrawCard(Rect r, SerializedProperty list, int i, Type host, int depth)
        {
            var element = list.GetArrayElementAtIndex(i);
            var value = element.managedReferenceValue;
            var enabled = element.FindPropertyRelative(EnabledField);
            var on = enabled == null || enabled.boolValue;
            var hasBody = value != null && Body(element) > 0f;
            var expanded = hasBody && element.isExpanded;
            var head = new Rect(r.x, r.y, r.width, Row);

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && head.Contains(e.mousePosition))
            {
                TypeMenu(list, i, host);
                e.Use();
            }

            Charm.Card(r, Row, expanded, depth % 2 == 0 ? Charm.Surface : Charm.Inset);
            Charm.Fill(new Rect(r.x, r.y + 5f, 2f, r.height - 10f), value != null && on ? Charm.Accent : new Color(Charm.Dim.r, Charm.Dim.g, Charm.Dim.b, 0.5f), 1f);

            if (hasBody)
                element.isExpanded = EditorGUI.Foldout(new Rect(head.x + 8f, head.y, 14f, Row), element.isExpanded, GUIContent.none, true);

            var caret = new Rect(head.xMax - 8f - 28f - 6f - 14f, head.y, 14f, Row);
            var name = new Rect(head.x + 26f, head.y, caret.x - head.x - 30f, Row);
            if (value == null)
            {
                if (Charm.Ghost(name, "Select addon…", Charm.NameAccent)) TypeMenu(list, i, host);
            }
            else
            {
                var label = ObjectNames.NicifyVariableName(value.GetType().Name);
                var style = on ? Charm.Name : Charm.NameDim;
                if (hasBody) { if (Charm.Ghost(name, label, style)) element.isExpanded = !element.isExpanded; }
                else GUI.Label(name, label, style);

                if (Charm.Ghost(caret, "▾", Charm.Chip)) TypeMenu(list, i, host);
                if (enabled != null)
                {
                    var next = Charm.Switch(new Rect(head.xMax - 8f - 28f, head.y, 28f, Row), on);
                    if (next != on) enabled.boolValue = next;
                }
            }

            if (!expanded) return;

            var old = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Max(110f, (r.width - 20f) * 0.4f);
            var y = r.y + Row + 4f;
            for (var pass = 0; pass < 2; pass++)
                foreach (var child in Children(element))
                {
                    if (child.name == EnabledField) continue;
                    var nested = Inner(value, child);
                    if ((nested != null) != (pass == 1)) continue;
                    float h;
                    if (nested != null)
                    {
                        h = Height(nested);
                        Draw(new Rect(r.x + 12f, y, r.width - 20f, h), nested, value.GetType(), depth + 1);
                    }
                    else
                    {
                        h = EditorGUI.GetPropertyHeight(child, true);
                        EditorGUI.PropertyField(new Rect(r.x + 12f, y, r.width - 20f, h), child, true);
                    }
                    y += h + 2f;
                }
            EditorGUIUtility.labelWidth = old;
        }

        static IEnumerable<SerializedProperty> Children(SerializedProperty element)
        {
            var it = element.Copy();
            var end = it.GetEndProperty();
            if (!it.NextVisible(true)) yield break;
            while (!SerializedProperty.EqualContents(it, end))
            {
                yield return it;
                if (!it.NextVisible(false)) break;
            }
        }

        static void TypeMenu(SerializedProperty list, int index, Type host)
        {
            var prop = list.Copy();
            var menu = new GenericMenu();
            var current = index >= 0 ? prop.GetArrayElementAtIndex(index).managedReferenceValue?.GetType() : null;

            var types = TypeCache.GetTypesDerivedFrom<Addon>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && Owner(t) is { } o && o.IsAssignableFrom(host))
                .OrderBy(t => t.Name).ToList();
            foreach (var type in types)
            {
                var pick = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(pick.Name)), pick == current, () => Set(prop, index, Activator.CreateInstance(pick)));
            }
            if (types.Count == 0) menu.AddDisabledItem(new GUIContent($"No addons for {host.Name}"));

            if (index >= 0)
            {
                menu.AddSeparator("");
                Item(menu, "Move Up", index > 0, () => Move(prop, index, index - 1));
                Item(menu, "Move Down", index < prop.arraySize - 1, () => Move(prop, index, index + 1));
                menu.AddSeparator("");
                Item(menu, "Clear", current != null, () => Set(prop, index, null));
                Item(menu, "Remove", true, () => { prop.DeleteArrayElementAtIndex(index); Apply(prop); });
            }
            menu.ShowAsContext();
        }

        static void Item(GenericMenu menu, string label, bool enabled, GenericMenu.MenuFunction action)
        {
            if (enabled) menu.AddItem(new GUIContent(label), false, action);
            else menu.AddDisabledItem(new GUIContent(label));
        }

        static Type Owner(Type type)
        {
            for (var t = type; t != null; t = t.BaseType)
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Addon<>))
                    return t.GetGenericArguments()[0];
            return null;
        }

        static void Set(SerializedProperty list, int index, object value)
        {
            if (index < 0)
            {
                index = list.arraySize;
                list.arraySize++;
            }
            var element = list.GetArrayElementAtIndex(index);
            element.managedReferenceValue = value;
            element.isExpanded = true;
            Apply(list);
        }

        static void Move(SerializedProperty list, int from, int to)
        {
            list.MoveArrayElement(from, to);
            Apply(list);
        }

        static void Apply(SerializedProperty list) => list.serializedObject.ApplyModifiedProperties();
    }
}
