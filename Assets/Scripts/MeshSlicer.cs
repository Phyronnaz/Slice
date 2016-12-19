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

        var middle = new List<Vector3>();

        vertices1.AddRange(vertices);
        vertices2.AddRange(vertices);

        var PPos = Vector3.Dot(point, normal);

        Func<Vector3, bool> Side = (Vector3 v) => Vector3.Dot(v, normal) > PPos;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            var aIndex = triangles[i];
            var bIndex = triangles[i + 1];
            var cIndex = triangles[i + 2];

            var a = vertices[aIndex];
            var b = vertices[bIndex];
            var c = vertices[cIndex];

            var aSide = Side(a);
            var bSide = Side(b);
            var cSide = Side(c);

            if (aSide && bSide && cSide) //All on side 1
            {
                triangles1.Add(aIndex);
                triangles1.Add(bIndex);
                triangles1.Add(cIndex);
            }
            else if (!aSide && !bSide && !cSide) //All on side 2 
            {
                triangles2.Add(aIndex);
                triangles2.Add(bIndex);
                triangles2.Add(cIndex);
            }
            else
            {
                if (aSide && bSide || !aSide && !bSide) //A && B same side
                {
                    Process(aIndex, bIndex, cIndex, normal, point, Side, vertices, vertices1, vertices2, triangles1, triangles2, middle);
                }
                else if (bSide && cSide || !bSide && !cSide) //B && C same side
                {
                    Process(bIndex, cIndex, aIndex, normal, point, Side, vertices, vertices1, vertices2, triangles1, triangles2, middle);
                }
                else if (aSide && cSide || !aSide && !cSide) //C && A same side
                {
                    Process(cIndex, aIndex, bIndex, normal, point, Side, vertices, vertices1, vertices2, triangles1, triangles2, middle);
                }
            }
        }

        /*
         * Middle
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
        var Y = Vector3.Cross(normal, X);

        var vertices2D = new Vector2[middle.Count];
        for (int i = 0; i < middle.Count; i++)
        {
            vertices2D[i] = new Vector2(Vector3.Dot(middle[i] - point, X), Vector3.Dot(middle[i] - point, Y));
        }
        var polygon = new Polygon(vertices2D);
        var trigs = polygon.Triangulate();

        Func<Vector2, Vector3> extract = (Vector2 v) => v.x * X + v.y * Y;

        for (int i = 0; i < trigs.Count; i++)
        {
            triangles1.Add(vertices1.Count);
            vertices1.Add(extract(trigs[i].Points[0]));
            triangles1.Add(vertices1.Count);
            vertices1.Add(extract(trigs[i].Points[1]));
            triangles1.Add(vertices1.Count);
            vertices1.Add(extract(trigs[i].Points[2]));

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
    private static void Process(int aIndex, int bIndex, int cIndex, Vector3 normal, Vector3 point, Func<Vector3, bool> Side, Vector3[] vertices,
        List<Vector3> vertices1, List<Vector3> vertices2, List<int> triangles1, List<int> triangles2, List<Vector3> middle)
    {
        if (!(Side(vertices[aIndex]) && Side(vertices[bIndex]) || !Side(vertices[aIndex]) && !Side(vertices[bIndex])))
        {
            Debug.LogError("A && B not the same side!");
        }

        var aSide = Side(vertices[aIndex]);

        var A = vertices[aIndex];
        var B = vertices[bIndex];
        var C = vertices[cIndex];

        var APos = Vector3.Dot(A, normal);
        var BPos = Vector3.Dot(B, normal);
        var CPos = Vector3.Dot(C, normal);
        var PPos = Vector3.Dot(point, normal);

        var D = (A + B) / 2;
        var E = B + (C - B) * (PPos - BPos) / (CPos - BPos);
        var F = C + (A - C) * (PPos - CPos) / (APos - CPos);

        Debug.Log("A :" + A.ToString() + ", B: " + B.ToString() + ", C:" + C.ToString() + ", D:" + D.ToString() + ", E:" + E.ToString() + ", F:" + B.ToString());

        var trig1 = aSide ? triangles1 : triangles2; //A && B trigs
        var trig2 = !aSide ? triangles1 : triangles2; //C trigs
        var vert1 = aSide ? vertices1 : vertices2; //A && B verts
        var vert2 = !aSide ? vertices1 : vertices2; //C verts

        //D
        var dIndex1 = vert1.Count;
        vert1.Add(D);
        //E
        var eIndex1 = vert1.Count;
        var eIndex2 = vert2.Count;
        vert1.Add(E);
        vert2.Add(E);
        //F
        var fIndex1 = vert1.Count;
        var fIndex2 = vert2.Count;
        vert1.Add(F);
        vert2.Add(F);

        /*
         * Side 1
         */
        //ADF
        trig1.Add(aIndex);
        trig1.Add(dIndex1);
        trig1.Add(fIndex1);
        //DEF
        trig1.Add(dIndex1);
        trig1.Add(eIndex1);
        trig1.Add(fIndex1);
        //DBE
        trig1.Add(dIndex1);
        trig1.Add(bIndex);
        trig1.Add(eIndex1);
        /*
         * Side 2
         */
        //FEC
        trig2.Add(fIndex2);
        trig2.Add(eIndex2);
        trig2.Add(cIndex);

        middle.Add(F);
        middle.Add(E);
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