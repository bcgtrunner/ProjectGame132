using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public enum WallNormalDirection
{
    X,
    Y,
    Z
}

public class Box
{
    public BoxSide top = new();
    public BoxSide bottom = new();
    public BoxSide left = new();
    public BoxSide right = new();
    public BoxSide front = new();
    public BoxSide back = new();

    public Vector3Int pos;
}

public class BoxSide
{
    public Wall wall;
    public Box box;
}

public class WorldGenerator : MonoBehaviour
{
    const int scale = 32;

    public Dictionary<Vector3Int, Box> boxes = new();

    public void Awake()
    {
        Spawn(new Vector3Int(0, 0, 0));
        SpawnNeighbors(new Vector3Int(0, 0, 0));
    }
    
    public void Spawn(Vector3Int pos)
    {
        if (boxes.ContainsKey(pos)) return;

        Box box = new()
        {
            pos = pos
        };

        Vector3Int up = pos + Vector3Int.up;
        if (boxes.TryGetValue(up, out Box upBox))
        {
            box.top.box = upBox;
            box.top.wall = upBox.bottom.wall;
        } else
        {
            box.top.wall = SpawnWall(up, WallNormalDirection.Y);
        }

        Vector3Int down = pos + Vector3Int.down;
        if (boxes.TryGetValue(down, out Box downBox))
        {
            box.bottom.box = downBox;
            box.bottom.wall = downBox.top.wall;
        } else
        {
            box.bottom.wall = SpawnWall(pos, WallNormalDirection.Y);
        }

        Vector3Int left = pos + Vector3Int.left;
        if (boxes.TryGetValue(left, out Box leftBox))
        {
            box.left.box = leftBox;
            box.left.wall = leftBox.right.wall;
        } else
        {
            box.left.wall = SpawnWall(left, WallNormalDirection.X);
        }

        Vector3Int right = pos + Vector3Int.right;
        if (boxes.TryGetValue(right, out Box rightBox))
        {
            box.right.box = rightBox;
            box.right.wall = rightBox.left.wall;
        } else
        {
            box.right.wall = SpawnWall(pos, WallNormalDirection.X);
        }

        Vector3Int forward = pos + Vector3Int.forward;
        if (boxes.TryGetValue(forward, out Box frontBox))
        {
            box.front.box = frontBox;
            box.front.wall = frontBox.back.wall;
        } else
        {
            box.front.wall = SpawnWall(forward, WallNormalDirection.Z);
        }

        Vector3Int back = pos + Vector3Int.back;
        if (boxes.TryGetValue(back, out Box backBox))
        {
            box.back.box = backBox;
            box.back.wall = backBox.front.wall;
        } else
        {
            box.back.wall = SpawnWall(pos, WallNormalDirection.Z);
        }

        boxes[pos] = box;
    }

    public void SpawnNeighbors(Vector3Int pos)
    {
        Vector3Int up = pos + Vector3Int.up;
        Spawn(up);

        Vector3Int down = pos + Vector3Int.down;
        Spawn(down);

        Vector3Int left = pos + Vector3Int.left;
        Spawn(left);

        Vector3Int right = pos + Vector3Int.right;
        Spawn(right);

        Vector3Int forward = pos + Vector3Int.forward;
        Spawn(forward);

        Vector3Int back = pos + Vector3Int.back;
        Spawn(back);
    }

    private Wall SpawnWall(Vector3Int pos, WallNormalDirection direction)
    {
        GameObject wallObj = new($"Wall_{pos}_{direction}");
        
        Wall wall = wallObj.AddComponent<Wall>();
        Vector3 worldPos = pos;
        if (direction == WallNormalDirection.X)
        {
            worldPos.x += 0.5f;
        }
        else if (direction == WallNormalDirection.Y)
        {
            worldPos.y -= 0.5f;
        } else if (direction == WallNormalDirection.Z)
        {
            worldPos.z -= 0.5f;
        }
        worldPos *= scale;
        wallObj.transform.position = worldPos;

        MeshRenderer renderer = wallObj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        renderer.sharedMaterial.color = Random.Range(0.2f, 0.5f) * Color.white;

        MeshFilter filter = wallObj.AddComponent<MeshFilter>();
        filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        BoxCollider collider = wallObj.AddComponent<BoxCollider>();

        switch (direction)
        {
            case WallNormalDirection.X:
                wallObj.transform.localScale = new Vector3(0.05f, scale, scale);
                break;
            case WallNormalDirection.Y:
                wallObj.transform.localScale = new Vector3(scale, 0.05f, scale);
                break;
            case WallNormalDirection.Z:
                wallObj.transform.localScale = new Vector3(scale, scale, 0.05f);
                break;
        }

        return wall;
    }
}