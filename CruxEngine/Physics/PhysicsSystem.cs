using CruxEngine.Utilities.Helpers;
using CruxEngine.Components;

namespace CruxEngine.Physics;

public static class PhysicsSystem
{
    public static int TotalColliders = 0;
    public static int TotalPhysicsObjects = 0;

    private static bool IntegratingAndComputing = false;

    public static bool TimeNextPhysicsStep = false;

    public static Octree Tree;

    private static readonly List<ColliderComponent> ColliderObjects = [];
    private static readonly Dictionary<ColliderComponent, PhysicsComponent> PhysicsObjects = [];

    //NEED TO SUPPORT NON STATIC COLLIDERS THAT DONT HAVE PHYSICS COMPONENTS
    //SO DYNAMIC OBJECTS OUTSIDE OF OCTREE LIKE PHYSICS

    private static readonly List<ColliderComponent> PendingAddColliderObjects = [];
    private static readonly Dictionary<ColliderComponent, PhysicsComponent> PendingAddPhysicsObjects = [];

    private static readonly List<ColliderComponent> PendingRemoveColliderObjects = [];
    private static readonly Dictionary<ColliderComponent, PhysicsComponent> PendingRemovePhysicsObjects = [];

    public static readonly Vector3 Gravity = new Vector3(0f, -9.8f, 0f);

    public static float FramesPerSecond = 0f;
    public static int PhysicsFrameCount = 0;
    public static int SphereChecks = 0;
    public static int AABBChecks = 0;
    public static int OBBChecks = 0;

    public static int SolverIterations = 1;

    static PhysicsSystem()
    {
        Tree = new Octree(new Vector3(-500, -500, -500), new Vector3(500, 500, 500), 7, "Physics Octree");
    }

    public static string GetShortInfo()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Physics FPS - {FramesPerSecond:F2}");
        sb.AppendLine($"Colliders - {ColliderObjects.Count}");
        sb.AppendLine($"Physics Objects - {PhysicsObjects.Count}");
        sb.AppendLine($"Sphere Checks - {SphereChecks}");
        sb.AppendLine($"AABB Checks - {AABBChecks}");
        sb.AppendLine($"OBB Checks - {OBBChecks}");
        
        return sb.ToString();
    }

    public static void RegisterColliderObject(ColliderComponent col)
    {
        lock (PendingAddColliderObjects)
        {
            if(!PendingAddColliderObjects.Contains(col))
                PendingAddColliderObjects.Add(col);
        }
    }

    public static void UnregisterColliderObject(ColliderComponent col)
    {
        lock (PendingRemoveColliderObjects)
        {
            if(!PendingRemoveColliderObjects.Contains(col))
                PendingRemoveColliderObjects.Add(col);
        }
    }

    public static void RegisterPhysicsObject(ColliderComponent col, PhysicsComponent phy)
    {          
        lock (PendingAddPhysicsObjects)
        {
            PendingAddPhysicsObjects.TryAdd(col, phy);
        }
    }

    public static void UnregisterPhysicsObject(ColliderComponent col, PhysicsComponent phy)
    {          
        lock (PendingRemovePhysicsObjects)
        {
            PendingRemovePhysicsObjects.TryAdd(col, phy);
        }
    }

    public static void MergeDictionaries()
    {
        //Add static
        lock (PendingAddColliderObjects)
        {
            ColliderObjects.AddRange(PendingAddColliderObjects);
            TotalColliders = ColliderObjects.Count;
            PendingAddColliderObjects.Clear();
        }

        //remove static
        lock (PendingRemoveColliderObjects)
        {
            ColliderObjects.RemoveAll(PendingRemoveColliderObjects.Contains);
            TotalColliders = ColliderObjects.Count;
            PendingRemoveColliderObjects.Clear();
        }

        //add Physics
        lock (PendingAddPhysicsObjects)
        {
            foreach (var pair in PendingAddPhysicsObjects)
                PhysicsObjects.Add(pair.Key, pair.Value);
            TotalPhysicsObjects = PhysicsObjects.Count;
            PendingAddPhysicsObjects.Clear();
        }

        //remove Physics
        lock (PendingRemovePhysicsObjects)
        {
            foreach (var pair in PendingRemovePhysicsObjects)
                PhysicsObjects.Remove(pair.Key);
            TotalPhysicsObjects = PhysicsObjects.Count;
            PendingRemovePhysicsObjects.Clear();
        }
    }

    public static void Update()
    {
        if(IntegratingAndComputing)
            return;
        IntegratingAndComputing = true;
        PhysicsFrameCount++;
        if(TimeNextPhysicsStep)
        {
            Logger.StartTimer("Physics Frame Update");
            TimeNextPhysicsStep = false;
        }
        //Logger.StartTimer("Merge Dictionaries");
        MergeDictionaries();
        //Logger.EndTimer();
        //Crux.Engine.DebugDisplayPositions.Clear();

        //Logger.StartTimer("Integrate");
        foreach (PhysicsComponent phy in PhysicsObjects.Values) //maps colliders to physics components
        {
            phy.Integrate();
        }
        //Logger.EndTimer();

        List<ColliderComponent> DynamicColliders = [];
        SphereChecks = 0;
        List<(ColliderComponent, ColliderComponent)> SphereConflicts = new List<(ColliderComponent, ColliderComponent)>();
        AABBChecks = 0;
        List<(ColliderComponent, ColliderComponent)> AABBConflicts = new List<(ColliderComponent, ColliderComponent)>();
        OBBChecks = 0;

        for(int i = 0; i < SolverIterations; i++)
        {      
            //Logger.StartTimer("Calculate World Bounds");
            //Calculate World Bounds for each collider that could have changed
            foreach (ColliderComponent col in ColliderObjects) //Contains all colliders, even the ones on Physics objects
            {
                if(!col.GameObject.IsFrozen)
                {
                    col.CalculateWorldData();
                    DynamicColliders.Add(col);
                }
            }
            //Logger.EndTimer();
            
            //For every physics object
            //Logger.StartTimer("Find Sphere Conflicts");
            foreach (var pair in PhysicsObjects)
            {
                List<ColliderComponent> nearby = Tree.FindNearbyNodes(pair.Key.AABBMin, pair.Key.AABBMax).OfType<ColliderComponent>().ToList();
                //nearby.AddRange(DynamicColliders); //make sure to check against dynamic, non octree colliders
                nearby.AddRange(PhysicsObjects.Keys); //make sure to check against Physicss always

                foreach (ColliderComponent collider in nearby)
                {
                    if (pair.Key == collider)
                        continue;

                    if (CheckSphere(pair.Key, collider))
                        SphereConflicts.Add((pair.Key, collider));
                    
                    SphereChecks++;
                }
            }
            //Logger.EndTimer();

            //Logger.StartTimer("Find AABB Conflicts");
            foreach (var (a, b) in SphereConflicts)
            {
                if (CheckAABB(a, b))
                    AABBConflicts.Add((a, b));
                AABBChecks++;
            }
            //Logger.EndTimer();
            
            //Logger.StartTimer("Find OBB Conflicts and Resolve");
            foreach (var (a, b) in AABBConflicts)
            {
                if (CheckOBB(a, b, out Vector3 resolution, out Vector3 contactPoint))
                    ResolveCollision(a, b, resolution, contactPoint);
                OBBChecks++;
            }
            //Logger.EndTimer();

            DynamicColliders.Clear();
            SphereConflicts.Clear();
            AABBConflicts.Clear();
        }
        /*
        for(int i = 0; i < SolverIterations; i++)
        {
            foreach (var (a, b) in AABBConflicts)
            {
                if (CheckOBB(a, b, out Vector3 resolution, out Vector3 contactPoint))
                    ResolveCollision(a, b, resolution, contactPoint);
                OBBChecks++;
            }
        }*/

        /*
        OBBChecks = 0;
        List<(ColliderComponent, ColliderComponent, Vector3 resolution, Vector3 contactPoint)> OBBConflicts = new();
        foreach (var (a, b) in AABBConflicts)
        {
            if (CheckOBB(a, b, out Vector3 resolution, out Vector3 contactPoint))
                OBBConflicts.Add((a, b, resolution, contactPoint));
            OBBChecks++;
        }
        
        OBBConflicts = OBBConflicts.OrderByDescending(conflict => conflict.contactPoint.Y).ToList();
        for(int i = 0; i < SolverIterations; i++)
            foreach (var (a, b, resolution, contactPoint) in OBBConflicts)
                ResolveCollision(a, b, resolution, contactPoint);
        */
        Logger.EndTimer();
        
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
    
        foreach (Vector3 normal in a.WorldNormals)
            axes.TryAdd(normal, true);

        foreach (Vector3 normal in b.WorldNormals)
            axes.TryAdd(normal, true);

        foreach (var edgeA in a.WorldEdges)
        {
            foreach (var edgeB in b.WorldEdges)
            {
                Vector3 cross = Vector3.Cross(edgeA, edgeB);
                if (cross.LengthSquared > 1e-6f) // small threshold for numerical stability
                    axes.TryAdd(Vector3.Normalize(cross), true);
            }
        }

        //30ms below
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
        
        float scale = MathF.Min(a.SphereRadius, b.SphereRadius);
        float penetrationEpsilon = scale * 0.01f; // 1% of object size

        if(minPenetration < penetrationEpsilon || bestAxis.LengthSquared < 1e-8f)
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
            //Logger.LogLine("point A");
            contactPoint = clippedAShape[0];
            return true;
        }

        if(clippedBShape.Count == 1) //single contact point is on surface
        {
            //Logger.LogLine("point B");
            contactPoint = clippedBShape[0];
            return true;
        }

        //EDGE
        if(aShape.Count == 2 && clippedAShape.Count == 2) //middle of edge will work
        {
            //Logger.LogWarning("aShape.Count == 2 && clippedAShape.Count == 2");  
            //Logger.LogLine("edge midpoint A");
            contactPoint = (clippedAShape[0] + clippedAShape[1]) / 2f;
            return true;
        }

        if(bShape.Count == 2 && clippedBShape.Count == 2) //middle of edge will work
        {
            //Logger.LogWarning("bShape.Count == 2 && clippedBShape.Count == 2");   
            //Logger.LogLine("edge midpoint B");
            contactPoint = (clippedBShape[0] + clippedBShape[1]) / 2f;
            return true;
        }
        
        if(aShape.Count == 2 && bShape.Count == 2) //two edges contacting
        {
            //Logger.LogWarning("aShape.Count == 2 && bShape.Count == 2");
            //Logger.LogLine($"edge intersection");
            contactPoint = ComputeEdgeIntersection(aShape[0], aShape[1], bShape[0], bShape[1]);
            return true;
        }

        if(aShape.Count == 2 && bShape.Count >= 3)
        {
            //Logger.LogWarning("aShape.Count == 2 && bShape.Count >= 3");
            //Logger.LogLine($"edge on face A");
            Vector3 midpoint = GetPolyhedronMidpoint(bShape);
            contactPoint = ClosestPointOnSegment(midpoint, aShape[0], aShape[1]);
            if(IsVertexInsideShape(bShape, bestAxis, contactPoint))
                return true;
        }

        if(bShape.Count == 2 && aShape.Count >= 3)
        {
            //Logger.LogWarning("bShape.Count == 2 && aShape.Count >= 3");
            //Logger.LogLine($"edge on face B");
            Vector3 midpoint = GetPolyhedronMidpoint(aShape);
            contactPoint = ClosestPointOnSegment(midpoint, bShape[0], bShape[1]);
            if(IsVertexInsideShape(aShape, bestAxis, contactPoint))
                return true;
        }

        //THESE ARE USED THE MOST FOR SOME REASON? CHECK THE LOGIC HERE, GETTING SLOWDOWS QUITE EASILY NOW.

        if (bShape.Count >= 3)
        {
            //Logger.LogWarning("bShape.Count >= 3");
            var clipped = SutherlandHodgmanClip(aShape, bShape, bestAxis);
            if (clipped == null || clipped.Count == 0)
                return false;

            contactPoint = GetPolyhedronMidpoint(clipped);
            return true;
        }

        if (aShape.Count >= 3)
        {
            //Logger.LogWarning("aShape.Count >= 3");
            var clipped = SutherlandHodgmanClip(bShape, aShape, bestAxis);
            if (clipped == null || clipped.Count == 0)
                return false;

            contactPoint = GetPolyhedronMidpoint(clipped);
            return true;
        }

        //Logger.LogWarning("You should never see this.");

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
        foreach (Vector3 vertex in a.WorldPoints)
        {
            float projection = Vector3.Dot(vertex, bestAxis);
            if (projection >= overlapStart && projection <= overlapEnd)
            {
                aIntersect.Add(vertex);
                //Crux.Engine.DebugDisplayPositions.Add(vertex);
            }
        }
        
        bIntersect = new List<Vector3>();
        foreach (Vector3 vertex in b.WorldPoints)
        {
            float projection = Vector3.Dot(vertex, bestAxis);
            if (projection >= overlapStart && projection <= overlapEnd)
            {
                bIntersect.Add(vertex);
                //Crux.Engine.DebugDisplayPositions.Add(vertex);
            }
        }
    }

    private static bool IsVertexInsideShape(List<Vector3> shape, Vector3 axis, Vector3 point)
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
        if (polygon == null || polygon.Count == 0)
            return Vector3.Zero;

        Vector3 midpoint = Vector3.Zero;
        foreach (Vector3 point in polygon)
            midpoint += point;

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

        foreach (Vector3 vertex in col.WorldPoints)
        {
            float projection = Vector3.Dot(vertex, axis);
            min = MathF.Min(min, projection);
            max = MathF.Max(max, projection);
        }

        return (min, max);
    }

    private static void ResolveCollision(ColliderComponent a, ColliderComponent b, Vector3 resolution, Vector3 contactPoint)
    {     
        if (PhysicsObjects.ContainsKey(a))
            PhysicsObjects[a].RespondToCollision(contactPoint, -resolution, PhysicsObjects.ContainsKey(b) ? PhysicsObjects[b] : null!);

        if (PhysicsObjects.ContainsKey(b))
            PhysicsObjects[b].RespondToCollision(contactPoint, resolution, PhysicsObjects.ContainsKey(a) ? PhysicsObjects[a] : null!);
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
            hit = new RayHit(null!, 0f, Vector3.Zero);
            return false;
        }
    }

    public static bool RaycastAll(Ray ray, out List<RayHit> hits)
    {
        hits = [];

        try
        {
            foreach (ColliderComponent col in ColliderObjects)
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

