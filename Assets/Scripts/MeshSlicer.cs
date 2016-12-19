using UnityEngine;
using System;
using System.Collections.Generic;

public static class MeshSlicer
{
    public static SlicedMesh Slice(Mesh mesh, Vector3 normal, Vector3 point)
    {
        normal.Normalize();
        var triangles = mesh.triangles;
        var triangles1 = new List<int>(triangles.Length);
        var triangles2 = new List<int>(triangles.Length);

        var vertices = mesh.vertices;
        var vertices1 = new List<Vector3>(vertices.Length);
        var vertices2 = new List<Vector3>(vertices.Length);


        var middleIndices = new List<Tuple<int>>();

        vertices1.AddRange(vertices);
        vertices2.AddRange(vertices);

        Debug.Log(vertices.Length);
        Debug.Log(string.Join("::", vertices1.ConvertAll((Vector3 i) => i.ToString()).ToArray()));

        var PPos = Vector3.Dot(point, normal);

        Func<Vector3, bool> Side = (Vector3 v) => Vector3.Dot(v, normal) > PPos;

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
                triangles1.Add(a);
                triangles1.Add(b);
                triangles1.Add(c);
            }
            else if (!aSide && !bSide && !cSide) //All on side 2 
            {
                triangles2.Add(a);
                triangles2.Add(b);
                triangles2.Add(c);
            }
            else
            {
                if (aSide && bSide || !aSide && !bSide) //A && B same side
                {
                    Process(a, b, c, normal, point, Side, vertices, vertices1, vertices2, triangles1, triangles2, middleIndices);
                }
                else if (bSide && cSide || !bSide && !cSide) //B && C same side
                {
                    Process(b, c, a, normal, point, Side, vertices, vertices1, vertices2, triangles1, triangles2, middleIndices);
                }
                else if (aSide && cSide || !aSide && !cSide) //C && A same side
                {
                    Process(c, a, b, normal, point, Side, vertices, vertices1, vertices2, triangles1, triangles2, middleIndices);
                }
            }
        }

        /*
         * Middle
         */

        //Sort middle list
        var middleVertices = new List<Tuple<Vector3>>(middleIndices.Count);
        var tmp = middleIndices.ConvertAll((Tuple<int> t) => new Tuple<Vector3>(vertices[t.A], vertices[t.B]));
        middleVertices.Add(tmp[0]);
        middleVertices.Add(tmp[1]);
        tmp.RemoveAt(1);
        tmp.RemoveAt(0);

        int x = tmp.IndexOf(middleVertices[0]);
        while (x != -1)
        {
            var y = x % 2 == 0 ? x + 1 : x - 1;
            middleVertices.Insert(0, tmp[x]);
            middleVertices.Insert(0, tmp[y]);
            tmp.RemoveAt(Mathf.Max(x, y));
            tmp.RemoveAt(Mathf.Min(x, y));

            x = tmp.IndexOf(middleVertices[0]);
        }


        for (int i = 0; i < middleVertices.Count; i++)
        {
            var go = new GameObject();
            var txt = go.AddComponent<TextMesh>();
            txt.text = i.ToString();
            txt.fontSize = 100;
            txt.characterSize = 0.01f;
            var A = middleVertices[i].A;
            var B = middleVertices[i].B;
            var APos = Vector3.Dot(A, normal);
            var BPos = Vector3.Dot(B, normal);
            var C = A + (B - A) * (PPos - APos) / (BPos - APos);
            go.transform.position = C;
        }

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

        var vertices2D = new Vector2[middleVertices.Count];
        for (int i = 0; i < middleVertices.Count; i++)
        {
            var A = middleVertices[i].A;
            var B = middleVertices[i].B;
            var APos = Vector3.Dot(A, normal);
            var BPos = Vector3.Dot(B, normal);
            var C = A + (B - A) * (PPos - APos) / (BPos - APos);
            vertices2D[i] = new Vector2(Vector3.Dot(C - point, X), Vector3.Dot(C - point, Y));
        }
        Debug.Log(string.Join("::", new List<Vector2>(vertices2D).ConvertAll((Vector2 i) => i.ToString()).ToArray()));
        var polygon = new Polygon(vertices2D);
        var trigs = polygon.Triangulate();
        Debug.Log(string.Join("::", trigs.ConvertAll(
            (Triangle i) => string.Join(",", new List<Vector2>(i.Points).ConvertAll((Vector2 v) => v.ToString()).ToArray())
            ).ToArray()));

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

        /*
         * Create sliced mesh
         */
        var mesh1 = new Mesh();
        var mesh2 = new Mesh();
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
        List<Vector3> vertices1, List<Vector3> vertices2, List<int> triangles1, List<int> triangles2, List<Tuple<int>> middle)
    {
        if (!(Side(vertices[a]) && Side(vertices[b]) || !Side(vertices[a]) && !Side(vertices[b])))
        {
            Debug.LogError("A && B not the same side!");
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
        trig1.Add(a);
        trig1.Add(d1);
        trig1.Add(f1);
        //DEF
        trig1.Add(d1);
        trig1.Add(e1);
        trig1.Add(f1);
        //DBE
        trig1.Add(d1);
        trig1.Add(b);
        trig1.Add(e1);
        /*
         * Side 2
         */
        //FEC
        trig2.Add(f2);
        trig2.Add(e2);
        trig2.Add(c);

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

public struct Tuple<T>
{
    public T A;
    public T B;

    public Tuple(T a, T b)
    {
        A = a;
        B = b;
    }

    public override string ToString()
    {
        return A.ToString() + ", " + B.ToString();
    }

    public override bool Equals(object obj)
    {
        if (obj is Tuple<T>)
        {
            var b = (Tuple<T>)obj;
            return (A.Equals(b.A) && B.Equals(b.B)) || (A.Equals(b.B) && B.Equals(b.A));
        }
        else
        {
            return base.Equals(obj);
        }
    }

    public override int GetHashCode()
    {
        return A.GetHashCode() + B.GetHashCode();
    }

    public bool IsNear(Tuple<T> t)
    {
        return A.Equals(t.A) || B.Equals(t.A) || A.Equals(t.B) || B.Equals(t.B);
    }

    public static bool operator ==(Tuple<T> a, Tuple<T> b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(Tuple<T> a, Tuple<T> b)
    {
        return !a.Equals(b);
    }
}