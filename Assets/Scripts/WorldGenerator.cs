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
    const int scale = 32;
    const float wallWidth = 1f;

    [SerializeField] private AIInput _botPrefab;
    [SerializeField] private PlayerController _botTarget;
    [SerializeField] private int _botsPerBox = 25;

    public Dictionary<Vector3Int, Box> boxes = new();
    public Dictionary<Wall, WallBoxes> wallBoxes = new();
    public List<AIInput> bots = new();

    private Material _sharedWallMaterial;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly HashSet<Vector3Int> _filledBoxes = new();

    public void Awake()
    {
        if (_botTarget == null)
        {
            _botTarget = FindAnyObjectByType<PlayerInput>()?.GetComponent<PlayerController>();
        }

        SpawnFilled(new Vector3Int(0, 0, 0));
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
            box.top.wall = SpawnWall(up, WallNormalDirection.Y);
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
            box.bottom.wall = SpawnWall(pos, WallNormalDirection.Y);
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
            box.left.wall = SpawnWall(left, WallNormalDirection.X);
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
            box.right.wall = SpawnWall(pos, WallNormalDirection.X);
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
            box.front.wall = SpawnWall(forward, WallNormalDirection.Z);
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
            box.back.wall = SpawnWall(pos, WallNormalDirection.Z);
        }
        RegisterWallBox(box.back.wall, box);

        boxes[pos] = box;
    }

    public void SpawnNeighbors(Vector3Int pos)
    {
        Vector3Int up = pos + Vector3Int.up;
        SpawnFilled(up);

        Vector3Int down = pos + Vector3Int.down;
        SpawnFilled(down);

        Vector3Int left = pos + Vector3Int.left;
        SpawnFilled(left);

        Vector3Int right = pos + Vector3Int.right;
        SpawnFilled(right);

        Vector3Int forward = pos + Vector3Int.forward;
        SpawnFilled(forward);

        Vector3Int back = pos + Vector3Int.back;
        SpawnFilled(back);
    }

    public void SpawnFilled(Vector3Int pos)
    {
        Spawn(pos);
        FillBox(pos);
    }

    public void SpawnFilled(Vector3Int pos, Vector3 openingDirection)
    {
        Spawn(pos);
        FillBox(pos, openingDirection);
    }

    public void FillBox(Vector3Int pos)
    {
        FillBox(pos, Vector3.zero);
    }

    public void FillBox(Vector3Int pos, Vector3 openingDirection)
    {
        if (_filledBoxes.Contains(pos) ||
            !boxes.TryGetValue(pos, out _))
        {
            return;
        }

        if (_botPrefab == null)
        {
            Debug.LogWarning($"WorldGenerator cannot fill box {pos} because Bot Prefab is not assigned.", this);
            return;
        }

        CleanupMissingBots();

        for (int i = 0; i < _botsPerBox; i++)
        {
            AIInput bot = Instantiate(_botPrefab, GetBotSpawnPosition(pos, i), Quaternion.identity);
            bot.Target = _botTarget;
            bot.Destroyed += HandleBotDestroyed;
            bots.Add(bot);
            bot.SetVirtualAttachment(GetSpawnSurfaceNormal(openingDirection));
            bot.LaunchImmediately();
        }

        _filledBoxes.Add(pos);
    }

    private Wall SpawnWall(Vector3Int pos, WallNormalDirection direction)
    {
        GameObject wallObj = new($"Wall_{pos}_{direction}");
        wallObj.transform.SetParent(transform, false);
        
        Wall wall = wallObj.AddComponent<Wall>();
        wall.OnDestroy += () => HandleWallDestroyed(wall, pos, direction);
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
        worldPos *= scale;
        wallObj.transform.position = worldPos;

        MeshRenderer renderer = wallObj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = GetSharedWallMaterial();
        MaterialPropertyBlock propertyBlock = new();
        propertyBlock.SetColor(BaseColorId, Random.Range(0.2f, 0.5f) * Color.white);
        renderer.SetPropertyBlock(propertyBlock);

        MeshFilter filter = wallObj.AddComponent<MeshFilter>();
        filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        wallObj.AddComponent<BoxCollider>();

        switch (direction)
        {
            case WallNormalDirection.X:
                wallObj.transform.localScale = new Vector3(wallWidth, scale, scale);
                break;
            case WallNormalDirection.Y:
                wallObj.transform.localScale = new Vector3(scale, wallWidth, scale);
                break;
            case WallNormalDirection.Z:
                wallObj.transform.localScale = new Vector3(scale, scale, wallWidth);
                break;
        }

        return wall;
    }

    private void HandleWallDestroyed(Wall wall, Vector3Int wallPos, WallNormalDirection direction)
    {
        wallBoxes.Remove(wall);
        KillCharactersAttachedToWall(wall);
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

    private void KillCharactersAttachedToWall(Wall wall)
    {
        Collider wallCollider = wall.GetComponent<Collider>();
        if (wallCollider == null) return;

        for (int i = bots.Count - 1; i >= 0; i--)
        {
            if (bots[i] == null) continue;

            PlayerController controller = bots[i].GetComponent<PlayerController>();
            if (controller != null && controller.AttachedWallCollider == wallCollider)
            {
                Destroy(bots[i].gameObject);
            }
        }

        if (_botTarget != null && _botTarget.AttachedWallCollider == wallCollider)
        {
            Destroy(_botTarget.gameObject);
        }
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
        if (wall == null || box == null)
        {
            return;
        }

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

    private Vector3 GetBotSpawnPosition(Vector3Int boxPos, int botIndex)
    {
        return new Vector3(boxPos.x * scale, boxPos.y * scale, boxPos.z * scale);
    }

    private float GetBotHalfHeight()
    {
        if (_botPrefab.TryGetComponent<BoxCollider>(out BoxCollider boxCollider))
        {
            return Mathf.Max(boxCollider.size.y * Mathf.Abs(_botPrefab.transform.localScale.y) * 0.5f, 0.5f);
        }

        if (_botPrefab.TryGetComponent<SphereCollider>(out SphereCollider sphereCollider))
        {
            return Mathf.Max(sphereCollider.radius * Mathf.Abs(_botPrefab.transform.localScale.y), 0.5f);
        }

        if (_botPrefab.TryGetComponent<CapsuleCollider>(out CapsuleCollider capsuleCollider))
        {
            return Mathf.Max(capsuleCollider.height * Mathf.Abs(_botPrefab.transform.localScale.y) * 0.5f, 0.5f);
        }

        if (_botPrefab.GetComponent<Collider>() == null)
        {
            return 0.5f;
        }

        return 0.5f;
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

    private static Vector3 GetSpawnSurfaceNormal(Vector3 openingDirection)
    {
        return openingDirection.sqrMagnitude > Mathf.Epsilon
            ? openingDirection.normalized
            : Vector3.up;
    }

    private void HandleBotDestroyed(AIInput bot)
    {
        if (bot == null)
        {
            return;
        }

        bot.Destroyed -= HandleBotDestroyed;
        bots.Remove(bot);
    }

    private void CleanupMissingBots()
    {
        for (int i = bots.Count - 1; i >= 0; i--)
        {
            if (bots[i] == null)
            {
                bots.RemoveAt(i);
            }
        }
    }
}
