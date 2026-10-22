using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Quark
{
    // Map builder: place pieces on the grid, paint decoration and drag placed pieces, all in the Scene view.
    [EditorTool("Map")]
    sealed class Builder : EditorTool
    {
        #region FIELDS

        public enum Tool { Place, Paint, Move }

        public static readonly List<GameObject> Picks = new();

        static readonly Color Blocked = new(0.9f, 0.25f, 0.25f);
        static readonly string[] Wheel = { "Place", "Paint", "Move", "Snap" };

        static Tool mode;
        static GameObject ghost, ghostSource;
        static Snap.Spot spot;
        static Snap.Hit hit;
        static Vector3 footprint;
        static bool blocked, aim;
        static Vector2 mouse;

        static Vector2 downAt;
        static Bounds stamped;
        static bool dragging, painted;
        static int group;
        static GameObject downOn;
        static Transform[] moving;
        static Vector3 anchor, currentMin;
        static Quaternion turn = Quaternion.identity;
        static float scale = 1f;

        static bool brushHit;
        static Vector3 brushAt, brushNormal, lastDab;

        static bool wheel;
        static Vector2 wheelAt, middleAt;
        static double middleTime;

        GUIContent icon;

        #endregion

        #region LIFETIME

        public override GUIContent toolbarIcon
        {
            get
            {
                if (icon != null) return icon;
                var texture = EditorGUIUtility.FindTexture("Grid.PaintTool");
                return icon = texture != null ? new GUIContent(texture, "Map (B)") : new GUIContent("Map", "Map (B)");
            }
        }

        public override void OnActivated()
        {
            SceneView.beforeSceneGui += Before;
            AssemblyReloadEvents.beforeAssemblyReload += ClearGhost;
            EditorApplication.playModeStateChanged += Leave;
        }

        public override void OnWillBeDeactivated()
        {
            SceneView.beforeSceneGui -= Before;
            AssemblyReloadEvents.beforeAssemblyReload -= ClearGhost;
            EditorApplication.playModeStateChanged -= Leave;
            ClearGhost();
            Library.open = false;
            wheel = false;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView view) return;
            var e = Event.current;
            mouse = e.mousePosition;

            if (Library.Draw(view)) { HideGhost(); return; }
            if (wheel) { Radial(view, e); return; }
            if (e.type == EventType.MouseLeaveWindow) HideGhost();
            if (Drop(e)) return;

            switch (mode)
            {
                case Tool.Place: Place(e); break;
                case Tool.Paint: Paint(e); break;
                case Tool.Move: Move(e); break;
            }
        }

        #endregion

        #region API

        public static Tool Mode
        {
            get => mode;
            set
            {
                mode = value;
                if (mode != Tool.Paint && Picks.Count > 1) Picks.RemoveRange(1, Picks.Count - 1);
                ClearGhost();
                SceneView.RepaintAll();
            }
        }

        public static void Pick(GameObject go)
        {
            if (mode == Tool.Paint)
            {
                if (!Picks.Remove(go)) Picks.Add(go);
            }
            else
            {
                Picks.Clear();
                Picks.Add(go);
                if (mode == Tool.Move) Mode = Tool.Place;
            }
            aim = true;
            SceneView.RepaintAll();
        }

        public static void Release()
        {
            Picks.Clear();
            ClearGhost();
            SceneView.RepaintAll();
        }

        public static void SetGrid(float size)
        {
            EditorSnapSettings.gridSize = Vector3.one * Mathf.Max(0.01f, size);
            aim = true;
            SceneView.RepaintAll();
        }

        public static void MatchGrid()
        {
            Bounds b;
            if (Picks.Count > 0) b = Snap.Of(Picks[0], Quaternion.identity);
            else if (Selection.activeGameObject != null) b = Snap.World(Selection.activeGameObject);
            else return;
            SetGrid(Mathf.Round(Mathf.Max(b.size.x, b.size.z) * 100f) / 100f);
        }

        #endregion

        #region MISC

        [Shortcut("Quark/Map/Toggle Tool", typeof(SceneView), KeyCode.B)]
        static void Toggle()
        {
            if (ToolManager.activeToolType == typeof(Builder)) ToolManager.RestorePreviousPersistentTool();
            else ToolManager.SetActiveTool<Builder>();
        }

        [Shortcut("Quark/Map/Turn X", typeof(SceneView), KeyCode.U)]
        static void TurnX() => Turn(Vector3.right, 1f);

        [Shortcut("Quark/Map/Turn X Back", typeof(SceneView), KeyCode.U, ShortcutModifiers.Shift)]
        static void TurnXBack() => Turn(Vector3.right, -1f);

        [Shortcut("Quark/Map/Turn Y", typeof(SceneView), KeyCode.I)]
        static void TurnY() => Turn(Vector3.up, 1f);

        [Shortcut("Quark/Map/Turn Y Back", typeof(SceneView), KeyCode.I, ShortcutModifiers.Shift)]
        static void TurnYBack() => Turn(Vector3.up, -1f);

        [Shortcut("Quark/Map/Turn Z", typeof(SceneView), KeyCode.O)]
        static void TurnZ() => Turn(Vector3.forward, 1f);

        [Shortcut("Quark/Map/Turn Z Back", typeof(SceneView), KeyCode.O, ShortcutModifiers.Shift)]
        static void TurnZBack() => Turn(Vector3.forward, -1f);

        // 45 degree steps around world axes, applied to the piece being placed.
        static void Turn(Vector3 axis, float sign)
        {
            if (ToolManager.activeToolType != typeof(Builder)) return;
            turn = Quaternion.AngleAxis(45f * sign, axis) * turn;
            aim = true;
            SceneView.RepaintAll();
        }

        public static string Turned
        {
            get
            {
                var e = turn.eulerAngles;
                var text = "";
                for (var i = 0; i < 3; i++)
                {
                    var v = Mathf.Round(e[i]) % 360f;
                    if (v != 0f) text += $"{"XYZ"[i]} {v}°  ";
                }
                return text.Length == 0 ? "0°" : text.TrimEnd();
            }
        }

        public static void ResetTurn()
        {
            turn = Quaternion.identity;
            aim = true;
            SceneView.RepaintAll();
        }

        [Shortcut("Quark/Map/Grow", typeof(SceneView), KeyCode.Equals)]
        static void Grow() => Resize(0.25f);

        [Shortcut("Quark/Map/Grow (Keypad)", typeof(SceneView), KeyCode.KeypadPlus)]
        static void GrowPad() => Resize(0.25f);

        [Shortcut("Quark/Map/Shrink", typeof(SceneView), KeyCode.Minus)]
        static void Shrink() => Resize(-0.25f);

        [Shortcut("Quark/Map/Shrink (Keypad)", typeof(SceneView), KeyCode.KeypadMinus)]
        static void ShrinkPad() => Resize(-0.25f);

        static void Resize(float step)
        {
            if (ToolManager.activeToolType != typeof(Builder)) return;
            Scale = scale + step;
        }

        public static float Scale
        {
            get => scale;
            set
            {
                scale = Mathf.Max(0.25f, value);
                aim = true;
                SceneView.RepaintAll();
            }
        }

        static void ToggleSnap()
        {
            var shelf = Shelf.Instance;
            shelf.Edit("Toggle Snap");
            shelf.snap = !shelf.snap;
            aim = true;
            SceneView.RepaintAll();
        }

        static void Leave(PlayModeStateChange change) => ClearGhost();

        // Middle tap (no drag) opens the wheel; a middle drag still pans the camera.
        static void Before(SceneView view)
        {
            var e = Event.current;
            if (e.button != 2) return;
            if (e.type == EventType.MouseDown)
            {
                middleAt = e.mousePosition;
                middleTime = EditorApplication.timeSinceStartup;
            }
            else if (e.type == EventType.MouseUp && (e.mousePosition - middleAt).sqrMagnitude < 25f && EditorApplication.timeSinceStartup - middleTime < 0.35)
            {
                wheel = true;
                wheelAt = e.mousePosition;
                HideGhost();
                view.Repaint();
            }
        }

        static void Radial(SceneView view, Event e)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);
            var delta = e.mousePosition - wheelAt;
            var hover = -1;
            if (delta.sqrMagnitude > 400f)
                hover = Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg, 360f) / 90f) % 4;

            Handles.BeginGUI();
            Charm.Fill(new Rect(wheelAt.x - 10f, wheelAt.y - 10f, 20f, 20f), Charm.Surface, 10f);
            Charm.Outline(new Rect(wheelAt.x - 10f, wheelAt.y - 10f, 20f, 20f), Charm.Line, 10f);
            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f * Mathf.Deg2Rad;
                var center = wheelAt + new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * 72f;
                var r = new Rect(center.x - 42f, center.y - 13f, 84f, 26f);
                var on = i == hover || (i < 3 && (int)mode == i);
                Charm.Fill(r, i == hover ? Charm.Accent : Charm.Surface2, 13f);
                Charm.Outline(r, on ? Charm.Accent : Charm.Line, 13f);
                var label = i == 3 ? (Shelf.Instance.snap ? "Snap on" : "Snap off") : Wheel[i];
                var style = new GUIStyle(i == hover ? Charm.Name : on ? Charm.NameAccent : Charm.NameDim) { alignment = TextAnchor.MiddleCenter };
                if (i == hover) style.normal.textColor = Charm.Surface2;
                GUI.Label(r, label, style);
            }
            Handles.EndGUI();

            if (e.type == EventType.MouseMove) view.Repaint();
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (hover == 3) ToggleSnap();
                else if (hover >= 0) Mode = (Tool)hover;
                wheel = false;
                e.Use();
            }
            else if ((e.type == EventType.MouseDown && e.button == 1) || (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape))
            {
                wheel = false;
                e.Use();
            }
        }

        // Any prefab or model dragged into the scene lands snapped.
        static bool Drop(Event e)
        {
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform && e.type != EventType.DragExited) return false;
            GameObject prefab = null;
            foreach (var o in DragAndDrop.objectReferences)
                if (o is GameObject go && EditorUtility.IsPersistent(go)) { prefab = go; break; }
            if (prefab == null) return false;

            if (e.type == EventType.DragExited) { ClearGhost(); return false; }
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            Aim(prefab, e.mousePosition, e.shift);
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (spot.valid && !blocked) Selection.activeGameObject = Put(prefab, Position(prefab), Rotation(prefab), scale);
                ClearGhost();
            }
            e.Use();
            return true;
        }

        static void Place(Event e)
        {
            var piece = Picks.Count > 0 ? Picks[0] : null;
            if (piece == null) { ClearGhost(); return; }

            var id = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                Picks.Clear();
                ClearGhost();
                e.Use();
                return;
            }
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(id);
                if (aim) Fit(piece, e.shift);
            }

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseMove:
                    Aim(piece, e.mousePosition, e.shift);
                    HandleUtility.Repaint();
                    break;
                case EventType.MouseDown when e.button == 0 && !e.alt:
                    GUIUtility.hotControl = id;
                    Begin("Place Pieces");
                    downAt = e.mousePosition;
                    Aim(piece, e.mousePosition, e.shift);
                    Stamp(piece, true);
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    Aim(piece, e.mousePosition, e.shift);
                    Stamp(piece, false);
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    End();
                    e.Use();
                    break;
                case EventType.Repaint:
                    Footprint();
                    break;
            }
        }

        // A click places one piece; dragging lays more only past a small threshold and never over the last one.
        static void Stamp(GameObject piece, bool first)
        {
            if (!spot.valid || blocked) return;
            var area = new Bounds(spot.min + footprint * 0.5f, footprint * 0.98f);
            if (!first && ((mouse - downAt).sqrMagnitude < 144f || area.Intersects(stamped))) return;
            Put(piece, Position(piece), Rotation(piece), scale);
            stamped = area;
            blocked = true;
        }

        static void Aim(GameObject prefab, Vector2 at, bool free)
        {
            hit = Snap.Cast(at, Every(ghost));
            Fit(prefab, free);
        }

        // Re-fits the last hit without picking again, so it is safe outside mouse events.
        static void Fit(GameObject prefab, bool free)
        {
            aim = false;
            var b = Snap.Of(prefab, turn, scale);
            footprint = b.size;
            spot = Snap.Fit(hit, b.size, Shelf.Instance.snap && !free);
            if (!spot.valid) { HideGhost(); return; }
            Ghost(prefab, Position(prefab), Rotation(prefab));
            blocked = Exists(prefab, Position(prefab), Rotation(prefab));
        }

        static Vector3 Position(GameObject prefab) => spot.min - Snap.Of(prefab, turn, scale).min;

        static Quaternion Rotation(GameObject prefab) => turn * prefab.transform.localRotation;

        static void Footprint()
        {
            if (!spot.valid) return;
            var min = spot.min;
            var max = spot.min + footprint;
            var y = min.y + 0.02f;

            if (Shelf.Instance.snap)
            {
                const int n = 4;
                var g = Snap.Grid;
                var c = (min + max) * 0.5f;
                var x0 = Mathf.Floor(c.x / g.x) * g.x;
                var z0 = Mathf.Floor(c.z / g.z) * g.z;
                var reach = n * Mathf.Max(g.x, g.z);
                var old = Handles.color;
                for (var i = -n; i <= n + 1; i++)
                    for (var j = -n; j <= n; j++)
                    {
                        Segment(new Vector3(x0 + i * g.x, y, z0 + j * g.z), new Vector3(x0 + i * g.x, y, z0 + (j + 1) * g.z), c, reach);
                        Segment(new Vector3(x0 + j * g.x, y, z0 + i * g.z), new Vector3(x0 + (j + 1) * g.x, y, z0 + i * g.z), c, reach);
                    }
                Handles.color = old;
            }

            var color = blocked ? Blocked : Charm.Accent;
            var fill = color;
            fill.a = 0.2f;
            Handles.DrawSolidRectangleWithOutline(new[]
            {
                new Vector3(min.x, y, min.z), new Vector3(max.x, y, min.z),
                new Vector3(max.x, y, max.z), new Vector3(min.x, y, max.z),
            }, fill, color);
        }

        static void Segment(Vector3 a, Vector3 b, Vector3 center, float reach)
        {
            var mid = (a + b) * 0.5f;
            var fade = 1f - Vector2.Distance(new Vector2(mid.x, mid.z), new Vector2(center.x, center.z)) / reach;
            if (fade <= 0f) return;
            Handles.color = new Color(1f, 1f, 1f, fade * 0.45f);
            Handles.DrawLine(a, b);
        }

        static void Paint(Event e)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && Picks.Count > 0) { Release(); e.Use(); return; }
            var shelf = Shelf.Instance;

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseMove:
                    brushHit = HandleUtility.PlaceObject(e.mousePosition, out brushAt, out brushNormal);
                    HandleUtility.Repaint();
                    break;
                case EventType.MouseDown when e.button == 0 && !e.alt:
                    GUIUtility.hotControl = id;
                    painted = false;
                    Begin(e.shift ? "Erase Pieces" : "Paint Pieces");
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    brushHit = HandleUtility.PlaceObject(e.mousePosition, out brushAt, out brushNormal);
                    if (brushHit && (!painted || Vector3.Distance(brushAt, lastDab) > shelf.radius * 0.5f))
                    {
                        if (e.shift) Erase(brushAt, shelf.radius);
                        else Dab(brushAt, shelf);
                        lastDab = brushAt;
                        painted = true;
                    }
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    brushHit = HandleUtility.PlaceObject(e.mousePosition, out brushAt, out brushNormal);
                    if (!painted && brushHit)
                    {
                        if (e.shift) Erase(brushAt, shelf.radius);
                        else Scatter(brushAt, brushNormal, shelf, null);
                    }
                    End();
                    e.Use();
                    break;
                case EventType.Repaint when brushHit:
                    var color = e.shift ? Blocked : Charm.Accent;
                    Handles.color = color;
                    Handles.DrawWireDisc(brushAt, brushNormal, shelf.radius);
                    color.a = 0.08f + shelf.strength * 0.17f;
                    Handles.color = color;
                    Handles.DrawSolidDisc(brushAt, brushNormal, shelf.radius);
                    break;
            }
        }

        static void Dab(Vector3 center, Shelf shelf)
        {
            var taken = Painted();
            var tries = Mathf.Max(1, Mathf.RoundToInt(shelf.strength * 12f));
            for (var i = 0; i < tries; i++)
            {
                var offset = Random.insideUnitCircle * shelf.radius;
                var sample = HandleUtility.WorldToGUIPoint(center + new Vector3(offset.x, 0f, offset.y));
                if (HandleUtility.PlaceObject(sample, out var at, out var normal)) Scatter(at, normal, shelf, taken);
            }
        }

        // One random piece of the mix on the surface; skipped on steep slopes or too close to another.
        static void Scatter(Vector3 at, Vector3 normal, Shelf shelf, List<Vector3> taken)
        {
            if (Picks.Count == 0 || Vector3.Angle(normal, Vector3.up) > shelf.slope) return;
            var prefab = Picks[Random.Range(0, Picks.Count)];
            var spin = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var size = Random.Range(shelf.scale.x, shelf.scale.y);
            var b = Snap.Of(prefab, spin, size);

            if (taken != null)
            {
                var gap = Mathf.Max(b.extents.x, b.extents.z) * 2f * Mathf.Lerp(2.5f, 0.7f, shelf.strength);
                foreach (var p in taken)
                    if (new Vector2(p.x - at.x, p.z - at.z).sqrMagnitude < gap * gap) return;
                taken.Add(at);
            }

            Put(prefab, at - new Vector3(b.center.x, b.min.y, b.center.z), spin * prefab.transform.localRotation, size);
        }

        static List<Vector3> Painted()
        {
            var list = new List<Vector3>();
            foreach (var prefab in Picks)
            {
                var parent = Group(prefab, false);
                if (parent == null) continue;
                foreach (Transform child in parent) list.Add(child.position);
            }
            return list;
        }

        static void Erase(Vector3 center, float radius)
        {
            var doomed = new List<GameObject>();
            foreach (var prefab in Picks)
            {
                var parent = Group(prefab, false);
                if (parent == null) continue;
                foreach (Transform child in parent)
                    if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab
                        && new Vector2(child.position.x - center.x, child.position.z - center.z).sqrMagnitude <= radius * radius)
                        doomed.Add(child.gameObject);
            }
            foreach (var go in doomed) Undo.DestroyObjectImmediate(go);
        }

        static void Move(Event e)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown when e.button == 0 && !e.alt:
                    var hit = HandleUtility.PickGameObject(e.mousePosition, true);
                    if (hit == null || !Snap.InMap(hit.transform)) break;
                    downOn = Snap.Root(hit);
                    downAt = e.mousePosition;
                    dragging = false;
                    GUIUtility.hotControl = id;
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    if (!dragging)
                    {
                        if ((e.mousePosition - downAt).sqrMagnitude < 16f) break;
                        Grab(e);
                        dragging = true;
                    }
                    Drag(e);
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    if (dragging) End();
                    else if (e.shift || e.control || e.command)
                    {
                        var list = new List<Object>(Selection.objects);
                        if (!list.Remove(downOn)) list.Add(downOn);
                        Selection.objects = list.ToArray();
                    }
                    else Selection.activeGameObject = downOn;
                    dragging = false;
                    spot.valid = false;
                    e.Use();
                    break;
                case EventType.Repaint when dragging:
                    Footprint();
                    break;
            }
        }

        static void Grab(Event e)
        {
            Begin("Move Pieces");
            if (!Selection.Contains(downOn)) Selection.activeGameObject = downOn;
            if (e.control || e.command) Unsupported.DuplicateGameObjectsUsingPasteboard();

            moving = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);
            var roots = new List<GameObject>();
            foreach (var t in moving) roots.Add(t.gameObject);

            var box = Snap.World(roots[0]);
            for (var i = 1; i < roots.Count; i++) box.Encapsulate(Snap.World(roots[i]));
            footprint = box.size;
            currentMin = box.min;

            anchor = Plane(downAt, box.min.y, out var p) ? box.min - p : Snap.Centered(footprint);
        }

        static void Drag(Event e)
        {
            // Slides on the plane of the piece's base, so the piece stays under the mouse.
            if (!Plane(e.mousePosition, currentMin.y, out var p)) return;
            var target = p + anchor;
            if (Shelf.Instance.snap && !e.shift)
            {
                var g = Snap.Grid;
                target.x = Mathf.Round(target.x / g.x) * g.x;
                target.z = Mathf.Round(target.z / g.z) * g.z;
            }
            target.y = currentMin.y;

            var delta = target - currentMin;
            if (delta.sqrMagnitude < 1e-8f) return;
            Undo.RecordObjects(moving, "Move Pieces");
            foreach (var t in moving) t.position += delta;
            currentMin = target;
            spot = new Snap.Spot { valid = true, min = target };
            blocked = false;
        }

        static bool Plane(Vector2 at, float height, out Vector3 point)
        {
            var ray = HandleUtility.GUIPointToWorldRay(at);
            point = default;
            if (Mathf.Abs(ray.direction.y) < 1e-4f) return false;
            var t = (height - ray.origin.y) / ray.direction.y;
            if (t <= 0f) return false;
            point = ray.GetPoint(t);
            return true;
        }

        static void Begin(string label)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(label);
            group = Undo.GetCurrentGroup();
        }

        static void End() => Undo.CollapseUndoOperations(group);

        static GameObject Put(GameObject prefab, Vector3 position, Quaternion rotation, float size)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, Group(prefab, true));
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = prefab.transform.localScale * size;
            Undo.RegisterCreatedObjectUndo(go, "Place " + prefab.name);
            return go;
        }

        static bool Exists(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var parent = Group(prefab, false);
            if (parent == null) return false;
            foreach (Transform child in parent)
                if ((child.position - position).sqrMagnitude < 1e-4f && Quaternion.Angle(child.rotation, rotation) < 1f
                    && (child.localScale - prefab.transform.localScale * scale).sqrMagnitude < 1e-4f
                    && PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab)
                    return true;
            return false;
        }

        // Map / <folder of the piece>, created on demand.
        static Transform Group(GameObject prefab, bool create)
        {
            var category = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(prefab)));
            if (string.IsNullOrEmpty(category)) category = "Pieces";

            GameObject map = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == "Map") { map = root; break; }
            if (map == null)
            {
                if (!create) return null;
                map = new GameObject("Map");
                Undo.RegisterCreatedObjectUndo(map, "Create Map");
            }

            var t = map.transform.Find(category);
            if (t != null || !create) return t;
            var child = new GameObject(category);
            Undo.RegisterCreatedObjectUndo(child, "Create Group");
            Undo.SetTransformParent(child.transform, map.transform, "Create Group");
            return child.transform;
        }

        static void Ghost(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (ghost == null || ghostSource != prefab)
            {
                ClearGhost();
                ghost = Instantiate(prefab);
                ghost.name = "Map Ghost";
                foreach (var t in ghost.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.HideAndDontSave;
                foreach (var c in ghost.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                ghostSource = prefab;
            }
            ghost.transform.SetPositionAndRotation(position, rotation);
            ghost.transform.localScale = prefab.transform.localScale * scale;
            if (!ghost.activeSelf) ghost.SetActive(true);
        }

        static void HideGhost()
        {
            spot.valid = false;
            if (ghost != null && ghost.activeSelf) ghost.SetActive(false);
        }

        static void ClearGhost()
        {
            spot.valid = false;
            if (ghost != null) DestroyImmediate(ghost);
            ghost = null;
            ghostSource = null;
        }

        static GameObject[] Every(params GameObject[] roots)
        {
            var all = new List<GameObject>();
            foreach (var root in roots)
                if (root != null)
                    foreach (var t in root.GetComponentsInChildren<Transform>(true)) all.Add(t.gameObject);
            return all.ToArray();
        }

        #endregion
    }
}
