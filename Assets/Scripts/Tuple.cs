using System;
using UnityEngine;


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

    public static bool operator ==(Tuple<T> a, Tuple<T> b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(Tuple<T> a, Tuple<T> b)
    {
        return !a.Equals(b);
    }
}

public struct TupleVector3 : IEquatable<TupleVector3>
{
    public Vector3 A;
    public Vector3 B;

    public TupleVector3(Vector3 a, Vector3 b)
    {
        A = a;
        B = b;
    }

    public override string ToString()
    {
        return "(" + A.x.ToString() + "," + A.y.ToString() + "," + A.z.ToString() + "), (" + B.x.ToString() + "," + B.y.ToString() + "," + B.z.ToString() + ")";
    }

    public override bool Equals(object obj)
    {
        if (obj is TupleVector3)
        {
            var b = (TupleVector3)obj;
            return Equals(b);
        }
        else
        {
            return base.Equals(obj);
        }
    }

    public bool Equals(TupleVector3 other)
    {
        return ((A - other.A).sqrMagnitude < 10e-6 && (B - other.B).sqrMagnitude < 10e-6) || ((A - other.B).sqrMagnitude < 10e-6 && (B - other.A).sqrMagnitude < 10e-6);
    }

    public override int GetHashCode()
    {
        return A.GetHashCode() + B.GetHashCode();
    }

    public static bool operator ==(TupleVector3 a, TupleVector3 b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(TupleVector3 a, TupleVector3 b)
    {
        return !a.Equals(b);
    }
}