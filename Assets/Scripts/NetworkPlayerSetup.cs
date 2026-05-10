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
        RegenerateClientRpc();
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

    public static Vector3 GetSpawnOffset(ulong clientId)
    {
        return new Vector3((clientId % 4) * 2f, 0f, ((clientId / 4) % 4) * 2f);
    }
}
