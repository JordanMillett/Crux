using System.Globalization;

namespace CruxEngine.Utilities.Helpers;

public static class VectorHelper
{        
    public static float[] Flatten(Vector3 input)
    {
        return new float[] { input.X, input.Y, input.Z };
    }

    public static Vector3 LineToVector3(string line)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4)
        {
            float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
            float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
            return new Vector3(x, y, z);
        }

        return Vector3.Zero;
    }
    
    public static Vector2 LineToVector2(string line)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
            return new Vector2(x, y);
        }

        return Vector2.Zero;
    }

    public static Vector3 Vector3Min(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Min(a.Z, b.Z)
        );
    }

    public static Vector3 Vector3Max(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Math.Max(a.X, b.X),
            Math.Max(a.Y, b.Y),
            Math.Max(a.Z, b.Z)
        );
    }

    public static bool IsVectorNaN(Vector3 vector)
    {
        return float.IsNaN(vector.X) || float.IsNaN(vector.Y) || float.IsNaN(vector.Z);
    }

    public static float LengthSquared(Vector3 vector)
    {
        return vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;
    }

    //PHYSICS SYSTEM

    public static bool IsPointInsideShape(Vector2 point, List<Vector2> shape)
    {
        if(shape.Count < 3)
            return false;

        for (int i = 0; i < shape.Count; i++)
        {
            int next = (i + 1) % shape.Count;
            Vector2 a = shape[i];
            Vector2 b = shape[next];

            float crossProduct = (b.X - a.X) * (point.Y - a.Y) - (b.Y - a.Y) * (point.X - a.X);

            if (crossProduct < -1e-6f) 
                return false;
        }
        return true;
    }

    public static float GetAngle(Vector2 centroid, Vector2 point)
    {
        return (float) Math.Atan2(point.Y - centroid.Y, point.X - centroid.X);
    }

    public static void PolarSort(ref List<Vector2> points)
    {
        // Compute the centroid of the polygon (average of all points)
        Vector2 centroid = new Vector2(0, 0);
        foreach (var point in points)
        {
            centroid += point;
        }
        centroid /= points.Count;

        // Sort the points based on their angle to the centroid
        points.Sort((p1, p2) => GetAngle(centroid, p1).CompareTo(GetAngle(centroid, p2)));
    }

    public static bool IsInside(Vector2 p, Vector2 a, Vector2 b)
    {
        return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X) >= 0;
    }

    public static Vector3 Interpolate3D(Vector3 first3D, Vector3 second3D, Vector2 first2D, Vector2 second2D, Vector2 intersection2D)
    {
        // Compute the interpolation factor t based on 2D distances
        float totalDistance = Vector2.Distance(first2D, second2D);
        float intersectionDistance = Vector2.Distance(first2D, intersection2D);
        
        // Avoid division by zero in case of precision issues
        float t = (totalDistance > 1e-6f) ? intersectionDistance / totalDistance : 0.5f;

        // Linearly interpolate the 3D position
        return first3D + t * (second3D - first3D);
    }

    public static Vector2 ProjectPointTo2D(Vector3 point, Vector3 axis)
    {
        if (axis.LengthSquared < 1e-8f)
        return Vector2.Zero;

        axis = Vector3.Normalize(axis);

        // Pick a safe perpendicular vector
        Vector3 u;
        if (MathF.Abs(axis.Y) < 0.99f)
            u = Vector3.Cross(axis, Vector3.UnitY);
        else
            u = Vector3.Cross(axis, Vector3.UnitX);

        if (u.LengthSquared < 1e-8f)
            return Vector2.Zero;

        u = Vector3.Normalize(u);

        Vector3 v = Vector3.Cross(axis, u);

        float x = Vector3.Dot(point, u);
        float y = Vector3.Dot(point, v);

        return new Vector2(x, y);
    }

    public static Vector2 ComputeLineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        Vector2 lineDirA = a2 - a1;
        Vector2 lineDirB = b2 - b1;
        Vector2 r = a1 - b1;
        float denom = lineDirA.X * lineDirB.Y - lineDirA.Y * lineDirB.X;

        if (Math.Abs(denom) < 1e-6f)
            return (a1 + b1) / 2.0f;

        float t = (r.X * lineDirB.Y - r.Y * lineDirB.X) / denom;
        return a1 + t * lineDirA;
    }

    public static Vector3 GetPolyhedronMidpoint(List<Vector3> polygon)
    {
        if (polygon == null || polygon.Count == 0)
            return Vector3.Zero;

        Vector3 midpoint = Vector3.Zero;
        foreach (Vector3 point in polygon)
            midpoint += point;

        return midpoint / polygon.Count;
    }

    public static bool OverlapOnAxis(ColliderComponent a, ColliderComponent b, Vector3 axis, out float penetration)
    {
        (float minA, float maxA) = ProjectOntoAxis(a, axis);
        (float minB, float maxB) = ProjectOntoAxis(b, axis);

        if (minA > maxB || minB > maxA)
        {
            penetration = 0;
            return false; // Separating axis found
        }

        penetration = MathF.Min(maxA, maxB) - MathF.Max(minA, minB);
        return true;
    }

    public static (float, float) ProjectOntoAxis(ColliderComponent col, Vector3 axis)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (Vector3 vertex in col.WorldPoints)
        {
            float projection = Vector3.Dot(vertex, axis);
            min = MathF.Min(min, projection);
            max = MathF.Max(max, projection);
        }

        return (min, max);
    }

    public static bool IsVertexInsideShape(List<Vector3> shape, Vector3 axis, Vector3 point)
    {
        if (shape.Count < 3)
            return false;

        List<Vector2> flattened = new List<Vector2>();
        foreach (Vector3 vertex in shape)
        {
            Vector2 projected = ProjectPointTo2D(vertex, axis);
            flattened.Add(projected);
        }
        PolarSort(ref flattened);

        Vector2 flatPoint = ProjectPointTo2D(point, axis);

        return IsPointInsideShape(flatPoint, flattened);
    }

    public static Vector3 ClosestPointOnSegment(Vector3 P, Vector3 A, Vector3 B)
    {
        Vector3 AB = B - A;
        float t = Vector3.Dot(P - A, AB) / Vector3.Dot(AB, AB);
        t = Math.Clamp(t, 0.0f, 1.0f); // Clamp between segment endpoints
        return A + t * AB;
    }

    public static Vector3 ComputeEdgeIntersection(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        Vector3 lineDirA = Vector3.Normalize(a2 - a1);
        Vector3 lineDirB = Vector3.Normalize(b2 - b1);
        Vector3 r = a1 - b1;
        float aDot = Vector3.Dot(lineDirA, lineDirA);
        float bDot = Vector3.Dot(lineDirA, lineDirB);
        float cDot = Vector3.Dot(lineDirB, lineDirB);
        float dDot = Vector3.Dot(lineDirA, r);
        float eDot = Vector3.Dot(lineDirB, r);
        float denom = aDot * cDot - bDot * bDot;

        if (Math.Abs(denom) < 1e-6f)
            return (a1 + b1) / 2.0f;

        float s = (bDot * eDot - cDot * dDot) / denom;
        float t = (aDot * eDot - bDot * dDot) / denom;
        Vector3 closestA = a1 + s * lineDirA;
        Vector3 closestB = b1 + t * lineDirB;
        return (closestA + closestB) / 2.0f;
    }
    
    public static Vector3 ComputeEdgeContactPoint(
        Vector3 a1,
        Vector3 a2,
        Vector3 b1,
        Vector3 b2)
    {
        Vector3 dA = a2 - a1;
        Vector3 dB = b2 - b1;
        Vector3 r = a1 - b1;

        float a = Vector3.Dot(dA, dA);
        float e = Vector3.Dot(dB, dB);
        float f = Vector3.Dot(dB, r);

        float s;
        float t;

        const float EPSILON = 1e-6f;

        if (a <= EPSILON && e <= EPSILON)
        {
            return (a1 + b1) * 0.5f;
        }

        if (a <= EPSILON)
        {
            s = 0f;
            t = Math.Clamp(f / e, 0f, 1f);
        }
        else
        {
            float c = Vector3.Dot(dA, r);

            if (e <= EPSILON)
            {
                t = 0f;
                s = Math.Clamp(-c / a, 0f, 1f);
            }
            else
            {
                float b = Vector3.Dot(dA, dB);
                float denom = a * e - b * b;

                if (MathF.Abs(denom) > EPSILON)
                {
                    s = Math.Clamp(
                        (b * f - c * e) / denom,
                        0f,
                        1f);
                }
                else
                {
                    s = 0f;
                }

                t = (b * s + f) / e;

                if (t < 0f)
                {
                    t = 0f;
                    s = Math.Clamp(-c / a, 0f, 1f);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = Math.Clamp((b - c) / a, 0f, 1f);
                }
            }
        }

        Vector3 closestA = a1 + dA * s;
        Vector3 closestB = b1 + dB * t;

        return (closestA + closestB) * 0.5f;
    }
}

