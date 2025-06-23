using Unity.Mathematics;
using UnityEngine;

public struct KdTreeNode
{
    public float3 position; // Position in 3D space
    public int leftChild;  // Index of the left child node
    public int rightChild; // Index of the right child node
}
