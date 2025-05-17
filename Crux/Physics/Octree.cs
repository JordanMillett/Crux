namespace Crux.Physics;

public class OctreeNode
{
    public List<Component> Components = [];
    public OctreeNode[]? Octants = null;
    public Vector3 Min;
    public Vector3 Max;
    public bool Culled = false;

    public bool IsLeaf => Octants == null;

    public OctreeNode(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;

        /*
        GameObject debug = GameEngine.Link.InstantiateGameObject();
        debug.AddComponent<Components.LineRenderComponent>();
        debug.Transform.WorldPosition = (Min + Max) * 0.5f;
        debug.Transform.Scale = Max - Min;
        */
    }

    public void Divide()
    {
        Vector3 center = (Min + Max) * 0.5f;
        Octants = new OctreeNode[8];

        Octants[0] = new OctreeNode(Min, center); // Bottom-left-front
        Octants[1] = new OctreeNode(new Vector3(center.X, Min.Y, Min.Z), new Vector3(Max.X, center.Y, center.Z)); // Bottom-right-front
        Octants[2] = new OctreeNode(new Vector3(Min.X, Min.Y, center.Z), new Vector3(center.X, center.Y, Max.Z)); // Bottom-left-back
        Octants[3] = new OctreeNode(new Vector3(center.X, Min.Y, center.Z), new Vector3(Max.X, center.Y, Max.Z)); // Bottom-right-back

        Octants[4] = new OctreeNode(new Vector3(Min.X, center.Y, Min.Z), new Vector3(center.X, Max.Y, center.Z)); // Top-left-front
        Octants[5] = new OctreeNode(new Vector3(center.X, center.Y, Min.Z), new Vector3(Max.X, Max.Y, center.Z)); // Top-right-front
        Octants[6] = new OctreeNode(new Vector3(Min.X, center.Y, center.Z), new Vector3(center.X, Max.Y, Max.Z)); // Top-left-back
        Octants[7] = new OctreeNode(center, Max); // Top-right-back
    }
}

public class Octree
{
    public OctreeNode Root;
    public readonly int MaxDepth;
    public readonly string OctreeName;

    public Octree(Vector3 min, Vector3 max, int maxDepth = 7, string octreeName = "Octree")
    {
        Root = new OctreeNode(min, max);
        MaxDepth = maxDepth;
        OctreeName = octreeName;
    }

    public (Vector3 MinKey, Vector3 MaxKey) RegisterComponentGetAABB(Component component, Vector3 min, Vector3 max)
    {
        //Logger.Log($"Added GameObject '{component.GameObject.Name}' to {OctreeName}");
        InsertComponent(Root, component, min, max, 0);
        return (min, max);
    }

    public OctreeNode RegisterComponentGetNode(Component component, Vector3 min, Vector3 max)
    {
        //Logger.Log($"Added GameObject '{component.GameObject.Name}' to {OctreeName}");
        return InsertComponent(Root, component, min, max, 0);
    }

    public void UnregisterComponent(Component component, (Vector3 minKey, Vector3 maxKey) octreeKeys)
    {
        RemoveComponent(Root, component, octreeKeys.minKey, octreeKeys.maxKey);
        Logger.Log($"Removed GameObject '{component.GameObject.Name}' from {OctreeName}");
    }

    public List<Component> FindNearbyNodes(Vector3 min, Vector3 max)
    {
        List<Component> found = new List<Component>();
        FindNearbyNodesRecursive(Root, min, max, ref found);
        return found;
    }

    private void FindNearbyNodesRecursive(OctreeNode node, Vector3 min, Vector3 max, ref List<Component> results)
    {
        if (!AABBIntersects(node.Min, node.Max, min, max))
            return; // If the object doesn't intersect this node, return

        // Add all objects in this node (could be leaves or non-split nodes)
        results.AddRange(node.Components);

        // If this node has children, check them too
        if (!node.IsLeaf)
        {
            foreach (var child in node.Octants!)
            {
                FindNearbyNodesRecursive(child, min, max, ref results);
            }
        }
    }

    private OctreeNode InsertComponent(OctreeNode node, Component component, Vector3 min, Vector3 max, int depth)
    {
        // If we've reached max depth, store the object in this node
        if (depth >= MaxDepth)
        {
            node.Components.Add(component);
            return node;
        }

        // If the node is not yet divided, divide it
        if (node.IsLeaf)
            node.Divide();

        // Try to insert into one of the children
        foreach (var child in node.Octants)
        {
            if (AABBContains(child.Min, child.Max, min, max))
            {
                // Object was placed in a child, so stop processing
                return InsertComponent(child, component, min, max, depth + 1);
            }
        }

        // If no child fully contained it, store the object in this node
        node.Components.Add(component);
        return node;
    }

    private bool RemoveComponent(OctreeNode node, Component component, Vector3 min, Vector3 max)
    {
        // Try removing from this node
        if (node.Components.Remove(component))
        {
            return true;
        }

        // If the node is a leaf, we can't go further
        if (node.IsLeaf) 
            return false;

        // Try to remove from child nodes
        foreach (var child in node.Octants)
        {
            if (AABBContains(child.Min, child.Max, min, max) && RemoveComponent(child, component, min, max))
            {
                // After removal, check if we should clean up
                CleanupNode(node);
                return true;
            }
        }

        return false;
    }

    private void CleanupNode(OctreeNode node)
    {
        // If any child node still contains components, don't remove children
        if (node.Octants!.Any(child => child.Components.Count > 0 || !child.IsLeaf))
            return;

        // If all children are empty, collapse the node
        node.Octants = null;
    }

    private bool AABBIntersects(Vector3 nodeMin, Vector3 nodeMax, Vector3 objMin, Vector3 objMax)
    {
        return objMax.X >= nodeMin.X && objMin.X <= nodeMax.X &&
            objMax.Y >= nodeMin.Y && objMin.Y <= nodeMax.Y &&
            objMax.Z >= nodeMin.Z && objMin.Z <= nodeMax.Z;
    }

    private bool AABBContains(Vector3 nodeMin, Vector3 nodeMax, Vector3 objMin, Vector3 objMax)
    {
        return objMin.X >= nodeMin.X && objMax.X <= nodeMax.X &&
            objMin.Y >= nodeMin.Y && objMax.Y <= nodeMax.Y &&
            objMin.Z >= nodeMin.Z && objMax.Z <= nodeMax.Z;
    }

    public void RecalculateVisibility()
    {
        RecalculateVisibilityRecursive(Root);
    }

    private void RecalculateVisibilityRecursive(OctreeNode node, bool forceCull = false)
    {
        if(!forceCull)
        {
            node.Culled = GameEngine.Link.Camera.OutsideOfFrustrum(node.Min, node.Max);
        }else
        {
            node.Culled = true;
        }

        if(!node.IsLeaf)
        {
            foreach (OctreeNode octant in node.Octants)
            {
                RecalculateVisibilityRecursive(octant, node.Culled);
            }
        }
    }
}
