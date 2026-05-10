using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    private const string MultiplayerSceneName = "Multiplayer";
    private const string HostAddress = "192.168.0.104";
    private const ushort Port = 7777;

    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        // After returning from a multiplayer session NetworkManager persists in DontDestroyOnLoad,
        // so the Networking GameObject embedded in this scene becomes a duplicate — destroy it.
        if (NetworkManager.Singleton != null)
        {
            foreach (var nm in FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (nm != NetworkManager.Singleton && nm.gameObject.scene == gameObject.scene)
                    Destroy(nm.gameObject);
            }
        }
    }

    private void Start()
    {
        playButton.onClick.AddListener(() => SceneManager.LoadScene(1));
        quitButton.onClick.AddListener(Application.Quit);
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
    }

    private void StartHost()
    {
        if (!ConfigureTransport("0.0.0.0", "0.0.0.0")) return;
        if (!NetworkManager.Singleton.StartHost()) return;
        NetworkManager.Singleton.SceneManager.LoadScene(MultiplayerSceneName, LoadSceneMode.Single);
    }

    private void StartClient()
    {
        if (!ConfigureTransport(HostAddress, "0.0.0.0")) return;
        NetworkManager.Singleton.StartClient();
    }

    private static bool ConfigureTransport(string address, string listenAddress)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("NetworkManager.Singleton is missing from the Menu scene.");
            return false;
        }
        var transport = nm.GetComponent<UnityTransport>();
        transport.ConnectionData.Address = address;
        transport.ConnectionData.ServerListenAddress = listenAddress;
        transport.ConnectionData.Port = Port;
        return true;
    }

    
}
