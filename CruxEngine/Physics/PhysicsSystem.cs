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

    //Logger.Log($"Solver Iterations: {PhysicsSystem.SolverIterations}");

    private static int solverIterations = 2; //2
    public static int SolverIterations
    { 
        get => solverIterations;
        set 
        { 
            if (solverIterations != value)
            {
                Logger.Log($"Solver Iterations Changed: {solverIterations} -> {value}");
                solverIterations = value;
            }
        }
    }

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
            if(!phy.IsSleeping)
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
                if(pair.Value.IsSleeping)
                    continue;

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
            if (!VectorHelper.OverlapOnAxis(a, b, axis, out float penetration))
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
            .Where(point => VectorHelper.IsVertexInsideShape(bShape, bestAxis, point))
            .ToList();

        List<Vector3> clippedBShape = bShape
            .Where(point => VectorHelper.IsVertexInsideShape(aShape, bestAxis, point))
            .ToList();
        
        //VERTEX
        if(aShape.Count == 1 && clippedAShape.Count == 0) //single contact point is off surface
            return false;
        if(bShape.Count == 1 && clippedBShape.Count == 0) //single contact point is off surface
            return false;

        if(clippedAShape.Count == 1) //single contact point is on surface
        {
            contactPoint = clippedAShape[0]; 
            if(Debug.FlagEnabled("LogCollisions"))
            {
                Logger.Log("clippedAShape.Count == 1");  
                Logger.Log(clippedAShape[0] - a.Transform.WorldPosition); 
                Logger.Log("");
            }
            return true;
        }

        if(clippedBShape.Count == 1) //single contact point is on surface
        {
            contactPoint = clippedBShape[0];
            if(Debug.FlagEnabled("LogCollisions"))
            {
                Logger.Log("clippedBShape.Count == 1");  
                Logger.Log(clippedBShape[0] - b.Transform.WorldPosition); 
                Logger.Log("");
            }
            return true;
        }

        //EDGE
        if(aShape.Count == 2 && clippedAShape.Count == 2) //middle of edge will work
        {
            contactPoint = (clippedAShape[0] + clippedAShape[1]) / 2f;
            if(Debug.FlagEnabled("LogCollisions"))
            {
                Logger.Log("aShape.Count == 2 && clippedAShape.Count == 2");  
                Logger.Log($"{contactPoint - a.Transform.WorldPosition}");
                Logger.Log("");
            }
            return true;
        }

        if(bShape.Count == 2 && clippedBShape.Count == 2) //middle of edge will work
        {
            contactPoint = (clippedBShape[0] + clippedBShape[1]) / 2f;
            if(Debug.FlagEnabled("LogCollisions"))
            {
                Logger.Log("bShape.Count == 2 && clippedBShape.Count == 2");  
                Logger.Log($"{contactPoint - b.Transform.WorldPosition}");
                Logger.Log("");
            }
            return true;
        }
        
        if(aShape.Count == 2 && bShape.Count == 2) //two edges contacting
        {
            contactPoint = VectorHelper.ComputeEdgeContactPoint(aShape[0], aShape[1], bShape[0], bShape[1]);
            if(Debug.FlagEnabled("LogCollisions"))
            {
                Logger.Log("aShape.Count == 2 && bShape.Count == 2");  
                Logger.Log($"{contactPoint - a.Transform.WorldPosition}");
                Logger.Log($"{contactPoint - b.Transform.WorldPosition}");
                Logger.Log("");
            }
            return true;
        }

        if(aShape.Count == 2 && bShape.Count >= 3)
        {
            Vector3 midpoint = VectorHelper.GetPolyhedronMidpoint(bShape);
            contactPoint = VectorHelper.ClosestPointOnSegment(midpoint, aShape[0], aShape[1]);
            if(VectorHelper.IsVertexInsideShape(bShape, bestAxis, contactPoint))
            {
                if(Debug.FlagEnabled("LogCollisions"))
                {
                    Logger.Log("aShape.Count == 2 && bShape.Count >= 3");  
                    Logger.Log($"{contactPoint - a.Transform.WorldPosition}");
                    Logger.Log($"{contactPoint - b.Transform.WorldPosition}");
                    Logger.Log("");
                }
                return true;
            }
        }

        if(bShape.Count == 2 && aShape.Count >= 3)
        {
            Vector3 midpoint = VectorHelper.GetPolyhedronMidpoint(aShape);
            contactPoint = VectorHelper.ClosestPointOnSegment(midpoint, bShape[0], bShape[1]);
            if(VectorHelper.IsVertexInsideShape(aShape, bestAxis, contactPoint))
            {
                if(Debug.FlagEnabled("LogCollisions"))
                {
                    Logger.Log("bShape.Count == 2 && aShape.Count >= 3");  
                    Logger.Log($"{contactPoint - a.Transform.WorldPosition}");
                    Logger.Log($"{contactPoint - b.Transform.WorldPosition}");
                    Logger.Log("");
                }
                return true;
            }
        }

        //THESE ARE USED THE MOST FOR SOME REASON? CHECK THE LOGIC HERE, GETTING SLOWDOWS QUITE EASILY NOW.

        if (bShape.Count >= 3)
        {
            var clipped = SutherlandHodgmanClip(aShape, bShape, bestAxis);
            if (clipped == null || clipped.Count == 0)
                return false;

            contactPoint = VectorHelper.GetPolyhedronMidpoint(clipped);

            if(Debug.FlagEnabled("LogCollisions"))
            {    
                Logger.Log("bShape.Count >= 3");  
                Logger.Log($"{contactPoint - b.Transform.WorldPosition}");
                Logger.Log("");
            }
            return true;
        }

        if (aShape.Count >= 3)
        {
            var clipped = SutherlandHodgmanClip(bShape, aShape, bestAxis);
            if (clipped == null || clipped.Count == 0)
                return false;

            contactPoint = VectorHelper.GetPolyhedronMidpoint(clipped);

            if(Debug.FlagEnabled("LogCollisions"))
            {
                Logger.Log("aShape.Count >= 3");  
                Logger.Log($"{contactPoint - a.Transform.WorldPosition}");
                Logger.Log("");
            }
            return true;
        }

        //Logger.LogWarning("You should never see this.");

        return false;
    }

    //Finds all of the points that are intersecting along the best axis (found from separating axis theorem)
    private static void FindIntersectingPoints(ColliderComponent a, ColliderComponent b, Vector3 bestAxis, out List<Vector3> aIntersect, out List<Vector3> bIntersect)
    {
        (float minA, float maxA) = VectorHelper.ProjectOntoAxis(a, bestAxis);
        (float minB, float maxB) = VectorHelper.ProjectOntoAxis(b, bestAxis);
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

    private static List<Vector3> SutherlandHodgmanClip(List<Vector3> subjectPolyhedron, List<Vector3> clipPolyhedron, Vector3 axis)
    {
        Dictionary<Vector2, Vector3> projectionMap = new Dictionary<Vector2, Vector3>();

        //Generate 2D subject
        List<Vector2> subjectPolygon = new List<Vector2>();
        foreach (Vector3 point in subjectPolyhedron)
        {
            Vector2 projected = VectorHelper.ProjectPointTo2D(point, axis);
            subjectPolygon.Add(projected);
            projectionMap[projected] = point;
        }
        VectorHelper.PolarSort(ref subjectPolygon);

        //Generate 2D clip
        List<Vector2> clipPolygon = new List<Vector2>();
        foreach (Vector3 point in clipPolyhedron)
        {
            Vector2 projected = VectorHelper.ProjectPointTo2D(point, axis);
            clipPolygon.Add(projected);
        }
        VectorHelper.PolarSort(ref clipPolygon);
        
        
        //Check subject is fully in clip
        bool allInside = true;
        foreach (Vector2 point in subjectPolygon)
        {
            if (!VectorHelper.IsPointInsideShape(point, clipPolygon))
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
            if (!VectorHelper.IsPointInsideShape(point, subjectPolygon))
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
                bool currInside = VectorHelper.IsInside(currVertex, clipEdgeStart, clipEdgeEnd);
                bool prevInside = VectorHelper.IsInside(prevVertex, clipEdgeStart, clipEdgeEnd);
                
                if (currInside)
                {
                    if (!prevInside)
                    {
                        Vector2 intersection2D = VectorHelper.ComputeLineIntersection(prevVertex, currVertex, clipEdgeStart, clipEdgeEnd);
                        Vector3 intersection3D = VectorHelper.Interpolate3D(projectionMap[prevVertex], projectionMap[currVertex], prevVertex, currVertex, intersection2D);
                        projectionMap[intersection2D] = intersection3D;
                        outputList.Add(intersection2D);
                    }
                    outputList.Add(currVertex);
                }
                else if (prevInside)
                {
                    Vector2 intersection2D = VectorHelper.ComputeLineIntersection(prevVertex, currVertex, clipEdgeStart, clipEdgeEnd);
                    Vector3 intersection3D = VectorHelper.Interpolate3D(projectionMap[prevVertex], projectionMap[currVertex], prevVertex, currVertex, intersection2D);
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

