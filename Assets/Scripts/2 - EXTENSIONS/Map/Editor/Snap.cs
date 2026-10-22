using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Quark
{
    // Placement geometry: piece bounds and where a footprint lands under the mouse.
    static class Snap
    {
        #region FIELDS

        public struct Spot
        {
            public bool valid;
            public Vector3 min;
            public Vector3 point;
            public Vector3 normal;
        }

        public struct Hit
        {
            public bool valid;
            public bool piece;
            public Vector3 point;
            public Vector3 normal;
            public Bounds box;
        }

        static readonly Dictionary<GameObject, Bounds> local = new();

        #endregion

        #region API

        public static Vector3 Grid => EditorSnapSettings.gridSize;

        public static void Clear() => local.Clear();

        // Bounds of a prefab asset around its pivot, after turning and scaling it.
        public static Bounds Of(GameObject prefab, Quaternion turn, float scale = 1f)
        {
            if (!local.TryGetValue(prefab, out var raw)) local[prefab] = raw = Measure(prefab);
            var rotation = turn * prefab.transform.localRotation;
            return Turn(raw, Matrix4x4.TRS(Vector3.zero, rotation, prefab.transform.localScale * scale));
        }

        // World bounds of a scene object and its children.
        public static Bounds World(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        public static GameObject Root(GameObject go)
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            return root != null ? root : go;
        }

        // What is under the mouse: the real surface of an object, the face of a placed piece, or the ground plane.
        // Picking renders, so this only runs on mouse events.
        public static Hit Cast(Vector2 mouse, GameObject[] ignore)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mouse);
            var hit = new Hit();
            var picked = HandleUtility.PickGameObject(mouse, false, ignore);
            if (picked != null)
            {
                var root = Root(picked);
                hit.piece = InMap(root.transform);
                hit.box = World(root);
                hit.valid = Surface(root, ray, out hit.point, out hit.normal) || (hit.piece && Face(hit.box, ray, out hit.point, out hit.normal));
                if (hit.valid) return hit;
                hit.piece = false;
            }

            if (Mathf.Abs(ray.direction.y) < 1e-4f) return hit;
            var t = -ray.origin.y / ray.direction.y;
            if (t <= 0f) return hit;
            hit.point = ray.GetPoint(t);
            hit.normal = Vector3.up;
            hit.valid = true;
            return hit;
        }

        // Where a footprint of the given size lands on a hit, centered on it: flush against the side of a
        // placed piece, on the surface of anything else.
        public static Spot Fit(Hit hit, Vector3 size, bool grid)
        {
            var spot = new Spot { valid = hit.valid, point = hit.point, normal = hit.normal };
            if (!hit.valid) return spot;
            var min = Free(hit.point, size, grid);
            var n = hit.normal;
            var box = hit.box;
            if (!hit.piece || n.y > 0.5f) min.y = hit.point.y;
            else if (n.y < -0.5f) min.y = box.min.y - size.y;
            else
            {
                min.y = box.min.y;
                if (Mathf.Abs(n.x) >= Mathf.Abs(n.z)) min.x = n.x > 0f ? box.max.x : box.min.x - size.x;
                else min.z = n.z > 0f ? box.max.z : box.min.z - size.z;
            }
            spot.min = min;
            return spot;
        }

        public static Vector3 Centered(Vector3 size) => new(-size.x * 0.5f, 0f, -size.z * 0.5f);

        // Pieces live under the Map root of the open scene.
        public static bool InMap(Transform t)
        {
            while (t.parent != null) t = t.parent;
            return t.name == "Map" && t.gameObject.scene == SceneManager.GetActiveScene();
        }

        #endregion

        #region MISC

        static Vector3 Free(Vector3 point, Vector3 size, bool grid)
        {
            var min = point + Centered(size);
            if (!grid) return min;
            var g = Grid;
            min.x = Round(min.x, g.x);
            min.z = Round(min.z, g.z);
            return min;
        }

        static float Round(float v, float step) => step > 1e-4f ? Mathf.Round(v / step) * step : v;

        static bool Surface(GameObject root, Ray ray, out Vector3 point, out Vector3 normal)
        {
            point = normal = default;
            Physics.SyncTransforms();
            var best = float.MaxValue;
            foreach (var c in root.GetComponentsInChildren<Collider>())
            {
                if (!c.enabled || !c.Raycast(ray, out var hit, best)) continue;
                best = hit.distance;
                point = hit.point;
                normal = hit.normal;
            }
            return best < float.MaxValue;
        }

        static bool Face(Bounds box, Ray ray, out Vector3 point, out Vector3 normal)
        {
            point = normal = default;
            var enter = float.MinValue;
            var exit = float.MaxValue;
            var axis = 0;
            for (var i = 0; i < 3; i++)
            {
                var d = ray.direction[i];
                if (Mathf.Abs(d) < 1e-6f)
                {
                    if (ray.origin[i] < box.min[i] || ray.origin[i] > box.max[i]) return false;
                    continue;
                }
                var a = (box.min[i] - ray.origin[i]) / d;
                var b = (box.max[i] - ray.origin[i]) / d;
                if (a > b) (a, b) = (b, a);
                if (a > enter) { enter = a; axis = i; }
                exit = Mathf.Min(exit, b);
            }
            if (enter > exit || exit < 0f) return false;
            point = ray.GetPoint(Mathf.Max(enter, 0f));
            normal[axis] = ray.direction[axis] > 0f ? -1f : 1f;
            return true;
        }

        static Bounds Measure(GameObject prefab)
        {
            var toRoot = prefab.transform.worldToLocalMatrix;
            var found = false;
            var b = new Bounds();
            foreach (var r in prefab.GetComponentsInChildren<Renderer>())
            {
                Mesh mesh = null;
                if (r is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
                else if (r.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
                if (mesh == null) continue;
                var part = Turn(mesh.bounds, toRoot * r.transform.localToWorldMatrix);
                if (found) b.Encapsulate(part);
                else { b = part; found = true; }
            }
            return found ? b : new Bounds(Vector3.up * 0.5f, Vector3.one);
        }

        static Bounds Turn(Bounds b, Matrix4x4 m)
        {
            var result = new Bounds(m.MultiplyPoint3x4(b.min), Vector3.zero);
            for (var i = 1; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                result.Encapsulate(m.MultiplyPoint3x4(corner));
            }
            return result;
        }

        #endregion
    }
}
