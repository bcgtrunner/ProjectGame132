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

    private TMP_InputField _nicknameInput;
    private List<string> ips = new();
    [SerializeField] private List<GameObject> connectButtons;

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
        InvokeRepeating("UpdateIPs", 0f, 0.5f);
        CreateNicknameInput();
    }

    private void CreateNicknameInput()
    {
        if (ipInput == null) return;
        var nickGO = Instantiate(ipInput.gameObject, ipInput.transform.parent);
        nickGO.name = "NicknameInput";
        var nickRect = nickGO.GetComponent<RectTransform>();
        var ipRect = ipInput.GetComponent<RectTransform>();
        nickRect.anchoredPosition = new Vector2(ipRect.anchoredPosition.x, ipRect.anchoredPosition.y + ipRect.rect.height + 8f);
        _nicknameInput = nickGO.GetComponent<TMP_InputField>();
        _nicknameInput.onValueChanged.RemoveAllListeners();
        _nicknameInput.characterLimit = 20;
        _nicknameInput.contentType = TMP_InputField.ContentType.Standard;
        _nicknameInput.text = PlayerNickname.Value;
        if (_nicknameInput.placeholder is TMP_Text placeholder)
            placeholder.text = "Nickname";
        _nicknameInput.onValueChanged.AddListener(val => PlayerNickname.Value = val);
    }

    void UpdateIPs()
    {
        for (int i = 0; i < ips.Count; i++)
        {
            var button = connectButtons[i];
            if (i >= 5 || button.activeSelf) continue;
            button.SetActive(true);
            string ip = ips[i];
            button.GetComponentInChildren<TMP_Text>().text = "Connect to " + ip;
            Debug.Log(i);
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                HostAddress = ip;
                StartClient();
            });
        }
    }

    public void OnIPReceived(string ip)
    {
        if (ips.Contains(ip) || ips.Count >= 6) return;
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
