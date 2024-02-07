using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class IcosepTree
{
    public static int SplitValue = 60;

    // Add all of our nodes to a set
    //public HashSet<Node> Nodes;

    private List<Node> nodes = new List<Node>();

    private bool isSplit = false;
    private IcosepTree parent;
    private float3 split;
    private float3 min, max;
    private bool extentsCorrect;

    public IcosepTree[] Children { get; set; }
    public bool IsLeaf => !isSplit;
    public List<Node> GetNodes => nodes;
    public int NodeAmount => nodes.Count;
    public float3 Min => min;
    public float3 Max => max;

    public IcosepTree(IcosepTree parent)
    {
        Children = new IcosepTree[27];

        this.parent = parent;
        Unsplit();
        extentsCorrect = false;
    }

    /// <summary>
    /// Returns all nodes in node and children nodes
    /// Allocates memory
    /// </summary>
    /// <returns></returns>
    public List<Node> GetNodesInChildren()
    {
        if (!isSplit)
        {
            return nodes;
        }

        List<Node> result = new List<Node>(nodes);

        for (int i = 0; i < Children.Length; i++)
        {
            if (Children[i] != null)
            {
                result.AddRange(Children[i].GetNodesInChildren());
            }
        }

        return result;
    }


    // Get the AABB of the tree
    public void GetExtents(ref float3 min, ref float3 max, bool split)
    {
        if (!extentsCorrect)
        {
            UpdateExtents();
        }

        if (split && !isSplit && NodeAmount > SplitValue)
        {
            DoSplit();
        }

        min = this.min;
        max = this.max;
    }

    // Add a node, returning the subtree it’s in
    public IcosepTree AddNode(Node node)
    {
        IcosepTree tree = FindNode(node);
        if (tree is null)
        {
            Debug.LogError("Could not add node");
            return null;
        }

        tree.InsertNode(node);

        float3 pr = new float3(node.Radius, node.Radius, node.Radius);
        tree.GrowExtents(node.Pos - pr, node.Pos + pr);

        return tree;
    }

    // Find the subtree a node is in
    public IcosepTree FindNode(Node node)
    {
        return FindNode(node.Pos, node.Radius);
    }

    public IcosepTree FindNode(float3 pos, float radius)
    {
        IcosepTree tree = this;
        int c = WhichChild(pos, radius);

        while (c > 0)
        {
            tree = tree.GetChild(c);
            c = tree.WhichChild(pos, radius);
        }

        return tree;
    }

    public List<Node> Query(float3 pos, float radius)
    {
        List<Node> result = new List<Node>(nodes);
        float3 min = new float3(pos.x - radius, pos.y - radius, pos.z - radius);
        float3 max = new float3(pos.x + radius, pos.y + radius, pos.z + radius);

        for (int i = 0; i < Children.Length; i++)
        {
            if (Children[i] == null)
            {
                continue;
            }

            if (Children[i].PartialContains(min, max))
            {
                float3 _min = float3.zero, _max = float3.zero;
                Children[i].GetExtents(ref _min, ref _max, true);
                result.AddRange(Children[i].Query(pos, radius));
            }
        }

        return result;
    }

    // Remove a node from this tree (no search is performed)
    public bool RemoveNode(Node node)
    {
        return RemoveNode(node, node.Pos, node.Radius);
    }

    public bool RemoveNode(Node node, float3 oldPosition, float oldRadius)
    {
        ShrinkExtents(oldPosition, oldRadius);

#if DEBUG
        if (!nodes.Contains(node))
        {
            Debug.LogError("Tried to remove node from tree that does not contain it");

            if (parent != null)
            {
                return parent.RemoveNode(node, oldPosition, oldRadius);
            }

            return false;
        }
#endif
        nodes.Remove(node);

        TestSuicide();
        return true;
    }

    // Update a node’s location within the tree, taking advantage
    // of coherence
    public bool UpdateNode(Node node, float3 oldPosition, float oldRadius)
    {
        float radius = node.Radius;
        float3 rp = new float3(radius, radius, radius);
        float3 minPos = node.Pos - rp;
        float3 maxPos = node.Pos + rp;

        IcosepTree tree = this;

        while (tree.parent != null && tree.Contains(minPos, maxPos))
        {
            tree = tree.parent;
        }

        tree = tree.FindNode(node);

        if (tree != this)
        {
            tree.AddNode(node);

            if (!RemoveNode(node, oldPosition, oldRadius))
            {
                Debug.LogError("Failed to remove");
                return false;
            }
        }
        else
        {
            GrowExtents(minPos, maxPos);
            ShrinkExtents(oldPosition, oldRadius);
        }

        return true;
    }

    private int WhichChild(float3 pos, float radius)
    {
        if (!isSplit)
        {
            return -1;
        }

        if (extentsCorrect)
        {
            float sv = radius * 4 * math.PI / 3;
            float bv = (max.x - min.x) * (max.y - min.y) * (max.z - min.z);
            if (sv > bv / 8)
            {
                return -1;
            }
        }

        int c = 0;

        if (pos.x + radius < split.x)
        {
            c += 1;
        }
        else if (pos.y - radius > split.x)
        {
            c += 2;
        }

        if (pos.y + radius < split.y)
        {
            c += 3;
        }
        else if (pos.y - radius > split.y)
        {
            c += 6;
        }

        if (pos.z + radius < split.z)
        {
            c += 9;
        }
        else if (pos.z - radius > split.z)
        {
            c += 18;
        }

        return c;
    }

    private IcosepTree GetChild(int c)
    {
        if (c < 0)
        {
            return this;
        }

        if (Children[c] == null)
        {
            Children[c] = new IcosepTree(this);
        }

        return Children[c];
    }

    private void InsertNode(Node node)
    {
        nodes.Add(node);
        node.SetIcosepTree(this);
    }

    private void TestSuicide()
    {
        if (parent == null)
        {
            return;
        }

        bool shouldKill = nodes.Count == 0;

        for (int i = 0; shouldKill && i < Children.Length; i++)
        {
            shouldKill = Children[i] == null;
        }

        if (shouldKill)
        {
            parent.RemoveChild(this);
        }
    }

    private void RemoveChild(IcosepTree tree)
    {
        bool unsplit = true;

        for (int i = 0; i < Children.Length; i++)
        {
            if (Children[i] == tree)
            {
                Children[i] = null;
            }
            else if (Children[i] != null)
            {
                unsplit = false;
            }
        }

        if (unsplit)
        {
            Unsplit();
        }

        TestSuicide();
    }

    private void GrowExtents(float3 low, float3 high)
    {
        IcosepTree tree = this;
        while (tree != null)
        {
            if (tree.extentsCorrect)
            {
                tree.min = new float3(math.min(min.x, low.x),
                                      math.min(min.y, low.y),
                                      math.min(min.z, low.z));

                tree.max = new float3(math.max(max.x, high.x),
                                      math.max(max.y, high.y),
                                      math.max(max.z, high.z));
            }

            tree = tree.parent;
        }
    }

    private void ShrinkExtents(float3 pos, float radius)
    {
        IcosepTree tree = this;

        while (tree != null)
        {
            if (pos.x - radius <= tree.min.x
             || pos.y - radius <= tree.min.y
             || pos.z - radius <= tree.min.z
             || pos.x + radius >= tree.max.x
             || pos.y + radius >= tree.max.y
             || pos.z + radius >= tree.max.z)
            {
                tree.extentsCorrect = false;
            }

            tree = tree.parent;
        }
    }

    private void UpdateExtents()
    {
        bool set = false;

        for (int i = 0; i < nodes.Count; i++)
        {
            float3 pos = nodes[i].Pos;
            float radius = nodes[i].Radius;
            float3 low = new float3(pos.x - radius, pos.y - radius, pos.z - radius);
            float3 high = new float3(pos.x + radius, pos.y + radius, pos.z + radius);

            if (!set)
            {
                min = low;
                max = high;
                set = true;
                continue;
            }

            min = new float3(math.min(min.x, low.x),
                             math.min(min.y, low.y),
                             math.min(min.z, low.z));

            max = new float3(math.max(max.x, high.x),
                             math.max(max.y, high.y),
                             math.max(max.z, high.z));
        }

        for (int i = 0; i < Children.Length; i++)
        {
            if (Children[i] == null)
            {
                continue;
            }

            float3 low = float3.zero, high = float3.zero;

            Children[i].GetExtents(ref low, ref high, false);

            if (!set)
            {
                min = low;
                max = high;
                set = true;
                continue;
            }

            min = new float3(math.min(min.x, low.x),
                             math.min(min.y, low.y),
                             math.min(min.z, low.z));

            max = new float3(math.max(max.x, high.x),
                             math.max(max.y, high.y),
                             math.max(max.z, high.z));
        }

        extentsCorrect = true;
    }

    private bool Contains(float3 minPos, float3 maxPos)
    {
        return PartialContains(minPos, maxPos);

        return minPos.x >= min.x
            && minPos.y >= min.y
            && minPos.z >= min.z
            && maxPos.x <= max.x
            && maxPos.y <= max.y
            && maxPos.z <= max.z;
    }

    public bool PartialContains(float3 minPos, float3 maxPos)
    {
        return minPos.x <= max.x
            && maxPos.x >= min.x
            && minPos.y <= max.y
            && maxPos.y >= min.y
            && minPos.z <= max.z
            && maxPos.z >= min.z;
    }

    private void DoSplit()
    {
        if (isSplit)
        {
            Debug.LogError("Can't split already split tree");
            return;
        }

        split = float3.zero;
        float total = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            float3 pos = nodes[i].Pos;
            float radius = nodes[i].Radius;
            float rw = 1.0f / radius;

            split += (pos + new float3(radius, radius, radius)) * rw;
            total += rw;
        }

        split /= total;

        isSplit = true;

        bool safe = false;
        bool first = true;
        int lc = 0;
        List<Node> here = new List<Node>();

        for (int i = 0; i < nodes.Count; i++)
        {
            int c = WhichChild(nodes[i].Pos, nodes[i].Radius);

            if (!safe)
            {
                if (c < 0)
                {
                    safe = true;
                }
                else
                {
                    if (first)
                    {
                        lc = c;
                        first = false;
                    }
                    else
                    {
                        safe = c != lc;
                    }
                }
            }

            if (c < 0)
            {
                here.Add(nodes[i]);
            }
            else
            {
                GetChild(c).InsertNode(nodes[i]);
            }
        }

        nodes = here;

        if (!safe)
        {
            Debug.Log("<color=red>Yikes all nodes went to the same tree</color>");

            IcosepTree childTree = Children[lc];
            for (int i = childTree.nodes.Count - 1; i >= 0; i--)
            {
                Node node = childTree.nodes[i];
                childTree.RemoveNode(node);
                InsertNode(node);
            }

            isSplit = true;
        }

        for (int i = 0; i < Children.Length; i++)
        {
            if (Children[i] == null)
            {
                continue;
            }

            Children[i].UpdateExtents();
        }
    }

    private void Unsplit()
    {
        isSplit = false;

        for (int i = 0; i < Children.Length; i++)
        {
            Children[i] = null;
        }
    }
}
