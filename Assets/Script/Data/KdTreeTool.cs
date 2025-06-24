using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct KdTreeTool
{
    public struct AxisComparer : IComparer<int>
    {
        private NativeArray<float3> pointsArray;
        private int axis;
        public AxisComparer(int axis, NativeArray<float3> pointsArray)
        {
            this.axis = axis;
            this.pointsArray = pointsArray;
        }

        public int Compare(int a, int b)
        {
            return pointsArray[a][axis].CompareTo(pointsArray[b][axis]);
        }
    }
    public struct KdTreeDelta
    {
        public NativeList<float3> addPosition;
        public NativeList<float3> removePosition;
    }
    private NativeList<KdTreeNode> nodes; // List of nodes in the KD-tree
    private NativeArray<int> indices; // Indices of points in the original array
    private NativeList<float3> points; // Points in the KD-tree
    private KdTreeDelta delta; // Delta for changes in the KD-tree
    public void BuildTree(NativeArray<float3> points, Allocator allocator)
    {
        if (!nodes.IsCreated)
        {
            nodes = new(allocator);
        }
        else
        {
            nodes.Clear();
        }
        
        // Clear existing nodes
        indices = new(points.Length, allocator);
        for (int i = 0; i < points.Length; i++)
        {
            indices[i] = i; // Initialize indices
        }
        BuildTreeRecursive(points, indices, 0);
        indices.Dispose(); // DisposeNode of indices after building the tree
    }
    private int BuildTreeRecursive(NativeArray<float3> points, NativeArray<int> indices, int depth)
    {
        if (indices.Length == 0)
        {
            return -1; // No points to process
        }
        int axis = depth % 3; // Cycle through axes (0, 1, 2 for x, y, z)
        AxisComparer comparer = new(axis, points);
        indices.Sort(comparer); // Sort indices based on the current axis
        int medianIndex = indices.Length / 2; // Find the median index
        int nodeIndex = nodes.Length; // Current node index
        var node = new KdTreeNode
        {
            position = points[indices[medianIndex]], // Set the position of the node
            leftChild = BuildTreeRecursive(points, indices.GetSubArray(0, medianIndex), depth + 1), // Left child
            rightChild = BuildTreeRecursive(points, indices.GetSubArray(medianIndex + 1, indices.Length - medianIndex - 1), depth + 1) // Right child
        };
        nodes.Add(node); // Add the node to the tree
        return nodeIndex;
    }
    public void DisposeNode()
    {
        if (nodes.IsCreated)
        {
            nodes.Clear(); // Clear the nodes list
            nodes.Dispose(); // DisposeNode of the nodes list
        }
    }
    public void DisposePoints()
    {
        if (points.IsCreated)
        {
            points.Clear(); // Clear the points array
            points.Dispose(); // DisposeNode of the points array
        }
    }
    public NativeArray<KdTreeNode> GetNodes(Allocator allocator)
    {
        return nodes.ToArray(allocator); // Return a copy of the nodes as a NativeArray
    }
    public void DetectChanges(NativeArray<float3> newPoints, Allocator allocator)
    {
        delta = new KdTreeDelta
        {
            addPosition = new NativeList<float3>(allocator),
            removePosition = new NativeList<float3>(allocator)
        };
        
        // Find added points
        foreach (var point in newPoints)
        {
            if (!points.Contains(point))
            {
                delta.addPosition.Add(point); // Add new point to the delta
            }
        }
        
        // Find removed points
        foreach (var point in points)
        {
            if (!newPoints.Contains(point))
            {
                delta.removePosition.Add(point); // Add removed point to the delta
            }
        }
    }
    public void InsertPoint(float3 point)
    {
        points.Add(point); // Add the new point to the points array
    }
    public void RemovePoint(float3 point)
    {
        int index = points.IndexOf(point); // Find the index of the point to remove
        if (index >= 0)
        {
            points.RemoveAt(index); // Remove the point from the points array
        }
    }
    public void ApplyChanges(NativeArray<float3> newPoints, Allocator allocator)
    {
        DetectChanges(newPoints, allocator); // Detect changes in the KD-tree
        
        // Add new points to the KD-tree
        foreach (var point in delta.addPosition)
        {
            InsertPoint(point); // Insert new point into the KD-tree
        }
        // Remove points from the KD-tree
        foreach (var point in delta.removePosition)
        {

            RemovePoint(point); // Remove point from the KD-tree
        }

        BuildTree(points.ToArray(allocator), allocator); // Rebuild the KD-tree with updated points
    }
}
