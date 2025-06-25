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
    private NativeArray<int> indices; // Indices of previousPoints in the original array
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
            return -1; // No previousPoints to process
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
            points.Clear(); // Clear the previousPoints array
            points.Dispose(); // DisposeNode of the previousPoints array
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
        
        // Find added previousPoints
        foreach (float3 point in newPoints)
        {
            if (!points.Contains(point))
            {
                delta.addPosition.Add(point); // Add new point to the delta
            }
        }
        
        // Find removed previousPoints
        foreach (float3 point in points)
        {
            if (!newPoints.Contains(point))
            {
                delta.removePosition.Add(point); // Add removed point to the delta
            }
        }
    }
    public void InsertPoint(float3 point)
    {
        points.Add(point); // Add the new point to the previousPoints array
        InsertIntoTree(point, 0, 0);
    }
    private void InsertIntoTree(float3 point, int currentIndex, int depth)
    {
        if (nodes.Length == 0)
        {
            nodes.Add(new KdTreeNode { position = point, leftChild = -1, rightChild = -1 });
            return;
        }

        int axis = depth % 3;
        KdTreeNode currentNode = nodes[currentIndex];

        if (point[axis] < currentNode.position[axis])
        {
            if (currentNode.leftChild == -1)
            {
                currentNode.leftChild = nodes.Length;
                nodes[currentIndex] = currentNode;
                nodes.Add(new KdTreeNode { position = point, leftChild = -1, rightChild = -1 });
            }
            else
            {
                InsertIntoTree(point, currentNode.leftChild, depth + 1);
            }
        }
        else
        {
            if (currentNode.rightChild == -1)
            {
                currentNode.rightChild = nodes.Length;
                nodes[currentIndex] = currentNode;
                nodes.Add(new KdTreeNode { position = point, leftChild = -1, rightChild = -1 });
            }
            else
            {
                InsertIntoTree(point, currentNode.rightChild, depth + 1);
            }
        }
    }
    public void RemovePoint(float3 point)
    {
        int index = points.IndexOf(point); // Find the index of the point to remove
        if (index >= 0)
        {
            points.RemoveAt(index); // Remove the point from the previousPoints array
            RemoveFromTree(point, 0, 0); // Remove the point from the KD-tree
        }
    }
    private void RemoveFromTree(float3 point, int currentIndex, int depth)
    {
        if (currentIndex < 0 || currentIndex >= nodes.Length) return;

        int axis = depth % 3;
        KdTreeNode node = nodes[currentIndex];

        if (math.all(node.position == point))
        {
            // Mark as removed (naive approach: replace with rightmost of left subtree)
            int replacementIndex = FindMax(node.leftChild, axis, depth + 1);
            if (replacementIndex != -1)
            {
                KdTreeNode replacement = nodes[replacementIndex];
                nodes[currentIndex] = replacement;
                RemoveFromTree(replacement.position, node.leftChild, depth + 1);
            }
            else
            {
                nodes[currentIndex] = new KdTreeNode { position = float3.zero, leftChild = -1, rightChild = -1 };
            }
            return;
        }

        if (point[axis] < node.position[axis])
        {
            RemoveFromTree(point, node.leftChild, depth + 1);
        }
        else
        {
            RemoveFromTree(point, node.rightChild, depth + 1);
        }
    }
    private int FindMax(int currentIndex, int axis, int depth)
    {
        if (currentIndex == -1) return -1;

        KdTreeNode node = nodes[currentIndex];
        int nodeAxis = depth % 3;

        if (nodeAxis == axis)
        {
            if (node.rightChild == -1) return currentIndex;
            return FindMax(node.rightChild, axis, depth + 1);
        }

        int leftMax = FindMax(node.leftChild, axis, depth + 1);
        int rightMax = FindMax(node.rightChild, axis, depth + 1);

        int maxIndex = currentIndex;
        if (leftMax != -1 && nodes[leftMax].position[axis] > nodes[maxIndex].position[axis])
            maxIndex = leftMax;
        if (rightMax != -1 && nodes[rightMax].position[axis] > nodes[maxIndex].position[axis])
            maxIndex = rightMax;

        return maxIndex;
    }
    public void ApplyChanges(NativeArray<float3> newPoints, NativeArray<float3> previousPoints, Allocator allocator)
    {
        foreach (float3 point in previousPoints)
        {
            points.Add(point);
        }

        DetectChanges(newPoints, allocator);

        foreach (float3 point in delta.addPosition)
        {
            InsertPoint(point);
        }

        foreach (float3 point in delta.removePosition)
        {
            RemovePoint(point);
        }
    }
}
