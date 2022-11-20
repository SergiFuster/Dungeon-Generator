using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

public class Partition
{
    public float width;
    public float height;
    public Vector3 center;
    public Partition first;
    public Partition second;
    public float offset;
    public Partition(float w, float h, Vector3 c, float _offset = 0, Partition first = null, Partition second = null)
    {
        width = w;
        height = h;
        center = c;
        this.first = first;
        this.second = second;
        offset = _offset;
    }

    public bool IsLeaf()
    {
        return first == null && second == null;
    }
}
