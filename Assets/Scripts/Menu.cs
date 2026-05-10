using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
        hostButton.onClick.AddListener(OnHostButtonClicked);
        clientButton.onClick.AddListener(OnClientButtonClicked);
    }

    private void OnPlayButtonClicked()
    {
        SceneManager.LoadScene(1);
    }
    private void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    private void OnHostButtonClicked()
    {
        if (NetworkManager.Singleton.IsHost) return;
        Debug.Log("Host button clicked");
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        
    }
    private void OnClientButtonClicked()
    {
        if (NetworkManager.Singleton.IsClient) return;
        Debug.Log("Client button clicked");
        var transport = GetComponent<UnityTransport>();
        transport.ConnectionData.Address = "10.10.228.132";
        transport.ConnectionData.Port = 7777;

        NetworkManager.Singleton.StartClient();
    }

    public void GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                Debug.Log(ip.ToString());
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        ToClient("Welcome to the server!", clientRpcParams);
    }

    [ClientRpc]
    private void ToClient(string message, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Server says: {message}");
    }
}
