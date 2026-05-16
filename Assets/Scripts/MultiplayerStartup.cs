using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerStartup : MonoBehaviour
{
    private void Start()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsServer)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // Only react when we (the local client) get disconnected — e.g., host shut down the session.
        if (nm.IsServer) return;
        if (clientId != nm.LocalClientId) return;

        ReturnToMenu();
    }

    private static void ReturnToMenu()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening) nm.Shutdown();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }

    private void OnClientConnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (!nm.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null) return;

        client.PlayerObject.transform.position = NetworkPlayerSetup.GetSpawnOffset(clientId);

        if (clientId == nm.LocalClientId) return;

        var hostPlayer = nm.LocalClient?.PlayerObject;
        if (hostPlayer != null && hostPlayer.TryGetComponent<NetworkPlayerSetup>(out var setup))
            setup.SendWorldStateTo(clientId);
    }

    private void Update()
    {
        if (InputManager.Instance.Actions.UI.Escape.WasPerformedThisFrame())
        {
            ReturnToMenu();
        }
    }
}
