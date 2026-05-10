using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class NetworkPlayerSetup : NetworkBehaviour
{
    public NetworkVariable<float> SyncedHp = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private PlayerController _controller;
    private float _lastSentHp;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        var controller = _controller;
        var input = GetComponent<PlayerInput>();

        SyncedHp.OnValueChanged += OnSyncedHpChanged;
        if (!IsOwner)
        {
            controller.SetRemoteHp(SyncedHp.Value);
        }

        if (!IsOwner)
        {
            if (input != null) input.enabled = false;
            controller.enabled = false;
            return;
        }
        _lastSentHp = controller.CurrentHp;
        SyncedHp.Value = controller.CurrentHp;
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
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || _controller == null) return;
        if (Mathf.Abs(_controller.CurrentHp - _lastSentHp) > 0.001f)
        {
            _lastSentHp = _controller.CurrentHp;
            SyncedHp.Value = _controller.CurrentHp;
        }
    }

    [ServerRpc]
    public void RequestRestartServerRpc()
    {
        RegenerateClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDealDamageServerRpc(NetworkObjectReference targetRef, float amount)
    {
        if (!targetRef.TryGet(out NetworkObject target)) return;
        if (!target.TryGetComponent<NetworkPlayerSetup>(out var targetSetup)) return;

        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { target.OwnerClientId } }
        };
        targetSetup.ApplyDamageClientRpc(amount, rpcParams);
    }

    [ClientRpc]
    public void ApplyDamageClientRpc(float amount, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        if (TryGetComponent<PlayerController>(out var pc))
            pc.TakeDamage(amount);
    }

    [ClientRpc]
    private void RegenerateClientRpc()
    {
        if (WorldGenerator.Instance != null)
            WorldGenerator.Instance.Regenerate();

        var nm = NetworkManager.Singleton;
        var local = nm != null ? nm.LocalClient?.PlayerObject : null;
        if (local != null)
        {
            local.transform.position = GetSpawnOffset(nm.LocalClientId);
            if (local.TryGetComponent<PlayerController>(out var pc))
            {
                pc.SetVirtualAttachment(Vector3.up);
                pc.SetMaxHp(pc.MaxHp);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnPaintServerRpc(Vector3 point, Vector3 normal, float scale, Color color)
    {
        SpawnPaintClientRpc(point, normal, scale, color);
    }

    [ClientRpc]
    private void SpawnPaintClientRpc(Vector3 point, Vector3 normal, float scale, Color color)
    {
        var nm = NetworkManager.Singleton;
        var local = nm != null ? nm.LocalClient?.PlayerObject : null;
        if (local != null && local.TryGetComponent<PaintShooter>(out var shooter))
            shooter.SpawnPaintAt(point, normal, scale, color);
    }

    public static Vector3 GetSpawnOffset(ulong clientId)
    {
        return new Vector3((clientId % 4) * 2f, 0f, ((clientId / 4) % 4) * 2f);
    }
}
