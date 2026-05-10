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
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (!nm.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null) return;

        client.PlayerObject.transform.position = NetworkPlayerSetup.GetSpawnOffset(clientId);
    }

    private void Update()
    {
        if (InputManager.Instance.Actions.UI.Escape.WasPerformedThisFrame())
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            SceneManager.LoadScene(0);
        }
    }
}
