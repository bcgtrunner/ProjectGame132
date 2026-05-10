using System;
using System.Collections.Generic;
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

public class WallBoxes
{
    public Box first;
    public Box second;
}

public class WorldGenerator : MonoBehaviour
{
    public const int Scale = 32;
    const float wallWidth = 1f;

    public event Action<Vector3Int, Vector3> BoxOpened;
    public event Action<Wall> WallAboutToBeDestroyed;

    private readonly Dictionary<Vector3Int, Box> boxes = new();
    private readonly Dictionary<Wall, WallBoxes> wallBoxes = new();

    private Material _sharedWallMaterial;
    private Mesh _cachedCubeMesh;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public static WorldGenerator Instance;

    public void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);
        Spawn(Vector3Int.zero);
    }

    public void Start()
    {
        BoxOpened?.Invoke(Vector3Int.zero, Vector3.zero);
    }

    public void Regenerate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        boxes.Clear();
        wallBoxes.Clear();

        if (BotSpawner.Instance != null)
            BotSpawner.Instance.ResetState();

        Spawn(Vector3Int.zero);
        BoxOpened?.Invoke(Vector3Int.zero, Vector3.zero);
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
            upBox.bottom.box = box;
        } else
        {
            box.top.wall = SpawnWall(up, WallNormalDirection.Y, pos);
        }
        RegisterWallBox(box.top.wall, box);

        Vector3Int down = pos + Vector3Int.down;
        if (boxes.TryGetValue(down, out Box downBox))
        {
            box.bottom.box = downBox;
            box.bottom.wall = downBox.top.wall;
            downBox.top.box = box;
        } else
        {
            box.bottom.wall = SpawnWall(pos, WallNormalDirection.Y, pos);
        }
        RegisterWallBox(box.bottom.wall, box);

        Vector3Int left = pos + Vector3Int.left;
        if (boxes.TryGetValue(left, out Box leftBox))
        {
            box.left.box = leftBox;
            box.left.wall = leftBox.right.wall;
            leftBox.right.box = box;
        } else
        {
            box.left.wall = SpawnWall(left, WallNormalDirection.X, pos);
        }
        RegisterWallBox(box.left.wall, box);

        Vector3Int right = pos + Vector3Int.right;
        if (boxes.TryGetValue(right, out Box rightBox))
        {
            box.right.box = rightBox;
            box.right.wall = rightBox.left.wall;
            rightBox.left.box = box;
        } else
        {
            box.right.wall = SpawnWall(pos, WallNormalDirection.X, pos);
        }
        RegisterWallBox(box.right.wall, box);

        Vector3Int forward = pos + Vector3Int.forward;
        if (boxes.TryGetValue(forward, out Box frontBox))
        {
            box.front.box = frontBox;
            box.front.wall = frontBox.back.wall;
            frontBox.back.box = box;
        } else
        {
            box.front.wall = SpawnWall(forward, WallNormalDirection.Z, pos);
        }
        RegisterWallBox(box.front.wall, box);

        Vector3Int back = pos + Vector3Int.back;
        if (boxes.TryGetValue(back, out Box backBox))
        {
            box.back.box = backBox;
            box.back.wall = backBox.front.wall;
            backBox.front.box = box;
        } else
        {
            box.back.wall = SpawnWall(pos, WallNormalDirection.Z, pos);
        }
        RegisterWallBox(box.back.wall, box);

        boxes[pos] = box;
    }

    public void SpawnNeighbors(Vector3Int pos)
    {
        SpawnFilled(pos + Vector3Int.up);
        SpawnFilled(pos + Vector3Int.down);
        SpawnFilled(pos + Vector3Int.left);
        SpawnFilled(pos + Vector3Int.right);
        SpawnFilled(pos + Vector3Int.forward);
        SpawnFilled(pos + Vector3Int.back);
    }

    public void SpawnFilled(Vector3Int pos)
    {
        Spawn(pos);
        BoxOpened?.Invoke(pos, Vector3.zero);
    }

    public void SpawnFilled(Vector3Int pos, Vector3 openingDirection)
    {
        Spawn(pos);
        BoxOpened?.Invoke(pos, openingDirection);
    }

    protected virtual int GetWallHealth(Vector3Int boxPos)
    {
        int manhattan = Mathf.Abs(boxPos.x) + Mathf.Abs(boxPos.y) + Mathf.Abs(boxPos.z);
        return manhattan * 30 + 8;
    }

    public static Vector3 GetSpawnSurfaceNormal(Vector3 openingDirection)
    {
        return openingDirection.sqrMagnitude > Mathf.Epsilon
            ? openingDirection.normalized
            : Vector3.up;
    }

    private Wall SpawnWall(Vector3Int pos, WallNormalDirection direction, Vector3Int boxPos)
    {
        GameObject wallObj = new($"Wall_{pos}_{direction}");
        wallObj.transform.SetParent(transform, false);

        Wall wall = wallObj.AddComponent<Wall>();
        wall.MaxHealth = GetWallHealth(boxPos);
        wall.Destroyed += () => HandleWallDestroyed(wall, pos, direction);

        Vector3 worldPos = pos;
        if (direction == WallNormalDirection.X)
        {
            worldPos.x += 0.5f;
        }
        else if (direction == WallNormalDirection.Y)
        {
            worldPos.y -= 0.5f;
        }
        else if (direction == WallNormalDirection.Z)
        {
            worldPos.z -= 0.5f;
        }
        worldPos *= Scale;
        wallObj.transform.position = worldPos;

        MeshRenderer renderer = wallObj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = GetSharedWallMaterial();
        MaterialPropertyBlock propertyBlock = new();
        propertyBlock.SetColor(BaseColorId, UnityEngine.Random.Range(0.2f, 0.5f) * Color.white);
        renderer.SetPropertyBlock(propertyBlock);

        MeshFilter filter = wallObj.AddComponent<MeshFilter>();
        if (_cachedCubeMesh == null)
            _cachedCubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        filter.sharedMesh = _cachedCubeMesh;

        wallObj.AddComponent<BoxCollider>();

        switch (direction)
        {
            case WallNormalDirection.X:
                wallObj.transform.localScale = new Vector3(wallWidth, Scale, Scale);
                break;
            case WallNormalDirection.Y:
                wallObj.transform.localScale = new Vector3(Scale, wallWidth, Scale);
                break;
            case WallNormalDirection.Z:
                wallObj.transform.localScale = new Vector3(Scale, Scale, wallWidth);
                break;
        }

        return wall;
    }

    private void HandleWallDestroyed(Wall wall, Vector3Int wallPos, WallNormalDirection direction)
    {
        WallAboutToBeDestroyed?.Invoke(wall);
        wallBoxes.Remove(wall);

        GetAdjacentBoxPositions(wallPos, direction, out Vector3Int firstBoxPos, out Vector3Int secondBoxPos);

        bool hasFirstBox = boxes.ContainsKey(firstBoxPos);
        bool hasSecondBox = boxes.ContainsKey(secondBoxPos);

        if (!hasFirstBox && !hasSecondBox)
        {
            SpawnFilled(firstBoxPos);
            SpawnFilled(secondBoxPos);
            return;
        }

        if (hasFirstBox == hasSecondBox)
        {
            return;
        }

        Vector3Int openedBoxPos = hasFirstBox ? secondBoxPos : firstBoxPos;
        Vector3 openingDirection = GetOpeningDirectionForBox(openedBoxPos, wallPos, direction);
        SpawnFilled(openedBoxPos, openingDirection);
    }

    private static void GetAdjacentBoxPositions(
        Vector3Int wallPos,
        WallNormalDirection direction,
        out Vector3Int firstBoxPos,
        out Vector3Int secondBoxPos)
    {
        switch (direction)
        {
            case WallNormalDirection.X:
                firstBoxPos = wallPos;
                secondBoxPos = wallPos + Vector3Int.right;
                return;
            case WallNormalDirection.Y:
                firstBoxPos = wallPos + Vector3Int.down;
                secondBoxPos = wallPos;
                return;
            default:
                firstBoxPos = wallPos + Vector3Int.back;
                secondBoxPos = wallPos;
                return;
        }
    }

    private void RegisterWallBox(Wall wall, Box box)
    {
        if (wall == null || box == null) return;

        if (!wallBoxes.TryGetValue(wall, out WallBoxes adjacentBoxes))
        {
            adjacentBoxes = new WallBoxes();
            wallBoxes[wall] = adjacentBoxes;
        }

        if (adjacentBoxes.first == null)
        {
            adjacentBoxes.first = box;
            return;
        }

        if (adjacentBoxes.first != box && adjacentBoxes.second == null)
        {
            adjacentBoxes.second = box;
        }
    }

    private Material GetSharedWallMaterial()
    {
        if (_sharedWallMaterial == null)
        {
            _sharedWallMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }

        return _sharedWallMaterial;
    }

    private static Vector3 GetOpeningDirectionForBox(Vector3Int boxPos, Vector3Int wallPos, WallNormalDirection direction)
    {
        return direction switch
        {
            WallNormalDirection.X => boxPos == wallPos ? Vector3.right : Vector3.left,
            WallNormalDirection.Y => boxPos == wallPos ? Vector3.down : Vector3.up,
            _ => boxPos == wallPos ? Vector3.back : Vector3.forward
        };
    }
}
