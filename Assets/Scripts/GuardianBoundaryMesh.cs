using UnityEngine;
using System.Linq;
using System.Collections.Generic;

// Draws the guardian bounds. 
public class GuardianBoundaryMesh : MonoBehaviour
{
    public int m_planeVerticesCount = 4000;
    public float m_elevationGradientDistance = 1f;
    public float m_floorElevation = 0.4f;

    // mesh filter
    MeshFilter m_meshFilter;
    // plane mesh
    Mesh m_baseMesh;
    // material (for height)
    Material m_material;
    // maps between vertices and their connections through triangles
    Dictionary<int, HashSet<int>> m_meshIndex = new Dictionary<int, HashSet<int>>();
    // 3D guardian boundary
    Vector3[] m_boundary;
    // 2D guardian boundary as a polygon
    Vector2[] m_poly;
    // all vertices that were taken care of in a given calibration
    HashSet<int> m_fetchedVertices = new HashSet<int>();
    // outer polygon, unordered
    HashSet<int> m_outerBorder;

    void Awake()
    {
        m_meshFilter = GetComponent<MeshFilter>();
        m_baseMesh = new Mesh();
        m_meshFilter.mesh = m_baseMesh;
        m_material = GetComponent<MeshRenderer>().material;
    
#if !UNITY_EDITOR
        bool configured = OVRManager.boundary.GetConfigured();
        if (!configured)
        {
            Debug.LogWarning("boundary not configured");
            // TODO show error here / someplace else?
            return;
        }

        // Note that these points are returned in (the newly reoriented) tracking space.
        m_boundary = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.OuterBoundary);
#else
        m_boundary = GameObject.Find("/GuardianDebug/Loader").GetComponent<GuardianJsonLoader>().OuterBoundaryPositions.ToArray();
#endif
        BuildBasePlane();
        m_material.SetFloat("_HeightCutoff", m_floorElevation - transform.position.y);

        var mesh = m_baseMesh;
        var verts = new Vector3[mesh.vertices.Length];
        m_fetchedVertices = new HashSet<int>();
        m_outerBorder = new HashSet<int>();

        // create a 2d polygon (based on x,z)
        m_poly = m_boundary.AsVector2().ToArray();

        // heighten vertices for everything within and near the polygon
        HeightenVerticesInPolygon(mesh.vertices, ref verts);


        // expand gradually
        var i = 0;
        var lastBorder = new HashSet<int>(m_fetchedVertices);
        do
        {
            
            lastBorder = ExpandPolygonFalloff(lastBorder, mesh.vertices, ref verts);
        }
        // stop when there are no more border points to expand
        while (lastBorder.Count > 0);

        m_baseMesh.vertices = verts;
        m_baseMesh.Optimize();
        m_baseMesh.RecalculateNormals();
        m_baseMesh.RecalculateBounds();
    }

    void BuildBasePlane()
    {        
        // find mins and maxes
        var points = m_boundary.AsVector2();
        var minX = points.Select(pt => pt.x).Min() - m_elevationGradientDistance;
        var maxX = points.Select(pt => pt.x).Max() + m_elevationGradientDistance;
        var minY = points.Select(pt => pt.y).Min() - m_elevationGradientDistance;
        var maxY = points.Select(pt => pt.y).Max() + m_elevationGradientDistance;
        var sizeX = Mathf.Abs(maxX - minX);
        var sizeY = Mathf.Abs(maxY - minY);
        var ratioXY = sizeX / sizeY;

        // determine vertices per side
        // FIXME consts
        if (m_planeVerticesCount < 100)
        {
            throw new System.Exception("minimum plane vertices count = 100");
        }
        
        var verticesPerSide = Mathf.RoundToInt(Mathf.Sqrt(m_planeVerticesCount)) * 2;
        var verticesX = Mathf.Max(5, Mathf.RoundToInt(verticesPerSide * (ratioXY / (ratioXY + 1))));
        var verticesY = verticesPerSide - verticesX;

        // calculate distance between vertices
        var distanceX = sizeX / verticesX;
        var distanceY = sizeY / verticesY;

        // calculate triangle count
        var triangleCount = (verticesX - 1) * (verticesY - 1) * 2;

        // build vertices and triangles
        var verts = new Vector2[verticesX * verticesY];
        var uvs = new Vector2[verts.Length];
        var triangles = new List<int>();

        for (var i = 0; i < verticesY; ++i)
        {
            for (var j = 0; j < verticesX; ++j)
            {
                verts[i * verticesX + j] = new Vector2(minX + j * distanceX, minY + i * distanceY);
                uvs[i * verticesX + j] = new Vector2((float)j / verticesX, (float)i / verticesY);
                if (i == verticesY - 1)
                {
                    continue;
                }

                if (j != verticesX - 1)
                {
                    triangles.Add((i + 1) * verticesX + j);
                    triangles.Add(i * verticesX + j + 1);
                    triangles.Add(i * verticesX + j);
                }
                if (j != 0)
                {
                    triangles.Add((i + 1) * verticesX + (j - 1));
                    triangles.Add((i + 1) * verticesX + j);
                    triangles.Add(i * verticesX + j);
                }
            }
        }

        m_baseMesh.vertices = verts
            .AsVector3(transform.position.y)
            .Select(pt => transform.InverseTransformPoint(pt))
            .ToArray();
        m_baseMesh.triangles = triangles.ToArray();
        m_baseMesh.uv = uvs;

        // FIXME can be optimized - we can build the map while we build the mesh
        BuildMeshIndex();
    }

    static float FalloffFunction(float step)
    {
        // smooth: 3*(distance^2) - 2*(distance^3)
        return 3 * Mathf.Pow(step, 2) - 2 * Mathf.Pow(step, 3);
    }
    static float DistanceFromPolygon(Vector2 v, Vector2[] p)
    {
        // return minimal distance
        return p.Min(pt => Vector2.Distance(pt, v));
    }

    // from https://codereview.stackexchange.com/questions/108857/point-inside-polygon-check
    static bool IsPointInPolygon(Vector2 v, Vector2[] p)
    {
        int j = p.Length - 1;
        bool c = false;
        for (int i = 0; i < p.Length; j = i++)
        {
            c ^= p[i].y > v.y ^ p[j].y > v.y && v.x < (p[j].x - p[i].x) * (v.y - p[i].y) / (p[j].y - p[i].y) + p[i].x;
        }
        return c;
    }
    
    void BuildMeshIndex()
    {
        // create a dictionary that maps all the vertices to their connections (triangles)
        var triangles = m_baseMesh.triangles;
        for (var i = 0; i < triangles.Length - 2; i += 3)
        {
            foreach (var perm in GetPermutations(new int[] {
                triangles[i],
                triangles[i + 1],
                triangles[i + 2]
            }, 2))
            {
                AddToMeshIndex(perm.First(), perm.Last());
            }
        }
    }
    void AddToMeshIndex(int from, int to)
    {
        if (!m_meshIndex.ContainsKey(from))
            m_meshIndex.Add(from, new HashSet<int>());

        m_meshIndex[from].Add(to);
    }

    // from https://stackoverflow.com/questions/756055/listing-all-permutations-of-a-string-integer
    static IEnumerable<IEnumerable<T>>
    GetPermutations<T>(IEnumerable<T> list, int length)
    {
        if (length == 1) return list.Select(t => new T[] { t });

        return GetPermutations(list, length - 1)
            .SelectMany(t => list.Where(e => !t.Contains(e)),
                (t1, t2) => t1.Concat(new T[] { t2 }));
    }

    void HeightenVerticesInPolygon(Vector3[] original, ref Vector3[] output)
    {
        for (var i = 0; i < original.Length; ++i)
        {
            var pt = transform.TransformPoint(original[i]);
            var y = transform.position.y - m_floorElevation;
            if (IsPointInPolygon(new Vector2(pt.x, pt.z), m_poly))
            {
                // lift to ground level (assume mesh is below ground level)
                y = transform.position.y;
                // mark as done
                m_fetchedVertices.Add(i);
            }
            output[i] = transform.InverseTransformPoint(new Vector3(pt.x, y, pt.z));
        }
    }

    HashSet<int> ExpandPolygonFalloff(HashSet<int> lastBorder, Vector3[] original, ref Vector3[] verts)
    {
        // find vertices that are 1 connection away from the previous vertices,
        //. this will basically find the contour vertices of the found ones
        var border = new HashSet<int>();
        foreach (var i in lastBorder)
        {
            if (!m_meshIndex.ContainsKey(i))
                continue;

            foreach (var vertex in m_meshIndex[i])
            {
                // don't connect inwards
                if (m_fetchedVertices.Contains(vertex))
                    continue;
                border.Add(vertex);
            }
        }
        lastBorder = new HashSet<int>(border);

        // expand the vertices while applying falloff
        foreach (var i in border)
        {
            var pt = transform.TransformPoint(original[i]);
            // calculate distance from polygon
            var dist = DistanceFromPolygon(new Vector2(pt.x, pt.z), m_poly);
            // calculate ratio to elevation gradient parameter and cap the result
            var distRatio = Mathf.Min(1, dist / m_elevationGradientDistance);
            // apply the prefered falloff function
            var y = transform.position.y + FalloffFunction(distRatio) * -m_floorElevation;
            verts[i] = transform.InverseTransformPoint(new Vector3(pt.x, y, pt.z));

            // mark as done
            m_fetchedVertices.Add(i);
            // if the point is already too far off, don't find its contour on the next round
            if (dist > m_elevationGradientDistance)
            {
                lastBorder.Remove(i);
                m_outerBorder.Add(i);
            }
        }
        return lastBorder;
    }
}
public static class Vector2Extension
{
    public static IEnumerable<Vector2> AsVector2(this IEnumerable<Vector3> e)
    {
        return e.Select(pt => new Vector2(pt.x, pt.z));
    }
    public static IEnumerable<Vector3> AsVector3(this IEnumerable<Vector2> e, float y = 0)
    {
        return e.Select(pt => new Vector3(pt.x, y, pt.y));
    }
}