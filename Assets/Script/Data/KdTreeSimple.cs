using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct KdTreeSimple
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
    private NativeList<KdTreeNode> nodes; // List of nodes in the KD-tree
    private NativeArray<int> indices; // Indices of points in the original array
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
        indices.Dispose(); // Dispose of indices after building the tree
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
    public void Dispose()
    {
        if (nodes.IsCreated)
        {
            nodes.Dispose();
        }
    }
    public NativeArray<KdTreeNode> GetNodes(Allocator allocator)
    {
        return nodes.ToArray(allocator); // Return a copy of the nodes as a NativeArray
    }
}
