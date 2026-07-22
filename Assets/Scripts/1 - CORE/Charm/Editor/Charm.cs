using UnityEditor;
using UnityEngine;

namespace Quark
{
    // AtomOS design tokens + IMGUI primitives shared by all charm editors.
    static class Charm
    {
        static bool Dark => EditorGUIUtility.isProSkin;

        public static Color Surface => Dark ? Hex(0x242429) : Hex(0xF4F2F1);
        public static Color Surface2 => Dark ? Hex(0x2B2B31) : Hex(0xFBFAF9);
        public static Color Inset => Dark ? Hex(0x17171A) : Hex(0xE3E0DE);
        public static Color Line => Dark ? new Color(0.93f, 0.91f, 0.91f, 0.13f) : new Color(0.22f, 0.22f, 0.25f, 0.14f);
        public static Color Dim => Dark ? Hex(0x84848C) : Hex(0x8A8A92);
        public static Color Title => Dark ? Hex(0xECE9E8) : Hex(0x383940);
        public static Color Accent => Dark ? Hex(0xD8794E) : Hex(0xC4643C);
        public static Color AccentSoft => new(0.77f, 0.39f, 0.24f, Dark ? 0.18f : 0.12f);

        static Color Hex(int v) => new(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);

        static GUIStyle miniAccent, miniDim, name, nameDim, nameAccent, hint, chip, button;
        public static GUIStyle MiniAccent => miniAccent ??= Mini(Accent);
        public static GUIStyle MiniDim => miniDim ??= Mini(Dim);
        public static GUIStyle Name => name ??= Label(12, FontStyle.Bold, Title);
        public static GUIStyle NameDim => nameDim ??= Label(12, FontStyle.Bold, Dim);
        public static GUIStyle NameAccent => nameAccent ??= Label(12, FontStyle.Bold, Accent);
        public static GUIStyle Hint => hint ??= Label(11, FontStyle.Italic, Dim);
        public static GUIStyle Chip => chip ??= new GUIStyle(EditorStyles.miniBoldLabel) { fontSize = 9, alignment = TextAnchor.MiddleCenter, normal = { textColor = Dim } };
        public static GUIStyle Button => button ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, alignment = TextAnchor.MiddleCenter, normal = { textColor = Accent } };

        static GUIStyle Mini(Color color) => new(EditorStyles.miniBoldLabel) { fontSize = 10, alignment = TextAnchor.MiddleLeft, normal = { textColor = color } };
        static GUIStyle Label(int size, FontStyle style, Color color) => new(EditorStyles.label) { fontSize = size, fontStyle = style, alignment = TextAnchor.MiddleLeft, normal = { textColor = color } };

        public static void Fill(Rect r, Color c, float radius) => Fill(r, c, new Vector4(radius, radius, radius, radius));

        public static void Fill(Rect r, Color c, Vector4 radii)
        {
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.zero, radii);
        }

        public static void Outline(Rect r, Color c, float radius)
        {
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, Vector4.one, new Vector4(radius, radius, radius, radius));
        }

        public static void Rule(float x, float y, float width) => Fill(new Rect(x, y, Mathf.Max(0f, width), 1f), Line, 0f);

        public const float Radius = 5f;

        public static void Card(Rect r, float header, bool expanded, Color body)
        {
            var head = new Rect(r.x, r.y, r.width, header);
            if (expanded)
            {
                Fill(head, Surface2, new Vector4(Radius, Radius, 0f, 0f));
                Fill(new Rect(r.x, r.y + header, r.width, r.height - header), body, new Vector4(0f, 0f, Radius, Radius));
            }
            else Fill(head, Surface2, Radius);
            Outline(r, Line, Radius);
        }

        public static bool Ghost(Rect r, string label, GUIStyle style)
        {
            GUI.Label(r, label, style);
            EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        public static bool Pill(Rect r, string label)
        {
            Fill(r, AccentSoft, r.height * 0.5f);
            Outline(r, new Color(Accent.r, Accent.g, Accent.b, 0.35f), r.height * 0.5f);
            return Ghost(r, label, Button);
        }

        public static bool Switch(Rect r, bool value)
        {
            var track = new Rect(r.x, r.y + (r.height - 14f) * 0.5f, 28f, 14f);
            Fill(track, value ? Accent : Inset, 7f);
            if (!value) Outline(track, Line, 7f);
            Fill(new Rect(value ? track.xMax - 12f : track.x + 2f, track.y + 2f, 10f, 10f), value ? Surface2 : Dim, 5f);
            EditorGUIUtility.AddCursorRect(track, MouseCursor.Link);
            return GUI.Button(track, GUIContent.none, GUIStyle.none) ? !value : value;
        }
    }
}
