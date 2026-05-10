using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class NetworkPlayerSetup : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        var controller = GetComponent<PlayerController>();
        var input = GetComponent<PlayerInput>();

        if (!IsOwner)
        {
            if (input != null) input.enabled = false;
            controller.enabled = false;
            return;
        }
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
        if (!IsOwner) return;
        if (GameManager.Instance != null && GameManager.Instance.Player == GetComponent<PlayerController>())
            GameManager.Instance.Player = null;
    }

    [ServerRpc]
    public void RequestRestartServerRpc()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        foreach (var pair in nm.ConnectedClients)
        {
            var po = pair.Value.PlayerObject;
            if (po == null) continue;
            po.transform.position = GetSpawnOffset(pair.Key);
        }

        RegenerateClientRpc();
    }

    [ClientRpc]
    private void RegenerateClientRpc()
    {
        if (WorldGenerator.Instance != null)
            WorldGenerator.Instance.Regenerate();

        var local = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClient?.PlayerObject : null;
        if (local != null && local.TryGetComponent<PlayerController>(out var pc))
            pc.SetMaxHp(pc.MaxHp);
    }

    public static Vector3 GetSpawnOffset(ulong clientId)
    {
        return new Vector3((clientId % 4) * 2f, 0f, ((clientId / 4) % 4) * 2f);
    }
}
