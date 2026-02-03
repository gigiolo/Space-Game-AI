using System.Collections.Generic;
using UnityEngine;

public static class IcosphereCreator
{
    private struct TriangleIndices { public int v1, v2, v3; public TriangleIndices(int v1, int v2, int v3) { this.v1 = v1; this.v2 = v2; this.v3 = v3; } }
    private static Dictionary<long, int> middlePointIndexCache;
    private static Dictionary<int, Mesh> _meshCache = new Dictionary<int, Mesh>();

    public static Mesh Create(int recursionLevel)
    {
        if (_meshCache.ContainsKey(recursionLevel) && _meshCache[recursionLevel] != null)
            return _meshCache[recursionLevel];

        middlePointIndexCache = new Dictionary<long, int>();
        Mesh mesh = new Mesh();
        mesh.name = $"Icosphere_Res{recursionLevel}";

        List<Vector3> geometry = new List<Vector3>();
        List<int> indices = new List<int>();

        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        geometry.Add(new Vector3(-1f, t, 0f).normalized);
        geometry.Add(new Vector3(1f, t, 0f).normalized);
        geometry.Add(new Vector3(-1f, -t, 0f).normalized);
        geometry.Add(new Vector3(1f, -t, 0f).normalized);
        geometry.Add(new Vector3(0f, -1f, t).normalized);
        geometry.Add(new Vector3(0f, 1f, t).normalized);
        geometry.Add(new Vector3(0f, -1f, -t).normalized);
        geometry.Add(new Vector3(0f, 1f, -t).normalized);
        geometry.Add(new Vector3(t, 0f, -1f).normalized);
        geometry.Add(new Vector3(t, 0f, 1f).normalized);
        geometry.Add(new Vector3(-t, 0f, -1f).normalized);
        geometry.Add(new Vector3(-t, 0f, 1f).normalized);

        List<TriangleIndices> faces = new List<TriangleIndices>
        {
            new(0, 11, 5), new(0, 5, 1), new(0, 1, 7), new(0, 7, 10), new(0, 10, 11),
            new(1, 5, 9), new(5, 11, 4), new(11, 10, 2), new(10, 7, 6), new(7, 1, 8),
            new(3, 9, 4), new(3, 4, 2), new(3, 2, 6), new(3, 6, 8), new(3, 8, 9),
            new(4, 9, 5), new(2, 4, 11), new(6, 2, 10), new(8, 6, 7), new(9, 8, 1)
        };

        for (int i = 0; i < recursionLevel; i++)
        {
            List<TriangleIndices> faces2 = new List<TriangleIndices>();
            foreach (var tri in faces)
            {
                int a = GetMiddlePoint(tri.v1, tri.v2, ref geometry);
                int b = GetMiddlePoint(tri.v2, tri.v3, ref geometry);
                int c = GetMiddlePoint(tri.v3, tri.v1, ref geometry);
                faces2.Add(new TriangleIndices(tri.v1, a, c));
                faces2.Add(new TriangleIndices(tri.v2, b, a));
                faces2.Add(new TriangleIndices(tri.v3, c, b));
                faces2.Add(new TriangleIndices(a, b, c));
            }
            faces = faces2;
        }

        foreach (var tri in faces) { indices.Add(tri.v1); indices.Add(tri.v2); indices.Add(tri.v3); }

        mesh.vertices = geometry.ToArray();
        mesh.triangles = indices.ToArray();

        Vector2[] uv = new Vector2[geometry.Count];
        for (int i = 0; i < geometry.Count; i++)
        {
            Vector3 v = geometry[i];
            uv[i] = new Vector2((Mathf.Atan2(v.x, v.z) / Mathf.PI + 1f) / 2f, Mathf.Asin(v.y) / Mathf.PI + 0.5f);
        }
        mesh.uv = uv;

        // --- ORDINE FONDAMENTALE ---
        mesh.RecalculateNormals();  // 1. Calcola Normali
        mesh.RecalculateTangents(); // 2. ORA calcola le Tangenti usando le Normali e le UV
        mesh.RecalculateBounds();
        
        _meshCache[recursionLevel] = mesh;
        return mesh;
    }

    private static int GetMiddlePoint(int p1, int p2, ref List<Vector3> vertices)
    {
        long smallerIndex = p1 < p2 ? p1 : p2;
        long greaterIndex = p1 < p2 ? p2 : p1;
        long key = (smallerIndex << 32) + greaterIndex;

        if (middlePointIndexCache.TryGetValue(key, out int ret)) return ret;

        Vector3 point1 = vertices[p1];
        Vector3 point2 = vertices[p2];
        Vector3 middle = new Vector3((point1.x + point2.x) / 2f, (point1.y + point2.y) / 2f, (point1.z + point2.z) / 2f).normalized;

        int i = vertices.Count;
        vertices.Add(middle);
        middlePointIndexCache.Add(key, i);
        return i;
    }
}