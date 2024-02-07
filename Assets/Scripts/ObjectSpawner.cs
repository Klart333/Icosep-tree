using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public Dictionary<int3, List<Node>> SimpleHashmap = new Dictionary<int3, List<Node>>();

    public List<Node> Nodes = new List<Node>();

    [Header("Things")]
    [SerializeField]
    private Thing thingPrefab;

    [SerializeField]
    private float spawnRadius = 5;

    [SerializeField]
    private int amount = 100;

    [SerializeField]
    private float speed = 2;

    [Header("Bullets")]
    [SerializeField]
    private Bullet bulletPrefab;

    [SerializeField]
    private int bulletAmount = 1;

    [SerializeField]
    private float bulletSpeed = 1;

    [Header("Sphere")]
    [SerializeField]
    private float area;

    [SerializeField]
    private Vector3 point;

    [SerializeField]
    private bool drawSphere;

    [Header("Tree")]
    [SerializeField]
    private bool updateExtents;

    [Header("Collision Type")]
    [SerializeField]
    private Bullet.CollisionType collisionType;

    public IcosepTree IcosepTree { get; private set; }

    private void Start()
    {
        IcosepTree = new IcosepTree(null);

        SpawnThings(amount);
    }

    private void Update()
    {
        if (speed > 0)
        {
            switch (collisionType)
            {
                case Bullet.CollisionType.BruteForce:
                    // Nothing to do
                    break;
                case Bullet.CollisionType.HashMap:

                    for (int i = 0; i < Nodes.Count; i++)
                    {
                        if (Nodes[i] == null)
                        {
                            continue;
                        }

                        Nodes[i].NodeObject.Move(false);

                        int3 newKey = GetSimpleKey(Nodes[i].NodeObject.gameObject.transform.position);
                        if (!newKey.Equals(Nodes[i].NodeObject.HashKey))
                        {
                            if (SimpleHashmap.ContainsKey(Nodes[i].NodeObject.HashKey))
                            {
                                SimpleHashmap[Nodes[i].NodeObject.HashKey].Remove(Nodes[i]);
                                if (SimpleHashmap[Nodes[i].NodeObject.HashKey].Count == 0)
                                {
                                    SimpleHashmap.Remove(Nodes[i].NodeObject.HashKey);
                                }
                            }


                            if (SimpleHashmap.ContainsKey(newKey))
                            {
                                SimpleHashmap[newKey].Add(Nodes[i]);
                            }
                            else
                            {
                                SimpleHashmap.Add(newKey, new List<Node> { Nodes[i] });
                            }

                            Nodes[i].NodeObject.HashKey = newKey;
                        }
                    }

                    break;
                case Bullet.CollisionType.IcosepTree:
                    for (int i = 0; i < Nodes.Count; i++)
                    {
                        if (i % 3 == 0)
                        {
                            Nodes[i].NodeObject.Move();
                        }
                    }
                    break;
                default:
                    break;
            }


        }

        if (updateExtents)
        {
            UpdateTreeExtents(IcosepTree);
        }

        if (!drawSphere)
        {
            return;
        }

        for (int i = 0; i < Nodes.Count; i++)
        {
            Nodes[i].NodeObject.SetColor(Color.red);
        }

        List<Node> colorNodes = IcosepTree.Query(point, area);

        for (int i = 0; i < colorNodes.Count; i++)
        {
            float distsq = math.distancesq(point, colorNodes[i].Pos);

            if (distsq < area * area)
            {
                colorNodes[i].NodeObject.SetColor(Color.green);
            }
            else
            {
                colorNodes[i].NodeObject.SetColor(Color.yellow);
            }
        }

    }

    public void SpawnThings(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector3 pos = UnityEngine.Random.insideUnitSphere * spawnRadius;
            Thing thing = Instantiate(thingPrefab, pos, Quaternion.identity);

            thing.Velocity = UnityEngine.Random.insideUnitSphere * speed;
            thing.Node = new Node { Pos = pos, Radius = 1, NodeObject = thing };
            thing.Spawner = this;

            switch (collisionType)
            {
                case Bullet.CollisionType.BruteForce:
                    // Nothing to do
                    break;
                case Bullet.CollisionType.HashMap:
                    int3 key = GetSimpleKey(pos);
                    thing.HashKey = key;
                    if (SimpleHashmap.ContainsKey(key))
                    {
                        SimpleHashmap[key].Add(thing.Node);
                    }
                    else
                    {
                        SimpleHashmap.Add(key, new List<Node>() { thing.Node });
                    }
                    break;
                case Bullet.CollisionType.IcosepTree:
                    IcosepTree.AddNode(thing.Node);
                    break;
                default:
                    break;
            }

            Nodes.Add(thing.Node);
        }

        // Split the tree
        float3 _min = float3.zero, _max = float3.zero;
        IcosepTree.GetExtents(ref _min, ref _max, true);
    }

    [ContextMenu("SpawnBullets")]
    public void SpawnBullets()
    {
        for (int i = 0; i < bulletAmount; i++)
        {
            Vector3 pos = UnityEngine.Random.insideUnitSphere.normalized * 1.5f * spawnRadius;
            Bullet bullet = Instantiate(bulletPrefab, pos, Quaternion.identity);

            bullet.Velocity = -pos.normalized * bulletSpeed;
            bullet.Spawner = this;
            //bullet.Node = new Node { Pos = pos, Radius = 0.5f, NodeObject = bullet };

            //IcosepTree.AddNode(bullet.Node);
        }
    }

    public void OnDrawGizmosSelected()
    {
        if (IcosepTree == null)
        {
            return;
        }

        switch (collisionType)
        {
            case Bullet.CollisionType.BruteForce:
                break;
            case Bullet.CollisionType.HashMap:

                float size = spawnRadius / 8.0f;
                Gizmos.color = Color.blue;
                foreach (float3 key in SimpleHashmap.Keys)
                {
                    Gizmos.DrawWireCube(key * size, Vector3.one * size);
                }
                break;

            case Bullet.CollisionType.IcosepTree:

                DrawTree(IcosepTree);

                if (!drawSphere)
                {
                    return;
                }
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(point, area);

                break;
            default:
                break;
        }

    }

    private void DrawTree(IcosepTree tree)
    {
        Gizmos.DrawWireCube((tree.Max + tree.Min) / 2.0f, tree.Max - tree.Min);


        for (int i = 0; i < tree.Children.Length; i++)
        {
            if (tree.Children[i] != null)
            {
                DrawTree(tree.Children[i]);
            }
        }
    }

    private void UpdateTreeExtents(IcosepTree tree)
    {
        float3 min = float3.zero, max = float3.zero;
        IcosepTree.GetExtents(ref min, ref max, false);

        for (int i = 0; i < tree.Children.Length; i++)
        {
            if (tree.Children[i] != null)
            {
                UpdateTreeExtents(tree.Children[i]);
            }
        }
    }

    public void RemoveNode(Node node, int3 hashKey)
    {
        switch (collisionType)
        {
            case Bullet.CollisionType.HashMap:

                if (SimpleHashmap.ContainsKey(hashKey))
                {
                    SimpleHashmap[hashKey].Remove(node);
                    if (SimpleHashmap[hashKey].Count == 0)
                    {
                        SimpleHashmap.Remove(hashKey);
                    }
                }

                break;
            default:
                break;
        }

        Nodes.Remove(node);
    }

    public int3 GetSimpleKey(float3 pos)
    {
        float size = spawnRadius / 8.0f;
        int x = (int)(pos.x / size);
        int y = (int)(pos.y / size);
        int z = (int)(pos.z / size);

        return new int3(x, y, z);
    }

    public List<Node> GetSurrounding(int3 key)
    {
        List<Node> result = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    int3 _key = new int3(key.x + x, key.y + y, key.z + z);
                    if (SimpleHashmap.ContainsKey(_key))
                    {
                        result.AddRange(SimpleHashmap[_key]);
                    }
                }
            }
        }

        return result;
    }
}

public class Node
{
    public INode NodeObject;
    public IcosepTree IcosepTree { get; private set; }
    public float3 Pos { get; set; }
    public float Radius { get; set; }

    public void SetIcosepTree(IcosepTree icosepTree)
    {
        this.IcosepTree = icosepTree;
    }
}