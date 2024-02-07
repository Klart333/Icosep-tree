using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Thing : MonoBehaviour, INode
{
    public Node Node { get; set; }
    public ObjectSpawner Spawner { get; set; }
    public bool Destroyable => true;

    public Vector3 Velocity { get; set; }
    public int3 HashKey { get; set; }

    public void Destroy()
    {
        print("Destroying");

        if (Node.IcosepTree != null)
        {
            Node.IcosepTree.RemoveNode(Node);
        }

        Spawner.RemoveNode(Node, HashKey);
        Destroy(gameObject);
    }

    public void SetColor(Color color)
    {
        this.GetComponent<MeshRenderer>().material.color = color;
    }

    public void Move(bool withUpdate)
    {
        float3 oldPos = transform.position;

        transform.position += Velocity * Time.deltaTime;

        if (withUpdate)
        {
            Node.Pos = transform.position;
            Node.IcosepTree.UpdateNode(Node, oldPos, 1);
        }


        return;

        List<Node> potentialCollisions = Node.IcosepTree.GetNodes;
        float3 newDir = Velocity;
        for (int i = 0; i < potentialCollisions.Count; i++)
        {
            float distancesq = math.distancesq(transform.position, potentialCollisions[i].Pos);
            if (distancesq <= float.Epsilon || distancesq > 5)
            {
                continue;
            }

            float3 dir = math.normalize(potentialCollisions[i].Pos - Node.Pos);
            newDir += dir;

            if (distancesq < 1.6f)
            {
                newDir += -dir * 3f;
            }
        }

        Velocity = (Vector3)math.normalize(newDir) + Velocity * 0.5f;
    }
}

public interface INode
{
    public GameObject gameObject { get; }

    public bool Destroyable { get; }
    public void Destroy();
    public void Move(bool withUpdate = true);

    public void SetColor(Color color);

    // For HashMap
    public int3 HashKey { get; set; }
}