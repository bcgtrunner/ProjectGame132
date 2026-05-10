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
}
