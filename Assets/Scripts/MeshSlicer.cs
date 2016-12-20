using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;

public static class MeshSlicer
{
    public static SlicedMesh Slice(Mesh mesh, Vector3 normal, Vector3 point)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        normal.Normalize();

        /*
         * Create arrays
         */
        var triangles = mesh.triangles;
        var triangles1 = new List<int>(triangles.Length);
        var triangles2 = new List<int>(triangles.Length);

        var vertices = mesh.vertices;
        var vertices1Map = new int[vertices.Length]; // 1 -> 0 because 0 default value
        var vertices2Map = new int[vertices.Length];
        var vertices1 = new List<Vector3>(vertices.Length);
        var vertices2 = new List<Vector3>(vertices.Length);

        var middleIndices = new List<Tuple<int>>();

        var PPos = Vector3.Dot(point, normal);

        Func<Vector3, bool> Side = (Vector3 v) => Vector3.Dot(v, normal) > PPos;
        Func<int, int> GetVertice1 = (int i) =>
        {
            if (vertices1Map[i] == 0)
            {
                vertices1.Add(Vector3.zero);
                vertices1Map[i] = vertices1.Count;
                return vertices1.Count - 1;
            }
            else
            {
                return vertices1Map[i] - 1;
            }
        };
        Func<int, int> GetVertice2 = (int i) =>
        {
            if (vertices2Map[i] == 0)
            {
                vertices2.Add(Vector3.zero);
                vertices2Map[i] = vertices2.Count;
                return vertices2.Count - 1;
            }
            else
            {
                return vertices2Map[i] - 1;
            }
        };

        /*
         * Create triangles
         */
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var a = triangles[i];
            var b = triangles[i + 1];
            var c = triangles[i + 2];

            var A = vertices[a];
            var B = vertices[b];
            var C = vertices[c];

            var aSide = Side(A);
            var bSide = Side(B);
            var cSide = Side(C);

            if (aSide && bSide && cSide) //All on side 1
            {
                var a1 = GetVertice1(a);
                var b1 = GetVertice1(b);
                var c1 = GetVertice1(c);
                vertices1[a1] = A;
                vertices1[b1] = B;
                vertices1[c1] = C;
                triangles1.Add(a1);
                triangles1.Add(b1);
                triangles1.Add(c1);
            }
            else if (!aSide && !bSide && !cSide) //All on side 2 
            {
                var a2 = GetVertice2(a);
                var b2 = GetVertice2(b);
                var c2 = GetVertice2(c);
                vertices2[a2] = A;
                vertices2[b2] = B;
                vertices2[c2] = C;
                triangles2.Add(a2);
                triangles2.Add(b2);
                triangles2.Add(c2);
            }
            else
            {
                if (aSide && bSide || !aSide && !bSide) //A && B same side
                {
                    Process(a, b, c, normal, point, Side, vertices, vertices1, vertices2, triangles1, triangles2, middleIndices, GetVertice1, GetVertice2);
                }
                else if (bSide && cSide || !bSide && !cSide) //B && C same side
                {
                    Process(b, c, a, normal, point, Side, vertices, vertices1, vertices2, triangles1, triangles2, middleIndices, GetVertice1, GetVertice2);
                }
                else if (aSide && cSide || !aSide && !cSide) //C && A same side
                {
                    Process(c, a, b, normal, point, Side, vertices, vertices1, vertices2, triangles1, triangles2, middleIndices, GetVertice1, GetVertice2);
                }
            }
        }

        /*
         * Sort middle list
         */


        var middleVertices = middleIndices.ConvertAll((Tuple<int> t) => new TupleVector3(vertices[t.A], vertices[t.B]));
        UnityEngine.Debug.Log(string.Join("\n", new List<TupleVector3>(middleVertices).ConvertAll((TupleVector3 i) => i.ToString()).ToArray()));
        //Process all different polygons of the middle
        while (middleVertices.Count > 0)
        {
            var middleVerticesPart = new List<TupleVector3>(middleIndices.Count);
            middleVerticesPart.Add(middleVertices[0]);
            middleVerticesPart.Add(middleVertices[1]);
            middleVertices.RemoveAt(1);
            middleVertices.RemoveAt(0);

            int x = middleVertices.IndexOf(middleVerticesPart[0]);
            bool end = false;
            while (x != -1)
            {
                var y = x % 2 == 0 ? x + 1 : x - 1;
                middleVerticesPart.Insert(0, middleVertices[y]);
                middleVertices.RemoveAt(Mathf.Max(x, y));
                middleVertices.RemoveAt(Mathf.Min(x, y));

                x = middleVertices.IndexOf(middleVerticesPart[end ? middleVerticesPart.Count - 1 : 0]);
                end = false;
                if (x == -1)
                {
                    x = middleVertices.IndexOf(middleVerticesPart[middleVerticesPart.Count - 1]);
                    end = true;
                }
            }

            for (int i = 0; i < middleVerticesPart.Count; i++)
            {
                var go = new GameObject();
                var txt = go.AddComponent<TextMesh>();
                txt.text = i.ToString();
                txt.fontSize = 100;
                txt.characterSize = 0.001f;
                var A = middleVerticesPart[i].A;
                var B = middleVerticesPart[i].B;
                var APos = Vector3.Dot(A, normal);
                var BPos = Vector3.Dot(B, normal);
                var C = A + (B - A) * (PPos - APos) / (BPos - APos);
                go.transform.position = C + Control.Position;
            }

            /*
             * Find an orthonormal base
             */
            Vector3 X;
            if (normal.x == 0) //X == 0
            {
                X = new Vector3(1, normal.y, normal.z);
            }
            else if (normal.y == 0) //Y == 0
            {
                X = new Vector3(normal.x, 1, normal.z);
            }
            else if (normal.z == 0) //Z == 0
            {
                X = new Vector3(normal.x, normal.y, 1);
            }
            else
            {
                X = new Vector3(normal.x, -normal.y, 0);
            }
            X = X - normal * Vector3.Dot(X, normal);
            var Y = Vector3.Cross(normal, X);

            X.Normalize();
            Y.Normalize();

            /*
             * Convert 3D vertices to 2D ones
             */
            var vertices2D = new Vector2[middleVerticesPart.Count];
            for (int i = 0; i < middleVerticesPart.Count; i++)
            {
                var A = middleVerticesPart[i].A;
                var B = middleVerticesPart[i].B;
                var APos = Vector3.Dot(A, normal);
                var BPos = Vector3.Dot(B, normal);
                var C = A + (B - A) * (PPos - APos) / (BPos - APos);
                vertices2D[i] = new Vector2(Vector3.Dot(C - point, X), Vector3.Dot(C - point, Y));
            }

            /*
             * Try to triangulate middle vertices
             */
            List<Triangle> trigs;
            try
            {
                var polygon = new Polygon(vertices2D);
                trigs = polygon.Triangulate();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Error during triangulation: " + e.Message);
                trigs = new List<Triangle>();
            }

            Func<Vector2, Vector3> extract = (Vector2 v) => v.x * X + v.y * Y + point;
            for (int i = 0; i < trigs.Count; i++)
            {
                triangles1.Add(vertices1.Count);
                vertices1.Add(extract(trigs[i].Points[2]));
                triangles1.Add(vertices1.Count);
                vertices1.Add(extract(trigs[i].Points[1]));
                triangles1.Add(vertices1.Count);
                vertices1.Add(extract(trigs[i].Points[0]));

                triangles2.Add(vertices2.Count);
                vertices2.Add(extract(trigs[i].Points[0]));
                triangles2.Add(vertices2.Count);
                vertices2.Add(extract(trigs[i].Points[1]));
                triangles2.Add(vertices2.Count);
                vertices2.Add(extract(trigs[i].Points[2]));
            }
        }

        /*
         * Create sliced mesh
         */
        var mesh1 = new Mesh();
        var mesh2 = new Mesh();
        UnityEngine.Debug.Log("Vertices 1: " + vertices1.Count.ToString() + "; Vertices 2: " + vertices2.Count.ToString() + "; Vertices: " + vertices.Length.ToString());

        mesh1.vertices = vertices1.ToArray();
        mesh1.triangles = triangles1.ToArray();
        mesh1.RecalculateNormals();

        mesh2.vertices = vertices2.ToArray();
        mesh2.triangles = triangles2.ToArray();
        mesh2.RecalculateNormals();
        return new SlicedMesh(mesh1, mesh2);
    }

    /*
     * A && B same side
     */
    private static void Process(int a, int b, int c, Vector3 normal, Vector3 point, Func<Vector3, bool> Side, Vector3[] vertices,
        List<Vector3> vertices1, List<Vector3> vertices2, List<int> triangles1, List<int> triangles2, List<Tuple<int>> middle,
         Func<int, int> getVertices1, Func<int, int> getVertices2)
    {
        if (!(Side(vertices[a]) && Side(vertices[b]) || !Side(vertices[a]) && !Side(vertices[b])))
        {
            UnityEngine.Debug.LogError("A && B not the same side!");
        }

        var aSide = Side(vertices[a]);

        var A = vertices[a];
        var B = vertices[b];
        var C = vertices[c];

        var APos = Vector3.Dot(A, normal);
        var BPos = Vector3.Dot(B, normal);
        var CPos = Vector3.Dot(C, normal);
        var PPos = Vector3.Dot(point, normal);

        var D = (A + B) / 2;
        var E = B + (C - B) * (PPos - BPos) / (CPos - BPos);
        var F = C + (A - C) * (PPos - CPos) / (APos - CPos);

        var trig1 = aSide ? triangles1 : triangles2; //A && B trigs
        var trig2 = !aSide ? triangles1 : triangles2; //C trigs
        var vert1 = aSide ? vertices1 : vertices2; //A && B verts
        var vert2 = !aSide ? vertices1 : vertices2; //C verts
        var getVert1 = aSide ? getVertices1 : getVertices2;
        var getVert2 = !aSide ? getVertices1 : getVertices2;

        //A
        var a1 = getVert1(a);
        vert1[a1] = A;
        //B
        var b1 = getVert1(b);
        vert1[b1] = B;
        //C
        var c2 = getVert2(c);
        vert2[c2] = C;
        //D
        var d1 = vert1.Count;
        vert1.Add(D);
        //E
        var e1 = vert1.Count;
        var e2 = vert2.Count;
        vert1.Add(E);
        vert2.Add(E);
        //F
        var f1 = vert1.Count;
        var f2 = vert2.Count;
        vert1.Add(F);
        vert2.Add(F);

        /*
         * Side 1
         */
        //ADF
        trig1.Add(a1);
        trig1.Add(d1);
        trig1.Add(f1);
        //DEF
        trig1.Add(d1);
        trig1.Add(e1);
        trig1.Add(f1);
        //DBE
        trig1.Add(d1);
        trig1.Add(b1);
        trig1.Add(e1);
        /*
         * Side 2
         */
        //FEC
        trig2.Add(f2);
        trig2.Add(e2);
        trig2.Add(c2);

        middle.Add(new Tuple<int>(b, c));
        middle.Add(new Tuple<int>(c, a));
    }
}

public struct SlicedMesh
{
    public readonly Mesh Mesh1;
    public readonly Mesh Mesh2;

    public SlicedMesh(Mesh mesh1, Mesh mesh2)
    {
        Mesh1 = mesh1;
        Mesh2 = mesh2;
    }
}
