using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public enum CollisionType { BruteForce, HashMap, IcosepTree }

    public ObjectSpawner Spawner { get; set; }

    public Vector3 Velocity { get; set; }
    public bool Destroyable => false;

    [SerializeField]
    private float radius = 0.5f;

    [SerializeField]
    private CollisionType collisionType;

    public void Update()
    {
        transform.position += Velocity * Time.deltaTime;

        switch (collisionType)
        {
            case CollisionType.BruteForce:
                BruteForceCollision();
                break;
            case CollisionType.HashMap:
                HashMapCollision();
                break;
            case CollisionType.IcosepTree:
                IcosepTreeCollision();
                break;
            default:
                break;
        }
    }

    public void BruteForceCollision()
    {
        float3 pos = transform.position;
        for (int i = 0; i < Spawner.Nodes.Count; i++)
        {
            if (Spawner.Nodes[i] == null)
            {
                continue;
            }

            float distsq = math.distancesq(Spawner.Nodes[i].Pos, pos);

            if (distsq < 2.25f)
            {
                Spawner.Nodes[i].NodeObject.Destroy();
            }
        }
    }

    public void HashMapCollision()
    {
        float3 pos = transform.position;
        int3 key = Spawner.GetSimpleKey(pos);
        List<Node> nodes = Spawner.GetSurrounding(key);
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null)
            {
                continue;
            }

            float distsq = math.distancesq(nodes[i].Pos, pos);

            if (distsq < 2.25f)
            {
                nodes[i].NodeObject.Destroy();
            }
        }
    }

    private void IcosepTreeCollision()
    {
        List<Node> nodes = Spawner.IcosepTree.Query(transform.position, radius);
        if (nodes == null) return;

        float3 pos = transform.position;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (!nodes[i].NodeObject.Destroyable) continue;

            float distsq = math.distancesq(nodes[i].Pos, pos);

            if (distsq < 2.25f)
            {
                nodes[i].NodeObject.Destroy();
            }
        }
    }
}
