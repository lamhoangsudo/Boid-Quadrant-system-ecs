using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct KdTreeSimple
{
    public struct KDNode
    {
        public float3 position; // Position in 3D space
        public int leftChild;  // Index of the left child node
        public int rightChild; // Index of the right child node
    }
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
}
