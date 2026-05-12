using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class Menu : MonoBehaviour
{
    private const string MultiplayerSceneName = "Multiplayer";
    private const ushort Port = 7777;

    private string HostAddress = "192.168.0.104";

    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_Text ipText;

    private List<string> ips;
    [SerializeField] private List<Button> connectButtons;

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
        connectButton.onClick.AddListener(StartClient);
        ipInput.onValueChanged.AddListener(ip => { if (NetUtils.IsIPCorrect(ip)) HostAddress = ip; });
        ipText.text = string.Join(' ', NetUtils.GetLocalIPAddress());
    }

    public void OnIPReceived(string ip)
    {
        if (ips.Contains(ip) || ips.Count == 6) return;
        Button newButton = connectButtons[ips.Count];
        newButton.gameObject.SetActive(true);
        newButton.GetComponent<TMP_Text>().text = $"Connect to {ip}";
        ips.Add(ip);
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
