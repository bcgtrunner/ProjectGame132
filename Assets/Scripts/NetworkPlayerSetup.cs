using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class NetworkPlayerSetup : NetworkBehaviour
{
    public NetworkVariable<float> SyncedHp = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public NetworkVariable<FixedString64Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> NicknameHidden = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> Kills = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Deaths = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private const float AttackerCreditWindow = 5f;

    private PlayerController _controller;
    private MeshRenderer _meshRenderer;
    private float _lastSentHp;
    private ulong _lastAttackerId;
    private bool _hasLastAttacker;
    private float _lastAttackerExpireTime;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        var controller = _controller;
        var input = GetComponent<PlayerInput>();

        SyncedHp.OnValueChanged += OnSyncedHpChanged;
        if (!IsOwner)
        {
            controller.SetRemoteHp(SyncedHp.Value);
            ApplyDeadVisibility(SyncedHp.Value);
        }

        if (!IsOwner)
        {
            if (input != null) input.enabled = false;
            controller.enabled = false;
            return;
        }
        _lastSentHp = controller.CurrentHp;
        SyncedHp.Value = controller.CurrentHp;
        Nickname.Value = new FixedString64Bytes(PlayerNickname.Value);
        if (GameManager.Instance != null) GameManager.Instance.Player = controller;

        var camera = Camera.main;
        if (camera != null)
        {
            var cameraController = camera.GetComponent<CameraController>();
            if (cameraController != null) cameraController.AssignPlayerController(controller);
        }
    }

    public override void OnNetworkDespawn()
    {
        SyncedHp.OnValueChanged -= OnSyncedHpChanged;
        if (!IsOwner) return;
        if (GameManager.Instance != null && GameManager.Instance.Player == GetComponent<PlayerController>())
            GameManager.Instance.Player = null;
    }

    private void OnSyncedHpChanged(float previous, float current)
    {
        if (IsOwner || _controller == null) return;
        _controller.SetRemoteHp(current);
        ApplyDeadVisibility(current);
    }

    private void ApplyDeadVisibility(float hp)
    {
        if (_meshRenderer != null) _meshRenderer.enabled = hp > 0f;
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || _controller == null) return;
        if (Mathf.Abs(_controller.CurrentHp - _lastSentHp) > 0.001f)
        {
            _lastSentHp = _controller.CurrentHp;
            SyncedHp.Value = _controller.CurrentHp;
        }

        if (!EscapeMenu.IsOpen && Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
            NicknameHidden.Value = !NicknameHidden.Value;
    }

    [ServerRpc]
    public void RequestRestartServerRpc()
    {
        RegenerateClientRpc();
    }

    public void SendWorldStateTo(ulong clientId)
    {
        if (WorldGenerator.Instance == null) return;
        WorldGenerator.Instance.CaptureState(
            out var boxPositions,
            out var destroyedWallPositions,
            out var destroyedWallDirections,
            out var damagedWallPositions,
            out var damagedWallDirections,
            out var damagedWallHealth,
            out var paintPoints,
            out var paintNormals,
            out var paintScales,
            out var paintColors);

        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
        SyncWorldStateClientRpc(
            boxPositions,
            destroyedWallPositions,
            destroyedWallDirections,
            damagedWallPositions,
            damagedWallDirections,
            damagedWallHealth,
            paintPoints,
            paintNormals,
            paintScales,
            paintColors,
            rpcParams);
    }

    [ClientRpc]
    private void SyncWorldStateClientRpc(
        Vector3Int[] boxPositions,
        Vector3Int[] destroyedWallPositions,
        int[] destroyedWallDirections,
        Vector3Int[] damagedWallPositions,
        int[] damagedWallDirections,
        float[] damagedWallHealth,
        Vector3[] paintPoints,
        Vector3[] paintNormals,
        float[] paintScales,
        Color[] paintColors,
        ClientRpcParams rpcParams = default)
    {
        if (WorldGenerator.Instance == null) return;
        WorldGenerator.Instance.ApplyServerWorldState(
            boxPositions,
            destroyedWallPositions,
            destroyedWallDirections,
            damagedWallPositions,
            damagedWallDirections,
            damagedWallHealth);

        if (paintPoints != null && paintPoints.Length > 0)
            StartCoroutine(SpawnPaintWhenReady(paintPoints, paintNormals, paintScales, paintColors));
    }

    private IEnumerator SpawnPaintWhenReady(Vector3[] points, Vector3[] normals, float[] scales, Color[] colors)
    {
        var nm = NetworkManager.Singleton;
        while (nm != null && nm.LocalClient?.PlayerObject == null)
            yield return null;

        Physics.SyncTransforms();

        var local = nm?.LocalClient?.PlayerObject;
        if (local == null || !local.TryGetComponent<PaintShooter>(out var shooter)) yield break;

        int n = Mathf.Min(points.Length, Mathf.Min(normals.Length, Mathf.Min(scales.Length, colors.Length)));
        for (int i = 0; i < n; i++)
            shooter.SpawnPaintAt(points[i], normals[i], scales[i], colors[i]);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDealDamageServerRpc(NetworkObjectReference targetRef, float amount, ServerRpcParams rpcParams = default)
    {
        if (!targetRef.TryGet(out NetworkObject target)) return;
        if (!target.TryGetComponent<NetworkPlayerSetup>(out var targetSetup)) return;
        ulong attackerId = rpcParams.Receive.SenderClientId;

        var clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { target.OwnerClientId } }
        };
        targetSetup.ApplyDamageClientRpc(amount, attackerId, clientParams);
    }

    [ClientRpc]
    public void ApplyDamageClientRpc(float amount, ulong attackerId, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        SetLastAttacker(attackerId);
        if (TryGetComponent<PlayerController>(out var pc))
            pc.TakeDamage(amount);
    }

    public void SetLastAttacker(ulong attackerId)
    {
        if (attackerId == OwnerClientId) return;
        _lastAttackerId = attackerId;
        _hasLastAttacker = true;
        _lastAttackerExpireTime = Time.time + AttackerCreditWindow;
    }

    public bool TryGetActiveAttacker(out ulong attackerId)
    {
        attackerId = _lastAttackerId;
        if (!_hasLastAttacker) return false;
        if (Time.time > _lastAttackerExpireTime) return false;
        if (attackerId == OwnerClientId) return false;
        return true;
    }

    [ServerRpc]
    public void ReportDeathServerRpc(ulong killerClientId, bool hasKiller)
    {
        Deaths.Value = Deaths.Value + 1;

        if (!hasKiller) return;
        if (killerClientId == OwnerClientId) return;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;
        if (!nm.ConnectedClients.TryGetValue(killerClientId, out var killer)) return;
        if (killer.PlayerObject == null) return;
        if (!killer.PlayerObject.TryGetComponent<NetworkPlayerSetup>(out var killerSetup)) return;

        killerSetup.Kills.Value = killerSetup.Kills.Value + 1;
    }

    [ClientRpc]
    private void RegenerateClientRpc()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResetRunState();

        if (WorldGenerator.Instance != null)
            WorldGenerator.Instance.Regenerate();

        var nm = NetworkManager.Singleton;
        var local = nm != null ? nm.LocalClient?.PlayerObject : null;
        if (local != null)
        {
            local.transform.position = GetSpawnOffset(nm.LocalClientId);
            if (local.TryGetComponent<PlayerController>(out var pc))
            {
                pc.SetFreeSpawnState();
                pc.SetMaxHp(pc.MaxHp);
            }
            if (local.TryGetComponent<PlayerInput>(out var pi))
            {
                pi.ResetState();
            }
        }
    }

    /// <summary>
    /// Single combined RPC to spawn paint then damage a wall (in that order).
    /// This prevents a race condition where two separate RPCs could arrive out of order,
    /// causing paint to be spawned after the wall is already destroyed (orphaned paint).
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnPaintAndDamageWallServerRpc(Vector3 point, Vector3 normal, float scale, Color color, Vector3Int wallPos, int direction, float amount)
    {
        WorldGenerator.Instance?.AddPaintRecord(point, normal, scale, color, wallPos, direction);
        SpawnPaintAndDamageWallClientRpc(point, normal, scale, color, wallPos, direction, amount);
    }

    [ClientRpc]
    private void SpawnPaintAndDamageWallClientRpc(Vector3 point, Vector3 normal, float scale, Color color, Vector3Int wallPos, int direction, float amount)
    {
        var nm = NetworkManager.Singleton;
        var local = nm != null ? nm.LocalClient?.PlayerObject : null;
        if (local != null && local.TryGetComponent<PaintShooter>(out var shooter))
        {
            // Spawn paint FIRST so it can attach to the wall via AttachTo()
            shooter.SpawnPaintAt(point, normal, scale, color);

            // Then damage the wall (may destroy it, triggering paint cleanup through Destroyed event)
            if (WorldGenerator.Instance != null && WorldGenerator.Instance.TryGetWall(wallPos, (WallNormalDirection)direction, out var wall) && wall != null)
                wall.DamageAt(point, amount);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnBulletServerRpc(Vector3 point, Vector3 dir, float scale, float speed, ServerRpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        var ids = nm.ConnectedClientsIds;
        var targets = new System.Collections.Generic.List<ulong>(ids.Count);
        for (int i = 0; i < ids.Count; i++)
            if (ids[i] != sender) targets.Add(ids[i]);
        if (targets.Count == 0) return;

        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = targets.ToArray() }
        };
        SpawnBulletClientRpc(point, dir, scale, speed, clientRpcParams);
    }

    [ClientRpc]
    private void SpawnBulletClientRpc(Vector3 point, Vector3 dir, float scale, float speed, ClientRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        var local = nm != null ? nm.LocalClient?.PlayerObject : null;
        if (local != null && local.TryGetComponent<PaintShooter>(out var shooter))
            shooter.SpawnVisualBullet(point, dir, scale, speed);
    }

    public static Vector3 GetSpawnOffset(ulong clientId)
    {
        return new Vector3((clientId % 4) * 2f, 0f, ((clientId / 4) % 4) * 2f);
    }
}
