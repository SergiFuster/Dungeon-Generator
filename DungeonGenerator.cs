using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class DungeonGenerator : MonoBehaviour
{
    public float width;
    public float height;
    public float minWidth;
    public float minHeight;
    [Range(0, 1)]
    public float horizontalCutRatio;
    [Range(0, 1)]
    public float tryOtherCutRatio;
    public LayerMask FloorMask;
    public bool ShowDebug;
    public GameObject floor;
    [Range(1, 10)]
    public float minOffset;
    [Range(1, 100)]
    public float maxOffset;
    public bool RandomEachIteration;
    public int PathLength;
    public int Iterations;
    public bool treeGenerated = true;
    private Partition root = null;
    private List<Partition> leafs = new List<Partition>();


    // Start is called before the first frame update
    void Start()
    {
        root = new Partition(width, height, transform.position);
        GenerateTree(root);
        Debug.Log("Number of leafs = " + leafs.Count);
        return;
        GenerateRooms();
        GenerateBridges();
    }

    private void GenerateBridges()
    {
        Stack<Partition> stack = new Stack<Partition> ();
        stack.Push(root);
        Partition actual;
        List<Partition> left_leafs = new List<Partition>();
        List<Partition> right_leafs =new List<Partition>();
        while (stack.Count > 0)
        {
            actual = stack.Pop();
            if (!actual.IsLeaf())
            {
                left_leafs.Clear();
                right_leafs.Clear();
                left_leafs = GetLeafsOfSide(actual, true);
                right_leafs = GetLeafsOfSide(actual, false);
                float minDistance = float.MaxValue;
                Partition[] two_closer = new Partition[2];
                foreach(Partition left in left_leafs)
                {
                    foreach(Partition right in right_leafs)
                    {
                        if(Vector3.Distance(left.center, right.center) < minDistance)
                        {
                            two_closer[0] = left;
                            two_closer[1] = right;
                        }
                    }
                }
                Bridge(two_closer[0], two_closer[1]);
                stack.Push(actual.first);
                stack.Push(actual.second);
            }
        }
    }

    private void Bridge(Partition one, Partition two)
    {
        float fsize = 1; // floor.GetComponent<Collider>().bounds.size.x;
        Vector3 actual_pos = one.center;
        Instantiate(floor, actual_pos, Quaternion.identity);
        while (actual_pos.y != two.center.y)
        {
            if(actual_pos.y < two.center.y)
                actual_pos.y += fsize;
            else
                actual_pos.y -= fsize;
            if (Math.Abs(Math.Abs(actual_pos.y) - Math.Abs(two.center.y)) < fsize) actual_pos.y = two.center.y;
            Instantiate(floor, actual_pos, Quaternion.identity);
        }
        while (actual_pos.x != two.center.x)
        {
            if (actual_pos.x < two.center.x)
                actual_pos.x += fsize;
            else
                actual_pos.x -= fsize;
            if (Math.Abs(Math.Abs(actual_pos.x) - Math.Abs(two.center.x)) < fsize) actual_pos.x = two.center.x;
            
            Instantiate(floor, actual_pos, Quaternion.identity);
        }
    }
    private void FakeStart()
    {
        root = new Partition(width, height, transform.position);
        GenerateTree(root);
    }
    private void GenerateTree(Partition part)
    {
        if (AddPartitions(part))
        {
            GenerateTree(part.first);
            GenerateTree(part.second);
        }
        else leafs.Add(part);
    }
    private void GenerateRooms()
    {
        if (leafs.Count > 0)
        {
            foreach (Partition leaf in leafs)
            {
                GenerateFloor(leaf);
            }
        }
    }

    private void GenerateFloor(Partition partition)
    {
        HashSet<Vector3> floor = new HashSet<Vector3>();
        floor.Add(partition.center);

    }

    private List<Partition> GetLeafsOfSide(Partition part, bool left)
    {
        if (part.IsLeaf()) return null;
        Stack<Partition> stack = new Stack<Partition>();
        List<Partition> leafs = new List<Partition>();
        Partition actual;

        if (left)
            stack.Push(part.first);
        else
            stack.Push(part.second);

        while(stack.Count > 0)
        {
            actual = stack.Pop();
            if (actual.IsLeaf()) leafs.Add(actual);
            else
            {
                stack.Push(actual.first);
                stack.Push(actual.second);
            }
        }
        return leafs;
    }
    private bool AddPartitions(Partition part) 
    {
        /* Return bool indicating if partitioning was posible */
        float random = UnityEngine.Random.value;
        bool canCut = false;
        if(random < horizontalCutRatio) //Horizontally
        {
            canCut = part.height >= minHeight * 2;
            if (canCut)
            {
                float cut = UnityEngine.Random.Range(minHeight, part.height - minHeight);
                float yCenter = part.center.y - part.height / 2 + cut + (part.height - cut) / 2;
                float offset = UnityEngine.Random.Range(minOffset, maxOffset);
                part.first = new Partition(part.width, part.height - cut, new Vector3(part.center.x, yCenter, 0), offset);
                yCenter = part.center.y - (part.height / 2) + (cut / 2);
                offset = UnityEngine.Random.Range(minOffset, maxOffset);
                part.second = new Partition(part.width, cut, new Vector3(part.center.x, yCenter, 0), offset);
            }
            else
            {
                if(UnityEngine.Random.value < tryOtherCutRatio)
                {
                    canCut = part.width >= minWidth * 2;
                    if (canCut)
                    {
                        float cut = UnityEngine.Random.Range(minWidth, part.width - minWidth);
                        float xCenter = part.center.x - (part.width / 2) + cut + (part.width - cut) / 2;
                        float offset = UnityEngine.Random.Range(minOffset, maxOffset);
                        part.first = new Partition(part.width - cut, part.height, new Vector3(xCenter, part.center.y, 0), offset);
                        xCenter = part.center.x - (part.width / 2) + (cut / 2);
                        offset = UnityEngine.Random.Range(minOffset, maxOffset);
                        part.second = new Partition(cut, part.height, new Vector3(xCenter, part.center.y, 0), offset);
                    }
                }
            }
        }
        else //Vertically
        {
            canCut = part.width >= minWidth * 2;
            if (canCut)
            {
                float cut = UnityEngine.Random.Range(minWidth, part.width - minWidth);
                float xCenter = part.center.x - (part.width / 2) + cut + (part.width - cut) / 2;
                float offset = UnityEngine.Random.Range(minOffset, maxOffset);
                part.first = new Partition(part.width - cut, part.height, new Vector3(xCenter, part.center.y, 0), offset);
                xCenter = part.center.x - (part.width / 2) + (cut / 2);
                offset = UnityEngine.Random.Range(minOffset, maxOffset);
                part.second = new Partition(cut, part.height, new Vector3(xCenter, part.center.y, 0), offset);
            }
            else
            {
                if (UnityEngine.Random.value < tryOtherCutRatio)
                {
                    canCut = part.height >= minHeight * 2;
                    if (canCut)
                    {
                        float cut = UnityEngine.Random.Range(minHeight, part.height - minHeight);
                        float yCenter = part.center.y - part.height / 2 + cut + (part.height - cut) / 2;
                        float offset = UnityEngine.Random.Range(minOffset, maxOffset);
                        part.first = new Partition(part.width, part.height - cut, new Vector3(part.center.x, yCenter, 0), offset);
                        yCenter = part.center.y - (part.height / 2) + (cut / 2);
                        offset = UnityEngine.Random.Range(minOffset, maxOffset);
                        part.second = new Partition(part.width, cut, new Vector3(part.center.x, yCenter, 0), offset);
                    }
                }
            }
        }
        return canCut;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0));
        if(!ShowDebug) return;

        if(!treeGenerated)
        {
            if (root  != null)
            {
                root.first = null;
                root.second = null;
            }
            leafs.Clear();
            FakeStart();
            treeGenerated = true;
        }
        if(leafs.Count > 0)
        {
            foreach(Partition leaf in leafs)
            {
                Gizmos.DrawWireCube(leaf.center, new Vector3(leaf.width - leaf.offset, leaf.height - leaf.offset, 0));
            }
        }
    }
}
