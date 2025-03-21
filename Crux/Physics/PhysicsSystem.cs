using Crux.Utilities.Helpers;
using Crux.Components;

namespace Crux.Physics;

public static class PhysicsSystem
{
    public static int TotalColliders = 0;
    public static int TotalDynamicObjects = 0;

    public static bool IntegratingAndComputing = false;

    private static Octree Tree;

    private static List<ColliderComponent> Colliders = [];
    private static Dictionary<ColliderComponent, PhysicsComponent> DynamicObjects = [];

    private static List<ColliderComponent> PendingAddColliders = [];
    private static Dictionary<ColliderComponent, PhysicsComponent> PendingAddDynamicObjects = [];

    private static List<ColliderComponent> PendingRemoveColliders = [];
    private static Dictionary<ColliderComponent, PhysicsComponent> PendingRemoveDynamicObjects = [];

    public static Vector3 Gravity = new Vector3(0f, -9.8f, 0f);

    public static float FramesPerSecond = 0f;
    public static int PhysicsFrameCount = 0;
    public static int SphereChecks = 0;
    public static int AABBChecks = 0;
    public static int OBBChecks = 0;

    static PhysicsSystem()
    {
        Tree = new Octree(new Vector3(-500, -500, -500), new Vector3(500, 500, 500), 7, "Physics Octree");
    }

    public static string GetShortInfo()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Physics FPS - {FramesPerSecond:F2}");
        sb.AppendLine($"Colliders - {Colliders.Count}");
        sb.AppendLine($"Dynamic Objects - {DynamicObjects.Count}");
        sb.AppendLine($"Sphere Checks - {SphereChecks}");
        sb.AppendLine($"AABB Checks - {AABBChecks}");
        sb.AppendLine($"OBB Checks - {OBBChecks}");
        
        return sb.ToString();
    }

    public static void RegisterAsStatic(ColliderComponent col)
    {
        lock (PendingAddColliders)
        {
            if(!PendingAddColliders.Contains(col))
                PendingAddColliders.Add(col);
        }
    }

    public static void UnregisterAsStatic(ColliderComponent col)
    {
        lock (PendingRemoveColliders)
        {
            if(!PendingRemoveColliders.Contains(col))
                PendingRemoveColliders.Add(col);
        }
    }

    public static void RegisterAsDynamic(ColliderComponent col, PhysicsComponent phy)
    {          
        lock (PendingAddDynamicObjects)
        {
            PendingAddDynamicObjects.TryAdd(col, phy);
        }
    }

    public static void UnregisterAsDynamic(ColliderComponent col, PhysicsComponent phy)
    {          
        lock (PendingRemoveDynamicObjects)
        {
            PendingRemoveDynamicObjects.TryAdd(col, phy);
        }
    }

    public static void MergeDictionaries()
    {
        //Add static
        lock (PendingAddColliders)
        {
            foreach (ColliderComponent col in PendingAddColliders)
            {
                Colliders.Add(col);
                col.OctreeKeys = Tree.RegisterComponentGetAABB(col, col.AABBMin, col.AABBMax);
            }
            TotalColliders = Colliders.Count;
            PendingAddColliders.Clear();
        }

        //remove static
        lock (PendingRemoveColliders)
        {
            foreach (ColliderComponent col in PendingRemoveColliders)
            {
                Colliders.Remove(col);
                Tree.UnregisterComponent(col, col.OctreeKeys);
            }
            TotalColliders = Colliders.Count;
            PendingRemoveColliders.Clear();
        }

        //add dynamic
        lock (PendingAddDynamicObjects)
        {
            foreach (var pair in PendingAddDynamicObjects)
            {
                DynamicObjects.Add(pair.Key, pair.Value);
                Tree.UnregisterComponent(pair.Key, pair.Key.OctreeKeys); //remove dynamic objects from octree
            }
            TotalDynamicObjects = DynamicObjects.Count;
            PendingAddDynamicObjects.Clear();
        }

        //remove dynamic
        lock (PendingRemoveDynamicObjects)
        {
            foreach (var pair in PendingRemoveDynamicObjects)
            {
                DynamicObjects.Remove(pair.Key);
            }
            TotalDynamicObjects = DynamicObjects.Count;
            PendingRemoveDynamicObjects.Clear();
        }
    }

    public static void Update()
    {
        if(IntegratingAndComputing)
            return;
        IntegratingAndComputing = true;
        PhysicsFrameCount++;
        MergeDictionaries();

        GameEngine.Link.DebugDisplayPositions.Clear();

        foreach (PhysicsComponent Dynamic in DynamicObjects.Values) //maps colliders to physics components
            Dynamic.Integrate();

        foreach (ColliderComponent Static in Colliders) //Contains all colliders, even the ones on dynamic objects
        {
            if(!Static.GameObject.IsFrozen)
                Static.ComputeBounds();
        }
        
        SphereChecks = 0;
        List<(ColliderComponent, ColliderComponent)> SphereConflicts = new List<(ColliderComponent, ColliderComponent)>();       
        foreach (var pair in DynamicObjects)
        {
            if(!pair.Value.Awake)
                continue;

            List<ColliderComponent> nearby = Tree.FindNearbyNodes(pair.Key.AABBMin, pair.Key.AABBMax).OfType<ColliderComponent>().ToList();
            nearby.AddRange(DynamicObjects.Keys); //make sure to check against dynamics always

            foreach (ColliderComponent collider in nearby)
            {
                if (pair.Key == collider)
                    continue;

                if (CheckSphere(pair.Key, collider))
                    SphereConflicts.Add((pair.Key, collider));
                
                SphereChecks++;
            }
        }

        AABBChecks = 0;
        List<(ColliderComponent, ColliderComponent)> AABBConflicts = new List<(ColliderComponent, ColliderComponent)>();
        foreach (var (a, b) in SphereConflicts)
        {
            if (CheckAABB(a, b))
                AABBConflicts.Add((a, b));
            AABBChecks++;
        }

        OBBChecks = 0;
        List<(ColliderComponent, ColliderComponent, Vector3 resolution, Vector3 contactPoint)> OBBConflicts = new();
        foreach (var (a, b) in AABBConflicts)
        {
            if (CheckOBB(a, b, out Vector3 resolution, out Vector3 contactPoint))
                OBBConflicts.Add((a, b, resolution, contactPoint));
            OBBChecks++;
        }

        OBBConflicts = OBBConflicts.OrderByDescending(conflict => conflict.contactPoint.Y).ToList();
        foreach (var (a, b, resolution, contactPoint) in OBBConflicts)
            ResolveCollision(a, b, resolution, contactPoint);

        IntegratingAndComputing = false;
    }

    public static bool CheckSphere(ColliderComponent a, ColliderComponent b)
    {
        float distanceSquared = (a.SphereCenter - b.SphereCenter).LengthSquared;
        float radiiSumSquared = (a.SphereRadius + b.SphereRadius) * (a.SphereRadius + b.SphereRadius);
        return distanceSquared <= radiiSumSquared;
    }

    public static bool CheckAABB(ColliderComponent a, ColliderComponent b)
    {
        return (a.AABBMin.X <= b.AABBMax.X && a.AABBMax.X >= b.AABBMin.X) &&
            (a.AABBMin.Y <= b.AABBMax.Y && a.AABBMax.Y >= b.AABBMin.Y) &&
            (a.AABBMin.Z <= b.AABBMax.Z && a.AABBMax.Z >= b.AABBMin.Z);
    }

    private static bool CheckOBB(ColliderComponent a, ColliderComponent b, out Vector3 resolution, out Vector3 contactPoint)
    {
        resolution = Vector3.Zero;
        contactPoint = Vector3.Zero;
        Dictionary<Vector3, bool> axes = new Dictionary<Vector3, bool>();
        
        foreach (Vector3 normal in a.GetWorldNormals())
            axes.TryAdd(normal, true);

        foreach (Vector3 normal in b.GetWorldNormals())
            axes.TryAdd(normal, true);

        foreach (var edgeA in a.GetWorldEdges())
        {
            foreach (var edgeB in b.GetWorldEdges())
            {
                Vector3 cross = Vector3.Cross(edgeA, edgeB);
                if (cross.LengthSquared > 0.0001f)
                {
                    axes.TryAdd(cross.Normalized(), true);
                }
            }
        }

        //Console.WriteLine(axes.Count);

        float minPenetration = float.MaxValue;
        Vector3 bestAxis = Vector3.Zero;

        foreach (Vector3 axis in axes.Keys)
        {
            if (!OverlapOnAxis(a, b, axis, out float penetration))
            {
                return false; // Found a separating axis → No collision
            }
            if (penetration < minPenetration)
            {
                minPenetration = penetration;
                bestAxis = axis;
            }
        }
        
        if(minPenetration < 0.002f)
        {
            resolution = Vector3.Zero;
            return false;
        }

        bestAxis = Vector3.Normalize(bestAxis);
        Vector3 relativePosition = b.Transform.WorldPosition - a.Transform.WorldPosition;
        
        if (Vector3.Dot(relativePosition, bestAxis) < 0)
            resolution = -bestAxis * minPenetration;
        else
            resolution = bestAxis * minPenetration;
        
        //FIND CONTACT POINT
        FindIntersectingPoints(a, b, bestAxis, out List<Vector3> aShape, out List<Vector3> bShape);

        List<Vector3> clippedAShape = aShape
            .Where(point => IsVertexInsideShape(bShape, bestAxis, point))
            .ToList();

        List<Vector3> clippedBShape = bShape
            .Where(point => IsVertexInsideShape(aShape, bestAxis, point))
            .ToList();
        
        //VERTEX
        if(aShape.Count == 1 && clippedAShape.Count == 0) //single contact point is off surface
            return false;
        if(bShape.Count == 1 && clippedBShape.Count == 0) //single contact point is off surface
            return false;

        if(clippedAShape.Count == 1) //single contact point is on surface
        {
            //Console.WriteLine("point A");
            contactPoint = clippedAShape[0];
            return true;
        }

        if(clippedBShape.Count == 1) //single contact point is on surface
        {
            //Console.WriteLine("point B");
            contactPoint = clippedBShape[0];
            return true;
        }

        //EDGE
        if(aShape.Count == 2 && clippedAShape.Count == 2) //middle of edge will work
        {
            //Console.WriteLine("edge midpoint A");
            contactPoint = (clippedAShape[0] + clippedAShape[1]) / 2f;
            return true;
        }

        if(bShape.Count == 2 && clippedBShape.Count == 2) //middle of edge will work
        {
            //Console.WriteLine("edge midpoint B");
            contactPoint = (clippedBShape[0] + clippedBShape[1]) / 2f;
            return true;
        }
        
        if(aShape.Count == 2 && bShape.Count == 2) //two edges contacting
        {
            //Console.WriteLine($"edge intersection");
            contactPoint = ComputeEdgeIntersection(aShape[0], aShape[1], bShape[0], bShape[1]);
            return true;
        }

        if(aShape.Count == 2 && bShape.Count >= 3)
        {
            //Console.WriteLine($"edge on face A");
            Vector3 midpoint = GetPolyhedronMidpoint(bShape);
            contactPoint = ClosestPointOnSegment(midpoint, aShape[0], aShape[1]);
            if(IsVertexInsideShape(bShape, bestAxis, contactPoint))
                return true;
        }

        if(bShape.Count == 2 && aShape.Count >= 3)
        {
            //Console.WriteLine($"edge on face B");
            Vector3 midpoint = GetPolyhedronMidpoint(aShape);
            contactPoint = ClosestPointOnSegment(midpoint, bShape[0], bShape[1]);
            if(IsVertexInsideShape(aShape, bestAxis, contactPoint))
                return true;
        }

        /*
        Console.WriteLine($"A Shape: {aShape.Count}");
        Console.WriteLine($"B Shape: {bShape.Count}");
        Console.WriteLine($"Clipped A Shape: {clippedAShape.Count}");
        Console.WriteLine($"Clipped B Shape: {clippedBShape.Count}"); 
        */

        if (clippedAShape.Count == 0 && clippedBShape.Count == 0)
        {
            return false;

            if(aShape.Count < bShape.Count)
            {
                Console.WriteLine("face midpoint A");
                contactPoint = GetPolyhedronMidpoint(aShape);
            }else
            {
                Console.WriteLine("face midpoint B");
                contactPoint = GetPolyhedronMidpoint(bShape);
            }

            GameEngine.Link.DebugDisplayPositions.Add(contactPoint);
            return true;
        }


        

        //GameEngine.Link.DebugDisplayPositions.Add(contactPoint);

        if(bShape.Count >= 3)
        {
            //Console.WriteLine($"face intersection");

            if(clippedBShape.Count == 0)
                contactPoint = GetPolyhedronMidpoint(SutherlandHodgmanClip(aShape, bShape, bestAxis));
            else
                contactPoint = GetPolyhedronMidpoint(SutherlandHodgmanClip(bShape, aShape, bestAxis));

            if(VectorHelper.IsVectorNaN(contactPoint))
                return false;
            
            return true;
        }
        
        if(aShape.Count >= 3)
        {
            //Console.WriteLine($"face intersection");

            if(clippedAShape.Count == 0)
                contactPoint = GetPolyhedronMidpoint(SutherlandHodgmanClip(bShape, aShape, bestAxis));
            else
                contactPoint = GetPolyhedronMidpoint(SutherlandHodgmanClip(aShape, bShape, bestAxis));
            
            if(VectorHelper.IsVectorNaN(contactPoint))
                return false;

            return true;
        }

        Console.WriteLine("NEVER");

        return false;
    }

    //Finds all of the points that are intersecting along the best axis (found from separating axis theorem)
    private static void FindIntersectingPoints(ColliderComponent a, ColliderComponent b, Vector3 bestAxis, out List<Vector3> aIntersect, out List<Vector3> bIntersect)
    {
        (float minA, float maxA) = ProjectOntoAxis(a, bestAxis);
        (float minB, float maxB) = ProjectOntoAxis(b, bestAxis);
        float overlapStart = Math.Max(minA, minB);
        float overlapEnd = Math.Min(maxA, maxB);
        
        aIntersect = new List<Vector3>();
        foreach (Vector3 vertex in a.GetWorldPoints())
        {
            float projection = Vector3.Dot(vertex, bestAxis);
            if (projection >= overlapStart && projection <= overlapEnd)
            {
                aIntersect.Add(vertex);
                //GameEngine.Link.DebugDisplayPositions.Add(vertex);
            }
        }
        
        bIntersect = new List<Vector3>();
        foreach (Vector3 vertex in b.GetWorldPoints())
        {
            float projection = Vector3.Dot(vertex, bestAxis);
            if (projection >= overlapStart && projection <= overlapEnd)
            {
                bIntersect.Add(vertex);
                //GameEngine.Link.DebugDisplayPositions.Add(vertex);
            }
        }
    }

    private static Vector3 ClampToPlane(Vector3 point, List<Vector3> shape)
    {
        Vector3 center = GetPolyhedronMidpoint(shape);

        Vector3 p1 = shape[0];
        Vector3 p2 = shape[1];
        Vector3 p3 = shape[2];

        Vector3 edge1 = p2 - p1;
        Vector3 edge2 = p3 - p1;

        Vector3 planeNormal = Vector3.Cross(edge1, edge2);
        planeNormal = Vector3.Normalize(planeNormal);

        float distance = Vector3.Dot(point - center, planeNormal);
        Vector3 projectedPoint = point - distance * planeNormal;

        return projectedPoint;
    }

    private static bool IsVertexInsideShape(List<Vector3> shape, Vector3 axis, Vector3 point)
    {
        if (shape.Count < 3)
            return false;

        /*
        if (shape.Count == 1)
            return Vector3.DistanceSquared(shape[0], point) < 1e-4f;

        if (shape.Count == 2)
            return IsPointNearSegment(point, shape[0], shape[1]);
        */

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

    private static bool IsPointNearSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = point - a;

        float t = Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab);
        t = Math.Clamp(t, 0, 1); // Clamp to segment

        Vector3 closestPoint = a + t * ab;
        return Vector3.DistanceSquared(closestPoint, point) < 1e-4f; // Tolerance check
    }

    private static Vector3 ClosestPointOnSegment(Vector3 P, Vector3 A, Vector3 B)
    {
        Vector3 AB = B - A;
        float t = Vector3.Dot(P - A, AB) / Vector3.Dot(AB, AB);
        t = Math.Clamp(t, 0.0f, 1.0f); // Clamp between segment endpoints
        return A + t * AB;
    }

    private static Vector3 ComputeEdgeIntersection(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
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

    private static List<Vector3> SutherlandHodgmanClip(List<Vector3> subjectPolyhedron, List<Vector3> clipPolyhedron, Vector3 axis)
    {
        Dictionary<Vector2, Vector3> projectionMap = new Dictionary<Vector2, Vector3>();

        //Generate 2D subject
        List<Vector2> subjectPolygon = new List<Vector2>();
        foreach (Vector3 point in subjectPolyhedron)
        {
            Vector2 projected = ProjectPointTo2D(point, axis);
            subjectPolygon.Add(projected);
            projectionMap[projected] = point;
        }
        PolarSort(ref subjectPolygon);

        //Generate 2D clip
        List<Vector2> clipPolygon = new List<Vector2>();
        foreach (Vector3 point in clipPolyhedron)
        {
            Vector2 projected = ProjectPointTo2D(point, axis);
            clipPolygon.Add(projected);
        }
        PolarSort(ref clipPolygon);
        
        
        //Check subject is fully in clip
        bool allInside = true;
        foreach (Vector2 point in subjectPolygon)
        {
            if (!IsPointInsideShape(point, clipPolygon))
            {
                allInside = false;
                break;
            }
        }
        if (allInside) return new List<Vector3>(subjectPolyhedron);

        //Check clip is fully in subject
        allInside = true;
        foreach (Vector2 point in clipPolygon)
        {
            if (!IsPointInsideShape(point, subjectPolygon))
            {
                allInside = false;
                break;
            }
        }
        if (allInside) return new List<Vector3>(clipPolyhedron);
        

        List<Vector2> outputList = new List<Vector2>(subjectPolygon);
        for (int i = 0; i < clipPolygon.Count; i++)
        {
            int next = (i + 1) % clipPolygon.Count;
            Vector2 clipEdgeStart = clipPolygon[i];
            Vector2 clipEdgeEnd = clipPolygon[next];
            
            List<Vector2> inputList = new List<Vector2>(outputList);
            outputList.Clear();
            
            if (inputList.Count == 0) continue;
            
            Vector2 prevVertex = inputList[inputList.Count - 1];
            foreach (Vector2 currVertex in inputList)
            {
                bool currInside = IsInside(currVertex, clipEdgeStart, clipEdgeEnd);
                bool prevInside = IsInside(prevVertex, clipEdgeStart, clipEdgeEnd);
                
                if (currInside)
                {
                    if (!prevInside)
                    {
                        Vector2 intersection2D = ComputeLineIntersection(prevVertex, currVertex, clipEdgeStart, clipEdgeEnd);
                        Vector3 intersection3D = Interpolate3D(projectionMap[prevVertex], projectionMap[currVertex], prevVertex, currVertex, intersection2D);
                        projectionMap[intersection2D] = intersection3D;
                        outputList.Add(intersection2D);
                    }
                    outputList.Add(currVertex);
                }
                else if (prevInside)
                {
                    Vector2 intersection2D = ComputeLineIntersection(prevVertex, currVertex, clipEdgeStart, clipEdgeEnd);
                    Vector3 intersection3D = Interpolate3D(projectionMap[prevVertex], projectionMap[currVertex], prevVertex, currVertex, intersection2D);
                    projectionMap[intersection2D] = intersection3D;
                    outputList.Add(intersection2D);
                }
                prevVertex = currVertex;
            }
        }

        /*
        if(outputList.Count != subjectPolyhedron.Count)
            return SutherlandHodgmanClip(clipPolyhedron, subjectPolyhedron, axis);
        */
        //Console.WriteLine($"Output list count: {outputList.Count}");
        //for(int i = 0; i < outputList.Count; i++)
            //Console.WriteLine($"Output #[{i}] = {outputList[i]}");

        List<Vector3> clipped3D = new List<Vector3>();
        foreach (Vector2 point2D in outputList)
        {
            if (projectionMap.ContainsKey(point2D))
            {
                clipped3D.Add(projectionMap[point2D]);
            }
        }
        
        return clipped3D;
    }

    private static bool IsPointInsideShape(Vector2 point, List<Vector2> shape)
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

    private static float GetAngle(Vector2 centroid, Vector2 point)
    {
        return (float) Math.Atan2(point.Y - centroid.Y, point.X - centroid.X);
    }

    private static void PolarSort(ref List<Vector2> points)
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

    private static bool IsInside(Vector2 p, Vector2 a, Vector2 b)
    {
        return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X) >= 0;
    }

    private static Vector3 Interpolate3D(Vector3 first3D, Vector3 second3D, Vector2 first2D, Vector2 second2D, Vector2 intersection2D)
    {
        // Compute the interpolation factor t based on 2D distances
        float totalDistance = Vector2.Distance(first2D, second2D);
        float intersectionDistance = Vector2.Distance(first2D, intersection2D);
        
        // Avoid division by zero in case of precision issues
        float t = (totalDistance > 1e-6f) ? intersectionDistance / totalDistance : 0.5f;

        // Linearly interpolate the 3D position
        return first3D + t * (second3D - first3D);
    }

    private static Vector2 ProjectPointTo2D(Vector3 point, Vector3 axis)
    {
        Vector3 u = Vector3.Cross(axis, Vector3.UnitX);
        if (u.LengthSquared < 1e-6f)
            u = Vector3.Cross(axis, Vector3.UnitY);
        u = Vector3.Normalize(u);
        Vector3 v = Vector3.Normalize(Vector3.Cross(axis, u));

        float x = Vector3.Dot(point, u);
        float y = Vector3.Dot(point, v);
        return new Vector2(x, y);
    }

    private static Vector2 ComputeLineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
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

    private static Vector3 GetPolyhedronMidpoint(List<Vector3> polygon)
    {
        Vector3 midpoint = Vector3.Zero;
        foreach (Vector3 point in polygon)
        {
            midpoint += point;
        }
        return midpoint / polygon.Count;
    }

    private static bool OverlapOnAxis(ColliderComponent a, ColliderComponent b, Vector3 axis, out float penetration)
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

    private static (float, float) ProjectOntoAxis(ColliderComponent col, Vector3 axis)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (Vector3 vertex in col.GetWorldPoints())
        {
            float projection = Vector3.Dot(vertex, axis);
            min = MathF.Min(min, projection);
            max = MathF.Max(max, projection);
        }

        return (min, max);
    }

    private static void ResolveCollision(ColliderComponent a, ColliderComponent b, Vector3 resolution, Vector3 contactPoint)
    {     
        if (DynamicObjects.ContainsKey(a))
            DynamicObjects[a].RespondToCollision(contactPoint, resolution, DynamicObjects.ContainsKey(b) ? DynamicObjects[b] : null);

        if (DynamicObjects.ContainsKey(b))
            DynamicObjects[b].RespondToCollision(contactPoint, -resolution,  DynamicObjects.ContainsKey(a) ? DynamicObjects[a] : null);
    }

    public static bool Raycast(Ray ray, out RayHit hit)
    {
        RaycastAll(ray, out List<RayHit> hits);
        
        if(hits.Count > 0)
        {
            hit = hits[0];
            return true;
        }else
        {
            hit = new RayHit(null, 0f, Vector3.Zero);
            return false;
        }
    }

    public static bool RaycastAll(Ray ray, out List<RayHit> hits)
    {
        hits = [];

        try
        {
            foreach (ColliderComponent col in Colliders)
            {
                if (RayIntersectsAABB(ray, col.AABBMin, col.AABBMax, out float distance))
                {
                    Vector3 hitPoint = ray.Origin + ray.Direction * distance;
                    hits.Add(new RayHit(col, distance, hitPoint));
                }
            }
        }catch
        {
            Logger.LogWarning("Raycast failed, collider collection was modified.");
        }

        if(hits.Count > 0)
        {
            hits.Sort((h1, h2) => h1.Distance.CompareTo(h2.Distance));
            return true;
        }
        
        return false;
    }

    private static bool RayIntersectsAABB(Ray ray, Vector3 aabbMin, Vector3 aabbMax, out float hitDistance)
    {
        hitDistance = float.MaxValue;

        // Calculate the intersections with the AABB boundaries
        Vector3 tMin = (aabbMin - ray.Origin) / ray.Direction;
        Vector3 tMax = (aabbMax - ray.Origin) / ray.Direction;

        // Ensure tMin and tMax represent the correct intersection boundaries
        float t1 = MathF.Min(tMin.X, tMax.X);
        float t2 = MathF.Max(tMin.X, tMax.X);
        
        float t3 = MathF.Min(tMin.Y, tMax.Y);
        float t4 = MathF.Max(tMin.Y, tMax.Y);

        // Find the largest of the "tMin" values and the smallest of the "tMax" values
        t1 = MathF.Max(t1, t3);
        t2 = MathF.Min(t2, t4);

        float t5 = MathF.Min(tMin.Z, tMax.Z);
        float t6 = MathF.Max(tMin.Z, tMax.Z);

        t1 = MathF.Max(t1, t5);
        t2 = MathF.Min(t2, t6);

        // If the ray doesn't intersect the AABB (t1 > t2), return false
        if (t1 > t2 || t2 < 0)
        {
            hitDistance = float.MaxValue;
            return false;
        }

        // If the ray starts inside the AABB, clamp t1 to 0, and return false (dont detect origin box)
        if (t1 < 0) 
        {
            t1 = 0;
            hitDistance = t1;
            return false;
        }

        hitDistance = t1;
        return hitDistance <= ray.Range;
    }

}

